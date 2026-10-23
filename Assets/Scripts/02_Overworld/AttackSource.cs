using UnityEngine;

namespace Overworld
{
    public class AttackSource : MonoBehaviour
    {
        private Collider hitbox;
        private Entity owner;
        private bool hitRegistered;

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
        }

        internal bool EndAttack()
        {
            if (hitbox != null)
                hitbox.enabled = false;

            return hitRegistered;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hitRegistered || owner == null) return;

            Entity target = other.GetComponentInParent<Entity>();
            if (target == null || target == owner) return;

            hitRegistered = true;
            owner.RegisterAttackHit(target);
        }
    }
}
