using UnityEngine;
using CombatSystem.ItemSystem;
using CombatSystem.Unit;

public class ItemTest : MonoBehaviour
{
    public ItemSO testItem; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            Debug.Log($"[ItemTest] Player contact with item: {testItem.itemName} (ID: {testItem.itemID})");

            Unit playerUnit = other.GetComponent<Unit>();

            if (playerUnit == null)
            {
                Debug.LogWarning("[ItemTest] Player does not have a Unit component.");
                return;
            }

            UnitInventory playerInventory = playerUnit.inventory;

            if (playerInventory == null)
            {
                Debug.LogWarning("[ItemTest] Player does not have a UnitInventory component.");
                return;
            }

            playerInventory.AddItem(testItem, 1);
            
            Debug.Log($"[ItemTest] Player picked up item: {testItem.itemName} (ID: {testItem.itemID})");
            Destroy(gameObject);
        }
    }
}