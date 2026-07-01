using _00_Core;
using JetBrains.Annotations;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    [CreateAssetMenu(fileName = "NewCharacterSheet", menuName =  "ScriptableObjects/CharacterSheet", order = 1)]
    public class CharacterSheetSO : ScriptableObject
    {
        [Header("General Info")] 
        public UNIT_TYPE unitType;
        [Tooltip(("Determines the type of damage associated with the Unit; this also determines which stat should be higher (Strength or Magic Power)."))]
        public DAMAGE_TYPE damageType;
        
        [Tooltip("Skill allocation per unit type:\n- Ally Units: max 3 (2 Basic, 1 Ultimate).\n- Regular Enemy Units: 2 Basic skills.\n- Mini Bosses: 2 skills (1 Basic, 1 Ultimate).\n- Bosses: 3 skills (2 Basic, 1 Ultimate).")]        
        public SkillSO[] skills;
        [Space(15)] 
        
        [Header("Unit Stats")] 
        [Space(5)] 
        
        [Tooltip(("Unit's Health Points"))]
        public float HP;
        
        [Tooltip(("The number of energy points the Unit has; these are used to activate or cast abilities."))]
        public float SP;
        
        [Tooltip(("Unit Speed determines how quickly it acts relative to other Units in combat."))]
        public float speed;
        
        [Tooltip(("Used in the calculation of Physical Attacks."))]
        public float strenght;
        
        [Tooltip(("It is used in calculations to determine a skill's power, how much damage a skill deals, or how much healing it provides."))]
        public float magicPower;
        
        [Tooltip(("Determines the percentage chance that the Unit will dodge an attack or a skill; this value is subtracted from the attacking Unit's accuracy; it may vary due to status changes."))]
        [Range(1f, 100f)]
        public float evasion = 10;
        
        [Tooltip(("Determines the accuracy of the Unit's attacks as a percentage; this may vary depending on changes in status."))]
        [Range(1f, 100f)]
        public float accuracy = 100;

        [Tooltip(("It is used to calculate the damage taken from physical attacks or skills. Percentage."))]
        public float physicalDefense;
        
        [Tooltip(("It is used to calculate the damage taken from magical attacks or skills. Percentage."))]
        public float magicalDefense;
        [Space(15)] 

        [Header("Character Information")] 
        [Space(5)]
        [CanBeNull]
        public Sprite characterPortrait;
        
        [Tooltip(("Characters name; used for the Wiki, the Party display, the info panel during combat, and for dialogue."))]
        public string characterName;
        
        [TextArea(4, 10)]
        [Tooltip(("Character description; used for the “Hunters” section on the Beeper Wiki"))]
        public string characterPhysicalDescription;
        [Space (15)]

        [Header("Model & Animations for Overworld")] 
        [Space(5)]
        [CanBeNull]
        public AnimationClip ow_idleAnimation;
        public AnimationClip ow_walkAnimation;
        public AnimationClip ow_interactAnimation;
        [Space(15)] 

        [Header("Model & Animations for Overworld")] 
        [Space(5)] 
        [CanBeNull]
        public AnimationClip cb_idleAnimation;
        public AnimationClip cb_runAnimation;
        public AnimationClip cb_basicAttackAnimation;
        public AnimationClip cb_skill1Animation;
        public AnimationClip cb_skill2Animation;
        public AnimationClip cb_ultimateAnimation;
    }
}
