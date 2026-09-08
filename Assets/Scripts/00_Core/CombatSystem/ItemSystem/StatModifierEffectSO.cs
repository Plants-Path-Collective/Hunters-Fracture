using UnityEngine;
using System.Collections.Generic;
using Core;

namespace CombatSystem.ItemSystem
{
    /// <summary>
    /// Concrete item effect that contributes a static (or formula-based) modifier
    /// to one stat — e.g. Glasscanon (+% Strength, +% both Defenses) or HP Booster
    /// (+HP per 10 Strength).
    /// </summary>
    /// <remarks>
    /// <para>Unlike <see cref="TriggeredEffectSO"/>, this effect never subscribes to
    /// anything on <c>Battle</c> — it's a pure function of "how much do I add, given
    /// the current base value and how many copies of this item the Unit holds".</para>
    /// <para><see cref="UnitStatsController.RecalculateStats"/> is expected to call
    /// <see cref="ApplyModifiers"/> once per stack, for every item the Unit currently
    /// holds whose effect is of this type, as part of its full recompute.</para>
    /// </remarks>
    [CreateAssetMenu(fileName = "NewStatModifierEffect", menuName = "Item System/Effects/Stat Modifier")]
    public class StatModifierEffectSO : ItemEffectSO
    {
        [Header("----- Target -----")]
        public STAT_TYPE statType;

        [Header("----- First Stack Contribution -----")]
        public float flatBonus;
        [Tooltip("0.1 = +10% of the current value")]
        public float percentBonus;

        [Header("----- Additional Stack Contribution -----")]
        public float perStackFlatBonus;
        [Tooltip("0.1 = +10% of the current value")]
        public float perStackPercentBonus;

        /// <summary>
        /// Given the current value of the targeted stat and how many stacks of
        /// this item the Unit holds, returns the new value after applying this
        /// effect's configured contributions.
        /// </summary>
        public float ApplyModifiers(float currentValue, int stackCount)
        {
            if (!Mathf.Approximately(percentBonus, 0f) || !Mathf.Approximately(perStackPercentBonus, 0f))
            {
                currentValue = ApplyPercentModifiers(currentValue, stackCount);
            }

            if (!Mathf.Approximately(flatBonus, 0f) || !Mathf.Approximately(perStackFlatBonus, 0f))
            {
                currentValue = ApplyFlatModifiers(currentValue, stackCount);
            }

            return currentValue;
        }

        private float ApplyPercentModifiers(float currentValue, int stackCount)
        {
            for (int stack = 0; stack < stackCount; stack++)
            {
                float percentBonusForStack = stack == 0 ? percentBonus : perStackPercentBonus;
                currentValue += currentValue * percentBonusForStack;
            }

            return currentValue;
        }

        private float ApplyFlatModifiers(float currentValue, int stackCount)
        {
            for (int stack = 0; stack < stackCount; stack++)
            {
                float flatBonusForStack = stack == 0 ? flatBonus : perStackFlatBonus;
                currentValue += flatBonusForStack;
            }

            return currentValue;
        }
    }
}