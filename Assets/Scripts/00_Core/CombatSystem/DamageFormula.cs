using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem
{
    /// <summary>Shared damage math: Damage Received = Base Damage / (1 + Defense), floored at 1.</summary>
    public static class DamageFormula
    {
        public static int Reduce(float baseDamage, Unit target, UNITY_TYPE damageType)
        {
            float defense = damageType == UNITY_TYPE.Magical
                ? target.EffectiveMagicalDefense
                : target.EffectivePhysicalDefense;

            return Mathf.Max(1, Mathf.RoundToInt(baseDamage / (1f + Mathf.Max(0f, defense))));
        }
    }
}