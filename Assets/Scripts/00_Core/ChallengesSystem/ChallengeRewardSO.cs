using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Base class for anything granted when a challenge is completed.
    /// </summary>
    public abstract class ChallengeRewardSO : ScriptableObject
    {
        /// <summary>
        /// Applies the reward. Must not store per-player state in the asset.
        /// </summary>
        /// <param name="manager">Manager that owns the persistent state.</param>
        public abstract void Grant(ChallengeManager manager);
    }
}