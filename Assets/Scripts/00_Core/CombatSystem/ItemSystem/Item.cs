using UnityEngine;

namespace CombatSystem.ItemSystem
{
    [Tooltip("This class is used only by the inventory (Dictionary) in UnitInventory.cs; it allows the inventory to add the required item type to the `int quantity`")]
    public class Item
    {
        public ItemSO itemSO;
        
        [Tooltip("The quantity of the item currently held by a Unit; it always starts at 0")]
        public int quantity;
    }
}
