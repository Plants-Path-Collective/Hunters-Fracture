using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Core.Clock;
using Core.UI;
using Core.CombatSystem.Backpack;

namespace Core.QuestsSystem
{
    [Serializable]
    public class QuestProgress
    {
        public string id;
        public QUEST_STATE state;
        public int step;
        public int counted;
    }

    [Serializable]
    public class QuestSaveData
    {
        public int version = 1;
        public List<QuestProgress> quests = new();
    }

    /// <summary>
    /// Persistent singleton that owns quest state. Quests are referenced by asset, never by string.
    /// Only the CURRENT step of an Active quest is watched; when its condition is met the quest
    /// moves to the next step, and after the last step it becomes ReadyToTurnIn.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Tooltip("Every quest in the game.")]
        [SerializeField] private List<QuestSO> quests = new();

        [Tooltip("Seconds between checks of state-based conditions (items, money).")]
        [SerializeField, Min(0.05f)] private float pollInterval = 0.25f;

        /// <summary>Raised when a quest changes state, advances a step or counts progress.</summary>
        public event Action<QuestSO> OnQuestChanged;

        public IReadOnlyList<QuestSO> Quests => quests;

        private readonly Dictionary<QuestSO, QuestProgress> progress = new();
        private readonly Dictionary<QuestSO, Action> detachers = new();
        private readonly List<QuestSO> buffer = new();
        private GameClock subscribedClock;
        private float pollTimer;
        private bool dirty;

        private string SavePath => Path.Combine(Application.persistentDataPath, "quests.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            Load();
            foreach (var pair in progress)
            {
                if (pair.Value.state == QUEST_STATE.Active) ListenToStep(pair.Key, pair.Value);
            }
        }

        private void Start()
        {
            if (Instance != this) return;

            subscribedClock = GameClock.Instance;
            if (subscribedClock != null) subscribedClock.OnPhaseChanged += HandlePhaseChanged;

            RefreshAllExpirations();
        }

