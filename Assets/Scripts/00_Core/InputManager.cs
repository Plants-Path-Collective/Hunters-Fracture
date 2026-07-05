using UnityEngine;
using InputSystem; // Generated wrapper namespace

namespace Core
{
    /// <summary>
    /// Singleton that owns the InputSystem_Actions asset and exposes each action map.
    /// Lives on the persistent GameManager GameObject and survives scene loads.
    ///
    /// Usage — subscribe from any system:
    ///   InputManager.Instance.Exploration.Move.performed += OnMove;
    ///   InputManager.Instance.Combat.BasicAttack.performed += OnAttack;
    ///
    /// Switch maps from a SceneSetter or anywhere:
    ///   InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Exploration);
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static InputManager Instance { get; private set; }

        // ── Generated asset ──────────────────────────────────────────────────
        // Read-only public access so any system can reach its map directly,
        // e.g. InputManager.Instance.Combat.BasicAttack.performed += ...
        public InputSystem_Actions Actions { get; private set; }

        // Convenience shorthand properties — avoids typing Actions.Xxx every time
        public InputSystem_Actions.UIActions       UI          => Actions.UI;
        public InputSystem_Actions.ExplorationActions Exploration => Actions.Exploration;
        public InputSystem_Actions.DialogueActions  Dialogue    => Actions.Dialogue;
        public InputSystem_Actions.CombatActions    Combat      => Actions.Combat;

        // ── State ────────────────────────────────────────────────────────────
        public INPUTACTION_MAP CurrentMap { get; private set; } = INPUTACTION_MAP.Empty;

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            // Destroy the duplicate component, NOT the whole GameObject.
            // (The GameObject holds GameManager and other persistent systems.)
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Actions = new InputSystem_Actions();
            Actions.Disable(); // Start with everything off; SceneSetter will enable the right map.
        }

        private void OnDestroy()
        {
            // Clean up unmanaged InputSystem resources.
            Actions?.Dispose();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Disables every map, then enables only the requested one.
        /// Safe to call multiple times with the same map.
        /// </summary>
        public void ChangeActionMap(INPUTACTION_MAP newMap)
        {
            if (CurrentMap == newMap) return;

            DisableAllMaps();

            CurrentMap = newMap;

            switch (newMap)
            {
                case INPUTACTION_MAP.UI:          Actions.UI.Enable();          break;
                case INPUTACTION_MAP.Exploration: Actions.Exploration.Enable(); break;
                case INPUTACTION_MAP.Dialogue:    Actions.Dialogue.Enable();    break;
                case INPUTACTION_MAP.Combat:      Actions.Combat.Enable();      break;
                case INPUTACTION_MAP.Empty:       /* intentionally blank */      break;
            }
        }

        /// <summary>
        /// Convenience: switches to UI map and remembers the previous map so
        /// you can restore it after closing a menu.
        /// </summary>
        public void PushUIMap()
        {
            _previousMap = CurrentMap;
            ChangeActionMap(INPUTACTION_MAP.UI);
        }

        /// <summary>
        /// Restores the map that was active before the last PushUIMap() call.
        /// </summary>
        public void PopUIMap()
        {
            ChangeActionMap(_previousMap);
        }

        // ── Internals ────────────────────────────────────────────────────────
        private INPUTACTION_MAP _previousMap = INPUTACTION_MAP.Exploration;

        private void DisableAllMaps()
        {
            Actions.UI.Disable();
            Actions.Exploration.Disable();
            Actions.Dialogue.Disable();
            Actions.Combat.Disable();
        }
    }
}