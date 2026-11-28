using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>
    /// Hemorrhage (item 011). Magnitude is the number of stacks: the tracker keeps ONE instance per
    /// status and unit and the strongest magnitude, so the applier sends (currentStacks + 1) each hit.
    /// Per stack: damage at the end of the bearer's turn, a Speed fraction and a flat Physical Defense loss.
    /// </summary>
    /// <remarks>
    /// Set isDebuff = true on the asset. The defense loss is FLAT (percentage points, floored at 0 by
    /// UnitStatsController), not a fraction of the current value.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewHemorrhageStatus", menuName = "Status System/Hemorrhage")]
    public class HemorrhageStatusSO : StatusSO
    {
        [Header("--- Per stack ---")]
        [SerializeField, Min(0)] private int damagePerStack = 10;
        [Tooltip("Speed lost per stack. 0.05 = -5%.")]
        [SerializeField, Range(0f, 1f)] private float speedLossPerStack = 0.05f;
        [Tooltip("Physical Defense lost per hit, in the stat's own units. 0.10 = 10 percentage points.")]
        [SerializeField, Min(0f)] private float physicalDefenseLossPerStack = 0.10f;

        [Header("--- Cap ---")]
        [Tooltip("Speed loss reaches 100% at 20 stacks with 5% per stack.")]
        [SerializeField, Min(1)] private int maxStacks = 20;

        public int MaxStacks => maxStacks;

        private int Stacks(StatusInstance instance) => Mathf.Clamp(Mathf.RoundToInt(instance.magnitude), 0, maxStacks);

        public override void OnTurnEnd(StatusInstance instance, CombatController combat)
        {
            int stacks = Stacks(instance);
            if (stacks <= 0 || damagePerStack <= 0) return;

            // Source is the item owner, not the acting unit, so IsDirectHit() ignores this damage.
            combat.DealDamage(instance.source, instance.target, damagePerStack * stacks, UNITY_TYPE.Physical, "hemorrhage");
        }

        public override void CollectStatModifiers(StatusInstance instance, List<TemporaryStatModifier> into)
        {
            int stacks = Stacks(instance);
            if (stacks <= 0) return;

            into.Add(new TemporaryStatModifier
            {
                stat = STAT_TYPE.Speed,
                fraction = -Mathf.Min(1f, speedLossPerStack * stacks)
            });

            into.Add(new TemporaryStatModifier
            {
                stat = STAT_TYPE.PhysicalDefense,
                flat = -physicalDefenseLossPerStack * stacks // NEW field, see patch notes
            });
        }

        public override int GetDisplayCount(StatusInstance instance) =>
            Mathf.Min(maxStacks, Mathf.RoundToInt(instance.magnitude));
    }
}