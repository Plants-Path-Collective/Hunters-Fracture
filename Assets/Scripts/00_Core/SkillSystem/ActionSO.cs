using System.Collections.Generic;
using UnityEngine;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>A technique: what it costs, who it targets and what it does.</summary>
    [CreateAssetMenu(fileName = "NewAction", menuName = "Skill System/Action")]
    public class ActionSO : ScriptableObject
    {
        [Header("--- Identity ---")]
        public string actionName;
        public Sprite icon;
        [TextArea(2, 4)] public string description;

        [Header("--- Rules ---")]
        [Min(0)] public int spCost;
        public TARGET_TYPE targetType = TARGET_TYPE.SingleEnemy;

        [Header("--- Effects (run in order) ---")]
        public List<ActionEffectSO> effects = new();
    }
}