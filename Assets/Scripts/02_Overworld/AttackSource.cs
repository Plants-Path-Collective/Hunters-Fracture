using UnityEngine;

namespace Overworld
{
    /// <summary>
    /// Hitbox of an Entity's weapon. While open it is tested every frame against the colliders it
    /// overlaps (a sweep, so contact with the blade's tip or edge counts at once); the physics
    /// trigger callback stays as a backup for non-box colliders.
    /// </summary>
    public class AttackSource : MonoBehaviour
    {
        private const int MaxOverlaps = 16;
        private static readonly Collider[] overlapBuffer = new Collider[MaxOverlaps];

        private Collider hitbox;
        private Entity owner;
        private bool hitRegistered;
        private bool active;

        internal bool Initialize(Entity attackOwner)
        {
            hitbox = GetComponent<Collider>();
            if (hitbox == null)
            {
                Debug.LogError($"[{nameof(AttackSource)}] {name} needs a Collider for its hitbox.", this);
                return false;
            }

            owner = attackOwner;
            hitbox.isTrigger = true;
            hitbox.enabled = false;

            if (GetComponentInParent<Rigidbody>() == null)
            {
                Rigidbody body = gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
            }

            return true;
        }

        internal void BeginAttack()
        {
            hitRegistered = false;
            hitbox.enabled = true;
            active = true;

            // Test right away instead of waiting for the next physics step.
            SweepForTargets();
        }

        internal bool EndAttack()
        {
            active = false;
            if (hitbox != null)
                hitbox.enabled = false;

            return hitRegistered;
        }

        // LateUpdate so the tween that moves the sword (Update) has already applied this frame's pose.
        private void LateUpdate()
        {
            if (active && !hitRegistered)
                SweepForTargets();
        }

        /// <summary>Overlap test of the hitbox's current pose against every collider it touches.</summary>
        private void SweepForTargets()
        {
            if (hitRegistered || owner == null) return;

            // Other collider shapes fall back to OnTriggerEnter.
            if (hitbox is not BoxCollider box) return;

            Transform boxTransform = box.transform;
            Vector3 scale = boxTransform.lossyScale;
            Vector3 halfExtents = Vector3.Scale(box.size,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;

            int count = Physics.OverlapBoxNonAlloc(boxTransform.TransformPoint(box.center), halfExtents,
                overlapBuffer, boxTransform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (TryRegisterHit(overlapBuffer[i]))
                    return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hitRegistered || owner == null) return;

            TryRegisterHit(other);
        }

        /// <summary>
        /// Registers the hit if <paramref name="other"/> belongs to a valid target. Filtered before
        /// consuming hitRegistered, so touching a bystander (another enemy, the owner itself) never
        /// uses up the one hit this swing gets.
        /// </summary>
        private bool TryRegisterHit(Collider other)
        {
            Entity target = other.GetComponentInParent<Entity>();
            if (target == null || !owner.CanHitTarget(target)) return false;

            hitRegistered = true;
            owner.RegisterAttackHit(target);
            return true;
        }

        private void OnDrawGizmos()
        {
            if (hitbox == null || !hitbox.enabled) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(hitbox.bounds.center, hitbox.bounds.size);
        }
    }
}