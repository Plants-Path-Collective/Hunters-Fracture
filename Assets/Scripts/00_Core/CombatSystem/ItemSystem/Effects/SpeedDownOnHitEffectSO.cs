using UnityEngine;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 007 (Bag with Fish): on landing a direct hit, rolls a chance to apply a temporary
    /// Speed debuff to the target. The debuff is a StatModifierStatusSO (stat = Speed, isDebuff on).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpeedDownOnHitEffect", menuName = "Item System/Effects/Types/Speed Down On Hit")]
    public class SpeedDownOnHitEffectSO : AfterDamageEffectSO
    {
        [Header("----- Trigger -----")]
        [Range(0f, 1f)] public float chance = 0.1f;

        [Header("----- Debuff -----")]
        [Tooltip("StatModifierStatusSO with stat = Speed and isDebuff enabled.")]
        public StatusSO speedDownStatus;
        [Tooltip("Reduction with a single copy. 0.05 = -5%.")]
        [Range(0f, 1f)] public float speedReductionPercent = 0.05f;
        [Tooltip("Extra reduction per additional copy. 0.015 = -1.5%.")]
        [Range(0f, 1f)] public float perStackReductionPercent = 0.015f;
        [Tooltip("Turns of the target.")]
        public int durationTurns = 3;

        protected override void OnOwnerDamage(Unit owner, DamageContext context)
        {
            if (speedDownStatus == null || context.source != owner) return;
            if (!IsDirectHit(context) || !context.target.IsAlive) return;
            if (Random.value >= chance) return;

            float reduction = speedReductionPercent + perStackReductionPercent * (GetStacks(owner) - 1);
            owner.Combat.ApplyStatus(owner, context.target, speedDownStatus, reduction, durationTurns);
        }
    }
}