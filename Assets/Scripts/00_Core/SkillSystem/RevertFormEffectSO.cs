namespace Core.CombatSystem.SkillSystem
{
    /// <summary>Makes the actor leave its alternate form voluntarily (e.g. Elowen's "return to human").</summary>
    [UnityEngine.CreateAssetMenu(fileName = "NewRevertFormEffect", menuName = "Skill System/Effects/Revert Form")]
    public class RevertFormEffectSO : ActionEffectSO
    {
        public override void Apply(ActionContext context, CombatController combat)
        {
            combat.ExitForm(context.actor, FORM_EXIT_REASON.Voluntary);
        }
    }
}