using UnityEngine;
using UnityEngine.AI;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Overworld
{
    /// <summary>
    /// Overworld-only enemy behaviour. Patrols within a room or around its spawn, detects the
    /// Player inside a configurable vision cone, pursues and attacks, and resumes patrol after
    /// a missed attack.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyEntity : Entity
    {
        [Header("----- Combat -----")]
        [SerializeField] private EnemyParty enemyParty = new();
        [SerializeField, Min(0f)] private float detectionDelay = 1f;
        [SerializeField, Min(0f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float postMissStandTime = 2f;
        [Tooltip("Seconds the enemy stands still after being restored from a combat (e.g. Fled) before it reacts again.")]
        [SerializeField, Min(0f)] private float postCombatGraceTime = 2f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        public EnemyParty EnemyParty => enemyParty;

        [Header("----- Vision -----")]
        [SerializeField] private Transform visionOrigin;
        [SerializeField, Min(0f)] private float visionRange = 12f;
        [SerializeField, Range(0f, 360f)] private float visionAngle = 100f;
        [SerializeField] private LayerMask visionObstacleMask = ~0;

        [Header("----- Patrol -----")]
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private float patrolPointReachedThreshold = 0.3f;
        [Tooltip("Max distance to snap a random point onto the NavMesh. Increase if patrol picks fail often in sparse/irregular areas.")]
        [SerializeField] private float navMeshSampleMaxDistance = 2f;

        private NavMeshAgent agent;
        private PlayerEntity player;
        private Vector3 patrolOrigin;
        private Bounds? patrolBounds;
        private ENEMY_STATE state = ENEMY_STATE.Patrol;
        private float stateTimer;

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
            player = FindFirstObjectByType<PlayerEntity>();
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
                case ENEMY_STATE.Detecting:
                    TickDetecting();
                    break;
                case ENEMY_STATE.Chase:
                    TickChase();
                    break;
                case ENEMY_STATE.Recover:
                    TickRecover();
                    break;
                case ENEMY_STATE.CombatStarting:
                    TickPostCombat();
                    break;
            }
        }

        /// <summary>
        /// Update only runs again in CombatStarting if the enemy was re-enabled after the fight
        /// (Fled / non-destroying Victory). Gives the player a grace period instead of leaving
        /// the enemy frozen in a state nothing else exits.
        /// </summary>
        private void TickPostCombat()
        {
            agent.ResetPath();
            agent.isStopped = true;
            state = ENEMY_STATE.Recover;
            stateTimer = postCombatGraceTime;
        }

        private void TickPatrol()
        {
            if (CanSeePlayer())
            {
                BeginDetecting();
                return;
            }

            if (!agent.isStopped && !agent.pathPending && agent.remainingDistance <= patrolPointReachedThreshold)
                PickNewPatrolPoint();
        }

        private void BeginDetecting()
        {
            agent.isStopped = true;
            state = ENEMY_STATE.Detecting;
            stateTimer = detectionDelay;
        }

        private void TickDetecting()
        {
            if (!IsValidPlayer())
            {
                ResumePatrol();
                return;
            }

            TurnTowards(player.transform.position);
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                agent.isStopped = false;
                state = ENEMY_STATE.Chase;
            }
        }

        private void TickChase()
        {
            if (!IsValidPlayer())
            {
                ResumePatrol();
                return;
            }

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= attackRange)
            {
                agent.isStopped = true;
                TurnTowards(player.transform.position);
                TryAttack();
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
        }

        private void TickRecover()
        {
            if (IsValidPlayer())
                TurnTowards(player.transform.position);

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            if (CanSeePlayer())
                BeginDetecting();
            else
                ResumePatrol();
        }

        private void ResumePatrol()
        {
            agent.isStopped = false;
            state = ENEMY_STATE.Patrol;
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
        }

        private bool IsInsidePatrolBounds(Vector3 position)
        {
            Bounds bounds = patrolBounds.Value;
            return position.x >= bounds.min.x && position.x <= bounds.max.x
                && position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        private bool CanSeePlayer()
        {
            if (!IsValidPlayer())
                return false;

            Vector3 origin = VisionOrigin;
            Vector3 toPlayer = player.transform.position - origin;
            toPlayer.y = 0f;
            float range = toPlayer.magnitude;
            if (range <= Mathf.Epsilon)
                return true;
            if (range > visionRange)
                return false;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                return false;

            forward.Normalize();
            if (Vector3.Angle(forward, toPlayer) > visionAngle * 0.5f)
                return false;

            RaycastHit[] hits = Physics.RaycastAll(origin, toPlayer.normalized, range,
                visionObstacleMask, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.PositiveInfinity;

            foreach (RaycastHit hit in hits)
            {
                if (hit.distance >= nearestDistance)
                    continue;
                if (hit.collider.GetComponentInParent<EnemyEntity>() == this)
                    continue;

                nearest = hit;
                nearestDistance = hit.distance;
            }

            return nearest.collider != null
                && nearest.collider.GetComponentInParent<PlayerEntity>() == player;
        }

        private bool IsValidPlayer()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerEntity>();

            return player != null && player.isActiveAndEnabled;
        }

        private Vector3 VisionOrigin => visionOrigin != null ? visionOrigin.position : transform.position;

        private void TurnTowards(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                turnSpeed * Time.deltaTime);
        }

        protected override bool CanHit(Entity target) => target is PlayerEntity;

        protected override void PlayAttackPlaceholder()
        {
            PlayAttackAnimation();
        }

        protected override void OnAttackMissed()
        {
            if (state == ENEMY_STATE.CombatStarting)
                return;

            agent.isStopped = true;
            state = ENEMY_STATE.Recover;
            stateTimer = postMissStandTime;
        }

        protected override void OnHitConnected(Entity target)
        {
            if (target is not PlayerEntity playerEntity)
                return;

            base.OnHitConnected(target);
            state = ENEMY_STATE.CombatStarting;
            agent.isStopped = true;
            CombatTransition.Begin(playerEntity.Party, enemyParty, UNIT_TEAM.Enemy,
                playerEntity.gameObject, gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = VisionOrigin;
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();
            float halfAngle = visionAngle * 0.5f;
            Vector3 leftDirection = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawLine(origin, origin + leftDirection * visionRange);
            Gizmos.DrawLine(origin, origin + rightDirection * visionRange);

            const int arcSegments = 24;
            Vector3 previous = origin + leftDirection * visionRange;
            for (int i = 1; i <= arcSegments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)arcSegments);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 next = origin + direction * visionRange;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.color = player != null && CanSeePlayer()
                ? new Color(1f, 0.2f, 0.2f, 1f)
                : new Color(0.2f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(origin, 0.12f);

            if (player != null)
                Gizmos.DrawLine(origin, player.transform.position);
        }
    }
}