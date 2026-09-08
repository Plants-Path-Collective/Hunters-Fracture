using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem.ItemSystem
{
    /// <summary>
    /// Central registry of every ItemSO in the game.
    /// </summary>
    /// <remarks>
    /// <para>Passive lookup only — this should never be consulted during combat
    /// action resolution. It exists to populate drop tables, UI, save/load, and
    /// to pre-initialize a UnitInventory with every valid item at quantity 0.</para>
    /// <para>There is exactly one asset of this type in the project. Anything that
    /// needs it (UnitInventory, drop tables, etc.) holds a direct serialized
    /// reference to that asset — no static Instance, no singleton pattern,
    /// following the same "no global state" approach as the rest of the combat
    /// system.</para>
    /// </remarks>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Item System/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [Header("----- All Items -----")]
        [SerializeField] private List<ItemSO> items = new List<ItemSO>();

        // Built once and cached from the list above, for O(1) lookups by ID.
        private Dictionary<int, ItemSO> _lookup;

        /// <summary>
        /// Read-only access to every item in the catalog. UnitInventory uses this
        /// to pre-populate its dictionary with every valid item at quantity 0.
        /// </summary>
        public IReadOnlyList<ItemSO> AllItems => items;

        /// <summary>
        /// Looks up an item by its itemID.
        /// </summary>
        /// <returns>The matching ItemSO, or null if no item has that ID.</returns>
        public ItemSO GetById(int itemID)
        {
            EnsureLookupBuilt();

            if (_lookup.TryGetValue(itemID, out ItemSO item))
                return item;

            Debug.LogWarning($"[ItemCatalog] No item found with ID {itemID}.");
            return null;
        }

        private void EnsureLookupBuilt()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<int, ItemSO>();
            foreach (ItemSO item in items)
            {
                if (item == null) continue;

                if (_lookup.ContainsKey(item.itemID))
                {
                    Debug.LogError($"[ItemCatalog] Duplicate itemID {item.itemID} between " +
                                    $"'{_lookup[item.itemID].name}' and '{item.name}'. " +
                                    "Every item must have a unique itemID.");
                    continue;
                }

                _lookup.Add(item.itemID, item);
            }
        }

#if UNITY_EDITOR
        // Rebuild the cached lookup whenever the list changes in the Inspector,
        // so a duplicate-ID mistake shows up right away instead of only at runtime.
        private void OnValidate()
        {
            _lookup = null;
        }
#endif
    }
}