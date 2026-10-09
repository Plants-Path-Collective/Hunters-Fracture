using UnityEngine;
using Core;
using Core.CombatSystem;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>Deals magnitude damage to the bearer at the end of each of its turns (burn, poison).
    /// Not reduced by defense: the amount is decided when the status is applied.</summary>
    [CreateAssetMenu(fileName = "NewDamageOverTimeStatus", menuName = "Status System/Damage Over Time")]
    public class DamageOverTimeStatusSO : StatusSO
    {
        public UNITY_TYPE damageType = UNITY_TYPE.Magical;

        public override void OnTurnEnd(StatusInstance instance, CombatController combat)
        {
            int amount = Mathf.Max(1, Mathf.RoundToInt(instance.magnitude));
            combat.DealDamage(instance.source, instance.target, amount, damageType, statusName);
        }
    }
}