using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Tween endScreenTween;

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

        private void Start()
        {
            if (Instance != this) return;

            if (RunManager.Instance == null)
            {
                Debug.LogError("[UIManager] Failed to subscribe to RunManager, instance is null");
                return;
            }
            RunManager.Instance.OnEndScreenRequested += ShowEndOfDayScreen;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (RunManager.Instance != null)
                RunManager.Instance.OnEndScreenRequested -= ShowEndOfDayScreen;
            Instance = null;
        }

        private void ShowEndOfDayScreen(END_CYCLE_REASONS reason)
        {
            // TODO: animate change of day, then come back to prev day and Merna vc plays repeting cycle
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.UI);
        }

        /// <summary>Hook this to the Continue button's OnClick.</summary>
        public void OnContinuePressed() => RunManager.Instance.ConfirmEndOfDay();
    }
}