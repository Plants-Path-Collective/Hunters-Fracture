using UnityEngine;

namespace PlantsPathCo.DialogueSystem
{
    /// <summary>
    /// Base class for data-driven actions executed when the player picks an answer
    /// (accept a quest, turn it in...). The asset is shared, so it must not store per-run state.
    /// </summary>
    public abstract class DialogueActionSO : ScriptableObject
    {
        /// <summary>Runs the action.</summary>
        /// <param name="source">Trigger of the NPC that is talking. Can be null.</param>
        public abstract void Execute(ConversationTrigger source);
    }
}