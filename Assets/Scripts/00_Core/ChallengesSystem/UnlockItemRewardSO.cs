using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Unlocks an item so it can appear in the reward pool.
    /// References the ItemSO directly, so there are no string keys to mistype.
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Rewards/Unlock Item", fileName = "UnlockItem_")]
    public class UnlockItemRewardSO : ChallengeRewardSO
    {
        [Tooltip("Item that becomes available once the challenge is completed.")]
        [SerializeField] private ItemSO item;

        public override void Grant(ChallengeManager manager)
        {
            if (item == null)
            {
                Debug.LogWarning($"[Challenges] {name} has no item assigned.", this);
                return;
            }

            manager.AddUnlock(UnlockKeys.Item(item.itemID));
        }
    }
}