using Core;
using JetBrains.Annotations;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    [CreateAssetMenu(fileName = "NewCharacterSheet", menuName = "ScriptableObjects/CharacterSheet", order = 1)]
    public class CharacterSheetSO : ScriptableObject
    {
        [Header("General Info")]
        public UNIT_TYPE unitType;

        [CanBeNull]
        [Tooltip("Enemy AI behavior. Leave null for ally units.")]
        public BehaviorSO behavior;

        [Tooltip("Determines the unit's primary offensive stat and the type of their basic attack.\n" +
                 "Physical → Strength is higher, basic attack deals physical damage.\n" +
                 "Magical  → MagicPower is higher, basic attack deals magical damage.\n" +
                 "Individual skills may use a different type — set damageType per SkillSO.")]
        public DAMAGE_TYPE damageType;

        [Header("Basic Attack")]
        [Tooltip("The unit's basic attack definition. Must always be assigned.")]
        public AttackSO basicAttack;

        [Header("Skills")]
        [Tooltip("Skill allocation per unit type:\n" +
                 "  Ally units:     2 Basic + 1 Ultimate (max 3)\n" +
                 "  Regular enemies: 2 Basic skills\n" +
                 "  Mini-bosses:    1 Basic + 1 Ultimate\n" +
                 "  Bosses:         2 Basic + 1 Ultimate")]
        public SkillSO[] skills;

        [Header("Combat Prefab")]
        [Tooltip("Instantiated by UnitSpawner in the CombatStage. Must have a Unit component.")]
        public GameObject combatPrefab;

        [Header("Stats")]
        [Space(5)]
        [Tooltip("Maximum Health Points.")]
        public float HP;
        [Tooltip("Maximum Skill Points — spent when using skills.")]
        public float SP;
        [Tooltip("Affects how fast the ATB bar fills. Higher = acts more frequently.")]
        public float speed;
        [Tooltip("Primary offensive stat for Physical-type damage.")]
        public float strenght;
        [Tooltip("Primary offensive stat for Magical-type damage.")]
        public float magicPower;
        [Tooltip("Dodge chance %. Subtracted from attacker's accuracy.")]
        [Range(1f, 100f)]
        public float evasion = 10f;
        [Tooltip("Hit chance % for this unit's attacks.")]
        [Range(1f, 100f)]
        public float accuracy = 90f;
        [Tooltip("Damage reduction % against physical hits.")]
        [Range(0f, 75f)]
        public float physicalDefense;
        [Tooltip("Damage reduction % against magical hits.")]
        [Range(0f, 75f)]
        public float magicalDefense;

        [Header("Character Information")]
        [Space(5)]
        [CanBeNull] public Sprite characterPortrait;
        public string characterName;
        [TextArea(4, 10)]
        public string characterDescription;

        [Header("Animations — Overworld")]
        [Space(5)]
        [CanBeNull] public AnimationClip ow_idleAnimation;
        [CanBeNull] public AnimationClip ow_walkAnimation;
        [CanBeNull] public AnimationClip ow_interactAnimation;

        [Header("Animations — Combat")]
        [Space(5)]
        [CanBeNull] public AnimationClip cb_idleAnimation;
        [CanBeNull] public AnimationClip cb_runAnimation;
        [CanBeNull] public AnimationClip cb_basicAttackAnimation;
        [CanBeNull] public AnimationClip cb_skill1Animation;
        [CanBeNull] public AnimationClip cb_skill2Animation;
        [CanBeNull] public AnimationClip cb_ultimateAnimation;
    }
}