using UnityEngine;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Picks the quest conversation for the NPC on this GameObject.
    /// A quest conversation slot that is empty is skipped, so the NPC falls back to its regular conversation.
    /// </summary>
    [RequireComponent(typeof(ConversationTrigger))]
    public class NpcQuestGiver : MonoBehaviour, IConversationOverride
    {

        private ConversationTrigger trigger;
        private QuestManager manager;

        private void Awake() => trigger = GetComponent<ConversationTrigger>();

        private void OnEnable() => Bind();

        private void Start() => Bind();

        private void OnDisable()
        {
            if (manager != null) manager.OnQuestChanged -= HandleQuestChanged;
            manager = null;
            if (trigger != null) trigger.SetRequest(false);
        }

        public bool TryGetConversation(out ConversationSO conversation)
        {
            conversation = null;
            if (!isActiveAndEnabled || !Bind()) return false;

            foreach (Slot slot in System.Enum.GetValues(typeof(Slot)))
            {
                foreach (QuestSO quest in manager.Quests)
                {
                    ConversationSO candidate = Pick(quest, slot);
                    if (candidate == null || candidate.lines == null || candidate.lines.Length == 0) continue;

                    conversation = candidate;
                    return true;
                }
            }

            return false;
        }

        private ConversationSO Pick(QuestSO quest, Slot slot)
        {
            if (quest == null) return null;

            QUEST_STATE state = manager.GetState(quest);
            bool isGiver = quest.giverNpcID == trigger.NpcID;
            bool isTurnIn = quest.EffectiveTurnInNpcID == trigger.NpcID;

            switch (slot)
            {
                case Slot.TurnIn:
                    return state == QUEST_STATE.ReadyToTurnIn && isTurnIn && QuestManager.InTurnInWindow(quest)
                        ? quest.turnInConversation : null;
                case Slot.Expired:
                    return state == QUEST_STATE.Expired && isTurnIn ? quest.expiredConversation : null;
                case Slot.Offer:
                    return state == QUEST_STATE.Available && isGiver && QuestManager.InOfferWindow(quest)
                        ? quest.offerConversation : null;
                case Slot.InProgress:
                    return (state == QUEST_STATE.Active || state == QUEST_STATE.ReadyToTurnIn) && isGiver
                        ? quest.inProgressConversation : null;
                case Slot.Completed:
                    return state == QUEST_STATE.Completed && isTurnIn ? quest.completedConversation : null;
            }

            return null;
        }

        private bool Bind()
        {
            if (manager != null) return true;

            manager = QuestManager.Instance;
            if (manager == null) return false;

            manager.OnQuestChanged += HandleQuestChanged;
            RefreshRequest();
            return true;
        }

        private void HandleQuestChanged(QuestSO quest) => RefreshRequest();

        // While this NPC has a pending quest, the regular conversation index must not advance.
        private void RefreshRequest()
        {
            if (trigger == null || manager == null) return;

            bool pending = false;
            foreach (QuestSO quest in manager.Quests)
            {
                if (quest == null || (quest.giverNpcID != trigger.NpcID && quest.EffectiveTurnInNpcID != trigger.NpcID))
                    continue;

                QUEST_STATE state = manager.GetState(quest);
                if (state == QUEST_STATE.Active || state == QUEST_STATE.ReadyToTurnIn)
                {
                    pending = true;
                    break;
                }
            }

            trigger.SetRequest(pending);
        }
    }
}