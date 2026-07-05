using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using CombatSystem.UnitSystem;
using UnityEngine;

namespace CombatSystem.Minigame
{
    /// <summary>
    /// Orchestrates both minigame types (Disputa and Alianza).
    ///
    /// Flow:
    ///   1. CombatManager calls Launch(type, unitA, unitB).
    ///   2. MinigameController generates a sequence and runs it step by step.
    ///   3. Each step broadcasts its required button to the UI and opens
    ///      an input window via MinigameInputHandler.
    ///   4. After the last step, compares Potencia and fires:
    ///        OnDisputaResolved(winner, loser, loserPotencia)
    ///        OnAlianzaResolved(unitA, unitB, potencia)
    ///   5. CombatManager receives the result and calls ActionResolver.
    /// </summary>
    public class MinigameController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action<Unit, Unit, int> OnDisputaResolved; // (winner, loser, loserPotencia)
        public event Action<Unit, Unit, int> OnAlianzaResolved; // (unitA, unitB, totalPotencia)

        /// <summary>Fired at the start of each step so the UI can show the prompt.</summary>
        public event Action<MinigameStep, int, int> OnStepStarted; // (step, stepIndex, totalSteps)

        /// <summary>Fired when a step is resolved (hit or miss).</summary>
        public event Action<bool> OnStepResult; // (wasHit)

        // ── Dependencies ──────────────────────────────────────────────────────
        [SerializeField] private MinigameSequence    sequence;
        [SerializeField] private MinigameInputHandler inputHandler;

        // ── State ─────────────────────────────────────────────────────────────
        private List<MinigameStep> _steps;
        private FACE_BUTTON        _requiredButton;
        private bool               _stepWindowOpen;
        private bool               _stepHit;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by CombatManager when two units reach ATB simultaneously.
        /// unitA is always the ally (or fastest ally in Alianza).
        /// </summary>
        public void Launch(SIMULTANEOUS_EVENT_TYPE type, Unit unitA, Unit unitB)
        {
            _steps = sequence.Generate();
            StartCoroutine(type == SIMULTANEOUS_EVENT_TYPE.Disputa
                ? RunDisputa(unitA, unitB)
                : RunAlianza(unitA, unitB));
        }

        // ── Disputa ───────────────────────────────────────────────────────────

        /// <summary>
        /// Disputa: enemy attacks in 3–5 steps. Player defends.
        /// Correct input → player gains Potencia.
        /// Incorrect / missed → enemy gains Potencia.
        /// </summary>
        private IEnumerator RunDisputa(Unit ally, Unit enemy)
        {
            inputHandler.StartListening();
            inputHandler.OnButtonPressed += OnButtonPressed;

            int allyPotencia  = 0;
            int enemyPotencia = 0;

            for (int i = 0; i < _steps.Count; i++)
            {
                MinigameStep step = _steps[i];
                OnStepStarted?.Invoke(step, i, _steps.Count);

                yield return RunStep(step);

                bool hit = step.wasHit;
                OnStepResult?.Invoke(hit);

                if (hit) allyPotencia++;
                else     enemyPotencia++;
            }

            inputHandler.StopListening();
            inputHandler.OnButtonPressed -= OnButtonPressed;

            // Determine winner
            Unit winner, loser;
            int  loserPotencia;

            if (allyPotencia >= enemyPotencia)
            {
                winner       = ally;
                loser        = enemy;
                loserPotencia = enemyPotencia;
            }
            else
            {
                winner       = enemy;
                loser        = ally;
                loserPotencia = allyPotencia;
            }

            OnDisputaResolved?.Invoke(winner, loser, loserPotencia);
        }

        // ── Alianza ───────────────────────────────────────────────────────────

        /// <summary>
        /// Alianza: player cooperates in 3–5 steps to build Potencia.
        /// Correct input → +1 Potencia.
        /// Missed → no Potencia for that step (sequence continues normally).
        /// </summary>
        private IEnumerator RunAlianza(Unit unitA, Unit unitB)
        {
            inputHandler.StartListening();
            inputHandler.OnButtonPressed += OnButtonPressed;

            int potencia = 0;

            for (int i = 0; i < _steps.Count; i++)
            {
                MinigameStep step = _steps[i];
                OnStepStarted?.Invoke(step, i, _steps.Count);

                yield return RunStep(step);

                bool hit = step.wasHit;
                OnStepResult?.Invoke(hit);

                if (hit) potencia++;
            }

            inputHandler.StopListening();
            inputHandler.OnButtonPressed -= OnButtonPressed;

            OnAlianzaResolved?.Invoke(unitA, unitB, potencia);
        }

        // ── Step execution ────────────────────────────────────────────────────

        /// <summary>
        /// Opens the input window for one step and waits for it to close.
        /// The window duration is defined by step.windowEnd - step.windowStart.
        /// </summary>
        private IEnumerator RunStep(MinigameStep step)
        {
            _requiredButton = step.button;
            _stepWindowOpen = true;
            _stepHit        = false;

            float duration = step.windowEnd - step.windowStart;
            float elapsed  = 0f;

            while (elapsed < duration && !_stepHit)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _stepWindowOpen = false;
            step.wasHit     = _stepHit;

            // Brief pause between steps so the UI can show the result
            yield return new WaitForSeconds(0.2f);
        }

        // ── Input callback ────────────────────────────────────────────────────

        private void OnButtonPressed(FACE_BUTTON pressed)
        {
            if (!_stepWindowOpen) return;
            if (pressed == _requiredButton)
                _stepHit = true;
            // Wrong button press counts as a miss — window stays open until timeout
        }
    }
}
