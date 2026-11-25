using UnityEngine;
using Core;
using UnityEngine.Localization;
using Core.Shop;

namespace Core.CombatSystem.Backpack
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Combat System/Backpack/Consumable")]
    public class ConsumableSO : ScriptableObject, IShopProduct
    {
        public Sprite consumableIcon;
        public string consumableName;
        public LocalizedString localizedName;
        public string consumableDescription;
        public LocalizedString localizedDescription;
        public STAT_TYPE statType;
        public int restoreAmount;

        [Header("IShopProduct")]
        public string DisplayName =>
            localizedName.IsEmpty ? consumableName : localizedName.GetLocalizedString();

        public string Description =>
            localizedDescription.IsEmpty ? consumableDescription : localizedDescription.GetLocalizedString();

        public Sprite Icon => consumableIcon;
    }
}