using System.Linq;
using UnityEngine;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.StatusSystem;
using Core.CombatSystem.Units;

namespace ExtendedDebug
{
    /// <summary>Test-only: applies the chosen status from the component's context menu in Play mode
    /// and logs every status event. Delete once skills apply statuses for real.</summary>
    public class StatusDebugHarness : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [SerializeField] private StatusSO status;
        [Tooltip("Meaning depends on the status: fraction (0.5 = 50%), damage per turn, SP per turn.")]
        [SerializeField] private float magnitude = 0.5f;
        [SerializeField, Min(1)] private int duration = 3;

        private void OnEnable()
        {
            combat.OnStatusApplied += LogApplied;
            combat.OnStatusExpired += LogExpired;
            combat.OnTurnSkipped += LogSkipped;
        }

        private void OnDisable()
        {
            if (combat == null) return;
            combat.OnStatusApplied -= LogApplied;
            combat.OnStatusExpired -= LogExpired;
            combat.OnTurnSkipped -= LogSkipped;
        }

        [ContextMenu("Apply to first living enemy")]
        private void ApplyToEnemy() => Apply(First(UNIT_TEAM.Enemy));

        [ContextMenu("Apply to first living ally")]
        private void ApplyToAlly() => Apply(First(UNIT_TEAM.Ally));

        [ContextMenu("Apply to current actor")]
        private void ApplyToActor() => Apply(combat.CurrentActor);

        private Unit First(UNIT_TEAM team) =>
            combat.Roster.FirstOrDefault(u => u.Team == team && u.IsAlive);

        private void Apply(Unit target)
        {
            if (target == null || status == null)
            {
                Debug.LogWarning("[StatusDebug] No target or status assigned.");
                return;
            }

            combat.ApplyStatus(combat.CurrentActor, target, status, magnitude, duration);
        }

        private void LogApplied(StatusContext c) =>
            Debug.Log($"[Status] {c.status.statusName} {(c.refreshed ? "refreshed" : "applied")} on " +
                      $"{c.target.Name} · magnitude {c.magnitude} · {c.remainingTurns} turn(s) left · " +
                      $"PDef {c.target.PhysicalDefense:0.###} · MDef {c.target.MagicalDefense:0.###}");

        private void LogExpired(StatusContext c) =>
            Debug.Log($"[Status] {c.status.statusName} expired on {c.target.Name} · " +
                      $"PDef {c.target.PhysicalDefense:0.###} · MDef {c.target.MagicalDefense:0.###}");

        private void LogSkipped(Unit unit) => Debug.Log($"[Status] {unit.Name} loses its turn.");
    }
}