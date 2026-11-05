using System;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Base class for "what must happen" in a challenge.
    /// The asset is shared, so it must NOT store per-challenge state:
    /// Attach() captures everything in a closure and returns the detach delegate.
    /// </summary>
    public abstract class ChallengeConditionSO : ScriptableObject
    {
        /// <summary>Progress value needed to complete the challenge.</summary>
        public abstract int Target { get; }

        /// <summary>
        /// Starts listening for events relevant to this condition.
        /// </summary>
        /// <param name="addProgress">Callback to invoke with the progress delta.</param>
        /// <returns>A delegate that stops listening when invoked.</returns>
        public abstract Action Attach(Action<int> addProgress);
    }
}