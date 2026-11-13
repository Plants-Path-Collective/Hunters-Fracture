using System;
using UnityEngine;
using Core.Clock;

namespace Core
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [ReadOnly] public END_CYCLE_REASONS endReason;
        public event Action OnResetCycle;

        private void OnEnable()
        {
            if (GameClock.Instance != null)
                TryToSubscribeToGameClock();
            else
                Debug.LogError("[RunManager] Failed to subscribe to the GameClock, no instance found");
        }

        void OnDisable()
        {
            GameClock.Instance.OnDayEnded -= EndDay;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void TryToSubscribeToGameClock()
        {
            GameClock.Instance.OnDayEnded += EndDay;
            Debug.Log("[RunManager] Subscribed correctly to GameClock");
        }

        private void EndDay()
        {
            END_CYCLE_REASONS reason = endReason;

            Debug.Log("[RunManager] Day ended, reason: " + reason);

            OnResetCycle?.Invoke();
        }
    }
}
