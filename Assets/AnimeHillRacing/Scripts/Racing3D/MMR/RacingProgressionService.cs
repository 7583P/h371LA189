using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingProgressionService
    {
        static readonly ProfilerMarker SaveMarker=new ProfilerMarker("Racing.League.Save");
        public static long SaveRequestsDuringGameplay { get; private set; }
        public static long SaveSnapshotCopies { get; private set; }
        static LocalLeagueState state;
        static LocalLeagueEngine engine;
        static LocalLeagueStore store;
        static long pendingUtc;
        static bool dirty, raceActive, externalGameplay;
        static double lastSaveRealtime;
        static int revision;
        sealed class SaveOutcome { public bool committed; public double milliseconds; public string failure; public int lastArchivedSeason; }
        static Task<SaveOutcome> pendingSave;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static int diagnosticEvents;
        static bool diagnosticSchedulerPaused;
        public static int PendingDiagnosticEvents => diagnosticEvents;
#endif
        public static event Action Changed;
        public static LocalLeagueState State { get { EnsureInitialized(); return state; } }
        public static LocalLeagueEngine Engine { get { EnsureInitialized(); return engine; } }
        public static int Revision => revision;
        public static bool IsRaceActive => raceActive || externalGameplay;
        public static string SaveNotice => store == null ? "" : store.RecoveryNotice;
        public static double LastPumpMilliseconds { get; private set; }
        public static double LastSaveMilliseconds { get; private set; }
        public static double LastSaveWorkerMilliseconds { get; private set; }
        public static bool HasPendingSave => pendingSave != null;
        public static bool HasPendingSeasonTransition => State.seasonTransitionCursor >= 0 || pendingUtc >= state.seasonEndTimestamp;
        public static double StartupMilliseconds { get; private set; }
        public static long RacePumpAllocatedBytes { get; private set; }
        public static long RacePumpCalls { get; private set; }
        public static void ObserveRacePump(long allocated) { RacePumpCalls++; RacePumpAllocatedBytes+=allocated; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CompletePendingSave(true);state = null; engine = null; store = null; pendingUtc = 0; dirty = raceActive = externalGameplay = false; revision = 0; Changed = null; LastSaveMilliseconds=LastSaveWorkerMilliseconds=StartupMilliseconds=0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            diagnosticEvents=0;
            string[] diagnosticArgs=Environment.GetCommandLineArgs();
            int modeIndex=Array.IndexOf(diagnosticArgs,"--closure-mode");
            diagnosticSchedulerPaused=modeIndex>=0 && modeIndex+1<diagnosticArgs.Length && diagnosticArgs[modeIndex+1]=="product";
#endif
            RacePumpAllocatedBytes=RacePumpCalls=0;
            SaveRequestsDuringGameplay=SaveSnapshotCopies=0;
        }

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
            state = store.Load();
            dirty = store.MigratedLegacy;
            if (state == null) {
                long now = DateTime.UtcNow.Ticks;
                state = LocalLeagueEngine.Create(now, unchecked((int)(now ^ (now >> 32))), config.population);
                // Migrate existing human progression without altering Adventure or regenerating a saved league.
                var human = state.human;
                bool newUnits = PlayerPrefs.GetInt("AHR.Racing.RatingScaleMaximum", 0) == 10000;
                int current = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.MMR.V1", 0));
                int peak = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.HighestMMR.V1", 0));
                human.currentMmr = newUnits ? current : LegacyLeagueMigration.Rating(current);
                human.peakMmr = Mathf.Max(human.currentMmr, newUnits ? peak : LegacyLeagueMigration.Rating(peak));
                human.races = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.RacesCount.V1", 0));
                human.wins = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.WinsCount.V1", 0));
                human.podiums = Mathf.Max(0, PlayerPrefs.GetInt("AHR.Racing.PodiumsCount.V1", 0));
                human.seasonStartingMmr = human.seasonPeakMmr = human.currentMmr;
                human.uncertainty = Mathf.Max(.12f, Mathf.Pow(.975f, human.races));
                dirty = true;
            }
            engine = new LocalLeagueEngine(state, config);
            state.human.vehiclePreference=RacingSaveManager.SelectedVehicleId;
            state.human.wheelPreference=RacingSaveManager.SelectedWheelId;
            RacingTitleCatalog.UpdateAchievements(state.human);
            Resume(DateTime.UtcNow.Ticks);
            if (dirty) Save(true);
            if (!string.IsNullOrEmpty(store.RecoveryNotice)) UnityEngine.Debug.LogWarning("[Local league] " + store.RecoveryNotice);
            startup.Stop(); StartupMilliseconds = startup.Elapsed.TotalMilliseconds;
        }

        public static void Resume(long deviceUtcTicks)
        {
            if (state == null) { EnsureInitialized(); return; }
            long previousUtc = state.lastKnownUtcTimestamp;
            long accepted = LocalSeasonEngine.AcceptUtc(state, deviceUtcTicks);
            dirty |= accepted != previousUtc;
            pendingUtc = Math.Max(pendingUtc, accepted);
            long interval = TimeSpan.TicksPerHour / engine.EventsPerHour;
            long due = Math.Max(0, (pendingUtc - state.lastLeagueSimulationUtc) / interval);
            long skipped = Math.Max(0, due - Math.Max(1, engine.Configuration.maximumCatchupEvents));
            if (skipped > 0) {
                state.skippedCatchupEvents += skipped;
                state.lastLeagueSimulationUtc += skipped * interval;
                dirty = true;
            }
        }

        public static void SetRaceActive(bool active)
        {
            // Preparing -> Countdown -> Racing -> PlayerFinished all remain
            // active. A forced save here used to copy the 10k league and join
            // its worker again on every transition, including the finish frame.
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
            var timer = Stopwatch.StartNew(); bool changed = false;
            long interval = TimeSpan.TicksPerHour / engine.EventsPerHour;
            while (timer.Elapsed.TotalMilliseconds < Math.Max(.1, engine.Configuration.menuBudgetMilliseconds)) {
                long nextEvent = state.lastLeagueSimulationUtc + interval;
                if (state.seasonEndTimestamp <= Math.Min(nextEvent, pendingUtc)) {
                    // Commit/release a completed archive before starting another
                    // missed season. Work remains bounded even after years offline.
                    bool archiveNeedsCommit = false;
                    foreach (var archive in state.archives) archiveNeedsCommit |= archive.records != null;
                    if (archiveNeedsCommit && !store.ReadOnlyProtection) { Save(); break; }
                    LocalSeasonEngine.AdvanceChunk(state, engine.Configuration, pendingUtc, 64, engine.Index);
                    changed = dirty = true;
                    if (state.seasonTransitionCursor < 0) { Save(); break; }
                    continue;
                }
                if (nextEvent > pendingUtc) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if(diagnosticEvents > 0) {engine.SimulateOne();diagnosticEvents--;changed=dirty=true;continue;}
#endif
                    break;
                }
                engine.SimulateOne(); state.lastLeagueSimulationUtc = nextEvent;
                changed = dirty = true;
            }
            timer.Stop(); LastPumpMilliseconds = timer.Elapsed.TotalMilliseconds;
            if (changed) { revision++; Changed?.Invoke(); }
            if (dirty && Time.realtimeSinceStartupAsDouble - lastSaveRealtime > 2) Save();
        }

        public static void SetHumanMmr(int value)
        {
            var human = State.human; human.currentMmr = Math.Max(0, value);
            human.peakMmr = Math.Max(human.peakMmr, human.currentMmr); human.seasonPeakMmr = Math.Max(human.seasonPeakMmr, human.currentMmr);
            RacingTitleCatalog.UpdateAchievements(human); MarkChanged(); Save();
            engine.RefreshHumanIndex();
        }

        public static void RecordRankedResults(RaceResults results)
        {
            if (results == null || results.entries == null) return;
            foreach (var entry in results.entries) {
                if (entry.racerId != "player_human") {
                    if (!Engine.TryIndex(entry.racerId, out int index)) throw new InvalidOperationException("Ranked result contains a nonpersistent opponent.");
                    engine.RecordLiveResult(index, entry.oldMmr, entry.finishPosition, entry.mmrDelta, entry.bestLapTime, entry.shortcutSuccesses, entry.aerialSuccesses, entry.landingSuccesses);
                    continue;
                }
                var competitor = State.human;
                // Live race ratings are frozen at matchmaking. Offline simulation is paused until results are committed.
                competitor.currentMmr = entry.oldMmr;
                LocalLeagueEngine.RecordResult(competitor, entry.finishPosition, entry.mmrDelta, entry.isHuman ? entry.bestLapTime : 0);
                competitor.shortcutSuccesses += Math.Max(0, entry.shortcutSuccesses);
                competitor.aerialSuccesses += Math.Max(0, entry.aerialSuccesses);
                competitor.landingSuccesses += Math.Max(0, entry.landingSuccesses);
                RacingTitleCatalog.UpdateAchievements(competitor);
            }
            engine.RefreshHumanIndex(); MarkChanged(); Save();
        }

        public static LeagueCompetitor Find(string id)
        {
            if (id == "player_human") return State.human;
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
            if (!RacingTitleCatalog.Select(State.human, id)) return false;
            MarkChanged(); Save(); return true;
        }

        public static LeagueCompetitor[] LeaderboardPage(int first, int count) => Engine.Page(first, count);

        public static void RememberLeaderboardPosition(string id, int position)
        {
            if (id == "player_human") State.human.lastLeaderboardPosition = position;
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
            if(waitForCommit)CompletePendingSave(true);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void DiagnosticAdvance(TimeSpan elapsed) { EnsureInitialized(); Resume(Math.Max(pendingUtc, state.lastKnownUtcTimestamp) + elapsed.Ticks); }
        // Explicit native UI fixture timing only. Normal menus and race profiles
        // always start unpaused; no persisted setting or release UI uses this.
        public static void DiagnosticPauseScheduler(bool paused) { diagnosticSchedulerPaused=paused; }
        public static void DiagnosticSimulate(int events) {
            if (IsRaceActive) return;
            EnsureInitialized(); diagnosticEvents = (int)Math.Min(int.MaxValue, (long)diagnosticEvents + Math.Max(0, events));
        }
#endif
    }
}
