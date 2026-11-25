using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.Shop
{
    [Serializable]
    public struct PriceRange
    {
        [Min(0)] public int min;
        [Min(0)] public int max;

        public PriceRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public PriceRange Normalized() => max < min ? new PriceRange(min, min) : this;
    }

    [CreateAssetMenu(menuName = "Core/Shop/Catalog", fileName = "NewShopCatalog")]
    public class ShopCatalogSO : ScriptableObject
    {
        [SerializeField] private string shopName = "Shop";
        [Tooltip("Fixed entries: always sold (consumables, guaranteed items).")]
        [SerializeField] private List<ShopEntry> entries = new();

        [Header("Random item slots (re-rolled every clock phase)")]
        [Tooltip("Items are drawn from this catalog, only the ones currently unlocked.")]
        [SerializeField] private ItemCatalog itemPool;
        [SerializeField, Min(0)] private int randomItemSlots = 0;

        [Header("Random item prices (by ItemSO quality)")]
        [Tooltip("Prices are always multiples of this (100 = Chilean style).")]
        [SerializeField, Min(1)] private int priceStep = 100;
        [SerializeField] private PriceRange commonPrice = new(700, 3000);
        [SerializeField] private PriceRange rarePrice = new(3100, 7500);

        public string ShopName => shopName;
        public IReadOnlyList<ShopEntry> Entries => entries;
        public ItemCatalog ItemPool => itemPool;
        public int RandomItemSlots => randomItemSlots;
        public int PriceStep => priceStep;

        public PriceRange GetPriceRange(ITEM_QUALITY quality) => quality switch
        {
            ITEM_QUALITY.Rare => rarePrice,
            _ => commonPrice
        };

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var e in entries)
            {
                if (e.ProductAsset != null && e.Product == null)
                    Debug.LogWarning($"[{name}] '{e.ProductAsset.name}' doesn't implement IShopProduct.", this);
            }

            if (randomItemSlots > 0 && itemPool == null)
                Debug.LogWarning($"[{name}] Random slots are set but no Item Pool is assigned.", this);

            commonPrice = commonPrice.Normalized(); // max can't be below min
            rarePrice = rarePrice.Normalized();
        }
#endif
    }
}