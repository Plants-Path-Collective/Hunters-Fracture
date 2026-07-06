using System.Collections.Generic;
using Core;
using CombatSystem.UnitSystem;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Pure calculation layer — no Unity lifecycle, no UI, no ATB.
    ///
    /// Unified damage / heal formula:
    ///   offensiveStat  = damageType == Physical ? caster.Strength : caster.MagicPower
    ///   defensiveStat  = damageType == Physical ? target.PhysicalDef : target.MagicalDef
    ///   rawPower       = skill.basePower + offensiveStat
    ///   finalDamage    = rawPower * (1 - defensiveStat / 100)
    ///
    /// SkillEffect entries only handle secondary outcomes (status, buff/debuff,
    /// revive, shield, drain). They never contain raw damage values.
    /// </summary>
    public class ActionResolver : MonoBehaviour
    {
        private const float FleeSuccessChance = 0.5f;

        // ── Flee ──────────────────────────────────────────────────────────────

        public bool TryFlee() => Random.value < FleeSuccessChance;

        // ── Basic Attack ──────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a basic attack from an AttackSO.
        /// Uses the AttackSO's own damageType (matches unit affinity by default,
        /// but can differ for special attack variants).
        /// Returns damage dealt (0 if miss).
        /// </summary>
        public float ResolveBasicAttack(Unit caster, Unit target)
        {
            AttackSO attack = caster.Sheet.basicAttack;
            if (attack == null)
            {
                Debug.LogWarning($"[ActionResolver] {caster.UnitName} has no AttackSO assigned.");
                return 0f;
            }

            if (!RollHit(attack.accuracy, target.Evasion)) return 0f;

            float damage = CalculateDamage(attack.basePower, caster, attack.damageType, target);
            target.ModifyHP(-damage);

            // Apply any on-hit secondary effects
            if (attack.onHitEffects != null)
                foreach (SkillEffect effect in attack.onHitEffects)
                    ApplySecondaryEffect(effect, caster, target, attack.basePower, attack.damageType);

            return damage;
        }

        // ── Skill ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a skill. Pays cost, applies main damage/heal, then secondary effects.
        /// </summary>
        public void ResolveSkill(SkillSO skill, Unit caster, List<Unit> targets, int skillIndex)
        {
            // Pay costs first
            caster.ModifySP(-skill.spCost);
            if (skill.hpCost > 0f) caster.ModifyHP(-skill.hpCost);
            caster.ConsumeSkillUse(skillIndex);

            foreach (Unit target in targets)
            {
                bool targetIsDead = target.State == UNIT_STATE.Dead;

                // Only Revive effects can target dead units — skip main formula for them
                if (targetIsDead && !SkillCanRevive(skill)) continue;
                if (targetIsDead) { ApplyAllSecondaryEffects(skill, caster, target); continue; }

                // Main damage or heal based on basePower
                if (skill.basePower > 0f)
                {
                    bool isHeal = skill.targetType == SKILL_TARGET.SingleAlly
                               || skill.targetType == SKILL_TARGET.AllAllies
                               || skill.targetType == SKILL_TARGET.Self;

                    if (!RollHit(skill.accuracy, target.Evasion) && !isHeal) continue;

                    if (isHeal)
                    {
                        float heal = CalculateHeal(skill.basePower, caster);
                        target.ModifyHP(heal);
                    }
                    else
                    {
                        float damage = CalculateDamage(skill.basePower, caster, skill.damageType, target);
                        target.ModifyHP(-damage);
                    }
                }

                // Secondary effects (status, buff/debuff, drain, shield…)
                ApplyAllSecondaryEffects(skill, caster, target);
            }
        }

        // ── Disputa result ────────────────────────────────────────────────────

        /// <summary>
        /// Loser takes damage proportional to enemy Potencia (5% maxHP per point).
        /// Winner keeps their turn (handled by CombatManager).
        /// </summary>
        public void ResolveDisputaResult(Unit winner, Unit loser, int loserPotencia)
        {
            float damage = loser.MaxHP * (loserPotencia * 0.05f);
            loser.ModifyHP(-damage);
            loser.ResetATB();

            Debug.Log($"[Disputa] {winner.UnitName} wins. " +
                      $"{loser.UnitName} takes {damage:F1} and resets ATB.");
        }

        // ── Alianza result ────────────────────────────────────────────────────

        /// <summary>
        /// Combined attack scaled by Potencia earned during the minigame.
        /// Each Potencia point adds 10% to the combined raw power.
        /// </summary>
        public void ResolveAlianzaAttack(Unit unitA, Unit unitB,
                                          int potencia, List<Unit> targets)
        {
            float powerA   = GetOffensiveStat(unitA, unitA.Sheet.damageType) + unitA.Sheet.basicAttack.basePower;
            float powerB   = GetOffensiveStat(unitB, unitB.Sheet.damageType) + unitB.Sheet.basicAttack.basePower;
            float combined = (powerA + powerB) * (1f + potencia * 0.1f);

            foreach (Unit target in targets)
            {
                if (target.State == UNIT_STATE.Dead) continue;
                // Use unitA's damage type as the dominant type for the combined hit
                float defense = GetDefensiveStat(target, unitA.Sheet.damageType);
                float damage  = Mathf.Max(1f, combined * (1f - defense / 100f));
                target.ModifyHP(-damage);
            }

            Debug.Log($"[Alianza] {unitA.UnitName} + {unitB.UnitName} — " +
                      $"potencia {potencia}, combined power {combined:F1}.");
        }

        // ── Core formulas ─────────────────────────────────────────────────────

        /// <summary>
        /// damage = (basePower + offensiveStat) * (1 - defense%)
        /// Minimum 1 damage always lands on a hit.
        /// </summary>
        private static float CalculateDamage(float basePower, Unit caster,
                                              DAMAGE_TYPE dmgType, Unit target)
        {
            float offStat = GetOffensiveStat(caster, dmgType);
            float defense = GetDefensiveStat(target, dmgType);
            return Mathf.Max(1f, (basePower + offStat) * (1f - defense / 100f));
        }

        /// <summary>
        /// heal = (basePower + caster.MagicPower)
        /// Healing always uses MagicPower regardless of affinity.
        /// </summary>
        private static float CalculateHeal(float basePower, Unit caster)
        {
            return basePower + caster.MagicPower;
        }

        private static bool RollHit(float accuracy, float evasion)
        {
            float hitChance = Mathf.Clamp(accuracy - evasion, 0f, 100f);
            return Random.Range(0f, 100f) <= hitChance;
        }

        private static float GetOffensiveStat(Unit unit, DAMAGE_TYPE dmgType)
            => dmgType == DAMAGE_TYPE.Physical ? unit.Strength : unit.MagicPower;

        private static float GetDefensiveStat(Unit target, DAMAGE_TYPE dmgType)
            => dmgType == DAMAGE_TYPE.Physical ? target.PhysicalDef : target.MagicalDef;

        // ── Secondary effects ─────────────────────────────────────────────────

        private void ApplyAllSecondaryEffects(SkillSO skill, Unit caster, Unit target)
        {
            if (skill.effects == null) return;
            foreach (SkillEffect effect in skill.effects)
                ApplySecondaryEffect(effect, caster, target, skill.basePower, skill.damageType);
        }

        private static void ApplySecondaryEffect(SkillEffect effect, Unit caster, Unit target,
                                                   float basePower, DAMAGE_TYPE dmgType)
        {
            if (!RollChance(effect.chance)) return;

            switch (effect.effectType)
            {
                case EFFECT_TYPE.Heal:
                {
                    // modifier acts as a multiplier on top of the heal formula
                    float heal = (basePower + caster.MagicPower) * Mathf.Max(1f, effect.modifier);
                    target.ModifyHP(heal);
                    break;
                }

                case EFFECT_TYPE.Buff:
                    ApplyStatMod(target, effect.statAffected, +effect.modifier);
                    break;

                case EFFECT_TYPE.Debuff:
                    ApplyStatMod(target, effect.statAffected, -effect.modifier);
                    break;

                case EFFECT_TYPE.StatusCondition:
                    target.ApplyStatus(effect.condition);
                    break;

                case EFFECT_TYPE.RemoveStatus:
                    target.ClearStatus();
                    break;

                case EFFECT_TYPE.Shield:
                {
                    // Shield as HP buffer = modifier% of (basePower + caster.MagicPower)
                    float buffer = (basePower + caster.MagicPower) * Mathf.Clamp01(effect.modifier);
                    target.ModifyHP(buffer);
                    break;
                }

                case EFFECT_TYPE.StealHP:
                {
                    // modifier = drain ratio (e.g. 0.5 = heal caster 50% of damage dealt)
                    float damage = CalculateDamageStatic(basePower, caster, dmgType, target);
                    target.ModifyHP(-damage);
                    caster.ModifyHP(damage * Mathf.Clamp01(effect.modifier));
                    break;
                }

                case EFFECT_TYPE.Revive:
                    if (target.State == UNIT_STATE.Dead)
                        target.Revive(Mathf.Clamp01(effect.modifier));
                    break;

                case EFFECT_TYPE.Damage:
                    // Should not appear in SkillEffect — use basePower on the skill instead.
                    Debug.LogWarning("[ActionResolver] EFFECT_TYPE.Damage found in SkillEffect. " +
                                     "Use basePower on the SkillSO/AttackSO instead.");
                    break;
            }
        }

        // Static version for use inside static contexts (StealHP)
        private static float CalculateDamageStatic(float basePower, Unit caster,
                                                     DAMAGE_TYPE dmgType, Unit target)
        {
            float offStat = GetOffensiveStat(caster, dmgType);
            float defense = GetDefensiveStat(target, dmgType);
            return Mathf.Max(1f, (basePower + offStat) * (1f - defense / 100f));
        }

        private static bool RollChance(float chance)
            => Random.value <= Mathf.Clamp01(chance);

        private static void ApplyStatMod(Unit target, STAT_TYPE stat, float delta)
        {
            switch (stat)
            {
                case STAT_TYPE.Speed:           target.SpeedMod       += delta; break;
                case STAT_TYPE.Strength:        target.StrengthMod    += delta; break;
                case STAT_TYPE.MagicPower:      target.MagicPowerMod  += delta; break;
                case STAT_TYPE.Evasion:         target.EvasionMod     += delta; break;
                case STAT_TYPE.Accuracy:        target.AccuracyMod    += delta; break;
                case STAT_TYPE.PhysicalDefense: target.PhysicalDefMod += delta; break;
                case STAT_TYPE.MagicDefense:    target.MagicalDefMod  += delta; break;
                case STAT_TYPE.HP:              target.ModifyHP(delta);          break;
                case STAT_TYPE.SP:              target.ModifySP(delta);          break;
            }
        }

        private static bool SkillCanRevive(SkillSO skill)
        {
            if (skill.effects == null) return false;
            foreach (var e in skill.effects)
                if (e.effectType == EFFECT_TYPE.Revive) return true;
            return false;
        }
    }
}