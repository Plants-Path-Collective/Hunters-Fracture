using System.Text;
using UnityEngine;
using Core.CombatSystem.Units;
using Overworld;

namespace ExtendedDebug
{
    /// <summary>
    /// Debug helper to test roster operations without a management UI.
    /// Use the component context menu in Play mode.
    /// </summary>
    public class RosterDebugHarness : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player whose party is edited. Defaults to the PlayerEntity on this GameObject.")]
        [SerializeField] private PlayerEntity player;

        [Header("Test values")]
        [Tooltip("First index used by Swap / Move / Remove.")]
        [SerializeField, Min(0)] private int indexA;

        [Tooltip("Second index used by Swap / Move.")]
        [SerializeField, Min(0)] private int indexB = 1;

        [Tooltip("Character added by 'Add Unit'.")]
        [SerializeField] private UnitDefinitionSO unitToAdd;

        private AllyParty Party => player != null ? player.Party : null;

        private void Awake()
        {
            if (player == null) TryGetComponent(out player);
        }

        [ContextMenu("Log Party")]
        private void LogParty()
        {
            if (Party == null) return;

            var sb = new StringBuilder("[Party] Roster:\n");
            for (int i = 0; i < Party.Count; i++)
            {
                PartyMemberData member = Party.Members[i];
                string role = Party.IsActive(i) ? "ACTIVE" : "BENCH";
                sb.AppendLine($"  {i} [{role}] {member.definition.unitName} (HP {member.currentHP}, SP {member.currentSP})");
            }
            Debug.Log(sb.ToString());
        }

        [ContextMenu("Swap A <-> B")]
        private void Swap() { if (Party != null) Debug.Log($"[Party] Swap: {Party.TrySwap(indexA, indexB)}"); }

        [ContextMenu("Move A -> B")]
        private void Move() { if (Party != null) Debug.Log($"[Party] Move: {Party.TryMove(indexA, indexB)}"); }

        [ContextMenu("Remove A")]
        private void Remove() { if (Party != null) Debug.Log($"[Party] Remove: {Party.TryRemoveAt(indexA)}"); }

        [ContextMenu("Add Unit")]
        private void Add()
        {
            if (Party == null || unitToAdd == null) return;
            Debug.Log($"[Party] Add: {Party.TryAdd(new PartyMemberData { definition = unitToAdd })}");
        }
    }
}