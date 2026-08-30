using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CombatSystem.ItemSystem;

namespace CombatSystem.Unit
{
    /// <summary>
    /// Manages the inventory of a unit, allowing addition, removal, and query of item quantities.
    /// </summary>
    /// <remarks>
    /// <para>The inventory is stored as a dictionary mapping <see cref="ItemSO.itemID"/> to <see cref="Item"/> instances.</para>
    /// <para>All possible items are pre‑initialized with a quantity of 0, so the dictionary keys always exist 
    /// for any valid <see cref="ItemSO"/>.</para>
    /// <para>Changes to item quantities trigger the <see cref="OnStackChanged"/> event, which can be used 
    /// to update UI or other reactive systems.</para>
    /// </remarks>
    public class UnitInventory : MonoBehaviour
    {
        [Header("----- Inventory -----")]
        [SerializeField] private Dictionary<int, Item> inventory = new Dictionary<int, Item>();

        /// <summary>
        /// Event invoked whenever an item's quantity changes (via <see cref="AddItem"/> or <see cref="RemoveItem"/>).
        /// </summary>
        [Tooltip("Triggered when an item quantity changes (added or removed).")]
        public UnityEvent OnStackChanged { get; private set; } = new UnityEvent();

        /// <summary>
        /// Adds a specified quantity of an item to the unit's inventory.
        /// </summary>
        /// <remarks>
        /// <para>This method searches for the <see cref="ItemSO.itemID"/> in the internal <c>inventory</c> dictionary.</para>
        /// <para>Given the inventory design, all game items are pre‑initialized with a base quantity of 0. 
        /// Therefore, the key is expected to exist for any valid <paramref name="itemSO"/>.</para>
        /// <para>If the ID exists, it increments the quantity by the specified amount and notifies listeners 
        /// via <see cref="OnStackChanged"/>.</para>
        /// <para>If the ID does not exist (edge case), it logs a warning to the console.</para>
        /// <para><b>Behavior to note:</b> 
        /// This method does not clamp to a maximum value. The inventory quantity can grow indefinitely 
        /// based on game events, rewards, or purchases.</para>
        /// </remarks>
        /// <param name="itemSO">
        /// ScriptableObject defining the item to add. Its <c>itemID</c> is used as the lookup key.
        /// </param>
        /// <param name="quantity">
        /// Positive number of units to add to the inventory. 
        /// The default value is 1. Values less than or equal to 0 are ignored.
        /// </param>
        public void AddItem(ItemSO itemSO, int quantity = 1)
        {
            if (itemSO == null) { Debug.LogError($"[UnitInventory] {gameObject.name} is null, can not be added to inventory"); return; }
            if (quantity <= 0) return;

            int itemID = itemSO.itemID;

            if (inventory.ContainsKey(itemID))
            {
                inventory[itemID].quantity += quantity;
                NotifyListeners();
            }
            else
            {
                Debug.LogWarning($"[UnitInventory] {gameObject.name} does not have {itemSO.name} " +
                                 "in its inventory. Don't forget to add it to the ItemCatalogue first!");
            }
        }

        /// <summary>
        /// Reduces the quantity of a specific item in the unit's inventory.
        /// </summary>
        /// <remarks>
        /// <para>This method searches for the <see cref="ItemSO.itemID"/> in the internal <c>inventory</c> dictionary.</para>
        /// <para>Given the inventory design, all game items are pre‑initialized with a base quantity of 0. 
        /// Therefore, the key is expected to exist for any valid <paramref name="itemSO"/>.</para>
        /// <para>If the ID exists, it subtracts the specified quantity from the entry and notifies listeners 
        /// via <see cref="OnStackChanged"/>.</para>
        /// <para>If the ID does not exist (edge case), it logs a warning to the console.</para>
        /// <para><b>Behavior to note:</b> 
        /// This method clamps the result using <see cref="Mathf.Max"/> to ensure the quantity 
        /// never drops below 0. If the subtraction exceeds the current stock, the item's quantity 
        /// will be set to exactly 0.</para>
        /// </remarks>
        /// <param name="itemSO">
        /// ScriptableObject defining the item to remove. Its <c>itemID</c> is used as the lookup key.
        /// </param>
        /// <param name="quantity">
        /// Positive number of units to subtract from the inventory. 
        /// The default value is 1. Values less than or equal to 0 are ignored.
        /// </param>
        private void RemoveItem(ItemSO itemSO, int quantity = 1)
        {
            if (itemSO == null) { Debug.LogError($"[UnitInventory] {gameObject.name} is null, can not be removed from inventory"); return; }
            if (quantity <= 0) return;

            int itemID = itemSO.itemID;

            if (inventory.ContainsKey(itemID))
            {
                // Clamp to zero to prevent negative inventory values
                inventory[itemID].quantity = Mathf.Max(0, inventory[itemID].quantity - quantity);
                NotifyListeners();
            }
            else
            {
                Debug.LogWarning($"[UnitInventory] {gameObject.name} does not have {itemSO.name} " +
                                 "in its inventory. Don't forget to add it to the ItemCatalogue first!");
            }
        }

        /// <summary>
        /// Gets the current quantity of a specific item in the inventory.
        /// </summary>
        /// <param name="itemSO">The item to query. Must not be null.</param>
        /// <returns>
        /// The quantity of the item if it exists in the inventory; otherwise, 0.
        /// </returns>
        public int GetItemQuantity(ItemSO itemSO)
        {
            if (itemSO == null)
            {
                Debug.LogError($"[UnitInventory] {gameObject.name} is null");
                return 0;
            }

            int itemID = itemSO.itemID;

            // Use TryGetValue for a single dictionary lookup
            if (inventory.TryGetValue(itemID, out Item item))
            {
                return item.quantity;
            }

            return 0;
        }

        /// <summary>
        /// Invokes the <see cref="OnStackChanged"/> event to notify any registered listeners.
        /// </summary>
        private void NotifyListeners()
        {
            OnStackChanged.Invoke();
        }
    }
}