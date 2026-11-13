namespace Core.QuestsSystem
{
    /// <summary>
    /// Lifecycle state of a quest during the current run.
    /// Unavailable and Available are derived (never saved); the rest are stored.
    /// </summary>
    public enum QUEST_STATE
    {
        /// <summary>Prerequisites are not met.</summary>
        Unavailable,
        /// <summary>Can be offered by its giver NPC.</summary>
        Available,
        /// <summary>Accepted, steps are being tracked.</summary>
        Active,
        /// <summary>All steps done, waiting for the turn-in NPC.</summary>
        ReadyToTurnIn,
        /// <summary>Turned in and rewarded.</summary>
        Completed,
        /// <summary>Accepted but not turned in before its window closed.</summary>
        Expired
    }

    /// <summary>
    /// Helpers to build consistent milestone IDs ("category:id") reported through
    /// ChallengeEvents.ReportMilestone and consumed by quest steps.
    /// </summary>
    public static class QuestMilestones
    {
        public static string QuestCompleted(string questID) => $"quest_completed:{questID}";
        public static string EnemyDefeated(string unitID) => $"enemy_defeated:{unitID}";
        public static string Talk(int npcID) => $"talk:{npcID}";
        public static string QuestItem(string itemID) => $"quest_item:{itemID}";
        public static string ItemCollected(string itemID) => $"item_collected:{itemID}";
        public static string SceneEntered(string sceneName) => $"scene_entered:{sceneName}";
    }
}