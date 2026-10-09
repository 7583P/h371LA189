using System;

namespace AnimeHillRacing.Racing3D.ML
{
    // Scheduled from the first active physics step, not object creation before
    // the countdown. Absolute deadlines retain each car's phase after rounding
    // to physics steps; late frames skip missed deadlines instead of bursting.
    public struct RacingInferenceSchedule
    {
        public const int PhaseCount = 11;
        bool initialized, hasTick;
        double next, lastObserved, lastTick, period;

        public void Reset() { initialized = hasTick = false; }

        public bool TryTick(double now, double interval, int phase, out float elapsed, out bool discontinuity)
        {
            elapsed = 0; discontinuity = false;
            if (double.IsNaN(now) || double.IsInfinity(now) || double.IsNaN(interval) || double.IsInfinity(interval) || interval <= 0)
            { discontinuity = initialized; Reset(); return false; }
            if (!initialized || now < lastObserved || now - lastObserved > .3 || interval != period)
            {
                discontinuity = initialized;
                initialized = true; hasTick = false; period = interval;
                int slot = (phase % PhaseCount + PhaseCount) % PhaseCount;
                next = now + slot * interval / PhaseCount;
                lastTick = now;
            }
            lastObserved = now;
            double tolerance = interval * .0001;
            if (now + tolerance < next) return false;
            elapsed = hasTick ? (float)(now - lastTick) : (float)interval;
            lastTick = now; hasTick = true;
            next += Math.Max(1, Math.Floor((now - next + tolerance) / interval) + 1) * interval;
            return true;
        }
    }
}
