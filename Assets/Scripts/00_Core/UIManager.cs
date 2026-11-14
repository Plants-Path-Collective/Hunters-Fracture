using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("--- End of the Cycle Elements ---")]
        [Tooltip("CanvasGroup of the end-of-day screen. Must be in a separate Canvas above the black overlay.")]
        public CanvasGroup canvasGroup;
        [SerializeField] private float showDuration = 0.75f;

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

            SetEndScreenState(0f);

            if (RunManager.Instance == null)
            {
                Debug.LogError("[UIManager] Failed to subscribe to RunManager, instance is null");
                return;
            }
            RunManager.Instance.OnEndScreenRequested += ShowEndOfDayScreen;
            RunManager.Instance.OnResetCycle += HideEndOfDayScreen;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnEndScreenRequested -= ShowEndOfDayScreen;
                RunManager.Instance.OnResetCycle -= HideEndOfDayScreen;
            }
            Instance = null;
        }

        private void ShowEndOfDayScreen(END_CYCLE_REASONS reason)
        {
            // TODO: pick a localized title/body per reason (TimeOut, GoToSleep, GameOver).
            endScreenTween?.Kill();
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // The RunManager set the Empty map; the screen needs UI input to press Continue.
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.UI);

            endScreenTween = canvasGroup.DOFade(1f, showDuration)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void HideEndOfDayScreen()
        {
            endScreenTween?.Kill();
            SetEndScreenState(0f);
        }

        /// <summary>Hook this to the Continue button's OnClick.</summary>
        public void OnContinuePressed()
        {
            canvasGroup.interactable = false; // avoid double confirmation
            RunManager.Instance.ConfirmEndOfDay();
        }

        private void SetEndScreenState(float alpha)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = alpha > 0f;
            canvasGroup.interactable = alpha > 0f;
        }
    }
}