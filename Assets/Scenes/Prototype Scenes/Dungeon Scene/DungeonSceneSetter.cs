using UnityEngine;

namespace Core
{
    // ── Overworld ─────────────────────────────────────────────────────────────

    public class DungeonSceneSetter : SceneSetter
    {
        protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Dungeon;

        public GameObject inputHints;
        public GameObject logText;
        public GameObject globalVisuals;

        protected override void OnSceneReady()
        {
            GameManager.Instance.inputsHints = inputHints;
            GameManager.Instance.logText = logText;
            GameManager.Instance.globalVisuals = globalVisuals;

            Debug.Log($"[DungeonSceneSetter] Scene ready — {InitialActionMap} map active.");
        }
    }
}