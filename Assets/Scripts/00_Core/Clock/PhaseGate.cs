using UnityEngine;
using UnityEngine.Localization;

namespace Core.Clock
{
    /// <summary>
    /// Pure rule component: answers whether something (a door, a zone) is open in the current day
    /// phase, and which message explains why it is not. It has no input or scene logic, so any
    /// interactable can query it.
    /// </summary>
    public class PhaseGate : MonoBehaviour
    {
        [Header("Rules")]
        [Tooltip("Phases in which this gate is open. Combine phases to allow more than one.")]
        [SerializeField] private DAY_PHASE allowedPhases = DAY_PHASE.Afternoon;

        [Header("Messages")]
        [Tooltip("Shown when the current phase is earlier than every allowed phase (e.g. the dungeon in the morning).")]
        [SerializeField] private LocalizedString notYetOpenMessage;

        [Tooltip("Shown when the current phase is later than the allowed phases (e.g. the dungeon at night).")]
        [SerializeField] private LocalizedString closedMessage;

        /// <summary>
        /// True if the gate is open right now. Also true when no <see cref="GameClock"/> exists,
        /// so test scenes without the persistent objects keep working.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                GameClock clock = GameClock.Instance;
                bool isOpen = clock == null || clock.IsIn(allowedPhases);
                string phase = clock != null ? clock.CurrentPhase.ToString() : "no GameClock (open by default)";
                Debug.Log(
                    $"[PhaseGate] '{name}' evaluated: phase={phase}, allowed={allowedPhases}, open={isOpen}.",
                    this);
                return isOpen;
            }
        }

        /// <summary>Returns the message that explains why the gate is closed in the current phase.</summary>
        public LocalizedString GetClosedMessage()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null || allowedPhases == DAY_PHASE.None)
            {
                Debug.Log($"[PhaseGate] '{name}' using the configured closed message.", this);
                return closedMessage;
            }

            // Phases are ordered by flag value (Morning < Afternoon < Night): isolate the lowest allowed one.
            int allowed = (int)allowedPhases;
            int lowestAllowed = allowed & -allowed;

            bool notYetOpen = (int)clock.CurrentPhase < lowestAllowed;
            Debug.Log(
                $"[PhaseGate] '{name}' selected {(notYetOpen ? "not-yet-open" : "closed")} message " +
                $"for phase {clock.CurrentPhase}.", this);
            return notYetOpen ? notYetOpenMessage : closedMessage;
        }
    }
}