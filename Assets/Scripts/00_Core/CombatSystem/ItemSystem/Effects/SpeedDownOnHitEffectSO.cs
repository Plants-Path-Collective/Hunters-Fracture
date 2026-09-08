using System.Collections.Generic;
using UnityEngine;
using CombatSystem.Unit;

namespace CombatSystem.ItemSystem
{
    /// <summary>
    /// Concrete TriggeredEffectSO — equivalent to item 007 (On-Hit SPEED reduction):
    /// on landing a hit, has a chance to apply a temporary Speed debuff to the target.
    /// </summary>
    /// <remarks>
    /// This is the class that actually implements Attach()/Detach(), since
    /// TriggeredEffectSO only declares their shape. Notice the per-Unit
    /// dictionary of handlers: since this asset is shared across every Unit
    /// that owns this item, we can't store "which unit owns me" in a field —
    /// each Attach() call captures its own Unit in a closure instead.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewSpeedDownOnHitEffect", menuName = "Item System/Effects/Types/Speed Down On Hit")]
    public class SpeedDownOnHitEffectSO : TriggeredEffectSO
    {
        [Range(0f, 1f)] public float chance = 0.2f;
        [Range(0f, 1f)] public float speedReductionPercent = 0.15f;
        public int durationTurns = 3;

        private readonly Dictionary<CombatSystem.Unit.Unit, System.Action<DamageDealtInfo>> _handlers = new();

        public override void Attach(CombatSystem.Unit.Unit unit)
        {
            void Handler(DamageDealtInfo info)
            {
                if (info.Source != unit) return;
                if (Random.value > chance) return;

                // Placeholder call — replace with the real StatusBuffTracker API
                // once it exists.
                info.Target.effectController.ApplyBuff(
                    "speed_down",
                    -speedReductionPercent,
                    durationTurns);
            }

            _handlers[unit] = Handler;
            //unit.Battle.OnDamageDealt += Handler;
        }

        public override void Detach(CombatSystem.Unit.Unit unit)
        {
            if (_handlers.TryGetValue(unit, out var handler))
            {
                //unit.Battle.OnDamageDealt -= handler;
                _handlers.Remove(unit);
            }
        }
    }

    /// <summary>
    /// Placeholder — replace with SimpleJRPG's actual event payload type for
    /// Battle.OnDamageDealt.
    /// </summary>
    public struct DamageDealtInfo
    {
        public CombatSystem.Unit.Unit Source;
        public CombatSystem.Unit.Unit Target;
        public float Amount;
    }
}
