using System;

namespace AnimeHillRacing.Racing3D
{
    public enum BotPresence : byte { Offline = 0, OnlineIdle = 1, InQueue = 2, InMatch = 3, Cooldown = 4 }
    public enum BotActivityProfile : byte { Casual = 0, Regular = 1, Grinder = 2, Pro = 3 }

    public struct ActivityProfileParameters
    {
        public double sessionsPerDay, medianRaces, racesSpread, minimumOfflineMinutes, queuePatienceSeconds, idleSeconds, cooldownSeconds;
    }

    // Continuous, organic activity. There is no hourly race target anywhere:
    // the league's volume emerges from individual bots starting sessions
    // (inhomogeneous Poisson arrivals with this intensity), queueing, racing
    // for the measured race duration, cooling down and going offline.
    //
    //   intensity_b(t) = sessionsPerDay_p * habit_b
    //                    * daily(localHour(t) - chronotype_b; day parameters)
    //                    * weekday(t) * season(t) / mean(daily)
    //
    // Every day parameter (amplitude, night depth, peak shift, density) is
    // seeded per local day and smoothly interpolated between day centres, so
    // 05:59 and 06:00 are almost identical and two days are never identical.
    public static class LeagueActivityModel
    {
        // Two-hour control points of the baseline local-time curve, from 00:00.
        static readonly double[] Knots = { .58, .32, .17, .19, .36, .52, .63, .66, .75, .90, 1.00, .85 };
        public const double CurveMean = .5775;
        public const double MaximumIntensityFactor = 2.8; // upper bound of Intensity() used by thinning

        public static ActivityProfileParameters Parameters(BotActivityProfile profile)
        {
            switch (profile)
            {
                case BotActivityProfile.Casual: return new ActivityProfileParameters { sessionsPerDay = 1.0, medianRaces = 2, racesSpread = .55, minimumOfflineMinutes = 90, queuePatienceSeconds = 150, idleSeconds = 40, cooldownSeconds = 45 };
                case BotActivityProfile.Regular: return new ActivityProfileParameters { sessionsPerDay = 2.4, medianRaces = 3.5, racesSpread = .5, minimumOfflineMinutes = 60, queuePatienceSeconds = 210, idleSeconds = 30, cooldownSeconds = 35 };
                case BotActivityProfile.Grinder: return new ActivityProfileParameters { sessionsPerDay = 3.6, medianRaces = 8, racesSpread = .45, minimumOfflineMinutes = 35, queuePatienceSeconds = 300, idleSeconds = 20, cooldownSeconds = 25 };
                default: return new ActivityProfileParameters { sessionsPerDay = 4.0, medianRaces = 6, racesSpread = .4, minimumOfflineMinutes = 45, queuePatienceSeconds = 360, idleSeconds = 18, cooldownSeconds = 22 };
            }
        }

        // Periodic Catmull-Rom spline over the 12 control points (C1 continuous).
        public static double BaseCurve(double hour)
        {
            double h = hour % 24; if (h < 0) h += 24;
            double x = h / 2; int i1 = (int)Math.Floor(x); double t = x - i1;
            double p0 = Knots[(i1 + 11) % 12], p1 = Knots[i1 % 12], p2 = Knots[(i1 + 1) % 12], p3 = Knots[(i1 + 2) % 12];
            double t2 = t * t, t3 = t2 * t;
            return .5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
        }

        public struct DayParameters { public double amplitude, nightDepth, peakShift, density, weekday; }

        static double Hash01(uint seed, long day, uint salt)
        {
            ulong x = unchecked((ulong)day * 0x9E3779B97F4A7C15UL ^ ((ulong)seed << 17) ^ salt * 0xBF58476D1CE4E5B9UL);
            x ^= x >> 30; x *= 0xBF58476D1CE4E5B9UL; x ^= x >> 27; x *= 0x94D049BB133111EBUL; x ^= x >> 31;
            return (x >> 11) * (1.0 / 9007199254740992.0);
        }

