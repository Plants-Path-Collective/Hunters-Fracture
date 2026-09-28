using System;
using UnityEngine;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// Persistent (Overworld) state of one party member: which definition it uses plus the
    /// mutable data that must survive between combats.
    /// </summary>
    [Serializable]
    public class PartyMemberData
    {
        [Header("--- Definition ---")]
        [Tooltip("Immutable template (identity, base stats). Never replaced during a run.")]
        public UnitDefinitionSO definition;

        [Header("--- Persistent State ---")]
        [Tooltip("-1 = full HP (used the first time). 0 = the unit enters combat dead.")]
        public int currentHP = -1;
        [Tooltip("-1 = full SP (used the first time).")]
        public int currentSP = -1;

        [Tooltip("Item quantities by itemID. Rewritten after every combat.")]
        public SerializableDictionary<int, int> inventorySnapshot = new();
    }
}