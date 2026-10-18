using UnityEngine;

namespace Core
{
    // ── Overworld ─────────────────────────────────────────────────────────────

    public class DungeonSceneSetter : SceneSetter
    {
        protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Overworld;

        protected override void OnSceneReady()
        {
            // Example: tell GameManager we entered overworld, start ambient music, etc.
            Debug.Log($"[DungeonSceneSetter] Scene ready — {InitialActionMap} map active.");
        }
    }
}