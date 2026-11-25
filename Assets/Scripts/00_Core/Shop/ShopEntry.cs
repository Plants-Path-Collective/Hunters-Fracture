using System;
using UnityEngine;

namespace Core.Shop
{
    [Serializable]
    public class ShopEntry
    {
        [Tooltip("An ItemSO or ConsumableSO (must implement IShopProduct).")]
        [SerializeField] private ScriptableObject product;
        [SerializeField, Min(0)] private float price = 100f;
        [Tooltip("-1 = unlimited stock.")]
        [SerializeField] private int stock = -1;

        public ShopEntry() { }

        public ShopEntry(ScriptableObject product, float price, int stock)
        {
            this.product = product;
            this.price = price;
            this.stock = stock;
        }

        public IShopProduct Product => product as IShopProduct;
        public ScriptableObject ProductAsset => product;
        public float Price => price;
        public int InitialStock => stock;
    }
}