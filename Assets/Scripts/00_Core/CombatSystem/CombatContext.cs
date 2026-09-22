using Core;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace Core.CombatSystem
{
    /// <summary>
    /// Mutable payload for OnBeforeDamage/OnAfterDamage. Subscribers can adjust Amount in
    /// OnBeforeDamage before CombatController.DealDamage actually applies it.
    /// </summary>
    public class DamageContext
    {
        public CombatUnit Source;
        public CombatUnit Target;
        public int Amount;
        public UNITY_TYPE DamageType;
        public string Via;
    }

    /// <summary>Mutable payload for OnBeforeHeal/OnAfterHeal, same idea as DamageContext.</summary>
    public class HealContext
    {
        public CombatUnit Source;
        public CombatUnit Target;
        public int Amount;
        public string Via;
    }

    /// <summary>Notification-only payload for OnFleeAttempt.</summary>
    public class FleeContext
    {
        public UNIT_TEAM Team;
        public bool Success;
    }
}