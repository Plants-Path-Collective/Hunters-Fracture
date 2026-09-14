namespace SimpleJRPG
{
    /// <summary>
    /// Contract for any actor that can participate in a battle.
    /// The battle system only needs a name, team, speed, alive state,
    /// and the two basic actions of taking damage or healing.
    /// </summary>
    public interface ICombatant
    {
        string Name { get; }
        bool IsAlive { get; }
        int Team { get; }
        float Speed { get; }
        void TakeDamage(int amount);
        void Heal(int amount);
    }
}
