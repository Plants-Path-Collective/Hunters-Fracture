using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.QuestsSystem
{
    /// <summary>The party owns at least N copies of an item (any unit, combat or bench).</summary>
    [CreateAssetMenu(fileName = "Cond_Item", menuName = "Quests/Conditions/Have Item")]
    public class HaveItemConditionSO : QuestConditionSO
    {
        // NOTE: add the 'using' for the namespace of ItemSO if the compiler asks for it.
        [SerializeField] private ItemSO item;

        [SerializeField, Min(1)] private int amount = 1;

        public override int Target => Mathf.Max(1, amount);

        public override int GetCurrent(int counted) => QuestBridge.CountItem(item);
    }
}