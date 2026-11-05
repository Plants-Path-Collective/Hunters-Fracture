using System;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Completes when a specific milestone is reported (defeat a boss, finish the game...).
    /// Use a required count greater than 1 for "do it N times".
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Conditions/Milestone", fileName = "Milestone_")]
    public class MilestoneConditionSO : ChallengeConditionSO
    {
        [Tooltip("Milestone ID that must be reported through ChallengeEvents.ReportMilestone.")]
        [SerializeField] private string milestoneID;

        [Tooltip("Times the milestone must be reported.")]
        [SerializeField, Min(1)] private int requiredCount = 1;

        public override int Target => requiredCount;

        public override Action Attach(Action<int> addProgress)
        {
            string id = milestoneID;
            Action<string> handler = reported =>
            {
                if (reported == id) addProgress(1);
            };

            ChallengeEvents.OnMilestoneReported += handler;
            return () => ChallengeEvents.OnMilestoneReported -= handler;
        }
    }
}