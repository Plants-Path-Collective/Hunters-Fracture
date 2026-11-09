using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Clock
{
    /// <summary>
    /// Persistent fictional clock. Splits the day into <see cref="DAY_PHASE"/> phases, advances only
    /// while no pause reason is active, and raises events when the phase changes or the day ends.
    /// Consumers should derive their state from <see cref="CurrentPhase"/> instead of accumulating ticks,
    /// so phase jumps, loading and console tests work without special cases.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        /// <summary>Singleton instance. Can be null in scenes loaded without the persistent objects.</summary>
        public static GameClock Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Durations per phase and the phase a new day starts in.")]
        [SerializeField] private GameClockSettingsSO settings;

        /// <summary>Raised when the phase changes (including jumps). Args: old phase, new phase.</summary>
        public event Action<DAY_PHASE, DAY_PHASE> OnPhaseChanged;

        /// <summary>Raised once when the night ends. The clock stops until <see cref="ResetClock"/> is called.</summary>
        public event Action OnDayEnded;

        /// <summary>Current day of the run. A day is a run for now, so this is mostly informative.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>Current phase of the day. Always a single phase, never a combined mask.</summary>
        public DAY_PHASE CurrentPhase { get; private set; } = DAY_PHASE.Morning;

        /// <summary>True after the night ended and before the next <see cref="ResetClock"/>.</summary>
        public bool IsDayOver { get; private set; }

        /// <summary>Debug multiplier applied to the clock speed. 1 is normal speed.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Progress (0-1) inside the current phase. Phases are uneven, so this is per phase, not per day.</summary>
        public float PhaseProgress
        {
            get
            {
                float duration = CurrentPhaseDuration;
                return duration > 0f ? Mathf.Clamp01(elapsedInPhase / duration) : 1f;
            }
        }

        /// <summary>True if at least one pause reason is active.</summary>
        public bool IsPaused => activePauseCount > 0;

        /// <summary>True when the clock is advancing right now.</summary>
        public bool IsRunning => settings != null && !IsDayOver && !IsPaused;

        private readonly Dictionary<CLOCK_PAUSE_REASON, int> pauseCounts = new Dictionary<CLOCK_PAUSE_REASON, int>();
        private int activePauseCount;
        private float elapsedInPhase;

        private float CurrentPhaseDuration =>
            settings != null ? Mathf.Max(settings.GetDurationSeconds(CurrentPhase), 0.01f) : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (settings == null)
            {
                Debug.LogError("[GameClock] No GameClockSettingsSO assigned. The clock will not run.", this);
                return;
            }

            // Initial state without raising events: nobody can be subscribed yet.
            Day = 1;
            IsDayOver = false;
            elapsedInPhase = 0f;
            CurrentPhase = settings.StartPhase;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsRunning) return;

            elapsedInPhase += Time.deltaTime * Mathf.Max(0f, SpeedMultiplier);

            // A while loop handles large deltas (e.g. high debug speed) crossing several phases.
            while (!IsDayOver && elapsedInPhase >= CurrentPhaseDuration)
            {
                elapsedInPhase -= CurrentPhaseDuration;
                AdvancePhase();
            }
        }

        /// <summary>Returns true if the current phase is part of the given phase mask.</summary>
        public bool IsIn(DAY_PHASE mask) => (mask & CurrentPhase) != 0;

        /// <summary>
        /// Jumps to a phase and restarts its progress. Also reopens the day if it was over.
        /// Used by the console now, and by sleeping or time skips later.
        /// </summary>
        /// <param name="phase">A single phase. Combined masks are rejected.</param>
        public void JumpTo(DAY_PHASE phase)
        {
            if (!IsSinglePhase(phase))
            {
                Debug.LogWarning($"[GameClock] JumpTo expects a single phase, got '{phase}'.", this);
                return;
            }

            IsDayOver = false;
            elapsedInPhase = 0f;
            ChangePhase(phase);
        }

        /// <summary>
        /// Resets the clock to day 1 and the configured start phase. Does not clear active pauses:
        /// whoever paused the clock (e.g. the run reset transition) is responsible for resuming it.
        /// </summary>
        public void ResetClock()
        {
            if (settings == null) return;

            Day = 1;
            IsDayOver = false;
            elapsedInPhase = 0f;
            ChangePhase(settings.StartPhase);
        }

        /// <summary>
        /// Ends the day immediately, as if the night had run out. Debug helper; gameplay should end the
        /// day through the run manager so fades and resets are not skipped.
        /// </summary>
        public void ForceEndDay()
        {
            if (IsDayOver) return;
            IsDayOver = true;
            elapsedInPhase = CurrentPhaseDuration;
            OnDayEnded?.Invoke();
        }

        /// <summary>Adds a pause for the given reason. Every call must be matched with <see cref="Resume"/>.</summary>
        public void Pause(CLOCK_PAUSE_REASON reason)
        {
            pauseCounts.TryGetValue(reason, out int count);
            pauseCounts[reason] = count + 1;
            activePauseCount++;
        }

        /// <summary>Removes one pause for the given reason. Extra calls are ignored.</summary>
        public void Resume(CLOCK_PAUSE_REASON reason)
        {
            if (!pauseCounts.TryGetValue(reason, out int count) || count <= 0) return;

            pauseCounts[reason] = count - 1;
            activePauseCount--;
        }

        /// <summary>Returns true if the given pause reason is currently active.</summary>
        public bool IsPausedBy(CLOCK_PAUSE_REASON reason) =>
            pauseCounts.TryGetValue(reason, out int count) && count > 0;

        /// <summary>Null-safe pause for systems that also run in scenes without a clock (test scenes).</summary>
        public static void RequestPause(CLOCK_PAUSE_REASON reason)
        {
            if (Instance != null) Instance.Pause(reason);
        }

        /// <summary>Null-safe counterpart of <see cref="RequestPause"/>.</summary>
        public static void ReleasePause(CLOCK_PAUSE_REASON reason)
        {
            if (Instance != null) Instance.Resume(reason);
        }

        /// <summary>Clears every active pause. Debug only: it hides bugs where a system forgot to resume.</summary>
        public void ClearAllPauses()
        {
            pauseCounts.Clear();
            activePauseCount = 0;
        }

        private void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case DAY_PHASE.Morning:
                    ChangePhase(DAY_PHASE.Afternoon);
                    break;
                case DAY_PHASE.Afternoon:
                    ChangePhase(DAY_PHASE.Night);
                    break;
                default:
                    // Night ended: stop at 100% and let the run manager decide what happens next.
                    IsDayOver = true;
                    elapsedInPhase = CurrentPhaseDuration;
                    OnDayEnded?.Invoke();
                    break;
            }
        }

        private void ChangePhase(DAY_PHASE next)
        {
            DAY_PHASE previous = CurrentPhase;
            CurrentPhase = next;

            if (previous == next) return;

            Debug.Log($"[GameClock] Phase changed: {previous} -> {next} (day {Day}).", this);
            OnPhaseChanged?.Invoke(previous, next);
        }

        private static bool IsSinglePhase(DAY_PHASE phase)
        {
            int value = (int)phase;
            return value > 0 && Mathf.IsPowerOfTwo(value);
        }
    }
}