using UnityEngine;
using Core;

namespace Core.CombatSystem.Unit
{
    public class Unit : MonoBehaviour
    {
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

        // ----- References -----
        public UnitInventory Inventory { get; private set; }
        public UnitEffectController EffectController { get; private set; }
        public UnitStatsController StatsController { get; private set; }

        /// <summary>
        /// Temporary stand-in for InitializeFromParty/InitializeFresh until those exist — only
        /// meant for the standalone Timeline/Combat test harnesses. Treats the given hp/sp as
        /// MaxHP/MaxSP too, since there's no real UnitStatsController to derive them from yet.
        /// </summary>
        public void DebugSetup(string name, UNIT_TEAM team, int hp, int sp, float speed)
        {
            this.Name = name;
            this.Team = team;
            this.HP = hp;
            this.MaxHP = hp;
            this.SP = sp;
            this.MaxSP = sp;
            this.Speed = speed;
        }

    }
}