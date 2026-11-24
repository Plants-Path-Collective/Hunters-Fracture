using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;
using PlantsPathCo.DialogueSystem;

namespace Core.UI
{
    /// <summary>
    /// Dialogue action: opens the item assignation UI with these items (e.g. a quest reward).
    /// Works on a line or on an answer. The UI opens when the conversation ends.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Dialogue Actions/Give Items", fileName = "GiveItems_")]
    public class GiveItemsActionSO : DialogueActionSO
    {
        [Tooltip("Items offered to the player. The player assigns them to party members.")]
        [SerializeField] private List<ItemSO> items = new();

        public override void Execute(ConversationTrigger source)
        {
            if (!ItemAssignationController.TryOpen(items))
                Debug.LogWarning("[Items] GiveItemsActionSO could not open the item assignation UI.", this);
        }
    }
}