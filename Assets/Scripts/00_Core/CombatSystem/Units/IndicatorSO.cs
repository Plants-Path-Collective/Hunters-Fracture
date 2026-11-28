using UnityEngine;

namespace Core.CombatSystem.Units
{
    /// <summary>An icon shown on a unit's status row that is not a status (guard, form, timeline shift...).</summary>
    [CreateAssetMenu(fileName = "Indicator_", menuName = "Units/Indicator")]
    public class IndicatorSO : ScriptableObject
    {
        public string indicatorName;
        public Sprite icon;
        [Tooltip("Color applied to the icon. Use a darker gray for 'already used' markers.")]
        public Color tint = Color.white;
        [Tooltip("Removed when the bearer's next turn begins (used by timeline shifts).")]
        public bool expiresOnOwnTurn;

        [Tooltip("Only with 'Expires On Own Turn': keep the icon during the bearer's turn and remove it when that turn ends, instead of when it starts.")]
        public bool lastsThroughOwnTurn;
    }

    public readonly struct IndicatorEntry
    {
        public readonly IndicatorSO definition;
        public readonly int count; // 0 = no counter
        public IndicatorEntry(IndicatorSO definition, int count) { this.definition = definition; this.count = count; }
    }
}