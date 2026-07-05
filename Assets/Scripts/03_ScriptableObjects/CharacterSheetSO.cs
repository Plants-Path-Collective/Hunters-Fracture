using Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace CombatSystem.UnitSystem
{
    [CreateAssetMenu(fileName = "NewCharacterSheet", menuName = "ScriptableObjects/CharacterSheet", order = 1)]
    public class CharacterSheetSO : ScriptableObject
    {
        [Header("General Info")]
        public UNIT_TYPE unitType;

        [CanBeNull]
        [Tooltip("The unit's behavior in combat. Only used if unitType is EnemyUnit.")]
        public BehaviorSO behavior;

        [Tooltip("Determines the type of damage associated with the Unit; also determines whether Strength or Magic Power is the primary offensive stat.")]
        public DAMAGE_TYPE damageType;

        [Tooltip("Skill allocation per unit type:\n- Ally Units: max 3 (2 Basic, 1 Ultimate).\n- Regular Enemy Units: 2 Basic skills.\n- Mini Bosses: 2 skills (1 Basic, 1 Ultimate).\n- Bosses: 3 skills (2 Basic, 1 Ultimate).")]
        public SkillSO[] skills;

        [Header("Prefab")]
        [Tooltip("The prefab instantiated in combat. Must have a Unit component.")]
        public GameObject combatPrefab;

        [Header("Unit Stats")]
        [Space(5)]

        [Tooltip("Unit's Health Points.")]
        public float HP;

        [Tooltip("Skill Points — used to activate skills.")]
        public float SP;

        [Tooltip("Determines how quickly the unit acts relative to others in ATB.")]
        public float speed;

        [Tooltip("Used in Physical Attack calculations.")]
        public float strenght;

        [Tooltip("Used to determine skill power, damage dealt, or healing provided.")]
        public float magicPower;

        [Tooltip("Percentage chance to dodge an attack. Subtracted from attacker's accuracy.")]
        [Range(1f, 100f)]
        public float evasion = 10;

        [Tooltip("Accuracy of the unit's attacks as a percentage.")]
        [Range(1f, 100f)]
        public float accuracy = 100;

        [Tooltip("Reduces damage taken from physical attacks/skills. Percentage.")]
        public float physicalDefense;

        [Tooltip("Reduces damage taken from magical attacks/skills. Percentage.")]
        public float magicalDefense;

        [Header("Character Information")]
        [Space(5)]
        [CanBeNull] public Sprite characterPortrait;

        [Tooltip("Used in the Wiki, Party display, combat UI, and dialogue.")]
        public string characterName;

        [FormerlySerializedAs("characterPhysicalDescription")]
        [TextArea(4, 10)]
        [Tooltip("Character description for the Hunters section of the Beeper Wiki.")]
        public string characterDescription;

        [Space(15)]
        [Header("Model & Animations — Overworld")]
        [Space(5)]
        [CanBeNull] public AnimationClip ow_idleAnimation;
        [CanBeNull] public AnimationClip ow_walkAnimation;
        [CanBeNull] public AnimationClip ow_interactAnimation;

        [Header("Model & Animations — Combat")]
        [Space(5)]
        [CanBeNull] public AnimationClip cb_idleAnimation;
        [CanBeNull] public AnimationClip cb_runAnimation;
        [CanBeNull] public AnimationClip cb_basicAttackAnimation;
        [CanBeNull] public AnimationClip cb_skill1Animation;
        [CanBeNull] public AnimationClip cb_skill2Animation;
        [CanBeNull] public AnimationClip cb_ultimateAnimation;
    }
}