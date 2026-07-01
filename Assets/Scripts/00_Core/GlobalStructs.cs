namespace _00_Core
{
    // UNITS
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
}