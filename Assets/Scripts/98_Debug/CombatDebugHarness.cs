using System.Linq;
using UnityEngine;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Units;
using CombatUnit = Core.CombatSystem.Units.Unit;

namespace ExtendedDebug
{
    /// <summary>
    /// Standalone test harness for CombatController. With useSetUp on, it builds a CombatSetUp
    /// from the Inspector (same path the Overworld will use) and CombatController starts from
    /// it. With it off, it spawns throwaway Units like before. EndTurn() and the action verbs
    /// are driven by hand from CombatDebugHarnessEditor.
    /// </summary>
    [RequireComponent(typeof(CombatController))]
    public class CombatDebugHarness : MonoBehaviour
    {
        [SerializeField] private UNIT_TEAM advantageTeam = UNIT_TEAM.Ally;

        [Header("--- Real setup path (CombatSetUp) ---")]
        [SerializeField] private bool useSetUp = true;
        [SerializeField] private AllyParty allyParty = new();
        [SerializeField] private EnemyParty enemyParty = new();

        public CombatController Combat { get; private set; }

        private void Awake()
        {
            Combat = GetComponent<CombatController>();
            SubscribeLogs();

            // Prepared in Awake so it is pending before CombatController.Start() consumes it,
            // whatever the Start() order between components.
            if (useSetUp)
                CombatSetUp.Prepare(allyParty, enemyParty, advantageTeam, OnFinished);
        }

        private void Start()
        {
            if (useSetUp) return; // CombatController.Start() already started the combat

            CombatUnit[] units =
            {
                CreateTestUnit("Ally_A", UNIT_TEAM.Ally, hp: 20, sp: 12, speed: 12),
                CreateTestUnit("Ally_B", UNIT_TEAM.Ally, hp: 15, sp: 7, speed: 7),
                CreateTestUnit("Enemy_A", UNIT_TEAM.Enemy, hp: 18, sp: 15, speed: 15),
                CreateTestUnit("Enemy_B", UNIT_TEAM.Enemy, hp: 10, sp: 4, speed: 4),
            };

            Combat.StartCombat(units, advantageTeam);
        }

        private void SubscribeLogs()
        {
            Combat.OnCombatStart  += roster  => Debug.Log("[Combat] Start · " + string.Join(", ", roster.Select(u => u.Name)));
            Combat.OnTurnStart    += unit    => Debug.Log($"[Combat] Turn start · {unit.Name}");
            Combat.OnTurnEnd      += unit    => Debug.Log($"[Combat] Turn end · {unit.Name}");
            Combat.OnUnitKilled   += unit    => Debug.Log($"[Combat] Killed · {unit.Name}");
            Combat.OnUnitRevived  += unit    => Debug.Log($"[Combat] Revived · {unit.Name}");
            Combat.OnCombatEnd    += outcome => Debug.Log($"[Combat] End · {outcome}");
            Combat.OnAfterDamage  += ctx     => Debug.Log($"[Combat] Damage · {ctx.target.Name} took {ctx.amount} ({ctx.damageType}) via {ctx.via} — HP now {ctx.target.HP}");
            Combat.OnAfterHeal    += ctx     => Debug.Log($"[Combat] Heal · {ctx.target.Name} healed {ctx.amount} via {ctx.via} — HP now {ctx.target.HP}");
            Combat.OnFleeAttempt  += ctx     => Debug.Log($"[Combat] Flee attempt · {ctx.team} · {(ctx.success ? "success" : "failed")}");
        }

        private CombatUnit CreateTestUnit(string name, UNIT_TEAM team, int hp, int sp, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);

            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.InitializeFromDefinition(CreateTestUnitDefinition(name, team, hp, sp, speed), team);
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

        private void OnFinished(COMBAT_OUTCOME outcome) =>
            Debug.Log($"[Combat] Finished callback · {outcome}");

        /// <summary>
        /// Starts a new encounter with the same (already updated) party, to test that state
        /// carries over between chained combats. Only when no combat is running.
        /// </summary>
        public void StartNewCombat()
        {
            if (Combat.IsCombatActive) return;

            CombatSetUp.Prepare(allyParty, enemyParty, advantageTeam, OnFinished);
            Combat.StartFromSetUp(CombatSetUp.Consume());
        }    
    }
}