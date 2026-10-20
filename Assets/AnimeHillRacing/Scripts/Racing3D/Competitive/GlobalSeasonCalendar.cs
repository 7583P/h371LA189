using System;

namespace AnimeHillRacing.Racing3D
{
    // Global Racing 3D season calendar. Every device resolves the same season
    // number from the same UTC instant: installation, first launch and first
    // ranked race never participate. Season n starts at Epoch + 3(n-1) calendar
    // months (real 90/91/92-day quarters, leap years included).
    //
    // Authoritative production epoch, confirmed by the owner on 2026-10-18:
    // 2026-10-01T00:00:00Z. Never derive this from installation or app version.
    public static class GlobalSeasonCalendar
    {
        public const int ContractVersion = 1;
        public static readonly long EpochTicks = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        public const int MaximumSeason = 4000; // year 3026; bounds every calendar computation

        public static long Start(int season)
        {
            if (season < 1 || season > MaximumSeason) throw new ArgumentOutOfRangeException(nameof(season));
            return new DateTime(EpochTicks, DateTimeKind.Utc).AddMonths(checked((season - 1) * 3)).Ticks;
        }

        public static long End(int season) => Start(season + 1);

        // Instants before the epoch belong to season 1 (pre-season clocks are
        // never allowed to create season 0 or negative history).
        public static int SeasonAt(long utcTicks)
        {
            if (utcTicks <= EpochTicks) return 1;
            var epoch = new DateTime(EpochTicks, DateTimeKind.Utc);
            var at = new DateTime(Math.Min(utcTicks, DateTime.MaxValue.Ticks), DateTimeKind.Utc);
            int months = (at.Year - epoch.Year) * 12 + at.Month - epoch.Month;
            int season = Math.Max(1, Math.Min(MaximumSeason, months / 3 + 1));
            // Exact boundary correction for any sub-month remainder.
            while (season > 1 && Start(season) > utcTicks) season--;
            while (season < MaximumSeason && End(season) <= utcTicks) season++;
            return season;
        }

        public static string TransitionId(int sourceSeason, int destinationSeason) =>
            "ST:" + sourceSeason + "->" + destinationSeason + ":c" + ContractVersion;

        public static string Label(int season)
        {
            var start = new DateTime(Start(season), DateTimeKind.Utc);
            return "SEASON " + season + " · " + start.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
        }

        public static TimeSpan Remaining(int season, long utcTicks) => TimeSpan.FromTicks(Math.Max(0, End(season) - utcTicks));
    }
}
