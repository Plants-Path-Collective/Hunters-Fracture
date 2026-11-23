using UnityEngine;
using Core;

namespace Core.QuestsSystem
{
    /// <summary>Shows the field in the inspector only when the given enum field equals the given reward.</summary>
    public class ShowIfRewardAttribute : PropertyAttribute
    {
        public readonly string EnumField;
        public readonly QUEST_REWARD Value;

        public ShowIfRewardAttribute(string enumField, QUEST_REWARD value)
        {
            EnumField = enumField;
            Value = value;
        }
    }
}