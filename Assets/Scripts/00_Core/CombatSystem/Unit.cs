using UnityEngine;
using SimpleJRPG;
using Core;
using UnityEngine.UI;

namespace CombatSystem.Unit 
{    
    public class Unit : MonoBehaviour, ICombatant
    {
        [Header("----- Identity -----")]
        [SerializeField] private string unitName;
        [SerializeField] private UNITY_TYPE unitType;

        [Tooltip("Sprite that will be used in the Turn Timeline on combat")]
        [SerializeField] private Sprite unitPortrait;
        [SerializeField] private string unitDescription;

        [Header("----- Battle (ICombatant) -----")]
        [SerializeField] private int team = 0;
        public string Name => unitName;
        public bool IsAlive => HP > 0;
        public int Team => team;
        public int HP { get;  set; }
        public int SP { get;  set; }
        public int MaxHP { get;  set; }
        public int MaxSP { get;  set; }
        public float Speed { get; set; }

        [Header("----- References -----")]
        public UnitInventory inventory { get; private set; }
        public UnitEffectController effectController { get; private set; }
        public UnitStatsController statsController { get; private set; }

        private void Awake()
        {
            inventory = GetComponent<UnitInventory>();
            effectController = GetComponent<UnitEffectController>();
            statsController = GetComponent<UnitStatsController>();

            // Explicit call instead of relying on Unity's Awake() execution
            // order between components on the same GameObject — that order
            // is not guaranteed, so MaxHP/MaxSP must be resolved here first.
            statsController.RecalculateStats(inventory);

            HP = statsController.MaxHP;
            SP = statsController.MaxSP;
            Speed = statsController.Speed;

        }

        public Unit(string name, int hp, int mp, float speed, int team)
        {
            unitName = name;
            HP = hp;
            MaxHP = hp;
            SP = mp;
            MaxSP = mp;
            Speed = speed;
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