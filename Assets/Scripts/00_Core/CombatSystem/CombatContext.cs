using Core;
using Core.CombatSystem.Units;

namespace Core.CombatSystem
{
    /// <summary>
    /// Mutable payload for OnBeforeDamage/OnAfterDamage. Subscribers can adjust Amount in
    /// OnBeforeDamage before CombatController.DealDamage actually applies it.
    /// </summary>
    public class DamageContext
    {
        public Unit source;
        public Unit target;
        public int amount;
        public UNITY_TYPE damageType;
        public string via;
    }

    /// <summary>Mutable payload for OnBeforeHeal/OnAfterHeal, same idea as DamageContext.</summary>
    public class HealContext
    {
        public Unit source;
        public Unit target;
        public int amount;
        public string via;
    }

    /// <summary>Notification-only payload for OnFleeAttempt.</summary>
    public class FleeContext
    {
        public UNIT_TEAM team;
        public bool success;
    }
}