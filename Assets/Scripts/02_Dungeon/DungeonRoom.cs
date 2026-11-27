using System.Collections.Generic;
using System.Linq;
using Core.Render;
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

namespace Dungeon
{
    /// <summary>
    /// Camera + room-activation zone for Overworld dungeon rooms (rail-mounted, angled-down
    /// camera — Binding of Isaac–in-3D style). On top of the priority switching inherited from
    /// CameraZone, entering a room activates its content plus its immediate neighbors' content
    /// and deactivates every other registered room, so only what's near the Player is ever
    /// rendered/simulated.
    ///
    /// Rooms are meant to be modular (1:1 size, stacked into 2:1 / 3:1 for longer rooms) — that
    /// construction system isn't built yet, so for now neighbors are wired by hand per room.
    /// </summary>
    public class DungeonRoom : CameraZone
    {
        [Header("----- Room Content -----")]
        [Tooltip("Root containing everything that belongs to this room (enemies, props, etc.). " +
                 "Activated when this room (or a neighbor) becomes current; deactivated otherwise.")]
        [SerializeField] private GameObject roomContent;

        [Tooltip("Rooms directly connected to this one. Activated alongside this room so crossing between them has no pop-in.")]
        [SerializeField] private List<DungeonRoom> neighbors = new();
        private static int neighborhoodVersion;

        [Tooltip("Enemies assigned to this room. Their patrol points are restricted to this room's collider bounds.")]
        [SerializeField] private List<EnemyEntity> enemyEntities = new();

        [Header("----- Bootstrap -----")]
        [Tooltip("Temporary until a real SceneSetter exists: self-activates on Start() so at least one room isn't permanently disabled.")]
        [SerializeField] private bool isStartingRoom;

        // Every OverworldRoomRender currently loaded, so ActivateNeighborhood() knows the full
        // set to turn off. Registered/unregistered per instance — no separate manager needed.
        private static readonly List<DungeonRoom> allRooms = new();

        /// <summary>Content roots of every currently-active registered room — used by CombatTransition
        /// to suspend and later restore exactly what was on before a fight started.</summary>
        public static IEnumerable<GameObject> ActiveRoomContents =>
            allRooms.Where(r => r.roomContent != null && r.roomContent.activeSelf).Select(r => r.roomContent);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            Collider roomBounds = GetComponent<Collider>();
            if (roomBounds != null)
            {
                foreach (EnemyEntity enemy in enemyEntities)
                    enemy?.ConfigurePatrolArea(roomBounds.bounds);
            }
            else
            {
                Debug.LogError($"[{nameof(DungeonRoom)}] '{name}' has no Collider to define enemy patrol bounds.", this);
            }

            // Mirror the camera's "start inactive" default — an external bootstrap/SceneSetter
            // is responsible for activating whichever room the Player actually starts in.
            SetContentActive(false);
        }

        private void Start()
        {
            // Start() is guaranteed to run only after every object's Awake()+OnEnable() in the
            // scene — so by now every room has registered itself in allRooms, and it's safe to
            // activate the starting one without missing any neighbors.
            if (isStartingRoom)
            {
                instantNextActivation = true;
                Activate();
            }
        }

        private void OnEnable()  => allRooms.Add(this);
        private void OnDisable() => allRooms.Remove(this);

        // ── CameraZone overrides ──────────────────────────────────────────────

        public override void Activate()
        {
            if (cinemachineCamera == null) return;

            // Read before ApplyBlend(), which resets the flag.
            bool blends = !instantNextActivation
                        && blendIn.Style != CinemachineBlendDefinition.Styles.Cut;

            ApplyBlend();
            cinemachineCamera.Priority = activePriority;
            GameManager.Instance.cmCameraActive = this.gameObject;

            ActivateNeighborhood(deferDeactivation: blends);
        }

        public override void Deactivate()
        {
            if (cinemachineCamera == null) return;
            cinemachineCamera.Priority = inactivePriority;
        }

        // Deliberately no Deactivate() override: whether this room's content should turn off
        // when the Player leaves it depends on whether the room they're entering counts it as
        // a neighbor. That's already decided by the next room's own Activate() call, which
        // scans every registered room — handling it here too would just duplicate that logic.

        // ── Room activation ───────────────────────────────────────────────────

        private void ActivateNeighborhood(bool deferDeactivation)
        {
            var shouldBeActive = new HashSet<DungeonRoom>(neighbors) { this };
            int version = ++neighborhoodVersion;

            // Turning on is always immediate: the new room must be ready before the camera arrives.
            foreach (DungeonRoom room in allRooms)
                if (shouldBeActive.Contains(room))
                    room.SetContentActive(true);

            if (deferDeactivation)
                StartCoroutine(DeactivateAfterBlend(shouldBeActive, version));
            else
                DeactivateOthers(shouldBeActive);
        }

        private IEnumerator DeactivateAfterBlend(HashSet<DungeonRoom> keepActive, int version)
        {
            // The brain starts the blend in LateUpdate, so wait a frame before checking IsBlending.
            yield return null;

            if (CinemachineBrain.ActiveBrainCount > 0)
            {
                CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
                float timeout = blendIn.Time + 1f; // safety net so content never stays on forever
                float elapsed = 0f;

                while (brain.IsBlending && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // A newer activation happened meanwhile: it owns the deactivation now.
            if (version != neighborhoodVersion) yield break;

            DeactivateOthers(keepActive);
        }

        private static void DeactivateOthers(HashSet<DungeonRoom> keepActive)
        {
            foreach (DungeonRoom room in allRooms)
                if (!keepActive.Contains(room))
                    room.SetContentActive(false);
        }

        private void SetContentActive(bool active)
        {
            if (roomContent == null)
            {
                Debug.LogWarning($"[{nameof(DungeonRoom)}] '{gameObject.name}' has no roomContent assigned.");
                return;
            }

            roomContent.SetActive(active);
        }
    }
}