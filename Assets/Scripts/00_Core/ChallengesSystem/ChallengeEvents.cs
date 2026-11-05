using System;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Static hub used by any gameplay system to report stats and milestones.
    /// Reporters never need to know whether a ChallengeManager exists: if nobody
    /// listens, the call is a no-op (safe to use from test scenes).
    /// </summary>
    public static class ChallengeEvents
    {
        /// <summary>Raised when a numeric stat changes (stat, amount).</summary>
        public static event Action<CHALLENGE_STAT, int> OnStatReported;

        /// <summary>Raised when a one-off story/gameplay milestone happens (milestone ID).</summary>
        public static event Action<string> OnMilestoneReported;

        /// <summary>
        /// Reports an increment of a numeric stat.
        /// </summary>
        /// <param name="stat">Stat that changed.</param>
        /// <param name="amount">Positive amount to add.</param>
        public static void ReportStat(CHALLENGE_STAT stat, int amount = 1)
        {
            if (amount <= 0) return;
            OnStatReported?.Invoke(stat, amount);
        }

        /// <summary>
        /// Reports a milestone such as "boss_defeated:boss_01" or "game_completed".
        /// </summary>
        /// <param name="milestoneID">Unique milestone identifier.</param>
        public static void ReportMilestone(string milestoneID)
        {
            if (string.IsNullOrEmpty(milestoneID)) return;
            OnMilestoneReported?.Invoke(milestoneID);
        }

        // Clears handlers when Domain Reload is disabled in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnStatReported = null;
            OnMilestoneReported = null;
        }
    }
}