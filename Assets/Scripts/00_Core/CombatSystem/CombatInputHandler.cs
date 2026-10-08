using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Core.CombatSystem.Units;
using Core.UI;
using InputSystem;

namespace Core.CombatSystem
{
    /// <summary>
    /// Translates Combat action map input into CombatController / ActionResolver calls.
    /// Lives in CombatStage, so it is destroyed with the scene. It never enables or disables
    /// action maps: CombatTransition (or a SceneSetter) owns that.
    /// </summary>
    /// <remarks>
    /// Target-first flow: Locked (enemy turn / resolving) → Ready (ally's turn). While Ready, a
    /// target is always highlighted from the first frame of the turn; the stick moves it, and an
    /// action hotkey (Basic Attack) is applied to whatever is highlighted. Defend and Flee ignore it.
    /// </remarks>
    [RequireComponent(typeof(CombatController), typeof(ActionResolver))]
    public class CombatInputHandler : MonoBehaviour
    {
        private enum InputState { Locked, Ready }

        [Header("── Target Selection ───────────────────────────")]
        [Tooltip("Flip the stick direction. By default right (D) moves forward along the ring: " +
                "enemies left → right, then allies right → left.")]
        [SerializeField] private bool invertTargetDirection;
        [Tooltip("How far the stick must be pushed before it counts as one step left/right.")]
        [SerializeField, Range(0.1f, 0.9f)] private float stickThreshold = 0.5f;

        /// <summary>Fired when the highlighted target changes. Null = highlight closed.</summary>
        public event Action<Unit> OnTargetChanged;

        /// <summary>A hold-to-confirm action started being held. The float is the hold duration in
        /// seconds (show the charge indicator).</summary>
        public event Action<InputAction, float> OnHoldStarted;

        /// <summary>The hold ended: released early, or finished (hide the charge indicator).</summary>
        public event Action<InputAction> OnHoldCanceled;

        private CombatController combat;
        private ActionResolver resolver;

        private InputState state = InputState.Locked;

        /// <summary>Every living unit of both sides in ring order (see CombatSlots.RingPosition).</summary>
        private List<Unit> targets = new();
        private int targetIndex;
        private int lastTargetDirection;

        /// <summary>Last highlighted ENEMY. The next ally turn starts on it if it is still alive, since
        /// Basic Attack (the most common action) can only hit enemies.</summary>
        private Unit lastEnemyTarget;

        private Unit CurrentTarget =>
            targetIndex >= 0 && targetIndex < targets.Count ? targets[targetIndex] : null;

        /// <summary>An action was attempted on a target it cannot hit (e.g. Basic Attack on an ally).
        /// Hook for UI feedback; the turn is not spent.</summary>
        public event Action<Unit> OnTargetRejected;

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

            input.BasicAttack.performed += OnBasicAttack;
            input.Defend.performed += OnDefend;
            input.Skills.performed += OnSkills;
            input.Backpack.performed += OnBackpack;
            input.Flee.performed += OnFlee;
            input.CastUltimate.performed += OnCastUltimate;
            // ConfirmAction is intentionally not subscribed: with target-first, the action
            // hotkey itself confirms. Remove it from the asset, or repurpose it later.

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

            input.BasicAttack.performed -= OnBasicAttack;
            input.Defend.performed -= OnDefend;
            input.Skills.performed -= OnSkills;
            input.Backpack.performed -= OnBackpack;
            input.Flee.performed -= OnFlee;
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
                OpenTargetSelection(); // target-first: the highlight is up from the start of the turn
            else
                Lock(); // enemy turn: no player input
        }

        private void OnCombatEnd(COMBAT_OUTCOME outcome)
        {
            lastEnemyTarget = null;
            Lock();
        }

