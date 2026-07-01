using _00_Core;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "ScriptableObjects/Skill", order = 2)]
    public class SkillSO
    {
        // ------- Basic Information -------
        public string skillName;
        [TextArea] public string description;
        public Sprite icon;
        public AnimationClip animationClip;   // Skill Animation
        public float animationDuration;       // Duration of the animation (affects the ATB)
        
        // ------- Objective-------
        public SKILL_TYPE skillType;
        
        // ------- Objective-------
        public SKILL_TARGET targetType; 
        
        // ------- Cost -------
        public float spCost;                  // Energy cost
        public float hpCost;                  // Vitality cost
        public int usageLimit;                // -1 = unlimited, >0 = uses per combat
        
        // ------- Effects -------
        public SkillEffect[] effects;         // List of effects (damage, healing, buffs, etc.)
        // Instead of fixed fields, we use a list to combine effects
        
        // ------- Combat Modifiers -------
        public float basePower;               // Base Power (for damage/healing)
        public float accuracy;                // Accuracy % (100% always hits)
        public DAMAGE_TYPE damageType;        

        // ------- Turno y prioridad -------
        public float speedModifier;           // Modificador de velocidad (ej. 0.8 = más lento)
        public bool isUltimate;               // Si es la habilidad definitiva
        public bool isPassive;                // Si es pasiva (no se usa activamente)

        // ------- Cooldown -------
        public float cooldown;                // Tiempo (segundos) en ATB para reutilizar
    }
}