        static DayParameters Day(uint seed, long localDay)
        {
            var dow = (DayOfWeek)(int)(((localDay % 7) + 7 + 4) % 7); // 1970-01-01 was a Thursday
            double weekday = dow == DayOfWeek.Saturday ? 1.12 : dow == DayOfWeek.Sunday ? 1.09 : dow == DayOfWeek.Friday ? 1.05 : dow == DayOfWeek.Monday ? .96 : 1.0;
            return new DayParameters {
                amplitude = .86 + .28 * Hash01(seed, localDay, 1),
                nightDepth = .72 + .6 * Hash01(seed, localDay, 2),
                peakShift = -1.4 + 2.8 * Hash01(seed, localDay, 3),
                density = .9 + .2 * Hash01(seed, localDay, 4),
                weekday = weekday
            };
        }

        static double Smooth(double t) => t * t * (3 - 2 * t);

        // Day parameters interpolated between neighbouring local-noon centres.
        public static DayParameters Interpolated(uint seed, double localDays)
        {
            double centre = localDays - .5; long d0 = (long)Math.Floor(centre); double t = Smooth(centre - d0);
            var a = Day(seed, d0); var b = Day(seed, d0 + 1);
            return new DayParameters {
                amplitude = a.amplitude + (b.amplitude - a.amplitude) * t, nightDepth = a.nightDepth + (b.nightDepth - a.nightDepth) * t,
                peakShift = a.peakShift + (b.peakShift - a.peakShift) * t, density = a.density + (b.density - a.density) * t,
                weekday = a.weekday + (b.weekday - a.weekday) * t };
        }

        [ThreadStatic] static long cachedSeasonStart, cachedSeasonEnd;
        public static double SeasonModifier(long utcTicks)
        {
            if (utcTicks < cachedSeasonStart || utcTicks >= cachedSeasonEnd || cachedSeasonEnd == 0)
            {
                int season = GlobalSeasonCalendar.SeasonAt(utcTicks);
                cachedSeasonStart = utcTicks <= GlobalSeasonCalendar.EpochTicks ? long.MinValue : GlobalSeasonCalendar.Start(season);
                cachedSeasonEnd = GlobalSeasonCalendar.End(season);
            }
            long seasonStart = cachedSeasonStart == long.MinValue ? GlobalSeasonCalendar.EpochTicks : cachedSeasonStart;
            double sinceStart = (utcTicks - seasonStart) / (double)TimeSpan.TicksPerDay;
            double untilEnd = (cachedSeasonEnd - utcTicks) / (double)TimeSpan.TicksPerDay;
            double opening = .16 * Math.Exp(-Math.Max(0, sinceStart) / 6.0);
            double finalPush = untilEnd < 7 ? .06 * Smooth(1 - Math.Max(0, untilEnd) / 7.0) : 0;
            return 1 + opening + finalPush;
        }

        // Normalised league intensity (mean ~1 over a typical day), before bot habits.
        public static double Intensity(uint seed, long utcTicks, int utcOffsetMinutes, int chronotypeMinutes)
        {
            double localDays = (utcTicks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks + (long)utcOffsetMinutes * TimeSpan.TicksPerMinute) / (double)TimeSpan.TicksPerDay;
            var day = Interpolated(seed, localDays);
            double hour = (localDays - Math.Floor(localDays)) * 24 - day.peakShift - chronotypeMinutes / 60.0;
            double curve = BaseCurve(hour);
            // Night depth scales how far quiet hours fall below the mean.
            if (curve < CurveMean) curve = CurveMean - (CurveMean - curve) * Math.Min(1.25, day.nightDepth);
            curve = CurveMean + (curve - CurveMean) * day.amplitude;
            curve = Math.Max(.04, curve);
            double weekdayDaytime = 1 + (day.weekday - 1) * Smooth(Math.Max(0, Math.Min(1, (BaseCurve(hour) - .3) / .5)));
            return curve / CurveMean * day.density * weekdayDaytime * SeasonModifier(utcTicks);
        }

        public static BotActivityProfile RollProfile(ref LeagueRandom rng, double skill)
        {
            // Skill nudges, never determines, how much a bot plays.
            double pro = .03 + .12 * skill * skill, grinder = .11, regular = .36;
            double roll = rng.Unit();
            if (roll < pro) return BotActivityProfile.Pro;
            if (roll < pro + grinder) return BotActivityProfile.Grinder;
            if (roll < pro + grinder + regular) return BotActivityProfile.Regular;
            return BotActivityProfile.Casual;
        }
    }
}
