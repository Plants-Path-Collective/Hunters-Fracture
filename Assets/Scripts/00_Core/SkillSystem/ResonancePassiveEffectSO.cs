using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>
    /// Fasila's passive. Every technique the owner uses adds a fragment; with enough fragments the
    /// next technique runs with a doubled effectScale and the count resets (the doubled one adds none).
    /// </summary>
    [CreateAssetMenu(fileName = "ResonancePassive", menuName = "Skill System/Passives/Resonance")]
    public class ResonancePassiveEffectSO : TriggeredEffectSO
    {
        [SerializeField, Min(1)] private int fragmentsNeeded = 3;
        [SerializeField, Min(1f)] private float doubledScale = 2f;

        // The asset is shared, so per-unit state is keyed by Unit and removed in Detach().
        private class State
        {
            public int fragments;
            public bool doubling;
            public Action<ActionContext> before;
            public Action<ActionContext> after;
        }

        private readonly Dictionary<Unit, State> states = new();

        public int GetFragments(Unit unit) => states.TryGetValue(unit, out State s) ? s.fragments : 0;

        public override void Attach(Unit unit)
        {
            if (unit.Combat == null) return;

            var state = new State();

            state.before = context =>
            {
                if (context.actor != unit) return;

                state.doubling = state.fragments >= fragmentsNeeded;
                if (state.doubling) context.effectScale *= doubledScale;
            };

            state.after = context =>
            {
                if (context.actor != unit) return;

                if (state.doubling) { state.fragments = 0; state.doubling = false; }
                else state.fragments = Mathf.Min(fragmentsNeeded, state.fragments + 1);
            };

            states[unit] = state;
            unit.Combat.OnBeforeAction += state.before;
            unit.Combat.OnAfterAction += state.after;
        }

        public override void Detach(Unit unit)
        {
            if (!states.TryGetValue(unit, out State state)) return;

            if (unit.Combat != null)
            {
                unit.Combat.OnBeforeAction -= state.before;
                unit.Combat.OnAfterAction -= state.after;
            }

            states.Remove(unit);
        }
    }
}