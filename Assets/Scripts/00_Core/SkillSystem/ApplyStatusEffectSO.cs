using UnityEngine;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "NewApplyStatusEffect", menuName = "Skill System/Effects/Apply Status")]
    public class ApplyStatusEffectSO : ActionEffectSO
    {
        [Header("--- Status ---")]
        [SerializeField] private StatusSO status;
        [Tooltip("Meaning depends on the status: fraction (0.5 = 50%), damage per turn, SP per turn. Ignored by skip-turn statuses.")]
        [SerializeField] private ScaledValue magnitude;
        [Tooltip("Turns of the bearer (skipped turns for skip-turn statuses).")]
        [SerializeField, Min(1)] private int duration = 3;

        public override void Apply(ActionContext context, CombatController combat)
        {
            if (status == null) return;

            float value = magnitude.Evaluate(context.actor) * context.effectScale;
            int turns = Mathf.Max(1, Mathf.RoundToInt(duration * context.effectScale));

            foreach (Unit target in ResolveTargets(context, combat))
                combat.ApplyStatus(context.actor, target, status, value, turns);
        }
    }
}