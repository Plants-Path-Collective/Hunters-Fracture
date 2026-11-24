using System;
using System.Collections.Generic;

namespace Core.Shop
{
    public class ShopService
    {
        private readonly Dictionary<ShopEntry, int> stock = new();

        public ShopCatalogSO Catalog { get; }
        public event Action<ShopEntry> Purchased;

        public ShopService(ShopCatalogSO catalog)
        {
            Catalog = catalog;
            foreach (var entry in catalog.Entries)
                stock[entry] = entry.InitialStock;
        }

        /// <summary>-1 means unlimited.</summary>
        public int GetStock(ShopEntry entry) => stock.TryGetValue(entry, out var s) ? s : 0;

        public bool CanAfford(ShopEntry entry) => GameManager.Instance.Pesos >= entry.Price;

        public PURCHASE_RESULT TryPurchase(ShopEntry entry)
        {
            if (entry == null || entry.Product == null || !stock.ContainsKey(entry))
                return PURCHASE_RESULT.InvalidEntry;

            if (stock[entry] == 0)
                return PURCHASE_RESULT.OutOfStock;

            if (!GameManager.Instance.TrySpendPesos(entry.Price))
                return PURCHASE_RESULT.NotEnoughPesos;

            if (stock[entry] > 0) stock[entry]--;

            Purchased?.Invoke(entry);
            return PURCHASE_RESULT.Success;
        }
    }
}