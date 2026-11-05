namespace Core.ChallengesSystem
{
    /// <summary>
    /// Numeric stats that gameplay systems can report to the challenge system.
    /// Add new entries here when a new kind of counter is needed.
    /// </summary>
    public enum CHALLENGE_STAT
    {
        EnemiesKilled,
        ItemsCollected,
        CombatsWon,
        CombatsFled,
        DamageDealt,
        RunsStarted,
        RunsCompleted
    }

    /// <summary>
    /// Defines how long the progress of a challenge is kept.
    /// </summary>
    public enum CHALLENGE_SCOPE
    {
        /// <summary>Progress is kept forever across runs.</summary>
        Lifetime,
        /// <summary>Progress is reset when a new run begins.</summary>
        PerRun
    }

    /// <summary>
    /// Helpers to build consistent unlock keys ("category:id").
    /// </summary>
    public static class UnlockKeys
    {
        public static string Item(int itemID) => $"item:{itemID}";
        public static string Unit(string unitID) => $"unit:{unitID}";
    }
}