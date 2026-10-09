using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Core.CombatSystem.Backpack;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>
    /// Combat HUD. Listens to CombatController (turn flow) and CombatInputHandler (target cursor,
    /// hold feedback); neither of them knows this class exists.
    /// </summary>
    public class CombatUIManager : MonoBehaviour
    {
        public static CombatUIManager Instance;

        [Header("--- Sources ---")]
        [SerializeField] private CombatController combat;
        [SerializeField] private CombatInputHandler inputHandler;
        [Tooltip("Camera used to place the target cursor. Falls back to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Header("--- Panels ---")]
        [SerializeField] private CombatPanelUI leftPanel;
        [SerializeField] private CombatPanelUI midPanel;
        [SerializeField] private CombatPanelUI rightPanel;
        //[SerializeField] private GameObject skillsPanel;
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private GameObject gameoverScreen;

        [Header("--- Unit Inspection ---")]
        [SerializeField] private GameObject inspectionPanel;
        [SerializeField] private TMP_Text inspectionNameText;
        [SerializeField] private Image inspectionPortrait;
        [SerializeField] private TMP_Text inspectionUnitTypeText;
        [SerializeField] private TMP_Text inspectionTeamText;
        [SerializeField] private TMP_Text inspectionStatsText;

        [System.Serializable]
        private class CombatPanelUI
        {
            public GameObject panel;
            public GameObject attackButton;
            public GameObject defendButton;
            public GameObject skillsButton;
            public GameObject backpackButton;
            public GameObject fleeButton;
        }

        [Header("--- Hold Indicators ---")]
        [Tooltip("Shared Image (Type = Filled) used for every hold action. Hidden until a hold starts.")]
        [SerializeField] private Image holdFill;

        [Header("--- Combat Extras ---")]
        [Tooltip("Marker over the highlighted target. A UI element (RectTransform) is placed in " +
                 "screen space; any other object is placed in world space.")]
        [SerializeField] private GameObject cursor;
        [Tooltip("Extra height above the top of the unit's renderers.")]
        [SerializeField] private float cursorHeightPadding = 0.5f;
        [Tooltip("Seconds the cursor takes to slide to a new target. 0 = snap.")]
        [SerializeField] private float cursorMoveDuration = 0.1f;

        [Header("--- Backapack ---")]
        [SerializeField] private GameObject backpackSlotPrefab;

        private readonly List<BackpackSlotUI> backpackSlotViews = new();
        private BackpackSlotUI selectedBackpackSlot;
        private readonly Dictionary<Image, Tween> holdTweens = new();
        private Tween cursorTween;
        private int backpackIndex;

        private void Awake()
        {
            // Local singleton, scoped to the Combat scene only (not persisted across scenes)
            if (Instance != null && Instance != this) { Destroy(this); return; }

            Instance = this;

            if (combat == null || inputHandler == null)
                Debug.LogError($"[{nameof(CombatUIManager)}] Assign CombatController and " +
                               "CombatInputHandler in the Inspector.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.OnTurnStart += HandleTurnStart;
                combat.OnTurnEnd += HandleTurnEnd;
                combat.OnCombatEnd += HandleCombatEnd;
            }

            if (inputHandler != null)
            {
                inputHandler.OnTargetChanged += HandleTargetChanged;
                inputHandler.OnHoldStarted += HandleHoldStarted;
                inputHandler.OnHoldCanceled += HandleHoldCanceled;
                inputHandler.OnInspectUnit += ToggleInspectionPanel;
            }

            // Everything starts hidden; the first ally turn opens the panel.
            CloseCombatUI();
            HideCursor();
            HideInspectionPanel();
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.OnTurnStart -= HandleTurnStart;
                combat.OnTurnEnd -= HandleTurnEnd;
                combat.OnCombatEnd -= HandleCombatEnd;
            }

            if (inputHandler != null)
            {
                inputHandler.OnTargetChanged -= HandleTargetChanged;
                inputHandler.OnHoldStarted -= HandleHoldStarted;
                inputHandler.OnHoldCanceled -= HandleHoldCanceled;
                inputHandler.OnInspectUnit -= ToggleInspectionPanel;
            }
        }

        void Start()
        {
            
        }

        // ── Turn Panel ─────────────────────────────────────────────────────────────

        /// <summary>Opens the left combat action panel by default.</summary>
        public void OpenCombatUI() => OpenCombatUI(COMBAT_SLOT.Left);

        /// <summary>Opens the combat action panel for the given ally slot.</summary>
        public void OpenCombatUI(COMBAT_SLOT slot)
        {
            CloseCombatUI();
            CombatPanelUI panel = GetPanelForSlot(slot);
            if (panel != null && panel.panel != null) panel.panel.SetActive(true);
        }

        /// <summary>Closes every ally action panel and cancels any hold indicator still filling.</summary>
        public void CloseCombatUI()
        {
            HideAllHolds();
            ClosePanel(leftPanel);
            ClosePanel(midPanel);
            ClosePanel(rightPanel);
        }

        private void HandleTurnStart(Unit actor)
        {
            if (actor.Team == UNIT_TEAM.Ally)
            {
                if (actor.Slot.HasValue) OpenCombatUI(actor.Slot.Value);
                else OpenCombatUI(COMBAT_SLOT.Left);
            }
            else CloseCombatUI();
        }

        private CombatPanelUI GetPanelForSlot(COMBAT_SLOT slot)
        {
            return slot switch
            {
                COMBAT_SLOT.Left => leftPanel,
                COMBAT_SLOT.Mid => midPanel,
                COMBAT_SLOT.Right => rightPanel,
                _ => null
            };
        }

        private static void ClosePanel(CombatPanelUI panel)
        {
            if (panel != null && panel.panel != null)
                panel.panel.SetActive(false);
        }

        // The action was resolved (or the turn was lost): the panel closes until the next ally turn.
        private void HandleTurnEnd(Unit actor)
        {
            CloseCombatUI();
            HideCursor();
            HideInspectionPanel();
        }

        private void HandleCombatEnd(COMBAT_OUTCOME outcome)
        {
            CloseCombatUI();
            HideCursor();
            HideInspectionPanel();
        }

        // ── Skills panel ─────────────────────────────────────────────────────

        // public void OpenSkillsPanel()
        // {
        //     if (skillsPanel != null) skillsPanel.SetActive(true);
        // }

        // public void CloseSkillsPanel()
        // {
        //     if (skillsPanel != null) skillsPanel.SetActive(false);
        // }

        // public void ToggleSkillsPanel()
        // {
        //     if (skillsPanel == null)
        //     {
        //         Debug.LogError($"[{nameof(CombatUIManager)}] Skills panel is not assigned.");
        //         return;
        //     }

        //     if (skillsPanel.activeSelf) CloseSkillsPanel();
        //     else OpenSkillsPanel();
        // }

        // ── Backpack panel ─────────────────────────────────────────────────────
        public void OpenBackpackPanel()
        {
            RenderBackpackSlots();
            if (backpackPanel != null) backpackPanel.SetActive(true);
            SelectBackpackSlotIndex(0);
        }

        public void CloseBackpackPanel()
        {
            if (backpackPanel != null) backpackPanel.SetActive(false);
            selectedBackpackSlot = null;
            foreach (BackpackSlotUI slotUI in backpackSlotViews)
                if (slotUI != null) slotUI.SetSelected(false);
        }

        public void SelectBackpackSlotIndex(int index)
        {
            if (backpackSlotViews.Count == 0)
            {
                selectedBackpackSlot = null;
                backpackIndex = 0;
                return;
            }

            index = Mathf.Clamp(index, 0, backpackSlotViews.Count - 1);
            backpackIndex = index;

            BackpackSlotUI slotUI = backpackSlotViews[index];
            SelectBackpackSlot(slotUI);
        }

        public int GetSelectedBackpackIndex() => backpackIndex;

        public ConsumableSO GetSelectedConsumable()
        {
            return selectedBackpackSlot != null ? selectedBackpackSlot.consumable : null;
        }

        public bool TryConsumeSelectedBackpackItem(CombatInputHandler inputHandler)
        {
            ConsumableSO consumable = GetSelectedConsumable();
            if (consumable == null || inputHandler == null) return false;
            return inputHandler.ConsumeSelectedItem(consumable);
        }

        private void SelectBackpackSlot(BackpackSlotUI slotUI)
        {
            if (selectedBackpackSlot == slotUI) return;

            foreach (BackpackSlotUI other in backpackSlotViews)
                if (other != null) other.SetSelected(other == slotUI);

            selectedBackpackSlot = slotUI;
        }

        public void ToggleBackpackPanel()
        {
            if (backpackPanel == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Backpack panel is not assigned.");
                return;
            }

            if (backpackPanel.activeSelf) CloseBackpackPanel();
            else OpenBackpackPanel();
        }

        private void RenderBackpackSlots()
        {
            if (backpackPanel == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Backpack panel is not assigned.");
                return;
            }

            VerticalLayoutGroup slotsLayout = backpackPanel.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (slotsLayout == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Backpack panel has no " +
                               $"{nameof(VerticalLayoutGroup)} to contain item slots.");
                return;
            }

            if (backpackSlotPrefab == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Backpack slot prefab is not assigned.");
                return;
            }

            Transform slotsContainer = slotsLayout.transform;
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);
            backpackSlotViews.Clear();

            if (PartyBackpack.Instance == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] No {nameof(PartyBackpack)} instance is available.");
                return;
            }

            foreach (BackpackSlot slot in PartyBackpack.Instance.backpackSlots)
            {
                if (slot.consumableSO == null)
                {
                    Debug.LogWarning($"[{nameof(CombatUIManager)}] Skipping a backpack slot with no consumable.");
                    continue;
                }

                GameObject slotObject = Instantiate(backpackSlotPrefab, slotsContainer, false);
                BackpackSlotUI slotUI = slotObject.GetComponent<BackpackSlotUI>();
                if (slotUI == null ||
                    slotUI.consumableIcon == null ||
                    slotUI.consumableNameText == null ||
                    slotUI.quantityText == null ||
                    slotUI.effectText == null)
                {
                    Debug.LogError($"[{nameof(CombatUIManager)}] Backpack slot prefab must have a " +
                                   $"{nameof(BackpackSlotUI)} with all UI fields assigned.");
                    Destroy(slotObject);
                    return;
                }

                slotUI.consumableIcon.sprite = slot.consumableSO.itemIcon;
                slotUI.consumableNameText.text = slot.consumableSO.itemName;
                slotUI.quantityText.text = $"x {slot.quantity}";
                slotUI.effectText.text = slot.consumableSO.effect;
                slotUI.SetConsumable(slot.consumableSO);
                slotUI.SetSelected(false);

                Button button = slotObject.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(() => SelectBackpackSlot(slotUI));
                else
                    slotObject.AddComponent<Button>().onClick.AddListener(() => SelectBackpackSlot(slotUI));

                backpackSlotViews.Add(slotUI);
            }

            if (backpackSlotViews.Count > 0)
                SelectBackpackSlot(backpackSlotViews[0]);
        }

        // ── Target cursor ─────────────────────────────────────────────────────

        private void HandleTargetChanged(Unit target)
        {
            if (target == null)
            {
                HideInspectionPanel();
            }
            else if (inspectionPanel != null && inspectionPanel.activeSelf)
            {
                ShowInspectionPanel(target);
            }

            if (cursor == null) return;

            if (target == null)
            {
                HideCursor();
                return;
            }

            Vector3 position = GetCursorPosition(target);
            bool wasHidden = !cursor.activeSelf;
            cursor.SetActive(true);

            cursorTween?.Kill();

            // First appearance snaps, so it never slides in from wherever it was last time.
            if (wasHidden || cursorMoveDuration <= 0f)
                cursor.transform.position = position;
            else
                cursorTween = cursor.transform.DOMove(position, cursorMoveDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(cursor);
        }

        private void ToggleInspectionPanel(Unit target)
        {
            if (inspectionPanel == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Inspection panel is not assigned.", this);
                return;
            }

            if (inspectionPanel.activeSelf) HideInspectionPanel();
            else ShowInspectionPanel(target);
        }

        private void ShowInspectionPanel(Unit target)
        {
            if (target == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Cannot inspect a null unit.", this);
                return;
            }

            if (inspectionPanel == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Inspection panel is not assigned.", this);
                return;
            }

            if (inspectionNameText == null || inspectionPortrait == null || inspectionUnitTypeText == null ||
                inspectionTeamText == null || inspectionStatsText == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Assign all inspection panel text and portrait references.", this);
                return;
            }

            inspectionNameText.text = target.Name;
            inspectionPortrait.sprite = target.Portrait;
            inspectionPortrait.enabled = target.Portrait != null;
            inspectionUnitTypeText.text = target.UnitType.ToString();
            inspectionTeamText.text = target.Team.ToString();
            inspectionStatsText.text =
                $"HP: {target.HP}/{target.MaxHP}\n" +
                $"SP: {target.SP}/{target.MaxSP}\n" +
                $"Speed: {target.Speed:0.##}\n" +
                $"Strength: {target.Strength:0.##}\n" +
                $"Magic Power: {target.MagicPower:0.##}\n" +
                $"Physical Defense: {target.PhysicalDefense:0.##}\n" +
                $"Magical Defense: {target.MagicalDefense:0.##}";
            inspectionPanel.SetActive(true);
        }

        private void HideInspectionPanel()
        {
            if (inspectionPanel != null) inspectionPanel.SetActive(false);
        }

        /// <summary>Point just above the top of the unit's visible renderers, so it works for any
        /// model size or pivot placement.</summary>
        private Vector3 GetCursorAnchor(Unit target)
        {
            Bounds bounds = default;
            bool hasBounds = false;

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;

                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }

            if (!hasBounds)
                return target.transform.position + Vector3.up * (1f + cursorHeightPadding);

            return new Vector3(bounds.center.x, bounds.max.y + cursorHeightPadding, bounds.center.z);
        }

        private Vector3 GetCursorPosition(Unit target)
        {
            Vector3 anchor = GetCursorAnchor(target);

            // World-space cursor (any non-UI object): use the anchor as is.
            if (cursor.transform is not RectTransform rect)
                return anchor;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] No camera to place the cursor. " +
                            "Assign worldCamera or tag the CombatStage camera as MainCamera.");
                return rect.position; // stay where it is instead of jumping to garbage coordinates
            }

            Vector2 screenPoint = cam.WorldToScreenPoint(anchor);
            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return screenPoint;

            // Screen Space - Camera / World Space: convert through the canvas plane.
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rect.parent as RectTransform, screenPoint, canvas.worldCamera, out Vector3 worldPoint);
            return worldPoint;
        }

        private void HideCursor()
        {
            cursorTween?.Kill();
            if (cursor != null) cursor.SetActive(false);
        }

        // ── Hold indicators ───────────────────────────────────────────────────

        private void HandleHoldStarted(InputAction action, float duration)
        {
            Image fill = FillFor(action);
            if (fill == null) return;

            KillHold(fill);
            fill.fillAmount = 0f;
            fill.gameObject.SetActive(true);

            holdTweens[fill] = DOTween.To(() => fill.fillAmount, v => fill.fillAmount = v, 1f, duration)
                .SetEase(Ease.Linear)
                .SetLink(fill.gameObject)
                .OnComplete(() => HideHold(fill));
        }

        // Fires on early release (cancels the fill) and also after a completed hold (already hidden).
        private void HandleHoldCanceled(InputAction action)
        {
            Image fill = FillFor(action);
            if (fill != null) HideHold(fill);
        }

        private Image FillFor(InputAction action)
        {
            // All hold actions share the same visual indicator to keep the HUD consistent.
            var input = InputManager.Instance.Combat;
            if (action == input.BasicAttack || action == input.Defend ||
                action == input.Skills || action == input.Backpack ||
                action == input.Flee || action == input.CastUltimate)
                return holdFill;

            return null;
        }

        private void HideHold(Image fill)
        {
            KillHold(fill);
            fill.fillAmount = 0f;
            fill.gameObject.SetActive(false);
        }

        private void KillHold(Image fill)
        {
            if (holdTweens.Remove(fill, out Tween tween))
                tween.Kill();
        }

        private void HideAllHolds()
        {
            foreach (Image fill in new List<Image>(holdTweens.Keys))
                HideHold(fill);

            // Also covers the shared fill left active in the scene at design time.
            if (holdFill != null) holdFill.gameObject.SetActive(false);
        }
    }
}