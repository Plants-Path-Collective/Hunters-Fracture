using UnityEngine;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item effect whose bonus to one stat depends on the value of ANOTHER stat, e.g. item 002
    /// (+15 HP per 10 Strength). Resolved by UnitStatsController in a second pass, after every
    /// StatModifierEffectSO, so the source stat already includes the other items.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatScalingEffect", menuName = "Item System/Effects/Stat Scaling")]
    public class StatScalingEffectSO : ItemEffectSO
    {
        [Header("----- Target (receives the bonus) -----")]
        public STAT_TYPE targetStat = STAT_TYPE.HP;

        [Header("----- Source (read to scale) -----")]
        public STAT_TYPE sourceStat = STAT_TYPE.Strength;
        [Tooltip("Size of one step of the source stat. 10 = one bonus per 10 points.")]
        [Min(1f)] public float step = 10f;

        [Header("----- Bonus per step -----")]
        [Tooltip("Flat bonus per step with a single copy.")]
        public float flatPerStep = 15f;
        [Tooltip("Extra flat bonus per step for each additional copy.")]
        public float perStackFlatPerStep = 5f;

        /// <summary>Flat bonus to add to the target stat. Whole steps only (floor).</summary>
        public float Evaluate(float sourceValue, int stackCount)
        {
            if (stackCount <= 0) return 0f;

            float steps = Mathf.Floor(sourceValue / Mathf.Max(1f, step));
            float perStep = flatPerStep + perStackFlatPerStep * (stackCount - 1);
            return steps * perStep;
        }
    }
}