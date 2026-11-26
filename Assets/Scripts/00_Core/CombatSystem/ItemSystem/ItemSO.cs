using System.Collections.Generic;
using UnityEngine;
using Core.Shop;
using UnityEngine.Localization;

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
        public ITEM_QUALITY quality = ITEM_QUALITY.Common;
        public LocalizedString localizedQuality;
        public Sprite itemIcon;
        public string itemName;
        public LocalizedString localizedName;
        public string itemDescription;
        public LocalizedString localizedDescription;

        public List<ItemEffectSO> effects; // List of effects this item has


        [Header("IShopProduct")]
        public string DisplayName => 
                    localizedName.IsEmpty ? itemName : localizedName.GetLocalizedString();
        public string Description => 
                    localizedDescription.IsEmpty ? itemDescription : localizedDescription.GetLocalizedString();
        public Sprite Icon => itemIcon;
    }
}