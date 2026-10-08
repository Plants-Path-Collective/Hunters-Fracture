using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>One row of the turn-order list: portrait, team-colored frame, optional name.</summary>
    public class TimelineEntryView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Image frame;
        [Tooltip("Optional.")]
        [SerializeField] private TMP_Text nameLabel;
        [Tooltip("Optional: shown only on the unit that is acting right now.")]
        [SerializeField] private GameObject currentMarker;

        [Header("--- Look ---")]
        [SerializeField] private Color allyColor = new(0.25f, 0.6f, 1f);
        [SerializeField] private Color enemyColor = new(1f, 0.35f, 0.3f);
        [SerializeField] private float currentScale = 1.15f;

        public void Bind(Unit unit, bool isCurrent)
        {
            if (portrait != null)
            {
                portrait.sprite = unit.Portrait;
                portrait.enabled = unit.Portrait != null;
            }

            if (frame != null)
                frame.color = unit.Team == UNIT_TEAM.Ally ? allyColor : enemyColor;

            if (nameLabel != null)
                nameLabel.text = unit.Name;

            if (currentMarker != null)
                currentMarker.SetActive(isCurrent);

            transform.localScale = isCurrent ? Vector3.one * currentScale : Vector3.one;
        }
    }
}