using System;
using System.Diagnostics;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static partial class Program
    {
        public static int Main(string[] args)
        {
            try{return Run(args);}catch(Exception exception){Console.Error.WriteLine(exception);return 1;}
        }
        static int Run(string[] args)
        {
            string mode = args.Length > 0 ? args[0] : "smoke";
            string output = args.Length > 1 ? args[1] : System.IO.Path.GetFullPath("../../Logs/CPPS/20261016/qa");
            System.IO.Directory.CreateDirectory(output);
            switch (mode)
            {
                case "smoke": return Smoke();
                case "bench": return Bench.Run();
                case "contracts": return ContractSuite.Run(output);
                case "seasons": return SeasonExpansionContracts.Run(output);
                case "scale": return SeasonScaleBench.Run(output);
                case "mmr-lab": return MmrEconomyLab.Run(output,args);
                case "mmr-contracts": return MmrExperimentContracts.Run(output);
                case "sizes": Console.WriteLine($"bot {LocalLeagueStore.BytesPerBot} season {LocalLeagueStore.BytesPerSeasonBot}"); return 0;
                default: return Suites.Run(mode, output);
            }
        }

        static int Smoke()
        {
            var t = Stopwatch.StartNew();
            long now = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc).Ticks;
            var state = LocalLeagueEngine.Create(now, 4207);
            Console.WriteLine($"create {t.ElapsedMilliseconds} ms, bytes/bot {LocalLeagueStore.BytesPerBot}");
            var c = new CompetitiveCoordinator(state, new LocalLeagueConfiguration());
            state.lastKnownUtcTimestamp = now + TimeSpan.TicksPerDay;
            t.Restart();
            c.RunToCompletion(null);
            Console.WriteLine($"1 day: {c.Engine.Simulator.CompletedRaces} races, {c.Engine.Simulator.ProcessedEvents} events, {t.ElapsedMilliseconds} ms, ranked {c.Engine.RankedCount()}");
            Console.WriteLine(c.Engine.Simulator.ValidateInvariants(out var f) ? "invariants ok" : f);
            return 0;
        }
    }

    public static partial class Suites
    {
        public static int Run(string mode, string output)
        {
            var parts = mode.Split(':');
            switch (parts[0])
            {
                case "sim": return SimulationSuite.Run(output, int.Parse(parts[1]), parts.Length > 2 ? int.Parse(parts[2]) : 4207, parts.Length > 3 ? parts[3] : parts[1] + "d",parts.Length>4?int.Parse(parts[4]):10000,parts.Length>5?float.Parse(parts[5],System.Globalization.CultureInfo.InvariantCulture):1100);
                default: Console.WriteLine("unknown mode " + mode); return 2;
            }
        }
    }
}
