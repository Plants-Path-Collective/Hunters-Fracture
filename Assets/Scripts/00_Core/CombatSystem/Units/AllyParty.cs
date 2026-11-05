using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// The player's roster. The first <see cref="MaxMembers"/> entries are the active party that
    /// enters combat (index 0 = left, 1 = center, 2 = right); any extra members wait on the bench.
    /// Carried by PlayerEntity in the Overworld. Edit it only outside combat.
    /// </summary>
    [Serializable]
    public class AllyParty
    {
        /// <summary>Number of combat slots, i.e. how many members fight at once.</summary>
        public const int MaxMembers = 3;

        // Same field name as before: Unity serializes arrays and lists identically,
        // so the party already configured in the Inspector is preserved.
        [SerializeField] private List<PartyMemberData> members = new();

        /// <summary>Raised after any change in membership or order.</summary>
        public event Action OnChanged;

        /// <summary>Full roster: active members first, then the bench.</summary>
        public IReadOnlyList<PartyMemberData> Members => members;

        /// <summary>Total members, active and benched.</summary>
        public int Count => members.Count;

        /// <summary>How many members will enter the next combat.</summary>
        public int ActiveCount => Mathf.Min(members.Count, MaxMembers);

        /// <summary>True if the member at this index enters combat.</summary>
        public bool IsActive(int index) => index >= 0 && index < ActiveCount;

        /// <summary>True if a member using this definition is already in the roster.</summary>
        public bool Contains(UnitDefinitionSO definition)
        {
            foreach (PartyMemberData member in members)
                if (member != null && member.definition == definition) return true;

            return false;
        }

        /// <summary>
        /// Adds a member at the end of the roster (the bench if the party is already full).
        /// Each character can appear only once.
        /// </summary>
        /// <returns>False if the member is invalid or the character is already in the roster.</returns>
        public bool TryAdd(PartyMemberData member)
        {
            if (member == null || member.definition == null) return false;
            if (Contains(member.definition)) return false;

            members.Add(member);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes the member at this index. The roster always keeps at least one member.
        /// </summary>
        public bool TryRemoveAt(int index)
        {
            if (!IsValidIndex(index) || members.Count <= 1) return false;

            members.RemoveAt(index);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Moves a member to another position, shifting the ones in between.</summary>
        public bool TryMove(int from, int to)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to) || from == to) return false;

            PartyMemberData member = members[from];
            members.RemoveAt(from);
            members.Insert(to, member);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Swaps two members (also works between an active slot and the bench).</summary>
        public bool TrySwap(int a, int b)
        {
            if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return false;

            (members[a], members[b]) = (members[b], members[a]);
            OnChanged?.Invoke();
            return true;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < members.Count;
    }
}