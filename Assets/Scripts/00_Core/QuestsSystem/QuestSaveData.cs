using System;
using System.Collections.Generic;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Progress of one objective, keyed by its stable objectiveID inside the quest.
    /// </summary>
    [Serializable]
    public class QuestObjectiveProgress
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
        // Keep the JSON field name "steps" so existing quest saves retain their shape.
        public List<QuestObjectiveProgress> steps = new();

        /// <summary>Current value of an objective (0 if it has no entry yet).</summary>
        public int GetValue(string objectiveID)
        {
            foreach (QuestObjectiveProgress step in steps)
            {
                if (step.id == objectiveID) return step.value;
            }

            return 0;
        }

        /// <summary>Sets the value of an objective, creating its entry if needed.</summary>
        public void SetValue(string objectiveID, int value)
        {
            foreach (QuestObjectiveProgress step in steps)
            {
                if (step.id != objectiveID) continue;
                step.value = value;
                return;
            }

            steps.Add(new QuestObjectiveProgress { id = objectiveID, value = value });
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