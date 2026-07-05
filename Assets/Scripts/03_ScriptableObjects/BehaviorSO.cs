using Core;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    /// <summary>
    /// Defines how an enemy unit chooses its actions in combat.
    /// TurnHandler reads this to pick an action automatically.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBehavior", menuName = "ScriptableObjects/EnemyBehavior", order = 3)]
    public class BehaviorSO : ScriptableObject
    {
        [Header("Action Weights")]
        [Tooltip("Relative probability of choosing a basic attack. Higher = more likely.")]
        [Min(0)] public float attackWeight  = 5f;

        [Tooltip("Relative probability of using a skill.")]
        [Min(0)] public float skillWeight   = 3f;

        [Tooltip("Relative probability of using the ultimate skill (if available).")]
        [Min(0)] public float ultimateWeight = 1f;

        [Header("Target Priority")]
        public TARGET_PRIORITY targetPriority = TARGET_PRIORITY.LowestHP;

        [Header("Skill HP Threshold")]
        [Tooltip("Enemy will prefer skills over basic attacks when its own HP % drops below this value. 0 = always prefer by weight only.")]
        [Range(0f, 1f)]
        public float useSkillBelowHPPercent = 0.4f;
    }
}