using System.Collections.Generic;
using UnityEngine;

namespace Core.Shop
{
    [CreateAssetMenu(menuName = "Shop/Catalog", fileName = "ShopCatalog_")]
    public class ShopCatalogSO : ScriptableObject
    {
        [SerializeField] private string shopName = "Shop";
        [SerializeField] private List<ShopEntry> entries = new();

        public string ShopName => shopName;
        public IReadOnlyList<ShopEntry> Entries => entries;

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var e in entries)
            {
                if (e.ProductAsset != null && e.Product == null)
                    Debug.LogWarning($"[{name}] '{e.ProductAsset.name}' doesn't implement IShopProduct.", this);
            }
        }
#endif
    }
}