        private void Update()
        {
            pollTimer += Time.unscaledDeltaTime;
            if (pollTimer < pollInterval) return;
            pollTimer = 0f;

            buffer.Clear();
            buffer.AddRange(progress.Keys);
            foreach (QuestSO quest in buffer)
            {
                if (progress[quest].state == QUEST_STATE.Active) Evaluate(quest);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            if (subscribedClock != null) subscribedClock.OnPhaseChanged -= HandlePhaseChanged;
            foreach (Action detach in detachers.Values) detach?.Invoke();
            detachers.Clear();
            Instance = null;
        }

        private void OnApplicationQuit()
        {
            if (dirty) Save();
        }

        #region Public API

        /// <summary>Current state. Also expires the quest if its turn-in window already passed.</summary>
        public QUEST_STATE GetState(QuestSO quest)
        {
            if (quest == null) return QUEST_STATE.Unavailable;

            RefreshExpiration(quest);

            if (progress.TryGetValue(quest, out QuestProgress p)) return p.state;
            return PrerequisitesMet(quest) ? QUEST_STATE.Available : QUEST_STATE.Unavailable;
        }

        /// <summary>Accepts an Available quest inside its offer window.</summary>
        public bool Accept(QuestSO quest, out string failReason)
        {
            if (quest == null) { failReason = "No quest assigned."; return false; }

            QUEST_STATE state = GetState(quest);
            if (state != QUEST_STATE.Available)
            {
                failReason = $"'{quest.name}' is {state}, not Available.";
                return false;
            }

            if (!InOfferWindow(quest))
            {
                failReason = $"'{quest.name}' cannot be accepted during {GameClock.Instance.CurrentPhase}.";
                return false;
            }

            if (!quest.HasValidSteps(out string error))
            {
                failReason = $"'{quest.name}': {error}";
                return false;
            }

            var p = new QuestProgress { id = quest.questID, state = QUEST_STATE.Active };
            progress[quest] = p;
            ListenToStep(quest, p);

            Save();
            OnQuestChanged?.Invoke(quest);
            Evaluate(quest); // steps may already be satisfied (e.g. the player already owns the item)

            failReason = null;
            return true;
        }

        /// <summary>Turns in a ReadyToTurnIn quest inside its turn-in window.</summary>
        public bool TryTurnIn(QuestSO quest, out string failReason)
        {
            if (quest == null) { failReason = "No quest assigned."; return false; }

            QUEST_STATE state = GetState(quest);
            if (state != QUEST_STATE.ReadyToTurnIn)
            {
                failReason = $"'{quest.name}' is {state}, not ReadyToTurnIn.";
                return false;
            }

            if (!InTurnInWindow(quest))
            {
                failReason = $"'{quest.name}' cannot be turned in during {GameClock.Instance.CurrentPhase}.";
                return false;
            }

            foreach (QuestStep step in quest.steps)
            {
                if (step.condition != null && !step.condition.CanConsume())
                {
                    failReason = $"'{quest.name}': the requirements are no longer met.";
                    return false;
                }
            }

            foreach (QuestStep step in quest.steps) step.condition?.Consume();

            progress[quest].state = QUEST_STATE.Completed;

            progress[quest].state = QUEST_STATE.Completed;

            foreach (QuestReward reward in quest.rewards) GrantReward(quest, reward);

            Save();
            OnQuestChanged?.Invoke(quest);

            failReason = null;
            return true;
        }

        #region REWARDS
        private void GrantReward(QuestSO quest, QuestReward reward)
        {
            switch (reward.type)
            {
                case QUEST_REWARD.Lore:
                    Debug.Log("[Quest Reward] " + reward.loreReward.GetLocalizedString());
                break;

                case QUEST_REWARD.Pesos:
                    GameManager.Instance.AddBalance(reward.pesosReward);
                break;

                case QUEST_REWARD.Item:
                    // ItemAssignationPanel.Instance
                break;

                case QUEST_REWARD.Consumable:
                    PartyBackpack.Instance.AddConsumable(reward.consumableReward, reward.consumableQty);
                break;

                case QUEST_REWARD.KeyItem:
                break;
            }
        }
        #endregion

        /// <summary>Current step data for a UI. False if the quest is not Active.</summary>
        public bool TryGetCurrentStep(QuestSO quest, out QuestStep step, out int current, out int target)
        {
            step = null; current = 0; target = 0;
            if (quest == null || !progress.TryGetValue(quest, out QuestProgress p)) return false;
            if (p.state != QUEST_STATE.Active || p.step >= quest.steps.Count) return false;

            step = quest.steps[p.step];
            target = step.condition != null ? step.condition.Target : 0;
            current = step.condition != null ? Mathf.Min(step.condition.GetCurrent(p.counted), target) : 0;
            return true;
        }

        /// <summary>Index of the current step (0-based). -1 if the quest has no progress.</summary>
        public int GetStepIndex(QuestSO quest) =>
            progress.TryGetValue(quest, out QuestProgress p) ? p.step : -1;

        /// <summary>Clears every quest. Call it when a new run/cycle starts.</summary>
        public void BeginRun()
        {
            buffer.Clear();
            buffer.AddRange(progress.Keys);

            foreach (Action detach in detachers.Values) detach?.Invoke();
            detachers.Clear();
            progress.Clear();
            Save();

            foreach (QuestSO quest in buffer) OnQuestChanged?.Invoke(quest);
        }

        public static bool InOfferWindow(QuestSO quest)
        {
            GameClock clock = GameClock.Instance;
            return clock == null || quest.AllowsOffer(clock.CurrentPhase);
        }

        public static bool InTurnInWindow(QuestSO quest)
        {
            GameClock clock = GameClock.Instance;
            return clock == null || quest.AllowsTurnIn(clock.CurrentPhase);
        }

        #endregion

        #region Steps

        // Moves the quest forward while the current step is complete. Returns true if anything changed.
        private bool Evaluate(QuestSO quest)
        {
            if (!progress.TryGetValue(quest, out QuestProgress p)) return false;

            bool changed = false;
            while (p.state == QUEST_STATE.Active)
            {
                if (p.step >= quest.steps.Count)
                {
                    Detach(quest);
                    p.state = QUEST_STATE.ReadyToTurnIn;
                    changed = true;
                    break;
                }

                QuestConditionSO condition = quest.steps[p.step].condition;
                if (condition != null && condition.GetCurrent(p.counted) < condition.Target) break;

                Detach(quest);
                p.step++;
                p.counted = 0;
                changed = true;
                ListenToStep(quest, p);
            }

            if (changed)
            {
                Save();
                OnQuestChanged?.Invoke(quest);
            }

            return changed;
        }

        private void ListenToStep(QuestSO quest, QuestProgress p)
        {
            Detach(quest);
            if (p.step >= quest.steps.Count) return;

            QuestConditionSO condition = quest.steps[p.step].condition;
            if (condition == null) return;

            Action detach = condition.Listen(delta => OnStepEvent(quest, delta));
            if (detach != null) detachers[quest] = detach;
        }

        private void OnStepEvent(QuestSO quest, int delta)
        {
            if (delta <= 0 || !progress.TryGetValue(quest, out QuestProgress p)) return;
            if (p.state != QUEST_STATE.Active) return;

            p.counted += delta;
            dirty = true;

            if (!Evaluate(quest)) OnQuestChanged?.Invoke(quest); // counted progress, same step
        }

        private void Detach(QuestSO quest)
        {
            if (detachers.Remove(quest, out Action detach)) detach?.Invoke();
        }

        #endregion

        #region Time and prerequisites

        private void HandlePhaseChanged(DAY_PHASE previous, DAY_PHASE next) => RefreshAllExpirations();

        private void RefreshAllExpirations()
        {
            buffer.Clear();
            buffer.AddRange(progress.Keys);
            foreach (QuestSO quest in buffer) RefreshExpiration(quest);
        }

        private void RefreshExpiration(QuestSO quest)
        {
            if (!progress.TryGetValue(quest, out QuestProgress p)) return;
            if (p.state != QUEST_STATE.Active && p.state != QUEST_STATE.ReadyToTurnIn) return;

            GameClock clock = GameClock.Instance;
            if (clock == null || !quest.IsPastTurnInWindow(clock.CurrentPhase)) return;

            Detach(quest);
            p.state = QUEST_STATE.Expired;
            Save();
            OnQuestChanged?.Invoke(quest);
        }

        private bool PrerequisitesMet(QuestSO quest)
        {
            foreach (QuestSO required in quest.prerequisiteQuests)
            {
                if (required == null) continue;
                if (!progress.TryGetValue(required, out QuestProgress p) || p.state != QUEST_STATE.Completed)
                    return false;
            }

            return true;
        }

        #endregion

        #region Save

        private void Load()
        {
            if (!File.Exists(SavePath)) return;

            try
            {
                var data = JsonUtility.FromJson<QuestSaveData>(File.ReadAllText(SavePath));
                if (data?.quests == null) return;

                foreach (QuestProgress p in data.quests)
                {
                    QuestSO quest = quests.Find(q => q != null && q.questID == p.id);
                    if (quest == null)
                    {
                        Debug.LogWarning($"[QuestManager] Saved quest '{p.id}' no longer exists, dropped.");
                        dirty = true;
                        continue;
                    }

                    progress[quest] = p;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestManager] Could not read {SavePath}: {e.Message}");
            }
        }

        private void Save()
        {
            var data = new QuestSaveData();
            data.quests.AddRange(progress.Values);

            try
            {
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                File.Copy(temp, SavePath, true);
                Debug.Log($"[QuestManager] Quest state saved to {SavePath}");
                File.Delete(temp);
                dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestManager] Could not write {SavePath}: {e.Message}");
            }
        }

        #endregion
    }
}