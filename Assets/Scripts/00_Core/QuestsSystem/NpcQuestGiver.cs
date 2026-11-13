using UnityEngine;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Selects quest conversations for the NPC on the same GameObject.
    /// Equal-priority quests are resolved in QuestCatalog order.
    /// </summary>
    [RequireComponent(typeof(ConversationTrigger))]
    public class NpcQuestGiver : MonoBehaviour, IConversationOverride
    {
        private enum ConversationPriority
        {
            TurnIn,
            Expired,
            Offer,
            Reminder,
            OfferClosed,
            Completed
        }

        private ConversationTrigger trigger;
        private QuestManager manager;

        private void Awake()
        {
            trigger = GetComponent<ConversationTrigger>();
        }

        private void Start()
        {
            if (!BindManager())
                Debug.LogError($"[{nameof(NpcQuestGiver)}] No QuestManager found for '{name}'.", this);
        }

        private void OnEnable()
        {
            if (trigger == null) trigger = GetComponent<ConversationTrigger>();
            BindManager();
        }

        private void OnDisable()
        {
            if (manager != null)
                manager.OnQuestStateChanged -= HandleQuestStateChanged;

            manager = null;
            if (trigger != null) trigger.SetRequest(false);
        }

        public bool TryGetConversation(out ConversationSO conversation)
        {
            conversation = null;
            if (!isActiveAndEnabled || trigger == null || !BindManager()) return false;

            var quests = manager.Catalog.Quests;
            for (int priority = 0; priority <= (int)ConversationPriority.Completed; priority++)
            {
                foreach (QuestSO quest in quests)
                {
                    if (quest == null ||
                        !TryGetConversationAtPriority(quest, trigger.NpcID, (ConversationPriority)priority, out ConversationSO candidate))
                        continue;

                    if (candidate == null || candidate.lines == null || candidate.lines.Length == 0)
                        Debug.LogWarning($"[{nameof(NpcQuestGiver)}] Quest '{quest.questID}' has no valid conversation for priority {(ConversationPriority)priority}.", this);

                    conversation = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool BindManager()
        {
            if (manager != null) return true;

            manager = QuestManager.Instance;
            if (manager == null) return false;

            manager.OnQuestStateChanged += HandleQuestStateChanged;
            RefreshRequest();
            return true;
        }

        private bool TryGetConversationAtPriority(
            QuestSO quest,
            int npcID,
            ConversationPriority priority,
            out ConversationSO conversation)
        {
            conversation = null;
            QUEST_STATE state = manager.GetState(quest);
            bool isGiver = quest.giverNpcID == npcID;
            bool isTurnInNpc = quest.EffectiveTurnInNpcID == npcID;
            bool matched = false;

            switch (priority)
            {
                case ConversationPriority.TurnIn:
                    if (state == QUEST_STATE.ReadyToTurnIn &&
                        isTurnInNpc &&
                        QuestManager.InTurnInWindow(quest))
                    {
                        matched = true;
                        conversation = quest.turnInConversation;
                    }
                    break;

                case ConversationPriority.Expired:
                    if (state == QUEST_STATE.Expired && isTurnInNpc)
                    {
                        matched = true;
                        conversation = quest.expiredConversation;
                    }
                    break;

                case ConversationPriority.Offer:
                    if (state == QUEST_STATE.Available &&
                        isGiver &&
                        QuestManager.InOfferWindow(quest))
                    {
                        matched = true;
                        conversation = quest.offerConversation;
                    }
                    break;

                case ConversationPriority.Reminder:
                    bool waitingOutsideTurnInWindow =
                        state == QUEST_STATE.ReadyToTurnIn && !QuestManager.InTurnInWindow(quest);
                    if ((state == QUEST_STATE.Active || waitingOutsideTurnInWindow) && isGiver)
                    {
                        matched = true;
                        conversation = quest.reminderConversation;
                    }
                    break;

                case ConversationPriority.OfferClosed:
                    if (state == QUEST_STATE.Available &&
                        isGiver &&
                        !QuestManager.InOfferWindow(quest))
                    {
                        matched = true;
                        conversation = quest.offerClosedConversation;
                    }
                    break;

                case ConversationPriority.Completed:
                    if (state == QUEST_STATE.Completed && isTurnInNpc)
                    {
                        matched = true;
                        conversation = quest.completedConversation;
                    }
                    break;
            }

            return matched;
        }

        private void HandleQuestStateChanged(QuestSO changed, QUEST_STATE state) => RefreshRequest();

        private void RefreshRequest()
        {
            if (trigger == null || manager == null) return;

            bool hasPendingQuest = false;
            foreach (QuestSO quest in manager.Catalog.Quests)
            {
                if (quest == null ||
                    (quest.giverNpcID != trigger.NpcID && quest.EffectiveTurnInNpcID != trigger.NpcID))
                    continue;

                QUEST_STATE state = manager.GetState(quest);
                if (state == QUEST_STATE.Active || state == QUEST_STATE.ReadyToTurnIn)
                {
                    hasPendingQuest = true;
                    break;
                }
            }

            trigger.SetRequest(hasPendingQuest);
        }
    }
}