using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Core.ChallengesSystem;
using PlantsPathCo.DialogueSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Data definition of a quest: identity, NPCs, requirements, steps, time windows and rewards.
    /// Progress is NOT stored here; it lives in QuestManager and the save file.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestSO", menuName = "Quests/QuestSO")]
    public class QuestSO : ScriptableObject
    {
        private const DAY_PHASE AllPhases = DAY_PHASE.Morning | DAY_PHASE.Afternoon | DAY_PHASE.Night;

        [Header("Identity")]
        [Tooltip("Stable unique key used in the save file. Never change it after release.")]
        public string questID;

        [Tooltip("Localized display name.")]
        public LocalizedString questName;

        [Tooltip("Localized description shown in the quest log.")]
        public LocalizedString description;

        [Header("NPCs")]
        [Tooltip("npcID (ConversationTrigger) of the NPC that offers the quest.")]
        public int giverNpcID;

        [Tooltip("npcID (ConversationTrigger) of the NPC that receives the turn-in. -1 = the giver.")]
        public int turnInNpcID = -1;

        [Header("Requirements")]
        [Tooltip("Quests that must be completed (this run) before this one becomes available.")]
        public List<QuestSO> prerequisiteQuests = new();

        [Tooltip("Unlock keys (see UnlockKeys) that must be unlocked by challenges.")]
        public List<string> prerequisiteUnlockKeys = new();

        [Header("Objectives")]
        [Tooltip("Objective assets in display order. All must be completed.")]
        public List<QuestObjectiveSO> objectives = new();

        [Tooltip("If true, completes as soon as every objective is done, without a turn-in.")]
        public bool autoComplete;

        [Header("Time windows")]
        [Tooltip("Phases in which the quest can be accepted. None = any phase.")]
        public DAY_PHASE offerPhases = AllPhases;

        [Tooltip("Phases in which the quest can be turned in. The quest expires once the day moves past the last one. None = any phase.")]
        public DAY_PHASE completePhases = AllPhases;

        [Header("Rewards")]
        [Tooltip("Rewards granted once, on completion. Same type used by challenges.")]
        public List<ChallengeRewardSO> rewards = new();

        [Header("Consequences")]
        [Tooltip("Executed when the quest expires.")]
        public List<QuestConsequenceSO> expirationConsequences = new();

        [Header("Conversations")]
        [Tooltip("Giver, Available inside the offer window. Its answers should use AcceptQuestActionSO.")]
        public ConversationSO offerConversation;

        [Tooltip("Giver, while Active (or done but not yet deliverable).")]
        public ConversationSO reminderConversation;

        [Tooltip("Turn-in NPC, ReadyToTurnIn inside the turn-in window. Its answer should use TurnInQuestActionSO.")]
        public ConversationSO turnInConversation;

        [Tooltip("Turn-in NPC, after the quest expired.")]
        public ConversationSO expiredConversation;

        [Tooltip("Giver, Available but outside the offer window ('I no longer hand this out').")]
        public ConversationSO offerClosedConversation;

        [Tooltip("Turn-in NPC, after the quest was completed.")]
        public ConversationSO completedConversation;

        /// <summary>NPC that receives the turn-in (falls back to the giver).</summary>
        public int EffectiveTurnInNpcID => turnInNpcID < 0 ? giverNpcID : turnInNpcID;

        /// <summary>True if the quest can be accepted during the given single phase.</summary>
        public bool AllowsOffer(DAY_PHASE phase) => AllowsPhase(offerPhases, phase);

        /// <summary>True if the quest can be turned in during the given single phase.</summary>
        public bool AllowsTurnIn(DAY_PHASE phase) => AllowsPhase(completePhases, phase);

        /// <summary>
        /// True if the given phase is later than the last phase of the turn-in window,
        /// meaning an accepted quest can no longer be delivered.
        /// </summary>
        public bool IsPastTurnInWindow(DAY_PHASE phase)
        {
            if (completePhases == DAY_PHASE.None) return false;

            int latest = 1;
            while ((latest << 1) <= (int)completePhases) latest <<= 1;
            return (int)phase > latest;
        }

        /// <summary>Checks that every objective is assigned, valid and uniquely identified in this quest.</summary>
        public bool HasValidObjectives(out string error)
        {
            var seen = new HashSet<string>();
            if (objectives == null)
            {
                error = "the objectives list is null.";
                return false;
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                QuestObjectiveSO objective = objectives[i];
                if (objective == null)
                {
                    error = $"objective {i} is not assigned.";
                    return false;
                }

                if (!objective.Validate(out string objectiveError))
                {
                    error = $"objective {i} {objectiveError}";
                    return false;
                }

                if (!seen.Add(objective.ObjectiveID))
                {
                    error = $"objectiveID '{objective.ObjectiveID}' is duplicated.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>Returns the index of the first milestone objective with this ID, or -1 if none matches.</summary>
        public int FindObjectiveIndex(string milestoneID)
        {
            if (string.IsNullOrEmpty(milestoneID) || objectives == null) return -1;

            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i] is MilestoneObjectiveSO objective &&
                    objective.MilestoneID == milestoneID)
                    return i;
            }

            return -1;
        }

        private static bool AllowsPhase(DAY_PHASE mask, DAY_PHASE phase) =>
            mask == DAY_PHASE.None || (mask & phase) != 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            bool changed = false;

            // Auto-fill the ID with the asset name the first time; later renames do not change it.
            if (string.IsNullOrEmpty(questID))
            {
                questID = name;
                changed = true;
            }

            if (changed) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}