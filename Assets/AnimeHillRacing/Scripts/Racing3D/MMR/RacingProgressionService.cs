using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    // Unity host for the pure CPPS CompetitiveCoordinator: threading, journal
    // saves, PlayerPrefs mirrors and scene hooks. Rating rules, presence,
    // reservation, exact-once settlement and seasons live in the coordinator
    // (shared verbatim with the dotnet QA harness).
    public static class RacingProgressionService
    {
        static readonly ProfilerMarker SaveMarker=new ProfilerMarker("Racing.League.Save");
        public static long SaveRequestsDuringGameplay { get; private set; }
        public static long SaveSnapshotCopies { get; private set; }
        static LocalLeagueState state;
        static LocalLeagueEngine engine;
        static CompetitiveCoordinator coordinator;
        static LocalLeagueStore store;
        static SeasonHistoryStore historyStore;
        static Task pendingHistory,historyMaintenance;
        static double nextHistoryMaintenance;
        public static SeasonHistoryStore History {get{EnsureInitialized();return historyStore;}}
        public static string CareerHistoryDirectory {get{EnsureInitialized();return Path.Combine(store.Directory,"seasons");}}
        static bool dirty, raceActive, externalGameplay;
        static double lastSaveRealtime;
        static int revision;
        sealed class SaveOutcome { public bool committed; public double milliseconds; public string failure; public int lastArchivedSeason; }
        static Task<SaveOutcome> pendingSave;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static bool diagnosticSchedulerPaused;
        public static int PendingDiagnosticEvents => 0;
#endif
        public static event Action Changed;
        public static LocalLeagueState State { get { EnsureInitialized(); return state; } }
        public static LocalLeagueEngine Engine { get { EnsureInitialized(); return engine; } }
        public static CompetitiveCoordinator Coordinator { get { EnsureInitialized(); return coordinator; } }
        public static int Revision => revision;
        public static bool IsRaceActive => raceActive || externalGameplay;
        public static string SaveNotice => store == null ? "" : store.RecoveryNotice;
        public static string LastRecoveryAction { get; private set; } = "none";
        public static CppsMigrationReport LastMigration { get; private set; }
        public static double LastPumpMilliseconds { get; private set; }
        public static double LastSaveMilliseconds { get; private set; }
        public static double LastSaveWorkerMilliseconds { get; private set; }
        public static bool HasPendingSave => pendingSave != null;
        public static bool HasPendingSeasonTransition => State.seasonTransitionCursor >= 0 || state.lastKnownUtcTimestamp >= state.seasonEndTimestamp;
        public static double StartupMilliseconds { get; private set; }
        public static long RacePumpAllocatedBytes { get; private set; }
        public static long RacePumpCalls { get; private set; }
        public static void ObserveRacePump(long allocated) { RacePumpCalls++; RacePumpAllocatedBytes+=allocated; }

        // Anti-rollback watermark of the committed post-CPPS human state. A lost
        // or deleted journal restores it instead of granting a fresh Placement.
        const string WatermarkKey = "AHR.Racing.Cpps.Watermark.V1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CompletePendingSave(true);state = null; engine = null; coordinator = null; store = null; dirty = raceActive = externalGameplay = false; revision = 0; Changed = null; LastSaveMilliseconds=LastSaveWorkerMilliseconds=StartupMilliseconds=0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string[] diagnosticArgs=Environment.GetCommandLineArgs();
            int modeIndex=Array.IndexOf(diagnosticArgs,"--closure-mode");
            diagnosticSchedulerPaused=modeIndex>=0 && modeIndex+1<diagnosticArgs.Length && diagnosticArgs[modeIndex+1]=="product";
#endif
            RacePumpAllocatedBytes=RacePumpCalls=0;
            pendingHistory=null;historyMaintenance=null;historyStore=null;nextHistoryMaintenance=0;
            SaveRequestsDuringGameplay=SaveSnapshotCopies=0;
            LastRecoveryAction="none";LastMigration=null;
        }

        static long MonotonicMilliseconds => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

        public static void EnsureInitialized()
        {
            if (state != null) return;
            var startup = Stopwatch.StartNew();
            var authored = Resources.Load<RacingProgressionConfiguration>("RacingProgressionConfiguration");
            var config = authored != null ? authored.league : new LocalLeagueConfiguration();
            if (authored != null) RacingTitleCatalog.Configure(authored.titles);
            string directory = Path.Combine(Application.persistentDataPath, "LocalRacingLeague");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--closure-output") directory = Path.Combine(args[i + 1], "isolated-product-save");
            store = new LocalLeagueStore(directory, value => JsonUtility.ToJson(value), JsonUtility.FromJson<LocalLeagueState>, JsonUtility.FromJson<LegacyLocalLeagueState>, config.population);
            historyStore=new SeasonHistoryStore(Path.Combine(directory,"LeaderboardHistory"));
            state = store.Load();
            dirty = store.MigratedLegacy;
            long now = DateTime.UtcNow.Ticks;
            var watermark = ReadWatermark();
            if (state == null) {
                state = LocalLeagueEngine.Create(now, unchecked((int)(now ^ (now >> 32))), config.population, config);
                state.utcOffsetMinutes = (int)Math.Round(TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes);
                // Pre-CPPS installs kept their rating in preferences. CPPS starts
                // every competitor UNRANKED; the old value survives as career best.
                var human = state.human;
                bool newUnits = PlayerPrefs.GetInt("AHR.Racing.RatingScaleMaximum", 0) == 10000;
                int current = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.MMR.V1", 0));
                int peak = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.HighestMMR.V1", 0));
                human.peakMmr = Mathf.Max(newUnits ? current : LegacyLeagueMigration.Rating(current), newUnits ? peak : LegacyLeagueMigration.Rating(peak));
                human.races = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.RacesCount.V1", 0));
                human.wins = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.WinsCount.V1", 0));
                human.podiums = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.PodiumsCount.V1", 0));
                if (watermark != null) RestoreHuman(human, watermark);
                dirty = true;
            }
            else if (!CppsMigration.AlreadySatisfied(state)) {
                var currentStore = store;
                LastMigration = CppsMigration.Apply(state, config, now, archive => currentStore.LoadArchiveRecords(archive), watermark);
                dirty |= LastMigration.applied;
            }
            dirty|=SeasonQualification.Migrate(state);
            if(!store.ReadOnlyProtection)foreach(var past in state.archives)
                if(past.seasonNumber>0&&past.seasonNumber<state.currentSeasonNumber)
                    historyStore.MarkUnavailable(past.seasonNumber,"this completed season predates immutable leaderboard snapshots; protected career statistics remain available.");
            coordinator = new CompetitiveCoordinator(state, config,
                (pending, settlement) => store.SaveSession(new LocalLeagueStore.RankedSessionFile { pending = pending, settlement = settlement, profileId = state.profileId }, value => JsonUtility.ToJson(value)),
                () => store.ClearSession(),FinalizeHistory);
            engine = coordinator.Engine;
            state.human.vehiclePreference=RacingSaveManager.SelectedVehicleId;
            state.human.wheelPreference=RacingSaveManager.SelectedWheelId;
            RacingTitleCatalog.UpdateAchievements(state.human);
            Resume(now);
            // A crash/forced close may have left a reserved, started or durably
            // settled-but-unapplied ranked race. Resolve it exactly once.
            LocalLeagueStore.RankedSessionFile session = null;
            try { session = store.LoadSession(value => JsonUtility.FromJson<LocalLeagueStore.RankedSessionFile>(value)); }
            catch (Exception exception) { UnityEngine.Debug.LogWarning("[Local league] Ranked session file ignored: " + exception.Message); }
            LastRecoveryAction = coordinator.ResolvePending(state.lastKnownUtcTimestamp, session);
            if (LastRecoveryAction != "none") dirty = true;
            if (dirty) Save(true);
            if (!string.IsNullOrEmpty(store.RecoveryNotice)) UnityEngine.Debug.LogWarning("[Local league] " + store.RecoveryNotice);
            startup.Stop(); StartupMilliseconds = startup.Elapsed.TotalMilliseconds;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Explicit isolated QA opt-in; production remains the validated 10k
            // control until Android performance/storage gates approve deployment.
            if(Array.IndexOf(args,"--league-population-100000")>=0){
                CompletePendingSave(true);state=LeaguePopulationMigration.Expand(store,state,config);
                coordinator=new CompetitiveCoordinator(state,config,
                    (pending,settlement)=>store.SaveSession(new LocalLeagueStore.RankedSessionFile{pending=pending,settlement=settlement,profileId=state.profileId},value=>JsonUtility.ToJson(value)),
                    ()=>store.ClearSession(),FinalizeHistory);engine=coordinator.Engine;dirty=true;Save(true);
            }
            // Automated QA transfer hooks: import first, then export (round trip).
            for (int i = 0; i + 1 < args.Length; i++) {
                if (args[i] == "--league-import") { var r = ImportLeague(args[i + 1]); File.WriteAllText(args[i + 1] + ".import-result.txt", r.accepted ? "accepted " + r.header.exportId : "rejected " + r.failure); }
            }
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--league-export") ExportLeague(args[i + 1]);
#endif
        }

        static LeagueCompetitor ReadWatermark()
        {
            string json = PlayerPrefs.GetString(WatermarkKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { var w = JsonUtility.FromJson<LeagueCompetitor>(json); return w != null && !string.IsNullOrEmpty(w.id) ? w : null; }
            catch (ArgumentException) { return null; }
        }

        static void RestoreHuman(LeagueCompetitor h, LeagueCompetitor w)
        {
            h.currentMmr = w.currentMmr; h.placementGames = w.placementGames; h.provisionalRating = w.provisionalRating;
            h.seasonPeakMmr = Math.Max(h.currentMmr, w.seasonPeakMmr); h.seasonStartingMmr = w.seasonStartingMmr; h.streak = w.streak;
            h.uncertainty = w.uncertainty; h.peakMmr = Math.Max(h.peakMmr, w.peakMmr);
            h.qualifiedSeason=w.qualifiedSeason;h.assassinerEarned=w.assassinerEarned;
        }

        static bool FinalizeHistory(LocalLeagueState season,LeagueRankIndex ignored)
        {
            if(pendingHistory==null){
                var snapshot=season.CopyForSave();var sink=historyStore;
                pendingHistory=Task.Run(()=>{lock(sink)sink.Capture(snapshot,new LeagueRankIndex(snapshot,true));});return false;
            }
            if(!pendingHistory.IsCompleted)return false;
            pendingHistory.GetAwaiter().GetResult();pendingHistory=null;return true;
        }

        public static void Resume(long deviceUtcTicks)
        {
            if (state == null) { EnsureInitialized(); return; }
            long previousUtc = state.lastKnownUtcTimestamp;
            long accepted = coordinator.AcceptTime(deviceUtcTicks, MonotonicMilliseconds);
            dirty |= accepted != previousUtc;
        }

        public static void SetRaceActive(bool active)
        {
            // Preparing -> Countdown -> Racing -> PlayerFinished all remain
            // active. League simulation is paused for the whole physical race.
            if(active&&!raceActive&&state!=null)Save(true);
            raceActive=active;
            if(!active&&state!=null)Resume(DateTime.UtcNow.Ticks);
        }

        public static void SetExternalGameplay(bool active) {if(active && state!=null)Save(true);externalGameplay=active; if(!active && state!=null)Resume(DateTime.UtcNow.Ticks);}

        public static void Pump()
        {
            EnsureInitialized();
            if (IsRaceActive) { LastPumpMilliseconds=0; return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(diagnosticSchedulerPaused){LastPumpMilliseconds=0;return;}
#endif
            CompletePendingSave(false);
            if(Time.realtimeSinceStartupAsDouble>=nextHistoryMaintenance&&(historyMaintenance==null||historyMaintenance.IsCompleted)){
                if(historyMaintenance!=null&&historyMaintenance.IsFaulted)UnityEngine.Debug.LogWarning("[Season history] Maintenance deferred: "+historyMaintenance.Exception.GetBaseException().Message);
                nextHistoryMaintenance=Time.realtimeSinceStartupAsDouble+60;var sink=historyStore;int season=state.currentSeasonNumber;long wall=DateTime.UtcNow.Ticks,mono=MonotonicMilliseconds;
                historyMaintenance=Task.Run(()=>{lock(sink)sink.MaintainTrash(season,wall,mono);});
            }
            Resume(DateTime.UtcNow.Ticks);
            var timer = Stopwatch.StartNew(); bool changed = false;
            double budget = Math.Max(.1, engine.Configuration.menuBudgetMilliseconds);
            while (timer.Elapsed.TotalMilliseconds < budget) {
                // A finished season archive is committed before the next season
                // starts, so years offline never hold several archives in memory.
                if (state.lastKnownUtcTimestamp >= state.seasonEndTimestamp && state.seasonTransitionCursor < 0) {
                    bool archiveNeedsCommit = false;
                    foreach (var archive in state.archives) archiveNeedsCommit |= archive.records != null;
                    if (archiveNeedsCommit && !store.ReadOnlyProtection) { Save(); break; }
                }
                var status = coordinator.Step(256);
                if (status == CatchupStatus.CaughtUp) break;
                changed = dirty = true;
                if (status == CatchupStatus.NeedsCommit) { Save(); break; }
            }
            timer.Stop(); LastPumpMilliseconds = timer.Elapsed.TotalMilliseconds;
            if (changed) { revision++; Changed?.Invoke(); }
            if (dirty && Time.realtimeSinceStartupAsDouble - lastSaveRealtime > 2) Save();
        }

        // ---------------- ranked physical race lifecycle ----------------
        public static PendingRankedRace PendingRankedRace => State.pendingHumanRace != null && !string.IsNullOrEmpty(state.pendingHumanRace.raceId) ? state.pendingHumanRace : null;

        // Atomically reserves eleven real, available persistent bots. The
        // reservation is written to the small session journal immediately.
        public static PendingRankedRace ReserveRankedRace(int seed)
        {
            EnsureInitialized(); Resume(DateTime.UtcNow.Ticks);
            var pending = coordinator.BeginRankedRace(seed, state.lastKnownUtcTimestamp);
            MarkChanged();
            return pending;
        }

        public static void MarkRankedStarted(string raceId)
        {
            if (state == null || string.IsNullOrEmpty(raceId)) return;
            coordinator.MarkStarted(raceId, Math.Max(state.lastKnownUtcTimestamp, DateTime.UtcNow.Ticks));
        }

        // Leaving after GO rates only the human's pairs as an abandonment;
        // before GO the reservation is cancelled with no rating effect.
        public static RaceSettlementRecord AbandonRankedRace(string raceId)
        {
            if (state == null || PendingRankedRace == null) return null;
            var record = coordinator.AbandonOrCancel(raceId, Math.Max(state.lastKnownUtcTimestamp, DateTime.UtcNow.Ticks));
            AfterSettlement(); return record;
        }

        // Test/QA races that reserved real bots but never count.
        public static void CancelRankedRace(string raceId)
        {
            if (state == null || PendingRankedRace == null) return;
            if (coordinator.CancelReservation(raceId, Math.Max(state.lastKnownUtcTimestamp, DateTime.UtcNow.Ticks))) MarkChanged();
        }

        // Exactly-once settlement of the reserved race; result entries receive
        // the authoritative CPPS rating values for the results screen.
        public static void RecordRankedResults(RaceResults results)
        {
            if (results == null || results.entries == null) return;
            EnsureInitialized();
            string raceId = results.rankedRaceId;
            if (string.IsNullOrEmpty(raceId) || (PendingRankedRace?.raceId != raceId && !state.ledger.IsSettled(raceId)))
            { UnityEngine.Debug.LogWarning("[Local league] Ranked results without a reserved race were not rated."); return; }
            var physical = new PhysicalRacerResult[results.entries.Length];
            for (int i = 0; i < results.entries.Length; i++) {
                var e = results.entries[i];
                physical[i] = new PhysicalRacerResult { racerId = e.isHuman ? CompetitiveCoordinator.HumanId : e.racerId, isHuman = e.isHuman, finished = e.finished,
                    position = e.finishPosition, bestLap = e.bestLapTime, shortcuts = e.shortcutSuccesses, aerials = e.aerialSuccesses, landings = e.landingSuccesses };
            }
            var record = coordinator.SettlePhysicalRace(raceId, physical, Math.Max(state.lastKnownUtcTimestamp, DateTime.UtcNow.Ticks));
            ApplyRecordToResults(record, results);
            AfterSettlement();
        }

        static void ApplyRecordToResults(RaceSettlementRecord record, RaceResults results)
        {
            if (record == null) return;
            results.settlementState = record.state;
            foreach (var e in results.entries) {
                string id = e.isHuman ? CompetitiveCoordinator.HumanId : e.racerId;
                int k = Array.IndexOf(record.participantIds, id); if (k < 0) continue;
                e.oldMmr = record.previousMmr[k]; e.newMmr = record.finalMmr[k]; e.mmrDelta = record.deltaMmr[k];
                e.placementBefore = record.placementBefore[k]; e.placementAfter = record.placementAfter[k];
            }
        }

        static void AfterSettlement()
        {
            RacingTitleCatalog.UpdateAchievements(state.human);
            engine.RefreshHumanIndex(); MarkChanged(); Save(true);
            WriteWatermark();
        }

        static void WriteWatermark()
        {
            var h = state.human;
            var w = new LeagueCompetitor { id = h.id, currentMmr = h.currentMmr, placementGames = h.placementGames, provisionalRating = h.provisionalRating,
                seasonPeakMmr = h.seasonPeakMmr, seasonStartingMmr = h.seasonStartingMmr, streak = h.streak, uncertainty = h.uncertainty, peakMmr = h.peakMmr };
            string json = JsonUtility.ToJson(w);
            if (PlayerPrefs.GetString(WatermarkKey, "") != json) { PlayerPrefs.SetString(WatermarkKey, json); PlayerPrefs.Save(); }
        }

        public static void SetHumanMmr(int value)
        {
            var human = State.human;
            // QA/developer override only. A forced positive MMR is a ranked
            // standing; 0 leaves an UNRANKED player in Placement.
            if (value > 0 && !human.Ranked) { human.placementGames = CompetitiveRating.PlacementRaces; human.provisionalRating = value; }
            human.currentMmr = human.Ranked ? Math.Max(0, value) : 0;
            human.peakMmr = Math.Max(human.peakMmr, human.currentMmr); human.seasonPeakMmr = Math.Max(human.seasonPeakMmr, human.currentMmr);
            RacingTitleCatalog.UpdateAchievements(human); MarkChanged(); Save();
            engine.RefreshHumanIndex();
        }

        public static LeagueCompetitor Find(string id)
        {
            if (id == CompetitiveCoordinator.HumanId) return State.human;
            return Engine.TryIndex(id, out int index) ? engine.View(index) : null;
        }

        public static Task<List<RacingSeasonHistory>> ReadBotHistory(string id)
        {
            EnsureInitialized();
            if (!engine.TryIndex(id, out int index)) return Task.FromResult(new List<RacingSeasonHistory>());
            // Snapshot only the retained archive metadata/records; a profile
            // never copies or expands the entire league.
            var metadata = new LocalLeagueState();
            foreach (var archive in state.archives) metadata.archives.Add(archive.CopyForSave());
            var destination = store;
            return Task.Run(() => destination.ReadHistory(metadata, index));
        }

        public static bool SelectHumanTitle(string id)
        {
            if (!RacingTitleCatalog.Select(State.human, id,State.currentSeasonNumber,Engine.OfficialPosition(State.human.id))) return false;
            MarkChanged(); Save(); return true;
        }

        public static LeagueCompetitor[] LeaderboardPage(int first, int count) => Engine.Page(first, count);

        public static void RememberLeaderboardPosition(string id, int position)
        {
            if (id == CompetitiveCoordinator.HumanId) State.human.lastLeaderboardPosition = position;
            else if (Engine.TryIndex(id, out int index)) { var b=state.bots[index]; b.lastLeaderboardPosition=position; state.bots[index]=b; }
            dirty = true;
        }

        public static void MarkChanged() { dirty = true; revision++; Changed?.Invoke(); }
        static bool CompletePendingSave(bool wait)
        {
            if(pendingSave==null)return true;
            if(!wait && !pendingSave.IsCompleted)return false;
            var result=pendingSave.GetAwaiter().GetResult();pendingSave=null;
            LastSaveWorkerMilliseconds=result.milliseconds;
            if(!result.committed)dirty=true;
            else foreach (var archive in state.archives) if (archive.seasonNumber <= result.lastArchivedSeason) archive.records=null;
            if(!string.IsNullOrEmpty(result.failure))UnityEngine.Debug.LogError("[Local league] Save failed: "+result.failure);
            return true;
        }

        public static void Save(bool waitForCommit=false)
        { using(SaveMarker.Auto()) SaveInternal(waitForCommit); }
        static void SaveInternal(bool waitForCommit)
        {
            if(IsRaceActive)SaveRequestsDuringGameplay++;
            if(!CompletePendingSave(waitForCommit) || state==null || store==null || !dirty || store.ReadOnlyProtection || (IsRaceActive && !waitForCommit))return;
            var timer = Stopwatch.StartNew();
            var snapshot=state.CopyForSave();SaveSnapshotCopies++;var destination=store;
            pendingSave=Task.Run(()=>{
                var workerTimer=Stopwatch.StartNew();var result=new SaveOutcome();
                try{result.committed=destination.Save(snapshot); if(snapshot.archives.Count>0)result.lastArchivedSeason=snapshot.archives[snapshot.archives.Count-1].seasonNumber;}
                catch(Exception exception){result.failure=exception.ToString();}
                workerTimer.Stop();result.milliseconds=workerTimer.Elapsed.TotalMilliseconds;return result;
            });
            // Mirror legacy human keys on the Unity thread; bot-only activity
            // does not flush unchanged registry/preferences values.
            var h=state.human;bool mirror=false;
            void Mirror(string key,int value){if(PlayerPrefs.GetInt(key,-1)!=value){PlayerPrefs.SetInt(key,value);mirror=true;}}
            Mirror("AHR.Racing.MMR.V1",h.currentMmr);Mirror("AHR.Racing.HighestMMR.V1",h.peakMmr);
            Mirror("AHR.Racing.RatingScaleMaximum",10000);
            Mirror("AHR.Racing.RacesCount.V1",h.races);Mirror("AHR.Racing.WinsCount.V1",h.wins);Mirror("AHR.Racing.PodiumsCount.V1",h.podiums);
            if(mirror)PlayerPrefs.Save();dirty=false;
            timer.Stop(); LastSaveMilliseconds = timer.Elapsed.TotalMilliseconds; lastSaveRealtime = Time.realtimeSinceStartupAsDouble;
            if(waitForCommit){CompletePendingSave(true);WriteWatermark();}
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ---------------- offline developer/QA export & import ----------------
        // Not compiled into release players: a player-side import could roll a
        // competitive profile back. See LeagueTransfer for the exact contents.
        public static LeagueExportHeader ExportLeague(string path)
        {
            EnsureInitialized(); CompletePendingSave(true);
            dirty = true; Save(true);
            var bytes = store.ReadNewestJournalBytes();
            var package = LeagueTransfer.Export(state, bytes, state.lastKnownUtcTimestamp, Application.version, header => JsonUtility.ToJson(header, true), out var written);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, package);
            return written;
        }

        public static LeagueImportResult ImportLeague(string path)
        {
            EnsureInitialized(); CompletePendingSave(true);
            var currentStore = store;
            var result = LeagueTransfer.Read(File.ReadAllBytes(path), JsonUtility.FromJson<LeagueExportHeader>,
                (byte[] journal, out long generation, out bool keyMatched) => currentStore.ParseJournalBytes(journal, out generation, out keyMatched), state);
            if (!result.accepted) { UnityEngine.Debug.LogWarning("[Local league] Import rejected: " + result.failure); return result; }
            // Any reservation in the replaced league is discarded with it.
            store.ClearSession();
            state = result.state;
            coordinator = new CompetitiveCoordinator(state, engine.Configuration,
                (pending, settlement) => store.SaveSession(new LocalLeagueStore.RankedSessionFile { pending = pending, settlement = settlement, profileId = state.profileId }, value => JsonUtility.ToJson(value)),
                () => store.ClearSession(),FinalizeHistory);
            engine = coordinator.Engine;
            // New clock session: T0 (export) -> now is accepted as real absence.
            Resume(DateTime.UtcNow.Ticks);
            dirty = true; Save(true); MarkChanged();
            return result;
        }

        // Ends the clock session first: an in-session forward jump is otherwise
        // (correctly) rejected as clock manipulation.
        public static void DiagnosticAdvance(TimeSpan elapsed) { EnsureInitialized(); coordinator.Clock.EndSession(); Resume(state.lastKnownUtcTimestamp + elapsed.Ticks); }
        // Explicit native UI fixture timing only. Normal menus and race profiles
        // always start unpaused; no persisted setting or release UI uses this.
        public static void DiagnosticPauseScheduler(bool paused) { diagnosticSchedulerPaused=paused; }
        // League events are time driven under CPPS: advance the accepted clock
        // by a typical per-event interval instead of forcing synthetic events.
        public static void DiagnosticSimulate(int events) {
            if (IsRaceActive) return;
            DiagnosticAdvance(TimeSpan.FromSeconds(Math.Max(0, events) * 15.0));
        }
#endif
    }
}
