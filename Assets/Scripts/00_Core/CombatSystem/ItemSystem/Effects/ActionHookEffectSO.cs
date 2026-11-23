using System;
using System.Collections.Generic;
using Core.CombatSystem.SkillSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Base for item effects that react to CombatController.OnBeforeAction / OnAfterAction. It owns
    /// the per-Unit handler bookkeeping (the asset is shared, so state is keyed by Unit) and only
    /// forwards the actions performed by the owner. Sibling of AfterDamageEffectSO.
    /// </summary>
    /// <remarks>
    /// A basic attack travels through the same pipeline as a skill; its ActionContext has
    /// action == null (see <see cref="IsBasicAttack"/>). Subclasses override only the hook they need.
    /// </remarks>
    public abstract class ActionHookEffectSO : TriggeredEffectSO
    {
        private sealed class Subscription
        {
            public Action<ActionContext> before;
            public Action<ActionContext> after;
        }

        private readonly Dictionary<Unit, Subscription> subscriptions = new();

        /// <summary>Owner is about to act. The context is mutable (targets, effectScale, cancelled).</summary>
        protected virtual void OnOwnerBeforeAction(Unit owner, ActionContext context) { }

        /// <summary>Owner finished resolving an action (the turn has not been closed yet).</summary>
        protected virtual void OnOwnerAfterAction(Unit owner, ActionContext context) { }

        /// <summary>Called from Detach, after the handlers were removed. Clear per-Unit state here.</summary>
        protected virtual void OnDetached(Unit unit) { }

        /// <summary>True for the basic attack (it has no ActionSO).</summary>
        protected static bool IsBasicAttack(ActionContext context) => context.action == null;

        public override void Attach(Unit unit)
        {
            if (unit == null || unit.Combat == null) return;

            // Never leave a stale handler behind if Attach is called twice for the same Unit.
            Detach(unit);

            var subscription = new Subscription
            {
                before = context =>
                {
                    if (context.actor == unit) OnOwnerBeforeAction(unit, context);
                },
                after = context =>
                {
                    if (context.actor == unit) OnOwnerAfterAction(unit, context);
                }
            };

            subscriptions[unit] = subscription;
            unit.Combat.OnBeforeAction += subscription.before;
            unit.Combat.OnAfterAction += subscription.after;
        }

        public override void Detach(Unit unit)
        {
            if (unit == null || !subscriptions.TryGetValue(unit, out Subscription subscription)) return;

            if (unit.Combat != null)
            {
                unit.Combat.OnBeforeAction -= subscription.before;
                unit.Combat.OnAfterAction -= subscription.after;
            }

            subscriptions.Remove(unit);
            OnDetached(unit);
        }
    }
}