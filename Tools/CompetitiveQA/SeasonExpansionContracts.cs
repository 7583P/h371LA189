using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static class SeasonExpansionContracts
    {
        static readonly long Start = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc).Ticks;
        static int passed, failed;
        static readonly List<string> checks = new List<string>(), failures = new List<string>();
        static void Check(bool ok, string label) { if (ok) { passed++; checks.Add(label); } else { failed++; failures.Add(label); Console.WriteLine("FAIL " + label); } }
        static bool Reject(Action action) { try { action(); return false; } catch (Exception e) when (e is IOException || e is InvalidDataException || e is InvalidOperationException || e is ArgumentException || e is OperationCanceledException) { return true; } }
        static PhysicalRacerResult[] Results(LocalLeagueState s, PendingRankedRace p, bool finished = true)
        {
            var r = new PhysicalRacerResult[12];
            r[0] = new PhysicalRacerResult { racerId = CompetitiveCoordinator.HumanId, isHuman = true, position = 1, finished = finished, bestLap = 68, shortcuts = 3, aerials = 1 };
            for (int i = 1; i < 12; i++) r[i] = new PhysicalRacerResult { racerId = LeagueBotCatalog.Id(s.populationSeed, p.botIndices[i - 1]), position = i + 1, finished = true };
            return r;
        }
        static RaceSettlementRecord Play(CompetitiveCoordinator c, int seed, bool finished = true)
        {
            var p = c.BeginRankedRace(seed, c.State.lastKnownUtcTimestamp);
            if (p == null) throw new InvalidOperationException("Fixture failed to find a lobby.");
            c.MarkStarted(p.raceId, c.State.lastKnownUtcTimestamp);
            return c.SettlePhysicalRace(p.raceId, Results(c.State, p, finished), c.State.lastKnownUtcTimestamp + TimeSpan.TicksPerMinute);
        }
        static LocalLeagueStore Store(string path) => new LocalLeagueStore(path, Json.To, Json.From<LocalLeagueState>);
        public static int Run(string output)
        {
            passed = failed = 0; checks.Clear(); failures.Clear();
            string run = Path.Combine(Path.GetFullPath(output), "isolated-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(run);
            for (int i = 0; i <= 20; i++)
            {
                var tier = (MmrTier)i; int min = MmrTierUtility.MinimumRating(tier);
                Check(MmrTierUtility.GetTier(min) == tier && (i == 0 || MmrTierUtility.GetTier(min - 1) == (MmrTier)(i - 1)), "Inclusive minimum " + min + " " + MmrTierUtility.GetTierDisplayName(tier));
            }
            Check(MmrTierUtility.GetTier(10000) == MmrTier.ApexII && MmrTierUtility.GetTier(10001) == MmrTier.ApexBeyond, "Hill strict Beyond rule retained at 10000/10001");
            foreach (int seed in new[] { 4207, 4208, 4209 }) Qualification(seed);
            int[] positions = { 1, 10, 11, 50, 51, 100, 101, 500, 501, 1000, 1001 };
            int[] bands = { 10, 10, 50, 50, 100, 100, 500, 500, 1000, 1000, 0 };
            for (int i = 0; i < positions.Length; i++)
            {
                var definition = RacingTitleCatalog.Resolve("current-season-rank", 7, 123, positions[i]);
                Check(SeasonalTitleRules.TopBand(positions[i]) == bands[i] && (bands[i] == 0 ? definition == null : definition.displayName == "CURRENT SEASON 7 TOP " + bands[i]), "Official title boundary " + positions[i]);
            }
            Check(RacingTitleCatalog.Resolve("current-season-rank", 7, 10000, 0) == null, "Season Placement cannot show a current top title");
            Check(RacingTitleCatalog.Resolve("season-top:3:10").displayName == "SEASON 3 TOP 10", "Historical title cannot masquerade as CURRENT");
            var unearned = new LeagueCompetitor(); RacingTitleCatalog.UpdateAchievements(unearned);
            Check(!unearned.unlockedTitleIds.Contains("assassiner") && !RacingTitleCatalog.Select(unearned, "assassiner"), "ASSASSINER cannot be selected without earning its racing challenge");
            History(run);
            Population(run);
            File.WriteAllText(Path.Combine(output, "season-expansion-contracts.json"), Json.Pretty(new { passed, failed, checks, failures, isolatedData = run }));
            Console.WriteLine($"Season expansion: {passed} passed, {failed} failed; retained QA data: {run}");
            return failed == 0 ? 0 : 1;
        }
        static void Qualification(int seed)
        {
            var s = LocalLeagueEngine.Create(Start, seed); var c = new CompetitiveCoordinator(s, new LocalLeagueConfiguration());
            s.lastKnownUtcTimestamp += 3 * TimeSpan.TicksPerHour; c.RunToCompletion(null);
            Check(!s.human.Ranked && s.human.placementGames == 0 && c.Engine.OfficialPosition(s.human.id) == 0, seed + ": career starts 0/3 with no position");
            Play(c, seed);
            Check(s.human.placementGames == 1 && s.human.qualifiedSeason == 1 && !SeasonQualification.IsOfficial(s.human, 1) && s.human.currentMmr == 0, seed + ": first seasonal finish counts without revealing career rank");
            Play(c, seed + 1); var third = Play(c, seed + 2);
            Check(s.human.Ranked && c.Engine.OfficialPosition(s.human.id) > 0, seed + ": 3/3 reveals rank and official position");
            Check(s.human.assassinerEarned && RacingTitleCatalog.Select(s.human, "assassiner"), seed + ": completed ranked win with 3 shortcuts and 1 aerial earns ASSASSINER");
            int mmr = s.human.currentMmr, races = s.human.races;
            c.ApplyRecord(third, null, s.lastKnownUtcTimestamp);
            Check(s.human.currentMmr == mmr && s.human.races == races, seed + ": replay cannot qualify or settle twice");
            var stale = third.Copy(); stale.raceId += "-stale"; stale.seasonId++;
            Check(Reject(() => c.ApplyRecord(stale, null, s.lastKnownUtcTimestamp)) && s.human.currentMmr == mmr, seed + ": stale-season settlement is rejected without rating changes");
            stale.seasonId = s.currentSeasonNumber; stale.origin = (byte)RaceOrigin.Simulated;
            Check(Reject(() => c.ApplyRecord(stale, null, s.lastKnownUtcTimestamp)) && s.human.races == races, seed + ": no simulated human settlements");

            // Authoritative reset curve and cap remain exactly unchanged.
            s.human.currentMmr = s.human.peakMmr = s.human.seasonPeakMmr = 10000;
            LocalSeasonEngine.AdvanceOne(s, c.Config, s.seasonEndTimestamp);
            c = new CompetitiveCoordinator(s, c.Config);
            Check(s.human.currentMmr == 10000 && s.human.Ranked && s.human.placementGames == 3 && s.human.qualifiedSeason == 0 && SeasonQualification.DisplayRank(s.human, 2) == "(Season Placement)", seed + ": reset retains 10000 visible MMR and career 3/3; season is 0/1");
            s.bots[0].placementGames = 3; s.bots[0].currentMmr = 100; s.bots[0].flags |= SeasonQualification.QualifiedFlag;
            c.Engine.Index.Update(0, 100); c.Engine.RefreshHumanIndex();
            Check(c.Engine.Index.AtRank(0) == 0 && c.Engine.OfficialPosition(s.human.id) == 0, seed + ": qualified 100 MMR sorts above pending 10000 MMR");
            Check(c.Engine.Index.Valid(), seed + ": qualification updates preserve order-statistics invariants");

            // A migration uses real evidence and never counts abandonment.
            var legacy = LocalLeagueEngine.Create(Start, seed + 1); legacy.qualificationContract = 0;
            legacy.human.placementGames = 3; legacy.human.seasonRaces = 99;
            SeasonQualification.Migrate(legacy);
            Check(legacy.human.qualifiedSeason == 0, seed + ": legacy race counters alone cannot grant seasonal qualification");
            legacy.qualificationContract = 0; var evidence = third.Copy(); evidence.seasonId = 1; evidence.state = (byte)RaceSettlementState.RaceSettled; evidence.settledUtc = Start;
            legacy.ledger.recentSettlements.Add(evidence); SeasonQualification.Migrate(legacy);
            Check(legacy.human.qualifiedSeason == 1, seed + ": genuine persisted current-season finish grandfathers qualification");

            var fresh = LocalLeagueEngine.Create(Start, seed + 100); var noFinish = new CompetitiveCoordinator(fresh, new LocalLeagueConfiguration());
            fresh.lastKnownUtcTimestamp += 3 * TimeSpan.TicksPerHour; noFinish.RunToCompletion(null);
            Play(noFinish, seed, false);
            Check(fresh.human.qualifiedSeason == 0, seed + ": DNF does not qualify");
            var pending = noFinish.BeginRankedRace(seed + 9, fresh.lastKnownUtcTimestamp);
            noFinish.MarkStarted(pending.raceId, fresh.lastKnownUtcTimestamp); noFinish.AbandonOrCancel(pending.raceId, fresh.lastKnownUtcTimestamp);
            Check(fresh.human.qualifiedSeason == 0, seed + ": abandonment does not qualify");
            var reservation=noFinish.BeginRankedRace(seed+10,fresh.lastKnownUtcTimestamp);
            var staleRecovery=third.Copy();staleRecovery.seasonId=0;staleRecovery.raceId+="-stale-recovery";
            int priorRaces=fresh.human.races;
            string recovery=noFinish.ResolvePending(fresh.lastKnownUtcTimestamp,new LocalLeagueStore.RankedSessionFile{settlement=staleRecovery,pending=reservation,profileId=fresh.profileId});
            Check(recovery.Contains("stale")&&!noFinish.HasPendingHumanRace&&fresh.human.races==priorRaces&&fresh.human.qualifiedSeason==0,seed+": stale old-season crash receipt is ignored and reservations released without qualification");
            var next=LocalLeagueEngine.Create(GlobalSeasonCalendar.Start(2)+TimeSpan.TicksPerDay,seed+200);
            next.human=s.human.CopyForSave();var nextCoordinator=new CompetitiveCoordinator(next,new LocalLeagueConfiguration());
            next.lastKnownUtcTimestamp+=3*TimeSpan.TicksPerHour;nextCoordinator.RunToCompletion(null);Play(nextCoordinator,seed+12);
            Check(next.human.placementGames==3&&next.human.qualifiedSeason==2&&nextCoordinator.Engine.OfficialPosition(next.human.id)>0,seed+": exactly one legitimate new-season finish qualifies without repeating career placement");
        }
        static LocalLeagueState HistoryFixture(int seed = 99, int population = 1000)
        {
            var s = LocalLeagueEngine.Create(Start, seed, population);
            s.lastKnownUtcTimestamp = s.seasonEndTimestamp; s.lastLeagueSimulationUtc = s.seasonEndTimestamp - 1;
            s.human.placementGames = 3; s.human.qualifiedSeason = 1; s.human.currentMmr = 10000;
            s.human.displayName = "Diego, \"Lima\"\n星"; s.human.selectedTitleId = "current-season-rank";
            for (int i = 0; i < s.bots.Length; i++) { s.bots[i].placementGames = 3; s.bots[i].currentMmr = 9000 - i; if (i % 2 == 0) s.bots[i].flags |= SeasonQualification.QualifiedFlag; }
            return s;
        }
        static void History(string run)
        {
            var s = HistoryFixture(); string historyPath = Path.Combine(run, "history"); var history = new SeasonHistoryStore(historyPath);
            string unrelated = Path.Combine(run, "purchases-wallet-adventure-keystore-backup.sentinel"); File.WriteAllText(unrelated, "protected unrelated data"); string sentinelHash = SeasonHistoryStore.FileHash(unrelated);
            history.Capture(s, new LeagueRankIndex(s, true)); string original = SeasonHistoryStore.FileHash(Path.Combine(historyPath, "season-1.hal"));
            history.Capture(s, new LeagueRankIndex(s, true));
            Check(SeasonHistoryStore.FileHash(Path.Combine(historyPath, "season-1.hal")) == original, "Final archive is immutable and idempotent");
            using (var reader = history.Open(1))
            {
                var rows = reader.Page(0, 14); Check(rows[0].position == 1 && rows[0].mmr == 10000 && rows[0].name == s.human.displayName && rows[0].title == "season-top:1:10", "Final archive captures pre-reset MMR, identity, name and historical title");
                Check(reader.Page(reader.Info.qualified, 1)[0].position == 0, "Unqualified historical rows have no official position");
                Check(reader.Find("Lima") == 0 && reader.Find("", true) == 0, "Lazy historical name search and jump to human");
            }
            s.human.displayName = "renamed"; using (var reader = history.Open(1)) Check(reader.Page(0, 1)[0].name.Contains("Lima"), "Later renames cannot rewrite historical names");
            var cancel = new CancellationToken(true); string cancelled = Path.Combine(run, "cancelled.zip");
            Check(Reject(() => history.Export(1, cancelled, cancel)) && !File.Exists(cancelled) && !Directory.GetFiles(run, "*.partial-*").Any(), "Cancelled export leaves no completed ZIP and preserves history");
            string zip = Path.Combine(run, "public-season-1.zip"); history.Export(1, zip); history.VerifyExport(1, zip);
            Check(File.Exists(zip) && SeasonHistoryStore.FileHash(Path.Combine(historyPath, "season-1.hal")) == original, "Verified public ZIP round trip handles quoted multiline UTF-8 names without deleting history");
            Check(Reject(() => history.PrepareDeletion(1, 1, Start)), "Current season cannot be deleted");
            for (int day = 1; day <= 29; day += 28)
            {
                long wall = Start + day * TimeSpan.TicksPerDay;
                history.MoveToTrash(history.PrepareDeletion(1, 2, Start), 2, 0); history.MaintainTrash(2, wall, day * 86400000L);
                var usage = history.StorageUsage(); Check(usage.activeBytes == 0 && usage.trashBytes > 0 && usage.recoverableBytes == usage.trashBytes, "Trash consumes recoverable bytes at day " + day);
                Check(history.Restore(1), "Restore works at day " + day);
            }
            history.MoveToTrash(history.PrepareDeletion(1, 2, Start), 2, 0);
            history.MaintainTrash(2, Start + 29 * TimeSpan.TicksPerDay, 29 * 86400000L);
            Check(history.List().Any(x => x.deleted), "Purge is forbidden before 30 trustworthy elapsed days");
            history.MaintainTrash(2, Start + 30 * TimeSpan.TicksPerDay, 30 * 86400000L);
            Check(!history.List().Any(), "At 30 trustworthy elapsed days only the confirmed archive is purged");
            Check(File.Exists(zip) && SeasonHistoryStore.FileHash(unrelated) == sentinelHash, "Purge preserves exported ZIP and unrelated purchases/progress/backup sentinel");
            history.Capture(s, new LeagueRankIndex(s, true)); history.MoveToTrash(history.PrepareDeletion(1, 2, Start), 2, 0);
            var restarted = new SeasonHistoryStore(historyPath); restarted.MaintainTrash(2, Start + 400 * TimeSpan.TicksPerDay, 400 * 86400000L);
            Check(restarted.List().Any(x => x.deleted) && restarted.Restore(1), "Restart plus wall-clock jump cannot prove elapsed days or purge history");
            restarted.MoveToTrash(restarted.PrepareDeletion(1, 2, Start), 2, 0);
            restarted.MaintainTrash(2, Start - TimeSpan.TicksPerDay, 30 * 86400000L);
            restarted.MaintainTrash(2, Start + 31 * TimeSpan.TicksPerDay, 31 * 86400000L);
            Check(restarted.List().Any(x => x.deleted) && restarted.Restore(1), "Clock rollback defers destructive purge and keeps restoration available");
            foreach (string checkpoint in new[] { "delete-intent", "restore-intent", "receipt-old-cleared" })
            {
                restarted.TransactionCheckpoint = name => { if (name == checkpoint) throw new IOException("Injected process interruption"); };
                if (checkpoint == "delete-intent") Check(Reject(() => restarted.MoveToTrash(restarted.PrepareDeletion(1, 2, Start), 2, 0)), "Interrupted delete intent is durable");
                else { restarted.MoveToTrash(restarted.PrepareDeletion(1, 2, Start), 2, 0); Check(Reject(() => restarted.Restore(1)), "Interrupted "+checkpoint+" is durable"); }
                restarted = new SeasonHistoryStore(historyPath); restarted.MaintainTrash(2, Start, 0); Check(restarted.Restore(1), "Interrupted " + checkpoint + " recovers without data loss");
            }
            byte[] corrupt = File.ReadAllBytes(Path.Combine(historyPath, "season-1.hal")); corrupt[corrupt.Length / 2] ^= 1; File.WriteAllBytes(Path.Combine(historyPath, "season-1.hal"), corrupt);
            Check(Reject(() => { using (new SeasonHistoryStore(historyPath).Open(1)) { } }), "Corrupt archive is rejected and preserved");
            var missing = HistoryFixture(100); missing.seasonHistoryIncomplete = true;
            var missingStore = new SeasonHistoryStore(Path.Combine(run, "unavailable")); missingStore.Capture(missing, new LeagueRankIndex(missing, true));
            Check(missingStore.List().Single().unavailableReason.Contains("NOT AVAILABLE"), "Skipped catch-up creates NOT AVAILABLE rather than fictional standings");
        }
        static void Population(string run)
        {
            var s = LocalLeagueEngine.Create(Start, 4207); s.utcOffsetMinutes = -300;
            var c = new CompetitiveCoordinator(s, new LocalLeagueConfiguration()); s.lastKnownUtcTimestamp += 3 * TimeSpan.TicksPerHour; c.RunToCompletion(null);
            byte[] old = MemoryMarshal.AsBytes(s.bots.AsSpan()).ToArray(); string[] names = (string[])s.botNames.Clone();
            string savePath = Path.Combine(run, "population"); var store = Store(savePath); store.Save(s.CopyForSave());
            Check(Reject(() => LeaguePopulationMigration.Expand(store, s, c.Config, stage => { if (stage == "backup-verified") throw new IOException("Interrupted migration"); })), "Population migration interruption after backup is recoverable");
            Check(Store(savePath).Load().population == 10000, "Interrupted expansion preserves the durable 10k generation");
            store = Store(savePath); var source = store.Load(); var timer = Stopwatch.StartNew();
            var expanded = LeaguePopulationMigration.Expand(store, source, c.Config); timer.Stop();
            bool preserved = MemoryMarshal.AsBytes(expanded.bots.AsSpan(0, 10000)).SequenceEqual(old) && expanded.botNames.Take(10000).SequenceEqual(names);
            Check(preserved, "100k migration preserves all original 10k bot bytes and names, including live reservations");
            Check(expanded.bots.Length == 100000 && expanded.bots.Skip(10000).All(b => b.currentMmr == 0 && b.races == 0 && b.placementGames == 0), "Exactly 90k new identities start legitimately at zero MMR and career 0/3");
            Check(Enumerable.Range(0, 100000).Select(i => LeagueBotCatalog.Id(expanded.populationSeed, i)).Distinct().Count() == 100000 && expanded.botNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 100000, "100k stable IDs and unique persisted names");
            Check(ReferenceEquals(expanded, LeaguePopulationMigration.Expand(store, expanded, c.Config)), "Repeated population migration is idempotent");
            var scheduler = new CompetitiveCoordinator(expanded, c.Config); int minimumOnline = int.MaxValue, maximumOnline = 0; var counts = new int[5];
            long dayStart = expanded.lastKnownUtcTimestamp;
            for (int hour = 1; hour <= 24; hour++)
            {
                expanded.lastKnownUtcTimestamp = dayStart + hour * TimeSpan.TicksPerHour; scheduler.RunToCompletion(null);
                scheduler.Engine.Simulator.CountPresence(counts); int online = counts[1] + counts[2] + counts[3] + counts[4]; minimumOnline = Math.Min(minimumOnline, online); maximumOnline = Math.Max(maximumOnline, online);
                Check(counts.Sum() == 100000 && online >= 50000 && scheduler.Engine.Simulator.ValidateInvariants(out _), "100k presence, 50k online floor and exclusive reservations at hour " + hour);
            }
            var pending = scheduler.BeginRankedRace(123, expanded.lastKnownUtcTimestamp);
            Check(pending != null && pending.botIndices.Distinct().Count() == 11 && !scheduler.Engine.Simulator.Reserve(pending.botIndices, expanded.lastKnownUtcTimestamp), "100k human lobby reserves exactly eleven unique available bots, never twice");
            scheduler.CancelReservation(pending.raceId, expanded.lastKnownUtcTimestamp);
            Check(expanded.human.races == 0 && scheduler.Engine.Index.Valid(), "No human race simulation and valid 100k qualification index");
            var snapshot = expanded.CopyForSave(); store.Save(snapshot); var loaded = Store(savePath).Load();
            Check(loaded != null && loaded.population == 100000 && loaded.expandedCommunityPresence && loaded.bots.Select(b => b.currentMmr).SequenceEqual(expanded.bots.Select(b => b.currentMmr)), "100k reload preserves ratings, presence profile and season qualification");
            File.WriteAllText(Path.Combine(run, "population-measurements.json"), Json.Pretty(new { migrationMilliseconds = timer.Elapsed.TotalMilliseconds, minimumOnline, maximumOnline, bytesPerBot = LocalLeagueStore.BytesPerBot, journalBytes = Directory.GetFiles(savePath, "league-v4-*.journal").Select(p => new FileInfo(p).Length).ToArray(), managedHeapBytes = GC.GetTotalMemory(true), expectedRating = expanded.ledger.budget.Expected(0), actualRating = expanded.bots.Sum(b => (long)b.currentMmr) + expanded.human.currentMmr }));
        }
    }
}
