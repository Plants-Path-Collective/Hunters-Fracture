using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Core.Clock;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;
using PlantsPathCo.DialogueSystem;

namespace Core.UI
{
    /// <summary>
    /// Out-of-combat item assignment (chests, shops, quest rewards). Owns the flow: waits for dialogue,
    /// switches the input map, pauses the clock, feeds the session from the player's input and tells the
    /// view what to draw. The rules live in <see cref="ItemAssignmentSession"/>; the visuals in the view.
    /// </summary>
    public class ItemAssignationController : MonoBehaviour
    {
        public static ItemAssignationController Instance { get; private set; }

        [Header("--- Sources ---")]
        [SerializeField] private ItemCatalog itemCatalog;
        [SerializeField] private ItemAssignationView view;

        [Header("--- Behaviour ---")]
        [Tooltip("If true, the player can confirm (or leave with Cancel when nothing is assigned) while some " +
                 "items remain unassigned. Those items are returned to the caller. Use it for chests.")]
        [SerializeField] private bool allowLeaveItems;

        [Header("--- Input ---")]
        [SerializeField, Range(0.1f, 0.9f)] private float stickThreshold = 0.5f;

        private ItemAssignmentSession session;
        private Action<List<ItemSO>> onClosed;
        private INPUTACTION_MAP? mapBefore;
        private bool opening;
        private bool isOpen;
        private bool clockPaused;
        private bool holding;
        private float holdStart;
        private float holdDuration;
        private int lastDirection;

        // Input hint pills (rebuilt on every open so rebinding is respected).
        private readonly List<InputHint> hints = new();
        private readonly InputHint[] assignHints = new InputHint[AllyParty.MaxMembers];
        private InputHint selectHint;
        private InputHint backHint;
        private InputHint confirmHint;

        public bool IsOpen => isOpen || opening;

        /// <summary>
        /// Static shortcut for other scripts (chests, shops, dialogue actions).
        /// False if there is no controller in the scene or it cannot open right now.
        /// </summary>
        public static bool TryOpen(IEnumerable<ItemSO> items, Action<List<ItemSO>> closed = null)
        {
            if (Instance == null)
            {
                Debug.LogError($"[{nameof(ItemAssignationController)}] No controller in the scene.");
                return false;
            }

            return Instance.Open(items, closed);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (view != null) view.Hide();
        }

        private void Update()
        {
            if (!holding || view == null) return;

            float progress = holdDuration > 0f ? (Time.unscaledTime - holdStart) / holdDuration : 1f;
            view.SetHoldProgress(Mathf.Clamp01(progress));
        }

