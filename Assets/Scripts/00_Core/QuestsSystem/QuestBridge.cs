using UnityEngine;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.QuestsSystem
{
    /// <summary>
    /// The ONLY place where the quest system touches the rest of the game (money and inventory).
    /// If the way you store pesos or items changes, edit this file and nothing else.
    /// </summary>
    public static class QuestBridge
    {
        /// <summary>Current money.</summary>
        public static float Pesos => GameManager.Instance != null ? GameManager.Instance.Pesos : 0f;

        /// <summary>Spends money. Returns false if there is not enough.</summary>
        public static bool TrySpendPesos(float amount) =>
            GameManager.Instance != null && GameManager.Instance.TrySpendPesos(amount);

        /// <summary>Total copies of an item across every unit of the party (combat + bench).</summary>
        public static int CountItem(ItemSO item)
        {
            if (item == null) return 0;

            AllyParty party = GameManager.Instance != null ? GameManager.Instance.party : null;
            if (party == null) return 0;

            int total = 0;
            foreach (PartyMemberData member in party.Members)
            {
                if (member == null || member.inventorySnapshot == null) continue;

                if (member.inventorySnapshot.TryGetValue(item.itemID, out int quantity))
                    total += quantity;
            }

            return total;
        }
    }
}