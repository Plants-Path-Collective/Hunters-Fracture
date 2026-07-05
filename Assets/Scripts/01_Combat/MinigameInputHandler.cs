using System;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CombatSystem.Minigame
{
    /// <summary>
    /// Listens to the Minigame action map during an active step window.
    /// Fires OnButtonPressed with the detected FACE_BUTTON so MinigameController
    /// can check whether it matches the required button for that step.
    ///
    /// Action map used: Minigame
    ///   PrimaryButton    → South  (Cross / A)
    ///   SecondaryButton  → East   (Circle / B)
    ///   TertiaryButton   → West   (Square / X)
    ///   QuaternaryButton → North  (Triangle / Y)
    /// </summary>
    public class MinigameInputHandler : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action<FACE_BUTTON> OnButtonPressed;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _listening = false;

        // ── Public API ────────────────────────────────────────────────────────

        public void StartListening()
        {
            if (_listening) return;

            // Switch to Minigame map — this disables Combat map automatically
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Minigame);

            _listening = true;
            Subscribe();
        }

        public void StopListening()
        {
            if (!_listening) return;

            _listening = false;
            Unsubscribe();

            // Return to Empty — CombatManager will set the correct map after resolving result
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty);
        }

        private void OnDestroy() => StopListening();

        // ── Subscription ──────────────────────────────────────────────────────

        private void Subscribe()
        {
            var mg = InputManager.Instance.Minigame;
            mg.PrimaryButton.performed    += OnPrimary;
            mg.SecondaryButton.performed  += OnSecondary;
            mg.TertiaryButton.performed   += OnTertiary;
            mg.QuaternaryButton.performed += OnQuaternary;
        }

        private void Unsubscribe()
        {
            if (InputManager.Instance == null) return;
            var mg = InputManager.Instance.Minigame;
            mg.PrimaryButton.performed    -= OnPrimary;
            mg.SecondaryButton.performed  -= OnSecondary;
            mg.TertiaryButton.performed   -= OnTertiary;
            mg.QuaternaryButton.performed -= OnQuaternary;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnPrimary   (InputAction.CallbackContext _) => Fire(FACE_BUTTON.Primary);
        private void OnSecondary (InputAction.CallbackContext _) => Fire(FACE_BUTTON.Secondary);
        private void OnTertiary  (InputAction.CallbackContext _) => Fire(FACE_BUTTON.Tertiary);
        private void OnQuaternary(InputAction.CallbackContext _) => Fire(FACE_BUTTON.Quaternary);

        private void Fire(FACE_BUTTON button)
        {
            if (!_listening) return;
            OnButtonPressed?.Invoke(button);
        }
    }
}