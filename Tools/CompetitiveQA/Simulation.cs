using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public sealed class RaceObserver : ILeagueSimulationObserver
    {
        public long races, promotions, demotions, placementsCompleted, perRaceNetNonZero;
        public long maxAbsNet;
        public readonly List<long> raceTimes = new List<long>();
        public int maxWinStreak, maxLossStreak;
        public readonly Dictionary<int, int> lastTier = new Dictionary<int, int>();
        public long reservations;
        public void RaceCompleted(long t, long seq, int[] participants, RatingParticipant[] before, RatingOutcome[] after, float[] seconds)
        {
            races++; raceTimes.Add(t);
            long net = 0;
            for (int k = 0; k < participants.Length; k++)
            {
                if (before[k].ranked)
                {
                    net += after[k].delta - after[k].floorInjection + after[k].ceilingRemoval;
                    int a = (int)MmrTierUtility.GetTier(before[k].rating), b = (int)MmrTierUtility.GetTier(after[k].newRating);
                    if (b > a) promotions++; else if (b < a) demotions++;
                }
                if (after[k].placementCompleted) placementsCompleted++;
            }
            if (net != 0) perRaceNetNonZero++;
            maxAbsNet = Math.Max(maxAbsNet, Math.Abs(net));
        }
        public void HumanReserved(long t, int[] bots) { reservations++; }
    }

    public static class Stats
    {
        public static double Percentile(IList<double> sorted, double p)
        {
            if (sorted.Count == 0) return double.NaN;
            double rank = p * (sorted.Count - 1); int lo = (int)Math.Floor(rank), hi = Math.Min(sorted.Count - 1, lo + 1);
            return sorted[lo] + (sorted[hi] - sorted[lo]) * (rank - lo);
        }
        public static double Pearson(double[] x, double[] y)
        {
            double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0, syy = 0;
            for (int i = 0; i < x.Length; i++) { sxy += (x[i] - mx) * (y[i] - my); sxx += (x[i] - mx) * (x[i] - mx); syy += (y[i] - my) * (y[i] - my); }
            return sxy / Math.Sqrt(sxx * syy);
        }
        public static double[] Ranks(double[] v)
        {
            var idx = Enumerable.Range(0, v.Length).OrderBy(i => v[i]).ToArray(); var r = new double[v.Length];
            for (int i = 0; i < idx.Length;) { int j = i; while (j + 1 < idx.Length && v[idx[j + 1]] == v[idx[i]]) j++; double avg = (i + j) / 2.0; for (int k = i; k <= j; k++) r[idx[k]] = avg; i = j + 1; }
            return r;
        }
        public static double Spearman(double[] x, double[] y) => Pearson(Ranks(x), Ranks(y));
        public static Dictionary<string, double> Describe(IEnumerable<double> values)
        {
            var s = values.OrderBy(v => v).ToList();
            if (s.Count == 0) return new Dictionary<string, double> { ["count"] = 0 };
            double mean = s.Average(), sd = Math.Sqrt(s.Sum(v => (v - mean) * (v - mean)) / s.Count);
            return new Dictionary<string, double> { ["count"] = s.Count, ["min"] = s[0], ["p5"] = Percentile(s, .05), ["p25"] = Percentile(s, .25), ["median"] = Percentile(s, .5),
                ["mean"] = mean, ["p75"] = Percentile(s, .75), ["p95"] = Percentile(s, .95), ["p99"] = Percentile(s, .99), ["max"] = s[s.Count - 1], ["sd"] = sd };
        }
    }

    public static class SimulationSuite
    {
        sealed class HourSample { public long utc; public int localHour, online, offline, queued, inMatch, cooldown, concurrentRaces, completed; }

        public static int Run(string output, int days, int seed, string label,int population=10000,float ratingScale=1100)
        {
            var report = new SortedDictionary<string, object>();
            long start = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc).Ticks;
            int utcOffset = -300; // America/Lima (UTC-5), the developer's locale; any offset is a parameter
            var config = new LocalLeagueConfiguration{ratingScale=ratingScale};
            var create = Stopwatch.StartNew();
            var state = LocalLeagueEngine.Create(start, seed, population, config); state.utcOffsetMinutes = utcOffset;state.expandedCommunityPresence=population>=100000;
            create.Stop();
            var observer = new RaceObserver();
            var history=new SeasonHistoryStore(Path.Combine(output,"history-"+label));
            var c = new CompetitiveCoordinator(state, config,finalizeSeason:(s,index)=>{history.Capture(s,index);return true;});
            c.Engine.Simulator.Observer = observer;
            var samples = new List<HourSample>();
            var presence = new int[5];
            var checkpoints = new[] { 1, 7, 30, 60, 90, 100, 180, 365 }.Where(d => d <= days).ToArray();
            var checkpointReports = new List<object>();
            var cpu = Stopwatch.StartNew();
            long cpuTicks = 0; long initialRating = 0; long seasonsProcessed = 0;
            List<int> previousTop10 = null, previousTop100 = null; var top10Turnover = new List<double>(); var top100Turnover = new List<double>();
            string invariantFailure = null;
            for (int hour = 1; hour <= days * 24; hour++)
            {
                long target = start + hour * TimeSpan.TicksPerHour;
                state.lastKnownUtcTimestamp = target;
                long before = observer.races;
                var w = Stopwatch.StartNew();
                c.RunToCompletion(() => seasonsProcessed++);
                w.Stop(); cpuTicks += w.ElapsedTicks;
                c.Engine.Simulator.CountPresence(presence);
                var local = new DateTime(target, DateTimeKind.Utc).AddMinutes(utcOffset);
                samples.Add(new HourSample { utc = target, localHour = local.Hour, offline = presence[0], online = presence[1] + presence[2] + presence[3] + presence[4], queued = presence[2], inMatch = presence[3], cooldown = presence[4], concurrentRaces = c.Engine.Simulator.ActiveRaces, completed = (int)(observer.races - before) });
                if (hour % 6 == 0 && !c.Engine.Simulator.ValidateInvariants(out var failure)) invariantFailure ??= "hour " + hour + ": " + failure;
                if (hour % 24 == 0)
                {
                    var top = TopRanked(state, 100);
                    if (previousTop10 != null)
                    {
                        top10Turnover.Add(1 - top.Take(10).Intersect(previousTop10).Count() / 10.0);
                        top100Turnover.Add(1 - top.Intersect(previousTop100).Count() / 100.0);
                    }
                    previousTop10 = top.Take(10).ToList(); previousTop100 = top;
                    int day = hour / 24;
                    if (checkpoints.Contains(day)) checkpointReports.Add(Checkpoint(day, state, c, observer, samples, cpuTicks, seasonsProcessed, initialRating, top10Turnover, top100Turnover));
                }
            }
            cpu.Stop();
            // Save size + reload timing.
            string dir = Path.Combine(output, "sim-save-" + label+"-"+Guid.NewGuid().ToString("N"));
            var store = new LocalLeagueStore(dir, Json.To, Json.From<LocalLeagueState>) { KeyProvider = new FixedKey() };
            var saveTimer = Stopwatch.StartNew(); store.Save(state.CopyForSave()); saveTimer.Stop();
            long journalBytes = Directory.GetFiles(dir, "*.journal").Sum(f => new FileInfo(f).Length);
            long archiveBytes = Directory.Exists(Path.Combine(dir, "seasons")) ? Directory.GetFiles(Path.Combine(dir, "seasons")).Sum(f => new FileInfo(f).Length) : 0;
            var loadTimer = Stopwatch.StartNew(); var reloaded = new LocalLeagueStore(dir, Json.To, Json.From<LocalLeagueState>) { KeyProvider = new FixedKey() }.Load(); loadTimer.Stop();
            bool identical = reloaded != null && reloaded.bots.Length == state.bots.Length && Enumerable.Range(0, state.bots.Length).All(i => reloaded.bots[i].currentMmr == state.bots[i].currentMmr && reloaded.bots[i].nextEventTicks == state.bots[i].nextEventTicks && reloaded.botNames[i] == state.botNames[i]);

            report["label"] = label; report["seed"] = seed; report["days"] = days; report["utcOffsetMinutes"] = utcOffset;
            report["population"]=population;report["ratingScale"]=ratingScale;report["retainedSavePath"]=dir;report["leaderboardArchiveBytes"]=history.StorageUsage().activeBytes;
            report["createMilliseconds"] = create.Elapsed.TotalMilliseconds;
            report["simulationCpuMilliseconds"] = cpuTicks * 1000.0 / Stopwatch.Frequency;
            report["cpuMillisecondsPerSimulatedDay"] = cpuTicks * 1000.0 / Stopwatch.Frequency / days;
            report["checkpoints"] = checkpointReports;
            report["invariantFailure"] = invariantFailure ?? "none";
            report["journalBytes"] = journalBytes; report["seasonArchiveBytes"] = archiveBytes;
            report["saveMilliseconds"] = saveTimer.Elapsed.TotalMilliseconds; report["loadMilliseconds"] = loadTimer.Elapsed.TotalMilliseconds;
            report["reloadIdentical"] = identical;
            report["bytesPerBotRecord"] = LocalLeagueStore.BytesPerBot;
            report["managedHeapBytes"] = GC.GetTotalMemory(true);
            report["seasonReceipts"] = state.ledger.seasonReceipts.Select(r => new { r.transitionId, boundary = new DateTime(r.boundaryUtc, DateTimeKind.Utc).ToString("u"), r.ratingBefore, r.ratingAfter, r.rankedHumansAndBots }).ToList();
            report["ratingBudget"] = Budget(state);
            File.WriteAllText(Path.Combine(output, "simulation-" + label + ".json"), Json.Pretty(report));
            // Hourly activity CSV (shape validation, not fixed bands).
            var csv = new StringBuilder("utc,local_hour,online,offline,queued,in_match,cooldown,concurrent_races,completed_races_in_hour\n");
            foreach (var s in samples) csv.Append($"{new DateTime(s.utc, DateTimeKind.Utc):yyyy-MM-ddTHH:mm}Z,{s.localHour},{s.online},{s.offline},{s.queued},{s.inMatch},{s.cooldown},{s.concurrentRaces},{s.completed}\n");
            File.WriteAllText(Path.Combine(output, "activity-" + label + ".csv"), csv.ToString());
            Console.WriteLine($"[{label}] {days} d: races={observer.races} cpu={report["simulationCpuMilliseconds"]:F0}ms invariant={report["invariantFailure"]} journal={journalBytes}B reload={identical}");
            return invariantFailure == null && identical ? 0 : 1;
        }

        static List<int> TopRanked(LocalLeagueState s, int count) => Enumerable.Range(0, s.bots.Length).Where(i => s.bots[i].Ranked).OrderByDescending(i => s.bots[i].currentMmr).ThenBy(i => i).Take(count).ToList();

        public static object Budget(LocalLeagueState s)
        {
            var b = s.ledger.budget; long actual = s.human.currentMmr; foreach (var bot in s.bots) actual += bot.currentMmr;
            return new { b.pairwiseNet, b.floorInjection, b.ceilingRemoval, b.placementIssuance, b.softResetRemoval, b.migrationRemoval, b.simulatedRaces, b.physicalRaces, b.settledParticipants,
                expectedPopulationRating = b.Expected(0), actualPopulationRating = actual, reconciled = b.Expected(0) == actual };
        }

        static object Checkpoint(int day, LocalLeagueState s, CompetitiveCoordinator c, RaceObserver o, List<HourSample> samples, long cpuTicks, long seasons, long initialRating, List<double> t10, List<double> t100)
        {
            var ranked = Enumerable.Range(0, s.bots.Length).Where(i => s.bots[i].Ranked).ToArray();
            var mmr = ranked.Select(i => (double)s.bots[i].currentMmr).ToArray();
            var skill = ranked.Select(i => s.bots[i].DevelopedSkill).ToArray();
            var lastDay = samples.Skip(Math.Max(0, samples.Count - 24)).ToList();
            var all = samples.Select(x => (double)x.completed).ToList();
            var byLocalHour = Enumerable.Range(0, 24).Select(h => new { hour = h, meanCompleted = samples.Where(x => x.localHour == h).Select(x => (double)x.completed).DefaultIfEmpty(0).Average(), meanOnline = samples.Where(x => x.localHour == h).Select(x => (double)x.online).DefaultIfEmpty(0).Average() }).ToList();
            var streakWins = s.bots.Max(b => (int)b.streak); var streakLoss = s.bots.Min(b => (int)b.streak);
            var tiers = ranked.GroupBy(i => MmrTierUtility.GetDisplayName(s.bots[i].currentMmr)).OrderBy(g => s.bots[g.First()].currentMmr).ToDictionary(g => g.Key, g => g.Count());
            var racesPerBot = s.bots.Select(b => (double)b.races).ToArray();
            return new
            {
                day, totalSimulatedRaces = o.races, seasonsProcessed = seasons,
                completedRacesPerHour = Stats.Describe(all), completedRacesPerHourLastDay = Stats.Describe(lastDay.Select(x => (double)x.completed)),
                onlineBots = Stats.Describe(samples.Select(x => (double)x.online)), inMatchBots = Stats.Describe(samples.Select(x => (double)x.inMatch)),
                queuedBots = Stats.Describe(samples.Select(x => (double)x.queued)), concurrentRaces = Stats.Describe(samples.Select(x => (double)x.concurrentRaces)),
                byLocalHour,
                rankedBots = ranked.Length, unrankedBots = s.bots.Length - ranked.Length, mmr = Stats.Describe(mmr), tiers,
                hiddenSkillVsMmrSpearman = ranked.Length > 10 ? Stats.Spearman(skill, mmr) : double.NaN,
                hiddenSkillVsMmrPearson = ranked.Length > 10 ? Stats.Pearson(skill, mmr) : double.NaN,
                promotions = o.promotions, demotions = o.demotions, placementsCompleted = o.placementsCompleted,
                racesPerNonZeroPairwiseNet = o.perRaceNetNonZero, maxAbsRaceNet = o.maxAbsNet,
                top10DailyTurnover = t10.Count > 0 ? t10.Average() : double.NaN, top100DailyTurnover = t100.Count > 0 ? t100.Average() : double.NaN,
                maxWinStreak = streakWins, maxLossStreak = -streakLoss, racesPerBot = Stats.Describe(racesPerBot),
                ratingBudget = Budget(s), cpuMilliseconds = cpuTicks * 1000.0 / Stopwatch.Frequency,
                topFive = TopRanked(s, 5).Select(i => new { name = LeagueBotCatalog.Name(s, i), mmr = s.bots[i].currentMmr, skill = Math.Round(s.bots[i].DevelopedSkill, 3), races = s.bots[i].races, profile = ((BotActivityProfile)s.bots[i].profile).ToString() }).ToList()
            };
        }
    }

    public sealed class FixedKey : IIntegrityKeyProvider { readonly byte[] key; public FixedKey(byte seed = 7) { key = new byte[32]; for (int i = 0; i < 32; i++) key[i] = (byte)(seed * 31 + i * 17); } public byte[] Key() => key; }
}
