using System.Collections.Generic;
using UnityEngine;

namespace Core.Render
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
    public class OverworldRoomRender : CameraZone
    {
        [Header("----- Room Content -----")]
        [Tooltip("Root containing everything that belongs to this room (enemies, props, etc.). " +
                 "Activated when this room (or a neighbor) becomes current; deactivated otherwise.")]
        [SerializeField] private GameObject roomContent;

        [Tooltip("Rooms directly connected to this one. Activated alongside this room so crossing between them has no pop-in.")]
        [SerializeField] private List<OverworldRoomRender> neighbors = new();

        // Every OverworldRoomRender currently loaded, so ActivateNeighborhood() knows the full
        // set to turn off. Registered/unregistered per instance — no separate manager needed.
        private static readonly List<OverworldRoomRender> allRooms = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            // Mirror the camera's "start inactive" default — an external bootstrap/SceneSetter
            // is responsible for activating whichever room the Player actually starts in.
            SetContentActive(false);
        }

        private void OnEnable()  => allRooms.Add(this);
        private void OnDisable() => allRooms.Remove(this);

        // ── CameraZone overrides ──────────────────────────────────────────────

        public override void Activate()
        {
            base.Activate();
            ActivateNeighborhood();
        }

        // Deliberately no Deactivate() override: whether this room's content should turn off
        // when the Player leaves it depends on whether the room they're entering counts it as
        // a neighbor. That's already decided by the next room's own Activate() call, which
        // scans every registered room — handling it here too would just duplicate that logic.

        // ── Room activation ───────────────────────────────────────────────────

        private void ActivateNeighborhood()
        {
            var shouldBeActive = new HashSet<OverworldRoomRender>(neighbors) { this };

            foreach (OverworldRoomRender room in allRooms)
                room.SetContentActive(shouldBeActive.Contains(room));
        }

        private void SetContentActive(bool active)
        {
            if (roomContent == null)
            {
                Debug.LogWarning($"[{nameof(OverworldRoomRender)}] '{gameObject.name}' has no roomContent assigned.");
                return;
            }

            roomContent.SetActive(active);
        }
    }
}