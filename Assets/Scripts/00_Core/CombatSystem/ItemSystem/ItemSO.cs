using System.Collections.Generic;
using UnityEngine;
using Core.Shop;

namespace Core.CombatSystem.ItemSystem
{
    [CreateAssetMenu(menuName = "Item System/Item", fileName = "Item_")]
    public class ItemSO : ScriptableObject, IShopProduct
    {
        [Tooltip("If true, this item only appears in the reward pool after a challenge unlocks it.")]
        public bool lockedByDefault;
        [Space(10)]

        [Tooltip("Unique identifier for this item.")]
        public int itemID; // Unique identifier
        public string itemName;
        public Sprite itemIcon;
        [TextArea(2, 4)] public string itemDescription;

        public List<ItemEffectSO> effects; // List of effects this item has


        [Header("IShopProduct")]
        public string DisplayName => itemName;
        public string Description => itemDescription;
        public Sprite Icon => itemIcon;
    }
}