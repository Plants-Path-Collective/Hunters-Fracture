using CombatSystem.Unit;

namespace CombatSystem.ItemSystem
{
    /// <summary>
    /// Abstract item effect that reacts to combat events (Battle.OnBeforeDamage,
    /// Battle.OnDamageDealt, Battle.OnTurnEnd, etc.) instead of contributing a
    /// static stat modifier — e.g. burn, sleep, on-hit chance effects.
    /// </summary>
    /// <remarks>
    /// <para>This is itself still abstract: it declares the Attach()/Detach() shape,
    /// but each concrete item (SpeedDownOnHitEffectSO, DariusPassiveEffectSO, etc.)
    /// implements them with its own logic — that's what lets UnitEffectController
    /// call Attach()/Detach() on any of them without a switch statement.</para>
    /// <para>IMPORTANT: because this is a ScriptableObject, the same asset instance
    /// is shared by every Unit that owns this item. Never store per-Unit runtime
    /// state (like "which unit owns me right now") in fields on a concrete
    /// subclass — capture the Unit in a closure inside Attach(), and keep track
    /// of that exact delegate so Detach() can remove it again.</para>
    /// </remarks>
    public abstract class TriggeredEffectSO : ItemEffectSO
    {
        /// <summary>
        /// Called by UnitEffectController when the owning Unit gains the first
        /// stack of the item this effect belongs to. Subscribe to whichever
        /// Battle event this effect reacts to here.
        /// </summary>
        public abstract void Attach(CombatSystem.Unit.Unit unit);

        /// <summary>
        /// Called by UnitEffectController when the owning Unit loses the last
        /// stack of the item this effect belongs to. Must unsubscribe the exact
        /// same delegate that Attach() registered.
        /// </summary>
        public abstract void Detach(CombatSystem.Unit.Unit unit);
    }
}