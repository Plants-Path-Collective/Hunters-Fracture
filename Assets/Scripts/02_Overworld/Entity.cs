using Core.CombatSystem;
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
        [Tooltip("Fraction of the forward swing during which the hitbox is already open. " +
                 "1 = open for the whole swing (the blade sweeps through targets); 0 = open only at the final pose.")]
        [SerializeField, Range(0f, 1f)] private float sweepFraction = 1f;

        private AttackSource attackSource;
        private Tween attackTween;
        private Quaternion restRotation;
        private bool attackSetupValid;
        private bool hitResolved;

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
            hitResolved = false;
            Debug.Log($"[{GetType().Name}] {name} started an attack.");
            PlayAttackPlaceholder();
        }

        /// <summary>
        /// Starts the attack swing and opens the trigger hitbox at the impact moment.
        /// </summary>
        protected abstract void PlayAttackPlaceholder();

        /// <summary>
        /// Opens the hitbox. Once a real animation is available, call this from its Animation
        /// Event (at the start of the blade's travel) instead.
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

            Sequence swing = DOTween.Sequence()
                .Append(visualsOrigin.DOLocalRotate(swingRotation, swingDuration)
                    .SetEase(Ease.OutCubic))
                .InsertCallback(impactTime + hitWindow, EndAttackHitWindow)
                .Append(visualsOrigin.DOLocalRotateQuaternion(restRotation, swingDuration)
                    .SetEase(Ease.InCubic))
                .OnComplete(CompleteAttack)
                .SetLink(gameObject);
            attackTween = swing;

            // Open the hitbox during the forward swing so the blade sweeps through targets instead
            // of only being tested at its final pose. Done after assigning attackTween: a hit on the
            // very first frame can start combat, which cancels the tween.
            float hitboxOpenTime = swingDuration * (1f - sweepFraction);
            if (hitboxOpenTime <= 0f)
                OnAttackHitFrame();
            else
                swing.InsertCallback(hitboxOpenTime, OnAttackHitFrame);
        }

        private void EndAttackHitWindow()
        {
            attackSource.EndAttack();
        }

        /// <summary>
        /// Called by AttackSource the moment its hitbox touches a valid target. Resolves the hit
        /// immediately (without waiting for the swing to finish), at most once per attack.
        /// </summary>
        internal void RegisterAttackHit(Entity target)
        {
            if (!isAttacking || hitResolved || !CanHitTarget(target)) return;

            // Another hit (e.g. both sides connecting in the same physics step) already started a fight.
            if (CombatTransition.IsTransitionInProgress) return;

            hitResolved = true;
            OnHitConnected(target);
        }

        /// <summary>
        /// Whether this Entity's attack is allowed to connect with <paramref name="target"/>.
        /// Subclasses restrict it to the opposing side so a swing can't be spent on a bystander.
        /// </summary>
        protected virtual bool CanHit(Entity target) => target != this;

        internal bool CanHitTarget(Entity target) => target != null && CanHit(target);

        /// <summary>
        /// Called the moment this Entity's hitbox touches a valid target (not when the swing ends).
        /// Whichever side calls this first is the side that gets the Speed advantage on combat
        /// entry (see combat.html "Entrada a combate"). Default just logs — PlayerEntity/EnemyEntity
        /// override to actually start the fight via CombatTransition.
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
        private void CompleteAttack()
        {
            attackSource?.EndAttack();
            isAttacking = false;

            // A connected hit was already resolved in RegisterAttackHit(); only a swing that
            // touched nothing is reported as a miss here.
            bool connected = hitResolved;
            hitResolved = false;
            if (!connected)
                OnAttackMissed();
        }

        /// <summary>
        /// Aborts any attack in flight without firing hit/miss callbacks: kills the tween, closes
        /// the hitbox, clears the lock and puts the sword back at rest. DOTween keeps running on
        /// deactivated objects, so this must happen whenever the Entity is disabled mid-swing.
        /// </summary>
        protected void CancelAttack()
        {
            attackTween?.Kill();
            attackTween = null;
            attackSource?.EndAttack();
            isAttacking = false;
            hitResolved = false;

            if (attackSetupValid && visualsOrigin != null)
                visualsOrigin.localRotation = restRotation;
        }

        protected virtual void OnDisable()
        {
            CancelAttack();
        }

        protected void EndAttack()
        {
            CompleteAttack();
        }
    }
}