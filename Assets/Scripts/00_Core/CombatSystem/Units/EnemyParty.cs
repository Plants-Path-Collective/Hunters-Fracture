using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// A reusable encounter preset: the enemies an EnemyEntity brings into combat. Unlike
    /// Party, there's no member cap — an encounter can be as large as the design calls for.
    /// </summary>
    [Serializable]
    public class EnemyParty
    {
        [SerializeField] private EnemySlotData[] slots = new EnemySlotData[0];

        public IReadOnlyList<EnemySlotData> Slots => slots;
    }
}