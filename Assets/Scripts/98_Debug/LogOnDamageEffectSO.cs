using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace ExtendedDebug
{
    /// <summary>Test-only effect: logs every OnAfterDamage where the owner is source or target.</summary>
    [CreateAssetMenu(fileName = "LogOnDamageEffect", menuName = "Item System/Effects/Debug/Log On Damage")]
    public class LogOnDamageEffectSO : TriggeredEffectSO
    {
        // One delegate per owner: the asset is shared, so this is keyed by Unit
        // and must be fully cleared in Detach().
        private readonly Dictionary<Unit, Action<DamageContext>> handlers = new();

        public override void Attach(Unit unit)
        {
            if (unit.Combat == null) return;

            Action<DamageContext> handler = ctx =>
            {
                if (ctx.source == unit || ctx.target == unit)
                    Debug.Log($"[LogOnDamage] {unit.Name} involved: {ctx.source?.Name} -> {ctx.target.Name} ({ctx.amount})");
            };

            handlers[unit] = handler;
            unit.Combat.OnAfterDamage += handler;
        }

        public override void Detach(Unit unit)
        {
            if (!handlers.TryGetValue(unit, out var handler)) return;

            if (unit.Combat != null)
                unit.Combat.OnAfterDamage -= handler;

            handlers.Remove(unit);
        }
    }
}