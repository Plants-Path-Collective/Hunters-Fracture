using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using Core;
using Core.Clock;
using Player;

namespace World
{
    /// <summary>
    /// A door between scenes. While the player stands inside its trigger, pressing the Interact
    /// action of the configured map loads the target scene. An optional <see cref="PhaseGate"/>
    /// can close it depending on the time of day (e.g. the dungeon entrance is open only in the afternoon).
    /// Use one door per direction: the entrance in the social area (gated) and the exit in the dungeon.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneDoor : MonoBehaviour, IPlayerInteractable
    {
        [Header("Destination")]
        [Tooltip("Name of the scene to load. It must be enabled in Build Settings.")]
        [SerializeField] private string targetScene = "Overworld - CombatDemo1";

        [Header("Interaction")]
        [Tooltip("Tag of the player object that can use this door.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Map whose Interact action opens the door. Social for the social area, Overworld for the dungeon.")]
        [SerializeField] private INPUTACTION_MAP interactMap = INPUTACTION_MAP.Social;

        [Header("Rules")]
        [Tooltip("Optional. If assigned and closed, the door refuses to open. Leave empty for an always-open door.")]
        [SerializeField] private PhaseGate phaseGate;

        [Header("Events")]
        [Tooltip("Raised when the player enters or leaves the trigger. Use it to show or hide an interact prompt.")]
        public UnityEvent<bool> onPlayerInRangeChanged;

        [Tooltip("Raised when the player tries to open a closed door. Receives the localized reason.")]
        public UnityEvent<LocalizedString> onEntryDenied;

        private bool playerInRange;
        private bool entering;
        public Transform InteractionPoint => transform;

        private void Reset()
        {
            // The door detects the player with a trigger collider.
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
            {
                Debug.Log($"[SceneDoor] Ignored trigger enter from '{other.name}' (tag: {other.tag}).", this);
                return;
            }

            Debug.Log($"[SceneDoor] Player entered '{name}' trigger.", this);
            SetPlayerInRange(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag))
            {
                Debug.Log($"[SceneDoor] Ignored trigger exit from '{other.name}' (tag: {other.tag}).", this);
                return;
            }

            Debug.Log($"[SceneDoor] Player exited '{name}' trigger.", this);
            SetPlayerInRange(false);
        }

        private void OnDisable()
        {
            SetPlayerInRange(false);
        }

        private void SetPlayerInRange(bool value)
        {
            if (playerInRange == value) return;
            playerInRange = value;
            onPlayerInRangeChanged?.Invoke(value);
        }

        public void Interact()
        {
            string currentMap = InputManager.Instance != null
                ? InputManager.Instance.CurrentMap.ToString()
                : "unavailable";
            Debug.Log(
                $"[SceneDoor] Interact received by '{name}'. In range: {playerInRange}; " +
                $"entering: {entering}; current map: {currentMap}; required map: {interactMap}.", this);

            if (!playerInRange || entering) return;
            if (InputManager.Instance == null || InputManager.Instance.CurrentMap != interactMap)
            {
                Debug.LogWarning($"[SceneDoor] Ignoring interaction because the required action map is not active.", this);
                return;
            }

            Debug.Log($"[SceneDoor] Player attempted to enter '{name}' (destination: '{targetScene}').", this);

            if (phaseGate != null && !phaseGate.IsOpen)
            {
                string phase = GameClock.Instance != null
                    ? GameClock.Instance.CurrentPhase.ToString()
                    : "unknown";
                LocalizedString denialMessage = phaseGate.GetClosedMessage();

                Debug.LogWarning($"[SceneDoor] Entry denied: '{name}' is closed in phase {phase}.", this);
                Debug.Log($"[SceneDoor] Entry denied reason: {denialMessage.GetLocalizedString()}", this);
                onEntryDenied?.Invoke(denialMessage);
                return;
            }

            LoadTarget();
        }

        private void LoadTarget()
        {
            entering = true;

            // Freeze the clock and the input while the scene loads. The target scene's SceneSetter
            // selects the right map again in Start().
            GameClock.RequestPause(CLOCK_PAUSE_REASON.Transition);
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty);

            AsyncOperation load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);

            if (load == null)
            {
                // LoadSceneAsync returns null (and logs an error) when the scene is not in Build Settings.
                Debug.LogError($"[SceneDoor] Could not load scene '{targetScene}'. Is it enabled in Build Settings?", this);
                GameClock.ReleasePause(CLOCK_PAUSE_REASON.Transition);
                InputManager.Instance.ChangeActionMap(interactMap);
                entering = false;
                return;
            }

            // This door is destroyed by the load, so release the pause from a callback that does not depend on it.
            load.completed += _ => GameClock.ReleasePause(CLOCK_PAUSE_REASON.Transition);
        }
    }
}