using System.Collections.Generic;
using CombatSystem.UnitSystem;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Reads CombatContext at scene start and instantiates unit prefabs at
    /// the assigned spawn points.
    ///
    /// Assign 3 ally and 3 enemy spawn Transforms in the Inspector.
    /// Each CharacterSheetSO must reference a prefab that has a Unit component.
    /// After spawning, hands the populated lists to CombatManager.
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("Up to 3 positions where ally units will appear.")]
        [SerializeField] private Transform[] allySpawnPoints  = new Transform[3];

        [Tooltip("Up to 3 positions where enemy units will appear.")]
        [SerializeField] private Transform[] enemySpawnPoints = new Transform[3];

        [Header("Dependencies")]
        [SerializeField] private CombatManager combatManager;

        private void Start()
        {
            if (CombatContext.Instance == null)
            {
                Debug.LogError("[UnitSpawner] CombatContext not found. " +
                               "Make sure CombatTrigger set the context before loading this scene.");
                return;
            }

            List<Unit> allies  = SpawnParty(CombatContext.Instance.AllyParty,  allySpawnPoints);
            List<Unit> enemies = SpawnParty(CombatContext.Instance.EnemyParty, enemySpawnPoints);

            combatManager.Initialize(allies, enemies);
        }

        private List<Unit> SpawnParty(PartySO party, Transform[] spawnPoints)
        {
            var units = new List<Unit>();

            if (party == null) return units;

            for (int i = 0; i < party.members.Length && i < spawnPoints.Length; i++)
            {
                CharacterSheetSO sheet = party.members[i];
                if (sheet == null) continue;
                if (sheet.combatPrefab == null)
                {
                    Debug.LogWarning($"[UnitSpawner] {sheet.characterName} has no combatPrefab assigned.");
                    continue;
                }

                GameObject go   = Instantiate(sheet.combatPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
                Unit       unit = go.GetComponent<Unit>();

                if (unit == null)
                {
                    Debug.LogError($"[UnitSpawner] Prefab for {sheet.characterName} is missing a Unit component.");
                    continue;
                }

                unit.InitFromSheet(sheet);
                units.Add(unit);
            }

            return units;
        }
    }
}
