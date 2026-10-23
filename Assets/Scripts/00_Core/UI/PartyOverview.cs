using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private InspectionPanel inspectionPanel;

        private readonly List<PartySlot> partySlots = new();
        private readonly List<PartySlot> hiddenPartySlots = new();
        private Action<PartyMemberData> consumableTargetSelected;

        private void OnEnable()
        {
            if (inspectionPanel != null)
                inspectionPanel.Hidden += RestorePartySlots;

            if (playerEntity == null)
                playerEntity = FindFirstObjectByType<PlayerEntity>();

            if (playerEntity == null)
            {
                Debug.LogError($"[{nameof(PartyOverview)}] No {nameof(PlayerEntity)} found.", this);
                return;
            }

            Initialize(playerEntity.Party);
        }

        private void OnDisable()
        {
            if (inspectionPanel != null)
                inspectionPanel.Hidden -= RestorePartySlots;

            RestorePartySlots();
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

        public void Refresh()
        {
            InitializePartyPanel();
        }

        public void BeginConsumableTargetSelection(Action<PartyMemberData> onSelected)
        {
            consumableTargetSelected = onSelected;
        }

        public void CancelConsumableTargetSelection()
        {
            consumableTargetSelected = null;
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

                slotComponent.Clicked += ShowInspection;
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

            ConfigurePartySlotNavigation(displayCount);
        }

        private void ShowInspection(PartyMemberData member, COMBAT_SLOT slot)
        {
            if (consumableTargetSelected != null)
            {
                Action<PartyMemberData> onSelected = consumableTargetSelected;
                consumableTargetSelected = null;
                onSelected.Invoke(member);
                return;
            }

            if (inspectionPanel == null)
            {
                Debug.LogError($"[{nameof(PartyOverview)}] Inspection panel is not assigned.", this);
                return;
            }

            inspectionPanel.Show(member, slot);
            if (inspectionPanel.isActiveAndEnabled)
                HideOtherPartySlots(slot);
        }

        private void ConfigurePartySlotNavigation(int displayCount)
        {
            List<Button> activeButtons = new();
            for (int i = 0; i < displayCount; i++)
            {
                if (!partySlots[i].gameObject.activeSelf)
                    continue;

                Button button = partySlots[i].GetComponent<Button>();
                if (button != null)
                    activeButtons.Add(button);
            }

            for (int i = 0; i < activeButtons.Count; i++)
            {
                Button button = activeButtons[i];

                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = activeButtons[(i + activeButtons.Count - 1) % activeButtons.Count];
                navigation.selectOnRight = activeButtons[(i + 1) % activeButtons.Count];
                button.navigation = navigation;
            }
        }

        private void HideOtherPartySlots(COMBAT_SLOT selectedCombatSlot)
        {
            RestorePartySlots();

            foreach (PartySlot slot in partySlots)
            {
                if (slot.combatSlot == selectedCombatSlot || !slot.gameObject.activeSelf) continue;

                hiddenPartySlots.Add(slot);
                slot.gameObject.SetActive(false);
            }
        }

        private void RestorePartySlots()
        {
            foreach (PartySlot slot in hiddenPartySlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(true);
            }

            hiddenPartySlots.Clear();
        }
    }
}