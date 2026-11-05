using UnityEngine;
using Core.ChallengesSystem;

namespace ExtendedDebug
{
    /// <summary>
    /// Debug helper to test challenges without playing a combat.
    /// Use the component context menu in Play mode.
    /// </summary>
    public class ChallengeDebugHarness : MonoBehaviour
    {
        [Header("Test values")]
        [Tooltip("Milestone reported by 'Report Milestone'.")]
        [SerializeField] private string milestoneID = "game_completed";

        [Tooltip("Amount reported by 'Report Enemy Kills'.")]
        [SerializeField, Min(1)] private int killAmount = 5;

        private void Start()
        {
            if (ChallengeManager.Instance == null) return;
            ChallengeManager.Instance.OnChallengeCompleted += HandleCompleted;
            ChallengeManager.Instance.OnProgressChanged += HandleProgress;
        }

        private void OnDisable()
        {
            if (ChallengeManager.Instance == null) return;
            ChallengeManager.Instance.OnChallengeCompleted -= HandleCompleted;
            ChallengeManager.Instance.OnProgressChanged -= HandleProgress;
        }

        [ContextMenu("Report Enemy Kills")]
        private void ReportEnemyKills()
        {
            Debug.Log($"[Harness] Reporting {killAmount} kill(s).");
            ChallengeEvents.ReportStat(CHALLENGE_STAT.EnemiesKilled, killAmount);
        }

        [ContextMenu("Report Milestone")]
        private void ReportMilestone() => ChallengeEvents.ReportMilestone(milestoneID);

        [ContextMenu("Begin Run")]
        private void BeginRun() => ChallengeManager.Instance?.BeginRun();

        private void HandleProgress(ChallengeSO challenge, int current, int target) =>
            Debug.Log($"[Challenges] {challenge.challengeID}: {current}/{target}");

        private void HandleCompleted(ChallengeSO challenge) =>
            Debug.Log($"[Challenges] COMPLETED: {challenge.challengeID}");
    }
}