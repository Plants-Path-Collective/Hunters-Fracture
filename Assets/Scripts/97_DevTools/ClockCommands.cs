#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using DevTools;
using UnityEngine.Scripting;
using Core.Clock;

namespace Core
{
    /// <summary>
    /// Development console commands to inspect and manipulate the <see cref="GameClock"/>.
    /// Excluded from production builds.
    /// </summary>
    [Preserve]
    internal static class ClockCommands
    {
        private const string NoClock = "GameClock is not available in the current scene setup.";

        [ConsoleCommand("clock.status", "Shows the current day, phase, progress and active pauses.")]
        private static string Status()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            return $"Day {clock.Day} | {clock.CurrentPhase} {clock.PhaseProgress:P0} | " +
                   $"running: {clock.IsRunning} | paused: {clock.IsPaused} | dayOver: {clock.IsDayOver} | " +
                   $"speed x{clock.SpeedMultiplier:0.##}";
        }

        [ConsoleCommand("clock.phase", "Shows or jumps to a phase: Morning, Afternoon or Night.")]
        private static string Phase(string phase = "")
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            if (string.IsNullOrWhiteSpace(phase))
                return $"Current phase: {clock.CurrentPhase}";

            if (!Enum.TryParse(phase, true, out DAY_PHASE parsed) || !Enum.IsDefined(typeof(DAY_PHASE), parsed) || parsed == DAY_PHASE.None)
                return $"Unknown phase '{phase}'. Use Morning, Afternoon or Night.";

            clock.JumpTo(parsed);
            return $"Jumped to {clock.CurrentPhase}.";
        }

        [ConsoleCommand("clock.pause", "Pauses the clock with the Debug reason.")]
        private static string PauseClock()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            if (clock.IsPausedBy(CLOCK_PAUSE_REASON.Debug)) return "Clock is already paused by Debug.";

            clock.Pause(CLOCK_PAUSE_REASON.Debug);
            return "Clock paused (Debug).";
        }

        [ConsoleCommand("clock.resume", "Removes the Debug pause. Other pause reasons stay active.")]
        private static string ResumeClock()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            clock.Resume(CLOCK_PAUSE_REASON.Debug);
            return clock.IsPaused
                ? "Debug pause removed, but other pause reasons are still active."
                : "Clock resumed.";
        }

        [ConsoleCommand("clock.speed", "Shows or sets the clock speed multiplier (1 = normal).")]
        private static string Speed(float factor = -1f)
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            if (factor < 0f) return $"Clock speed: x{clock.SpeedMultiplier:0.##}";

            clock.SpeedMultiplier = factor;
            return $"Clock speed set to x{clock.SpeedMultiplier:0.##}.";
        }

        [ConsoleCommand("clock.reset", "Resets the clock to day 1 and the configured start phase.")]
        private static string Reset()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            clock.ResetClock();
            return $"Clock reset: day {clock.Day}, {clock.CurrentPhase}.";
        }

        [ConsoleCommand("day.end", "Ends the day immediately (debug; the run manager will own this later).")]
        private static string EndDay()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null) return NoClock;

            clock.ForceEndDay();
            return "Day ended.";
        }
    }
}
#endif