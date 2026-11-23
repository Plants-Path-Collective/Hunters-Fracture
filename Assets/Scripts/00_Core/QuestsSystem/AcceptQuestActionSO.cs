using UnityEngine;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>Dialogue action: accepts a quest. Refusing is simply an answer WITHOUT this action.</summary>
    [CreateAssetMenu(menuName = "Quests/Dialogue Actions/Accept Quest", fileName = "AcceptQuest_")]
    public class AcceptQuestActionSO : DialogueActionSO
    {
        [SerializeField] private QuestSO quest;

        public override void Execute(ConversationTrigger source)
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("[Quests] No QuestManager: quest not accepted.", this);
                return;
            }

            if (!QuestManager.Instance.Accept(quest, out string reason))
                Debug.LogWarning($"[Quests] Accept failed: {reason}", this);
        }
    }
}