using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>
    /// Mirrors slots between the actor and each target: the target takes the actor's slot on its own
    /// side, the actor takes the target's slot on its side. Whoever held a destination slot takes
    /// the vacated one.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSwapSlotsEffect", menuName = "Skill System/Effects/Swap Slots With Target")]
    public class SwapSlotsWithTargetEffectSO : ActionEffectSO
    {
        public override void Apply(ActionContext context, CombatController combat)
        {
            Unit actor = context.actor;

            foreach (Unit target in ResolveTargets(context, combat))
            {
                if (!actor.Slot.HasValue || !target.Slot.HasValue) continue;

                // Read both slots before moving anyone.
                COMBAT_SLOT actorSlot = actor.Slot.Value;
                COMBAT_SLOT targetSlot = target.Slot.Value;
                if (actorSlot == targetSlot) continue;

                combat.MoveToSlot(target, actorSlot);
                combat.MoveToSlot(actor, targetSlot);
            }
        }
    }
}