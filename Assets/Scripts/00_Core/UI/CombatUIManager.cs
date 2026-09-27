using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Core.UI
{
    public class CombatUIManager : MonoBehaviour
    {
        public static CombatUIManager Instance;

        [Header("--- Panels ---")]
        [SerializeField] private GameObject combatUIPanel;

        [Header("--- Actions Buttons ---")]
        [SerializeField] private GameObject attackButton;
        [SerializeField] private GameObject defendButton;
        [SerializeField] private GameObject skillsButton;
        [SerializeField] private GameObject backpackButton;
        [SerializeField] private GameObject fleeButton;

        [Header("--- Combat Extras ---")]
        [SerializeField] private GameObject cursor;

        private void Awake()
        {
            // Local singleton, scoped to the Combat scene only (not persisted across scenes)
            if (Instance != null && Instance != this) { Destroy(this); return; }

            Instance = this;
        }

        /// <summary>
        /// Opens the Combat UI panel by enabling it.
        /// Hook this up to the "Start Combat" button or event.
        /// </summary>
        public void OpenCombatUI()
        {
            if (combatUIPanel != null) combatUIPanel.SetActive(true);
        }

        /// <summary>
        /// Closes the Combat UI panel by disabling it.
        /// Hook this up to the "End Combat" button or event.
        /// </summary>
        public void CloseCombatUI()
        {
            if (combatUIPanel != null) combatUIPanel.SetActive(false);
        }
    }
}