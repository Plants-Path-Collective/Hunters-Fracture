using UnityEngine;
using System.Collections.Generic;
using Core;

namespace Core.CombatSystem.Units
{
    [RequireComponent(typeof(UnitInventory), typeof(UnitStatsController), typeof(UnitEffectController))]
    public class Unit : MonoBehaviour
    {
        [SerializeField] private UnitDefinitionSO definition;
        public UnitDefinitionSO Definition => definition;

        public CombatController Combat { get; private set; }

        // ----- Identity -----
        public string Name { get; private set; }
        public UNITY_TYPE UnitType { get; private set; }
        public UNIT_TEAM Team { get; private set; }
        public Sprite Portrait { get; private set; }
        public string Description { get; private set; }

        // ----- Stats -----
        public bool IsAlive => HP > 0;

        // Current HP/SP are the only stats Unit actually owns — they're persisted state, not
        // derived. Only CombatController should write these (Kill/Revive/DealDamage/Heal/
        // SpendSP/RestoreSP).
        public int HP { get; internal set; }
        public int SP { get; internal set; }

        /// <summary>True while a Defender's guard is still up. Lasts for exactly the next turn taken
        /// by anyone else, then clears right as the turn after that begins (so with only 2 combatants
        /// this coincides with "my own next turn," matching combat.html's wording) — see
        /// CombatController.Defend() / TickDefending(). Reduces incoming damage via
        /// UnitDefinitionSO.defenseReduction (see ActionResolver.ResolveAttack).</summary>
        public bool isDefending => defendingTurnsRemaining > 0;

        /// <summary>Starts the guard. Only CombatController.Defend() should call this.</summary>
        internal void SetDefending(int turns) => defendingTurnsRemaining = turns;

        /// <summary>Counts one turn advance toward the guard's expiry. Called once per
        /// AdvanceToNextTurn, for every Unit in the roster — see CombatController.</summary>
        internal void TickDefending()
        {
            if (defendingTurnsRemaining > 0)
                defendingTurnsRemaining--;
        }

        // The 7 derived stats are owned by UnitStatsController, not copied here — this is a
        // pure pass-through, so RecalculateStats() (item pickups, buffs, etc. down the line)
        // is reflected everywhere automatically instead of going stale on a value copied once
        // at initialization.
        public int MaxHP => StatsController.MaxHP;
        public int MaxSP => StatsController.MaxSP;
        public float Speed => StatsController.Speed;
        public float Strength => StatsController.Strength;
        public float MagicPower => StatsController.MagicPower;
        public float PhysicalDefense => StatsController.PhysicalDefense;
        public float MagicalDefense => StatsController.MagicalDefense;

        // ----- References -----
        public UnitInventory Inventory { get; private set; }
        public UnitEffectController EffectController { get; private set; }
        public UnitStatsController StatsController { get; private set; }

        // ----- Internal state -----
        /// <summary>
        /// turns that this unit can still defend for. 
        /// Decremented at the start of any unit's turn, 
        /// isDefending turns to false when it reaches 0.
        /// </summary>
        private int defendingTurnsRemaining;

        private void Awake()
        {
            Inventory = GetComponent<UnitInventory>();
            EffectController = GetComponent<UnitEffectController>();
            StatsController = GetComponent<UnitStatsController>();

            // Deliberately NOT calling InitializeFromDefinition here: AddComponent<Unit>()
            // fires Awake() synchronously, before whoever creates this Unit dynamically (a test
            // harness, CombatController) has a chance to assign a definition. Initialization is
            // always an explicit call from the creator, matching combat.html's
            // "Instantiate → Unit.InitializeFresh(...)" flow — never automatic.
        }

        /// <summary>
        /// team is an explicit parameter and always wins over definition.team — the definition's
        /// own team field is just a reference/default for a future character-sheet inspector,
        /// not something that should lock a UnitDefinitionSO to one side forever.
        /// </summary>
        public void InitializeFromDefinition(UnitDefinitionSO definition, UNIT_TEAM team)
        {
            SetIdentity(definition, team);

            StatsController.SetBase(definition);
            StatsController.RecalculateStats(Inventory);

            HP = MaxHP;
            SP = MaxSP;
        }

        /// <summary>
        /// Called by CombatController.StartCombat() once this Unit is part of the roster —
        /// separate from InitializeFromDefinition() because identity/stats are set once, but
        /// which CombatController a Unit is fighting under is fresh each encounter. Triggers
        /// UnitEffectController's first reconcile pass, now that TriggeredEffectSO.Attach() can
        /// reach unit.Combat to subscribe to its events.
        /// </summary>
        public void EnterCombat(CombatController combat)
        {
            Combat = combat;
            EffectController.Initialize(this);
        }

        /// <summary>Releases the combat link: detaches all triggered effects and drops the controller reference.</summary>
        public void ExitCombat()
        {
            if (EffectController != null)
                EffectController.DetachAll();

            Combat = null;
        }

        private void SetIdentity(UnitDefinitionSO definition, UNIT_TEAM team)
        {
            this.definition = definition;

            Name = definition.unitName;
            UnitType = definition.type;
            Team = team;
            Portrait = definition.portrait;
            Description = definition.description;
        }

        /// <summary>
        /// Builds a combat Unit from persistent party data. Inventory goes in first so the stat
        /// recompute inside InitializeFromDefinition already includes item modifiers. HP/SP are
        /// clamped to the recalculated max; -1 means "full".
        /// </summary>
        public void InitializeFromParty(PartyMemberData member)
        {
            if (member == null || member.definition == null)
            {
                Debug.LogError($"[Unit] {gameObject.name}: PartyMemberData or its definition is null.");
                return;
            }

            StatsController.SetBase(member.definition);
            Inventory.Populate(member.inventorySnapshot);
            SetIdentity(member.definition, UNIT_TEAM.Ally);

            HP = member.currentHP < 0 ? MaxHP : Mathf.Min(member.currentHP, MaxHP);
            SP = member.currentSP < 0 ? MaxSP : Mathf.Min(member.currentSP, MaxSP);
        }



        /// <summary>
        /// Builds a Unit at full HP/SP, with an optional starting inventory (elite enemies).
        /// </summary>
        public void InitializeFresh(UnitDefinitionSO definition, IReadOnlyDictionary<int, int> snapshot = null, 
        UNIT_TEAM team = UNIT_TEAM.Enemy)
        {
            if (definition == null)
            {
                Debug.LogError($"[Unit] {gameObject.name}: definition is null.");
                return;
            }

            StatsController.SetBase(definition);
            Inventory.Populate(snapshot);
            SetIdentity(definition, team);

            HP = MaxHP;
            SP = MaxSP;
        }
    }
}