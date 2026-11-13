using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Legacy inline quest-step data. New quests should use QuestObjectiveSO assets.
    /// </summary>
    [Obsolete("Use QuestObjectiveSO assets in QuestSO.objectives.")]
    [Serializable]
    public struct QuestStep
    {
        public string stepID;
        public LocalizedString text;
        public string milestoneID;
        [Min(1)] public int requiredCount;
        public int Target => Mathf.Max(1, requiredCount);
    }
}
