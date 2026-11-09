using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Persistent singleton that owns challenge progress and unlocks.
    /// Listens to conditions (never to scene-bound systems), grants rewards
    /// and persists through an IChallengeSaveBackend.
    /// </summary>
    public class ChallengeManager : MonoBehaviour
    {
        public static ChallengeManager Instance { get; private set; }

        [Header("Data")]
        [Tooltip("Catalog with every challenge in the game.")]
        [SerializeField] private ChallengeCatalog catalog;

        /// <summary>Raised once when a challenge is completed.</summary>
        public event Action<ChallengeSO> OnChallengeCompleted;

        /// <summary>Raised on every progress change (challenge, current, target).</summary>
        public event Action<ChallengeSO, int, int> OnProgressChanged;

        /// <summary>The catalog used by this manager (for UI lists).</summary>
        public ChallengeCatalog Catalog => catalog;

        private IChallengeSaveBackend backend;
        private ChallengeSaveData saveData;
        private bool isDirty;

        private readonly Dictionary<string, ChallengeProgress> progressByID = new();
        private readonly HashSet<string> unlocks = new();
        private readonly Dictionary<string, Action> detachers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (catalog == null)
{
    Debug.LogError("[Challenges] ChallengeManager has no catalog assigned.", this);
    return;
}

Debug.Log($"[Challenges] Ready. {catalog.Challenges.Count} challenge(s). Save folder: {Application.persistentDataPath}");

            backend = new JsonChallengeSaveBackend();
            LoadSave();
            AttachPending();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            foreach (Action detach in detachers.Values) detach?.Invoke();
            detachers.Clear();
            Instance = null;
        }

        private void OnApplicationQuit() => SaveIfDirty();

        #region Public API

        /// <summary>Resets the progress of every non-completed PerRun challenge.</summary>
        public void BeginRun()
        {
            foreach (ChallengeSO challenge in catalog.Challenges)
            {
                if (challenge == null || challenge.condition == null) continue;
                if (challenge.scope != CHALLENGE_SCOPE.PerRun) continue;

                ChallengeProgress progress = GetOrCreateProgress(challenge.challengeID);
                if (progress.completed || progress.value == 0) continue;

                progress.value = 0;
                isDirty = true;
                OnProgressChanged?.Invoke(challenge, 0, challenge.condition.Target);
            }

            SaveIfDirty();
        }

        /// <summary>Returns true if the challenge was completed at some point.</summary>
        public bool IsCompleted(string challengeID) =>
            progressByID.TryGetValue(challengeID, out ChallengeProgress p) && p.completed;

        /// <summary>Completes a challenge by ID, granting its rewards and saving the result.</summary>
        public bool ForceComplete(string challengeID)
        {
            if (catalog == null || string.IsNullOrEmpty(challengeID) ||
                !catalog.TryGet(challengeID, out ChallengeSO challenge) ||
                challenge == null || IsCompleted(challengeID))
            {
                return false;
            }

            ChallengeProgress progress = GetOrCreateProgress(challengeID);
            int target = challenge.condition != null ? challenge.condition.Target : 0;
            progress.value = target;
            isDirty = true;
            OnProgressChanged?.Invoke(challenge, progress.value, target);
            Complete(challenge, progress);
            return true;
        }

        /// <summary>Gets current progress and target of a challenge (for UI).</summary>
        public (int current, int target) GetProgress(ChallengeSO challenge)
        {
            int target = challenge.condition != null ? challenge.condition.Target : 0;
            if (!progressByID.TryGetValue(challenge.challengeID, out ChallengeProgress p)) return (0, target);
            return (p.completed ? target : p.value, target);
        }

        /// <summary>Returns true if the content key was unlocked by a reward.</summary>
        public bool IsUnlocked(string unlockKey) => unlocks.Contains(unlockKey);

        /// <summary>Unlocks a content key. Called by rewards.</summary>
        public void AddUnlock(string unlockKey)
        {
            if (unlocks.Add(unlockKey)) isDirty = true;
        }

        /// <summary>Writes to disk only if something changed. Call after combats or scene changes.</summary>
        public void SaveIfDirty()
        {
            if (isDirty) Save();
        }

        #endregion

        #region Internals

        private void LoadSave()
        {
            saveData = backend.Load();

            progressByID.Clear();
            foreach (ChallengeProgress p in saveData.progress)
            {
                if (p != null && !string.IsNullOrEmpty(p.id)) progressByID[p.id] = p;
            }

            unlocks.Clear();
            foreach (string key in saveData.unlocks) unlocks.Add(key);
        }

        private void Save()
        {
            saveData.progress.Clear();
            saveData.progress.AddRange(progressByID.Values);

            saveData.unlocks.Clear();
            saveData.unlocks.AddRange(unlocks);

            backend.Save(saveData);
            isDirty = false;
        }

        private void AttachPending()
        {
            foreach (ChallengeSO challenge in catalog.Challenges)
            {
                if (challenge == null)
                {
                    Debug.LogWarning("[Challenges] Null entry in the catalog.");
                    continue;
                }

                if (challenge.condition == null)
                {
                    Debug.LogWarning($"[Challenges] '{challenge.challengeID}' has no condition assigned, skipped.", challenge);
                    continue;
                }

                if (IsCompleted(challenge.challengeID)) continue;

                Attach(challenge);
            }
        }

        private void Attach(ChallengeSO challenge)
        {
            if (detachers.ContainsKey(challenge.challengeID)) return;

            detachers[challenge.challengeID] =
                challenge.condition.Attach(delta => AddProgress(challenge, delta));
        }

        private void Detach(string challengeID)
        {
            if (detachers.Remove(challengeID, out Action detach)) detach?.Invoke();
        }

        private void AddProgress(ChallengeSO challenge, int delta)
        {
            ChallengeProgress progress = GetOrCreateProgress(challenge.challengeID);
            if (progress.completed || delta <= 0) return;

            int target = challenge.condition.Target;
            progress.value = Mathf.Min(progress.value + delta, target);
            isDirty = true;

            Debug.Log($"[Challenges] {challenge.challengeID}: {progress.value}/{target}");

            OnProgressChanged?.Invoke(challenge, progress.value, target);

            if (progress.value >= target) Complete(challenge, progress);
        }

        private void Complete(ChallengeSO challenge, ChallengeProgress progress)
        {
            progress.completed = true;
            Detach(challenge.challengeID);

            foreach (ChallengeRewardSO reward in challenge.rewards)
            {
                if (reward != null) reward.Grant(this);
            }

            OnChallengeCompleted?.Invoke(challenge);
            Save();
        }

        private ChallengeProgress GetOrCreateProgress(string challengeID)
        {
            if (!progressByID.TryGetValue(challengeID, out ChallengeProgress progress))
            {
                progress = new ChallengeProgress { id = challengeID };
                progressByID[challengeID] = progress;
            }

            return progress;
        }

        #endregion
    }
}