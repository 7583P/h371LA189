#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingSeasonResetContracts
    {
        static readonly int[] Ratings = { 9999, 10000, 10001, 10100, 10500, 11000, 12000, 15000, 20000, int.MaxValue };
        [Serializable] sealed class Sample { public string curve, rankAfter; public int oldMmr, normalResult, finalMmr, beyondLevelBefore; }
        [Serializable] sealed class Samples { public Sample[] cases; public string note = "Alternate curves are isolated test fixtures. The installed curve and save schema are unchanged."; }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--closure-output");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Missing --closure-output");
            string output = args[at + 1]; Directory.CreateDirectory(output);
            var report = new ClosureContractReport();
            try { Curves(report, output); Transition(report, output); Badge(report); }
            catch (Exception error) { report.Check(false, error.ToString()); }
            File.WriteAllText(Path.Combine(output, "season-reset-contracts.json"), JsonUtility.ToJson(report, true));
            if (report.failed != 0) throw new InvalidOperationException("Season reset contracts failed: " + report.failed);
            Debug.Log("SEASON_RESET_CONTRACTS passed=" + report.passed + " failed=" + report.failed);
        }

        static LocalLeagueConfiguration LowerCurve() => new LocalLeagueConfiguration { resetCurve = new[] {
            new SoftResetPoint(0, 0), new SoftResetPoint(10000, 9500), new SoftResetPoint(10500, 9950), new SoftResetPoint(20000, 14000) } };
        static void Curves(ClosureContractReport report, string output)
        {
            var authored = Resources.Load<RacingProgressionConfiguration>("RacingProgressionConfiguration").league;
            var defaults = new LocalLeagueConfiguration(); var lower = LowerCurve();
            var higher = new LocalLeagueConfiguration { resetCurve = new[] {
                new SoftResetPoint(0, 0), new SoftResetPoint(10000, 10000), new SoftResetPoint(10500, 10200),
                new SoftResetPoint(12500, 10900), new SoftResetPoint(20000, 14000) } };
            bool unchanged = true, ordered = true;
            for (int rating = 0; rating <= 10000; rating++) {
                unchanged &= authored.ResetMmr(rating) == defaults.CalculateNormalSeasonReset(rating);
                ordered &= authored.ResetMmr(rating) >= 0 && authored.ResetMmr(rating) <= rating;
                if (rating > 0) ordered &= authored.ResetMmr(rating) >= authored.ResetMmr(rating - 1);
            }
            report.Check(unchanged && ordered, "Installed 0-10k curve retains all 10001 ordered, non-increasing reset results");
            var samples = new System.Collections.Generic.List<Sample>();
            foreach (var pair in new[] { ("installed", authored), ("below-ceiling fixture", lower), ("above-ceiling fixture", higher) }) {
                foreach (int old in Ratings) {
                    int normal = pair.Item2.CalculateNormalSeasonReset(old), final = pair.Item2.ResetMmr(old);
                    report.Check(final == (old > 10000 ? Math.Min(normal, 10000) : normal), pair.Item1 + ": curve-first rule at " + old);
                    report.Check(old <= 10000 || final <= 10000, pair.Item1 + ": no current Beyond survives " + old);
                    samples.Add(new Sample { curve = pair.Item1, oldMmr = old, normalResult = normal, finalMmr = final,
                        beyondLevelBefore = MmrTierUtility.GetBeyondCounter(old), rankAfter = MmrTierUtility.GetDisplayName(final) });
                }
            }
            report.Check(lower.CalculateNormalSeasonReset(10500) == 9950 && lower.ResetMmr(10500) == 9950, "10500 -> 9950 stays below 10k without an artificial promotion");
            report.Check(higher.CalculateNormalSeasonReset(10500) == 10200 && higher.ResetMmr(10500) == 10000, "10500 -> normal 10200 -> final 10000");
            report.Check(higher.CalculateNormalSeasonReset(12500) == 10900 && higher.ResetMmr(12500) == 10000, "12500 -> normal 10900 -> final 10000");
            report.Check(higher.CalculateNormalSeasonReset(20000) == 14000 && higher.ResetMmr(20000) == 10000, "20000 -> normal 14000 -> final 10000");
            var reduction = new LocalLeagueConfiguration { resetCurve = new[] { new SoftResetPoint(0, 0), new SoftResetPoint(10100, 9600), new SoftResetPoint(20000, 14000) } };
            report.Check(reduction.CalculateNormalSeasonReset(10100) == 9600 && reduction.ResetMmr(10100) == 9600, "10100 -> 9600 retains the full normal reduction");
            report.Check(lower.ResetMmr(10000) == 9500, "Exactly 10000 takes the normal curve, with no Beyond floor");
            var rounding = new LocalLeagueConfiguration { resetCurve = new[] { new SoftResetPoint(0, 0), new SoftResetPoint(10000, 10000), new SoftResetPoint(10002, 10001) } };
            report.Check(rounding.CalculateNormalSeasonReset(10001) == 10001 && rounding.ResetMmr(10001) == 10000, "Integer rounding above the ceiling cannot retain 10001");
            var half = new LocalLeagueConfiguration { resetCurve = new[] { new SoftResetPoint(0, 0), new SoftResetPoint(20000, 10000) } };
            report.Check(half.CalculateNormalSeasonReset(10001) == 5001 && half.ResetMmr(10001) == 5001, "Existing away-from-zero half rounding below the ceiling is preserved");
            bool calculated = false;
            try { new LocalLeagueConfiguration { resetCurve = null }.ResetMmr(10001); }
            catch (InvalidOperationException) { calculated = true; }
            report.Check(calculated, "Beyond cannot bypass normal curve validation");
            File.WriteAllText(Path.Combine(output, "season-reset-samples.json"), JsonUtility.ToJson(new Samples { cases = samples.ToArray() }, true));
        }

        static LocalLeagueStore Store(string path) => new LocalLeagueStore(path, value => JsonUtility.ToJson(value), JsonUtility.FromJson<LocalLeagueState>);
        static void Transition(ClosureContractReport report, string output)
        {
            long epoch = new DateTime(2026, 1, 31, 12, 30, 0, DateTimeKind.Utc).Ticks;
            string freshPath = Path.Combine(output, "fresh-install"); var freshStore = Store(freshPath);
            var fresh = LocalLeagueEngine.Create(epoch, 4207);
            report.Check(freshStore.Save(fresh) && Store(freshPath).Load().bots.All(b => b.currentMmr == 0) && fresh.human.currentMmr == 0, "Fresh 10000-bot v3 install remains zero-rated");
            var state = LocalLeagueEngine.Create(epoch, 4207); var config = LowerCurve();
            state.currentSeasonNumber = 2; state.lastProcessedSeasonNumber = 1;
            state.seasonStartTimestamp = LocalSeasonEngine.Boundary(epoch, 1); state.seasonEndTimestamp = LocalSeasonEngine.Boundary(epoch, 2);
            state.lastKnownUtcTimestamp = state.seasonEndTimestamp;
            var oldArchive = new LeagueSeasonArchive { seasonNumber = 1, population = state.population, startTimestamp = epoch,
                endTimestamp = state.seasonStartTimestamp, records = new CompactBotSeason[state.population] };
            state.archives.Add(oldArchive);
            for (int i = 0; i < state.bots.Length; i++) {
                var bot = state.bots[i]; bot.currentMmr = Ratings[i % Ratings.Length];
                bot.peakMmr = Math.Max(25000, bot.currentMmr); bot.seasonPeakMmr = Math.Max(11250, bot.currentMmr); bot.seasonStartingMmr = 9000;
                bot.races = 123; bot.wins = 24; bot.podiums = 51; bot.seasonRaces = 12; bot.seasonWins = 3; bot.seasonPodiums = 6;
                bot.bestLap = 55; bot.seasonBestLap = 57; bot.titleFlags = 126; bot.selectedTitle = 6;
                // Preserve the selected career title; ordinary bots may select a newly earned season title.
                bot.personalitySeed &= ~3; state.bots[i] = bot;
                oldArchive.records[i] = new CompactBotSeason { startingMmr = 8000, endingMmr = 16500, peakMmr = 18000, nextStartingMmr = 9000, selectedTitle = 6 };
            }
            var human = state.human; human.currentMmr = 10500; human.peakMmr = 20000; human.seasonStartingMmr = 9000; human.seasonPeakMmr = 11250;
            human.races = 123; human.wins = 24; human.podiums = 51; human.bestLap = 55;
            human.seasonRaces = 12; human.seasonWins = 3; human.seasonPodiums = 6; human.seasonBestLap = 57;
            human.unlockedTitleIds.AddRange(new[] { "beyond-apex", "shortcut-hunter", "season:1:20" }); human.selectedTitleId = "beyond-apex";
            human.history.Add(new RacingSeasonHistory { seasonNumber = 1, endingMmr = 16500, peakMmr = 18000, beyondPeakCounter = 8, endingRank = "APEX BEYOND 10K" });
            var before = state.CopyForSave(); long boundary = state.seasonEndTimestamp;
            string path = Path.Combine(output, "existing-v3-transition"); var store = Store(path); store.Save(state);
            var engine = new LocalLeagueEngine(state, config);
            report.Check(!LocalSeasonEngine.AdvanceChunk(state, config, boundary - 1, 137, engine.Index), "A season cannot reset early");
            LocalSeasonEngine.AdvanceChunk(state, config, boundary, 137, engine.Index);
            report.Check(state.seasonTransitionCursor == 137 && store.Save(state), "Interrupted season saves the exact cursor and pending archive");
            var resumed = Store(path).Load();
            report.Check(resumed != null && resumed.version == 3 && resumed.seasonTransitionCursor == 137 && resumed.human.currentMmr == 10500,
                "Existing v3 journal relaunch resumes the partial transition without a migration");
            bool partial = true;
            for (int i = 0; i < resumed.bots.Length; i++) partial &= resumed.bots[i].currentMmr == (i < 137 ? config.ResetMmr(before.bots[i].currentMmr) : before.bots[i].currentMmr);
            report.Check(partial, "Partial relaunch neither skips nor resets an already processed bot twice");
            Finish(resumed, config, boundary); VerifyCompleted(report, resumed, before, config);
            var saved = Store(path); saved.Load(); report.Check(saved.Save(resumed), "Completed reset and both season archives save durably");
            var reloader = Store(path); var reloaded = reloader.Load();
            report.Check(reloaded.version == 3 && reloaded.currentSeasonNumber == 3 && !reloader.MigratedLegacy, "Relaunch keeps save schema v3 and the completed transition");
            var humanHistory = reloaded.human.history;
            report.Check(humanHistory.Count == 2 && humanHistory[0].endingMmr == 16500 && humanHistory[0].peakMmr == 18000 && humanHistory[0].beyondPeakCounter == 8,
                "Existing human career history survives unchanged");
            bool history = true;
            foreach (int bot in Enumerable.Range(0, Ratings.Length)) {
                var rows = reloader.ReadHistory(reloaded, bot); var old = before.bots[bot];
                history &= rows.Count == 2 && rows[0].endingMmr == 16500 && rows[0].peakMmr == 18000 && rows[0].beyondPeakCounter == 8;
                history &= rows[1].endingMmr == old.currentMmr && rows[1].peakMmr == old.seasonPeakMmr && rows[1].nextStartingMmr == config.ResetMmr(old.currentMmr)
                    && rows[1].beyondPeakCounter == MmrTierUtility.GetBeyondCounter(old.seasonPeakMmr) && rows[1].selectedTitleId == "beyond-apex";
            }
            report.Check(history, "Lazy bot history preserves previous finals, peaks, Beyond levels and earned titles for every tested boundary");
            int[] ratings = reloaded.bots.Select(b => b.currentMmr).ToArray();
            report.Check(!LocalSeasonEngine.AdvanceOne(reloaded, config, boundary) && !LocalSeasonEngine.AdvanceChunk(reloaded, config, boundary, 137)
                && ratings.SequenceEqual(reloaded.bots.Select(b => b.currentMmr)) && reloaded.archives.Count == 2 && reloaded.human.history.Count == 2,
                "Repeated completed season processing is idempotent after relaunch");
            // This directory belongs only to this fixture: simulate a torn latest journal and uncommitted temporary file.
            File.WriteAllBytes(Path.Combine(path, "league-v3-1.journal"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(path, "league-v3-0.journal.pending"), new byte[] { 4, 5, 6 });
            var recoveryStore = Store(path); var recovered = recoveryStore.Load();
            report.Check(recovered != null && !recoveryStore.ReadOnlyProtection && recovered.seasonTransitionCursor == 137,
                "Torn latest generation recovers the intact previous cursor; uncommitted temporary bytes are ignored");
            Finish(recovered, config, boundary);
            report.Check(recovered.human.currentMmr == reloaded.human.currentMmr && ratings.SequenceEqual(recovered.bots.Select(b => b.currentMmr))
                && recovered.human.history.Count == 2 && recovered.archives.Count == 2 && recoveryStore.Save(recovered),
                "Crash recovery produces identical human/bot ratings and one archive per season");
            report.Check(!LocalSeasonEngine.AdvanceOne(Store(path).Load(), config, boundary), "Crash-recovered transition remains idempotent on another relaunch");
        }
        static void Finish(LocalLeagueState state, LocalLeagueConfiguration config, long boundary)
        {
            var engine = new LocalLeagueEngine(state, config);
            while (state.currentSeasonNumber == 2) LocalSeasonEngine.AdvanceChunk(state, config, boundary, 137, engine.Index);
        }
        static void VerifyCompleted(ClosureContractReport report, LocalLeagueState after, LocalLeagueState before, LocalLeagueConfiguration config)
        {
            bool parity = true, career = true, combos = true, titles = true;
            for (int i = 0; i < after.bots.Length; i++) {
                var old = before.bots[i]; var bot = after.bots[i];
                parity &= bot.currentMmr == config.ResetMmr(old.currentMmr) && bot.currentMmr <= 10000 && bot.seasonStartingMmr == bot.currentMmr;
                career &= bot.peakMmr == old.peakMmr && bot.races == old.races && bot.wins == old.wins && bot.podiums == old.podiums && bot.bestLap == old.bestLap;
                combos &= bot.id == old.id && bot.vehicle == old.vehicle && bot.wheels == old.wheels && bot.character == old.character && bot.accessory == old.accessory;
                titles &= bot.titleFlags == old.titleFlags && bot.selectedTitle == old.selectedTitle;
            }
            report.Check(parity && after.human.currentMmr == after.bots[4].currentMmr && after.human.currentMmr == 9950, "Human and all 10000 persistent bots share the curve-first rule and cannot start in Beyond");
            report.Check(career && combos && titles, "All 10000 bots preserve all-time peaks, career statistics, IDs, combos and earned title flags");
            var human = after.human; var oldHuman = before.human; var entry = human.history[1];
            report.Check(human.peakMmr == oldHuman.peakMmr && MmrTierUtility.GetBeyondCounter(human.peakMmr) == 10
                && human.races == oldHuman.races && human.wins == oldHuman.wins && human.podiums == oldHuman.podiums && human.bestLap == oldHuman.bestLap
                && human.vehiclePreference == oldHuman.vehiclePreference && human.wheelPreference == oldHuman.wheelPreference
                && human.characterPreference == oldHuman.characterPreference && human.accessoryPreference == oldHuman.accessoryPreference,
                "Human all-time prestige, career statistics and combo survive a reset below 10k");
            report.Check(oldHuman.unlockedTitleIds.All(human.unlockedTitleIds.Contains) && human.selectedTitleId == oldHuman.selectedTitleId,
                "Earned human titles, including historical Beyond, are retained");
            report.Check(entry.endingMmr == 10500 && entry.peakMmr == 11250 && entry.beyondPeakCounter == 1 && entry.nextStartingMmr == 9950
                && entry.endingRank == "APEX BEYOND 10K" && entry.peakRank == "APEX BEYOND 10K", "Previous season retains final/peak ranks and Beyond while current rank is normal");
            var engine = new LocalLeagueEngine(after, config);
            var oracle = Enumerable.Range(0, after.bots.Length + 1).OrderByDescending(i => i == after.bots.Length ? human.currentMmr : after.bots[i].currentMmr).ThenBy(i => i).ToArray();
            report.Check(engine.Index.Valid() && oracle.SequenceEqual(Enumerable.Range(0, oracle.Length).Select(engine.Index.AtRank)),
                "Current leaderboard for 10000 bots plus human matches an independent sort of new-season MMR");
            report.Check(engine.Page(0, 14).All(c => c.currentMmr <= 10000 && MmrTierUtility.GetTier(c.currentMmr) != MmrTier.ApexBeyond),
                "Visible leaderboard rows cannot display current Beyond after reset");
        }

        static void Badge(ClosureContractReport report)
        {
            var oldScene = SceneManager.GetActiveScene();
            if (Application.isBatchMode && string.IsNullOrEmpty(oldScene.path)) oldScene = EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var fixture = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(fixture);
                var root = new GameObject("Isolated seasonal boundary badge", typeof(RectTransform)); var badge = root.AddComponent<RacingRankBadge>();
                foreach (int mmr in new[] { 9999, 10000, 10001 }) {
                    badge.Configure(mmr); bool beyond = mmr > 10000;
                    report.Check((MmrTierUtility.GetTier(mmr) == MmrTier.ApexBeyond) == beyond
                        && root.transform.Find("Beyond diamond counter").GetComponent<Text>().text == (beyond ? "0" : "")
                        && root.transform.Find("Live rank division").GetComponent<Text>().text == (beyond ? "APEX" : "II"),
                        "Current rank artwork and diamond label at " + mmr + " respect strict >10000 Beyond");
                }
                report.Check(MmrTierUtility.GetDivisionProgress(10000) == 1f && MmrTierUtility.GetBeyondCounter(10000) == 0
                    && MmrTierUtility.GetBeyondCounter(11000) == 1 && MmrTierUtility.GetBeyondCounter(20000) == 10,
                    "Exactly 10k completes normal Apex II; existing floor((MMR-10000)/1000) prestige is unchanged");
                foreach (int mmr in new[] { 9999, 10000, 10001 }) {
                    var owner = new GameObject("Isolated results boundary " + mmr); var manager = owner.AddComponent<RaceManager>(); manager.PersistResults = false;
                    var results = owner.AddComponent<RacingResultsUI>();
                    typeof(RacingResultsUI).GetField("raceManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(results, manager);
                    results.ShowResults(new RaceResults { entries = new[] { new RacerResultEntry { isHuman = true, displayName = "Boundary fixture", oldMmr = mmr,
                        newMmr = mmr, finishPosition = 1, finished = true, totalTime = 60, vehicleId = "car-01", wheelId = "wheel-default", titleId = "" } } });
                    var canvas = fixture.GetRootGameObjects().First(go => go.name == "Ranked results");
                    report.Check(canvas.GetComponentsInChildren<Text>(true).Any(text => text.text == "APEX BEYOND 10K") == (mmr > 10000),
                        "Actual results screen at " + mmr + " never labels a normal rank as current Beyond");
                    UnityEngine.Object.DestroyImmediate(canvas); UnityEngine.Object.DestroyImmediate(owner);
                }
            } finally { if (oldScene.IsValid() && oldScene.isLoaded) SceneManager.SetActiveScene(oldScene); EditorSceneManager.CloseScene(fixture, true); }
        }
    }
}
#endif
