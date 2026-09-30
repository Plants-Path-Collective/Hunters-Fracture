using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Units;

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
        [Header("----- Combat -----")]
        [SerializeField] private EnemyParty enemyParty = new();
        public EnemyParty EnemyParty => enemyParty;

        [Header("----- Patrol -----")]
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private float patrolPointReachedThreshold = 0.3f;
        [Tooltip("Max distance to snap a random point onto the NavMesh. Increase if patrol picks fail often in sparse/irregular areas.")]
        [SerializeField] private float navMeshSampleMaxDistance = 2f;

        private NavMeshAgent agent;
        private Vector3 patrolOrigin;
        private ENEMY_STATE state = ENEMY_STATE.Patrol;

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
                case ENEMY_STATE.Patrol:
                    TickPatrol();
                    break;
                case ENEMY_STATE.Chase:
                    // TODO: reacting to spotting the Player — not designed yet.
                    break;
            }
        }

        private void TickPatrol()
        {
            if (CanSeePlayer())
            {
                state = ENEMY_STATE.Chase;
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
            if (target is not PlayerEntity player) return;

            base.OnHitConnected(target);
            CombatTransition.Begin(player.Party, enemyParty, UNIT_TEAM.Enemy, player.gameObject, gameObject);
        }
    }
}