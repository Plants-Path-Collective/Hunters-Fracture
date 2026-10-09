using System.Collections.Generic;
using UnityEngine;
using Core;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.Backpack
{
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

        public bool TryConsume(Unit actor, Unit target, ConsumableSO consumable, bool consumeOnlyIfValid = true)
        {
            if (actor == null || target == null || consumable == null)
            {
                Debug.LogWarning($"[{nameof(PartyBackpack)}] Cannot consume a null item or target.");
                return false;
            }

            if (target.Team != UNIT_TEAM.Ally)
            {
                Debug.LogWarning($"[{nameof(PartyBackpack)}] Consumable '{consumable.name}' can only be used on allies.");
                return false;
            }

            BackpackSlot slot = backpackSlots.Find(s => s.consumableSO == consumable);
            if (slot.consumableSO == null || slot.quantity <= 0)
            {
                Debug.LogWarning($"[{nameof(PartyBackpack)}] No '{consumable.name}' left in the backpack.");
                return false;
            }

            if (actor.Combat == null)
            {
                Debug.LogWarning($"[{nameof(PartyBackpack)}] Actor '{actor.Name}' is not in combat; cannot consume item.");
                return false;
            }

            switch (consumable.statType)
            {
                case STAT_TYPE.HP:
                    actor.Combat.Heal(actor, target, consumable.restoreAmount, $"consumable:{consumable.name}");
                    break;

                case STAT_TYPE.SP:
                    actor.Combat.RestoreSP(target, consumable.restoreAmount);
                    break;

                default:
                    Debug.LogWarning($"[{nameof(PartyBackpack)}] Consumable '{consumable.name}' targets {consumable.statType}, which is not implemented for ally recovery yet.");
                    return false;
            }

            if (consumeOnlyIfValid)
                RemoveConsumable(consumable, 1);

            return true;
        }
    }
}