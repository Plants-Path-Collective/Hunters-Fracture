using Core.CombatSystem.Units;

namespace Core.CombatSystem
{
    /// <summary>
    /// Mutable payload for CombatController.OnBeforeDeath. A handler that leaves the unit alive
    /// (HP above 0) sets prevented, and CombatController skips Kill().
    /// </summary>
    public class DeathContext
    {
        public Unit unit;
        public bool prevented;
    }
}