using UnityEngine;

namespace Core.Shop
{
    public interface IShopProduct
    {
        string DisplayName { get; }
        string Description { get; }
        Sprite Icon { get; }
    }
}