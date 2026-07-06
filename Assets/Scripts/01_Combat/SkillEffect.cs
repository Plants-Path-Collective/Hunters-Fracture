using Core;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Describes a secondary effect that a skill or attack applies after
    /// the main damage/heal calculation is resolved by ActionResolver.
    ///
    /// IMPORTANT — what goes here and what doesn't:
    ///
    ///   YES (secondary effects, post-calculation):
    ///     - Status conditions  (Burn, Poison, Paralysis…)
    ///     - Stat buffs/debuffs (raise/lower a stat by a flat or % amount)
    ///     - Revive             (restore a dead ally at X% HP)
    ///     - RemoveStatus       (cleanse a condition)
    ///     - Shield             (grant HP buffer as % of caster's MagicPower)
    ///
    ///   NO (handled automatically by ActionResolver from SkillSO/AttackSO):
    ///     - Raw damage  → set basePower + damageType on the skill/attack asset
    ///     - Raw healing → use EFFECT_TYPE.Heal; ActionResolver scales it by
    ///                     (basePower + caster.MagicPower)
    ///
    /// modifier field meaning per effectType:
    ///   Buff / Debuff      → flat stat delta  (e.g. +25 PhysicalDef, -20 Accuracy)
    ///   StatusCondition    → unused (leave at 0)
    ///   Revive             → HP percent to restore  (0.0–1.0, e.g. 0.4 = 40% HP)
    ///   RemoveStatus       → unused (leave at 0)
    ///   Shield             → HP buffer as % of (basePower + caster.MagicPower)
    ///   StealHP            → drain ratio  (0.0–1.0, e.g. 0.5 = heal 50% of damage dealt)
    ///   Heal               → heal multiplier over base formula (usually 1.0)
    /// </summary>
    [System.Serializable]
    public class SkillEffect
    {
        [Tooltip("What this effect does.")]
        public EFFECT_TYPE effectType;

        [Tooltip("Which stat is raised or lowered. Only relevant for Buff and Debuff.")]
        public STAT_TYPE statAffected;

        [Tooltip("Which status condition to apply. Only relevant for StatusCondition.")]
        public STATUS_CONDITION condition;

        [Tooltip("See class summary for per-effectType meaning. " +
                 "For Buff/Debuff: flat delta. For Revive: HP%. For StealHP: drain ratio.")]
        public float modifier;

        [Tooltip("How many ATB seconds the effect lasts. 0 = instant / permanent until battle ends.")]
        public float durationInSeconds;

        [Tooltip("Probability that this effect triggers (0 = never, 1 = always).")]
        [Range(0f, 1f)]
        public float chance = 1f;
    }
}