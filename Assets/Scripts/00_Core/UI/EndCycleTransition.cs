using System;
using DG.Tweening;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Handles the two layers of the end-of-cycle screen: a black overlay that fades first,
    /// and the day-change screen that fades in once the black is solid.
    /// </summary>
    public class EndCycleTransition : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Solid black overlay. Fades first.")]
        private CanvasGroup blackout;
        [SerializeField, Tooltip("Day-change screen (text + Continue button). Fades after the blackout is solid.")]
        private CanvasGroup dayScreen;

        [Header("Fade to black")]
        [SerializeField] private float blackoutFadeInDuration = 2f;
        [SerializeField, Tooltip("Pause between the black being solid and the day screen starting to appear.")]
        private float holdBeforeDayScreen = 0.25f;
        [SerializeField] private float dayScreenFadeInDuration = 0.75f;

        [Header("Fade from black")]
        [SerializeField] private float dayScreenFadeOutDuration = 0.5f;
        [SerializeField] private float blackoutFadeOutDuration = 1f;

        [SerializeField] private Ease ease = Ease.InOutSine;

        private Sequence sequence;

        private void Awake()
        {
            SetGroup(blackout, 0f, false);
            SetGroup(dayScreen, 0f, false);
        }

        /// <summary>
        /// Blackout fades in, then the day screen fades in. onComplete is invoked once
        /// the day screen is fully visible and interactable.
        /// </summary>
        public void FadeToBlack(Action onComplete = null)
        {
            sequence?.Kill();

            blackout.blocksRaycasts = true;
            dayScreen.alpha = 0f;
            dayScreen.blocksRaycasts = true;
            dayScreen.interactable = false;

            sequence = DOTween.Sequence()
                .Append(blackout.DOFade(1f, blackoutFadeInDuration).SetEase(ease))
                .AppendInterval(holdBeforeDayScreen)
                .Append(dayScreen.DOFade(1f, dayScreenFadeInDuration).SetEase(ease))
                .SetUpdate(true)               // unscaled time; applies to the whole sequence
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    dayScreen.interactable = true;
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// Day screen fades out, then the blackout fades out. onComplete is invoked when the screen is clear.
        /// </summary>
        public void FadeFromBlack(Action onComplete = null)
        {
            sequence?.Kill();
            dayScreen.interactable = false;

            if (blackout.alpha <= 0.001f && dayScreen.alpha <= 0.001f)
            {
                SetGroup(blackout, 0f, false);
                SetGroup(dayScreen, 0f, false);
                onComplete?.Invoke();
                return;
            }

            sequence = DOTween.Sequence()
                .Append(dayScreen.DOFade(0f, dayScreenFadeOutDuration).SetEase(ease))
                .Append(blackout.DOFade(0f, blackoutFadeOutDuration).SetEase(ease))
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetGroup(blackout, 0f, false);
                    SetGroup(dayScreen, 0f, false);
                    onComplete?.Invoke();
                });
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool blocksRaycasts)
        {
            group.alpha = alpha;
            group.blocksRaycasts = blocksRaycasts;
            group.interactable = false;
        }
    }
}