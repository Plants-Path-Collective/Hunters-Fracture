using UnityEngine;
using Core;
using UnityEngine.Localization;

namespace Core.CombatSystem.Backpack
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Combat System/Backpack/Consumable")]
    public class ConsumableSO : ScriptableObject
    {
        public string itemName;
        public LocalizedString localizedItemName;
        public Sprite itemIcon;
        public string effect;
        public LocalizedString localizedEffect;
        public STAT_TYPE statType;
        public int restoreAmount;

        // Add any other properties or methods related to consumables here
    }
}