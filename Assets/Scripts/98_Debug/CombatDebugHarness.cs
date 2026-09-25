using System.Linq;
using UnityEngine;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Unit;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace ExtendedDebug
{
    /// <summary>
    /// Standalone test harness for CombatController — no ActionResolver yet, so EndTurn() (and
    /// the action verbs) are triggered by hand from CombatDebugHarnessEditor. Spawns a few
    /// throwaway Units, starts a combat, and logs every lifecycle event to the console. Delete
    /// once real actions exist.
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
                CreateTestUnit("Ally_A", UNIT_TEAM.Ally, hp: 20, sp: 12, speed: 12),
                CreateTestUnit("Ally_B", UNIT_TEAM.Ally, hp: 15, sp: 7, speed: 7),
                CreateTestUnit("Enemy_A", UNIT_TEAM.Enemy, hp: 18, sp: 15, speed: 15),
                CreateTestUnit("Enemy_B", UNIT_TEAM.Enemy, hp: 10, sp: 4, speed: 4),
            };

            Combat.OnCombatStart  += roster  => Debug.Log("[Combat] Start · " + string.Join(", ", roster.Select(u => u.Name)));
            Combat.OnTurnStart    += unit    => Debug.Log($"[Combat] Turn start · {unit.Name}");
            Combat.OnTurnEnd      += unit    => Debug.Log($"[Combat] Turn end · {unit.Name}");
            Combat.OnUnitKilled   += unit    => Debug.Log($"[Combat] Killed · {unit.Name}");
            Combat.OnUnitRevived  += unit    => Debug.Log($"[Combat] Revived · {unit.Name}");
            Combat.OnCombatEnd    += outcome => Debug.Log($"[Combat] End · {outcome}");
            Combat.OnAfterDamage  += ctx     => Debug.Log($"[Combat] Damage · {ctx.Target.Name} took {ctx.Amount} ({ctx.DamageType}) via {ctx.Via} — HP now {ctx.Target.HP}");
            Combat.OnAfterHeal    += ctx     => Debug.Log($"[Combat] Heal · {ctx.Target.Name} healed {ctx.Amount} via {ctx.Via} — HP now {ctx.Target.HP}");
            Combat.OnFleeAttempt  += ctx     => Debug.Log($"[Combat] Flee attempt · {ctx.Team} · {(ctx.Success ? "success" : "failed")}");

            Combat.StartCombat(units, advantageTeam);
        }

        private CombatUnit CreateTestUnit(string name, UNIT_TEAM team, int hp, int sp, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);

            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.InitializeFromDefinition(CreateTestUnitDefinition(name, team, hp, sp, speed));
            return unit;
        }

        private UnitDefinitionSO CreateTestUnitDefinition(string name, UNIT_TEAM team, int hp, int sp, float speed)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinitionSO>();
            definition.unitName = name;
            definition.type = UNITY_TYPE.Magical; 
            definition.team = team;
            definition.maxHP = hp;
            definition.maxSP = sp;
            definition.speed = speed;
            return definition;
        }

        // ── Convenience for the custom Editor ────────────────────────────────

        public CombatUnit FindUnit(string unitName) =>
            Combat.Roster.FirstOrDefault(u => u != null && u.Name == unitName);
    }
}