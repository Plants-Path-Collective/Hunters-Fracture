using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using Core.CombatSystem.Units;
using Core.Render;

namespace Core.CombatSystem
{
    /// <summary>
    /// Orchestrates the Overworld ↔ CombatStage handoff: hides the two Entities involved, the
    /// active room content, and the Overworld camera (so its AudioListener doesn't fight
    /// CombatStage's), loads CombatStage additively, hands CombatController its CombatSetUp,
    /// and switches to the Combat input map. On the way back it undoes all of that and unloads
    /// CombatStage — unless the run ended in Defeat, which is handled separately (see
    /// HandleDefeat).
    /// </summary>
    /// <remarks>
    /// Static and stateless between combats on purpose: only one encounter can be in flight at
    /// a time, and nothing here needs to survive a domain reload the way a persistent singleton
    /// (InputManager, GameManager) does.
    /// </remarks>
    public static class CombatTransition
    {
        private const string CombatStageScene = "CombatStage";

        // All snapshotted right before combat starts, so the exact same set comes back — not
        // "whatever gets recomputed now" if something changes mid-combat.
        private static List<GameObject> suspendedRoomContents;
        private static GameObject suspendedCamera;
        private static GameObject suspendedPlayer;
        private static GameObject suspendedEnemy;

        public static void Begin(AllyParty party, EnemyParty enemyParty, UNIT_TEAM advantageTeam,
            GameObject playerObject, GameObject enemyObject)
        {
            suspendedRoomContents = OverworldRoomRender.ActiveRoomContents.ToList();
            foreach (GameObject content in suspendedRoomContents)
                content.SetActive(false);

            // need to adapt it to cinemachinecamera (currently not working as intended)
            suspendedCamera = Camera.main != null ? Camera.main.gameObject : null;
            suspendedCamera?.SetActive(false);

            GameManager.Instance.cmCameraActive.SetActive(false);

            suspendedPlayer = playerObject;
            suspendedPlayer?.SetActive(false);

            suspendedEnemy = enemyObject;
            suspendedEnemy?.SetActive(false);

            CombatSetUp.Prepare(party, enemyParty, advantageTeam, OnCombatFinished);

            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Combat);
            SceneManager.LoadSceneAsync(CombatStageScene, LoadSceneMode.Additive);
        }

        private static void OnCombatFinished(COMBAT_OUTCOME outcome)
        {
            if (outcome == COMBAT_OUTCOME.Defeat)
            {
                HandleDefeat();
                return;
            }

            foreach (GameObject content in suspendedRoomContents)
                if (content != null)
                    content.SetActive(true);
            suspendedRoomContents = null;

            GameManager.Instance.cmCameraActive.SetActive(true);

            suspendedCamera?.SetActive(true);
            suspendedCamera = null;

            suspendedPlayer?.SetActive(true);
            suspendedPlayer = null;

            // Victory will eventually destroy the enemy's Overworld GameObject instead of
            // restoring it (once RewardsScreen exists) — see combat.html's "salida-de-combate".
            // For now every non-Defeat outcome just brings it back, same as a Fled.
            suspendedEnemy?.SetActive(true);
            suspendedEnemy = null;

            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Overworld);
            SceneManager.UnloadSceneAsync(CombatStageScene);
        }

        /// <summary>
        /// TODO: replace with GameOverScreen (stats summary, quick reset, main menu) once it
        /// exists. For now just logs and leaves everything — CombatStage, the input map, the
        /// hidden Player/Enemy/rooms — exactly as it was when the fight ended, so the state is
        /// inspectable.
        /// </summary>
        private static void HandleDefeat()
        {
            Debug.Log("[CombatTransition] Defeat — TODO: show GameOverScreen instead of this log.");
        }
    }
}