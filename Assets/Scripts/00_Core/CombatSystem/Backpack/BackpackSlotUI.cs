using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        public void SetConsumable(ConsumableSO value) => consumable = value;

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }
    }
}