using Core.CombatSystem.Units;
using DG.Tweening;
using Overworld;
using UnityEngine;

namespace ExtendedDebug
{
    public class HealStation : MonoBehaviour
    {
        [SerializeField] private GameObject sphereFeedback;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            PlayerEntity playerEntity = other.GetComponent<PlayerEntity>();
            if (playerEntity == null) return;

            HealParty(playerEntity.Party);

            if (sphereFeedback != null)
                sphereFeedback.transform.DOPunchScale(new Vector3(5, 5, 5), 0.5f, 1, 1f);
        }

        private static void HealParty(AllyParty party)
        {
            if (party == null) return;

            foreach (PartyMemberData member in party.Members)
            {
                if (member == null || member.definition == null) continue;

                member.currentHP = member.definition.maxHP;
                member.currentSP = member.definition.maxSP;
            }
        }
    }
}