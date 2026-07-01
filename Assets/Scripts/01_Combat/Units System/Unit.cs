using _00_Core;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    public class Unit : MonoBehaviour
    {
        [Header("Combat Settings")] 
        [Space(5)]
        
        public UNIT_STATE unitState;
        
        [Tooltip("Current ATB progress. Fills over time at a rate equal to unit's speed. When it reaches ATB_THRESHOLD, the unit acts and the progress resets.")]
        public float atbPoints;

        [Tooltip("Global threshold in ATB points. Determines how long a full turn takes (e.g., 1000 points → 10s for speed 100). Shared across all units.")]
        public float atbThreshold = 1000f;
        [Space(15)] 
        
        
        [Header("CharacterSheet")]
        [SerializeField] private CharacterSheetSO _characterSheetSO;
        [Space(10)]
    
        [Header("Unit Stats")]
        [Space (5)]
        public float currentHP;
        public float maxHP;

        public float currentSP;
        public float maxSP;
    
        public float speed;
        public float strenght;
        public float magicPower;
    
        public float evasion;
        public float accuracy;

        public float physicalDefense;
        public float magicalDefense;

    }
}