using UnityEngine;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Dialogue action that accepts a quest. Declining is simply an answer without this action.
    /// </summary>
    [CreateAssetMenu(menuName = "Quests/Dialogue Actions/Accept Quest", fileName = "AcceptQuest_")]
    public class AcceptQuestActionSO : DialogueActionSO
    {
        [Tooltip("Quest accepted when the player picks the answer.")]
        [SerializeField] private QuestSO quest;

        public override void Execute(ConversationTrigger source)
        {
            if (quest == null)
            {
                Debug.LogWarning("[Quests] AcceptQuestActionSO has no quest assigned.", this);
                return;
            }

            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[Quests] No QuestManager: quest not accepted.", this);
                return;
            }

            if (!manager.Accept(quest.questID, out string reason))
                Debug.LogWarning($"[Quests] Accept action failed: {reason}", this);
        }
    }
}