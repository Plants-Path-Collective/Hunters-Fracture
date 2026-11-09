using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Clock
{
    /// <summary>
    /// Minimal HUD for the game clock: shows the current phase name and fills an image with the
    /// progress inside that phase. It polls the clock instead of listening to events, so it stays
    /// correct after phase jumps, scene loads and console tests.
    /// </summary>
    public class ClockHud : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Text that shows the current phase name.")]
        [SerializeField] private TMP_Text phaseLabel;

        [Tooltip("Optional image (Image Type: Filled) that shows the progress inside the current phase.")]
        [SerializeField] private Image progressFill;

        [Header("Behaviour")]
        [Tooltip("Hide the whole HUD while no GameClock exists (e.g. test scenes without the persistent objects).")]
        [SerializeField] private bool hideWithoutClock = true;

        private DAY_PHASE shownPhase = DAY_PHASE.None;

        private void LateUpdate()
        {
            GameClock clock = GameClock.Instance;

            if (clock == null)
            {
                if (hideWithoutClock) SetVisible(false);
                return;
            }

            SetVisible(true);

            // Only touch the text when the phase changes to avoid per-frame string allocations.
            if (clock.CurrentPhase != shownPhase)
            {
                shownPhase = clock.CurrentPhase;
                if (phaseLabel != null) phaseLabel.text = shownPhase.ToString();
            }

            if (progressFill != null)
                progressFill.fillAmount = clock.PhaseProgress;
        }

        private void SetVisible(bool visible)
        {
            if (phaseLabel != null && phaseLabel.gameObject.activeSelf != visible)
                phaseLabel.gameObject.SetActive(visible);

            if (progressFill != null && progressFill.gameObject.activeSelf != visible)
                progressFill.gameObject.SetActive(visible);
        }
    }
}