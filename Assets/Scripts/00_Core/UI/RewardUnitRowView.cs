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
        [Tooltip("Parent of the item icons. Give it a HorizontalLayoutGroup.")]
        [SerializeField] private RectTransform itemsRoot;

        private readonly List<RewardItemView> icons = new();

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

        /// <summary>Shows exactly these items, in order. Icons are pooled.</summary>
        public void SetItems(IReadOnlyList<(ItemSO item, int quantity)> items, RewardItemView prefab)
        {
            while (icons.Count < items.Count)
                icons.Add(Instantiate(prefab, itemsRoot));

            for (int i = 0; i < icons.Count; i++)
            {
                bool used = i < items.Count;
                icons[i].gameObject.SetActive(used);
                if (used) icons[i].Bind(items[i].item, items[i].quantity);
            }
        }
    }
}