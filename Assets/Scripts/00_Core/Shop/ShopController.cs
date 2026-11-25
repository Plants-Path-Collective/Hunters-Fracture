using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Backpack;

namespace Core.Shop
{
    [RequireComponent(typeof(UIDocument))]
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset itemTemplate;

        private static readonly CultureInfo Clp = new("es-CL");

        private readonly Dictionary<ShopCatalogSO, ShopService> services = new();
        private readonly Dictionary<ShopEntry, VisualElement> tiles = new();

        private UIDocument document;
        private VisualElement root, itemsPanel, consumablesPanel;
        private ScrollView list, grid;
        private Label title, balance, status, detailName, detailDesc, detailInfo;
        private Button closeButton, detailBuy;
        private ShopService service;
        private ShopEntry selected; // selected consumable

        public bool IsOpen { get; private set; }

        /// <summary>Raised after an item/consumable was bought and delivered.</summary>
        public event Action<IShopProduct> ItemPurchased;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
            var r = document.rootVisualElement;

            root = r.Q<VisualElement>("shop-root");
            itemsPanel = r.Q<VisualElement>("shop-items-panel");
            consumablesPanel = r.Q<VisualElement>("shop-consumables-panel");
            list = r.Q<ScrollView>("shop-list");
            grid = r.Q<ScrollView>("shop-grid");
            title = r.Q<Label>("shop-title");
            balance = r.Q<Label>("shop-balance");
            status = r.Q<Label>("shop-status");
            detailName = r.Q<Label>("detail-name");
            detailDesc = r.Q<Label>("detail-desc");
            detailInfo = r.Q<Label>("detail-info");
            detailBuy = r.Q<Button>("detail-buy");
            closeButton = r.Q<Button>("shop-close");

            if (AnyNull(root, itemsPanel, consumablesPanel, list, grid, title, balance, status,
                        detailName, detailDesc, detailInfo, detailBuy, closeButton))
            {
                Debug.LogError($"[{nameof(ShopController)}] UI elements not found. Check that the UIDocument Source Asset is Shop.uxml and the element names match.", this);
                root = null; // makes Open() report "not initialized"
                return;
            }

