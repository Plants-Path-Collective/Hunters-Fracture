using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Unlocks a playable character. The unlock is permanent (saved with the challenges);
    /// PartyRecruiter is what actually adds the character to the party.
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Rewards/Unlock Unit", fileName = "UnlockUnit_")]
    public class UnlockUnitRewardSO : ChallengeRewardSO
    {
        [Tooltip("Character that becomes available once the challenge is completed.")]
        [SerializeField] private UnitDefinitionSO unit;

        public override void Grant(ChallengeManager manager)
        {
            if (unit == null || string.IsNullOrEmpty(unit.unitID))
            {
                Debug.LogWarning($"[Challenges] {name} has no unit assigned, or the unit has an empty unitID.", this);
                return;
            }

            manager.AddUnlock(UnlockKeys.Unit(unit.unitID));
        }
    }
}