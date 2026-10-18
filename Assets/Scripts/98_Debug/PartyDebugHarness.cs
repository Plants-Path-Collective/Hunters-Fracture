using UnityEngine;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace ExtendedDebug
{
    /// <summary>
    /// Test-only: spawns Units from a Party / enemy slots authored in the Inspector and logs
    /// their resulting state, so Unit initialization can be checked without any combat.
    /// </summary>
    public class PartyDebugHarness : MonoBehaviour
    {
        [SerializeField] private AllyParty party = new();
        [SerializeField] private EnemySlotData[] enemies;

        [Header("--- Optional test item (added to member 0's snapshot in code) ---")]
        [SerializeField] private ItemSO testItem;
        [SerializeField] private int testItemQuantity = 1;

        private void Start()
        {
            if (testItem != null && party.Members.Count > 0)
                party.Members[0].inventorySnapshot[testItem.itemID] = testItemQuantity;

            foreach (PartyMemberData member in party.Members)
            {
                Unit unit = Instantiate(member.definition.unitPrefab, transform).GetComponent<Unit>();
                unit.InitializeFromParty(member);
                LogUnit(unit);
            }

            foreach (EnemySlotData slot in enemies)
            {
                Unit unit = Instantiate(slot.definition.unitPrefab, transform).GetComponent<Unit>();
                unit.InitializeFresh(slot.definition, slot.inventorySnapshot);
                LogUnit(unit);
            }
        }

        private static void LogUnit(Unit unit)
        {
            Debug.Log($"[PartyDebug] {unit.Name} · {unit.Team} · HP {unit.HP}/{unit.MaxHP} · SP {unit.SP}/{unit.MaxSP}" +
                      $" · Str {unit.Strength} · Speed {unit.Speed} · alive: {unit.IsAlive}");
        }
    }
}