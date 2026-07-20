using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace World
{
    /// <summary>
    /// Place this component on a trigger collider that marks the boundary of a camera area.
    /// When the player enters, the assigned virtual camera is activated (priority raised).
    /// When the player exits back to a previous area, it's deactivated (priority lowered).
    ///
    /// Setup per area:
    ///   1. Create an empty GameObject with a collider set as Trigger.
    ///   2. Add this component and assign the CinemachineCamera for this area.
    ///   3. The CinemachineBrain on the main camera handles the blend 
    ///
    /// Priority convention used here:
    ///   Active area   → priority 10
    ///   Inactive area → priority  0
    /// </summary>
    public class AreaCameraZone : MonoBehaviour
    {
        [FormerlySerializedAs("virtualCamera")]
        [Tooltip("The CinemachineCamera that covers this area.")]
        [SerializeField] private CinemachineCamera cinemachineCamera;

        [Header("Priority")]
        [SerializeField] private int activePriority   = 10;
        [SerializeField] private int inactivePriority =  0;

        [Header("Player Detection")]
        [Tooltip("Tag used to identify the player GameObject.")]
        [SerializeField] private string playerTag = "Player";

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (cinemachineCamera == null)
            {
                Debug.LogError($"[AreaCameraZone] '{gameObject.name}' has no CinemachineCamera assigned.");
                return;
            }

            // Start inactive — the first area's zone should have its cinemachinecamera
            // already set to a higher priority in the Inspector, or call
            // Activate() from the SceneSetter.
            cinemachineCamera.Priority = inactivePriority;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            Activate();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            Deactivate();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Raises this zone's virtual camera priority so Cinemachine makes it live.
        /// The Brain handles the blend automatically.
        /// </summary>
        public void Activate()
        {
            if (cinemachineCamera == null) return;
            cinemachineCamera.Priority = activePriority;
        }

        /// <summary>
        /// Lowers priority so Cinemachine falls back to whichever vcam is next highest.
        /// </summary>
        public void Deactivate()
        {
            if (cinemachineCamera == null) return;
            cinemachineCamera.Priority = inactivePriority;
        }
    }
}
