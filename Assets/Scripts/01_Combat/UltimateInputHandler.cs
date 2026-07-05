using System;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CombatSystem
{
    /// <summary>
    /// Detects the Ultimate input: both shoulders held simultaneously for 5 seconds.
    /// Lives on the same GameObject as TurnHandler.
    /// Only active while it's an ally's turn (enabled/disabled by TurnHandler).
    ///
    /// Action map: Combat
    ///   LeftShoulder  + RightShoulder held for ultimateHoldDuration → OnUltimateCharged
    ///
    /// The progress (0–1) is broadcast each frame via OnUltimateProgress so the UI
    /// can show a charge bar.
    /// </summary>
    public class UltimateInputHandler : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired when both shoulders have been held for the full duration.</summary>
        public event Action OnUltimateCharged;

        /// <summary>Broadcast each frame while charging (0 = start, 1 = ready).</summary>
        public event Action<float> OnUltimateProgress;

        /// <summary>Fired when the charge is cancelled (a shoulder was released early).</summary>
        public event Action OnUltimateCancelled;

        // ── Config ────────────────────────────────────────────────────────────
        [Tooltip("Seconds both shoulders must be held to trigger the Ultimate.")]
        [SerializeField] private float ultimateHoldDuration = 5f;

        // ── State ─────────────────────────────────────────────────────────────
        private bool  _leftHeld;
        private bool  _rightHeld;
        private float _holdTimer;
        private bool  _charged;       // guard — fire event only once per charge
        private bool  _listening;

        // ── Public API ────────────────────────────────────────────────────────

        public void StartListening()
        {
            if (_listening) return;
            _listening  = true;
            _leftHeld   = false;
            _rightHeld  = false;
            _holdTimer  = 0f;
            _charged    = false;
            Subscribe();
        }

        public void StopListening()
        {
            if (!_listening) return;
            _listening = false;
            Unsubscribe();
            ResetCharge();
        }

        // ── Subscription ──────────────────────────────────────────────────────

        private void Subscribe()
        {
            var combat = InputManager.Instance.Combat;
            combat.LeftShoulder.started   += OnLeftStarted;
            combat.LeftShoulder.canceled  += OnLeftCanceled;
            combat.RightShoulder.started  += OnRightStarted;
            combat.RightShoulder.canceled += OnRightCanceled;
        }

        private void Unsubscribe()
        {
            if (InputManager.Instance == null) return;
            var combat = InputManager.Instance.Combat;
            combat.LeftShoulder.started   -= OnLeftStarted;
            combat.LeftShoulder.canceled  -= OnLeftCanceled;
            combat.RightShoulder.started  -= OnRightStarted;
            combat.RightShoulder.canceled -= OnRightCanceled;
        }

        private void OnDestroy() => StopListening();

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnLeftStarted  (InputAction.CallbackContext _) => _leftHeld  = true;
        private void OnRightStarted (InputAction.CallbackContext _) => _rightHeld = true;

        private void OnLeftCanceled(InputAction.CallbackContext _)
        {
            _leftHeld = false;
            if (_holdTimer > 0f && !_charged) CancelCharge();
        }

        private void OnRightCanceled(InputAction.CallbackContext _)
        {
            _rightHeld = false;
            if (_holdTimer > 0f && !_charged) CancelCharge();
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_listening || _charged) return;

            if (_leftHeld && _rightHeld)
            {
                _holdTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_holdTimer / ultimateHoldDuration);
                OnUltimateProgress?.Invoke(progress);

                if (_holdTimer >= ultimateHoldDuration)
                {
                    _charged = true;
                    OnUltimateCharged?.Invoke();
                }
            }
            else if (_holdTimer > 0f)
            {
                // One shoulder released — cancel and reset
                CancelCharge();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void CancelCharge()
        {
            ResetCharge();
            OnUltimateCancelled?.Invoke();
        }

        private void ResetCharge()
        {
            _holdTimer = 0f;
            _charged   = false;
        }
    }
}