using System;
using UnityEngine;
using DG.Tweening;

namespace Core
{
    public class UIManager : MonoBehaviour 
    {
        public static UIManager Instance { get; private set; }

        // References
        [Header("--- End of the Cycle Elements ---")]
        public CanvasGroup canvasGroup;

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

        private void OnEnable() 
        {
            if (RunManager.Instance != null)
                TryToSubscribeToRunManager();
            else 
                Debug.LogError("[UIManager] Failed to subscribe to the cycle reset on RunManager, instance is null");
        }

        private void OnDisable()
        {
            RunManager.Instance.OnResetCycle -= EndCycleTransition;
        }

        private void TryToSubscribeToRunManager()
        {
            RunManager.Instance.OnResetCycle += EndCycleTransition;
            Debug.Log("[UIManager] Subscribed correctly to cycle reset on RunManager");
        }

        private void EndCycleTransition()
        {
            throw new NotImplementedException();
        }
    }
}