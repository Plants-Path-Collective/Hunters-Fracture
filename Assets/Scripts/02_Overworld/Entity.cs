using DG.Tweening;
using UnityEngine;

namespace Overworld
{
    /// <summary>
    /// Base class for any combat-capable Overworld entity (Player or Enemy). Owns the
    /// attack animation and trigger-based hit detection.
    /// Subclasses only decide what triggers the attack (input vs AI — Overworld AI isn't
    /// designed yet) and, optionally, what happens once a hit connects.
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        [Header("----- Attack -----")]
        [SerializeField] private Transform visualsOrigin;
        [SerializeField] private Vector3 swingEulerAngles = new(0f, 0f, 90f);
        [SerializeField] private float attackDuration = 0.45f;
        [SerializeField] private float attackHitWindow = 0.15f;

        private AttackSource attackSource;
        private Tween attackTween;
        private Quaternion restRotation;
        private bool attackSetupValid;

        protected virtual void Awake()
        {
            if (visualsOrigin == null)
            {
                Debug.LogError($"[{nameof(Entity)}] Assign a sword pivot to {nameof(visualsOrigin)} on {name}.", this);
                return;
            }

            attackSource = visualsOrigin.GetComponentInChildren<AttackSource>(true);
            if (attackSource == null)
            {
                Debug.LogError($"[{nameof(Entity)}] No {nameof(AttackSource)} found under {nameof(visualsOrigin)} on {name}.", this);
                return;
            }

            attackSetupValid = attackSource.Initialize(this);
            restRotation = visualsOrigin.localRotation;
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
            if (!attackSetupValid) return;
            if (isAttacking) return;

            isAttacking = true;
            Debug.Log($"[{GetType().Name}] {name} started an attack.");
            PlayAttackPlaceholder();
        }

        /// <summary>
        /// Starts the attack swing and opens the trigger hitbox at the impact moment.
        /// </summary>
        protected abstract void PlayAttackPlaceholder();

        /// <summary>
        /// Called at the animation's impact moment. Once a real animation is available, call
        /// this from its Animation Event instead.
        /// </summary>
        protected void OnAttackHitFrame()
        {
            attackSource.BeginAttack();
        }

        /// <summary>
        /// Swings the sword around its assigned pivot and enables its trigger collider at impact.
        /// </summary>
        protected void PlayAttackAnimation()
        {
            attackTween?.Kill();

            float duration = Mathf.Max(attackDuration, 0.03f);
            float hitWindow = Mathf.Clamp(attackHitWindow, 0f, duration - 0.02f);
            float swingDuration = (duration - hitWindow) * 0.5f;
            float impactTime = swingDuration;
            Vector3 swingRotation = restRotation.eulerAngles + swingEulerAngles;

            attackTween = DOTween.Sequence()
                .Append(visualsOrigin.DOLocalRotate(swingRotation, swingDuration)
                    .SetEase(Ease.OutCubic))
                .InsertCallback(impactTime, OnAttackHitFrame)
                .InsertCallback(impactTime + hitWindow, EndAttackHitWindow)
                .Append(visualsOrigin.DOLocalRotateQuaternion(restRotation, swingDuration)
                    .SetEase(Ease.InCubic))
                .OnComplete(EndAttack)
                .SetLink(gameObject);
        }

        private void EndAttackHitWindow()
        {
            if (!attackSource.EndAttack())
                OnAttackMissed();
        }

        internal void RegisterAttackHit(Entity target)
        {
            if (!isAttacking || target == this) return;

            OnHitConnected(target);
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

        protected virtual void OnAttackMissed()
        {
        }

        /// <summary>
        /// Clears the attacking lock. Subclasses call this once the placeholder animation (and
        /// its hit check) is fully done.
        /// </summary>
        protected void EndAttack()
        {
            attackSource?.EndAttack();
            isAttacking = false;
        }
    }
}