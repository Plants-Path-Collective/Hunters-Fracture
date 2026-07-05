// ─────────────────────────────────────────────────────────────────────────────
// Example SceneSetter subclasses — one file per scene type in your real project.
// These live in their own files; they're grouped here for reference only.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace Core
{
    // ── Overworld / Exploration ───────────────────────────────────────────────

    public class OverworldSceneSetter : SceneSetter
    {
        protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Exploration;

        protected override void OnSceneReady()
        {
            // Example: tell GameManager we entered overworld, start ambient music, etc.
            Debug.Log("[OverworldSceneSetter] Scene ready — Exploration map active.");
        }
    }
}