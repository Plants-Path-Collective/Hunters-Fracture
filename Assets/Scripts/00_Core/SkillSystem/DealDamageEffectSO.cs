using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>
    /// Deals damage to the effect's targets. Base damage is the sum of every term
    /// (baseValue + perPoint × the caster's stat), multiplied by the action's effectScale.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDealDamageEffect", menuName = "Skill System/Effects/Deal Damage")]
    public class DealDamageEffectSO : ActionEffectSO
    {
        [Header("--- Damage ---")]
        [SerializeField] private UNITY_TYPE damageType = UNITY_TYPE.Physical;
        [Tooltip("Summed together. Each term is baseValue + perPoint × the caster's stat.")]
        [SerializeField] private ScaledValue[] terms;
        [Tooltip("Reduce the damage with the target's defense of this type (same formula as the basic attack).")]
        [SerializeField] private bool applyDefense = true;

        public override void Apply(ActionContext context, CombatController combat)
        {
            float baseDamage = 0f;
            foreach (ScaledValue term in terms)
                baseDamage += term.Evaluate(context.actor);
            baseDamage *= context.effectScale;

            foreach (Unit target in ResolveTargets(context, combat))
            {
                int amount = applyDefense
                    ? DamageFormula.Reduce(baseDamage, target, damageType)
                    : Mathf.Max(1, Mathf.RoundToInt(baseDamage));

                combat.DealDamage(context.actor, target, amount, damageType, "skill");
            }
        }
    }
}