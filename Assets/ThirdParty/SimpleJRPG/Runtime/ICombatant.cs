namespace SimpleJRPG
{
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
