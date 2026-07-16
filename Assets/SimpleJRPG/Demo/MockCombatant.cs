using System;
using SimpleJRPG;

namespace SimpleJRPG.Demo
{
    public class MockCombatant : ICombatant
    {
        public string Name { get; }
        public bool IsAlive => HP > 0;
        public int Team { get; }
        public float Speed { get; }

        public int HP { get; private set; }
        public int MaxHP { get; }
        public int MP { get; set; }
        public int MaxMP { get; }

        public MockCombatant(string name, int hp, int mp, float speed, int team)
        {
            Name = name;
            HP = hp;
            MaxHP = hp;
            MP = mp;
            MaxMP = mp;
            Speed = speed;
            Team = team;
        }

        public void TakeDamage(int amount)
        {
            HP = Math.Max(0, HP - amount);
        }

        public void Heal(int amount)
        {
            HP = Math.Min(MaxHP, HP + amount);
        }
    }
}
