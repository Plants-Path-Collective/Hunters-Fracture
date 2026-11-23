using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.ChallengesSystem;
using Core.QuestsSystem;
using Core.Clock;
using Core.UI;

namespace Core
{
    /// <summary>
    /// Single entry point that ends and starts a run (day cycle).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private EndCycleTransition endCycleTransition;

        [Header("Flow")]
        [SerializeField, Tooltip("Scene loaded when a new cycle starts.")]
        private string startingSceneName = "Social";
        [SerializeField, Tooltip("If true, the reset continues right after the end screen is requested.")]
        private bool autoConfirm = false;

        public bool IsTransitioning { get; private set; }
        public END_CYCLE_REASONS LastEndReason { get; private set; }

        /// <summary>Fired when the end of the cycle begins (fade starts).</summary>
        public event Action<END_CYCLE_REASONS> OnCycleEnding;
        /// <summary>Fired when the screen is fully black. The UI Manager shows the end-of-day UI here.</summary>
        public event Action<END_CYCLE_REASONS> OnEndScreenRequested;
        /// <summary>Fired while the screen is black so subsystems can reset themselves (party, inventory...).</summary>
        public event Action OnResetCycle;
        /// <summary>Fired when the new cycle is visible and the clock is running again.</summary>
        public event Action OnCycleStarted;

        // Private variables
        private bool awaitingConfirm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (Instance != this) return;

            if (GameClock.Instance == null)
            {
                Debug.LogError("[RunManager] GameClock instance not found.");
                return;
            }
            GameClock.Instance.OnDayEnded += HandleDayEnded;

            if (endCycleTransition == null)
                Debug.LogError("[RunManager] EndCycleTransition reference is missing.");
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (GameClock.Instance != null)
                GameClock.Instance.OnDayEnded -= HandleDayEnded;
            Instance = null;
        }

        private void HandleDayEnded() => EndDay(END_CYCLE_REASONS.TimeOut);

        /// <summary>Ends the current day/run. Called by the clock, a bed, a console command, etc.</summary>
        public void EndDay(END_CYCLE_REASONS reason)
        {
            if (IsTransitioning) return;

            if (endCycleTransition == null)
            {
                Debug.LogError("[RunManager] EndCycleTransition reference is missing. Assign it in the Inspector.");
                return;
            }

            IsTransitioning = true;
            LastEndReason = reason;

            Debug.Log($"[RunManager] Day ended, reason: {reason}");
            OnCycleEnding?.Invoke(reason);

            GameClock.Instance.Pause(CLOCK_PAUSE_REASON.Transition);
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty); // adjust to your API

            endCycleTransition.FadeToBlack(() =>
            {
                awaitingConfirm = true;
                OnEndScreenRequested?.Invoke(reason);
                if (autoConfirm) ConfirmEndOfDay();
            });
        }

        /// <summary>Called by the end-of-day UI (button or timer) to continue to the next run.</summary>
        public void ConfirmEndOfDay()
        {
            if (!IsTransitioning || !awaitingConfirm) return;
            awaitingConfirm = false;
            ResetRun();
        }
        /// <summary>Starts the very first run (e.g. "New game" in the Main Menu), without an end screen.</summary>
        public void StartRun()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            ResetRun();
        }

        private void ResetRun()
        {
            // Defeat leaves the Combat pause active, so clear everything and keep only our own.
            GameClock.Instance.ClearAllPauses();
            GameClock.Instance.Pause(CLOCK_PAUSE_REASON.Transition);

            ChallengeManager.Instance?.BeginRun();   // resets incomplete PerRun challenges
            QuestManager.Instance?.BeginRun();       // clears quest states and progress
            OnResetCycle?.Invoke();                  // party HP/SP/inventory, etc.
            GameClock.Instance.ResetClock();         // Day = 1, phase = Morning (needs adding)

            // Single mode also unloads CombatStage if it was still loaded.
            AsyncOperation load = SceneManager.LoadSceneAsync(startingSceneName, LoadSceneMode.Single);
            load.completed += _ => FinishReset();
        }

        private void FinishReset()
        {
            // The scene's SceneSetter restores the input map in its Start().
            endCycleTransition.FadeFromBlack(() =>
            {
                GameClock.Instance.Resume(CLOCK_PAUSE_REASON.Transition);
                IsTransitioning = false;
                OnCycleStarted?.Invoke();
            });
        }
    }
}