using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Render
{
    /// <summary>
    /// Shared behavior for any trigger-based camera zone: raises its CinemachineCamera's
    /// priority when the Player enters, lowers it when the Player exits. The CinemachineBrain
    /// on the main camera handles the actual blend between cameras automatically as priorities
    /// change — this class never touches the blend itself.
    /// </summary>
    public abstract class CameraZone : MonoBehaviour
    {
        [FormerlySerializedAs("virtualCamera")]
        [Tooltip("The CinemachineCamera that covers this zone.")]
        [SerializeField] protected CinemachineCamera cinemachineCamera;

        [Header("Priority")]
        [SerializeField] protected int activePriority   = 10;
        [SerializeField] protected int inactivePriority =  0;

        [Header("Player Detection")]
        [Tooltip("Tag used to identify the player GameObject.")]
        [SerializeField] protected string playerTag = "Player";

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            if (cinemachineCamera == null)
            {
                Debug.LogError($"[{GetType().Name}] '{gameObject.name}' has no CinemachineCamera assigned.");
                return;
            }

            // Start inactive — the first zone of a scene should either have its camera already
            // set to a higher priority in the Inspector, or something external (a SceneSetter)
            // should call Activate() on it once the scene is ready.
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
        /// Raises this zone's camera priority so Cinemachine makes it live. Virtual so
        /// OverworldRoomRender can piggyback room-activation logic on the same trigger.
        /// </summary>
        public virtual void Activate()
        {
            if (cinemachineCamera == null) return;
            cinemachineCamera.Priority = activePriority;
        }

        /// <summary>
        /// Lowers priority so Cinemachine falls back to whichever camera is next highest.
        /// </summary>
        public virtual void Deactivate()
        {
            if (cinemachineCamera == null) return;
            cinemachineCamera.Priority = inactivePriority;
        }
    }
}