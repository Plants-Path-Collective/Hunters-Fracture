using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>Mutable payload for OnBeforeAction / OnAfterAction.</summary>
    public class ActionContext
    {
        public Unit actor;
        public ActionSO action;

        /// <summary>Units the action's TargetType resolved to (empty for TargetType.None).</summary>
        public List<Unit> targets = new();

        /// <summary>Multiplies the magnitude and duration of every effect (resonance sets 2).</summary>
        public float effectScale = 1f;

        public int spCost;
        public bool cancelled;
    }

    /// <summary>value = baseValue + perPoint × the caster's stat. perPoint 0 makes it a constant.</summary>
    [Serializable]
    public struct ScaledValue
    {
        public float baseValue;
        public STAT_TYPE stat;
        [Tooltip("Added per point of the caster's stat (negative allowed). HP means max HP.")]
        public float perPoint;

        public float Evaluate(Unit caster) => baseValue + perPoint * caster.GetStat(stat);
    }
}