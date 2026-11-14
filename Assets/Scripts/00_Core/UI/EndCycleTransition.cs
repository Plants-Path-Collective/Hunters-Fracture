using System;
using DG.Tweening;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Full-screen black overlay driven by a CanvasGroup alpha tween.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class EndCycleTransition : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("CanvasGroup of the black overlay. Must NOT be a parent of the end-of-day UI.")]
        private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float fadeToBlackDuration = 2f;
        [SerializeField] private float fadeFromBlackDuration = 1f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        private Tween fadeTween;

        private void Reset() => canvasGroup = GetComponent<CanvasGroup>();

        private void Awake() => SetVisible(0f);

        /// <summary>Raises the alpha to 1 and invokes onComplete once the screen is fully black.</summary>
        public void FadeToBlack(Action onComplete = null)
        {
            fadeTween?.Kill();
            canvasGroup.blocksRaycasts = true;

            fadeTween = canvasGroup.DOFade(1f, fadeToBlackDuration)
                .SetEase(ease)
                .SetUpdate(true)               // unscaled time, so timescale 0 can't freeze it
                .SetLink(gameObject)           // auto-kill if this object is destroyed
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Lowers the alpha to 0 and invokes onComplete when the screen is clear.</summary>
        public void FadeFromBlack(Action onComplete = null)
        {
            fadeTween?.Kill();

            if (canvasGroup.alpha <= 0.001f)
            {
                SetVisible(0f);
                onComplete?.Invoke();
                return;
            }

            fadeTween = canvasGroup.DOFade(0f, fadeFromBlackDuration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
        }

        private void SetVisible(float alpha)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = alpha > 0f;
            canvasGroup.interactable = false;
        }
    }
}