using UnityEngine;
using InputSystem;

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
    /// Switch maps:
    ///   InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Combat);
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static InputManager Instance { get; private set; }

        // ── Generated asset ───────────────────────────────────────────────────
        public InputSystem_Actions Actions { get; private set; }

        // ── Map shorthands ────────────────────────────────────────────────────
        // UI
        public InputSystem_Actions.UIActions          UI          => Actions.UI;

        // Exploration
        //   Move            → leftStick / WASD
        //   Interact        → buttonWest (□/X) / E
        //   OpenInventory   → buttonNorth (△/Y) / I
        //   OpenPauseMenu   → touchpad / select / I
        public InputSystem_Actions.ExplorationActions Exploration => Actions.Exploration;

        // Dialogue
        //   Move                        → leftStick / WASD
        //   CompleteLineConfirmSelection → advance dialogue / confirm choice
        //   SkipConversation            → Hold to skip
        //   AbandonConversation         → Hold to abandon
        public InputSystem_Actions.DialogueActions    Dialogue    => Actions.Dialogue;

        // Combat
        //   TargetSelection  → navigate targets
        //   MoveinMenu       → navigate action menu
        //   BasicAttack      → confirm / attack shortcut
        //   BackfromMenu     → cancel / go back in menu
        //   OpenSkillsMenu   → open skill selection
        //   OpenInventory    → open item inventory
        //   LeftShoulder     → used combined with RightShoulder to charge Ultimate (5s hold)
        //   RightShoulder    → used combined with LeftShoulder to charge Ultimate (5s hold)
        public InputSystem_Actions.CombatActions      Combat      => Actions.Combat;

        // ── State ─────────────────────────────────────────────────────────────
        public INPUTACTION_MAP CurrentMap { get; private set; } = INPUTACTION_MAP.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Actions = new InputSystem_Actions();
            Actions.Disable();
        }

        private void OnDestroy()
        {
            Actions?.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────────

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
                case INPUTACTION_MAP.Empty:       /* all maps disabled */         break;
                case INPUTACTION_MAP.UI:          Actions.UI.Enable();            break;
                case INPUTACTION_MAP.Exploration: Actions.Exploration.Enable();   break;
                case INPUTACTION_MAP.Dialogue:    Actions.Dialogue.Enable();      break;
                case INPUTACTION_MAP.Combat:      Actions.Combat.Enable();        break;
            }
        }

        /// <summary>
        /// Switches to UI map, remembering the previous map so PopUIMap() can restore it.
        /// Useful for opening menus during Exploration or Combat.
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

        // ── Internals ─────────────────────────────────────────────────────────
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