            closeButton.clicked += Close;
            detailBuy.clicked += OnDetailBuyClicked;
            root.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.clicked -= Close;
            if (detailBuy != null) detailBuy.clicked -= OnDetailBuyClicked;
            Unsubscribe();
        }

        private static bool AnyNull(params object[] elements)
        {
            foreach (var e in elements)
                if (e == null) return true;
            return false;
        }

        // ── Open / close ──────────────────────────────────────────────────────

        public void Open(ShopCatalogSO catalog)
        {
            if (catalog == null) return;

            if (root == null)
            {
                Debug.LogError($"[{nameof(ShopController)}] Not initialized. Is this the scene instance, and is its GameObject active?", this);
                return;
            }

            Unsubscribe();

            int stamp = ShopService.CurrentClockStamp();
            if (!services.TryGetValue(catalog, out service) || service.ClockStamp != stamp)
            {
                service = new ShopService(catalog);
                services[catalog] = service;
            }

            GameManager.Instance.PesosChanged += OnPesosChanged;

            selected = null;
            title.text = catalog.ShopName;
            status.text = string.Empty;
            RefreshBalance();
            RebuildRows(resetScroll: true);

            root.style.display = DisplayStyle.Flex;
            IsOpen = true;
        }

        public void Close()
        {
            Unsubscribe();
            root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        /// <summary>Forces every shop to re-roll its items and stock (dev console).</summary>
        public void ResetStock()
        {
            ShopCatalogSO current = IsOpen && service != null ? service.Catalog : null;
            services.Clear();
            if (current != null) Open(current);
        }

        private void Unsubscribe()
        {
            if (GameManager.Instance != null) GameManager.Instance.PesosChanged -= OnPesosChanged;
        }

        // ── Building the two panels ───────────────────────────────────────────

        private void RebuildRows(bool resetScroll = false)
        {
            Vector2 listOffset = list.scrollOffset;
            Vector2 gridOffset = grid.scrollOffset;

            list.Clear();
            grid.Clear();
            tiles.Clear();

            foreach (var entry in service.Entries)
            {
                if (entry.Product == null)
                {
                    string what = entry.ProductAsset != null ? entry.ProductAsset.name : "an empty slot";
                    Debug.LogWarning($"[{nameof(ShopController)}] Skipped '{what}' in '{service.Catalog.name}': " +
                                     "it is missing or doesn't implement IShopProduct.", service.Catalog);
                    continue;
                }

                if (entry.ProductAsset is ConsumableSO) grid.Add(BuildTile(entry));
                else list.Add(BuildRow(entry));
            }

            // Don't show an empty panel (e.g. a shop that only sells items).
            itemsPanel.style.display = list.contentContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            consumablesPanel.style.display = grid.contentContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (selected != null && !tiles.ContainsKey(selected)) selected = null;
            ApplySelection();

            if (!resetScroll)
            {
                list.scrollOffset = listOffset;
                grid.scrollOffset = gridOffset;
            }
        }

        // Items: one row each, with its own buy button.
        private VisualElement BuildRow(ShopEntry entry)
        {
            var product = entry.Product;
            var row = itemTemplate.Instantiate();

            row.Q<Label>("item-name").text = product.DisplayName;
            row.Q<Label>("item-desc").text = product.Description;

            var icon = row.Q<VisualElement>("item-icon");
            if (product.Icon != null) icon.style.backgroundImage = new StyleBackground(product.Icon);

            int stock = service.GetStock(entry);
            row.Q<Label>("item-stock").text = StockText(stock);

            var buy = row.Q<Button>("item-buy");
            buy.text = FormatPesos(entry.Price);
            buy.SetEnabled(stock != 0 && service.CanAfford(entry));
            buy.clicked += () => OnBuyClicked(entry);

            return row;
        }

        // Consumables: square tile, click to select (details + buy button show below the grid).
        private VisualElement BuildTile(ShopEntry entry)
        {
            var product = entry.Product;
            int stock = service.GetStock(entry);

            var tile = new VisualElement();
            tile.AddToClassList("shop-tile");

            var icon = new VisualElement();
            icon.AddToClassList("shop-tile__icon");
            if (product.Icon != null) icon.style.backgroundImage = new StyleBackground(product.Icon);
            tile.Add(icon);

            var badge = new Label(stock < 0 ? string.Empty : stock == 0 ? "Sold out" : $"x{stock}");
            badge.AddToClassList("shop-tile__badge");
            tile.Add(badge);

            if (stock == 0) tile.AddToClassList("shop-tile--soldout");
            else if (!service.CanAfford(entry)) tile.AddToClassList("shop-tile--unaffordable");

            tile.RegisterCallback<ClickEvent>(_ => SelectConsumable(entry));
            tiles[entry] = tile;
            return tile;
        }

        private void SelectConsumable(ShopEntry entry)
        {
            selected = entry;
            ApplySelection();
        }

        private void ApplySelection()
        {
            foreach (var pair in tiles)
                pair.Value.EnableInClassList("shop-tile--selected", pair.Key == selected);

            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (selected == null)
            {
                detailName.text = "Select a consumable";
                detailDesc.text = string.Empty;
                detailInfo.text = string.Empty;
                detailBuy.style.display = DisplayStyle.None;
                return;
            }

            var product = selected.Product;
            int stock = service.GetStock(selected);

            detailName.text = product.DisplayName;
            detailDesc.text = product.Description;
            detailInfo.text = StockText(stock);

            detailBuy.style.display = DisplayStyle.Flex;
            detailBuy.text = "Buy · " + FormatPesos(selected.Price);
            detailBuy.SetEnabled(stock != 0 && service.CanAfford(selected));
        }

        private static string StockText(int stock) =>
            stock < 0 ? string.Empty : stock == 0 ? "Sold out" : $"Stock: {stock}";

        // ── Buying ────────────────────────────────────────────────────────────

        private void OnDetailBuyClicked()
        {
            if (selected != null) OnBuyClicked(selected);
        }

        private void OnBuyClicked(ShopEntry entry)
        {
            // Items go through the assignment UI: don't take the money if it can't open.
            if (entry.Product is ItemSO)
            {
                var assignation = ItemAssignationController.Instance;
                if (assignation == null || assignation.IsOpen)
                {
                    status.text = "Can't buy right now.";
                    return;
                }
            }

            var s = service;
            PURCHASE_RESULT result = s.TryPurchase(entry);

            if (result != PURCHASE_RESULT.Success)
            {
                status.text = result switch
                {
                    PURCHASE_RESULT.NotEnoughPesos => "Not enough pesos.",
                    PURCHASE_RESULT.OutOfStock => "Out of stock.",
                    _ => "Something went wrong."
                };
                RebuildRows();
                return;
            }

            if (!Deliver(s, entry))
            {
                s.Refund(entry);
                status.text = "Couldn't deliver the item. You were refunded.";
                RebuildRows();
                return;
            }

            status.text = $"Bought {entry.Product.DisplayName}!";
            ItemPurchased?.Invoke(entry.Product);
            RebuildRows();
        }

        private bool Deliver(ShopService s, ShopEntry entry)
        {
            switch (entry.Product)
            {
                case ItemSO item:
                    bool opened = ItemAssignationController.TryOpen(
                        new[] { item },
                        leftover =>
                        {
                            // Only possible with allowLeaveItems on: the player left without taking it.
                            if (leftover.Count > 0) s.Refund(entry);
                        });

                    if (opened) Close(); // the assignment UI pauses the clock and switches the input map
                    return opened;

                case ConsumableSO consumable:
                    if (PartyBackpack.Instance == null) return false; // triggers the refund
                    PartyBackpack.Instance.AddConsumable(consumable, 1);
                    return true;

                default:
                    return false;
            }
        }

        // ── Balance ───────────────────────────────────────────────────────────

        private void OnPesosChanged(float _) => RefreshBalance();

        private void RefreshBalance() => balance.text = FormatPesos(GameManager.Instance.Pesos);

        private static string FormatPesos(float amount) => "$ " + amount.ToString("N0", Clp);
    }
}