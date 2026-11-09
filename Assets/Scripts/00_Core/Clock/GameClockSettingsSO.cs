using UnityEngine;

namespace Core.Clock
{
    /// <summary>
    /// Designer-facing configuration for <see cref="GameClock"/>: how long each day phase lasts
    /// and which phase a new day starts in. Referenced by the clock; not a Singleton.
    /// </summary>
    [CreateAssetMenu(fileName = "GameClockSettings", menuName = "Clock/Game Clock Settings")]
    public class GameClockSettingsSO : ScriptableObject
    {
        [Header("Phase durations (real minutes)")]
        [Tooltip("Duration of the morning phase, in real minutes.")]
        [Min(0.1f)] [SerializeField] private float morningMinutes = 8f;

        [Tooltip("Duration of the afternoon phase, in real minutes. The dungeon is open only during this phase.")]
        [Min(0.1f)] [SerializeField] private float afternoonMinutes = 11f;

        [Tooltip("Duration of the night phase, in real minutes. When it ends, the day is over.")]
        [Min(0.1f)] [SerializeField] private float nightMinutes = 6f;

        [Header("Start")]
        [Tooltip("Phase a new day starts in. Must be a single phase.")]
        [SerializeField] private DAY_PHASE startPhase = DAY_PHASE.Morning;

        /// <summary>Phase a new day starts in.</summary>
        public DAY_PHASE StartPhase => startPhase;

        /// <summary>Total length of a full day, in real seconds.</summary>
        public float TotalDurationSeconds =>
            (morningMinutes + afternoonMinutes + nightMinutes) * 60f;

        /// <summary>
        /// Returns the duration of a single phase in real seconds, or 0 if the value is not a single phase.
        /// </summary>
        public float GetDurationSeconds(DAY_PHASE phase)
        {
            switch (phase)
            {
                case DAY_PHASE.Morning:   return morningMinutes * 60f;
                case DAY_PHASE.Afternoon: return afternoonMinutes * 60f;
                case DAY_PHASE.Night:     return nightMinutes * 60f;
                default:                  return 0f;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // startPhase must be exactly one flag.
            int value = (int)startPhase;
            if (value <= 0 || !Mathf.IsPowerOfTwo(value))
                startPhase = DAY_PHASE.Morning;
        }
#endif
    }
}