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
}