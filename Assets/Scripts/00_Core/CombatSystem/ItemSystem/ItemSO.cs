using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem.ItemSystem
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Item System/Item")]
    public class ItemSO : ScriptableObject
    {
        public int itemID; // Unique identifier
        public string itemName;
        public Sprite itemIcon;

        public List<ItemEffectSO> effects; // List of effects this item has
    }
}