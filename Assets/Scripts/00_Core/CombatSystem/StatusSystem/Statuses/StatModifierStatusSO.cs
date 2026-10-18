using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>Raises or lowers one stat. Magnitude is a positive fraction of the current value
    /// (0.8 = 80%); isDebuff decides whether it adds or subtracts.</summary>
    [CreateAssetMenu(fileName = "NewStatModifierStatus", menuName = "Status System/Stat Modifier")]
    public class StatModifierStatusSO : StatusSO
    {
        public STAT_TYPE stat;

        public override void CollectStatModifiers(StatusInstance instance, List<TemporaryStatModifier> into)
        {
            float signed = isDebuff ? -instance.magnitude : instance.magnitude;
            into.Add(new TemporaryStatModifier { stat = stat, fraction = signed });
        }
    }
}