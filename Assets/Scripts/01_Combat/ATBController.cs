using System;
using System.Collections.Generic;
using Core;
using CombatSystem.UnitSystem;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Drives the Active Time Battle loop.
    /// Each frame it accumulates atbPoints for every living unit based on its Speed.
    /// When one or more units reach the threshold in the same frame, it fires the
    /// appropriate event for CombatManager to handle.
    ///
    /// ATB formula: atbPoints += speed * Time.deltaTime
    /// Example: speed=100, threshold=1000 → full bar fills in 10 seconds.
    /// </summary>
    public class ATBController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired when exactly one unit reaches the threshold.</summary>
        public event Action<Unit> OnUnitReady;

        /// <summary>Fired when two units reach the threshold in the same frame.</summary>
        public event Action<SIMULTANEOUS_EVENT_TYPE, Unit, Unit> OnSimultaneousTurn;

        // ── Configuration ─────────────────────────────────────────────────────
        [Tooltip("Points required to fill the ATB bar and trigger a turn.")]
        [SerializeField] private float threshold = 1000f;

        // ── State ─────────────────────────────────────────────────────────────
        private List<Unit> _allUnits = new();
        private bool       _ticking  = false;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetUnits(List<Unit> allies, List<Unit> enemies)
        {
            _allUnits.Clear();
            _allUnits.AddRange(allies);
            _allUnits.AddRange(enemies);
        }

        public void StartTicking() => _ticking = true;
        public void Pause()        => _ticking = false;
        public void Resume()       => _ticking = true;

        // ── Loop ──────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_ticking) return;

            // Accumulate points for every living unit
            foreach (Unit unit in _allUnits)
            {
                if (unit.State != Core.UNIT_STATE.Alive) continue;

                unit.ATBPoints += unit.Speed * Time.deltaTime;
                unit.BroadcastATBProgress(threshold);
            }

            // Collect units that crossed the threshold this frame
            var ready = new List<Unit>();
            foreach (Unit unit in _allUnits)
            {
                if (unit.State == Core.UNIT_STATE.Alive && unit.ATBPoints >= threshold)
                    ready.Add(unit);
            }

            if (ready.Count == 0) return;

            Pause(); // Always pause before dispatching — CombatManager resumes when appropriate

            if (ready.Count >= 2)
            {
                // Determine event type by checking if the two fastest are on opposite sides
                Unit a = ready[0];
                Unit b = ready[1];

                SIMULTANEOUS_EVENT_TYPE eventType =
                    (a.UnitType != b.UnitType)
                        ? SIMULTANEOUS_EVENT_TYPE.Disputa
                        : SIMULTANEOUS_EVENT_TYPE.Alianza;

                // Units past the second one are edge cases (3 simultaneous);
                // reset their ATB so they act next round in speed order
                for (int i = 2; i < ready.Count; i++)
                    ready[i].ATBPoints = threshold - 1f;

                OnSimultaneousTurn?.Invoke(eventType, a, b);
            }
            else
            {
                OnUnitReady?.Invoke(ready[0]);
            }
        }
    }
}
