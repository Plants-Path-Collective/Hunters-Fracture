using System.Collections.Generic;
using DG.Tweening;
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
        [SerializeField] private GameObject combatUIPanel;
        [SerializeField] private GameObject skillsPanel;
        [SerializeField] private GameObject backpackPanel;

        [Header("--- Actions Buttons ---")]
        [SerializeField] private GameObject attackButton;
        [SerializeField] private GameObject defendButton;
        [SerializeField] private GameObject skillsButton;
        [SerializeField] private GameObject backpackButton;
        [SerializeField] private GameObject fleeButton;

        [Header("--- Hold Indicators ---")]
        [Tooltip("One Image (Type = Filled) per hold action. They are hidden until the hold starts.")]
        [SerializeField] private Image attackHoldFill;
        [SerializeField] private Image defendHoldFill;
        [SerializeField] private Image skillsHoldFill;
        [SerializeField] private Image backpackHoldFill;
        [SerializeField] private Image fleeHoldFill;
        [SerializeField] private Image ultimateHoldFill;

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

        private readonly Dictionary<Image, Tween> holdTweens = new();
        private Tween cursorTween;

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
            }

            // Everything starts hidden; the first ally turn opens the panel.
            CloseCombatUI();
            HideCursor();
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
            }
        }

        void Start()
        {
            
        }

        // ── Turn Panel ─────────────────────────────────────────────────────────────

        /// <summary>Opens the Combat UI panel by enabling it.</summary>
        public void OpenCombatUI()
        {
            if (combatUIPanel != null) combatUIPanel.SetActive(true);
        }

        /// <summary>Closes the Combat UI panel and cancels any hold indicator still filling.</summary>
        public void CloseCombatUI()
        {
            HideAllHolds();
            if (combatUIPanel != null) combatUIPanel.SetActive(false);
        }

        private void HandleTurnStart(Unit actor)
        {
            if (actor.Team == UNIT_TEAM.Ally) OpenCombatUI();
            else CloseCombatUI();
        }

        // The action was resolved (or the turn was lost): the panel closes until the next ally turn.
        private void HandleTurnEnd(Unit actor)
        {
            CloseCombatUI();
            HideCursor();
        }

        private void HandleCombatEnd(COMBAT_OUTCOME outcome)
        {
            CloseCombatUI();
            HideCursor();
        }

        // ── Skills panel ─────────────────────────────────────────────────────

        public void OpenSkillsPanel()
        {
            if (skillsPanel != null) skillsPanel.SetActive(true);
        }

        public void CloseSkillsPanel()
        {
            if (skillsPanel != null) skillsPanel.SetActive(false);
        }

        public void ToggleSkillsPanel()
        {
            if (skillsPanel == null)
            {
                Debug.LogError($"[{nameof(CombatUIManager)}] Skills panel is not assigned.");
                return;
            }

            if (skillsPanel.activeSelf) CloseSkillsPanel();
            else OpenSkillsPanel();
        }

        // ── Backpack panel ─────────────────────────────────────────────────────
        public void OpenBackpackPanel()
        {
            RenderBackpackSlots();
            if (backpackPanel != null) backpackPanel.SetActive(true);
        }

        public void CloseBackpackPanel()
        {
            if (backpackPanel != null) backpackPanel.SetActive(false);
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
            }
        }

        // ── Target cursor ─────────────────────────────────────────────────────

        private void HandleTargetChanged(Unit target)
        {
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
            var input = InputManager.Instance.Combat;

            if (action == input.BasicAttack) return attackHoldFill;
            if (action == input.Defend) return defendHoldFill;
            if (action == input.Skills) return skillsHoldFill;
            if (action == input.Backpack) return backpackHoldFill;
            if (action == input.Flee) return fleeHoldFill;
            if (action == input.CastUltimate) return ultimateHoldFill;
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

            // Also covers fills left active in the scene at design time.
            foreach (Image fill in new[] { attackHoldFill, defendHoldFill, skillsHoldFill,
                         backpackHoldFill, fleeHoldFill, ultimateHoldFill })
                if (fill != null) fill.gameObject.SetActive(false);
        }
    }
}