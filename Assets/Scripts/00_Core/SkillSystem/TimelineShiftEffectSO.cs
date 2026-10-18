using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    public enum TIMELINE_SHIFT { Advance, Delay }

    [CreateAssetMenu(fileName = "NewTimelineShiftEffect", menuName = "Skill System/Effects/Timeline Shift")]
    public class TimelineShiftEffectSO : ActionEffectSO
    {
        [Header("--- Shift ---")]
        [SerializeField] private TIMELINE_SHIFT shift;
        [SerializeField, Min(1)] private int amount = 1;

        public override void Apply(ActionContext context, CombatController combat)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(amount * context.effectScale));

            // The acting unit is out of the queue until its turn ends, so shifting it is a no-op.
            foreach (Unit target in ResolveTargets(context, combat))
            {
                if (shift == TIMELINE_SHIFT.Advance) combat.Advance(target, n);
                else combat.Delay(target, n);
            }
        }
    }
}