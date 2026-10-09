using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 005 (Moonstone Necklace): when damage leaves the owner under an HP threshold, it
    /// heals a percentage of its max HP and pays a percentage of its CURRENT SP. Needs SP to
    /// trigger. Lethal damage cannot be saved: the unit is already dead when this runs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEmergencySpShield", menuName = "Item System/Effects/Types/Emergency SP Shield")]
    public class EmergencySpShieldEffectSO : AfterDamageEffectSO
    {
        [Header("----- Trigger -----")]
        [Tooltip("Triggers while HP / MaxHP is below this. 0.30 = 30%.")]
        [SerializeField, Range(0f, 1f)] private float hpThreshold = 0.30f;

        [Header("----- Heal (percent of max HP) -----")]
        [SerializeField, Range(0f, 1f)] private float healPercent = 0.20f;
        [SerializeField, Range(0f, 1f)] private float perStackHealPercent = 0.10f;

        [Header("----- Cost (percent of current SP) -----")]
        [SerializeField, Range(0f, 1f)] private float spCostPercent = 0.50f;
        [SerializeField, Range(0f, 1f)] private float perStackSpCostPercent = 0.10f;

        protected override void OnOwnerDamage(Unit owner, DamageContext context)
        {
            if (context.target != owner || !owner.IsAlive || owner.MaxHP <= 0) return;
            if (owner.HP / (float)owner.MaxHP >= hpThreshold) return;
            if (owner.SP <= 0) return;

            int extraStacks = GetStacks(owner) - 1;
            float healFraction = Mathf.Clamp01(healPercent + perStackHealPercent * extraStacks);
            float costFraction = Mathf.Clamp01(spCostPercent + perStackSpCostPercent * extraStacks);

            int spCost = Mathf.Max(1, Mathf.RoundToInt(owner.SP * costFraction));
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(owner.MaxHP * healFraction));

            if (!owner.Combat.SpendSP(owner, spCost)) return;
            owner.Combat.Heal(owner, owner, healAmount, "EmergencySpShield");
        }
    }
}