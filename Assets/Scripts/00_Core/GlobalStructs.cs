using Core.CombatSystem.Backpack;

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

    /// <summary>Side of an effect's targets, relative to the actor.</summary>
    public enum RELATIVE_SIDE { Allies, Enemies }

    [System.Flags]
    public enum SLOT_MASK { None = 0, Left = 1, Mid = 2, Right = 4 }

    #endregion

    #region DUNGEON
    public enum ENEMY_STATE
    {
        Patrol,
        Chase // not implemented yet — reserved for when perception exists
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
}