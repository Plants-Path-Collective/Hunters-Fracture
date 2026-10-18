using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.Units;
using TMPro;
using DG.Tweening;

namespace Core.UI
{
    /// <summary>
    /// One row of the turn-order list. Positioned by CombatTimelineView (no LayoutGroup), which
    /// tweens it to the slot of its index. Owns its own tweens so a new reorder can cut them.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TimelineEntryView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Image frame;
        [Tooltip("Optional.")]
        [SerializeField] private TMP_Text nameLabel;
        [Tooltip("Optional: shown only on the unit that is acting right now.")]
        [SerializeField] private GameObject currentMarker;

        [Header("--- Look ---")]
        [SerializeField] private Color allyColor = new(0.25f, 0.6f, 1f);
        [SerializeField] private Color enemyColor = new(1f, 0.35f, 0.3f);
        [SerializeField] private float currentScale = 1.15f;

        private RectTransform rect;
        private CanvasGroup group;

        /// <summary>The unit this row currently shows. Null while the row is free in the pool.</summary>
        public Unit Unit { get; private set; }
        public bool InUse { get; private set; }

        public float Y => rect.anchoredPosition.y;
        public float Alpha => group.alpha;

        private void Awake()
        {
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
        }

        public void Bind(Unit unit, bool isCurrent)
        {
            Unit = unit;
            InUse = true;
            gameObject.SetActive(true);

            if (portrait != null)
            {
                portrait.sprite = unit.Portrait;
                portrait.enabled = unit.Portrait != null;
            }

            if (frame != null)
                frame.color = unit.Team == UNIT_TEAM.Ally ? allyColor : enemyColor;

            if (nameLabel != null)
                nameLabel.text = unit.Name;

            if (currentMarker != null)
                currentMarker.SetActive(isCurrent);

            transform.localScale = isCurrent ? Vector3.one * currentScale : Vector3.one;
        }

        public void SetY(float y) => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        public void SetAlpha(float alpha) => group.alpha = alpha;

        // Plain DOTween.To, so the DOTween UI module is not required. Tweens are tagged with this
        // row (SetTarget) so KillTweens() can cut exactly its own, and linked to its GameObject.
        public Tween MoveTo(float y, float duration, float delay = 0f) =>
            DOTween.To(() => rect.anchoredPosition.y, SetY, y, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutCubic)
                .SetTarget(this)
                .SetLink(gameObject);

        public Tween FadeTo(float alpha, float duration, float delay = 0f) =>
            DOTween.To(() => group.alpha, a => group.alpha = a, alpha, duration)
                .SetDelay(delay)
                .SetTarget(this)
                .SetLink(gameObject);

        public void KillTweens() => DOTween.Kill(this);

        /// <summary>Back to the pool: cuts tweens and hides the row.</summary>
        public void Release()
        {
            KillTweens();
            Unit = null;
            InUse = false;
            gameObject.SetActive(false);
        }

        /// <summary>Marks the row as taken. Rebuild() acquires every missing row before binding any of
        /// them, so the pool has to know right away that this one is no longer free.</summary>
        public void Claim() => InUse = true;
    }
}