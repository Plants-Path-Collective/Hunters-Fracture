using System.Collections.Generic;
using Core;
using CombatSystem.UnitSystem;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Pure calculation layer — no Unity lifecycle, no UI, no ATB.
    /// Receives an action description and applies it to the targets.
    /// Called by TurnHandler (normal turns) and MinigameController (Alianza result).
    /// </summary>
    public class ActionResolver : MonoBehaviour
    {
        // ── Flee ─────────────────────────────────────────────────────────────

        private const float FleeSuccessChance = 0.5f;

        public bool TryFlee()
        {
            return Random.value < FleeSuccessChance;
        }

        // ── Basic Attack ──────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a basic attack from attacker to a single target.
        /// Uses the attacker's damage type to pick Strength or MagicPower.
        /// Returns actual damage dealt (0 if miss).
        /// </summary>
        public float ResolveBasicAttack(Unit attacker, Unit target, DAMAGE_TYPE damageType)
        {
            if (!RollHit(attacker.Accuracy, target.Evasion)) return 0f;

            float raw    = damageType == DAMAGE_TYPE.Physical ? attacker.Strength : attacker.MagicPower;
            float defense = damageType == DAMAGE_TYPE.Physical ? target.PhysicalDef : target.MagicalDef;
            float damage = CalculateDamage(raw, defense);

            target.ModifyHP(-damage);
            return damage;
        }

        // ── Skill ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves all effects of a skill from caster to a list of targets.
        /// Also deducts SP/HP cost from the caster.
        /// </summary>
        public void ResolveSkill(SkillSO skill, Unit caster, List<Unit> targets, int skillIndex)
        {
            // Pay costs
            caster.ModifySP(-skill.spCost);
            caster.ModifyHP(-skill.hpCost);
            caster.ConsumeSkillUse(skillIndex);

            // Apply every effect to every target
            foreach (Unit target in targets)
            {
                if (target.State == UNIT_STATE.Dead && !SkillCanTargetDead(skill)) continue;

                foreach (SkillEffect effect in skill.effects)
                    ApplyEffect(skill, effect, caster, target);
            }
        }

        // ── Disputa result ────────────────────────────────────────────────────

        /// <summary>
        /// Applies the result of a Disputa minigame.
        /// Winner keeps their turn (handled by CombatManager).
        /// Loser takes damage proportional to potencia and resets their ATB.
        /// </summary>
        public void ResolveDisputaResult(Unit winner, Unit loser, int loserPotencia)
        {
            // Damage scales linearly with potencia: each point = 5% of loser's maxHP
            float damagePercent = loserPotencia * 0.05f;
            float damage        = loser.MaxHP * damagePercent;

            loser.ModifyHP(-damage);
            loser.ResetATB();

            Debug.Log($"[Disputa] {winner.UnitName} wins. " +
                      $"{loser.UnitName} takes {damage:F1} damage and resets ATB.");
        }

        // ── Alianza result ────────────────────────────────────────────────────

        /// <summary>
        /// Applies the combined attack from an Alianza minigame.
        /// Damage scales with potencia earned during the minigame.
        /// Both units act; targets are all enemies (can be changed per design).
        /// </summary>
        public void ResolveAlianzaAttack(Unit unitA, Unit unitB,
                                         int potencia, List<Unit> targets)
        {
            // Base power is average of both units' offensive stat
            float powerA   = GetOffensiveStat(unitA);
            float powerB   = GetOffensiveStat(unitB);
            float combined = (powerA + powerB) * (1f + potencia * 0.1f); // +10% per potencia point

            foreach (Unit target in targets)
            {
                if (target.State == UNIT_STATE.Dead) continue;

                float defense = GetDefenseStat(target, unitA.Sheet.damageType);
                float damage  = CalculateDamage(combined, defense);
                target.ModifyHP(-damage);
            }

            Debug.Log($"[Alianza] {unitA.UnitName} + {unitB.UnitName} " +
                      $"combined attack with potencia {potencia}.");
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void ApplyEffect(SkillSO skill, SkillEffect effect, Unit caster, Unit target)
        {
            switch (effect.effectType)
            {
                case EFFECT_TYPE.Damage:
                {
                    if (!RollHit(skill.accuracy, target.Evasion)) return;
                    float offStat = skill.damageType == DAMAGE_TYPE.Physical
                        ? caster.Strength : caster.MagicPower;
                    float defense = GetDefenseStat(target, skill.damageType);
                    float damage  = CalculateDamage(skill.basePower * offStat, defense) * effect.value;
                    target.ModifyHP(-damage);
                    break;
                }

                case EFFECT_TYPE.Heal:
                {
                    float heal = skill.basePower * caster.MagicPower * effect.value;
                    target.ModifyHP(heal);
                    break;
                }

                case EFFECT_TYPE.StealHP:
                {
                    if (!RollHit(skill.accuracy, target.Evasion)) return;
                    float damage = CalculateDamage(skill.basePower * caster.Strength, GetDefenseStat(target, DAMAGE_TYPE.Physical));
                    target.ModifyHP(-damage);
                    caster.ModifyHP(damage * effect.value); // value = drain ratio (e.g. 0.5)
                    break;
                }

                case EFFECT_TYPE.Buff:
                    ApplyStatMod(target, effect.statAffected, +effect.value);
                    break;

                case EFFECT_TYPE.Debuff:
                    if (RollChance(effect.chance))
                        ApplyStatMod(target, effect.statAffected, -effect.value);
                    break;

                case EFFECT_TYPE.StatusCondition:
                    if (RollChance(effect.chance))
                        target.ApplyStatus(effect.condition);
                    break;

                case EFFECT_TYPE.RemoveStatus:
                    target.ClearStatus();
                    break;

                case EFFECT_TYPE.Shield:
                    // Shield is implemented as a temporary HP buffer
                    target.ModifyHP(+effect.value);
                    break;

                case EFFECT_TYPE.Revive:
                    if (target.State == UNIT_STATE.Dead)
                        target.Revive(effect.value); // value = revive HP percent (e.g. 0.3 = 30%)
                    break;
            }
        }

        private static float CalculateDamage(float rawPower, float defensePercent)
        {
            // Defense is a percentage reduction (0–100)
            float reduction = Mathf.Clamp01(defensePercent / 100f);
            return Mathf.Max(1f, rawPower * (1f - reduction));
        }

        private static bool RollHit(float accuracy, float evasion)
        {
            float hitChance = Mathf.Clamp(accuracy - evasion, 0f, 100f);
            return Random.Range(0f, 100f) <= hitChance;
        }

        private static bool RollChance(float chance)
        {
            // chance is 0–1
            return Random.value <= Mathf.Clamp01(chance);
        }

        private static float GetOffensiveStat(Unit unit)
        {
            return unit.Sheet.damageType == DAMAGE_TYPE.Physical
                ? unit.Strength : unit.MagicPower;
        }

        private static float GetDefenseStat(Unit target, DAMAGE_TYPE damageType)
        {
            return damageType == DAMAGE_TYPE.Physical
                ? target.PhysicalDef : target.MagicalDef;
        }

        private static void ApplyStatMod(Unit target, STAT_TYPE stat, float delta)
        {
            switch (stat)
            {
                case STAT_TYPE.Speed:          target.SpeedMod       += delta; break;
                case STAT_TYPE.Strength:       target.StrengthMod    += delta; break;
                case STAT_TYPE.MagicPower:     target.MagicPowerMod  += delta; break;
                case STAT_TYPE.Evasion:        target.EvasionMod     += delta; break;
                case STAT_TYPE.Accuracy:       target.AccuracyMod    += delta; break;
                case STAT_TYPE.PhysicalDefense:target.PhysicalDefMod += delta; break;
                case STAT_TYPE.MagicDefense:   target.MagicalDefMod  += delta; break;
                case STAT_TYPE.HP:             target.ModifyHP(delta);          break;
                case STAT_TYPE.SP:             target.ModifySP(delta);          break;
            }
        }

        private static bool SkillCanTargetDead(SkillSO skill)
        {
            foreach (var effect in skill.effects)
                if (effect.effectType == EFFECT_TYPE.Revive) return true;
            return false;
        }
    }
}
