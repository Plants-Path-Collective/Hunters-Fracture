using Core;
using CombatSystem;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    /// <summary>
    /// Defines a unit's basic attack.
    /// Each CharacterSheetSO holds one AttackSO reference.
    ///
    /// Damage formula (resolved by ActionResolver):
    ///   damage = (basePower + caster.Strength OR caster.MagicPower) * (1 - target.Defense%)
    ///
    /// The damageType field decides which offensive stat is used:
    ///   Physical → caster.Strength
    ///   Magical  → caster.MagicPower
    ///
    /// Secondary effects (poison on hit, debuff on hit, etc.) are optional
    /// and work exactly like SkillEffect entries on a SkillSO.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttack", menuName = "ScriptableObjects/Attack", order = 1)]
    public class AttackSO : ScriptableObject
    {
        [Header("Basic Information")]
        public string attackName = "Basic Attack";
        [TextArea] public string description;
        public Sprite icon;
        public AnimationClip animationClip;

        [Header("Damage")]
        [Tooltip("Physical uses Strength. Magical uses MagicPower.")]
        public DAMAGE_TYPE damageType;

        [Tooltip("Added to the caster's offensive stat before applying defense.\n" +
                 "Example: basePower 20 + Strength 80 = 100 before defense reduction.")]
        public float basePower = 10f;

        [Tooltip("Hit chance as a percentage. Subtracted from this is the target's Evasion.")]
        [Range(0f, 100f)]
        public float accuracy = 90f;

        [Header("Secondary Effects (optional)")]
        [Tooltip("Additional effects applied after the hit lands (status, debuff, etc.).\n" +
                 "Each effect has its own chance. Leave empty for a plain attack.")]
        public SkillEffect[] onHitEffects;
    }
}
