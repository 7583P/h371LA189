using System;

namespace AnimeHillRacing.Racing3D
{
    // Availability is separate from racing frequency. Distributed home zones
    // and long idle availability windows never create wins, MMR or auto races.
    // The original session-arrival process still decides actual bot races.
    public static class ExpandedCommunitySchedule
    {
        public const byte PassiveFlag = 4;
        public static int HomeOffset(CompactLeagueBot b,int leagueOffset=-300) => (uint)b.personalitySeed%5==0?leagueOffset:((int)((uint)b.personalitySeed % 24) - 12) * 60;
        public static bool Available(int seed, CompactLeagueBot b, long utc, out long nextChange,int leagueOffset=-300)
        {
            long offset = HomeOffset(b,leagueOffset) * TimeSpan.TicksPerMinute;
            long day = (utc + offset) / TimeSpan.TicksPerDay;
            bool online = false; nextChange = long.MaxValue;
            for (long localDay = day - 1; localDay <= day + 1; localDay++)
            {
                var rng = new LeagueRandom(LeagueSimulator.Mix(seed ^ b.personalitySeed, localDay, 0x57414b45));
                double wakeHour = 6 + b.chronotypeMinutes / 60.0 + rng.Unit() * 1.2;
                double availableHours = 16.2 + rng.Unit() * 1.4;
                long start = localDay * TimeSpan.TicksPerDay + (long)(wakeHour * TimeSpan.TicksPerHour) - offset;
                long end = start + (long)(availableHours * TimeSpan.TicksPerHour);
                if (utc >= start && utc < end) online = true;
                if (start > utc) nextChange = Math.Min(nextChange, start);
                if (end > utc) nextChange = Math.Min(nextChange, end);
            }
            return online;
        }
    }
}
