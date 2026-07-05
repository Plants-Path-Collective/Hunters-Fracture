using Core;
using CombatSystem.UnitSystem;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Singleton that survives scene loads.
    /// The overworld writes the two parties here before loading CombatStage.
    /// CombatStage reads from here to spawn units.
    /// After combat, the result is written back so the overworld can react.
    ///
    /// Lives on the same persistent GameManager GameObject as InputManager,
    /// or on its own DontDestroyOnLoad object — your call.
    /// </summary>
    public class CombatContext : MonoBehaviour
    {
        public static CombatContext Instance { get; private set; }

        // ── Incoming data (written by CombatTrigger) ──────────────────────────
        public PartySO AllyParty   { get; private set; }
        public PartySO EnemyParty  { get; private set; }

        // ── Outgoing data (written by CombatManager) ──────────────────────────
        public COMBAT_RESULT LastResult { get; private set; } = COMBAT_RESULT.None;

        // ── Name of the scene to return to after combat ───────────────────────
        public string ReturnScene { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called by CombatTrigger before loading CombatStage.
        /// </summary>
        public void SetCombat(PartySO allyParty, PartySO enemyParty, string returnScene)
        {
            AllyParty   = allyParty;
            EnemyParty  = enemyParty;
            ReturnScene = returnScene;
            LastResult  = COMBAT_RESULT.None;
        }

        /// <summary>
        /// Called by CombatManager when the encounter ends.
        /// </summary>
        public void SetResult(COMBAT_RESULT result)
        {
            LastResult = result;
        }
    }
}
