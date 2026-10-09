using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;
using Core.CombatSystem;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>
    /// Post-combat rewards screen (demo version). On Victory it switches to the Rewards input map,
    /// offers a few items, lets the player assign each one to an ally, and on a held Confirm adds
    /// them to the allies' inventories and closes the combat (CombatController.CloseCombat), which
    /// returns to the Overworld.
    /// </summary>
    /// <remarks>
    /// Requires CombatController.autoCloseOnEnd = false: this screen is the one that calls
    /// CloseCombat(). Fled and Defeat have no rewards, so they close the combat right away.
    /// Items are only written to the inventories when the player confirms; until then assignments
    /// can be undone with Cancel / Back.
    /// </remarks>
    public class RewardsScreen : MonoBehaviour
    {
        private enum ScreenState { Idle, Active, Closing }

        private struct Assignment
        {
            public ItemSO item;
            public Unit unit;
            public int originalIndex;
        }

        public GameObject combatHints;

        [Header("--- Sources ---")]
        [SerializeField] private CombatController combat;
        [SerializeField] private ItemCatalog itemCatalog;

        [Header("--- Rewards ---")]
        [SerializeField, Min(1)] private int rewardCount = 2;
        [Tooltip("Testing: if not empty, exactly these items are offered instead of random ones.")]
        [SerializeField] private List<ItemSO> forcedRewards = new();

        [Header("--- Panel ---")]
        [Tooltip("Root of the screen. Hidden until a Victory. Must be a CHILD of this object, " +
                 "because this component has to stay active to hear the combat end.")]
        [SerializeField] private GameObject panel;

        [Header("--- Unit rows (one per ally slot) ---")]
        [SerializeField] private RewardUnitRowView leftRow;
        [SerializeField] private RewardUnitRowView midRow;
        [SerializeField] private RewardUnitRowView rightRow;

        [Header("--- Unassigned items ---")]
        [Tooltip("Parent of the unassigned icons. Give it a VerticalLayoutGroup.")]
        [SerializeField] private RectTransform unassignedRoot;
        [SerializeField] private RewardItemView itemPrefab;

        [Header("--- Description ---")]
        [SerializeField] private TMP_Text itemNameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [Tooltip("Optional: shown once every item is assigned ('hold Confirm to continue').")]
        [SerializeField] private GameObject confirmHint;

        [Header("--- Confirm hold ---")]
        [Tooltip("Image (Type = Filled) that fills while Confirm is held. Optional.")]
        [SerializeField] private Image holdFill;

        [Header("--- Input ---")]
        [Tooltip("How far the stick must be pushed before it counts as one step up/down.")]
        [SerializeField, Range(0.1f, 0.9f)] private float stickThreshold = 0.5f;

        private ScreenState state = ScreenState.Idle;
        private readonly Dictionary<COMBAT_SLOT, Unit> allies = new();
        private readonly List<RewardItemView> unassignedViews = new();
        private List<ItemSO> unassigned = new();
        private readonly List<Assignment> assignments = new();
        private int selectedIndex;
        private int lastDirection;
        private Tween holdTween;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (panel != null) panel.SetActive(false);
            HideHoldFill();

            if (combat != null) combat.OnCombatEnd += OnCombatEnd;
            else Debug.LogError($"[{nameof(RewardsScreen)}] Assign the CombatController in the Inspector.");

            if (InputManager.Instance == null)
            {
                Debug.LogError($"[{nameof(RewardsScreen)}] InputManager.Instance is null. " +
                               "Is the GameManager present in the scene flow?");
                return;
            }

            var input = InputManager.Instance.Rewards;

            input.Navigate.performed += OnNavigate;
            input.Navigate.canceled += OnNavigate; // resets the edge detection

            input.Confirm.started += OnConfirmStarted;
            input.Confirm.performed += OnConfirmPerformed;
            input.Confirm.canceled += OnConfirmCanceled;

            input.CancelBack.performed += OnCancel;

            input.AsigntoUnitLeft.performed += OnAssignLeft;
            input.AsigntoUnitMid.performed += OnAssignMid;
            input.AsigntoUnitRight.performed += OnAssignRight;
        }

        private void OnDisable()
        {
            if (combat != null) combat.OnCombatEnd -= OnCombatEnd;
            holdTween?.Kill();

            // InputManager is DontDestroyOnLoad and can be gone first when the app closes.
            if (InputManager.Instance == null) return;

            var input = InputManager.Instance.Rewards;

            input.Navigate.performed -= OnNavigate;
            input.Navigate.canceled -= OnNavigate;

            input.Confirm.started -= OnConfirmStarted;
            input.Confirm.performed -= OnConfirmPerformed;
            input.Confirm.canceled -= OnConfirmCanceled;

            input.CancelBack.performed -= OnCancel;

            input.AsigntoUnitLeft.performed -= OnAssignLeft;
            input.AsigntoUnitMid.performed -= OnAssignMid;
            input.AsigntoUnitRight.performed -= OnAssignRight;
        }

        // ── Combat flow ───────────────────────────────────────────────────────

        private void OnCombatEnd(COMBAT_OUTCOME outcome)
        {
            if (state != ScreenState.Idle) return;

            // Only a Victory gives rewards. Fled/Defeat keep the old behaviour (close right away).
            if (outcome != COMBAT_OUTCOME.Victory)
            {
                state = ScreenState.Closing;
                StartCoroutine(CloseCombatNextFrame());
                return;
            }

            Open();
        }

        private void Open()
        {
            combatHints.SetActive(false);

            allies.Clear();
            foreach (Unit unit in combat.Roster)
                if (unit != null && unit.Team == UNIT_TEAM.Ally && unit.Slot.HasValue)
                    allies[unit.Slot.Value] = unit;

            unassigned = RollRewards();
            assignments.Clear();
            selectedIndex = 0;
            lastDirection = 0;

            if (unassigned.Count == 0)
            {
                Debug.LogWarning($"[{nameof(RewardsScreen)}] Nothing to offer (no catalog and no forced rewards).");
                state = ScreenState.Closing;
                StartCoroutine(CloseCombatNextFrame());
                return;
            }

            state = ScreenState.Active;
            if (panel != null) panel.SetActive(true);

            RefreshRows();
            RefreshUnassigned();
            RefreshDescription();

            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Rewards);
        }

        /// <summary>Called by the input handlers once everything is assigned. Writes the items into
        /// the allies' inventories (CloseCombat then persists them) and ends the combat.</summary>
        private void Confirm()
        {
            if (state != ScreenState.Active) return;

            if (unassigned.Count > 0)
            {
                Debug.Log($"[{nameof(RewardsScreen)}] Assign every item before continuing.");
                return;
            }

            foreach (Assignment assignment in assignments)
                assignment.unit.Inventory.AddItem(assignment.item);

            state = ScreenState.Closing;
            if (panel != null) panel.SetActive(false);
            StartCoroutine(CloseCombatNextFrame());
        }

        // Waits one frame so CombatController.EndCombat() finishes (it keeps running after OnCombatEnd).
        private IEnumerator CloseCombatNextFrame()
        {
            yield return null;
            combat.CloseCombat();
        }

        private List<ItemSO> RollRewards()
        {
            var result = new List<ItemSO>();

            if (forcedRewards.Count > 0)
            {
                foreach (ItemSO item in forcedRewards)
                    if (item != null) result.Add(item);
                return result;
            }

            if (itemCatalog == null)
            {
                Debug.LogError($"[{nameof(RewardsScreen)}] No ItemCatalog assigned.");
                return result;
            }

            // Distinct random items.
            var pool = new List<ItemSO>();
            foreach (ItemSO item in itemCatalog.AllItems)
                if (item != null) pool.Add(item);

            for (int i = 0; i < rewardCount && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void Move(int direction)
        {
            if (unassigned.Count < 2) return;

            selectedIndex = (selectedIndex + direction + unassigned.Count) % unassigned.Count;
            RefreshUnassigned();
            RefreshDescription();
        }

        private void Assign(COMBAT_SLOT slot)
        {
            if (state != ScreenState.Active || unassigned.Count == 0) return;

            if (!allies.TryGetValue(slot, out Unit unit))
            {
                Debug.Log($"[{nameof(RewardsScreen)}] There is no ally in slot {slot}.");
                return;
            }

            ItemSO item = unassigned[selectedIndex];
            unassigned.RemoveAt(selectedIndex);
            assignments.Add(new Assignment { item = item, unit = unit, originalIndex = selectedIndex });

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, unassigned.Count - 1));

            RefreshRow(slot);
            RefreshUnassigned();
            RefreshDescription();
        }

        /// <summary>Undoes the last assignment: the item goes back to the unassigned list.</summary>
        private void UndoLastAssignment()
        {
            if (state != ScreenState.Active || assignments.Count == 0) return;

            Assignment last = assignments[assignments.Count - 1];
            assignments.RemoveAt(assignments.Count - 1);

            int index = Mathf.Min(last.originalIndex, unassigned.Count);
            unassigned.Insert(index, last.item);
            selectedIndex = index;

            RefreshRows();
            RefreshUnassigned();
            RefreshDescription();
        }

        // ── View refresh ──────────────────────────────────────────────────────

        private RewardUnitRowView RowFor(COMBAT_SLOT slot) => slot switch
        {
            COMBAT_SLOT.Left => leftRow,
            COMBAT_SLOT.Mid => midRow,
            _ => rightRow
        };

        private void RefreshRows()
        {
            foreach (COMBAT_SLOT slot in new[] { COMBAT_SLOT.Left, COMBAT_SLOT.Mid, COMBAT_SLOT.Right })
                RefreshRow(slot);
        }

        private void RefreshRow(COMBAT_SLOT slot)
        {
            RewardUnitRowView row = RowFor(slot);
            if (row == null) return;

            if (!allies.TryGetValue(slot, out Unit unit))
            {
                row.Hide();
                return;
            }

            row.Bind(unit);
            row.SetItems(BuildRowItems(unit), itemPrefab);
        }

        /// <summary>What the unit holds now, plus the items assigned to it on this screen.</summary>
        private List<(ItemSO item, int quantity)> BuildRowItems(Unit unit)
        {
            var items = new List<(ItemSO item, int quantity)>();

            foreach (Item held in unit.Inventory.inventory.Values)
                if (held != null && held.itemSO != null && held.quantity > 0)
                    items.Add((held.itemSO, held.quantity));

            foreach (Assignment assignment in assignments)
            {
                if (assignment.unit != unit) continue;

                int existing = items.FindIndex(i => i.item == assignment.item);
                if (existing >= 0) items[existing] = (assignment.item, items[existing].quantity + 1);
                else items.Add((assignment.item, 1));
            }

            return items;
        }

        private void RefreshUnassigned()
        {
            while (unassignedViews.Count < unassigned.Count)
                unassignedViews.Add(Instantiate(itemPrefab, unassignedRoot));

            for (int i = 0; i < unassignedViews.Count; i++)
            {
                bool used = i < unassigned.Count;
                unassignedViews[i].gameObject.SetActive(used);

                if (!used) continue;
                unassignedViews[i].Bind(unassigned[i]);
                unassignedViews[i].SetSelected(i == selectedIndex);
            }
        }

        private void RefreshDescription()
        {
            bool hasSelection = unassigned.Count > 0;
            ItemSO item = hasSelection ? unassigned[selectedIndex] : null;

            if (itemNameLabel != null) itemNameLabel.text = hasSelection ? item.itemName : string.Empty;
            if (descriptionLabel != null) descriptionLabel.text = hasSelection ? item.itemDescription : string.Empty;
            if (confirmHint != null) confirmHint.SetActive(!hasSelection);
        }

        // ── Input handlers ────────────────────────────────────────────────────

        private void OnNavigate(InputAction.CallbackContext context)
        {
            // Edge detection: a held stick/key fires performed repeatedly, only a fresh push counts.
            float y = context.ReadValue<Vector2>().y;
            int direction = y > stickThreshold ? -1 : y < -stickThreshold ? 1 : 0; // up = previous item

            bool isNewStep = direction != 0 && direction != lastDirection;
            lastDirection = direction;

            if (isNewStep && state == ScreenState.Active)
                Move(direction);
        }

        private void OnAssignLeft(InputAction.CallbackContext context) => Assign(COMBAT_SLOT.Left);
        private void OnAssignMid(InputAction.CallbackContext context) => Assign(COMBAT_SLOT.Mid);
        private void OnAssignRight(InputAction.CallbackContext context) => Assign(COMBAT_SLOT.Right);

        private void OnCancel(InputAction.CallbackContext context) => UndoLastAssignment();

        // Confirm is a Hold in the asset: started = pressed, performed = held long enough,
        // canceled = released (also after performed).
        private void OnConfirmStarted(InputAction.CallbackContext context)
        {
            if (state != ScreenState.Active || unassigned.Count > 0) return;
            ShowHoldFill(GetHoldDuration(context));
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            HideHoldFill();
            Confirm();
        }

        private void OnConfirmCanceled(InputAction.CallbackContext context) => HideHoldFill();

        /// <summary>Duration configured in the asset. HoldInteraction.duration is 0 when the binding
        /// does not override it, which means the project-wide default hold time.</summary>
        private static float GetHoldDuration(InputAction.CallbackContext context)
        {
            float duration = context.interaction is HoldInteraction hold ? hold.duration : 0f;

            // Fully qualified on purpose: the generated "InputSystem" namespace shadows the class name.
            return duration > 0f
                ? duration
                : UnityEngine.InputSystem.InputSystem.settings.defaultHoldTime;
        }

        // ── Hold indicator ────────────────────────────────────────────────────

        private void ShowHoldFill(float duration)
        {
            if (holdFill == null) return;

            holdTween?.Kill();
            holdFill.fillAmount = 0f;
            holdFill.gameObject.SetActive(true);

            holdTween = DOTween.To(() => holdFill.fillAmount, v => holdFill.fillAmount = v, 1f, duration)
                .SetEase(Ease.Linear)
                .SetLink(holdFill.gameObject);
        }

        private void HideHoldFill()
        {
            holdTween?.Kill();
            if (holdFill == null) return;

            holdFill.fillAmount = 0f;
            holdFill.gameObject.SetActive(false);
        }
    }
}