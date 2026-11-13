using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Core.QuestsSystem
{
    /// <summary>
    /// One visible step of a quest, advanced by a milestone reported through
    /// ChallengeEvents.ReportMilestone. Progress is counted only since the quest was accepted.
    /// </summary>
    [Serializable]
    public struct QuestStep
    {
        [Tooltip("Stable key inside this quest, used by the save file. Never change it after release.")]
        public string stepID;

        [Tooltip("Smart String shown in the UI, e.g. 'Defeat {current}/{target} wolves'.")]
        public LocalizedString text;

        [Tooltip("Milestone ID that advances this step (see QuestMilestones).")]
        public string milestoneID;

        [Tooltip("Times the milestone must be reported.")]
        [Min(1)] public int requiredCount;

        /// <summary>Required count, never below 1 (a freshly added row serializes as 0).</summary>
        public int Target => Mathf.Max(1, requiredCount);
    }
}