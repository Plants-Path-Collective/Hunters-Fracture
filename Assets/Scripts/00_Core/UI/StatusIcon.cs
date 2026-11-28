using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.StatusSystem;

namespace Core.UI
{
    /// <summary>
    /// One status on one unit's bars. UnitBarsView spawns it, then drives it with SetStatus (first
    /// appearance), SetTurns (refresh or countdown) and Remove (status gone).
    /// The counter is only shown while more than one turn remains.
    /// </summary>
    public class StatusIcon : MonoBehaviour
    {
        [Header("--- Animation ---")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float popDuration = 0.2f;

        public int remainingTurns;

        public object Key { get; private set; }
        /// <summary>The status this icon represents. UnitBarsView uses it to match icons to statuses.</summary>
        public StatusSO Definition => Key as StatusSO;

        private TextMeshProUGUI turnsText;
        private Image icon;
        private Tween textTween;
        private bool textVisible;
        private bool removing;

        private void Awake()
        {
            icon = GetComponent<Image>();
            turnsText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Show(object key, Sprite sprite, int count, Color tint)
        {
            Key = key;
            remainingTurns = count;
            icon.sprite = sprite;
            icon.color = new Color(tint.r, tint.g, tint.b, 0f);
            turnsText.text = count.ToString();

            turnsText.transform.localScale = Vector3.zero;
            textVisible = false;

            icon.DOFade(tint.a, fadeDuration)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    if (remainingTurns > 1 && !textVisible && !removing) ShowText();
                });
        }

        /// <summary>The count changed: a refresh from a new application or a turn countdown.</summary>
        public void SetTurns(int turns)
        {
            if (removing || turns == remainingTurns) return;

            remainingTurns = turns;
            turnsText.text = turns.ToString();

            if (turns > 1)
            {
                if (textVisible) PunchText();
                else ShowText();
            }
            else if (textVisible)
            {
                // Down to the last turn: the counter has nothing left to say.
                HideText();
            }
        }

        /// <summary>The status is gone: fades everything out, then destroys this object.</summary>
        public void Remove()
        {
            if (removing) return;
            removing = true;

            icon.DOKill();
            textTween?.Kill();

            Sequence outro = DOTween.Sequence().SetLink(gameObject);
            outro.Append(icon.DOFade(0f, fadeDuration));
            outro.Join(turnsText.DOFade(0f, fadeDuration));
            outro.OnComplete(() => Destroy(gameObject));
        }

        private void ShowText()
        {
            textTween?.Kill();
            textVisible = true;
            textTween = turnsText.transform.DOScale(1f, popDuration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        private void HideText()
        {
            textTween?.Kill();
            textVisible = false;
            textTween = turnsText.transform.DOScale(0f, popDuration * 0.5f)
                .SetEase(Ease.InBack)
                .SetLink(gameObject);
        }

        private void PunchText()
        {
            textTween?.Kill(true); // finish any running pop so the punch starts from scale 1
            textTween = turnsText.transform.DOPunchScale(Vector3.one * 0.35f, 0.25f, 6, 0.5f)
                .SetLink(gameObject);
        }

        private void SetIconAlpha(float alpha)
        {
            Color c = icon.color;
            c.a = alpha;
            icon.color = c;
        }
    }
}