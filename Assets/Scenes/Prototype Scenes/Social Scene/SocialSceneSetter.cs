using UnityEngine;

namespace Core
{
    // ── Social ─────────────────────────────────────────────────────────────

    public class SocialSceneSetter : SceneSetter
    {
        protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Social;

        public GameObject inputsHints;

        protected override void OnSceneReady()
        {
            UIManager.Instance.inputsHints = inputsHints;
            
            Debug.Log($"[SocialSceneSetter] Scene ready — {InitialActionMap} map active.");
        }
    }
}