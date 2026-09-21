using System.Linq;
using UnityEngine;
using Core;
using Core.CombatSystem;
using CombatUnit = Core.CombatSystem.Unit.Unit;
 
namespace ExtendedDebug
{
    /// <summary>
    /// Standalone test harness — no CombatController, no CombatSetUp, no ActionResolver.
    /// Spawns a handful of throwaway Units, feeds them to a TimelineController, and logs every
    /// OnTimelineChanged to the console so order/advantage/operations can be sanity-checked
    /// before anything else in combat exists. Delete once CombatController drives the timeline
    /// for real.
    /// </summary>
    public class TimelineDebugHarness : MonoBehaviour
    {
        [SerializeField] private TimelineController timeline;
        [SerializeField] private UNIT_TEAM advantageTeam = UNIT_TEAM.Ally;

        private void Awake()
        {
            if (timeline == null)
                timeline = GetComponent<TimelineController>();
        }

        private void Start()
        {
            if (timeline == null)
            {
                Debug.LogError($"[{nameof(TimelineDebugHarness)}] No TimelineController assigned.");
                return;
            }

            CombatUnit[] units =
            {
                CreateTestUnit("Ally_A", UNIT_TEAM.Ally, speed: 12),
                CreateTestUnit("Ally_B", UNIT_TEAM.Ally, speed: 7),
                CreateTestUnit("Enemy_A", UNIT_TEAM.Enemy, speed: 15),
                CreateTestUnit("Enemy_B", UNIT_TEAM.Enemy, speed: 4),
            };

            timeline.OnTimelineChanged += (unit, operation, delta) =>
                Debug.Log($"[Timeline] {operation} · {(unit != null ? unit.Name : "—")}" +
                          (delta != 0 ? $" (delta {delta})" : ""));

            timeline.Initialize(units, advantageTeam);
            LogQueue();
        }

        private CombatUnit CreateTestUnit(string name, UNIT_TEAM team, float speed)
        {
            GameObject unitObject = new GameObject(name);
            unitObject.transform.SetParent(transform);

            CombatUnit unit = unitObject.AddComponent<CombatUnit>();
            unit.DebugSetup(name, team, hp: 10, speed: speed);
            return unit;
        }

        private void LogQueue()
        {
            Debug.Log("[Timeline] Initial order: " + string.Join(" → ", timeline.Queue.Select(u => u.Name)));
        }
    }
}
