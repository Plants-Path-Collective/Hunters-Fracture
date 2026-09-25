using UnityEngine;

namespace Core.CombatSystem.Unit
{
    [CreateAssetMenu(fileName = "NewUnitDefinition", menuName = "Units/Unit Definition")]
    public class UnitDefinitionSO : ScriptableObject
    {
        [Header("--- Identity ---")]
        public string unitName;
        public UNITY_TYPE type;
        public UNIT_TEAM team;
        public Sprite portrait;
        [TextArea(3, 5)]
        public string description;

        [Header("--- Stats ---")]
        public int maxHP;
        public int maxSP;
        public float speed;
        public float strength;
        public float magicPower;
        public float physicalDefense;
        public float magicalDefense;

        [Header("--- Combat Parameters ---")]
        public int attackRange;
    }
}