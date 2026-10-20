using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    // CPPS lifecycle + transfer contracts against the exact Unity source files.
    public static class ContractSuite
    {
        sealed class Report { public int passed, failed; public List<string> failures = new List<string>(); public List<string> successes = new List<string>(); }
        static Report report;
        static void Check(bool ok, string name) { if (ok) { report.passed++; report.successes.Add(name); } else { report.failed++; report.failures.Add(name); Console.WriteLine("FAIL " + name); } }

        const long Hour = TimeSpan.TicksPerHour, Minute = TimeSpan.TicksPerMinute, Day = TimeSpan.TicksPerDay;
        static long monotonic;

        sealed class League
        {
            public LocalLeagueState s; public CompetitiveCoordinator c;
            public PendingRankedRace lastPending; public RaceSettlementRecord lastRecord;
            public Func<RaceSettlementRecord, bool> onSettlement; // return false to "crash" before apply
        }

        static League New(long t0, int seed, int population = 10000)
        {
            var l = new League();
            l.s = LocalLeagueEngine.Create(t0, seed, population);
            l.c = Coordinator(l, l.s);
            Advance(l, 3 * Hour);
            return l;
        }
        static CompetitiveCoordinator Coordinator(League l, LocalLeagueState s) =>
            new CompetitiveCoordinator(s, new LocalLeagueConfiguration(), (p, r) => { l.lastPending = p; if (r != null) l.lastRecord = r; }, () => { });

        // Real time passing inside one session (monotonic and wall clock agree).
        static void Advance(League l, long ticks)
        {
            long target = l.s.lastKnownUtcTimestamp + ticks; monotonic += ticks / TimeSpan.TicksPerMillisecond;
            l.c.AcceptTime(target, monotonic); l.c.RunToCompletion(null);
        }
        // Absence: the app was closed (new session) and the device clock moved.
        static void Relaunch(League l, long deviceUtc) { l.c.Clock.EndSession(); monotonic += 1000; l.c.AcceptTime(deviceUtc, monotonic); l.c.RunToCompletion(null); }

        static PhysicalRacerResult[] Results(LocalLeagueState s, PendingRankedRace p, int humanPosition, bool humanFinished = true)
        {
            var r = new PhysicalRacerResult[12]; int position = 1;
            r[0] = new PhysicalRacerResult { racerId = CompetitiveCoordinator.HumanId, isHuman = true, position = humanPosition, finished = humanFinished, bestLap = 67.5f };
            for (int k = 0; k < 11; k++)
            {
                if (position == humanPosition) position++;
                r[k + 1] = new PhysicalRacerResult { racerId = LeagueBotCatalog.Id(s.populationSeed, p.botIndices[k]), position = position++, finished = true, bestLap = 68f + k * .2f };
            }
            return r;
        }

        static RaceSettlementRecord PlayRanked(League l, int humanPosition, int seed)
        {
            var p = l.c.BeginRankedRace(seed, l.s.lastKnownUtcTimestamp);
            if (p == null) return null;
            l.c.MarkStarted(p.raceId, l.s.lastKnownUtcTimestamp + Minute / 6);
            var record = l.c.SettlePhysicalRace(p.raceId, Results(l.s, p, humanPosition), l.s.lastKnownUtcTimestamp + 4 * Minute);
            Advance(l, 5 * Minute);
            return record;
        }

        static bool Reconciled(LocalLeagueState s)
        {
            long actual = s.human.currentMmr; foreach (var b in s.bots) actual += b.currentMmr;
            return s.ledger.budget.Expected(0) == actual && s.ledger.budget.pairwiseNet == 0 || s.ledger.budget.Expected(0) == actual;
        }

        public static int Run(string output)
        {
            report = new Report();
            long t0 = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc).Ticks; // mid-season, busy hour
            monotonic = 1000;

            // ---- Placement: exactly 3 races, no visible MMR before 3/3 ----
            var l = New(t0, 4207);
            var h = l.s.human;
            Check(!h.Ranked && h.currentMmr == 0 && h.placementGames == 0, "Fresh human is UNRANKED, Placement 0/3, no visible MMR");
            var r1 = PlayRanked(l, 2, 11); Check(r1 != null && !h.Ranked && h.currentMmr == 0 && h.placementGames == 1, "Placement 1/3: still UNRANKED with no visible MMR");
            var r2 = PlayRanked(l, 1, 12); Check(r2 != null && !h.Ranked && h.currentMmr == 0 && h.placementGames == 2, "Placement 2/3: still UNRANKED with no visible MMR");
            var r3 = PlayRanked(l, 3, 13); Check(r3 != null && h.Ranked && h.currentMmr > 0 && h.placementGames == 3, "Placement 3/3 reveals a ranked MMR (" + h.currentMmr + ")");
            Check(l.s.ledger.budget.placementIssuance > 0 && Reconciled(l.s), "Placement issuance is recorded and the rating budget reconciles");

            // ---- Exact-once settlement ----
            int mmr = h.currentMmr, races = h.races; long dupBefore = l.s.ledger.duplicateSettlementRejections;
            var again = l.c.SettlePhysicalRace(r3.raceId, Results(l.s, new PendingRankedRace { botIndices = new int[11] }, 1), l.s.lastKnownUtcTimestamp);
            l.c.ApplyRecord(r3, null, l.s.lastKnownUtcTimestamp);
            Check(again != null && again.raceId == r3.raceId && h.currentMmr == mmr && h.races == races && l.s.ledger.duplicateSettlementRejections == dupBefore + 2,
                "A RaceId settles exactly once; replayed settlement/apply are rejected and counted");

            // ---- Atomic reservation ----
            var pending = l.c.BeginRankedRace(21, l.s.lastKnownUtcTimestamp);
            bool reserved = pending != null && pending.botIndices.Length == 11 && pending.botIndices.Distinct().Count() == 11 &&
                pending.botIndices.All(i => l.s.bots[i].presence == (byte)BotPresence.InMatch && l.s.bots[i].activeRace == LeagueSimulator.HumanReservation);
            Check(reserved, "Eleven distinct real bots are atomically reserved IN_MATCH for the human RaceId");
            Advance(l, 3 * Minute);
            Check(pending.botIndices.All(i => l.s.bots[i].activeRace == LeagueSimulator.HumanReservation) && l.c.Engine.Simulator.ValidateInvariants(out _),
                "Reserved bots never enter simulated races while the human race runs; invariants hold");

            // ---- Cancel before GO: no rating effect ----
            int mmrBefore = h.currentMmr; int racesBefore = h.races;
            var cancelled = l.c.AbandonOrCancel(pending.raceId, l.s.lastKnownUtcTimestamp);
            Check(cancelled == null && h.currentMmr == mmrBefore && h.races == racesBefore && pending.botIndices.All(i => l.s.bots[i].activeRace == LeagueSimulator.NoRace),
                "Leaving before GO cancels: no rating change, bots released");

            // ---- Unrated test-race release ----
            var test = l.c.BeginRankedRace(22, l.s.lastKnownUtcTimestamp); l.c.MarkStarted(test.raceId, l.s.lastKnownUtcTimestamp);
            Check(l.c.CancelReservation(test.raceId, l.s.lastKnownUtcTimestamp) && h.currentMmr == mmrBefore && !l.c.HasPendingHumanRace,
                "Unrated test race releases its reservation without rating anyone");
            Advance(l, 5 * Minute);

            // ---- Abandon after GO ----
            var ab = l.c.BeginRankedRace(23, l.s.lastKnownUtcTimestamp); l.c.MarkStarted(ab.raceId, l.s.lastKnownUtcTimestamp);
            int abandonments = h.abandonments;
            var abRecord = l.c.AbandonOrCancel(ab.raceId, l.s.lastKnownUtcTimestamp + Minute);
            Check(abRecord != null && abRecord.abandonment && h.abandonments == abandonments + 1 && h.currentMmr <= mmrBefore && l.s.ledger.IsSettled(ab.raceId),
                "Leaving after GO is a settled abandonment (human rated last against the reserved lobby)");
            Advance(l, 5 * Minute);

            // ---- Crash after durable settlement, before apply ----
            var cp = l.c.BeginRankedRace(24, l.s.lastKnownUtcTimestamp); l.c.MarkStarted(cp.raceId, l.s.lastKnownUtcTimestamp);
            var preApply = l.s.CopyForSave();
            var record = l.c.SettlePhysicalRace(cp.raceId, Results(l.s, cp, 1), l.s.lastKnownUtcTimestamp + 3 * Minute);
            var expected = l.s.human.CopyForSave();
            var crashed = new League { s = preApply }; crashed.c = Coordinator(crashed, preApply);
            var session = new LocalLeagueStore.RankedSessionFile { pending = l.lastPending, settlement = record.Copy(), profileId = preApply.profileId };
            string action = crashed.c.ResolvePending(preApply.lastKnownUtcTimestamp, session);
            int afterFirst = preApply.human.currentMmr;
            string action2 = crashed.c.ResolvePending(preApply.lastKnownUtcTimestamp, session);
            Check(action.StartsWith("completed-durable-settlement") && afterFirst == expected.currentMmr && preApply.human.races == expected.races && preApply.human.currentMmr == afterFirst && !action2.StartsWith("completed"),
                "Crash after the durable settlement journal completes it exactly once on relaunch");

            // ---- Crash after GO without settlement ----
            Advance(l, 5 * Minute);
            var st = l.c.BeginRankedRace(25, l.s.lastKnownUtcTimestamp); l.c.MarkStarted(st.raceId, l.s.lastKnownUtcTimestamp);
            var killed = l.s.CopyForSave(); int ab2 = killed.human.abandonments;
            var kl = new League { s = killed }; kl.c = Coordinator(kl, killed);
            string action3 = kl.c.ResolvePending(killed.lastKnownUtcTimestamp, new LocalLeagueStore.RankedSessionFile { pending = l.lastPending, profileId = killed.profileId });
            Check(action3.StartsWith("abandonment") && killed.human.abandonments == ab2 + 1 && !kl.c.HasPendingHumanRace, "Force-close after GO resolves as abandonment on relaunch");
            l.c.AbandonOrCancel(st.raceId, l.s.lastKnownUtcTimestamp);

            // ---- Human MMR immune to time inside a season ----
            int frozenMmr = h.currentMmr; int season = l.s.currentSeasonNumber;
            Relaunch(l, l.s.lastKnownUtcTimestamp + 6 * Day);
            Check(l.s.currentSeasonNumber == season && h.currentMmr == frozenMmr, "Six days offline move bots only; human MMR is unchanged without races");

            // ---- Clock manipulation ----
            long high = l.s.lastKnownUtcTimestamp; long anomalies = l.s.ledger.totalAnomalies;
            l.c.Clock.EndSession(); monotonic += 1000;
            long accepted = l.c.AcceptTime(high - 3 * Day, monotonic);
            Check(accepted == high && l.c.Clock.Frozen && l.s.ledger.totalAnomalies == anomalies + 1, "Backward device clock never rewinds accepted time; league freezes and records it");
            monotonic += 1000; long jump = l.c.AcceptTime(high + 2 * Day, monotonic);
            Check(jump < high + Hour && l.s.ledger.totalAnomalies == anomalies + 2, "In-session forward clock jump is ignored (monotonic time wins) and recorded");

            // ---- Global season boundary ----
            long end = l.s.seasonEndTimestamp; int seasonBefore = l.s.currentSeasonNumber; int receipts = l.s.ledger.seasonReceipts.Count;
            Relaunch(l, end + 2 * Day);
            int receiptsAfter = l.s.ledger.seasonReceipts.Count;
            Relaunch(l, end + 2 * Day + Hour);
            Check(l.s.currentSeasonNumber == seasonBefore + 1 && l.s.currentSeasonNumber == GlobalSeasonCalendar.SeasonAt(l.s.lastKnownUtcTimestamp) &&
                receiptsAfter == receipts + 1 && l.s.ledger.seasonReceipts.Count == receiptsAfter &&
                l.s.ledger.seasonReceipts.Select(x => x.transitionId).Distinct().Count() == l.s.ledger.seasonReceipts.Count,
                "Crossing one global UTC boundary processes exactly one idempotent season transition");
            Check(l.s.seasonStartTimestamp == GlobalSeasonCalendar.Start(l.s.currentSeasonNumber) && new DateTime(l.s.seasonEndTimestamp, DateTimeKind.Utc) == new DateTime(l.s.seasonStartTimestamp, DateTimeKind.Utc).AddMonths(3),
                "Season spans exactly three calendar months in UTC");
            Check(Reconciled(l.s) && l.c.Engine.Simulator.ValidateInvariants(out _), "Rating budget reconciles and simulator invariants hold after the season");

            // ---- Quiet hour matchmaking ----
            long night = new DateTime(2026, 11, 5, 4, 30, 0, DateTimeKind.Utc).Ticks;
            var q = New(night - 6 * Hour, 9001);
            Advance(q, 6 * Hour);
            int ok = 0, brought = 0;
            for (int i = 0; i < 25; i++)
            {
                var rr = PlayRanked(q, 1 + i % 12, 300 + i);
                if (rr != null) ok++;
                if (q.c.LastMatchUsedSessionBringForward) brought++;
            }
            Check(ok == 25 && q.c.Engine.Simulator.ValidateInvariants(out _), "25 consecutive deep-night ranked searches all reserve real bots (" + brought + " used session bring-forward)");

            // ---- Forced shortage: small league at night must bring sessions forward ----
            var small = New(night - 2 * Hour, 77, 40);
            Advance(small, 2 * Hour);
            int availableBefore = Enumerable.Range(0, 40).Count(i => small.c.Engine.Simulator.IsAvailable(i, true));
            var busyBefore = Enumerable.Range(0, 40).Where(i => small.s.bots[i].presence == (byte)BotPresence.InMatch || small.s.bots[i].activeRace != LeagueSimulator.NoRace).ToHashSet();
            var sp = small.c.BeginRankedRace(5, small.s.lastKnownUtcTimestamp);
            Check(availableBefore < 11 && sp != null && small.c.LastMatchUsedSessionBringForward && sp.botIndices.All(i => !busyBefore.Contains(i)) && small.c.Engine.Simulator.ValidateInvariants(out _),
                "Shortage (" + availableBefore + " available) brings real offline sessions forward; no bot already racing is taken");
            if (sp != null) small.c.CancelReservation(sp.raceId, small.s.lastKnownUtcTimestamp);

            // ---- Export / import ----
            string dirA = Path.Combine(output, "transfer-a"), dirB = Path.Combine(output, "transfer-b");
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true); if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            var storeA = new LocalLeagueStore(dirA, Json.To, Json.From<LocalLeagueState>) { KeyProvider = new FixedKey(1) }; storeA.Load();
            Check(storeA.Save(l.s), "Source league commits to its journal");
            byte[] package = LeagueTransfer.Export(l.s, storeA.ReadNewestJournalBytes(), l.s.lastKnownUtcTimestamp, "qa", Json.To, out var header);
            var storeB = new LocalLeagueStore(dirB, Json.To, Json.From<LocalLeagueState>) { KeyProvider = new FixedKey(2) }; storeB.Load();
            LeagueTransfer.JournalParser parser = (byte[] j, out long g, out bool k) => storeB.ParseJournalBytes(j, out g, out k);
            var imported = LeagueTransfer.Read(package, Json.From<LeagueExportHeader>, parser, null);
            bool same = imported.accepted && imported.state.human.currentMmr == l.s.human.currentMmr && imported.state.human.placementGames == l.s.human.placementGames &&
                imported.state.human.unlockedTitleIds.SequenceEqual(l.s.human.unlockedTitleIds) && imported.state.human.vehiclePreference == l.s.human.vehiclePreference &&
                imported.state.bots.Length == l.s.bots.Length && Enumerable.Range(0, l.s.bots.Length).All(i => imported.state.bots[i].currentMmr == l.s.bots[i].currentMmr && imported.state.bots[i].placementGames == l.s.bots[i].placementGames && imported.state.bots[i].presence == l.s.bots[i].presence) &&
                imported.state.botNames.SequenceEqual(l.s.botNames) && imported.state.currentSeasonNumber == l.s.currentSeasonNumber && imported.state.lastLeagueSimulationUtc == l.s.lastLeagueSimulationUtc;
            Check(same, "Import on another install restores human Placement/MMR/titles/combo, all 10k bots, names, presence, season and simulation cursor");
            Check(!imported.installKeyMatched, "Cross-install import is accepted on the device-agnostic checksum (install HMAC reported, not required)");
            var il = new League { s = imported.state }; il.c = Coordinator(il, imported.state);
            int humanBefore = il.s.human.currentMmr; int humanRaces = il.s.human.races; long simBefore = il.s.lastLeagueSimulationUtc;
            long t1 = il.s.lastKnownUtcTimestamp + 4 * Day;
            Relaunch(il, t1);
            Check(il.s.lastLeagueSimulationUtc >= t1 - Minute && il.s.lastLeagueSimulationUtc > simBefore && il.s.human.races == humanRaces &&
                (il.s.currentSeasonNumber != imported.header.currentSeason || il.s.human.currentMmr == humanBefore),
                "After import, bots catch up T0->T1 while the human simulates no races");
            var reimport = LeagueTransfer.Read(package, Json.From<LeagueExportHeader>, parser, il.s);
            Check(!reimport.accepted && reimport.failure.Contains("already imported"), "The same export cannot be imported twice into a league");
            var tampered = (byte[])package.Clone(); tampered[tampered.Length / 2] ^= 0x5A;
            Check(!LeagueTransfer.Read(tampered, Json.From<LeagueExportHeader>, parser, null).accepted, "A modified package is rejected");
            var busy = l.c.BeginRankedRace(99, l.s.lastKnownUtcTimestamp); bool refused = false;
            try { LeagueTransfer.Export(l.s, storeA.ReadNewestJournalBytes(), l.s.lastKnownUtcTimestamp, "qa", Json.To, out _); } catch (InvalidOperationException) { refused = true; }
            Check(busy != null && refused, "Export is refused while a ranked race is reserved");
            l.c.CancelReservation(busy.raceId, l.s.lastKnownUtcTimestamp);
            var future = LeagueTransfer.Read(package, Json.From<LeagueExportHeader>, parser, null);
            var fl = new League { s = future.state }; fl.c = Coordinator(fl, future.state);
            long nextSeasonInstant = GlobalSeasonCalendar.End(future.state.currentSeasonNumber) + 10 * Day;
            Relaunch(fl, nextSeasonInstant);
            Check(fl.s.currentSeasonNumber == GlobalSeasonCalendar.SeasonAt(nextSeasonInstant), "Importing into a later global season resolves the current season (current season wins)");

            File.WriteAllText(Path.Combine(output, "cpps-contracts.json"), Json.Pretty(new { report.passed, report.failed, report.failures, report.successes }));
            Console.WriteLine($"CPPS contracts: {report.passed} passed, {report.failed} failed");
            return report.failed == 0 ? 0 : 1;
        }
    }
}
