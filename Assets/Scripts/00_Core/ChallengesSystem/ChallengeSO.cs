using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Data definition of a single challenge: identity, condition and rewards.
    /// Progress is NOT stored here; it lives in ChallengeManager and the save file.
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Challenge", fileName = "Challenge_")]
    public class ChallengeSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique key used in the save file. Never change it after release.")]
        public string challengeID;

        [Tooltip("Localized display name.")]
        public LocalizedString challengeName;

        [Tooltip("Localized description shown in the challenges list.")]
        public LocalizedString description;

        [Tooltip("Icon shown in the UI.")]
        public Sprite icon;

        [Tooltip("If true, the UI hides name and description until it is completed.")]
        public bool hidden;

        [Header("Logic")]
        [Tooltip("What must happen to complete this challenge.")]
        public ChallengeConditionSO condition;

        [Tooltip("Lifetime keeps progress forever; PerRun resets when a new run begins.")]
        public CHALLENGE_SCOPE scope = CHALLENGE_SCOPE.Lifetime;

        [Header("Rewards")]
        [Tooltip("Rewards granted once, on completion.")]
        public List<ChallengeRewardSO> rewards = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-fill the ID with the asset name the first time; later renames do not change it.
            if (string.IsNullOrEmpty(challengeID))
            {
                challengeID = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}