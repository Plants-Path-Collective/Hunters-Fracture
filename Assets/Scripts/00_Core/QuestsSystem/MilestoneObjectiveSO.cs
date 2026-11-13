using System;
using UnityEngine;
using Core.ChallengesSystem;

namespace Core.QuestsSystem
{
    /// <summary>Advances when the matching gameplay milestone is reported.</summary>
    [CreateAssetMenu(menuName = "Quests/Objectives/Milestone", fileName = "MilestoneObjective_")]
    public class MilestoneObjectiveSO : QuestObjectiveSO
    {
        [Tooltip("Milestone ID reported through ChallengeEvents.")]
        [SerializeField] private string milestoneID;

        [Tooltip("Number of matching reports required to complete this objective.")]
        [SerializeField, Min(1)] private int requiredCount = 1;

        public string MilestoneID => milestoneID;
        public override int Target => Mathf.Max(1, requiredCount);

        public override Action Subscribe(Action<int> addProgress)
        {
            if (addProgress == null) throw new ArgumentNullException(nameof(addProgress));

            string id = milestoneID;
            Action<string> handler = reported =>
            {
                if (reported == id) addProgress(1);
            };

            ChallengeEvents.OnMilestoneReported += handler;
            return () => ChallengeEvents.OnMilestoneReported -= handler;
        }

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;

            if (string.IsNullOrWhiteSpace(milestoneID))
            {
                error = $"'{ObjectiveID}' has no milestoneID.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
