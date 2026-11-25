using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Clock;
using Core.CombatSystem.ItemSystem;

namespace Core.Shop
{
    public enum PURCHASE_RESULT { Success, NotEnoughPesos, OutOfStock, InvalidEntry }

    public class ShopService
    {
        private readonly List<ShopEntry> entries = new();
        private readonly Dictionary<ShopEntry, int> stock = new();

        public ShopCatalogSO Catalog { get; }

        /// <summary>Fixed catalog entries + the random items rolled for this clock phase.</summary>
        public IReadOnlyList<ShopEntry> Entries => entries;

        /// <summary>Day/phase this service was created in. Compared on Open to know if it is outdated.</summary>
        public int ClockStamp { get; }

        public event Action<ShopEntry> Purchased;

        public ShopService(ShopCatalogSO catalog)
        {
            Catalog = catalog;
            ClockStamp = CurrentClockStamp();

            entries.AddRange(catalog.Entries);
            entries.AddRange(RollRandomEntries(catalog));

            foreach (var entry in entries)
                stock[entry] = entry.InitialStock;
        }

        /// <summary>Changes whenever the day or the phase changes. 0 when there is no clock (test scenes).</summary>
        public static int CurrentClockStamp()
        {
            GameClock clock = GameClock.Instance;
            return clock == null ? 0 : clock.Day * 100 + (int)clock.CurrentPhase;
        }

        private static List<ShopEntry> RollRandomEntries(ShopCatalogSO catalog)
        {
            var result = new List<ShopEntry>();
            if (catalog.RandomItemSlots <= 0 || catalog.ItemPool == null) return result;

            // Don't offer an item twice: skip the ones the shop already sells as fixed entries.
            var alreadySold = new HashSet<ItemSO>();
            foreach (var e in catalog.Entries)
                if (e.ProductAsset is ItemSO fixedItem) alreadySold.Add(fixedItem);

            // Only unlocked items (lockedByDefault + challenge unlocks), via ItemCatalog.IsAvailable.
            var candidates = new List<ItemSO>();
            foreach (ItemSO item in catalog.ItemPool.AllItems)
                if (catalog.ItemPool.IsAvailable(item) && !alreadySold.Contains(item))
                    candidates.Add(item);

            int count = Mathf.Min(catalog.RandomItemSlots, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count); // max is exclusive
                ItemSO chosen = candidates[index];

                result.Add(new ShopEntry(chosen, RollPrice(catalog, chosen.quality), 1));
                candidates.RemoveAt(index); // no duplicates within the roll
            }

            return result;
        }

        /// <summary>Random price inside the quality's range, always a multiple of the catalog's price step.</summary>
        private static float RollPrice(ShopCatalogSO catalog, ITEM_QUALITY quality)
        {
            PriceRange range = catalog.GetPriceRange(quality);
            int step = Mathf.Max(1, catalog.PriceStep);

            int minSteps = Mathf.CeilToInt(range.min / (float)step);
            int maxSteps = Mathf.Max(minSteps, Mathf.FloorToInt(range.max / (float)step));

            // Working in "steps" guarantees round prices and includes both ends (Range's int max is exclusive).
            return UnityEngine.Random.Range(minSteps, maxSteps + 1) * step;
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

        public void Refund(ShopEntry entry)
        {
            if (entry == null || !stock.ContainsKey(entry)) return;

            if (stock[entry] >= 0) stock[entry]++; // -1 = unlimited, leave it alone
            GameManager.Instance.AddBalance(entry.Price);
        }
    }
}