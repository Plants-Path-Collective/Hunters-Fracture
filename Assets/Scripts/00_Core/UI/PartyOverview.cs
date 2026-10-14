using System.Collections.Generic;
using UnityEngine;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Units;
using Overworld;

namespace UI
{
    /// <summary>
    /// Manages the party overview UI, displaying information about the player's party members.
    /// </summary>
    public class PartyOverview : MonoBehaviour
    {
        private AllyParty currentParty;
        [SerializeField] private PlayerEntity playerEntity;
        [SerializeField] private GameObject partySlotPrefab;
        [SerializeField] private Transform partySlotsContainer;

        private readonly List<PartySlot> partySlots = new();

        private void OnEnable()
        {
            if (playerEntity == null)
                playerEntity = FindFirstObjectByType<PlayerEntity>();

            if (playerEntity == null)
            {
                Debug.LogError($"[{nameof(PartyOverview)}] No {nameof(PlayerEntity)} found.", this);
                return;
            }

            Initialize(playerEntity.Party);
        }

        public void Initialize(AllyParty party)
        {
            if (party == null)
            {
                Debug.LogError($"[{nameof(PartyOverview)}] Cannot initialize from a null party.", this);
                return;
            }

            currentParty = party;
            InitializePartyPanel();
        }

        private void InitializePartyPanel()
        {
            if (currentParty == null) return;

            if (partySlotPrefab == null)
            {
                Debug.LogError($"[{nameof(PartyOverview)}] Party slot prefab is not assigned.", this);
                return;
            }

            if (currentParty.Members.Count > AllyParty.MaxMembers)
                Debug.LogWarning($"[{nameof(PartyOverview)}] Party has {currentParty.Members.Count} members; " +
                                 $"only the first {AllyParty.MaxMembers} can be displayed.", this);

            int displayCount = Mathf.Min(currentParty.Members.Count, AllyParty.MaxMembers);
            Transform container = partySlotsContainer != null ? partySlotsContainer : transform;

            while (partySlots.Count < displayCount)
            {
                GameObject slotObject = Instantiate(partySlotPrefab, container, false);
                PartySlot slotComponent = slotObject.GetComponent<PartySlot>();

                if (slotComponent == null)
                {
                    Debug.LogError($"[{nameof(PartyOverview)}] Party slot prefab is missing a {nameof(PartySlot)} component.", this);
                    Destroy(slotObject);
                    break;
                }

                partySlots.Add(slotComponent);
            }

            for (int i = 0; i < partySlots.Count; i++)
            {
                PartySlot slot = partySlots[i];
                bool hasMember = i < displayCount && currentParty.Members[i] != null;
                slot.gameObject.SetActive(hasMember);

                if (!hasMember) continue;

                if (!CombatSlots.TryFromIndex(i, out COMBAT_SLOT combatSlot))
                {
                    Debug.LogError($"[{nameof(PartyOverview)}] Party member index {i} has no combat slot.", this);
                    slot.gameObject.SetActive(false);
                    continue;
                }

                slot.PopulateSlot(currentParty.Members[i], combatSlot);
            }
        }
    }
}