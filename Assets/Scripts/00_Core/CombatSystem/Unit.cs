using UnityEngine;
using SimpleJRPG;
using Core;

namespace CombatSystem.Unit 
{    
    public class Unit : MonoBehaviour, ICombatant
    {
        [Header("----- Identity -----")]
        [SerializeField] private string unitName;
        [SerializeField] private UNITY_TYPE unitType;

        [Tooltip("Sprite that will be used in the Turn Timeline on combat")]
        [SerializeField] private string unitPortrait;
        [SerializeField] private string unitDescription;

        [Header("----- Battle (ICombatant) -----")]
        [SerializeField] private int team = 0;
        public string Name => unitName;
        public bool IsAlive => HP > 0;
        public int Team => team;
        public int HP { get; private set; }
        public int SP { get; private set; }
        public float Speed => statsController.Speed;

        [Header("----- References -----")]
        public UnitInventory inventory { get; private set; }
        public UnitEffectController effectController { get; private set; }
        public UnitStatsController statsController { get; private set; }

        private void Awake()
        {
            inventory = GetComponent<UnitInventory>();
            effectController = GetComponent<UnitEffectController>();
            statsController = GetComponent<UnitStatsController>();

            HP = statsController.MaxHP;
            SP = statsController.MaxSP;

        }

        // ----- Iherited from ICombatant  -----
        public void TakeDamage(int amount)
        {
            HP = Mathf.Max(0, HP - amount);
        }

        public void Heal(int amount)
        {
            HP = Mathf.Min(statsController.MaxHP, HP + amount);
        }
    }
}