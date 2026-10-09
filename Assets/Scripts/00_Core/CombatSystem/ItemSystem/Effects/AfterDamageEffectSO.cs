using System;
using System.Collections.Generic;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Base for item effects that react to CombatController.OnAfterDamage. It owns the
    /// per-Unit handler bookkeeping (the asset is shared, so state is keyed by Unit) and only
    /// forwards the events in which the owner is the source or the target of the damage.
    /// </summary>
    /// <remarks>
    /// Subclasses only implement <see cref="OnOwnerDamage"/> and decide for themselves which role
    /// (source / target) they care about. Attach/Detach are already handled here.
    /// </remarks>
    public abstract class AfterDamageEffectSO : TriggeredEffectSO
    {
        private readonly Dictionary<Unit, Action<DamageContext>> handlers = new();

        /// <summary>Called after damage in which <paramref name="owner"/> is source or target.</summary>
        protected abstract void OnOwnerDamage(Unit owner, DamageContext context);

        public override void Attach(Unit unit)
        {
            if (unit == null || unit.Combat == null) return;

            // Never leave a stale handler behind if Attach is called twice for the same Unit.
            Detach(unit);

            void Handler(DamageContext context)
            {
                if (context.source != unit && context.target != unit) return;
                OnOwnerDamage(unit, context);
            }

            handlers[unit] = Handler;
            unit.Combat.OnAfterDamage += Handler;
        }

        public override void Detach(Unit unit)
        {
            if (unit == null || !handlers.TryGetValue(unit, out Action<DamageContext> handler)) return;

            if (unit.Combat != null)
                unit.Combat.OnAfterDamage -= handler;

            handlers.Remove(unit);
        }
    }
}