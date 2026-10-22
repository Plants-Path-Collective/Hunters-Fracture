using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Core;
using Core.CombatSystem.Units;
using UnityEngine.Localization;

namespace UI
{
    public class PartySlot : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public event Action<PartyMemberData, COMBAT_SLOT> Clicked;

        [Header("--- Identity ---")]
        public TextMeshProUGUI nameText;
        public Image portraitImage;

        [Header("--- Combat Info ---")]
        [Tooltip("The combat slot this party member is assigned to.")]
        public COMBAT_SLOT combatSlot; 
        public TextMeshProUGUI combatSlotText;

        [Header("--- Status Bars ---")]
        public Image HPBar;
        public TextMeshProUGUI HPText;
        public Image SPBar;
        public TextMeshProUGUI SPText;

        [Tooltip("The container for the status effect icons.")]
        public RectTransform effectsContainer;
        public GameObject statusEffectIconPrefab;

        [Header("--- Feedback ---")]
        public GameObject selectionHighlight;

        private Button button;
        private PartyMemberData member;
        private LocalizedString localizedCombatSlot;

        private void Awake()
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(false);

            button = GetComponent<Button>();
            if (button == null)
                Debug.LogError($"[{nameof(PartySlot)}] A {nameof(Button)} component is required.", this);
            else
                button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        private void OnEnable()
        {
            if (localizedCombatSlot != null && combatSlotText != null)
                localizedCombatSlot.StringChanged += UpdateCombatSlotText;
        }

        private void OnDisable()
        {
            if (localizedCombatSlot != null)
                localizedCombatSlot.StringChanged -= UpdateCombatSlotText;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetSelectionHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetSelectionHighlight(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetSelectionHighlight(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetSelectionHighlight(false);
        }

        private void SetSelectionHighlight(bool active)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(active);
        }

        private void HandleClick()
        {
            if (member != null)
                Clicked?.Invoke(member, combatSlot);
        }

        public void PopulateSlot(PartyMemberData member, COMBAT_SLOT slot)
        {
            if (member == null || member.definition == null)
            {
                Debug.LogError($"[{nameof(PartySlot)}] Cannot populate slot from a null member or definition.", this);
                return;
            }

            this.member = member;
            UnitDefinitionSO definition = member.definition;
            int maxHP = Mathf.Max(0, definition.maxHP);
            int maxSP = Mathf.Max(0, definition.maxSP);
            int currentHP = member.currentHP < 0 ? maxHP : Mathf.Clamp(member.currentHP, 0, maxHP);
            int currentSP = member.currentSP < 0 ? maxSP : Mathf.Clamp(member.currentSP, 0, maxSP);

            if (nameText != null)
                nameText.text = definition.unitName;

            if (portraitImage != null)
            {
                portraitImage.sprite = definition.partyPortrait;
                portraitImage.enabled = definition.partyPortrait != null;
            }

            if (HPBar != null)
                HPBar.fillAmount = maxHP > 0 ? currentHP / (float)maxHP : 0f;
            if (HPText != null)
                HPText.text = $"[{currentHP} / {maxHP}]";
            if (SPBar != null)
                SPBar.fillAmount = maxSP > 0 ? currentSP / (float)maxSP : 0f;
            if (SPText != null)
                SPText.text = $"[{currentSP} / {maxSP}]";

            combatSlot = slot;
            if (localizedCombatSlot != null)
            {
                localizedCombatSlot.StringChanged -= UpdateCombatSlotText;
                localizedCombatSlot = null;
            }

            if (combatSlotText != null)
            {
                string key = GetCombatSlotLocalizationKey(slot);
                if (key == null)
                {
                    combatSlotText.text = slot.ToString();
                }
                else
                {
                    localizedCombatSlot = new LocalizedString("UI_Units", key);
                    if (isActiveAndEnabled)
                        localizedCombatSlot.StringChanged += UpdateCombatSlotText;
                }
            }
        }

        private void UpdateCombatSlotText(string value)
        {
            if (combatSlotText != null)
                combatSlotText.text = value;
        }

        private static string GetCombatSlotLocalizationKey(COMBAT_SLOT slot)
        {
            return slot switch
            {
                COMBAT_SLOT.Left => "ui_units_left",
                COMBAT_SLOT.Mid => "ui_units_mid",
                COMBAT_SLOT.Right => "ui_units_right",
                _ => null
            };
        }

        public void SetCombatSlot(COMBAT_SLOT newSlot, out COMBAT_SLOT previousSlot)
        {
            previousSlot = combatSlot;
            combatSlot = newSlot;
        }
    }
}