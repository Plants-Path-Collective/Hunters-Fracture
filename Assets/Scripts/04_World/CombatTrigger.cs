using CombatSystem.UnitSystem;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CombatSystem
{
    /// <summary>
    /// Place on an enemy GameObject in the overworld.
    /// When the player enters the trigger collider, this component:
    ///   1. Writes both parties into CombatContext.
    ///   2. Disables all input so the player can't move during the transition.
    ///   3. Loads the CombatStage scene.
    ///
    /// Requires a Collider set to Is Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CombatTrigger : MonoBehaviour
    {
        [Header("Party Data")]
        [Tooltip("The enemy party for this encounter.")]
        [SerializeField] private PartySO enemyParty;

        [Header("Scene")]
        [Tooltip("Exact name of the combat scene to load.")]
        [SerializeField] private string combatScene = "CombatStage";

        [Header("Player Detection")]
        [SerializeField] private string playerTag = "Player";

        // Guard so multiple simultaneous triggers don't fire twice
        private bool _triggered = false;

        private void Awake()
        {
            // Ensure the collider is a trigger
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag(playerTag)) return;

            _triggered = true;
            StartCombat();
        }

        private void StartCombat()
        {
            // Retrieve the player's party from PartyHolder on the player
            PartySO allyParty = FindAllyParty();
            if (allyParty == null)
            {
                Debug.LogError("[CombatTrigger] Could not find ally PartySO. " +
                               "Make sure the player GameObject has a PartyHolder component.");
                _triggered = false;
                return;
            }

            if (enemyParty == null)
            {
                Debug.LogError($"[CombatTrigger] '{gameObject.name}' has no enemy PartySO assigned.");
                _triggered = false;
                return;
            }

            // Lock input during scene transition
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty);

            // Store which scene to return to after combat
            string returnScene = SceneManager.GetActiveScene().name;
            CombatContext.Instance.SetCombat(allyParty, enemyParty, returnScene);

            SceneManager.LoadScene(combatScene);
        }

        private static PartySO FindAllyParty()
        {
            // PartyHolder lives on the player or on a persistent manager
            PartyHolder holder = FindFirstObjectByType<PartyHolder>();
            return holder != null ? holder.party : null;
        }
    }
}
