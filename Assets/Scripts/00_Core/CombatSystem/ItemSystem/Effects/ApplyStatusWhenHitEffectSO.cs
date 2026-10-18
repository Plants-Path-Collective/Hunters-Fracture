using UnityEngine;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// When the owner is hit by an enemy, rolls a chance to apply a status to the owner itself.
    /// Item 006 (Heavy Stunt Armor) uses it with a skip-turn status ("Stun").
    /// </summary>
    [CreateAssetMenu(fileName = "NewApplyStatusWhenHit", menuName = "Item System/Effects/Types/Apply Status When Hit")]
    public class ApplyStatusWhenHitEffectSO : AfterDamageEffectSO
    {
        [Header("----- Trigger -----")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.1f;

        [Header("----- Status applied to the owner -----")]
        [SerializeField] private StatusSO status;
        [Tooltip("Meaning depends on the status. Ignored by skip-turn statuses.")]
        [SerializeField] private float magnitude;
        [Tooltip("Turns of the owner (skipped turns for skip-turn statuses).")]
        [SerializeField, Min(1)] private int duration = 1;

        protected override void OnOwnerDamage(Unit owner, DamageContext context)
        {
            if (status == null || context.target != owner || !owner.IsAlive) return;
            if (!IsDirectHit(context)) return;
            if (Random.value >= chance) return;

            owner.Combat.ApplyStatus(context.source, owner, status, magnitude, duration);
        }
    }
}