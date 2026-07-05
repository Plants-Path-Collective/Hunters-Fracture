using Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace CombatSystem
{
    [System.Serializable]
    public class SkillEffect
    {
        public EFFECT_TYPE effectType;        
        public STAT_TYPE statAffected;       // For buffs/debuffs 
        public STATUS_CONDITION condition;   // To apply status changes
        public float value;
        public float durationInSeconds;      
        public float chance;                 // Probability of applying the effect (0-1)
    }
}