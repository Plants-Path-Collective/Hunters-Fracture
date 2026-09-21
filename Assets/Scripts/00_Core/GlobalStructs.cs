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
        Social,
        Overworld,
        Dialogue,
        Combat
    }

    #endregion

    #region COMBAT

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
        Fled // not producible yet — Flee() verb not implemented
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

    #endregion
}