namespace Core.ChallengesSystem
{
    /// <summary>
    /// Persistence abstraction. Swap the implementation (JSON, Steam, cloud...)
    /// without touching ChallengeManager.
    /// </summary>
    public interface IChallengeSaveBackend
    {
        /// <summary>Loads saved data, or an empty instance if none exists.</summary>
        ChallengeSaveData Load();

        /// <summary>Persists the given data.</summary>
        void Save(ChallengeSaveData data);
    }
}