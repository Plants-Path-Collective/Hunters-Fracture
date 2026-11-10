using UnityEngine;

namespace Core
{
    // ── Overworld ─────────────────────────────────────────────────────────────

    public class DungeonSceneSetter : SceneSetter
    {
        protected override INPUTACTION_MAP InitialActionMap => INPUTACTION_MAP.Dungeon;

        public GameObject inputHints;
        public GameObject logText;

        protected override void OnSceneReady()
        {
            GameManager.Instance.inputsHints = inputHints;
            GameManager.Instance.logText = logText;

            Debug.Log($"[DungeonSceneSetter] Scene ready — {InitialActionMap} map active.");
        }
    }
}