using System;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// itemID → quantity. The serialized type used by inventory snapshots; it has its own
    /// Inspector drawer (ItemSnapshotDrawer) so items can be assigned by dragging an ItemSO.
    /// </summary>
    [Serializable]
    public class ItemSnapshot : SerializableDictionary<int, int>
    {
    }
}