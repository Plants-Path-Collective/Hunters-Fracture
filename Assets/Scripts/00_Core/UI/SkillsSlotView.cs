using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.SkillSystem;
using UnityEngine.Localization;

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

        private LocalizedString localizedActionName;

        private void OnEnable()
        {
            if (localizedActionName != null)
                localizedActionName.StringChanged += UpdateName;
        }

        private void OnDisable()
        {
            if (localizedActionName != null)
                localizedActionName.StringChanged -= UpdateName;
        }

        public void Bind(ActionSO action, int cost, bool affordable)
        {
            if (localizedActionName != null)
                localizedActionName.StringChanged -= UpdateName;

            localizedActionName = action.localizedActionName;
            if (localizedActionName != null)
            {
                if (isActiveAndEnabled)
                    localizedActionName.StringChanged += UpdateName;
            }
            else
            {
                nameLabel.text = action.actionName;
            }

            costLabel.text = $"{cost} SP";

            if (icon != null)
            {
                icon.sprite = action.icon;
                icon.enabled = action.icon != null;
            }

            if (group != null) group.alpha = affordable ? 1f : unaffordableAlpha;
        }

        private void UpdateName(string value)
        {
            if (nameLabel != null)
                nameLabel.text = value;
        }

        public void SetSelected(bool selected) => selectedMarker.SetActive(selected);
    }
}