using System;
using UnityEngine;
using Core.ChallengesSystem;

namespace Core.QuestsSystem
{
    /// <summary>Counts a challenge stat (e.g. enemies defeated) from the moment the step starts.</summary>
    [CreateAssetMenu(fileName = "Cond_Kill_", menuName = "Quests/Conditions/Kill Enemies")]
    public class KillEnemiesConditionSO : QuestConditionSO
    {
        [Tooltip("Stat reported by combat (set it to the 'enemies defeated' stat).")]
        [SerializeField] private CHALLENGE_STAT stat;

        [Tooltip("How many to count.")]
        [SerializeField, Min(1)] private int amount = 3;

        public override int Target => Mathf.Max(1, amount);

        public override int GetCurrent(int counted) => counted;

        public override Action Listen(Action<int> onEvent)
        {
            void Handler(CHALLENGE_STAT reported, int delta)
            {
                if (reported == stat) onEvent(delta);
            }

            ChallengeEvents.OnStatReported += Handler;
            return () => ChallengeEvents.OnStatReported -= Handler;
        }
    }
}