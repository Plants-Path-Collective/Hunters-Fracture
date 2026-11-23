using UnityEngine;

namespace Core.QuestsSystem
{
    /// <summary>The player has at least X pesos. Optionally charges them on turn-in.</summary>
    [CreateAssetMenu(fileName = "Cond_Pesos_", menuName = "Quests/Conditions/Have Pesos")]
    public class HaveCurrencyConditionSO : QuestConditionSO
    {
        [SerializeField, Min(1)] private int amount = 1000;

        [Tooltip("If true, the amount is spent when the quest is turned in.")]
        [SerializeField] private bool spendOnTurnIn;

        public override int Target => Mathf.Max(1, amount);

        public override int GetCurrent(int counted) => Mathf.FloorToInt(QuestBridge.Pesos);

        public override bool CanConsume() => !spendOnTurnIn || QuestBridge.Pesos >= amount;

        public override void Consume()
        {
            if (spendOnTurnIn) QuestBridge.TrySpendPesos(amount);
        }
    }
}