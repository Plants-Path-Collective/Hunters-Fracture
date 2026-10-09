using UnityEngine;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>Scales the damage the bearer deals by (1 ± magnitude). 0.15 = +15%.</summary>
    [CreateAssetMenu(fileName = "NewDamageDealtStatus", menuName = "Status System/Damage Dealt")]
    public class DamageDealtStatusSO : StatusSO
    {
        public override float OutgoingDamageMultiplier(StatusInstance instance) =>
            Mathf.Max(0f, 1f + (isDebuff ? -instance.magnitude : instance.magnitude));
    }
}