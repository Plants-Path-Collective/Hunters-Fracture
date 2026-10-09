using UnityEngine;
using Core.CombatSystem;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>Restores magnitude SP to the bearer at the end of each of its turns.</summary>
    [CreateAssetMenu(fileName = "NewSpRegenStatus", menuName = "Status System/SP Regen")]
    public class SpRegenStatusSO : StatusSO
    {
        public override void OnTurnEnd(StatusInstance instance, CombatController combat)
        {
            combat.RestoreSP(instance.target, Mathf.RoundToInt(instance.magnitude));
        }
    }
}