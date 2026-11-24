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

        [Header("IShopProduct")]
        public string DisplayName =>
            localizedItemName.IsEmpty ? itemName : localizedItemName.GetLocalizedString();

        public string Description =>
            localizedEffect.IsEmpty ? effect : localizedEffect.GetLocalizedString();

        public Sprite Icon => itemIcon;
    }
}