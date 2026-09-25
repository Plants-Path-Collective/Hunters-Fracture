using UnityEngine;
using Core;

namespace Core.CombatSystem.Unit
{
    public class Unit : MonoBehaviour
    {
        public UnitDefinitionSO definition;

        // ----- Identity -----
        public string Name { get; private set; }
        public UNITY_TYPE UnitType { get; private set; }
        public UNIT_TEAM Team { get; private set; }
        public Sprite Portrait { get; private set; }
        public string Description { get; private set; }

        // ----- Stats -----

        public bool IsAlive => HP > 0;
        public int HP { get; internal set; }
        public int MaxHP { get; set; }

        public int SP { get; internal set; }
        public int MaxSP { get; set; }

        public float Speed { get; set; }
        
        public float Strength { get; set; }
        public float MagicPower { get; set; }

        public float PhysicalDefense { get; set; }
        public float MagicalDefense { get; set; }

        // ----- References -----
        public UnitInventory Inventory { get; private set; }
        public UnitEffectController EffectController { get; private set; }
        public UnitStatsController StatsController { get; private set; }

        private void Awake()
        {
            Inventory = GetComponent<UnitInventory>();
            EffectController = GetComponent<UnitEffectController>();
            StatsController = GetComponent<UnitStatsController>();

            InitializeFromDefinition(definition);
        }

        public void InitializeFromDefinition(UnitDefinitionSO definition)
        {
            this.definition = definition;

            Name = definition.unitName;
            UnitType = definition.type;
            Team = definition.team;
            Portrait = definition.portrait;
            Description = definition.description;

            MaxHP = definition.maxHP;
            HP = MaxHP;

            MaxSP = definition.maxSP;
            SP = MaxSP;

            Speed = definition.speed;
            Strength = definition.strength;
            MagicPower = definition.magicPower;
            PhysicalDefense = definition.physicalDefense;
            MagicalDefense = definition.magicalDefense;
        }
    }
}