        private void OnDisable()
        {
            UnsubscribeInput();

            if (clockPaused)
            {
                GameClock.ReleasePause(CLOCK_PAUSE_REASON.Menu);
                clockPaused = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the assignment UI for the active party. If a dialogue is running (e.g. called from a
        /// dialogue action) it waits for it to end, so DialogueManager does not restore the previous
        /// input map on top of this UI.
        /// </summary>
        /// <param name="closed">Receives the items that were NOT assigned (empty = all assigned).</param>
        /// <returns>False if already open, nothing to offer, or there is no party/view.</returns>
        public bool Open(IEnumerable<ItemSO> items, Action<List<ItemSO>> closed = null)
        {
            if (IsOpen) return false;

            if (view == null)
            {
                Debug.LogError($"[{nameof(ItemAssignationController)}] No view assigned.", this);
                return false;
            }

            if (GameManager.Instance == null || GameManager.Instance.party == null)
            {
                Debug.LogError($"[{nameof(ItemAssignationController)}] No GameManager.party available.", this);
                return false;
            }

            var list = new List<ItemSO>();
            foreach (ItemSO item in items)
                if (item != null) list.Add(item);

            if (list.Count == 0) return false;

            opening = true;
            StartCoroutine(OpenRoutine(list, closed));
            return true;
        }

        // ── Open / close ──────────────────────────────────────────────────────

        private IEnumerator OpenRoutine(List<ItemSO> items, Action<List<ItemSO>> callback)
        {
            while (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking)
                yield return null;

            yield return null; // let DialogueManager finish restoring its map and clock

            Begin(items, callback);
        }

        private void Begin(List<ItemSO> items, Action<List<ItemSO>> callback)
        {
            opening = false;

            AllyParty party = GameManager.Instance != null ? GameManager.Instance.party : null;
            if (party == null || InputManager.Instance == null)
            {
                callback?.Invoke(new List<ItemSO>(items));
                return;
            }

            session = new ItemAssignmentSession(items, BuildTargets(party), allowLeaveItems);
            session.OnChanged += Refresh;
            onClosed = callback;
            lastDirection = 0;
            BuildHints();

            isOpen = true;
            view.Show();
            Refresh();

            InputManager input = InputManager.Instance;
            if (input.CurrentMap != INPUTACTION_MAP.Rewards) mapBefore = input.CurrentMap;
            input.ChangeActionMap(INPUTACTION_MAP.Rewards);

            if (!clockPaused)
            {
                GameClock.RequestPause(CLOCK_PAUSE_REASON.Menu);
                clockPaused = true;
            }

            SubscribeInput();
        }

        // One target per party slot (left / mid / right); null when the slot has no member.
        private AssignTarget[] BuildTargets(AllyParty party)
        {
            var targets = new AssignTarget[AllyParty.MaxMembers];

            for (int i = 0; i < targets.Length; i++)
            {
                if (i >= party.ActiveCount) continue;

                PartyMemberData member = party.Members[i];
                if (member == null || member.definition == null) continue;

                UnitDefinitionSO definition = member.definition;
                targets[i] = new AssignTarget
                {
                    Name = definition.unitName,
                    Portrait = definition.partyPortrait != null ? definition.partyPortrait : definition.portrait,
                    GetStacks = () => PartyItems.GetStacks(member, itemCatalog),
                    Add = item => PartyItems.Add(member, item)
                };
            }

            return targets;
        }

        private void Close(List<ItemSO> leftover)
        {
            if (!isOpen) return;
            isOpen = false;

            UnsubscribeInput();
            StopHold();

            session.OnChanged -= Refresh;
            session = null;
            view.Hide();

            if (InputManager.Instance != null)
                InputManager.Instance.ChangeActionMap(mapBefore ?? INPUTACTION_MAP.Dungeon);
            mapBefore = null;

            if (clockPaused)
            {
                GameClock.ReleasePause(CLOCK_PAUSE_REASON.Menu);
                clockPaused = false;
            }

            Action<List<ItemSO>> callback = onClosed;
            onClosed = null;
            callback?.Invoke(leftover);
        }

        private void Refresh()
        {
            if (session == null || view == null) return;

            view.Render(session);
            UpdateHints();
        }

        // ── Input hints ───────────────────────────────────────────────────────

        private void BuildHints()
        {
            var map = InputManager.Instance.Rewards;
            string[] slotNames = { "Assign left", "Assign center", "Assign right" };
            InputAction[] slotActions = { map.AsigntoUnitLeft, map.AsigntoUnitMid, map.AsigntoUnitRight };

            hints.Clear();

            selectHint = new InputHint { key = KeyOf(map.Navigate), label = "Select" };
            hints.Add(selectHint);

            for (int i = 0; i < assignHints.Length; i++)
            {
                assignHints[i] = new InputHint { key = KeyOf(slotActions[i]), label = slotNames[i] };
                hints.Add(assignHints[i]);
            }

            backHint = new InputHint { key = KeyOf(map.CancelBack), label = "Undo" };
            hints.Add(backHint);

            confirmHint = new InputHint { key = KeyOf(map.Confirm), label = "Hold to continue" };
            hints.Add(confirmHint);
        }

        // Dims the pills whose action would do nothing right now.
        private void UpdateHints()
        {
            selectHint.enabled = session.Unassigned.Count > 1;

            for (int i = 0; i < assignHints.Length; i++)
                assignHints[i].enabled = session.Unassigned.Count > 0 && session.GetTarget(i) != null;

            bool canLeave = session.AllowLeave && !session.HasAssignments;
            backHint.label = canLeave ? "Leave" : "Undo";
            backHint.enabled = session.HasAssignments || session.AllowLeave;

            confirmHint.enabled = session.CanConfirm;

            view.SetHints(hints);
        }

        // Binding text as the Input System shows it; only the first one when there are several (keyboard | pad).
        private static string KeyOf(InputAction action)
        {
            string text = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
            int separator = text.IndexOf(" | ", StringComparison.Ordinal);
            return separator >= 0 ? text.Substring(0, separator) : text;
        }

        private void Confirm()
        {
            if (!isOpen) return;

            if (!session.TryCommit(out List<ItemSO> leftover))
            {
                Debug.Log($"[{nameof(ItemAssignationController)}] Assign every item before continuing.");
                return;
            }

            Close(leftover);
        }

        // Undo the last assignment; with nothing to undo and allowLeaveItems, leave without assigning.
        private void Cancel()
        {
            if (!isOpen || session.Undo()) return;

            if (session.AllowLeave) Close(new List<ItemSO>(session.Offered));
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void SubscribeInput()
        {
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

        private void UnsubscribeInput()
        {
            if (InputManager.Instance == null) return; // can be gone first when the app closes

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

        private void OnNavigate(InputAction.CallbackContext context)
        {
            // Edge detection: a held stick/key fires performed repeatedly, only a fresh push counts.
            float y = context.ReadValue<Vector2>().y;
            int direction = y > stickThreshold ? -1 : y < -stickThreshold ? 1 : 0; // up = previous item

            bool isNewStep = direction != 0 && direction != lastDirection;
            lastDirection = direction;

            if (isNewStep && isOpen) session.MoveSelection(direction);
        }

        private void OnAssignLeft(InputAction.CallbackContext context) => Assign(0);
        private void OnAssignMid(InputAction.CallbackContext context) => Assign(1);
        private void OnAssignRight(InputAction.CallbackContext context) => Assign(2);

        private void Assign(int slot)
        {
            if (isOpen) session.Assign(slot);
        }

        private void OnCancel(InputAction.CallbackContext context) => Cancel();

        // Confirm is a Hold in the asset: started = pressed, performed = held long enough, canceled = released.
        private void OnConfirmStarted(InputAction.CallbackContext context)
        {
            if (!isOpen || !session.CanConfirm) return;

            holdDuration = GetHoldDuration(context);
            holdStart = Time.unscaledTime;
            holding = true;
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            StopHold();
            Confirm();
        }

        private void OnConfirmCanceled(InputAction.CallbackContext context) => StopHold();

        private void StopHold()
        {
            holding = false;
            if (view != null) view.SetHoldProgress(0f);
        }

        private static float GetHoldDuration(InputAction.CallbackContext context)
        {
            float duration = context.interaction is HoldInteraction hold ? hold.duration : 0f;

            // Fully qualified on purpose: the generated "InputSystem" namespace shadows the class name.
            return duration > 0f
                ? duration
                : UnityEngine.InputSystem.InputSystem.settings.defaultHoldTime;
        }
    }
}