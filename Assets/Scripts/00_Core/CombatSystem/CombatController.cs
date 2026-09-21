using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace Core.CombatSystem
{
    /// <summary>
    /// Root of a combat encounter. The single point every Unit mutation goes through — nothing
    /// talks to another Unit directly, it always passes through here first. Drives the turn
    /// loop around TimelineController; AwaitInput/AISelect/ActionResolver don't exist yet, so
    /// EndTurn() currently has to be called from outside (see ExtendedDebug.CombatDebugHarness).
    /// </summary>
    [RequireComponent(typeof(TimelineController))]
    public class CombatController : MonoBehaviour
    {
        private TimelineController timeline;
        private List<CombatUnit> roster = new();

        /// <summary>Everyone in this encounter, alive or dead. Distinct from Timeline's queue,
        /// which only tracks turn order for units still waiting their turn.</summary>
        public IReadOnlyList<CombatUnit> Roster => roster;

        public CombatUnit CurrentActor { get; private set; }
        public bool IsCombatActive { get; private set; }

        public event Action<IReadOnlyList<CombatUnit>> OnCombatStart;
        public event Action<CombatUnit> OnTurnStart;
        public event Action<CombatUnit> OnTurnEnd;
        public event Action<CombatUnit> OnUnitKilled;
        public event Action<CombatUnit> OnUnitRevived;
        public event Action<COMBAT_OUTCOME> OnCombatEnd;

        private void Awake()
        {
            timeline = GetComponent<TimelineController>();
        }

        // ── Flow ──────────────────────────────────────────────────────────────

        public void StartCombat(IEnumerable<CombatUnit> units, UNIT_TEAM advantageTeam)
        {
            roster = units.ToList();
            IsCombatActive = true;

            timeline.Initialize(roster, advantageTeam);
            OnCombatStart?.Invoke(roster);

            AdvanceToNextTurn();
        }

        private void AdvanceToNextTurn()
        {
            CurrentActor = timeline.Pop();
            if (CurrentActor == null)
            {
                Debug.LogWarning($"[{nameof(CombatController)}] Timeline is empty — nothing to act.");
                return;
            }

            // TODO: clear the Defending status on CurrentActor once StatusBuffTracker exists.
            OnTurnStart?.Invoke(CurrentActor);
        }

        /// <summary>
        /// Closes out the current unit's turn. Called from outside for now — normally this
        /// would only fire once an action has fully resolved through ActionResolver.
        /// </summary>
        public void EndTurn()
        {
            if (!IsCombatActive || CurrentActor == null) return;

            CombatUnit finishedActor = CurrentActor;
            OnTurnEnd?.Invoke(finishedActor);
            timeline.Reinsert(finishedActor);
            CurrentActor = null;

            if (TryResolveOutcome(out COMBAT_OUTCOME outcome))
            {
                EndCombat(outcome);
                return;
            }

            AdvanceToNextTurn();
        }

        private void EndCombat(COMBAT_OUTCOME outcome)
        {
            IsCombatActive = false;
            timeline.Clear();
            OnCombatEnd?.Invoke(outcome);
        }

        private bool TryResolveOutcome(out COMBAT_OUTCOME outcome)
        {
            bool anyAllyAlive  = roster.Any(u => u.Team == UNIT_TEAM.Ally  && u.IsAlive);
            bool anyEnemyAlive = roster.Any(u => u.Team == UNIT_TEAM.Enemy && u.IsAlive);

            if (!anyEnemyAlive && anyAllyAlive) { outcome = COMBAT_OUTCOME.Victory; return true; }
            if (!anyAllyAlive)                  { outcome = COMBAT_OUTCOME.Defeat;  return true; }

            outcome = default;
            return false;
        }

        // ── Verbs (while ActionResolver do not exist) ──────────

        /// <summary>Marks a unit dead, pulls it out of the timeline entirely, notifies.</summary>
        public void Kill(CombatUnit unit)
        {
            if (!IsCombatActive || unit == null || !unit.IsAlive) return;

            unit.HP = 0;
            timeline.Remove(unit);
            OnUnitKilled?.Invoke(unit);

            if (TryResolveOutcome(out COMBAT_OUTCOME outcome))
                EndCombat(outcome);
        }

        /// <summary>Revives a dead unit with the given HP, reinserting it at the back of the queue.</summary>
        public void Revive(CombatUnit unit, int hp)
        {
            if (!IsCombatActive || unit == null || unit.IsAlive) return;

            unit.HP = Mathf.Max(1, hp);
            timeline.Reinsert(unit);
            OnUnitRevived?.Invoke(unit);
        }
    }
}