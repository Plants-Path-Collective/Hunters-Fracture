using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// The player's party: 1 to 3 members, one per slot (index 0 = left, 1 = center, 2 = right).
    /// Carried by PlayerEntity in the Overworld.
    /// </summary>
    [Serializable]
    public class AllyParty
    {
        public const int MaxMembers = 3;

        [SerializeField] private PartyMemberData[] members = new PartyMemberData[0];

        public IReadOnlyList<PartyMemberData> Members => members;
    }
}