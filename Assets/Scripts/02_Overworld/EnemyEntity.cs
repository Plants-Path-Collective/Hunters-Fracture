using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

namespace Overworld
{
    /// <summary>
    /// Overworld-only component. Patrols by walking to random points within a radius of its
    /// spawn position. "Seeing" the Player is a stub for now — Overworld enemy perception isn't
    /// designed yet, so patrol runs unconditionally until that's built.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyEntity : Entity
    {
        private enum EnemyState
        {
            Patrol,
            Chase // not implemented yet — reserved for when perception exists
        }

        [Header("----- Patrol -----")]
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private float patrolPointReachedThreshold = 0.3f;
        [Tooltip("Max distance to snap a random point onto the NavMesh. Increase if patrol picks fail often in sparse/irregular areas.")]
        [SerializeField] private float navMeshSampleMaxDistance = 2f;

        private NavMeshAgent agent;
        private Vector3 patrolOrigin;
        private EnemyState state = EnemyState.Patrol;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            // Center of the patrol area — wherever this enemy was placed in the scene.
            patrolOrigin = transform.position;
            PickNewPatrolPoint();
        }

        private void Update()
        {
            switch (state)
            {
                case EnemyState.Patrol:
                    TickPatrol();
                    break;
                case EnemyState.Chase:
                    // TODO: reacting to spotting the Player — not designed yet.
                    break;
            }
        }

        private void TickPatrol()
        {
            if (CanSeePlayer())
            {
                state = EnemyState.Chase;
                return;
            }

            bool reachedDestination = !agent.pathPending && agent.remainingDistance <= patrolPointReachedThreshold;
            if (reachedDestination)
                PickNewPatrolPoint();
        }

        private void PickNewPatrolPoint()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = patrolOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleMaxDistance, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            // If sampling misses (e.g. candidate landed off any NavMesh island), just keep
            // whatever destination is already set — TickPatrol() retries next time it's reached.
        }

        /// <summary>
        /// Placeholder — Overworld enemy vision/perception isn't designed yet. Always false, so
        /// patrol runs unconditionally until this is implemented.
        /// </summary>
        private bool CanSeePlayer() => false;

        protected override void PlayAttackPlaceholder()
        {
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0.5f)
                .OnComplete(() =>
                {
                    OnAttackHitFrame();
                    EndAttack();
                });
        }

        protected override void OnHitConnected(Entity target)
        {
            if (target is not PlayerEntity)
                return;

            base.OnHitConnected(target);
            // TODO: once CombatSetUp exists — CombatSetUp.Begin(playerAdvantage: false, this);
        }
    }
}