using System;
using UnityEngine;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// One enemy of an encounter. Unlike PartyMemberData it carries no HP/SP: enemies always
    /// enter combat at full HP/SP. The snapshot is optional (elite variants only).
    /// </summary>
    [Serializable]
    public class EnemySlotData
    {
        public UnitDefinitionSO definition;

        [Tooltip("Optional. Leave empty for regular enemies.")]
        public SerializableDictionary<int, int> inventorySnapshot = new();
    }
}