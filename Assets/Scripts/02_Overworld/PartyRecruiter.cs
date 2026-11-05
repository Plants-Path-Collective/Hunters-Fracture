using System.Collections.Generic;
using UnityEngine;
using Core.ChallengesSystem;
using Core.CombatSystem.Units;

namespace Overworld
{
    /// <summary>
    /// Adds characters unlocked through challenges to the player's party.
    /// Syncs when the Overworld starts (unlocks are permanent, so they join every run)
    /// and whenever a challenge is completed (they join the current run immediately).
    /// </summary>
    public class PartyRecruiter : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player whose party receives the recruits. Defaults to the PlayerEntity on this GameObject.")]
        [SerializeField] private PlayerEntity player;

        [Header("Data")]
        [Tooltip("Every character that a challenge can unlock (see UnlockUnitRewardSO).")]
        [SerializeField] private List<UnitDefinitionSO> recruitableUnits = new();

        private ChallengeManager manager;

        private void Start()
        {
            if (player == null) TryGetComponent(out player);

            manager = ChallengeManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[{nameof(PartyRecruiter)}] ChallengeManager.Instance is null — " +
                    "recruits will not be added (are you Play-ing this scene without the boot scene?).");
                return;
            }

            manager.OnChallengeCompleted += HandleChallengeCompleted;
            SyncParty();
        }

        private void OnDestroy()
        {
            if (manager != null) manager.OnChallengeCompleted -= HandleChallengeCompleted;
        }

        /// <summary>
        /// Adds every unlocked recruitable character that is not in the party yet.
        /// </summary>
        public void SyncParty()
        {
            if (manager == null || player == null) return;

            foreach (UnitDefinitionSO unit in recruitableUnits)
            {
                if (unit == null || string.IsNullOrEmpty(unit.unitID)) continue;
                if (!manager.IsUnlocked(UnlockKeys.Unit(unit.unitID))) continue;
                if (player.Party.Contains(unit)) continue;

                // currentHP / currentSP default to -1, so the recruit starts at full HP/SP.
                player.Party.TryAdd(new PartyMemberData { definition = unit });
            }
        }

        private void HandleChallengeCompleted(ChallengeSO challenge) => SyncParty();
    }
}