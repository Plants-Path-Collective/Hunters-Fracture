using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Unlocks a content key (e.g. "item:glasscanon", "unit:elowen").
    /// Other systems query ChallengeManager.IsUnlocked(key) to filter their content.
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Rewards/Unlock", fileName = "Unlock_")]
    public class UnlockRewardSO : ChallengeRewardSO
    {
        [Tooltip("Key to unlock. Format: category:id (see UnlockKeys).")]
        [SerializeField] private string unlockKey;

        public override void Grant(ChallengeManager manager)
        {
            if (string.IsNullOrEmpty(unlockKey))
            {
                Debug.LogWarning($"[Challenges] {name} has an empty unlock key.", this);
                return;
            }

            manager.AddUnlock(unlockKey);
        }
    }
}