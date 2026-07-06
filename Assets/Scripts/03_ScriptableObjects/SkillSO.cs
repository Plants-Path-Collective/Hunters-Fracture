using Core;
using CombatSystem;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    /// <summary>
    /// Defines an active skill (Basic or Ultimate).
    ///
    /// Damage / heal formula (resolved by ActionResolver):
    ///   Physical damage: (basePower + caster.Strength)   * (1 - target.PhysicalDef%)
    ///   Magical  damage: (basePower + caster.MagicPower) * (1 - target.MagicalDef%)
    ///   Heal:            (basePower + caster.MagicPower) * healMultiplier
    ///
    /// The damageType field drives which offensive stat is used. A physical-affinity
    /// character can have a magical skill — just set damageType to Magical on that skill.
    ///
    /// SkillEffect entries handle only SECONDARY outcomes (status, buff/debuff, revive).
    /// Do NOT use SkillEffect to express damage or healing; set basePower instead.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "ScriptableObjects/Skill", order = 2)]
    public class SkillSO : ScriptableObject
    {
        [Header("Basic Information")]
        public string skillName;
        [TextArea] public string description;
        public Sprite icon;
        public AnimationClip animationClip;
        [Tooltip("Animation length in seconds. Influences ATB delay after use.")]
        public float animationDuration;

        [Header("Classification")]
        public SKILL_TYPE   skillType;
        public SKILL_TARGET targetType;
        public bool isUltimate;
        public bool isPassive;

        [Header("Cost")]
        [Tooltip("SP consumed when the skill is used.")]
        public float spCost;
        [Tooltip("HP consumed when the skill is used (e.g. a sacrifice skill).")]
        public float hpCost;
        [Tooltip("Maximum uses per combat. -1 = unlimited.")]
        public int usageLimit = -1;

        [Header("Damage / Heal")]
        [Tooltip("Physical → uses caster Strength. Magical → uses caster MagicPower.\n" +
                 "A physical-affinity unit can have a magical skill — set this per skill.")]
        public DAMAGE_TYPE damageType;

        [Tooltip("Added to the caster's offensive stat to form the raw power value.\n" +
                 "Set to 0 for pure utility skills (buff/debuff only, no damage or heal).")]
        public float basePower;

        [Tooltip("Hit chance %. Target Evasion is subtracted from this.")]
        [Range(0f, 100f)]
        public float accuracy = 90f;

        [Header("Secondary Effects")]
        [Tooltip("Status conditions, buffs, debuffs, revive, etc.\n" +
                 "Each entry is resolved after the main damage/heal.\n" +
                 "Do NOT add a Damage effect here — use basePower above instead.")]
        public SkillEffect[] effects;

        [Header("ATB Modifiers")]
        [Tooltip("Multiplier applied to the unit's speed when this skill is queued.\n" +
                 "1 = normal speed. <1 = slower (heavy skill). >1 = faster (quick skill).")]
        public float speedModifier = 1f;
        [Tooltip("ATB seconds before this skill can be used again after casting.")]
        public float cooldown;
    }
}