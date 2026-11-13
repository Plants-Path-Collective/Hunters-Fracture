using UnityEngine;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Base class for data-driven actions executed when a quest expires
    /// (e.g. make an NPC leave). Must not store per-run state in the asset.
    /// </summary>
    public abstract class QuestConsequenceSO : ScriptableObject
    {
        /// <summary>Applies the consequence.</summary>
        /// <param name="manager">Manager that owns the quest state.</param>
        /// <param name="quest">Quest that expired.</param>
        public abstract void Execute(QuestManager manager, QuestSO quest);
    }
}