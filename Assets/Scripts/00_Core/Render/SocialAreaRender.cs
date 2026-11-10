using System.Collections.Generic;
using UnityEngine;

namespace Core.Render
{
    /// <summary>
    /// On top of the camera
    /// switching inherited from <see cref="CameraZone"/>, entering an area activates its content plus
    /// its immediate neighbors' content and deactivates every other registered area, so only what is
    /// near the Player is rendered and simulated.
    ///
    /// Setup per area:
    ///   1. Create an empty GameObject with a collider set as Trigger.
    ///   2. Add this component and assign the CinemachineCamera for this area.
    ///   3. Optionally assign the content root and the neighbor areas.
    ///   4. Mark exactly one area as the starting area (the one the Player spawns in).
    /// </summary>
    public class SocialAreaRender : CameraZone
    {
        [Header("----- Area Content -----")]
        [Tooltip("Root containing everything that belongs to this area (NPCs, props, etc.). It is active " +
                 "while this area or a neighbor is current, and deactivated otherwise. Optional: an area " +
                 "without content only switches the camera.")]
        [SerializeField] private GameObject areaContent;

        [Tooltip("Areas directly connected to this one. Activated alongside this area so crossing between " +
                 "them has no pop-in. Wire it both ways: if A lists B, B should list A.")]
        [SerializeField] private List<SocialAreaRender> neighbors = new();

        [Header("----- Bootstrap -----")]
        [Tooltip("Marks the area the Player starts in. It activates itself in Start(), which raises its " +
                 "camera priority and turns on its content and its neighbors'. Mark exactly one per scene.")]
        [SerializeField] private bool isStartingArea;

        // Every SocialAreaRender currently enabled, so ActivateNeighborhood() knows the full set to turn off.
        private static readonly List<SocialAreaRender> allAreas = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            // The base class resets this area's camera priority to "inactive". That also overrides any
            // higher priority set in the Inspector, so the starting area must be activated in Start().
            base.Awake();

            // Mirror the camera's "start inactive" default: Start() turns on the starting area.
            SetContentActive(false);
        }

        private void OnEnable()  => allAreas.Add(this);
        private void OnDisable() => allAreas.Remove(this);

        private void Start()
        {
            // Start() runs after every Awake()/OnEnable() in the scene, so every area is already
            // registered and the starting one can safely turn on its neighbors.
            if (isStartingArea)
                Activate();

            // Run the sanity check once per scene, from the first registered area only.
            if (allAreas.Count > 0 && allAreas[0] == this)
                ValidateStartingAreas();
        }

        // ── CameraZone overrides ──────────────────────────────────────────────

        public override void Activate()
        {
            base.Activate();
            ActivateNeighborhood();
        }

        // Deliberately no Deactivate() override: whether this area's content should turn off when the
        // Player leaves it depends on whether the area they enter counts it as a neighbor. That is
        // decided by the next area's own Activate() call, which scans every registered area.

        // ── Area activation ───────────────────────────────────────────────────

        private void ActivateNeighborhood()
        {
            var shouldBeActive = new HashSet<SocialAreaRender>(neighbors) { this };

            foreach (SocialAreaRender area in allAreas)
                area.SetContentActive(shouldBeActive.Contains(area));
        }

        private void SetContentActive(bool active)
        {
            if (areaContent != null)
                areaContent.SetActive(active);
        }

        // ── Validation ────────────────────────────────────────────────────────

        private static void ValidateStartingAreas()
        {
            int startingCount = 0;
            foreach (SocialAreaRender area in allAreas)
                if (area.isStartingArea) startingCount++;

            if (startingCount == 0)
                Debug.LogWarning($"[{nameof(SocialAreaRender)}] No starting area is marked in this scene. " +
                                 "Area content stays off and no camera is raised until the Player enters a trigger.");
            else if (startingCount > 1)
                Debug.LogWarning($"[{nameof(SocialAreaRender)}] {startingCount} starting areas are marked; " +
                                 "only one should be. The last one to activate wins the camera.");
        }

        /// <summary>
        /// Logs every neighbor that does not list this area back. A one-way link causes pop-in when
        /// walking in the direction that is missing.
        /// </summary>
        [ContextMenu("Validate Neighbors")]
        private void ValidateNeighbors()
        {
            foreach (SocialAreaRender neighbor in neighbors)
            {
                if (neighbor == null)
                {
                    Debug.LogWarning($"[{nameof(SocialAreaRender)}] '{name}' has an empty neighbor slot.", this);
                    continue;
                }

                if (!neighbor.neighbors.Contains(this))
                    Debug.LogWarning($"[{nameof(SocialAreaRender)}] '{name}' lists '{neighbor.name}' as a neighbor, " +
                                     $"but '{neighbor.name}' does not list '{name}'.", this);
            }
        }
    }
}