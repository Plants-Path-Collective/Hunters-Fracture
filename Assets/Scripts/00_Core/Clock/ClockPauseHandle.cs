namespace Core.Clock
{
    /// <summary>
    /// Holds at most one pause of a given reason on the <see cref="GameClock"/>. Open and close paths
    /// can call <see cref="Set"/> as many times as they want without stacking pauses or releasing one
    /// that was never taken, which is the usual way a pause ends up stuck.
    /// </summary>
    public sealed class ClockPauseHandle
    {
        private readonly CLOCK_PAUSE_REASON reason;
        private bool isHeld;

        /// <param name="reason">Pause reason this handle takes and releases.</param>
        public ClockPauseHandle(CLOCK_PAUSE_REASON reason)
        {
            this.reason = reason;
        }

        /// <summary>True while this handle is holding a pause.</summary>
        public bool IsHeld => isHeld;

        /// <summary>
        /// Takes or releases the pause. Repeated calls with the same value do nothing. Null-safe when
        /// there is no clock in the scene setup.
        /// </summary>
        public void Set(bool paused)
        {
            if (paused == isHeld) return;
            isHeld = paused;

            if (paused) GameClock.RequestPause(reason);
            else GameClock.ReleasePause(reason);
        }
    }
}