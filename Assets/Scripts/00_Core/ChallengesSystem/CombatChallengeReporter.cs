using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.Units;
// If DamageContext is not found, add the using of the namespace where it is declared.

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Translates CombatController events into challenge stats.
    /// Lives in CombatStage, so it dies with the combat and never leaves stale subscriptions.
    /// </summary>
    public class CombatChallengeReporter : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Controller of this CombatStage whose events are translated.")]
        [SerializeField] private CombatController combatController;

        // Kill() can raise OnUnitKilled twice for the same unit (see its summary), so count each unit once.
        private readonly HashSet<Unit> countedKills = new();

        private void OnEnable()
        {
            if (combatController == null)
            {
                Debug.LogWarning($"[{nameof(CombatChallengeReporter)}] No CombatController assigned.", this);
                return;
            }

            combatController.OnCombatStart += HandleCombatStart;
            combatController.OnUnitKilled += HandleUnitKilled;
            combatController.OnAfterDamage += HandleAfterDamage;
            combatController.OnCombatEnd += HandleCombatEnd;
        }

        private void OnDisable()
        {
            if (combatController == null) return;

            combatController.OnCombatStart -= HandleCombatStart;
            combatController.OnUnitKilled -= HandleUnitKilled;
            combatController.OnAfterDamage -= HandleAfterDamage;
            combatController.OnCombatEnd -= HandleCombatEnd;
        }

        private void HandleCombatStart(IReadOnlyList<Unit> roster) => countedKills.Clear();

        private void HandleUnitKilled(Unit unit)
        {
            if (unit == null || unit.Team != UNIT_TEAM.Enemy) return;
            if (!countedKills.Add(unit)) return;

            ChallengeEvents.ReportStat(CHALLENGE_STAT.EnemiesKilled);
        }

        private void HandleAfterDamage(DamageContext context)
        {
            // Note: amount is the requested damage, so overkill is included.
            if (context.source != null && context.source.Team == UNIT_TEAM.Ally)
                ChallengeEvents.ReportStat(CHALLENGE_STAT.DamageDealt, context.amount);
        }

        private void HandleCombatEnd(COMBAT_OUTCOME outcome)
        {
            switch (outcome)
            {
                case COMBAT_OUTCOME.Victory:
                    ChallengeEvents.ReportStat(CHALLENGE_STAT.CombatsWon);
                    break;
                case COMBAT_OUTCOME.Fled:
                    ChallengeEvents.ReportStat(CHALLENGE_STAT.CombatsFled);
                    break;
            }

            // Persist once per combat instead of on every increment.
            ChallengeManager.Instance?.SaveIfDirty();
        }
    }
}