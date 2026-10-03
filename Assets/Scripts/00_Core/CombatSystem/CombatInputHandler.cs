using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using InputSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem
{
    /// <summary>
    /// Translates Combat action map input into CombatController / ActionResolver calls.
    /// Lives in CombatStage, so it is destroyed with the scene. It never enables or disables
    /// action maps: CombatTransition (or a SceneSetter) owns that.
    /// </summary>
    /// <remarks>
    /// Flow: Locked (enemy turn / resolving) → Root (ally's turn, waiting for a hotkey) →
    /// SelectingTarget (after Basic Attack) → Confirm resolves the attack. Every action is a
    /// direct hotkey, so pressing another one while selecting a target simply switches to it.
    /// </remarks>
    [RequireComponent(typeof(CombatController), typeof(ActionResolver))]
    public class CombatInputHandler : MonoBehaviour
    {
        private enum InputState { Locked, Root, SelectingTarget }

        [Header("── Target Selection ───────────────────────────")]
        [Tooltip("How far the stick must be pushed before it counts as one step left/right.")]
        [SerializeField, Range(0.1f, 0.9f)] private float stickThreshold = 0.5f;
        [Tooltip("Flip left/right if a lower SlotIndex is not on the left side of the screen.")]
        [SerializeField] private bool invertTargetDirection;

        /// <summary>Fired when the highlighted target changes. Null = selection closed.</summary>
        public event Action<Unit> OnTargetChanged;

        /// <summary>A hold-to-confirm action started being held. The float is the hold duration in
        /// seconds (show the charge indicator).</summary>
        public event Action<InputAction, float> OnHoldStarted;

        /// <summary>The hold ended: released early, or finished (hide the charge indicator).</summary>
        public event Action<InputAction> OnHoldCanceled;

        private CombatController combat;
        private ActionResolver resolver;

        private InputState state = InputState.Locked;
        private List<Unit> targets = new();
        private int targetIndex;
        private int lastTargetDirection;

        private void Awake()
        {
            combat = GetComponent<CombatController>();
            resolver = GetComponent<ActionResolver>();
        }

        private void OnEnable()
        {
            if (InputManager.Instance == null)
            {
                Debug.LogError($"[{nameof(CombatInputHandler)}] InputManager.Instance is null. " +
                               "Is the GameManager present in the scene flow?");
                return;
            }

            var input = InputManager.Instance.Combat;

            input.Flee.performed += OnFlee;
            input.BasicAttack.performed += OnBasicAttack;
            input.Defend.performed += OnDefend;
            input.Skills.performed += OnSkills;
            input.Backpack.performed += OnBackpack;
            input.ConfirmAction.performed += OnConfirmAction;
            input.CastUltimate.performed += OnCastUltimate;

            input.TargetSelection.performed += OnTargetSelection;
            input.TargetSelection.canceled += OnTargetSelection; // resets the edge detection

            // ── Hold feedback ────────────────────────────────────────────
            // Needed only while these actions use the Hold interaction in the asset.
            // No-hold variant: remove "Hold" from them in InputSystem_Actions and delete this
            // block + the region below. The performed subscriptions above work unchanged
            // (they fire on press instead of after the hold).
            foreach (InputAction action in HoldActions(input))
            {
                action.started += OnHoldActionStarted;
                action.canceled += OnHoldActionCanceled;
            }

            combat.OnTurnStart += OnTurnStart;
            combat.OnCombatEnd += OnCombatEnd;
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.OnTurnStart -= OnTurnStart;
                combat.OnCombatEnd -= OnCombatEnd;
            }

            // InputManager is DontDestroyOnLoad and can be gone first when the app closes.
            if (InputManager.Instance == null) return;

            var input = InputManager.Instance.Combat;

            input.Flee.performed -= OnFlee;
            input.BasicAttack.performed -= OnBasicAttack;
            input.Defend.performed -= OnDefend;
            input.Skills.performed -= OnSkills;
            input.Backpack.performed -= OnBackpack;
            input.ConfirmAction.performed -= OnConfirmAction;
            input.CastUltimate.performed -= OnCastUltimate;

            input.TargetSelection.performed -= OnTargetSelection;
            input.TargetSelection.canceled -= OnTargetSelection;

            foreach (InputAction action in HoldActions(input))
            {
                action.started -= OnHoldActionStarted;
                action.canceled -= OnHoldActionCanceled;
            }
        }

        // ── Turn flow ─────────────────────────────────────────────────────────

        private void OnTurnStart(Unit actor)
        {
            if (actor.Team == UNIT_TEAM.Ally)
            {
                CloseTargetSelection();
                state = InputState.Root;
            }
            else
            {
                Lock(); // enemy turn: no player input
            }
        }

        private void OnCombatEnd(COMBAT_OUTCOME outcome) => Lock();

        private void Lock()
        {
            CloseTargetSelection();
            state = InputState.Locked;
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void OnFlee(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;

            Unit actor = combat.CurrentActor;
            // Lock BEFORE calling: success ends the combat and failure ends the turn,
            // both synchronously, so the next OnTurnStart must be what unlocks us.
            Lock();
            combat.Flee(actor);
        }
        
        // TEMP: debug logs. Remove once target selection is confirmed working.
        private void OnBasicAttack(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked)
            {
                Debug.Log($"[{nameof(CombatInputHandler)}] BasicAttack ignored: Locked " +
                        $"(current actor: {combat.CurrentActor?.Name}).");
                return;
            }
            OpenTargetSelection();
        }

        private void OnDefend(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;

            Unit actor = combat.CurrentActor;
            // Lock BEFORE calling: Defend() ends the turn synchronously, and the next
            // OnTurnStart (if it is an ally) must be the one that unlocks us again.
            Lock();
            combat.Defend(actor);
        }

        private void OnSkills(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;
            Debug.Log($"[{nameof(CombatInputHandler)}] Skills menu not implemented yet (needs ActionSO).");
        }

        private void OnBackpack(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;
            Debug.Log($"[{nameof(CombatInputHandler)}] Backpack is disabled until the system exists.");
        }

        private void OnCastUltimate(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;
            Debug.Log($"[{nameof(CombatInputHandler)}] Ultimate not implemented yet.");
        }

        private void OnConfirmAction(InputAction.CallbackContext context)
        {
            if (state != InputState.SelectingTarget)
            {
                Debug.Log($"[{nameof(CombatInputHandler)}] Confirm ignored: state is {state}.");
                return;
            }

            Unit actor = combat.CurrentActor;
            Unit target = targets[targetIndex];

            Lock(); // same reasoning as OnDefend: EndTurn() runs inside ResolveAttack()

            // Invalid target (e.g. died in the meantime): reopen the selection, keep the turn.
            if (!resolver.ResolveAttack(actor, target))
                OpenTargetSelection();
        }

        // ── Target selection ──────────────────────────────────────────────────

        private void OpenTargetSelection()
        {
            Unit actor = combat.CurrentActor;
            if (actor == null) return;

            targets = combat.Roster
                .Where(u => u.Team != actor.Team && u.IsAlive)
                .OrderBy(u => u.SlotIndex)
                .ToList();

            if (targets.Count == 0) return;

            Debug.Log($"[{nameof(CombatInputHandler)}] Target selection: {targets.Count} target(s).");

            targetIndex = 0;
            state = InputState.SelectingTarget;
            OnTargetChanged?.Invoke(targets[targetIndex]);
        }

        private void CloseTargetSelection()
        {
            bool wasSelecting = state == InputState.SelectingTarget;

            targets.Clear();
            targetIndex = 0;

            if (wasSelecting)
                OnTargetChanged?.Invoke(null);
        }

        private void OnTargetSelection(InputAction.CallbackContext context)
        {
            // Value action: a held stick fires performed repeatedly, so only a fresh push past
            // the threshold counts as one step (edge detection). canceled reads (0,0) and resets it.
            float x = context.ReadValue<Vector2>().x;
            int direction = x > stickThreshold ? 1 : x < -stickThreshold ? -1 : 0;

            bool isNewStep = direction != 0 && direction != lastTargetDirection;
            lastTargetDirection = direction;

            if (!isNewStep || state != InputState.SelectingTarget) return;

            if (invertTargetDirection) direction = -direction;

            int next = Mathf.Clamp(targetIndex + direction, 0, targets.Count - 1);
            if (next == targetIndex) return;

            targetIndex = next;
            Debug.Log($"[{nameof(CombatInputHandler)}] Target → {targets[targetIndex].Name} " +
                $"(slot {targets[targetIndex].SlotIndex}).");

            OnTargetChanged?.Invoke(targets[targetIndex]);
        }

        // ── Hold feedback ─────────────────────────────────────────────────────
        #region HoldFeedback

        private static IEnumerable<InputAction> HoldActions(InputSystem_Actions.CombatActions input)
        {
            yield return input.Flee;
            yield return input.BasicAttack;
            yield return input.Defend;
            yield return input.Skills;
            yield return input.Backpack;
            yield return input.CastUltimate;
        }

        private void OnHoldActionStarted(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;
            OnHoldStarted?.Invoke(context.action, GetHoldDuration(context));
        }

        /// <summary>Reads the duration configured in the asset. HoldInteraction.duration is 0 when the
        /// binding does not override it, which means the project-wide default hold time.</summary>
        private static float GetHoldDuration(InputAction.CallbackContext context)
        {
            float duration = context.interaction is HoldInteraction hold ? hold.duration : 0f;

            // Fully qualified on purpose: the generated "InputSystem" namespace shadows the class name.
            return duration > 0f
                ? duration
                : UnityEngine.InputSystem.InputSystem.settings.defaultHoldTime;
        }

        // Fires on early release AND on release after a completed hold: both hide the indicator.
        private void OnHoldActionCanceled(InputAction.CallbackContext context)
        {
            OnHoldCanceled?.Invoke(context.action);
        }

        #endregion
    }
}