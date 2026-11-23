using UnityEngine;
using Core.CombatSystem.SkillSystem;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 011 (Darius Passive): every enemy the owner's attack or skill reaches gets one more stack of
    /// Hemorrhage (damage per turn, less Speed, less Physical Defense). One stack per target per action.
    /// </summary>
    /// <remarks>
    /// Does not scale with copies. The status keeps the stack count in its magnitude: this effect reads
    /// the current stacks from the target and re-applies the status with one more.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewBleed", menuName = "Item System/Effects/Types/Bleed")]
    public class BleedEffectSO : ActionHookEffectSO
    {
        [Tooltip("HemorrhageStatusSO asset with isDebuff enabled.")]
        [SerializeField] private HemorrhageStatusSO hemorrhage;
        [Tooltip("Turns of the target. A huge value means 'rest of the combat'.")]
        [SerializeField, Min(1)] private int durationTurns = 999;

        protected override void OnOwnerAfterAction(Unit owner, ActionContext context)
        {
            CombatController combat = owner.Combat;
            if (hemorrhage == null || combat == null || !combat.IsCombatActive || !owner.IsAlive) return;

            foreach (Unit target in context.targets)
            {
                if (target == null || !target.IsAlive || target.Team == owner.Team) continue;

                int next = Mathf.Min(CurrentStacks(combat, target) + 1, hemorrhage.MaxStacks);
                combat.ApplyStatus(owner, target, hemorrhage, next, durationTurns);
            }
        }

        private int CurrentStacks(CombatController combat, Unit target)
        {
            foreach (StatusInstance instance in combat.GetStatuses(target))
                if (instance.definition == hemorrhage)
                    return Mathf.RoundToInt(instance.magnitude);

            return 0;
        }
    }
}