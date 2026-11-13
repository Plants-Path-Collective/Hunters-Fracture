namespace PlantsPathCo.DialogueSystem
{
    /// <summary>
    /// Optional component next to a ConversationTrigger that can replace the NPC's regular
    /// conversation (e.g. a quest offer). While an override plays, the regular index does not advance.
    /// </summary>
    public interface IConversationOverride
    {
        /// <summary>
        /// Returns true and the conversation to play, or false to fall back to the regular index.
        /// </summary>
        bool TryGetConversation(out ConversationSO conversation);
    }
}