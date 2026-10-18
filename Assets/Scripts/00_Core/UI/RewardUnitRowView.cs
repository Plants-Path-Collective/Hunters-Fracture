using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>
    /// One ally row of the rewards screen: portrait, name and the horizontal list of the items the
    /// unit holds. Pure view: RewardsScreen decides what goes in it.
    /// </summary>
    public class RewardUnitRowView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private List<GameObject> items = new();

        public void Bind(Unit unit)
        {
            gameObject.SetActive(true);

            if (portrait != null)
            {
                portrait.sprite = unit.Portrait;
                portrait.enabled = unit.Portrait != null;
            }

            if (nameLabel != null) nameLabel.text = unit.Name;
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Shows exactly these items, in order, using the row's preconfigured item views.</summary>
        public void SetItems(IReadOnlyList<(ItemSO item, int quantity)> itemStacks)
        {
            for (int i = 0; i < items.Count; i++)
            {
                bool used = i < itemStacks.Count
                    && itemStacks[i].item != null
                    && itemStacks[i].quantity > 0;
                GameObject itemObject = items[i];
                itemObject.SetActive(used);
                if (!used) continue;

                if (itemObject.TryGetComponent(out ItemInInventoryView inventoryView))
                {
                    inventoryView.Bind(itemStacks[i].item, itemStacks[i].quantity);
                    continue;
                }

                if (itemObject.TryGetComponent(out RewardItemView rewardItemView))
                {
                    rewardItemView.Bind(itemStacks[i].item, itemStacks[i].quantity);
                    continue;
                }

                Debug.LogError($"Reward item GameObject '{itemObject.name}' is missing an ItemInInventoryView or RewardItemView.", itemObject);
            }
        }
    }
}