using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.QuestsSystem
{
    /// <summary>The party owns at least N copies of an item (any member, combat slots or bench).</summary>
    [CreateAssetMenu(fileName = "Cond_Item", menuName = "Quests/Conditions/Have Item")]
    public class HaveItemConditionSO : QuestConditionSO
    {
        [SerializeField] private ItemSO item;

        [SerializeField, Min(1)] private int amount = 1;

        [Tooltip("If true, the items are removed from the party when the quest is turned in.")]
        [SerializeField] private bool consumeOnTurnIn;

        public override int Target => Mathf.Max(1, amount);

        public override int GetCurrent(int counted) => QuestBridge.CountItem(item);

        // With consumeOnTurnIn the party must still own the items at turn-in time.
        public override bool CanConsume() => !consumeOnTurnIn || QuestBridge.CountItem(item) >= Target;

        public override void Consume()
        {
            if (consumeOnTurnIn) QuestBridge.TryRemoveItem(item, Target);
        }
    }
}