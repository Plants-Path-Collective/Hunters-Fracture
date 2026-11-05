using System.Collections.Generic;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Single asset listing every challenge. Passive lookup by challengeID.
    /// Not a singleton: ChallengeManager receives it by reference.
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Challenge Catalog", fileName = "ChallengeCatalog")]
    public class ChallengeCatalog : ScriptableObject
    {
        [Tooltip("All challenges available in the game.")]
        [SerializeField] private List<ChallengeSO> challenges = new();

        private Dictionary<string, ChallengeSO> lookup;

        /// <summary>All registered challenges.</summary>
        public IReadOnlyList<ChallengeSO> Challenges => challenges;

        /// <summary>
        /// Finds a challenge by ID.
        /// </summary>
        public bool TryGet(string challengeID, out ChallengeSO challenge)
        {
            BuildLookup();
            return lookup.TryGetValue(challengeID, out challenge);
        }

        private void BuildLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, ChallengeSO>();
            foreach (ChallengeSO challenge in challenges)
            {
                if (challenge == null) continue;

                if (!lookup.TryAdd(challenge.challengeID, challenge))
                    Debug.LogWarning($"[Challenges] Duplicate challengeID '{challenge.challengeID}'.", challenge);
            }
        }

        private void OnValidate() => lookup = null;
    }
}