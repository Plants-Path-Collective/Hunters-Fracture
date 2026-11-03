using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{

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

            if (targetMode == EFFECT_TARGET_MODE.RightNeighborOfActionTargets)
            {
                foreach (Unit unit in context.targets)
                {
                    if (unit == null) continue;

                    Unit neighbor = FindRightNeighbor(unit, combat) ?? (unit.IsAlive ? unit : null);
                    if (neighbor != null && !result.Contains(neighbor))
                        result.Add(neighbor);
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

        /// <summary>Nearest living unit of the same team with a higher slot (Left → Mid → Right), or null.</summary>
        private static Unit FindRightNeighbor(Unit unit, CombatController combat)
        {
            if (!unit.Slot.HasValue) return null;

            Unit best = null;
            foreach (Unit candidate in combat.Roster)
            {
                if (candidate == unit || !candidate.IsAlive || candidate.Team != unit.Team) continue;
                if (!candidate.Slot.HasValue || candidate.Slot.Value <= unit.Slot.Value) continue;

                if (best == null || candidate.Slot.Value < best.Slot.Value)
                    best = candidate;
            }

            return best;
        }
    }
}