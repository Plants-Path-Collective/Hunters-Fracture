using UnityEngine;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>The bearer loses its next turns (sleep, freeze). Duration counts skipped turns.</summary>
    [CreateAssetMenu(fileName = "NewSkipTurnStatus", menuName = "Status System/Skip Turn")]
    public class SkipTurnStatusSO : StatusSO
    {
        public override bool SkipsTurn => true;
        public override bool DecrementsOnTurnEnd => false;
    }
}