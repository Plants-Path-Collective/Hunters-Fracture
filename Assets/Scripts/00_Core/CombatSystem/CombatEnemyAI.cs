using System.Collections;
using System.Linq;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem
{
    /// <summary>
    /// Minimal enemy brain: on each enemy turn it waits a short moment, then performs a basic
    /// attack against a random living ally. Placeholder for the weighted action table described
    /// in combat.html ("IA enemiga"); it only exists so a full combat can be played end to end.
    /// </summary>
    [RequireComponent(typeof(CombatController), typeof(ActionResolver))]
    public class CombatEnemyAI : MonoBehaviour
    {
        [Tooltip("Seconds to wait before acting. Also gives the previous action's hop time to finish.")]
        [SerializeField, Min(0f)] private float actionDelay = 0.8f;

        private CombatController combat;
        private ActionResolver resolver;

        private void Awake()
        {
            combat = GetComponent<CombatController>();
            resolver = GetComponent<ActionResolver>();
        }

        private void OnEnable()
        {
            combat.OnTurnStart += OnTurnStart;
        }

        private void OnDisable()
        {
            if (combat != null)
                combat.OnTurnStart -= OnTurnStart;

            StopAllCoroutines();
        }

        private void OnTurnStart(Unit actor)
        {
            if (actor.Team != UNIT_TEAM.Enemy) return;
            StartCoroutine(TakeTurn(actor));
        }

        private IEnumerator TakeTurn(Unit actor)
        {
            yield return new WaitForSeconds(actionDelay);

            // The combat may have ended, or the turn moved on, while we were waiting.
            if (!combat.IsCombatActive || combat.CurrentActor != actor || !actor.IsAlive)
                yield break;

            var targets = combat.Roster
                .Where(u => u.Team != actor.Team && u.IsAlive)
                .ToList();

            if (targets.Count > 0)
            {
                Unit target = targets[Random.Range(0, targets.Count)];
                if (resolver.ResolveAttack(actor, target))
                    yield break; // ResolveAttack already ended the turn
            }

            // No valid action: pass the turn instead of freezing the whole combat.
            combat.EndTurn();
        }
    }
}