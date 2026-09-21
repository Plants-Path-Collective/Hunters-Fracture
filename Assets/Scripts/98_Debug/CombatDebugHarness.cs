using System.Linq;
using UnityEngine;
using Core;
using Core.CombatSystem;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace ExtendedDebug
{
    /// <summary>
    /// Standalone test harness for CombatController — no ActionResolver yet, so EndTurn() is
    /// triggered by hand from CombatDebugHarnessEditor. Spawns a few throwaway Units, starts a
    /// combat, and logs every lifecycle event to the console. Delete once real actions exist.
    /// </summary>
    [RequireComponent(typeof(CombatController))]
    public class CombatDebugHarness : MonoBehaviour
    {
        [SerializeField] private UNIT_TEAM advantageTeam = UNIT_TEAM.Ally;

        public CombatController Combat { get; private set; }

        private void Awake()
        {
            Combat = GetComponent<CombatController>();
        }

        private void Start()
        {
            CombatUnit[] units =
            {
                CreateTestUnit("Ally_A", UNIT_TEAM.Ally, hp: 20, speed: 12),
                CreateTestUnit("Ally_B", UNIT_TEAM.Ally, hp: 15, speed: 7),
                CreateTestUnit("Enemy_A", UNIT_TEAM.Enemy, hp: 18, speed: 15),
                CreateTestUnit("Enemy_B", UNIT_TEAM.Enemy, hp: 10, speed: 4),
            };

            Combat.OnCombatStart += roster => Debug.Log("[Combat] Start · " + string.Join(", ", roster.Select(u => u.Name)));
            Combat.OnTurnStart   += unit   => Debug.Log($"[Combat] Turn start · {unit.Name}");
            Combat.OnTurnEnd     += unit   => Debug.Log($"[Combat] Turn end · {unit.Name}");
            Combat.OnUnitKilled  += unit   => Debug.Log($"[Combat] Killed · {unit.Name}");
            Combat.OnUnitRevived += unit   => Debug.Log($"[Combat] Revived · {unit.Name}");
            Combat.OnCombatEnd   += outcome => Debug.Log($"[Combat] End · {outcome}");

            Combat.StartCombat(units, advantageTeam);
        }

        private CombatUnit CreateTestUnit(string name, UNIT_TEAM team, int hp, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);

            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.DebugSetup(name, team, hp, speed);
            return unit;
        }

        // ── Convenience for the custom Editor ────────────────────────────────

        public CombatUnit FindUnit(string unitName) =>
            Combat.Roster.FirstOrDefault(u => u != null && u.Name == unitName);
    }
}