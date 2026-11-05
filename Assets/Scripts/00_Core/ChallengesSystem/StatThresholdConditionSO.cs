using System;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Completes when a numeric stat accumulates a target amount
    /// (e.g. kill 100 enemies, collect 20 items).
    /// </summary>
    [CreateAssetMenu(menuName = "Challenges/Conditions/Stat Threshold", fileName = "StatThreshold_")]
    public class StatThresholdConditionSO : ChallengeConditionSO
    {
        [Tooltip("Stat that is accumulated.")]
        [SerializeField] private CHALLENGE_STAT stat;

        [Tooltip("Amount of the stat required to complete the challenge.")]
        [SerializeField, Min(1)] private int target = 10;

        public override int Target => target;

        public override Action Attach(Action<int> addProgress)
        {
            Action<CHALLENGE_STAT, int> handler = (reported, amount) =>
            {
                if (reported == stat) addProgress(amount);
            };

            ChallengeEvents.OnStatReported += handler;
            return () => ChallengeEvents.OnStatReported -= handler;
        }
    }
}