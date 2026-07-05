using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CombatSystem.Minigame
{
    /// <summary>
    /// Data container for a single step in a Disputa or Alianza sequence.
    /// </summary>
    [Serializable]
    public class MinigameStep
    {
        public FACE_BUTTON button;       // Button the player must press
        public float       windowStart;  // Seconds from sequence start when window opens
        public float       windowEnd;    // Seconds from sequence start when window closes
        public bool        wasHit;       // Set at runtime by MinigameController
    }

    /// <summary>
    /// Generates a randomized sequence of MinigameSteps for Disputa or Alianza.
    /// Immutable once generated — MinigameController drives playback.
    /// </summary>
    public class MinigameSequence : MonoBehaviour
    {
        [Header("Sequence Settings")]
        [Tooltip("Minimum number of steps per sequence.")]
        [SerializeField] private int minSteps = 3;

        [Tooltip("Maximum number of steps per sequence.")]
        [SerializeField] private int maxSteps = 5;

        [Tooltip("Seconds the input window stays open per step.")]
        [SerializeField] private float windowDuration = 0.6f;

        [Tooltip("Seconds between the start of consecutive steps.")]
        [SerializeField] private float stepInterval = 1.2f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Generates and returns a new randomized step list.</summary>
        public List<MinigameStep> Generate()
        {
            int count = Random.Range(minSteps, maxSteps + 1);
            var steps = new List<MinigameStep>(count);

            for (int i = 0; i < count; i++)
            {
                float start = i * stepInterval;
                steps.Add(new MinigameStep
                {
                    button      = (FACE_BUTTON)Random.Range(0, 4),
                    windowStart = start,
                    windowEnd   = start + windowDuration,
                    wasHit      = false
                });
            }

            return steps;
        }

        /// <summary>Total duration of a generated sequence in seconds.</summary>
        public float GetTotalDuration(List<MinigameStep> steps)
        {
            if (steps == null || steps.Count == 0) return 0f;
            return steps[steps.Count - 1].windowEnd;
        }
    }
}