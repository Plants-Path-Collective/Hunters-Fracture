using UnityEngine;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>Dialogue action: turns in a quest (charges money if the condition says so).</summary>
    [CreateAssetMenu(menuName = "Quests/Dialogue Actions/Turn In Quest", fileName = "TurnInQuest_")]
    public class TurnInQuestActionSO : DialogueActionSO
    {
        [SerializeField] private QuestSO quest;

        public override void Execute(ConversationTrigger source)
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("[Quests] No QuestManager: quest not turned in.", this);
                return;
            }

            if (!QuestManager.Instance.TryTurnIn(quest, out string reason))
                Debug.LogWarning($"[Quests] Turn-in failed: {reason}", this);
        }
    }
}