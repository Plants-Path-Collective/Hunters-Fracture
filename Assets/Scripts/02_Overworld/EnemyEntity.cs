using UnityEngine;
using UnityEngine.AI;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Overworld
{
    /// <summary>
    /// Overworld-only component. Patrols within its room's collider bounds when assigned by a
    /// room, or within a radius of its spawn position as a fallback. "Seeing" the Player is a
    /// stub for now — Overworld enemy perception isn't
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
        private Bounds? patrolBounds;
        private ENEMY_STATE state = ENEMY_STATE.Patrol;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            // Center of the patrol area — wherever this enemy was placed in the scene unless
            // a room assigned a room-bound area through ConfigurePatrolArea().
            patrolOrigin = transform.position;
            PickNewPatrolPoint();
        }

        /// <summary>Restricts patrol points to the bounds of the room that owns this enemy.</summary>
        public void ConfigurePatrolArea(Bounds bounds)
        {
            patrolBounds = bounds;
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
            Vector3 candidate;
            if (patrolBounds.HasValue)
            {
                Bounds bounds = patrolBounds.Value;
                candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    patrolOrigin.y,
                    Random.Range(bounds.min.z, bounds.max.z));
            }
            else
            {
                Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
                candidate = patrolOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleMaxDistance, NavMesh.AllAreas)
                && (!patrolBounds.HasValue || IsInsidePatrolBounds(hit.position)))
            {
                agent.SetDestination(hit.position);
            }
            // If sampling misses (e.g. candidate landed off any NavMesh island), just keep
            // whatever destination is already set — TickPatrol() retries next time it's reached.
        }

        private bool IsInsidePatrolBounds(Vector3 position)
        {
            Bounds bounds = patrolBounds.Value;
            return position.x >= bounds.min.x && position.x <= bounds.max.x
                && position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        /// <summary>
        /// Placeholder — Overworld enemy vision/perception isn't designed yet. Always false, so
        /// patrol runs unconditionally until this is implemented.
        /// </summary>
        private bool CanSeePlayer() => false;

        protected override void PlayAttackPlaceholder()
        {
            PlayAttackAnimation();
        }

        protected override void OnHitConnected(Entity target)
        {
            if (target is not PlayerEntity player) return;

            base.OnHitConnected(target);
            CombatTransition.Begin(player.Party, enemyParty, UNIT_TEAM.Enemy, player.gameObject, gameObject);
        }
    }
}