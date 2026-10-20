using System;

namespace AnimeHillRacing.Racing3D
{
    // Accepted league time. Offline there is no trusted time source, so:
    //  * Inside one foreground session, time advances only by monotonic
    //    elapsed time; wall-clock jumps beyond a tolerance are ignored.
    //  * Across sessions (relaunch / resume from background, where the
    //    monotonic clock may not run during deep sleep) a forward device
    //    clock is accepted (indistinguishable from real absence) and bounded
    //    by catch-up limits; a backward clock never moves accepted time back,
    //    so the league freezes until real time passes the high-water mark.
    //  * Human MMR never changes with time; time only advances bots and global
    //    seasons, each boundary at most once, so clock games cannot farm MMR.
    public sealed class CompetitiveClock
    {
        public const long InSessionToleranceTicks = 10 * TimeSpan.TicksPerMinute;
        public const long BackwardToleranceTicks = 2 * TimeSpan.TicksPerMinute;
        public static readonly long ImplausibleForwardTicks = 400 * TimeSpan.TicksPerDay;
        bool sessionOpen;
        long sessionUtc, sessionMonotonicMs, lastMonotonicMs;
        public bool Frozen { get; private set; }

        public void EndSession() => sessionOpen = false;

        public long Accept(LocalLeagueState s, long deviceUtc, long monotonicMs)
        {
            long accepted;
            if (!sessionOpen || monotonicMs < lastMonotonicMs)
            {
                if (deviceUtc < s.lastKnownUtcTimestamp - BackwardToleranceTicks)
                {
                    Record(s, "backward-clock-across-sessions", deviceUtc, s.lastKnownUtcTimestamp, monotonicMs, (s.lastKnownUtcTimestamp - deviceUtc) / TimeSpan.TicksPerSecond);
                    Frozen = true;
                }
                else Frozen = false;
                if (deviceUtc > s.lastKnownUtcTimestamp + ImplausibleForwardTicks)
                    Record(s, "implausible-forward-clock-across-sessions", deviceUtc, s.lastKnownUtcTimestamp, monotonicMs, (deviceUtc - s.lastKnownUtcTimestamp) / TimeSpan.TicksPerSecond);
                accepted = Math.Max(deviceUtc, s.lastKnownUtcTimestamp);
                sessionOpen = true; sessionUtc = accepted; sessionMonotonicMs = monotonicMs;
            }
            else
            {
                long expected = sessionUtc + (monotonicMs - sessionMonotonicMs) * TimeSpan.TicksPerMillisecond;
                long drift = deviceUtc - expected;
                if (Math.Abs(drift) > InSessionToleranceTicks)
                {
                    Record(s, drift > 0 ? "forward-clock-jump-in-session" : "backward-clock-jump-in-session", deviceUtc, expected, monotonicMs, drift / TimeSpan.TicksPerSecond);
                    accepted = expected;
                    // Re-anchor so a single manipulation is recorded once.
                    sessionUtc = expected; sessionMonotonicMs = monotonicMs;
                }
                else accepted = deviceUtc;
                if (Frozen && deviceUtc >= s.lastKnownUtcTimestamp) Frozen = false;
            }
            lastMonotonicMs = monotonicMs;
            accepted = Math.Max(accepted, s.lastKnownUtcTimestamp);
            s.lastKnownUtcTimestamp = Math.Max(s.installationTimestamp, accepted);
            s.highestAcceptedSeason = Math.Max(s.highestAcceptedSeason, GlobalSeasonCalendar.SeasonAt(s.lastKnownUtcTimestamp));
            return s.lastKnownUtcTimestamp;
        }

        static void Record(LocalLeagueState s, string kind, long observed, long accepted, long monotonicMs, long magnitudeSeconds)
        {
            s.ledger.Record(new ClockAnomaly { kind = kind, observedUtc = observed, acceptedUtc = accepted, monotonicMilliseconds = monotonicMs, magnitudeSeconds = magnitudeSeconds });
        }
    }
}
