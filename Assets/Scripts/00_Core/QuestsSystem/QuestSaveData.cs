using System;
using System.Collections.Generic;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Progress of one step, keyed by its stable stepID inside the quest.
    /// </summary>
    [Serializable]
    public class QuestStepProgress
    {
        public string id;
        public int value;
    }

    /// <summary>
    /// Stored state of one quest, keyed by its stable questID.
    /// Only Active, ReadyToTurnIn, Completed and Expired are stored.
    /// </summary>
    [Serializable]
    public class QuestProgress
    {
        public string id;
        public QUEST_STATE state;
        public List<QuestStepProgress> steps = new();

        /// <summary>Current value of a step (0 if it has no entry yet).</summary>
        public int GetValue(string stepID)
        {
            foreach (QuestStepProgress step in steps)
            {
                if (step.id == stepID) return step.value;
            }

            return 0;
        }

        /// <summary>Sets the value of a step, creating its entry if needed.</summary>
        public void SetValue(string stepID, int value)
        {
            foreach (QuestStepProgress step in steps)
            {
                if (step.id != stepID) continue;
                step.value = value;
                return;
            }

            steps.Add(new QuestStepProgress { id = stepID, value = value });
        }
    }

    /// <summary>
    /// Root object serialized to disk.
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        /// <summary>Save format version, for future migrations.</summary>
        public int version = 1;
        public List<QuestProgress> quests = new();
    }
}