using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using PlantsPathCo.DialogueSystem;
using Core.CombatSystem.Backpack;
using Core.CombatSystem.ItemSystem;

namespace Core.QuestsSystem
{
    /// <summary>One step of a quest: some text for the player plus the condition that finishes it.</summary>
    [System.Serializable]
    public class QuestStep
    {
        [Tooltip("Text shown in the quest tracker for this step (e.g. 'Find the lost package').")]
        public LocalizedString description;

        [Tooltip("What the player must do to finish this step.")]
        public QuestConditionSO condition;
    }

    /// <summary>One reward given when a quest is turned in. A quest can have several.</summary>
    [System.Serializable]
    public class QuestReward
    {
        [Tooltip("Which type of reward this entry gives.")]
        public QUEST_REWARD type;

        [ShowIfReward(nameof(type), QUEST_REWARD.Lore)]
        [Tooltip("Lore entry unlocked when the quest is turned in.")]
        public LocalizedString loreReward;

        [ShowIfReward(nameof(type), QUEST_REWARD.Pesos)]
        [Tooltip("Amount of pesos given.")]
        public float pesosReward;

        [ShowIfReward(nameof(type), QUEST_REWARD.Item)]
        public ItemSO itemReward;

        [ShowIfReward(nameof(type), QUEST_REWARD.Consumable)]
        public ConsumableSO consumableReward;

        [ShowIfReward(nameof(type), QUEST_REWARD.Consumable)]
        [Range(1, 999)] public int consumableQty = 1;

        [ShowIfReward(nameof(type), QUEST_REWARD.KeyItem)]
        [Tooltip("Key item prefab given to the player.")]
        public GameObject keyItemReward;
    }

    /// <summary>
    /// Data definition of a quest. Progress is NOT stored here; it lives in QuestManager.
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "Quests/Quest")]
    public class QuestSO : ScriptableObject
    {
        private const DAY_PHASE AllPhases = DAY_PHASE.Morning | DAY_PHASE.Afternoon | DAY_PHASE.Night;

        [Header("Identity")]
        [Tooltip("Key used ONLY in the save file. Auto-filled with the asset name; never change it after that.")]
        public string questID;

        [Tooltip("Localized quest name.")]
        public LocalizedString title;

        [Tooltip("Localized description / lore shown in the quest log.")]
        public LocalizedString summary;

        [Header("NPCs")]
        [Tooltip("npcID (ConversationTrigger) of the NPC that offers the quest.")]
        public int giverNpcID;

        [Tooltip("npcID (ConversationTrigger) of the NPC that receives the quest. -1 = same as the giver.")]
        public int turnInNpcID = -1;

        [Header("Rules")]
        [Tooltip("Quests that must be Completed (this run) before this one is offered.")]
        public List<QuestSO> prerequisiteQuests = new();

        [Header("Rewards")]
        [Tooltip("Everything the player receives when the quest is turned in.")]
        public List<QuestReward> rewards = new();

        [Tooltip("Phases in which the quest can be accepted. None = any phase.")]
        public DAY_PHASE offerPhases = AllPhases;

        [Tooltip("Phases in which the quest can be turned in. After the last one the quest expires. None = any phase.")]
        public DAY_PHASE turnInPhases = AllPhases;

        [Header("Steps (in order)")]
        [Tooltip("The quest moves to the next step when the current condition is met. After the last step it waits for the turn-in.")]
        public List<QuestStep> steps = new();

        [Header("Conversations (leave empty to use the NPC's regular conversation)")]
        [Tooltip("Giver, quest Available. Its answers use AcceptQuestActionSO (accept) or nothing (refuse).")]
        public ConversationSO offerConversation;

        [Tooltip("Giver, while the quest is Active.")]
        public ConversationSO inProgressConversation;

        [Tooltip("Turn-in NPC, quest ReadyToTurnIn. Its answer uses TurnInQuestActionSO.")]
        public ConversationSO turnInConversation;

        [Tooltip("Turn-in NPC, after the quest was completed.")]
        public ConversationSO completedConversation;

        [Tooltip("Turn-in NPC, after the quest expired.")]
        public ConversationSO expiredConversation;

        /// <summary>NPC that receives the turn-in (falls back to the giver).</summary>
        public int EffectiveTurnInNpcID => turnInNpcID < 0 ? giverNpcID : turnInNpcID;

        public bool AllowsOffer(DAY_PHASE phase) => AllowsPhase(offerPhases, phase);

        public bool AllowsTurnIn(DAY_PHASE phase) => AllowsPhase(turnInPhases, phase);

        /// <summary>True if the phase is later than the last phase of the turn-in window.</summary>
        public bool IsPastTurnInWindow(DAY_PHASE phase)
        {
            if (turnInPhases == DAY_PHASE.None) return false;

            int latest = 1;
            while ((latest << 1) <= (int)turnInPhases) latest <<= 1;
            return (int)phase > latest;
        }

        /// <summary>Checks that every step has a condition assigned.</summary>
        public bool HasValidSteps(out string error)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null || steps[i].condition == null)
                {
                    error = $"step {i} has no condition assigned.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool AllowsPhase(DAY_PHASE mask, DAY_PHASE phase) =>
            mask == DAY_PHASE.None || (mask & phase) != 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questID))
            {
                questID = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}