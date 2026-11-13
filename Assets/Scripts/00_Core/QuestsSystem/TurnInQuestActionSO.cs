using UnityEngine;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Dialogue action that turns in a quest and grants its rewards.
    /// </summary>
    [CreateAssetMenu(menuName = "Quests/Dialogue Actions/Turn In Quest", fileName = "TurnInQuest_")]
    public class TurnInQuestActionSO : DialogueActionSO
    {
        [Tooltip("Quest turned in when the player picks the answer.")]
        [SerializeField] private QuestSO quest;

        public override void Execute(ConversationTrigger source)
        {
            if (quest == null)
            {
                Debug.LogWarning("[Quests] TurnInQuestActionSO has no quest assigned.", this);
                return;
            }

            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[Quests] No QuestManager: quest not turned in.", this);
                return;
            }

            if (!manager.TryTurnIn(quest.questID, out string reason))
                Debug.LogWarning($"[Quests] Turn-in action failed: {reason}", this);
        }
    }
}