using DG.Tweening;
using UnityEngine;

namespace Overworld
{
    /// <summary>
    /// Base class for any combat-capable Overworld entity (Player or Enemy). Owns the
    /// "pseudo-attack": a placeholder animation followed by a hit check via SphereCast.
    /// Subclasses only decide what triggers the attack (input vs AI — Overworld AI isn't
    /// designed yet) and, optionally, what happens once a hit connects.
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        [Header("----- Attack Detection -----")]
        [Tooltip("Origin of the SphereCast. Defaults to this transform if left empty.")]
        [SerializeField] protected Transform attackOrigin;
        [SerializeField] protected float attackRange = 1.5f;
        [SerializeField] protected float attackRadius = 0.5f;
        [Tooltip("Optional dedicated unit-sized sphere shown at the end of the attack SphereCast.")]
        [SerializeField] private GameObject attackRadiusFeedback;
        [SerializeField] private float attackFeedbackDuration = 0.3f;

        private Tween attackFeedbackTween;
        protected float AttackDuration => attackFeedbackDuration;

        protected virtual void Awake()
        {
            if (attackRadiusFeedback != null)
            {
                attackRadiusFeedback.transform.localScale = Vector3.zero;
                attackRadiusFeedback.SetActive(false);
            }
        }

        /// <summary>
        /// True while an attack is mid-flight (placeholder animation running). Prevents the
        /// same Entity from re-triggering TryAttack() before the current one resolves.
        /// </summary>
        protected bool isAttacking;

        /// <summary>
        /// Starts the attack: locks out re-triggering until it resolves, then plays the
        /// placeholder animation. Subclasses call this from whatever decides an attack should
        /// happen (player input, future enemy AI, a debug key, etc.).
        /// </summary>
        public virtual void TryAttack()
        {
            if (isAttacking) return;

            isAttacking = true;
            Debug.Log($"[{GetType().Name}] {name} started an attack.");
            BeginAttackRadiusFeedback();
            PlayAttackPlaceholder();
        }

        /// <summary>
        /// Placeholder attack animation (DOTween for now, one per concrete Entity). Whatever it
        /// does, it must call <see cref="OnAttackHitFrame"/> at the moment that should count as
        /// the "impact". Once real animations exist, that call moves to an Animation Event and
        /// this method's body changes — nothing else in this class needs to.
        /// </summary>
        protected abstract void PlayAttackPlaceholder();

        /// <summary>
        /// Hit-check entry point. Call this from the placeholder's impact callback now, and from
        /// an Animation Event once real animations exist. SphereCasts forward from
        /// <see cref="attackOrigin"/> and resolves the hit via <see cref="OnHitConnected"/> if it
        /// lands on another Entity.
        /// </summary>
        protected void OnAttackHitFrame()
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            ShowAttackRadiusFeedbackAtImpact(origin);

            Entity target = null;
            if (Physics.SphereCast(origin, attackRadius, transform.forward, out RaycastHit hit, attackRange))
            {
                // GetComponentInParent in case the collider that got hit lives on a child
                // (e.g. a dedicated hitbox) rather than on the Entity's own root.
                target = hit.collider.GetComponentInParent<Entity>();
            }

            RetractAttackRadiusFeedback();

            if (target != null && target != this)
                OnHitConnected(target);
        }

        private void BeginAttackRadiusFeedback()
        {
            if (attackRadiusFeedback == null) return;

            Transform feedbackTransform = attackRadiusFeedback.transform;
            attackFeedbackTween?.Kill();
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            feedbackTransform.position = origin + transform.forward * attackRange;
            feedbackTransform.localScale = Vector3.zero;
            attackRadiusFeedback.SetActive(true);

            attackFeedbackTween = feedbackTransform
                .DOScale(Vector3.one * (attackRadius * 2f), attackFeedbackDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(attackRadiusFeedback);
        }

        private void ShowAttackRadiusFeedbackAtImpact(Vector3 origin)
        {
            if (attackRadiusFeedback == null) return;

            attackFeedbackTween?.Kill();
            Transform feedbackTransform = attackRadiusFeedback.transform;
            feedbackTransform.position = origin + transform.forward * attackRange;
            feedbackTransform.localScale = Vector3.one * (attackRadius * 2f);
        }

        private void RetractAttackRadiusFeedback()
        {
            if (attackRadiusFeedback == null) return;

            Transform feedbackTransform = attackRadiusFeedback.transform;
            attackFeedbackTween = feedbackTransform
                .DOScale(Vector3.zero, 0.15f)
                .SetEase(Ease.InCubic)
                .SetLink(attackRadiusFeedback)
                .OnComplete(() => attackRadiusFeedback.SetActive(false));
        }

        /// <summary>
        /// Called when this Entity's attack connects with another Entity. Whichever side calls
        /// this first is the side that gets the Speed advantage on combat entry (see
        /// combat.html "Entrada a combate"). Default just logs — PlayerEntity/EnemyEntity override
        /// to actually start the fight via CombatTransition.
        /// </summary>
        protected virtual void OnHitConnected(Entity target)
        {
            Debug.Log($"[{GetType().Name}] {name} connected an attack on {target.name}.");
        }

        /// <summary>
        /// Clears the attacking lock. Subclasses call this once the placeholder animation (and
        /// its hit check) is fully done.
        /// </summary>
        protected void EndAttack() => isAttacking = false;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin + transform.forward * attackRange, attackRadius);
        }
#endif
    }
}