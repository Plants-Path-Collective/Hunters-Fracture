using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>
    /// The ONLY place where the assignment UI touches PartyMemberData.inventorySnapshot
    /// (an ItemSnapshot: itemID -> quantity). If the snapshot format changes, edit this file and nothing else.
    /// </summary>
    public static class PartyItems
    {
        /// <summary>Item stacks a party member holds out of combat.</summary>
        public static List<(ItemSO item, int quantity)> GetStacks(PartyMemberData member, ItemCatalog catalog)
        {
            var stacks = new List<(ItemSO item, int quantity)>();
            if (member?.inventorySnapshot == null) return stacks;

            if (catalog == null)
            {
                Debug.LogError("[Items] PartyItems.GetStacks needs an ItemCatalog to resolve item IDs.");
                return stacks;
            }

            foreach (KeyValuePair<int, int> entry in member.inventorySnapshot)
            {
                if (entry.Value <= 0) continue;

                ItemSO item = catalog.GetById(entry.Key);
                if (item != null) stacks.Add((item, entry.Value));
            }

            return stacks;
        }

        /// <summary>Adds one copy of an item to a party member's snapshot.</summary>
        public static void Add(PartyMemberData member, ItemSO item)
        {
            if (member == null || item == null) return;

            member.inventorySnapshot ??= new ItemSnapshot();
            member.inventorySnapshot.TryGetValue(item.itemID, out int current);
            member.inventorySnapshot[item.itemID] = current + 1;
        }
    }
}