using System;
using UnityEngine;

namespace Core.CombatSystem
{
    /// <summary>
    /// Slot layout rules in one place. Slots are camera-relative: Left/Right are the screen's
    /// left/right, for both teams.
    /// </summary>
    public static class CombatSlots
    {
        public const int MaxPerTeam = 3;

        /// <summary>Party/EnemyParty array index → slot. 0 = Left, 1 = Mid, 2 = Right.</summary>
        public static bool TryFromIndex(int index, out COMBAT_SLOT slot)
        {
            switch (index)
            {
                case 0: slot = COMBAT_SLOT.Left;  return true;
                case 1: slot = COMBAT_SLOT.Mid;   return true;
                case 2: slot = COMBAT_SLOT.Right; return true;
                default: slot = default;          return false;
            }
        }

        /// <summary>
        /// Order in which the target cursor visits a side's slots. Together they draw a ring around
        /// the arena as seen from the camera: enemies Left → Mid → Right, then allies
        /// Right → Mid → Left, then back to the first enemy.
        /// </summary>
        public static int RingPosition(UNIT_TEAM team, COMBAT_SLOT slot)
        {
            int leftToRight = slot switch
            {
                COMBAT_SLOT.Left => 0,
                COMBAT_SLOT.Mid => 1,
                _ => 2
            };

            return team == UNIT_TEAM.Enemy ? leftToRight : 2 - leftToRight;
        }
    }

    /// <summary>
    /// One Transform per slot, with named fields so the Inspector shows Mid / Left / Right
    /// instead of Element 0 / 1 / 2.
    /// </summary>
    [Serializable]
    public struct SlotAnchors
    {
        public Transform left;
        public Transform mid;
        public Transform right;

        public Transform Get(COMBAT_SLOT slot) => slot switch
        {
            COMBAT_SLOT.Left => left,
            COMBAT_SLOT.Mid => mid,
            COMBAT_SLOT.Right => right,
            _ => null
        };

        public void Set(COMBAT_SLOT slot, Transform anchor)
        {
            switch (slot)
            {
                case COMBAT_SLOT.Left: left = anchor; break;
                case COMBAT_SLOT.Mid: mid = anchor; break;
                case COMBAT_SLOT.Right: right = anchor; break;
            }
        }
    }
}