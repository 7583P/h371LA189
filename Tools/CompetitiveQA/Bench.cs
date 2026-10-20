using System;
using System.Diagnostics;
using AnimeHillRacing.Racing3D;
namespace CompetitiveQA
{
    public static class Bench
    {
        public static int Run()
        {
            long t0 = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc).Ticks; double sink = 0;
            var w = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++) sink += LeagueActivityModel.Intensity(4207, t0 + i * 600000000L, -300, 30);
            Console.WriteLine($"Intensity: {w.Elapsed.TotalMilliseconds * 1000 / 1000000:F3} us/call");
            var p = new RatingParticipant[12]; var o = new RatingOutcome[12]; var sc = new double[12]; var cfg = new LocalLeagueConfiguration();
            for (int k = 0; k < 12; k++) p[k] = new RatingParticipant { rating = 2000 + k * 100, ranked = true, placementGames = 3, uncertainty = .3f, position = k + 1 };
            w.Restart(); for (int i = 0; i < 100000; i++) CompetitiveRating.Settle(p, cfg, o, sc);
            Console.WriteLine($"Settle: {w.Elapsed.TotalMilliseconds * 1000 / 100000:F3} us/call {sink > 0}");
            return 0;
        }
    }
}
