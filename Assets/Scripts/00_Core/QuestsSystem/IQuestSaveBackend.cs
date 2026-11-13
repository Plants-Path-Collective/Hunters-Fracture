namespace Core.QuestsSystem
{
    /// <summary>
    /// Persistence abstraction. Swap the implementation (JSON, Steam, cloud...)
    /// without touching QuestManager.
    /// </summary>
    public interface IQuestSaveBackend
    {
        /// <summary>Loads saved data, or an empty instance if none exists.</summary>
        QuestSaveData Load();

        /// <summary>Persists the given data.</summary>
        void Save(QuestSaveData data);
    }
}