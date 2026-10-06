using System;
using UnityEngine;
using System.Collections.Generic;

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

        // ----- Resource events -----

        /// <summary>Fired whenever HP or SP change, and whenever stats are recalculated (MaxHP/MaxSP may
        /// have changed). Subscribers read HP/MaxHP/SP/MaxSP from the Unit; bars bind to this per Unit.</summary>
        public event Action<Unit> OnResourcesChanged;

        private int hp;
        private int sp;

        // Current HP/SP are the only stats Unit actually owns — they're persisted state, not
        // derived. Only CombatController should write these (Kill/Revive/DealDamage/Heal/
        // SpendSP/RestoreSP).
        public int HP
        {
            get => hp;
            internal set
            {
                if (hp == value) return;
                hp = value;
                OnResourcesChanged?.Invoke(this);
            }
        }

        public int SP
        {
            get => sp;
            internal set
            {
                if (sp == value) return;
                sp = value;
                OnResourcesChanged?.Invoke(this);
            }
        }

        /// <summary>True if the Unit is currently guarding. Guarding is a temporary state that
        /// lasts for a number of turns, and is set by CombatController.Defend(). While
        /// defending, the Unit's effective defense stats are multiplied by UnitDefinitionSO.defendMultiplier
        /// (see EffectivePhysicalDefense / EffectiveMagicalDefense). Guarding is not a permanent
        /// state, and is reset to false at the start of any Unit's turn. Guarding is not a status effect, 
        /// and is not affected by buffs or debuffs.
        /// Multiplies the matching defense stat by UnitDefinitionSO.defendMultiplier
        /// (see EffectivePhysicalDefense / EffectiveMagicalDefense).</summary>
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

        /// <summary>Physical defense including the Defending multiplier. Use this (not PhysicalDefense)
        /// whenever damage is being mitigated.</summary>
        public float EffectivePhysicalDefense =>
            isDefending ? PhysicalDefense * Definition.defendMultiplier : PhysicalDefense;

        /// <summary>Magical defense including the Defending multiplier.</summary>
        public float EffectiveMagicalDefense =>
            isDefending ? MagicalDefense * Definition.defendMultiplier : MagicalDefense;

        // ----- References -----
        public UnitInventory Inventory { get; private set; }
        public UnitEffectController EffectController { get; private set; }
        public UnitStatsController StatsController { get; private set; }

        // ----- Internal state -----
        /// <summary>Index into the Party/EnemyParty this Unit was spawned from — matches the index of
        /// its position Transforms in CombatController's positioning lists. -1 until CombatController
        /// assigns it (e.g. a Unit created outside StartFromSetUp, like the old debug-only path).</summary>
        public int SlotIndex { get; internal set; } = -1;

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

            StatsController.OnStatsRecalculated.AddListener(() => OnResourcesChanged?.Invoke(this));
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