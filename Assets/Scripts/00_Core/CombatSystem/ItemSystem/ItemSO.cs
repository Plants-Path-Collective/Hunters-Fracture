using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item System/Item")]
public class ItemSO : ScriptableObject
{
    public int itemID; // Unique identifier
    public string itemName;
    public Sprite itemIcon;
}