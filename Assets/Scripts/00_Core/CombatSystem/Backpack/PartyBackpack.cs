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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void AddConsumable(ConsumableSO consumable, int quantity = 1)
        {
            if (consumable == null || quantity <= 0)
            {
                Debug.LogWarning($"[{nameof(PartyBackpack)}] Cannot add a null consumable or non-positive quantity.");
                return;
            }

            for (int i = 0; i < backpackSlots.Count; i++)
            {
                BackpackSlot slot = backpackSlots[i];
                if (slot.consumableSO != consumable)
                    continue;

                slot.quantity += quantity;
                backpackSlots[i] = slot;
                return;
            }

            backpackSlots.Add(new BackpackSlot { consumableSO = consumable, quantity = quantity });
        }

        public void Clear()
        {
            backpackSlots.Clear();
        }

        public void RemoveConsumable(ConsumableSO consumable, int quantity = 1)
        {
            if (consumable == null || quantity <= 0)
            {
                Debug.LogWarning($"[{nameof(PartyBackpack)}] Cannot remove a null consumable or non-positive quantity.");
                return;
            }

            for (int i = 0; i < backpackSlots.Count; i++)
            {
                BackpackSlot slot = backpackSlots[i];
                if (slot.consumableSO != consumable)
                    continue;

                slot.quantity -= quantity;
                if (slot.quantity <= 0)
                    backpackSlots.RemoveAt(i);
                else
                    backpackSlots[i] = slot;
                return;
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