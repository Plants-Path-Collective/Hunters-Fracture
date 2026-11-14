#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Core;
using Core.Clock;
using UnityEngine.Scripting;

namespace DevTools
{
    /// <summary>
    /// Console commands to test the run lifecycle without waiting for the clock.
    /// </summary>
    [Preserve]
    internal static class RunCommands
    {
        [ConsoleCommand("run.end", "Ends the current day with the given reason (TimeOut, GoToSleep, GameOver). Default: TimeOut.")]
        private static string End(END_CYCLE_REASONS reason = END_CYCLE_REASONS.TimeOut)
        {
            if (!TryGetManager(out RunManager manager, out string error)) return error;
            if (manager.IsTransitioning) return "A cycle transition is already in progress.";

            manager.EndDay(reason);
            return $"Ending day (reason: {reason}).";
        }

        [ConsoleCommand("run.confirm", "Confirms the end-of-day screen and starts the next run.")]
        private static string Confirm()
        {
            if (!TryGetManager(out RunManager manager, out string error)) return error;
            if (!manager.IsTransitioning) return "No end-of-day transition is waiting for confirmation.";

            manager.ConfirmEndOfDay();
            return "Confirmed, resetting run.";
        }

        [ConsoleCommand("run.start", "Starts a fresh run without an end-of-day screen.")]
        private static string Start()
        {
            if (!TryGetManager(out RunManager manager, out string error)) return error;
            if (manager.IsTransitioning) return "A cycle transition is already in progress.";

            manager.StartRun();
            return "Starting a new run.";
        }

        [ConsoleCommand("run.status", "Shows the run transition state, last end reason and clock state.")]
        private static string Status()
        {
            if (!TryGetManager(out RunManager manager, out string error)) return error;

            string clock = GameClock.Instance != null
                ? $"day {GameClock.Instance.Day}, phase {GameClock.Instance.CurrentPhase}"
                : "no GameClock";

            return $"Transitioning: {manager.IsTransitioning} | Last reason: {manager.LastEndReason} | Clock: {clock}";
        }

        [ConsoleCommand("day.end", "Alias of run.end: ends the day with TimeOut.")]
        private static string DayEnd() => End();

        private static bool TryGetManager(out RunManager manager, out string error)
        {
            manager = RunManager.Instance;
            error = manager == null ? "RunManager is not available. Start from the boot scene." : null;
            return manager != null;
        }
    }
}
#endif