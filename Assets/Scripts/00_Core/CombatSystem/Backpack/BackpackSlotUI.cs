using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

namespace Core.CombatSystem.Backpack
{
    public class BackpackSlotUI : MonoBehaviour
    {
        public Image consumableIcon;
        public TextMeshProUGUI consumableNameText;
        public TextMeshProUGUI quantityText;
        public TextMeshProUGUI effectText;
        public GameObject selectionHighlight;
        public ConsumableSO consumable { get; private set; }

        private LocalizedString localizedName;
        private LocalizedString localizedEffect;

        private void OnEnable()
        {
            SubscribeLocalizedText();
        }

        private void OnDisable()
        {
            UnsubscribeLocalizedText();
        }

        public void SetConsumable(ConsumableSO value)
        {
            UnsubscribeLocalizedText();
            localizedName = null;
            localizedEffect = null;
            consumable = value;

            if (value == null)
                return;

            localizedName = value.localizedItemName;
            localizedEffect = value.localizedEffect;

            if (consumableNameText != null)
                consumableNameText.text = value.itemName;
            if (effectText != null)
                effectText.text = value.effect;

            SubscribeLocalizedText();
        }

        private void SubscribeLocalizedText()
        {
            if (!isActiveAndEnabled || consumable == null)
                return;

            if (localizedName != null)
                localizedName.StringChanged += UpdateName;
            if (localizedEffect != null)
                localizedEffect.StringChanged += UpdateEffect;
        }

        private void UnsubscribeLocalizedText()
        {
            if (localizedName != null)
                localizedName.StringChanged -= UpdateName;
            if (localizedEffect != null)
                localizedEffect.StringChanged -= UpdateEffect;
        }

        private void UpdateName(string value)
        {
            if (consumableNameText != null)
                consumableNameText.text = value;
        }

        private void UpdateEffect(string value)
        {
            if (effectText != null)
                effectText.text = value;
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }
    }
}