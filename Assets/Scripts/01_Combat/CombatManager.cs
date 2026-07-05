using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using CombatSystem.Minigame;
using CombatSystem.UnitSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace CombatSystem
{
    /// <summary>
    /// Director of the combat encounter.
    /// Owns the COMBAT_STATE machine and wires together:
    ///   ATBController → TurnHandler → ActionResolver → MinigameController
    ///
    /// Receives populated unit lists from UnitSpawner via Initialize().
    /// Drives the full loop shown in the combat flowchart.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action<COMBAT_STATE>  OnStateChanged;
        public event Action<Unit>          OnTurnStarted;    // UI highlights active unit
        public event Action<COMBAT_RESULT> OnCombatEnded;

        // ── Dependencies ──────────────────────────────────────────────────────
        [SerializeField] private ATBController      ATBController;
        [SerializeField] private TurnHandler        turnHandler;
        [SerializeField] private ActionResolver     actionResolver;
        [SerializeField] private MinigameController minigameController;

        [Header("Scene")]
        [Tooltip("Name of the scene to load after combat ends.")]
        [SerializeField] private string overworldScene = "Overworld";

        [Tooltip("Seconds to wait after combat ends before transitioning.")]
        [SerializeField] private float endDelay = 2f;

        // ── State ─────────────────────────────────────────────────────────────
        public COMBAT_STATE State { get; private set; } = COMBAT_STATE.Inactive;

        private List<Unit> _allies  = new();
        private List<Unit> _enemies = new();

        // ── Initialization ────────────────────────────────────────────────────

        /// <summary>Called by UnitSpawner after all units are instantiated.</summary>
        public void Initialize(List<Unit> allies, List<Unit> enemies)
        {
            _allies  = allies;
            _enemies = enemies;

            // Register death listeners
            foreach (Unit u in _allies)  u.OnDeath += OnUnitDied;
            foreach (Unit u in _enemies) u.OnDeath += OnUnitDied;

            // Wire ATBController
            ATBController.SetUnits(_allies, _enemies);
            ATBController.OnUnitReady        += HandleUnitReady;
            ATBController.OnSimultaneousTurn += HandleSimultaneousTurn;

            // Wire TurnHandler
            turnHandler.SetParties(_allies, _enemies);
            turnHandler.OnTurnResolved += HandleTurnResolved;
            turnHandler.OnFled         += HandleFled;

            // Wire MinigameController
            minigameController.OnDisputaResolved += HandleDisputaResolved;
            minigameController.OnAlianzaResolved += HandleAlianzaResolved;

            StartCombat();
        }

        // ── Combat lifecycle ──────────────────────────────────────────────────

        private void StartCombat()
        {
            SetState(COMBAT_STATE.Initializing);
            SetState(COMBAT_STATE.ATBRunning);
            ATBController.StartTicking();
        }

        private void SetState(COMBAT_STATE newState)
        {
            State = newState;
            OnStateChanged?.Invoke(State);
        }

        // ── ATB callbacks ─────────────────────────────────────────────────────

        private void HandleUnitReady(Unit unit)
        {
            // ATBController already paused itself before firing this event
            SetState(COMBAT_STATE.TurnPending);
            OnTurnStarted?.Invoke(unit);
            turnHandler.HandleTurn(unit);
        }

        private void HandleSimultaneousTurn(SIMULTANEOUS_EVENT_TYPE type, Unit unitA, Unit unitB)
        {
            SetState(COMBAT_STATE.Minigame);

            // Ensure unitA is always an ally for consistency inside the minigame controllers
            Unit ally  = unitA.UnitType == UNIT_TYPE.AllyUnit ? unitA : unitB;
            Unit other = unitA.UnitType == UNIT_TYPE.AllyUnit ? unitB : unitA;

            // Switch to Combat input map so the minigame can receive input
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Combat);

            minigameController.Launch(type, ally, other);
        }

        // ── TurnHandler callbacks ─────────────────────────────────────────────

        private void HandleTurnResolved()
        {
            CheckEndCondition();
        }

        private void HandleFled()
        {
            EndCombat(COMBAT_RESULT.Fled);
        }

        // ── Minigame callbacks ────────────────────────────────────────────────

        private void HandleDisputaResolved(Unit winner, Unit loser, int loserPotencia)
        {
            actionResolver.ResolveDisputaResult(winner, loser, loserPotencia);

            if (CheckEndCondition()) return;

            // Winner keeps their turn; if winner is an ally, let player act
            SetState(COMBAT_STATE.TurnPending);
            InputManager.Instance.ChangeActionMap(
                winner.UnitType == UNIT_TYPE.AllyUnit
                    ? INPUTACTION_MAP.Combat
                    : INPUTACTION_MAP.Empty);

            OnTurnStarted?.Invoke(winner);
            turnHandler.HandleTurn(winner);
        }

        private void HandleAlianzaResolved(Unit unitA, Unit unitB, int potencia)
        {
            // Collect all living enemies as targets for the combined attack
            var targets = new List<Unit>();
            foreach (Unit e in _enemies)
                if (e.State == UNIT_STATE.Alive) targets.Add(e);

            actionResolver.ResolveAlianzaAttack(unitA, unitB, potencia, targets);

            if (CheckEndCondition()) return;

            // Both units acted — reset ATB, resume normal loop
            unitA.ResetATB();
            unitB.ResetATB();

            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty);
            ResumeATB();
        }

        // ── Death handler ─────────────────────────────────────────────────────

        private void OnUnitDied(Unit unit)
        {
            Debug.Log($"[CombatManager] {unit.UnitName} has fallen.");
            // CheckEndCondition is called after each action resolves,
            // so we don't need to do it here — avoids double-checking.
        }

        // ── End condition ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if combat ended, false if it continues.
        /// </summary>
        private bool CheckEndCondition()
        {
            bool alliesAlive  = AnyAlive(_allies);
            bool enemiesAlive = AnyAlive(_enemies);

            if (!alliesAlive)  { EndCombat(COMBAT_RESULT.Defeat);  return true; }
            if (!enemiesAlive) { EndCombat(COMBAT_RESULT.Victory);  return true; }

            return false;
        }

        private static bool AnyAlive(List<Unit> units)
        {
            foreach (Unit u in units)
                if (u.State == UNIT_STATE.Alive) return true;
            return false;
        }

        // ── End combat ────────────────────────────────────────────────────────

        private void EndCombat(COMBAT_RESULT result)
        {
            ATBController.Pause();
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty);

            COMBAT_STATE endState = result == COMBAT_RESULT.Victory
                ? COMBAT_STATE.Victory : COMBAT_STATE.Defeat;
            SetState(endState);

            CombatContext.Instance.SetResult(result);
            OnCombatEnded?.Invoke(result);

            StartCoroutine(ReturnToOverworld());
        }

        private IEnumerator ReturnToOverworld()
        {
            yield return new WaitForSeconds(endDelay);

            string scene = CombatContext.Instance.ReturnScene;
            if (string.IsNullOrEmpty(scene)) scene = overworldScene;
            SceneManager.LoadScene(scene);
        }

        // ── Resume helper ─────────────────────────────────────────────────────

        private void ResumeATB()
        {
            SetState(COMBAT_STATE.ATBRunning);
            ATBController.Resume();
        }

        // Exposed so TurnHandler's FinishTurn path can trigger resume
        public void OnActionResolved()
        {
            if (CheckEndCondition()) return;
            ResumeATB();
        }
    }
}
