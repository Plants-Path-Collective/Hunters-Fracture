using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using Core.CombatSystem.Units;
using Dungeon;

namespace World
{
    /// <summary>
    /// Dungeon exit back to the Social scene. Listens to the Dungeon map's Interact action and,
    /// before loading, hands the current party (HP/SP and inventories included) from PlayerEntity
    /// to GameManager. The destination spawn is configured with SceneDoor's "Target Spawn Id".
    /// </summary>
    public class EmergencyExit : SceneDoor
    {
        private bool subscribed;

        private void OnEnable() => Subscribe();

        // InputManager may not exist yet in OnEnable if the scene starts before the persistent objects.
        private void Start() => Subscribe();

        protected override void OnDisable()
        {
            base.OnDisable();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || InputManager.Instance == null) return;

            InputManager.Instance.Dungeon.Interact.performed += OnInteractPerformed;
            subscribed = true;
            Debug.Log("[EmergencyExit] Subscribed to interact action.");
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;

            // The InputManager can be destroyed before this object (e.g. on application quit).
            if (InputManager.Instance != null)
                InputManager.Instance.Dungeon.Interact.performed -= OnInteractPerformed;

            subscribed = false;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            // SceneDoor.Interact() ignores the call unless the player is in range,
            // the door is not already loading, and the Dungeon map is active.
            Interact();
        }

        protected override bool OnBeforeLoad()
        {
            if (PlayerEntity.Instance == null || GameManager.Instance == null)
            {
                Debug.LogError("[EmergencyExit] PlayerEntity or GameManager is missing; the party was not saved.", this);
                return false;
            }

            AllyParty party = PlayerEntity.Instance.Party;
            GameManager.Instance.SetCurrentParty(party);
            Debug.Log($"[EmergencyExit] Saved party to GameManager ({party.Count} members).", this);
            return true;
        }
    }
}