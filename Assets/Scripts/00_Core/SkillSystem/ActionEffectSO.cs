using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    public enum EFFECT_TARGET_MODE
    {
        ActionTargets,            // every unit the action resolved to
        ActionTargetsAlliesOnly,  // ...only those on the actor's side
        ActionTargetsEnemiesOnly, // ...only those on the other side
        SlotsOfSide               // units picked by side and slot, ignoring the player's target
    }

    /// <summary>Abstract base of one step of an ActionSO. Subclasses implement Apply().</summary>
    public abstract class ActionEffectSO : ScriptableObject
    {
        [Header("--- Targets ---")]
        [SerializeField] private EFFECT_TARGET_MODE targetMode = EFFECT_TARGET_MODE.ActionTargets;
        [Tooltip("SlotsOfSide only.")]
        [SerializeField] private RELATIVE_SIDE side = RELATIVE_SIDE.Enemies;
        [Tooltip("SlotsOfSide only: which slots of that side are affected.")]
        [SerializeField] private SLOT_MASK slots = SLOT_MASK.None;

        public abstract void Apply(ActionContext context, CombatController combat);

        /// <summary>Living units this effect touches, after the side/slot rules.</summary>
        protected List<Unit> ResolveTargets(ActionContext context, CombatController combat)
        {
            var result = new List<Unit>();

            if (targetMode == EFFECT_TARGET_MODE.SlotsOfSide)
            {
                foreach (Unit unit in combat.Roster)
                {
                    if (!unit.IsAlive || !unit.Slot.HasValue) continue;
                    if ((unit.Team == context.actor.Team) != (side == RELATIVE_SIDE.Allies)) continue;
                    if (!slots.HasFlag(ToMask(unit.Slot.Value))) continue;
                    result.Add(unit);
                }

                return result;
            }

            foreach (Unit unit in context.targets)
            {
                if (unit == null || !unit.IsAlive) continue;

                bool ally = unit.Team == context.actor.Team;
                if (targetMode == EFFECT_TARGET_MODE.ActionTargetsAlliesOnly && !ally) continue;
                if (targetMode == EFFECT_TARGET_MODE.ActionTargetsEnemiesOnly && ally) continue;
                result.Add(unit);
            }

            return result;
        }

        private static SLOT_MASK ToMask(COMBAT_SLOT slot) => slot switch
        {
            COMBAT_SLOT.Left => SLOT_MASK.Left,
            COMBAT_SLOT.Mid => SLOT_MASK.Mid,
            _ => SLOT_MASK.Right
        };
    }
}