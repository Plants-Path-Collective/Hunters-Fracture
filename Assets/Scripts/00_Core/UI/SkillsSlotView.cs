using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.SkillSystem;

namespace Core.UI
{
    public class SkillSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text costLabel;
        [Tooltip("Optional.")]
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedMarker;
        [Tooltip("Optional: dimmed when the unit cannot pay the cost.")]
        [SerializeField] private CanvasGroup group;
        [SerializeField, Range(0f, 1f)] private float unaffordableAlpha = 0.4f;

        public void Bind(ActionSO action, int cost, bool affordable)
        {
            nameLabel.text = action.actionName;
            costLabel.text = $"{cost} SP";

            if (icon != null)
            {
                icon.sprite = action.icon;
                icon.enabled = action.icon != null;
            }

            if (group != null) group.alpha = affordable ? 1f : unaffordableAlpha;
        }

        public void SetSelected(bool selected) => selectedMarker.SetActive(selected);
    }
}