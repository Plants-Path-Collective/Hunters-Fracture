using System.Collections.Generic;
using UnityEngine;
using Core;

public class PartyBackpack : MonoBehaviour
{
    public static PartyBackpack Instance { get; private set; }
    public List<BackpackSlot> backpackSlots = new List<BackpackSlot>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void AddConsumable(ConsumableSO consumable, int quantity = 1)
    {
        BackpackSlot existingSlot = backpackSlots.Find(slot => slot.consumableSO == consumable);
        if (existingSlot.consumableSO != null)
        {
            existingSlot.quantity += quantity;
        }
        else
        {
            backpackSlots.Add(new BackpackSlot { consumableSO = consumable, quantity = quantity });
        }
    }

    public void RemoveConsumable(ConsumableSO consumable, int quantity = 1)
    {
        BackpackSlot existingSlot = backpackSlots.Find(slot => slot.consumableSO == consumable);
        if (existingSlot.consumableSO != null)
        {
            existingSlot.quantity -= quantity;
            if (existingSlot.quantity <= 0)
            {
                backpackSlots.Remove(existingSlot);
            }
        }
    }
}