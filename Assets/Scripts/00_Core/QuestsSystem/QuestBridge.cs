using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// The ONLY place where the quest system touches the rest of the game (money and inventory).
    /// If the way you store pesos or items changes, edit this file and nothing else.
    /// </summary>
    public static class QuestBridge
    {
        private static bool warnedItems;

        /// <summary>Current money.</summary>
        public static float Pesos => GameManager.Instance != null ? GameManager.Instance.Pesos : 0f;

        /// <summary>Spends money. Returns false if there is not enough.</summary>
        public static bool TrySpendPesos(float amount) =>
            GameManager.Instance != null && GameManager.Instance.TrySpendPesos(amount);

        /// <summary>Total copies of an item across every unit of the party (combat + bench).</summary>
        public static int CountItem(ItemSO item)
        {
            if (item == null) return 0;

            // TODO(wire): sum the quantity of 'item' over every PartyMemberData snapshot in AllyParty.
            if (!warnedItems)
            {
                Debug.LogWarning("[Quests] QuestBridge.CountItem is not wired to AllyParty yet.");
                warnedItems = true;
            }

            return 0;
        }
    }
}