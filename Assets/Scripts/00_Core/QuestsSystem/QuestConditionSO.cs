using System;
using UnityEngine;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Base class for "what the player must do" in a quest step.
    /// The asset is shared, so it must NOT store per-quest state: the manager owns the counter.
    /// </summary>
    public abstract class QuestConditionSO : ScriptableObject
    {
        /// <summary>Value that completes the step (e.g. 3 kills, 1000 pesos).</summary>
        public abstract int Target { get; }

        /// <summary>
        /// Current value of the condition.
        /// <paramref name="counted"/> is the sum of the deltas sent through <see cref="Listen"/>
        /// since the step started; state-based conditions (items, money) ignore it.
        /// </summary>
        public abstract int GetCurrent(int counted);

        /// <summary>
        /// Optional. Event-based conditions (kills) listen here and call onEvent(delta).
        /// Return an action that stops listening. State-based conditions return null.
        /// </summary>
        public virtual Action Listen(Action<int> onEvent) => null;

        /// <summary>Checked on turn-in. False blocks the turn-in (e.g. not enough money to pay).</summary>
        public virtual bool CanConsume() => true;

        /// <summary>Called on a successful turn-in (e.g. spends the money).</summary>
        public virtual void Consume() { }
    }
}