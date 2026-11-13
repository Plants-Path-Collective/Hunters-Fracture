using System;
using System.Collections.Generic;
using UnityEngine;
using Core.ChallengesSystem;
using Core.Clock;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Persistent singleton that owns quest states and step progress.
    /// Steps listen to ChallengeEvents milestones only while their quest is Active,
    /// so progress counts from the moment the quest was accepted.
    /// Time windows are evaluated lazily from GameClock (on query, on phase change and on load).
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Data")]
        [Tooltip("Catalog with every quest in the game.")]
        [SerializeField] private QuestCatalog catalog;

        /// <summary>Raised when a quest changes state (quest, new state).</summary>
        public event Action<QuestSO, QUEST_STATE> OnQuestStateChanged;

        /// <summary>Raised on every step progress change (quest, step index, current, target).</summary>
        public event Action<QuestSO, int, int, int> OnStepProgress;

        /// <summary>The catalog used by this manager (for UI lists and console).</summary>
        public QuestCatalog Catalog => catalog;

        private IQuestSaveBackend backend;
        private QuestSaveData saveData;
        private bool isDirty;
        private GameClock subscribedClock;

        // Only Active, ReadyToTurnIn, Completed and Expired quests have an entry.
        private readonly Dictionary<string, QuestProgress> progressByID = new();
        private readonly Dictionary<string, List<Action>> detachers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            if (catalog == null)
            {
                Debug.LogError("[Quests] QuestManager has no catalog assigned.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            backend = new JsonQuestSaveBackend();
            LoadSave();
            AttachActiveQuests();

            Debug.Log($"[Quests] Ready. {catalog.Quests.Count} quest(s). Save folder: {Application.persistentDataPath}");
        }

        private void Start()
        {
            if (Instance != this) return;

            // GameClock may be created after this Awake, so subscribe here.
            subscribedClock = GameClock.Instance;
            if (subscribedClock != null) subscribedClock.OnPhaseChanged += HandlePhaseChanged;
            else Debug.LogWarning("[Quests] No GameClock found: time windows are ignored.");

            RefreshAllExpirations();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            if (subscribedClock != null) subscribedClock.OnPhaseChanged -= HandlePhaseChanged;

            foreach (List<Action> attached in detachers.Values)
            {
                foreach (Action detach in attached) detach?.Invoke();
            }

            detachers.Clear();
            Instance = null;
        }

        private void OnApplicationQuit() => SaveIfDirty();

        #region Public API

        /// <summary>
        /// Returns the current state of a quest. Also expires it if its turn-in window already passed.
        /// </summary>
        public QUEST_STATE GetState(QuestSO quest)
        {
            if (quest == null) return QUEST_STATE.Unavailable;

            RefreshExpiration(quest);

            if (progressByID.TryGetValue(quest.questID, out QuestProgress progress)) return progress.state;
            return ArePrerequisitesMet(quest) ? QUEST_STATE.Available : QUEST_STATE.Unavailable;
        }

        /// <summary>Returns the current state of a quest by ID (Unavailable if the ID is unknown).</summary>
        public QUEST_STATE GetState(string questID) =>
            catalog.TryGet(questID, out QuestSO quest) ? GetState(quest) : QUEST_STATE.Unavailable;

        /// <summary>Returns true if the quest was turned in during this run.</summary>
        public bool IsCompleted(string questID) =>
            progressByID.TryGetValue(questID, out QuestProgress p) && p.state == QUEST_STATE.Completed;

        /// <summary>Accepts an Available quest inside its offer window.</summary>
        public bool Accept(string questID) => Accept(questID, out _);

        /// <summary>Accepts an Available quest inside its offer window.</summary>
        /// <param name="failReason">Why it was refused, or null on success.</param>
        public bool Accept(string questID, out string failReason)
        {
            if (!catalog.TryGet(questID, out QuestSO quest))
            {
                failReason = $"Unknown quest '{questID}'.";
                return false;
            }

            QUEST_STATE state = GetState(quest);
            if (state != QUEST_STATE.Available)
            {
                failReason = $"'{questID}' is {state}, not Available.";
                return false;
            }

            if (!InOfferWindow(quest))
            {
                failReason = $"'{questID}' cannot be accepted during {GameClock.Instance.CurrentPhase}.";
                return false;
            }

            if (!quest.HasValidSteps(out string stepError))
            {
                failReason = $"'{questID}' has invalid steps: {stepError}";
                return false;
            }

            var progress = new QuestProgress { id = quest.questID };
            progressByID[quest.questID] = progress;

            AttachSteps(quest);
            SetState(quest, progress, QUEST_STATE.Active);

            // A quest without steps is already done.
            CheckStepsMet(quest, progress);

            failReason = null;
            return true;
        }

        /// <summary>Turns in a ReadyToTurnIn quest inside its turn-in window.</summary>
        public bool TryTurnIn(string questID) => TryTurnIn(questID, out _);

        /// <summary>Turns in a ReadyToTurnIn quest inside its turn-in window.</summary>
        /// <param name="failReason">Why it was refused, or null on success.</param>
        public bool TryTurnIn(string questID, out string failReason)
        {
            if (!catalog.TryGet(questID, out QuestSO quest))
            {
                failReason = $"Unknown quest '{questID}'.";
                return false;
            }

            QUEST_STATE state = GetState(quest);
            if (state != QUEST_STATE.ReadyToTurnIn)
            {
                failReason = $"'{questID}' is {state}, not ReadyToTurnIn.";
                return false;
            }

            if (!InTurnInWindow(quest))
            {
                failReason = $"'{questID}' cannot be turned in during {GameClock.Instance.CurrentPhase}.";
                return false;
            }

            Complete(quest, progressByID[quest.questID]);
            failReason = null;
            return true;
        }

        /// <summary>Gets current progress and target of a step (for UI).</summary>
        public (int current, int target) GetStepProgress(QuestSO quest, int stepIndex)
        {
            if (quest == null || stepIndex < 0 || stepIndex >= quest.steps.Count) return (0, 0);

            QuestStep step = quest.steps[stepIndex];
            if (!progressByID.TryGetValue(quest.questID, out QuestProgress progress)) return (0, step.Target);
            return (progress.GetValue(step.stepID), step.Target);
        }

        /// <summary>
        /// Clears every quest state and progress, and the save file. Called when a new run begins.
        /// </summary>
        public void BeginRun()
        {
            var affected = new List<string>(progressByID.Keys);

            foreach (List<Action> attached in detachers.Values)
            {
                foreach (Action detach in attached) detach?.Invoke();
            }

            detachers.Clear();
            progressByID.Clear();
            isDirty = true;
            Save();

            foreach (string id in affected)
            {
                if (catalog.TryGet(id, out QuestSO quest)) OnQuestStateChanged?.Invoke(quest, GetState(quest));
            }
        }

        /// <summary>Writes to disk only if something changed.</summary>
        public void SaveIfDirty()
        {
            if (isDirty && backend != null) Save();
        }

        #endregion

        #region Internals

        private void LoadSave()
        {
            saveData = backend.Load();

            progressByID.Clear();
            foreach (QuestProgress progress in saveData.quests)
            {
                if (progress == null || string.IsNullOrEmpty(progress.id)) continue;

                if (!catalog.TryGet(progress.id, out _))
                {
                    Debug.LogWarning($"[Quests] Saved quest '{progress.id}' is not in the catalog, dropped.");
                    isDirty = true;
                    continue;
                }

                progress.steps ??= new List<QuestStepProgress>();
                progressByID[progress.id] = progress;
            }
        }

        private void Save()
        {
            saveData.quests.Clear();
            saveData.quests.AddRange(progressByID.Values);

            backend.Save(saveData);
            isDirty = false;
        }

        private void AttachActiveQuests()
        {
            foreach (QuestProgress progress in progressByID.Values)
            {
                if (progress.state != QUEST_STATE.Active) continue;
                if (catalog.TryGet(progress.id, out QuestSO quest)) AttachSteps(quest);
            }
        }

        private void AttachSteps(QuestSO quest)
        {
            DetachSteps(quest.questID);

            var attached = new List<Action>();
            for (int i = 0; i < quest.steps.Count; i++)
            {
                string milestone = quest.steps[i].milestoneID;
                if (string.IsNullOrEmpty(milestone)) continue;

                int index = i;
                Action<string> handler = reported =>
                {
                    if (reported == milestone) AddStepProgress(quest, index, 1);
                };

                ChallengeEvents.OnMilestoneReported += handler;
                attached.Add(() => ChallengeEvents.OnMilestoneReported -= handler);
            }

            detachers[quest.questID] = attached;
        }

        private void DetachSteps(string questID)
        {
            if (!detachers.Remove(questID, out List<Action> attached)) return;

            foreach (Action detach in attached) detach?.Invoke();
        }

        private void AddStepProgress(QuestSO quest, int index, int delta)
        {
            if (delta <= 0 || index >= quest.steps.Count) return;
            if (!progressByID.TryGetValue(quest.questID, out QuestProgress progress)) return;

            RefreshExpiration(quest);
            if (progress.state != QUEST_STATE.Active) return;

            QuestStep step = quest.steps[index];
            int previous = progress.GetValue(step.stepID);
            int current = Mathf.Min(previous + delta, step.Target);
            if (current == previous) return;

            progress.SetValue(step.stepID, current);
            isDirty = true;

            Debug.Log($"[Quests] {quest.questID}/{step.stepID}: {current}/{step.Target}");

            OnStepProgress?.Invoke(quest, index, current, step.Target);
            CheckStepsMet(quest, progress);
        }

        private void CheckStepsMet(QuestSO quest, QuestProgress progress)
        {
            if (progress.state != QUEST_STATE.Active) return;

            foreach (QuestStep step in quest.steps)
            {
                if (progress.GetValue(step.stepID) < step.Target) return;
            }

            DetachSteps(quest.questID);

            if (quest.autoComplete) Complete(quest, progress);
            else SetState(quest, progress, QUEST_STATE.ReadyToTurnIn);
        }

        private void Complete(QuestSO quest, QuestProgress progress)
        {
            DetachSteps(quest.questID);
            SetState(quest, progress, QUEST_STATE.Completed);
            GrantRewards(quest);

            // Bridge with challenges: a challenge with a MilestoneConditionSO can listen to this.
            ChallengeEvents.ReportMilestone(QuestMilestones.QuestCompleted(quest.questID));
        }

        private void Expire(QuestSO quest, QuestProgress progress)
        {
            DetachSteps(quest.questID);
            SetState(quest, progress, QUEST_STATE.Expired);

            foreach (QuestConsequenceSO consequence in quest.expirationConsequences)
            {
                if (consequence != null) consequence.Execute(this, quest);
            }
        }

        private void SetState(QuestSO quest, QuestProgress progress, QUEST_STATE state)
        {
            progress.state = state;
            isDirty = true;
            Save();

            Debug.Log($"[Quests] {quest.questID} -> {state}");
            OnQuestStateChanged?.Invoke(quest, state);
        }

        private void GrantRewards(QuestSO quest)
        {
            if (quest.rewards.Count == 0) return;

            ChallengeManager challenges = ChallengeManager.Instance;
            if (challenges == null)
            {
                Debug.LogWarning($"[Quests] No ChallengeManager: rewards of '{quest.questID}' were not granted.");
                return;
            }

            foreach (ChallengeRewardSO reward in quest.rewards)
            {
                if (reward != null) reward.Grant(challenges);
            }

            challenges.SaveIfDirty();
        }

        private bool ArePrerequisitesMet(QuestSO quest)
        {
            foreach (QuestSO required in quest.prerequisiteQuests)
            {
                if (required == null) continue;
                if (!IsCompleted(required.questID)) return false;
            }

            if (quest.prerequisiteUnlockKeys.Count == 0) return true;

            ChallengeManager challenges = ChallengeManager.Instance;
            if (challenges == null) return false;

            foreach (string key in quest.prerequisiteUnlockKeys)
            {
                if (!string.IsNullOrEmpty(key) && !challenges.IsUnlocked(key)) return false;
            }

            return true;
        }

        private void HandlePhaseChanged(DAY_PHASE previous, DAY_PHASE next) => RefreshAllExpirations();

        private void RefreshAllExpirations()
        {
            foreach (QuestSO quest in catalog.Quests)
            {
                if (quest != null) RefreshExpiration(quest);
            }
        }

        private void RefreshExpiration(QuestSO quest)
        {
            if (!progressByID.TryGetValue(quest.questID, out QuestProgress progress)) return;
            if (progress.state != QUEST_STATE.Active && progress.state != QUEST_STATE.ReadyToTurnIn) return;

            GameClock clock = GameClock.Instance;
            if (clock == null) return;

            if (quest.IsPastTurnInWindow(clock.CurrentPhase)) Expire(quest, progress);
        }

        // Without a clock (test scenes) every window counts as open.
        /// <summary>True if the quest can be offered right now (always true without a clock).</summary>
        public static bool InOfferWindow(QuestSO quest)
        {
            GameClock clock = GameClock.Instance;
            return clock == null || quest.AllowsOffer(clock.CurrentPhase);
        }

        /// <summary>True if the quest can be turned in right now (always true without a clock).</summary>
        public static bool InTurnInWindow(QuestSO quest)
        {
            GameClock clock = GameClock.Instance;
            return clock == null || quest.AllowsTurnIn(clock.CurrentPhase);
        }

        #endregion
    }
}