using System.Collections.Generic;
using UnityEngine;

namespace Core.CombatSystem.ItemSystem
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Item System/Item")]
    public class ItemSO : ScriptableObject
    {
        public int itemID; // Unique identifier
        public string itemName;
        public Sprite itemIcon;
        [TextArea(2, 4)] public string itemDescription;

        public List<ItemEffectSO> effects; // List of effects this item has
    }
}