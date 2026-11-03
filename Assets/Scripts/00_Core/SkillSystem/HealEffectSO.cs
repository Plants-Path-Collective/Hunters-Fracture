using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>Heals the effect's targets. Amount = sum of terms × multiplier × the action's effectScale.</summary>
    [CreateAssetMenu(fileName = "NewHealEffect", menuName = "Skill System/Effects/Heal")]
    public class HealEffectSO : ActionEffectSO
    {
        [Tooltip("Summed together. Each term is baseValue + perPoint × the caster's stat.")]
        [SerializeField] private ScaledValue[] terms;
        [Tooltip("Multiplies the summed amount (0.5 = half).")]
        [SerializeField, Min(0f)] private float multiplier = 1f;

        public override void Apply(ActionContext context, CombatController combat)
        {
            float amount = 0f;
            foreach (ScaledValue term in terms)
                amount += term.Evaluate(context.actor);

            int final = Mathf.Max(1, Mathf.RoundToInt(amount * multiplier * context.effectScale));

            foreach (Unit target in ResolveTargets(context, combat))
                combat.Heal(context.actor, target, final, "skill");
        }
    }
}