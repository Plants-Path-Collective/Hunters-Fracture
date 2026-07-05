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
        Exploration,
        Dialogue,
        Combat,
        Minigame
    }

    #endregion
    
    #region UNITS
    
    public enum UNIT_TYPE
    {
        AllyUnit,
        EnemyUnit
    }
    
    public enum ENEMYUNIT_FACTION
    {
        CorruptedFauna,
        Occultists,
        Mafia,
        Corpo,
        Cyborgs,
        Afligida
    }

    public enum ENEMYUNIT_RANK
    {
        Common,
        MiniBoss,
        Boss
    }

    public enum ENEMYUNIT_CATEGORY
    {
        Melee,
        Ranged,
        Tank,
        Support
    }
    
    public enum UNIT_STATE
    {
        Alive,
        Recovering,
        Dead
    }
    
    #endregion
    
    #region Combat
    
    /// <summary>
    /// Overall state of the combat encounter, driven by CombatManager.
    /// </summary>
    public enum COMBAT_STATE
    {
        Inactive,
        Initializing,
        ATBRunning,        // Normal loop — ATB ticking for all units
        TurnPending,       // One unit reached threshold; ATB paused, awaiting action
        ResolvingAction,   // ActionResolver is executing an action
        Minigame,          // Disputa or Alianza minigame is running; ATB paused
        Victory,
        Defeat
    }

    /// <summary>
    /// Which simultaneous-turn event was triggered.
    /// </summary>
    public enum SIMULTANEOUS_EVENT_TYPE
    {
        Disputa,   // Ally + Enemy reached threshold at the same frame
        Alianza    // Ally + Ally reached threshold at the same frame
    }

    /// <summary>
    /// Result of a combat encounter, written to CombatContext before returning to overworld.
    /// </summary>
    public enum COMBAT_RESULT
    {
        None,
        Victory,
        Defeat,
        Fled
    }
    
    /// <summary>
    /// Determines which ally the enemy targets.
    /// </summary>
    public enum TARGET_PRIORITY
    {
        LowestHP,       // Focus the most wounded ally
        HighestHP,      // Focus the tankiest ally
        LowestSpeed,    // Focus the slowest ally
        HighestSpeed,   // Focus the most dangerous ally
        Random          // Pick a random alive ally
    }
    
    /// <summary>
    /// Which face button is required for a minigame step.
    /// Maps directly to the Minigame action map buttons.
    /// </summary>
    public enum FACE_BUTTON
    {
        Primary,     // South  — Cross / A      → Minigame.PrimaryButton
        Secondary,   // East   — Circle / B     → Minigame.SecondaryButton
        Tertiary,    // West   — Square / X     → Minigame.TertiaryButton
        Quaternary   // North  — Triangle / Y   → Minigame.QuaternaryButton
    }
    
    #endregion

    #region Skills
    
    // FOR UNITS AND FOR SKILLS
    public enum DAMAGE_TYPE
    {
        Physical,
        Magical
    }
    
    // SKILLS
    public enum SKILL_TARGET
    {
        SingleEnemy,          
        AllEnemies,           
        SingleAlly,           
        AllAllies,            
        Self,                 
        RandomEnemy,          
        AllUnits              
    }

    public enum SKILL_TYPE
    {
        Basic,
        Ultimate
    }

    public enum EFFECT_TYPE
    {
        Damage, 
        Heal, 
        Buff, 
        Debuff, 
        StatusCondition, 
        Shield, 
        Revive, 
        RemoveStatus, 
        StealHP, 
        etc
    }

    public enum STAT_TYPE
    {
        HP, 
        SP, 
        Strength, 
        MagicPower, 
        Speed, 
        Evasion, 
        Accuracy, 
        MagicDefense, 
        PhysicalDefense
    }

    public enum STATUS_CONDITION
    {
        None, 
        Poison, 
        Paralysis, 
        Burn, 
        Freeze, 
        Sleep, 
        Confusion
    }
    
    #endregion
}