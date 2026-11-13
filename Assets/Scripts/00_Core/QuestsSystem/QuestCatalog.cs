using System.Collections.Generic;
using UnityEngine;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Single asset listing every quest. Passive lookup by questID.
    /// Not a singleton: QuestManager receives it by reference.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Quests/QuestCatalog")]
    public class QuestCatalog : ScriptableObject
    {
        [Tooltip("All quests available in the game.")]
        [SerializeField] private List<QuestSO> quests = new();

        private Dictionary<string, QuestSO> lookup;

        /// <summary>All registered quests.</summary>
        public IReadOnlyList<QuestSO> Quests => quests;

        /// <summary>
        /// Finds a quest by ID.
        /// </summary>
        public bool TryGet(string questID, out QuestSO quest)
        {
            BuildLookup();
            if (string.IsNullOrEmpty(questID))
            {
                quest = null;
                return false;
            }

            return lookup.TryGetValue(questID, out quest);
        }

        private void BuildLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, QuestSO>();
            foreach (QuestSO quest in quests)
            {
                if (quest == null) continue;

                if (!lookup.TryAdd(quest.questID, quest))
                    Debug.LogWarning($"[Quests] Duplicate questID '{quest.questID}'.", quest);
            }
        }

        private void OnValidate() => lookup = null;
    }
}