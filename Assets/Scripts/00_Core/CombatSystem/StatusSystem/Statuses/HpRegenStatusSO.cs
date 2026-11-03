using UnityEngine;
using Core.CombatSystem;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>Restores magnitude HP to the bearer at the end of each of its turns.</summary>
    [CreateAssetMenu(fileName = "NewHpRegenStatus", menuName = "Status System/HP Regen")]
    public class HpRegenStatusSO : StatusSO
    {
        public override void OnTurnEnd(StatusInstance instance, CombatController combat)
        {
            int amount = Mathf.Max(1, Mathf.RoundToInt(instance.magnitude));
            combat.Heal(instance.source, instance.target, amount, "regen");
        }
    }
}