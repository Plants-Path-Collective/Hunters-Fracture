using UnityEngine;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>
    /// The actor takes the damage aimed at each target for a while, and raises its own guard as if it
    /// had used Defend (without ending the turn or restoring SP: the action itself does that).
    /// </summary>
    [CreateAssetMenu(fileName = "NewBodyshieldEffect", menuName = "Skill System/Effects/Bodyshield")]
    public class BodyshieldEffectSO : ActionEffectSO
    {
        [Header("--- Bodyshield ---")]
        [SerializeField] private BodyshieldStatusSO status;
        [Tooltip("Turns of the protected unit during which the actor takes its damage.")]
        [SerializeField, Min(1)] private int duration = 1;
        [Tooltip("Turn advances the actor's guard lasts. 2 is the same as Defend: it only covers the next " +
                 "unit to act. Raise it to cover more enemy turns.")]
        [SerializeField, Min(1)] private int guardAdvances = 2;

        public override void Apply(ActionContext context, CombatController combat)
        {
            if (status == null) return;

            Unit guardian = context.actor;
            int turns = Mathf.Max(1, Mathf.RoundToInt(duration * context.effectScale));
            bool protectedAnyone = false;

            foreach (Unit target in ResolveTargets(context, combat))
            {
                if (target == guardian) continue;

                combat.ApplyStatus(guardian, target, status, 1f, turns);
                protectedAnyone = true;
            }

            if (protectedAnyone)
                combat.RaiseGuard(guardian, guardAdvances);
        }
    }
}