using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>
    /// Moves each target to a random slot of its own team, different from its current one.
    /// Whoever holds the destination slot swaps with it.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMoveToRandomSlotEffect", menuName = "Skill System/Effects/Move To Random Slot")]
    public class MoveToRandomSlotEffectSO : ActionEffectSO
    {
        private static readonly COMBAT_SLOT[] AllSlots = { COMBAT_SLOT.Left, COMBAT_SLOT.Mid, COMBAT_SLOT.Right };

        [Tooltip("If on, the target only swaps with a living teammate (it never moves to an empty slot). " +
                 "If no teammate exists, it stays where it is.")]
        [SerializeField] private bool onlyOccupiedSlots;

        public override void Apply(ActionContext context, CombatController combat)
        {
            foreach (Unit target in ResolveTargets(context, combat))
            {
                if (!target.Slot.HasValue) continue;

                var options = new List<COMBAT_SLOT>();
                foreach (COMBAT_SLOT slot in AllSlots)
                {
                    if (slot == target.Slot.Value) continue;

                    bool occupied = combat.Roster.Any(u =>
                        u != target && u.IsAlive && u.Team == target.Team && u.Slot == slot);
                    if (onlyOccupiedSlots && !occupied) continue;

                    options.Add(slot);
                }

                if (options.Count == 0) continue;
                combat.MoveToSlot(target, options[Random.Range(0, options.Count)]);
            }
        }
    }
}