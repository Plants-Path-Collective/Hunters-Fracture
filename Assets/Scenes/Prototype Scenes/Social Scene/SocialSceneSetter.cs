using UnityEngine;

namespace Core
{
    // ── Social ─────────────────────────────────────────────────────────────

    public class SocialSceneSetter : SceneSetter
    {
        protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Social;

        protected override void OnSceneReady()
        {
            // Example: tell GameManager we entered social area, start ambient music, etc.
            Debug.Log($"[SocialSceneSetter] Scene ready — {InitialActionMap} map active.");
        }
    }
}