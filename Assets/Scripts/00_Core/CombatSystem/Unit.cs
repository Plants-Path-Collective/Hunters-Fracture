using UnityEngine;
using Core;

namespace Core.CombatSystem.Unit
{
    /// <summary>
    /// Pure C# combatant consumed by SimpleJRPG's Battle. This class is never a
    /// MonoBehaviour — it is always built with `new`, either directly (tests /
    /// prototyping, no linked components) or from an Overworld entity's own
    /// UnitStatsController / UnitInventory / UnitEffectController when that
    /// entity enters combat.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        // ----- Identity -----
        public string Name { get; private set; }
        public UNITY_TYPE UnitType { get; private set; }
        public Sprite Portrait { get; private set; }
        public string Description { get; private set; }

        // ----- Battle (ICombatant) -----
        public bool IsAlive => HP > 0;
        public int Team { get; private set; }
        public int HP { get; set; }
        public int SP { get; set; }
        public int MaxHP { get; set; }
        public int MaxSP { get; set; }
        public float Speed { get; set; }

        // ----- References -----
        // Null unless this Unit was built via the Overworld constructor below.
        // TimelineTurnTest units (and any pure test data) simply never touch these.
        public UnitInventory inventory { get; private set; }
        public UnitEffectController effectController { get; private set; }
        public UnitStatsController statsController { get; private set; }

        /// <summary>
        /// Test/prototype constructor: fixed base stats, no linked components.
        /// Used by TimelineTurnTest to seed party/enemy data without a real
        /// Overworld entity behind it.
        /// </summary>
        public Unit(string name, int hp, int mp, float speed, int team,
            UNITY_TYPE unitType = default, Sprite portrait = null, string description = "")
        {
            Name = name;
            HP = hp;
            MaxHP = hp;
            SP = mp;
            MaxSP = mp;
            Speed = speed;
            Team = team;
            UnitType = unitType;
            Portrait = portrait;
            Description = description;
        }

        /// <summary>
        /// Overworld → Combat constructor. Derives HP/SP/Speed from the real
        /// UnitStatsController (which already includes item modifiers) and
        /// keeps references to inventory/effects so combat systems (e.g.
        /// StatusBuffTracker, ActionResolver) can read them mid-battle.
        /// </summary>
        public Unit(string name, UnitStatsController statsController, UnitInventory inventory,
            UnitEffectController effectController, int team,
            UNITY_TYPE unitType = default, Sprite portrait = null, string description = "")
        {
            Name = name;
            this.statsController = statsController;
            this.inventory = inventory;
            this.effectController = effectController;

            MaxHP = statsController.MaxHP;
            MaxSP = statsController.MaxSP;
            HP = MaxHP;
            SP = MaxSP;
            Speed = statsController.Speed;
            Team = team;
            UnitType = unitType;
            Portrait = portrait;
            Description = description;
        }

        // ----- ICombatant -----

        /// <summary>
        /// Applies incoming damage and prevents health from going below zero.
        /// The battle system interprets HP 0 as a defeated unit.
        /// </summary>
        public void TakeDamage(int amount)
        {
            HP = Mathf.Max(0, HP - amount);
        }

        /// <summary>
        /// Restores health without exceeding this Unit's own MaxHP. Uses the
        /// Unit's own MaxHP (not statsController.MaxHP) so this works whether
        /// or not a real UnitStatsController is linked.
        /// </summary>
        public void Heal(int amount)
        {
            HP = Mathf.Min(MaxHP, HP + amount);
        }
    }
}