        private void Lock()
        {
            if (state == InputState.Ready)
                OnTargetChanged?.Invoke(null);

            targets.Clear();
            targetIndex = 0;
            state = InputState.Locked;
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void OnBasicAttack(InputAction.CallbackContext context)
        {
            Unit target = CurrentTarget;
            if (state == InputState.Locked || target == null) return;

            Unit actor = combat.CurrentActor;

            // The cursor can rest on any unit, but Basic Attack only hits enemies.
            if (target.Team == actor.Team)
            {
                OnTargetRejected?.Invoke(target);
                return;
            }

            // Lock BEFORE calling: EndTurn() runs synchronously inside ResolveAttack(), and the
            // next OnTurnStart (if it is an ally) must be the one that unlocks us again.
            Lock();

            // Invalid target (e.g. died in the meantime): reopen the highlight, keep the turn.
            if (!resolver.ResolveAttack(actor, target))
                OpenTargetSelection();
        }

        private void OnDefend(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;

            Unit actor = combat.CurrentActor;
            Lock(); // same reasoning as OnBasicAttack: Defend() ends the turn synchronously
            combat.Defend(actor);
        }

        private void OnFlee(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;

            Unit actor = combat.CurrentActor;
            Lock(); // success ends the combat and failure ends the turn, both synchronously
            combat.Flee(actor);
        }

        private void OnSkills(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;

            if (CombatUIManager.Instance == null)
            {
                Debug.LogError($"[{nameof(CombatInputHandler)}] {nameof(CombatUIManager)}.Instance is null.");
                return;
            }

            CombatUIManager.Instance.ToggleSkillsPanel();
        }

        private void OnBackpack(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;

            if (CombatUIManager.Instance == null)
            {
                Debug.LogError($"[{nameof(CombatInputHandler)}] {nameof(CombatUIManager)}.Instance is null.");
                return;
            }

            CombatUIManager.Instance.ToggleBackpackPanel();
        }

        private void OnCastUltimate(InputAction.CallbackContext context)
        {
            if (state == InputState.Locked) return;
            Debug.Log($"[{nameof(CombatInputHandler)}] Ultimate not implemented yet.");
        }

        // ── Target selection ──────────────────────────────────────────────────

        /// <summary>
        /// Builds the ring of living units and highlights one: the previous enemy target if it is
        /// still alive, otherwise the first enemy. Dead allies are left out until a DeadAlly target
        /// type exists (revive skills).
        /// </summary>
        private void OpenTargetSelection()
        {
            if (combat.CurrentActor == null) return;

            List<Unit> allies = AliveOf(UNIT_TEAM.Ally);
            List<Unit> enemies = AliveOf(UNIT_TEAM.Enemy);

            // One flat ring around the arena as seen from the camera: the enemy row left → right, then
            // the ally row right → left, wrapping back to the first enemy.
            targets = AliveOf(UNIT_TEAM.Enemy).Concat(AliveOf(UNIT_TEAM.Ally)).ToList();

            state = InputState.Ready;

            if (targets.Count == 0)
            {
                OnTargetChanged?.Invoke(null);
                return;
            }

            int remembered = lastEnemyTarget != null ? targets.IndexOf(lastEnemyTarget) : -1;
            int firstEnemy = targets.FindIndex(u => u.Team == UNIT_TEAM.Enemy);

            SetTarget(remembered >= 0 ? remembered : Mathf.Max(0, firstEnemy));
        }

        private List<Unit> AliveOf(UNIT_TEAM team) =>
            combat.Roster
                .Where(u => u.Team == team && u.IsAlive)
                // Units without a slot (debug-only path) go last instead of silently disappearing.
                .OrderBy(u => u.Slot.HasValue ? CombatSlots.RingPosition(team, u.Slot.Value) : int.MaxValue)
                .ToList();

        private void SetTarget(int index)
        {
            targetIndex = index;
            Unit target = targets[index];

            if (target.Team == UNIT_TEAM.Enemy)
                lastEnemyTarget = target;

            OnTargetChanged?.Invoke(target);
        }

        private void OnTargetSelection(InputAction.CallbackContext context)
        {
            // Value action: a held stick fires performed repeatedly, so only a fresh push past the
            // threshold counts as one step (edge detection). canceled reads (0,0) and resets it.
            float x = context.ReadValue<Vector2>().x;
            int direction = x > stickThreshold ? 1 : x < -stickThreshold ? -1 : 0;

            bool isNewStep = direction != 0 && direction != lastTargetDirection;
            lastTargetDirection = direction;

            if (!isNewStep || state != InputState.Ready || targets.Count < 2) return;

            if (invertTargetDirection) direction = -direction;

            // Wraps around: the row is a ring.
            SetTarget((targetIndex + direction + targets.Count) % targets.Count);
        }

        // ── Hold feedback ─────────────────────────────────────────────────────
        #region HoldFeedback

        private static IEnumerable<InputAction> HoldActions(InputSystem_Actions.CombatActions input)
        {
            yield return input.BasicAttack;
            yield return input.Defend;
            yield return input.Skills;
            yield return input.Backpack;
            yield return input.Flee;
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