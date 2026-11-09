using UnityEngine;
using Core.CombatSystem.Backpack;
using Core.CombatSystem.Units;

namespace Core
{
    #region INPUT

    /// <summary>
    /// If you want to add a new input action map to the project,
    /// you must add it to this enum in the order shown in the
    /// project settings; otherwise, it will not work.
    /// </summary>
    public enum INPUTACTION_MAP
    {
        Empty,
        UI,
        Rewards,
        Social,
        Overworld,
        Dialogue,
        Combat
    }

    #endregion

    #region COMBAT

    public enum COMBAT_SLOT
    {
        Left,
        Mid,
        Right
    }

    /// <summary>
    /// Timeline operations that can be performed on the queue. Used to notify
    /// subscribers of changes to the timeline.
    /// </summary>
    public enum TIMELINE_OPERATION
    {
        Initialize,
        Pop,
        Reinsert,
        Advance,
        Delay,
        MoveToFront,
        MoveToBack,
        Swap,
        InsertAfter,
        GrantExtraTurn,
        Remove,
        Clear
    }

    /// <summary>
    /// Result of a finished combat encounter.
    /// </summary>
    public enum COMBAT_OUTCOME
    {
        Victory,
        Defeat,
        Fled 
    }

    /// <summary>
    /// The team a Unit belongs to. Used to determine which units are allies
    /// and which are enemies.
    /// </summary>
    public enum UNIT_TEAM
    {
        Ally,
        Enemy
    }

    /// <summary>
    /// The type of combatant a Unit is. Used to determine which
    /// abilities it can use and which stats it has.
    /// </summary>
    public enum UNITY_TYPE
    {
        Physical,
        Magical
    }

    /// <summary>
    /// Which stat a StatModifierEffectSO applies to.
    /// </summary>
    public enum STAT_TYPE
    {
        HP,
        SP,
        Speed,
        Strength,
        MagicPower,
        PhysicalDefense,
        MagicalDefense
    }

    /// <summary>Who an ActionSO needs the player to pick (or resolves by itself).</summary>
    public enum TARGET_TYPE
    {
        None,        // no pick: its effects choose by side and slot
        Self,
        SingleEnemy,
        SingleAlly,  // alive ally, the actor included
        SingleAny,   // any living unit of either side
        AllEnemies,
        AllAllies
    }

    public enum EFFECT_TARGET_MODE
    {
        ActionTargets,            // every unit the action resolved to
        ActionTargetsAlliesOnly,  // ...only those on the actor's side
        ActionTargetsEnemiesOnly, // ...only those on the other side
        SlotsOfSide,              // units picked by side and slot, ignoring the player's target
        RightNeighborOfActionTargets
    }

    /// <summary>Side of an effect's targets, relative to the actor.</summary>
    public enum RELATIVE_SIDE { Allies, Enemies }

    [System.Flags]
    public enum SLOT_MASK { None = 0, Left = 1, Mid = 2, Right = 4 }

    /// <summary>Why a unit left its alternate form.</summary>
    public enum FORM_EXIT_REASON
    {
        Voluntary,      // the unit chose to revert (its ultimate)
        FuryDepleted,   // the Fury meter reached 0
        Downed          // HP reached 0 while in the form
    }

    /// <summary>Flat amount added to a derived stat while a form is active.</summary>
    [System.Serializable]
    public struct FlatStatBonus
    {
        public STAT_TYPE stat;
        public float value;
    }

    /// <summary>Model shown while a unit is in a given alternate form.</summary>
    [System.Serializable]
    public struct FormModel
    {
        public UnitFormSO form;
        public GameObject model;
    }

    /// <summary>value = baseValue + perPoint × the caster's stat. perPoint 0 makes it a constant.</summary>
    [System.Serializable]
    public struct ScaledValue
    {
        public float baseValue;
        public STAT_TYPE stat;
        [Tooltip("Added per point of the caster's stat (negative allowed). HP means max HP.")]
        public float perPoint;

        public float Evaluate(Unit caster) => baseValue + perPoint * caster.GetStat(stat);
    }

    #endregion

    #region DUNGEON
    public enum ENEMY_STATE
    {
        Patrol,
        Detecting,
        Chase,
        Recover,
        CombatStarting
    }

    #endregion

    #region SOCIAL

    [System.Serializable]
    public struct BackpackSlot
    {
        public ConsumableSO consumableSO;
        public int quantity;
    }

    #endregion

    #region UNITS

    public struct UnitStatValues
    {
        public float HP;
        public float SP;
        public float Speed;
        public float Strength;
        public float MagicPower;
        public float PhysicalDefense;
        public float MagicalDefense;

        public float Get(STAT_TYPE stat) => stat switch
        {
            STAT_TYPE.HP => HP,
            STAT_TYPE.SP => SP,
            STAT_TYPE.Speed => Speed,
            STAT_TYPE.Strength => Strength,
            STAT_TYPE.MagicPower => MagicPower,
            STAT_TYPE.PhysicalDefense => PhysicalDefense,
            STAT_TYPE.MagicalDefense => MagicalDefense,
            _ => 0f
        };

        public void Set(STAT_TYPE stat, float value)
        {
            switch (stat)
            {
                case STAT_TYPE.HP: HP = value; break;
                case STAT_TYPE.SP: SP = value; break;
                case STAT_TYPE.Speed: Speed = value; break;
                case STAT_TYPE.Strength: Strength = value; break;
                case STAT_TYPE.MagicPower: MagicPower = value; break;
                case STAT_TYPE.PhysicalDefense: PhysicalDefense = value; break;
                case STAT_TYPE.MagicalDefense: MagicalDefense = value; break;
            }
        }

        public static UnitStatValues From(UnitDefinitionSO definition) => new()
        {
            HP = definition.maxHP,
            SP = definition.maxSP,
            Speed = definition.speed,
            Strength = definition.strength,
            MagicPower = definition.magicPower,
            PhysicalDefense = definition.physicalDefense,
            MagicalDefense = definition.magicalDefense
        };
    }

    #endregion

    #region SCHEDULE
    /// <summary>Phases of the in-game day. Flags so windows and access rules can be masks.</summary>
    [System.Flags]
    public enum DAY_PHASE
    {
        None      = 0,
        Morning   = 1,
        Afternoon = 2,
        Night     = 4
    }

    /// <summary>Reasons that can pause the game clock.</summary>
    public enum CLOCK_PAUSE_REASON
    {
        Combat,
        Dialogue,
        Transition,
        Menu,
        Debug
    }
    #endregion
}