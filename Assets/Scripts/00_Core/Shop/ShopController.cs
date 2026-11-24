using System;
using System.Globalization;
using System.Collections.Generic;
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
        private readonly Dictionary<ShopCatalogSO, ShopService> services = new();
        [SerializeField] private VisualTreeAsset itemTemplate;
        private static readonly CultureInfo Clp = new("es-CL");

        private UIDocument document;
        private VisualElement root;
        private ScrollView list;
        private Label title, balance, status;
        private Button closeButton;
        private ShopService service;

        public bool IsOpen { get; private set; }

        /// <summary>Hook your future inventory here.</summary>
        public event Action<IShopProduct> ItemPurchased;

        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
            var r = document.rootVisualElement;

            root = r.Q<VisualElement>("shop-root");
            list = r.Q<ScrollView>("shop-list");
            title = r.Q<Label>("shop-title");
            balance = r.Q<Label>("shop-balance");
            status = r.Q<Label>("shop-status");
            closeButton = r.Q<Button>("shop-close");

            if (root == null || list == null || title == null ||
                balance == null || status == null || closeButton == null)
            {
                Debug.LogError($"[{nameof(ShopController)}] UI elements not found. Check that the UIDocument Source Asset is Shop.uxml and the element names match.", this);
                return;
            }

            closeButton.clicked += Close;
            root.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.clicked -= Close;
            Unsubscribe();
        }

        public void Open(ShopCatalogSO catalog)
        {
            if (catalog == null) return;

            if (root == null)
            {
                Debug.LogError($"[{nameof(ShopController)}] Not initialized. Is this the scene instance, and is its GameObject active?", this);
                return;
            }

            Unsubscribe();

            if (!services.TryGetValue(catalog, out service))
            {
                service = new ShopService(catalog);
                services[catalog] = service;
            }

            GameManager.Instance.PesosChanged += OnPesosChanged;

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

        private void Unsubscribe()
        {
            if (GameManager.Instance != null) GameManager.Instance.PesosChanged -= OnPesosChanged;
        }

        private void RebuildRows(bool resetScroll = false)
        {
            var offset = list.scrollOffset;
            list.Clear();

            foreach (var entry in service.Catalog.Entries)
            {
                var product = entry.Product;
                if (product == null) continue;

                var row = itemTemplate.Instantiate();
                row.Q<Label>("item-name").text = product.DisplayName;
                row.Q<Label>("item-desc").text = product.Description;

                var icon = row.Q<VisualElement>("item-icon");
                if (product.Icon != null) icon.style.backgroundImage = new StyleBackground(product.Icon);

                int stock = service.GetStock(entry);
                var stockLabel = row.Q<Label>("item-stock");
                stockLabel.text = stock < 0 ? string.Empty : stock == 0 ? "Sold out" : $"Stock: {stock}";

                var buy = row.Q<Button>("item-buy");
                buy.text = FormatPesos(entry.Price);
                buy.SetEnabled(stock != 0 && service.CanAfford(entry));
                buy.clicked += () => OnBuyClicked(entry);

                list.Add(row);
            }

            if (!resetScroll) list.scrollOffset = offset;
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

        private void OnPesosChanged(float _) => RefreshBalance();

        private void RefreshBalance() => balance.text = FormatPesos(GameManager.Instance.Pesos);

        private static string FormatPesos(float amount) => "$ " + amount.ToString("N0", Clp);
    
        public void ResetStock()
        {
            ShopCatalogSO current = IsOpen && service != null ? service.Catalog : null;
            services.Clear();
            if (current != null) Open(current);
        }
    }
}