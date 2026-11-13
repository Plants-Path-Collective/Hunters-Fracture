using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Reusable objective definition. Runtime progress belongs to QuestManager, never to this asset.
    /// </summary>
    public abstract class QuestObjectiveSO : ScriptableObject
    {
        [Header("Objective")]
        [Tooltip("Stable identifier used to save this objective's progress inside its quest.")]
        [SerializeField] private string objectiveID;

        [Tooltip("Localized objective text. Smart String variables may be supplied by the UI.")]
        [SerializeField] private LocalizedString text;

        public string ObjectiveID => objectiveID;
        public LocalizedString Text => text;
        public abstract int Target { get; }

        /// <summary>Starts listening for objective progress and returns a delegate that stops listening.</summary>
        public abstract Action Subscribe(Action<int> addProgress);

        /// <summary>Validates shared objective data before a quest is accepted.</summary>
        public virtual bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(objectiveID))
            {
                error = "has no objectiveID.";
                return false;
            }

            if (Target < 1)
            {
                error = $"'{objectiveID}' has a target below 1.";
                return false;
            }

            error = null;
            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!string.IsNullOrEmpty(objectiveID)) return;

            objectiveID = name;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
