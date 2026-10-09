using UnityEngine;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 001 (Magical Defense Gambling): when the owner is hit by an enemy, rolls a chance to
    /// buff its own Magical Defense. The buff grows with the missing HP (min at full HP, max at
    /// ~0 HP) and with each extra copy. The buff is a StatModifierStatusSO (MagicalDefense, buff).
    /// </summary>
    /// <remarks>
    /// Statuses keep only the strongest magnitude when re-applied, so several procs do not add up.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewMagicalDefenseGambling", menuName = "Item System/Effects/Types/Magical Defense Gambling")]
    public class MagicalDefenseGamblingEffectSO : AfterDamageEffectSO
    {
        [Header("----- Trigger -----")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.05f;

        [Header("----- Buff -----")]
        [Tooltip("StatModifierStatusSO with stat = MagicalDefense and isDebuff disabled.")]
        [SerializeField] private StatusSO buffStatus;
        [Tooltip("Magnitude at full HP. 0.05 = +5%.")]
        [SerializeField, Min(0f)] private float minMagnitude = 0.05f;
        [Tooltip("Magnitude at 0 HP. 0.10 = +10%.")]
        [SerializeField, Min(0f)] private float maxMagnitude = 0.10f;
        [Tooltip("Extra magnitude per additional copy.")]
        [SerializeField, Min(0f)] private float perStackBonus = 0.05f;
        [Tooltip("Turns of the owner. A huge value means 'rest of the combat'.")]
        [SerializeField, Min(1)] private int durationTurns = 999;

        protected override void OnOwnerDamage(Unit owner, DamageContext context)
        {
            if (buffStatus == null || context.target != owner || !owner.IsAlive) return;
            if (!IsDirectHit(context)) return;
            if (Random.value >= chance) return;

            float missingHp = 1f - owner.HP / (float)Mathf.Max(1, owner.MaxHP);
            float magnitude = Mathf.Lerp(minMagnitude, maxMagnitude, missingHp)
                              + perStackBonus * (GetStacks(owner) - 1);

            owner.Combat.ApplyStatus(owner, owner, buffStatus, magnitude, durationTurns);
        }
    }
}