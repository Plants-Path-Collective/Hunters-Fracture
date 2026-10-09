using UnityEngine;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 003 (Birthday Hat): on landing a direct hit, steals a percentage of the target's
    /// Magic Power for the rest of the combat. The target gets a MagicPower debuff of that
    /// fraction; the owner gets a MagicPower buff worth the same absolute amount.
    /// </summary>
    /// <remarks>
    /// Needs two StatModifierStatusSO assets (MagicPower buff and MagicPower debuff): isDebuff
    /// decides the sign, so one asset cannot do both. Statuses keep the strongest magnitude, so
    /// repeated hits do not stack.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewStealMagicPower", menuName = "Item System/Effects/Types/Steal Magic Power")]
    public class StealMagicPowerEffectSO : AfterDamageEffectSO
    {
        [Header("----- Steal -----")]
        [Tooltip("Fraction stolen with a single copy. 0.10 = 10%.")]
        [SerializeField, Range(0f, 1f)] private float stealPercent = 0.10f;
        [Tooltip("Extra fraction per additional copy. 0.05 = +5%.")]
        [SerializeField, Range(0f, 1f)] private float perStackPercent = 0.05f;

        [Header("----- Statuses -----")]
        [Tooltip("StatModifierStatusSO: stat = MagicPower, isDebuff disabled. Applied to the owner.")]
        [SerializeField] private StatusSO buffStatus;
        [Tooltip("StatModifierStatusSO: stat = MagicPower, isDebuff enabled. Applied to the target.")]
        [SerializeField] private StatusSO debuffStatus;
        [Tooltip("Turns of the bearer. A huge value means 'rest of the combat'.")]
        [SerializeField, Min(1)] private int durationTurns = 999;

        protected override void OnOwnerDamage(Unit owner, DamageContext context)
        {
            if (buffStatus == null || debuffStatus == null || context.source != owner) return;
            if (!IsDirectHit(context)) return;

            Unit target = context.target;
            if (!owner.IsAlive || !target.IsAlive) return;

            float fraction = stealPercent + perStackPercent * (GetStacks(owner) - 1);

            // Read the target's Magic Power BEFORE the debuff lowers it.
            float stolen = target.MagicPower * fraction;
            if (stolen <= 0f) return;

            owner.Combat.ApplyStatus(owner, target, debuffStatus, fraction, durationTurns);

            // A percentage buff on a 0 Magic Power owner is worth nothing.
            if (owner.MagicPower > 0f)
                owner.Combat.ApplyStatus(owner, owner, buffStatus, stolen / owner.MagicPower, durationTurns);
        }
    }
}