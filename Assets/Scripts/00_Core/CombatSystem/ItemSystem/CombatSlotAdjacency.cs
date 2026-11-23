using System.Collections.Generic;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Slot adjacency inside one team: Left-Center and Center-Right are adjacent, Left-Right are not.
    /// Built on CombatSlots.TryFromIndex (index 0 = left, 1 = center, 2 = right), so it does not
    /// depend on the enum's member names.
    /// </summary>
    public static class CombatSlotAdjacency
    {
        /// <summary>Slot index (0..MaxPerTeam-1) or -1 if the slot is unknown.</summary>
        public static int IndexOf(COMBAT_SLOT slot)
        {
            for (int i = 0; i < CombatSlots.MaxPerTeam; i++)
                if (CombatSlots.TryFromIndex(i, out COMBAT_SLOT candidate) && candidate.Equals(slot))
                    return i;

            return -1;
        }

        /// <summary>Fills <paramref name="into"/> with the LIVING units of the same team whose slot is
        /// next to the unit's slot. Dead units do not hold a slot, so they never count.</summary>
        public static void CollectAdjacent(IReadOnlyList<Unit> roster, Unit unit, List<Unit> into)
        {
            into.Clear();
            if (unit == null || !unit.Slot.HasValue) return;

            int origin = IndexOf(unit.Slot.Value);
            if (origin < 0) return;

            foreach (Unit other in roster)
            {
                if (other == null || other == unit || !other.IsAlive || other.Team != unit.Team) continue;
                if (!other.Slot.HasValue) continue;

                int index = IndexOf(other.Slot.Value);
                if (index >= 0 && System.Math.Abs(index - origin) == 1)
                    into.Add(other);
            }
        }
    }
}