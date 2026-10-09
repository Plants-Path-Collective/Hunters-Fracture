using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.ItemSystem;

namespace Core.UI
{
    /// <summary>
    /// One item icon of the rewards screen. The same prefab is used for the unassigned list
    /// (selectable) and for the icons inside each unit row (with a stack count).
    /// </summary>
    public class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [Tooltip("Optional: shows 'xN' when the quantity is greater than 1.")]
        [SerializeField] private TMP_Text quantityLabel;
        [Tooltip("Optional: shown only on the highlighted item.")]
        [SerializeField] private GameObject selectedMarker;

        public ItemSO Item { get; private set; }

        public void Bind(ItemSO item, int quantity = 1)
        {
            Item = item;

            icon.sprite = item.itemIcon;
            icon.enabled = item.itemIcon != null;
            icon.preserveAspect = true;

            if (quantityLabel != null)
            {
                quantityLabel.gameObject.SetActive(quantity > 1);
                quantityLabel.text = $"x{quantity}";
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedMarker != null) selectedMarker.SetActive(selected);
        }
    }
}