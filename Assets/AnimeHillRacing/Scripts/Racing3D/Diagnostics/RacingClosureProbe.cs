#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public struct ClosureDriveSample
    {
        public float time, speed, steering, throttle, braking, lookahead, curvature, targetSpeed;
        public float lateralOffset, lineOffset, distanceFromCenter, trackWidth, slip, upcomingTurn;
        public int currentNode, targetNode, lap, checkpoint,routeSegment,item,routeIndex,wallContacts,racerContacts,itemHits;
        public float airborneSeconds,lastJumpAge;
        public string routeAbort,intent;
        public bool shortcut, grounded, offroad, drifting, spunOut, boosting,lancer,miniHop;
        public Vector3 position, forward, target, velocity;
    }
    [Serializable]
    public sealed class ClosureRecoveryTrace
    {
        public string racerId, name, reason;
        public int mmr, seed, recoveryNumber;
        public List<ClosureDriveSample> before = new List<ClosureDriveSample>();
    }
    [Serializable]
    public sealed class ClosureRaceResult
    {
        public string classification = "SCRIPTED_UI_AND_REAL_UNITY_PHYSICS";
        public int seed, playerMmr, racers, humans, bots, movingBeforeGo, finished, checkpointVisits;
        public int playerPosition, maxRecoveries, totalRecoveries, runtimeErrors;
        public int stalledRacers, underTrackRacers;
        public float timeScale, raceSeconds;
        public string winner, state;
        public bool returnedToMenu, validFinishes, validDnfTimes, samePhysics, passed;
    }
    [Serializable]
    public sealed class ClosureSuiteResult
    {
        public string classification = "SCRIPTED_REAL_UNITY_PLAY_MODE";
        public List<ClosureRaceResult> races = new List<ClosureRaceResult>();
        public int errors, warnings;
        public bool passed;
    }
    public sealed class RacingClosureProbe : MonoBehaviour
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LaunchStandaloneValidation()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--closure-player")<0)return;
            string Value(string key,string fallback){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
            new GameObject("Explicit development build validation").AddComponent<RacingClosureProbe>().Begin(
                Value("--closure-output","Logs/RacingPhase2/closure/player"),Value("--closure-mode","profile"),Value("--closure-seeds","4207"),
                float.Parse(Value("--closure-timescale","1"),System.Globalization.CultureInfo.InvariantCulture),int.Parse(Value("--closure-mmr","1000")),Array.IndexOf(args,"--closure-capture")>=0);
        }
#endif
        sealed class TrackedDriver
        {
            public RacerProgress racer;
            public AiVehicleController driver;
            public ClosureDriveSample[] ring = new ClosureDriveSample[120];
            public int cursor, count, traceNumber;
            public float nextEdgeTrace;
        }
        sealed class BufferedTrace
        {
            public TrackedDriver driver;
            public string reason, prefix;
            public int count, number, recoveryNumber;
            public readonly ClosureDriveSample[] before=new ClosureDriveSample[121];
        }
        static readonly bool bufferedDrivingTraces=Array.IndexOf(Environment.GetCommandLineArgs(),"--buffered-driving-traces")>=0;
        static readonly ProfilerMarker TraceIoMarker=new ProfilerMarker("Racing.Diagnostics.TraceIO");
        public int SynchronousTraceWrites { get; private set; }
        public double SynchronousTraceWriteMs { get; private set; }
        BufferedTrace[] traceBuffer;
        int bufferedTraceCount, droppedTraces;
        string output, mode, raceOutput;
        int[] seeds;
        static readonly int[] BenchmarkRatings = {100,1000,2500,5000,7500,9000,9700,10000};
        int[] ratings = BenchmarkRatings;
        int currentSeed, playerMmr, errorsAtStart;
        float simulationScale, nextSample;
        bool capture, watchingPreGo;
        int movedBeforeGo;
        RaceManager manager;
        readonly List<TrackedDriver> drivers = new List<TrackedDriver>(12);
        readonly ClosureSuiteResult suite = new ClosureSuiteResult();
#if UNITY_EDITOR
        RacingFinalQa finalQa;
#endif

        public void Begin(string directory, string validationMode, string seedList, float scale, int mmr, bool screenshots)
        {
            output = Path.GetFullPath(directory);
            mode = validationMode;
            string[] seedStrings = seedList.Split(',');
            seeds = new int[seedStrings.Length];
            for (int i = 0; i < seeds.Length; i++) seeds[i] = int.Parse(seedStrings[i]);
            simulationScale = Mathf.Clamp(scale, 1, 6);
            playerMmr = mmr;
            string[] arguments = Environment.GetCommandLineArgs();
            int ratingFlag = Array.IndexOf(arguments, "--closure-ratings");
            if (ratingFlag >= 0 && ratingFlag + 1 < arguments.Length)
            {
                string[] values = arguments[ratingFlag + 1].Split(',');
                ratings = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    ratings[i] = int.Parse(values[i]);
                    if (ratings[i] < 0 || ratings[i] > 10000) throw new ArgumentOutOfRangeException("--closure-ratings");
                }
            }
            capture = screenshots;
            Directory.CreateDirectory(output);
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            Application.logMessageReceived += Log;
            SceneManager.sceneLoaded += Loaded;
            RacingSaveManager.CurrentMmr = mmr;
            if (mode == "profile") RacingQualityScope.Select(RacingQuality.High);
            StartCoroutine(Run());
        }
        void Loaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name != "Racing3D") return;
            manager = FindFirstObjectByType<RaceManager>();
            if (manager == null) return;
            var configuration = RaceRules.CreateRanked(currentSeed);
            configuration.EqualBuildBenchmark = mode == "benchmark" || mode == "profile";
            configuration.BenchmarkMmr=configuration.EqualBuildBenchmark?playerMmr:-1;
            manager.Configure(configuration);
            manager.PersistResults = mode == "final-qa";
#if !UNITY_EDITOR
            if (mode == "profile")
            {
                var rendered = GetComponent<RacingRenderedPlayerProbe>();
                if (rendered == null) rendered = gameObject.AddComponent<RacingRenderedPlayerProbe>();
                rendered.Bind(Camera.main, manager);
                if (RacingFrameForensics.Requested)
                {
                    var forensic = GetComponent<RacingFrameForensics>() ?? gameObject.AddComponent<RacingFrameForensics>();
                    forensic.Bind(manager);
                }
            }
#endif
        }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Warning)
            {
                suite.warnings++;
                File.AppendAllText(Path.Combine(output, "warnings.txt"), message + "\n");
            }
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            suite.errors++;
            File.AppendAllText(Path.Combine(output, "errors.txt"), message + "\n" + stack + "\n");
        }
        IEnumerator Run()
        {
#if UNITY_EDITOR
            if (mode == "contracts")
            {
                raceOutput=output;
                var contracts = new ClosureContractReport();
                yield return RacingClosureContracts.ItemsAndDrift(contracts, output);
                var ui=new ClosureContractReport();yield return RacingImprovementContracts.Menu(ui,capture?Capture:null);
                File.WriteAllText(Path.Combine(output,"improvement-ui-contracts.json"),JsonUtility.ToJson(ui,true));
                Finish(contracts.failed == 0 && ui.failed==0 && SessionState.GetBool("AHR.Racing.Closure.ContractsPassed", false) && suite.errors == 0);
                yield break;
            }
#endif
            if (mode == "texture-smoke")
            {
                foreach (string scene in new[] { "MainMenu", "Garage", "Adventure2_5D", "PreStart" })
                {
                    if (scene == "PreStart") AnimeHillRacing.Core.GameSession.BeginAdventureTrackSelection();
                    yield return SceneManager.LoadSceneAsync(scene);
                    yield return new WaitForSecondsRealtime(scene == "Adventure2_5D" ? 8 : 4);
                    CaptureTextureScene(scene);
                    yield return new WaitForSecondsRealtime(1);
                    if (scene == "Garage")
                    {
                        var garage = FindFirstObjectByType<AnimeHillRacing.UI.GarageUI>();
                        if (garage != null)
                        {
                            garage.ShowStore();
                            yield return new WaitForSecondsRealtime(3);
                            foreach (float scrollPosition in new[] { 1f, .75f, .5f, .25f, 0f })
                            {
                                foreach (var scroll in FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))
                                    if (scroll.isActiveAndEnabled && scroll.vertical) scroll.verticalNormalizedPosition = scrollPosition;
                                yield return new WaitForSecondsRealtime(2);
                            }
                            CaptureTextureScene("Store");
                            garage.ShowTuning();
                            yield return new WaitForSecondsRealtime(3);
                            CaptureTextureScene("Tuning");
                            garage.ShowUpgrade();
                            yield return new WaitForSecondsRealtime(3);
                            garage.ShowSkins();
                            yield return new WaitForSecondsRealtime(3);
                            garage.ShowSettings();
                            yield return new WaitForSecondsRealtime(3);
                        }
                    }
                    if (scene == "PreStart")
                    {
                        FindFirstObjectByType<AnimeHillRacing.UI.PreStartMenuUI>()?.Adventure();
                        yield return new WaitForSecondsRealtime(3);
                        foreach (float scrollPosition in new[] { 1f, .75f, .5f, .25f, 0f })
                        {
                            foreach (var scroll in FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))
                                if (scroll.isActiveAndEnabled && scroll.vertical) scroll.verticalNormalizedPosition = scrollPosition;
                            yield return new WaitForSecondsRealtime(2);
                        }
                        CaptureTextureScene("AdventureTrackSelection");
                    }
                }
                foreach (string scene in new[] { "RacingSelect", "RacingGarage", "Racing3D" })
                {
                    yield return SceneManager.LoadSceneAsync(scene);
                    yield return new WaitForSecondsRealtime(4);
                    CaptureTextureScene(scene);
                }
                Finish(suite.errors == 0);
                yield break;
            }
            // These are real Button callbacks. The human slot is driven by a test driver,
            // so this must never be reported as subjective human driving approval.
            int raceCount = seeds.Length * (mode == "benchmark" ? ratings.Length : 1);
            for (int race = 0; race < raceCount; race++)
            {
                currentSeed = seeds[race % seeds.Length];
                if (mode == "benchmark") playerMmr = ratings[race / seeds.Length];
                RacingSaveManager.CurrentMmr = playerMmr;
                raceOutput = mode == "benchmark" ? Path.Combine(output,"mmr-"+playerMmr,"seed-"+currentSeed) : Path.Combine(output, "seed-" + currentSeed);
                Directory.CreateDirectory(raceOutput);
                errorsAtStart = suite.errors;
                drivers.Clear();
                manager = null;
                Time.timeScale = 1;
                if (SceneManager.GetActiveScene().name != "PreStart") SceneManager.LoadScene("PreStart");
                yield return new WaitForSecondsRealtime(mode == "legacy-repro" ? 2 : .8f);
                Capture("01-main-menu");
                Capture("02-play-selection");
                if (mode == "legacy-repro") SceneManager.LoadScene("RacingSelect");
                else { yield return WaitButton("RACING");Click("RACING"); }
                yield return new WaitForSecondsRealtime(mode == "legacy-repro" ? 2 : .6f);
                Capture("03-racing-hub");
                if (mode == "legacy-repro") SceneManager.LoadScene("RacingGarage");
                else {yield return WaitButton("RANKED");Click("RANKED");}
                yield return new WaitForSecondsRealtime(mode == "legacy-repro" ? 2 : .6f);
                Capture("04-garage");
                if (mode == "legacy-repro")
                {
                    SceneManager.LoadScene("Racing3D");
                    yield return new WaitForSecondsRealtime(1.5f);
                }
                else
                {
                    yield return WaitButton("CONFIRM BUILD");Click("CONFIRM BUILD");
                    yield return new WaitForSecondsRealtime(.4f);
                    Capture("05-pre-race");
                    yield return WaitButton("START RACE");Click("START RACE");
                }
                float loadDeadline = Time.realtimeSinceStartup + 20;
                while ((manager == null || manager.SpawnedVehicles.Count < 12) && Time.realtimeSinceStartup < loadDeadline)
                    yield return null;
                if (manager == null || manager.SpawnedVehicles.Count != 12)
                {
                    Debug.LogError("Closure validation did not reach a 12-racer starting grid.");
                    Finish(false);
                    yield break;
                }
                var playerDriver = manager.PlayerVehicle.gameObject.AddComponent<AiVehicleController>();
                playerDriver.Initialize(AiSkillModel.CreateProfile(playerMmr, currentSeed), manager.ProgressTracker.TrackGraph,
                    manager.ProgressTracker, "SCRIPTED_PLAYER");
                manager.PlayerVehicle.SetCommandProvider(playerDriver);
                manager.PlayerVehicle.GetComponent<ML.RacingTelemetryRecorder>()?.MarkScripted();
                var actualTrack=FindFirstObjectByType<RacingTrackInfo>();
                string telemetryTrack=actualTrack!=null&&actualTrack.TrackId!="night_city"?actualTrack.TrackId:"NightCityGrandCircuit";
                foreach(var car in manager.SpawnedVehicles)
                {
                    var recorder=car.GetComponent<ML.RacingTelemetryRecorder>()??car.gameObject.AddComponent<ML.RacingTelemetryRecorder>();
                    var progress=manager.ProgressTracker.FindRacer(car);
                    recorder.SetSink(new ML.LocalTelemetrySink(Path.Combine(output,"telemetry")));
                    recorder.StartSession(telemetryTrack,currentSeed,progress.Identity.mmr,false);
                    if (RacingFrameForensics.Requested && (RacingFrameForensics.ReducedDiagnostics || RacingFrameForensics.Variant == "telemetry-off")) recorder.enabled = false;
                    var ml=car.GetComponent<ML.RacingMlInferenceAdapter>();
                    if(ml!=null)
                    {
                        string[] args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--closure-ml");
                        if(flag>=0&&flag+1<args.Length&&Enum.TryParse(args[flag+1],true,out ML.MlExecutionMode requested))ml.executionMode=requested;
                    }
                }
#if UNITY_EDITOR
                if (mode == "final-qa")
                {
                    finalQa = gameObject.AddComponent<RacingFinalQa>();
                    finalQa.Begin(manager, raceOutput);
                }
#endif
                foreach (var racer in manager.ProgressTracker.Leaderboard)
                {
                    var tracked = new TrackedDriver { racer = racer, driver = racer.Vehicle.GetComponent<AiVehicleController>() };
                    drivers.Add(tracked);
                    racer.Recovery.OnRecoveryRequested += reason => WriteTrace(tracked, reason);
                }
                bufferedTraceCount=droppedTraces=0;
                if(bufferedDrivingTraces && traceBuffer==null)
                {
                    // Open-edge runs also capture near misses. The former 128
                    // slots dropped recovery histories after these filled it.
                    // Allocate only for requested traces, before GO; export later.
                    traceBuffer=new BufferedTrace[512];
                    for(int i=0;i<traceBuffer.Length;i++)traceBuffer[i]=new BufferedTrace();
                }
                var metrics = FindFirstObjectByType<RacingRaceMetrics>();
                if (RacingEliteForensics.Requested)
                {
                    gameObject.AddComponent<RacingEliteForensics>().Bind(manager);
                    if (!RacingFrameForensics.Requested) gameObject.AddComponent<RacingShortcutTimingProbe>().Bind(manager);
                }
                if(RacingFrameForensics.Requested)gameObject.AddComponent<RacingShortcutTimingProbe>().Bind(manager);
                if (metrics != null) metrics.ExportPath = Path.Combine(raceOutput, "metrics.json");
                movedBeforeGo = 0;
                watchingPreGo = true;
                Capture("06-starting-grid");
#if !UNITY_EDITOR
                if (mode == "profile" && !RacingFrameForensics.Requested) GetComponent<RacingRenderedPlayerProbe>().SaveFrame(Path.Combine(raceOutput, "06-rendered-grid.png"));
#endif
                Time.timeScale = simulationScale;
                Time.maximumDeltaTime = Mathf.Max(.333f, simulationScale / 10f);
                if (mode == "profile") Application.targetFrameRate = -1;
                if (RacingFrameForensics.Requested)
                {
                    if (RacingFrameForensics.Variant == "hud-off")
                    {
                        var hud = FindFirstObjectByType<RacingHUD>(); if (hud != null) hud.enabled = false;
                        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (canvas.name == "Racing HUD") canvas.gameObject.SetActive(false);
                    }
                    if (RacingFrameForensics.Variant == "particles-off") foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                float deadline = Time.realtimeSinceStartup + 600 / simulationScale + 60;
                float nextCapture = 15;
                var captured = new HashSet<string>();
                while (manager.State != RaceState.Results && Time.realtimeSinceStartup < deadline)
                {
                    if (RacingFrameForensics.Requested && GetComponent<RacingFrameForensics>().WindowComplete) break;
                    if (manager.State == RaceState.Racing) watchingPreGo = false;
                    if (capture && manager.RaceElapsed > nextCapture)
                    {
                        nextCapture += 55;
                        Capture("07-gameplay-" + Mathf.RoundToInt(manager.RaceElapsed));
                    }
                    if (capture && manager.RaceElapsed > 8)
                    {
                        var car = manager.PlayerVehicle;
                        manager.ProgressTracker.TrackGraph.ProjectPosition(car.transform.position, out int node, out _, out _);
                        string feature = node >= 53 && node <= 62 ? "bridge-jump" :
                            node >= 24 && node <= 36 ? "tunnel" : car.IsDrifting ? "drift" :
                            car.IsBoosting ? "item-boost" : null;
                        if (feature != null && captured.Add(feature)) Capture("08-" + feature);
                        if (manager.State == RaceState.PlayerFinished && captured.Add("finish")) Capture("09-finish");
                    }
                    yield return null;
                }
                metrics?.StopMeasurement();
                GetComponent<RacingEliteForensics>()?.Export(raceOutput);
                if (GetComponent<RacingEliteForensics>() != null) Destroy(GetComponent<RacingEliteForensics>());
                GetComponent<RacingFrameForensics>()?.Export(raceOutput);
                GetComponent<RacingShortcutTimingProbe>()?.Export(raceOutput);
                if(GetComponent<RacingShortcutTimingProbe>()!=null)Destroy(GetComponent<RacingShortcutTimingProbe>());
                ExportBufferedTraces();
                Time.timeScale = 1;
                watchingPreGo = false;
                bool reachedResults=manager.State==RaceState.Results;
                var result = Summarize();
                yield return new WaitForSecondsRealtime(.7f);
                Capture(reachedResults ? "10-results" : "10-race-timeout");
#if !UNITY_EDITOR
                if (mode == "profile")
                {
                    var rendered = GetComponent<RacingRenderedPlayerProbe>();
                    if (!RacingFrameForensics.Requested) rendered.SaveFrame(Path.Combine(raceOutput, reachedResults ? "10-rendered-results.png" : "10-rendered-race-timeout.png"));
                    rendered.Export(Path.Combine(raceOutput, "render-proof.json"));
                }
#endif
                if (metrics != null && !File.Exists(Path.Combine(raceOutput, "metrics.json")))
                    metrics.Export(Path.Combine(raceOutput, "metrics.json"));
#if UNITY_EDITOR
                if (finalQa != null) yield return finalQa.Guarded(finalQa.AfterRace());
#endif
                drivers.Clear();
                if(reachedResults)
                {
                    Click("RACING HUB");
                    yield return new WaitForSecondsRealtime(.6f);
                    float returnDeadline=Time.realtimeSinceStartup+20;
                    while(SceneManager.GetActiveScene().name!="RacingSelect"&&Time.realtimeSinceStartup<returnDeadline)yield return null;
                    result.returnedToMenu = SceneManager.GetActiveScene().name == "RacingSelect";
                }
                else
                {
                    Debug.LogWarning("CLOSURE_RACE_TIMEOUT seed="+currentSeed+"; failed race retained, QA scene cleanup only.");
                    result.returnedToMenu=false;
                    yield return SceneManager.LoadSceneAsync("RacingSelect");
                }
                result.runtimeErrors = suite.errors - errorsAtStart;
                result.passed = result.state == "Results" && result.racers == 12 && result.humans == 1 && result.bots == 11 &&
                    result.movingBeforeGo == 0 && result.maxRecoveries <= 2 && result.runtimeErrors == 0 &&
                    result.stalledRacers == 0 && result.underTrackRacers == 0 &&
                    result.validFinishes && result.validDnfTimes && result.samePhysics && result.returnedToMenu;
                suite.races.Add(result);
                File.WriteAllText(Path.Combine(raceOutput, "race.json"), JsonUtility.ToJson(result, true));
                File.WriteAllText(Path.Combine(output, "suite.json"), JsonUtility.ToJson(suite, true));
                Debug.Log($"CLOSURE_RACE seed={currentSeed} state={result.state} player=P{result.playerPosition} recoveries={result.maxRecoveries}/{result.totalRecoveries} errors={result.runtimeErrors} pass={result.passed}");
            }
            bool passed = suite.errors == 0;
            foreach (var result in suite.races) passed &= result.passed;
#if UNITY_EDITOR
            if (finalQa != null)
            {
                yield return finalQa.Guarded(finalQa.Adventure());
                finalQa.Export();
                passed &= finalQa.Checks.failed == 0 && suite.errors == 0;
            }
#endif
            Finish(passed);
        }
        ClosureRaceResult Summarize()
        {
            foreach(var racer in manager.ProgressTracker.Leaderboard)
            {
                var recorder=racer.Vehicle.GetComponent<ML.RacingTelemetryRecorder>();
                var metric=FindFirstObjectByType<RacingRaceMetrics>()?.Report.racers.Find(r=>r.physics==racer.Vehicle);
                recorder?.EndSession(racer.RacePosition,racer.Finished?racer.FinishTime:manager.RaceElapsed,racer.GetDisplayBestLap(),metric!=null?metric.lapTimes.ToArray():Array.Empty<float>());
            }
            var result = new ClosureRaceResult { seed = currentSeed, playerMmr = playerMmr, timeScale = simulationScale,
                racers = manager.SpawnedVehicles.Count, movingBeforeGo = movedBeforeGo, state = manager.State.ToString(),
                raceSeconds = manager.RaceElapsed, validFinishes = true, validDnfTimes = true, samePhysics = true };
            int expectedVisits = 1 + FindObjectsByType<CheckpointTrigger3D>(FindObjectsSortMode.None).Length * manager.Rules.TotalLaps;
            foreach (var racer in manager.ProgressTracker.Leaderboard)
            {
                if (racer.Identity.isHuman) { result.humans++; result.playerPosition = racer.RacePosition; }
                else result.bots++;
                if (racer.RacePosition == 1) result.winner = racer.Identity.displayName;
                if (racer.Finished) { result.finished++; result.validFinishes &= racer.CheckpointVisits == expectedVisits && racer.FinishTime > 0; }
                if (!racer.Finished && manager.RaceElapsed - racer.LastCheckpointTime > 60) result.stalledRacers++;
                manager.ProgressTracker.TrackGraph.ProjectPosition(racer.Vehicle.transform.position, out _, out float along, out _);
                if (racer.Vehicle.transform.position.y < manager.ProgressTracker.TrackGraph.SampleDistance(along).Position.y - 5) result.underTrackRacers++;
                result.checkpointVisits += racer.CheckpointVisits;
                result.maxRecoveries = Mathf.Max(result.maxRecoveries, racer.Recovery.RecoveryCount);
                result.totalRecoveries += racer.Recovery.RecoveryCount;
                var stats = RacingStatsCalculator.Calculate(racer.Identity.vehicleId, racer.Identity.wheelId);
                result.samePhysics &= JsonUtility.ToJson(racer.Vehicle.Stats)==JsonUtility.ToJson(stats) && Mathf.Abs(racer.Vehicle.Body.mass-stats.WeightKg)<.01f;
            }
            if (manager.FinalResults != null)
                foreach (var entry in manager.FinalResults.entries) result.validDnfTimes &= entry.finished ? entry.totalTime > 0 : entry.totalTime == 0;
            return result;
        }
        IEnumerator WaitButton(string name)
        {
            float deadline=Time.realtimeSinceStartup+25;
            while(Time.realtimeSinceStartup<deadline)
            {
                if(!RacingLoadingScreen.IsLoading)foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))if(button.name==name&&button.isActiveAndEnabled&&button.interactable)yield break;
                yield return null;
            }
        }
        void FixedUpdate()
        {
            if (manager == null || drivers.Count == 0) return;
            if (watchingPreGo && manager.State != RaceState.Racing)
                foreach (var tracked in drivers) if (tracked.racer.Vehicle.SpeedKmh > .5f) movedBeforeGo++;
            if (mode == "profile" && !bufferedDrivingTraces) return;
            if (Time.time < nextSample) return;
            nextSample = Time.time + .1f;
            foreach (var tracked in drivers)
            {
                ClosureDriveSample sample = Sample(tracked);
                tracked.ring[tracked.cursor] = sample;
                tracked.cursor = (tracked.cursor + 1) % tracked.ring.Length;
                tracked.count = Mathf.Min(tracked.count + 1, tracked.ring.Length);
                if (mode!="profile" && !sample.shortcut && sample.distanceFromCenter > sample.trackWidth * .5f - .85f && Time.time > tracked.nextEdgeTrace)
                {
                    tracked.nextEdgeTrace = Time.time + 8;
                    WriteTrace(tracked, "Edge excursion", false);
                }
            }
        }
        ClosureDriveSample Sample(TrackedDriver tracked)
        {
            var car = tracked.racer.Vehicle;
            var ai = tracked.driver;
            var graph = manager.ProgressTracker.TrackGraph;
            graph.ProjectPosition(car.transform.position, out int node, out float distance, out float lateral);
            var road = graph.SampleDistance(distance);
            var cmd = car.LastCommands;
            return new ClosureDriveSample { time = manager.RaceElapsed, position = car.transform.position, forward = car.transform.forward,
                velocity = car.Body.linearVelocity, target = ai.CurrentTarget, speed = car.SpeedKmh, steering = cmd.Steering,
                throttle = cmd.Throttle, braking = cmd.Brake, currentNode = node, targetNode = ai.TargetNodeIndex,
                lookahead = ai.LookAheadDistance, curvature = ai.TrackCurvature, targetSpeed = ai.TargetSpeedKmh,
                lateralOffset = lateral, lineOffset = ai.RacingLineOffset, distanceFromCenter = Mathf.Abs(lateral),
                trackWidth = road.TrackWidth, slip = car.SlipAngle, upcomingTurn = ai.UpcomingTurnDegrees,
                shortcut = ai.ExecutingShortcut, grounded = car.IsGrounded, offroad = car.IsOffroad, drifting = car.IsDrifting,
                spunOut = car.IsSpunOut, boosting = car.IsBoosting, lap = tracked.racer.CurrentLap, checkpoint = tracked.racer.LastCheckpointIndex,
                routeSegment=ai.RouteSegment,routeIndex=ai.ActiveRouteIndex,routeAbort=ai.LastRouteAbortReason,intent=ai.CurrentIntent,
                wallContacts=car.WallCollisions,racerContacts=car.RacerCollisions,itemHits=car.ItemHits,
                airborneSeconds=car.AirborneTime,lastJumpAge=float.IsFinite(car.LastJumpTime)?Time.time-car.LastJumpTime:-1,lancer=car.IsLancerActive,miniHop=car.IsMiniHopping,
                item=(int)(car.GetComponent<ItemInventory>()?.HeldItem??RaceItemType.None) };
        }
        void WriteTrace(TrackedDriver tracked, string reason, bool recovery = true)
        {
            if (RacingFrameForensics.ReducedDiagnostics) return;
            // Profile mode does not sample driving histories. Its recovery
            // callback must not allocate/serialize/write a one-sample JSON in
            // the measured frame. The explicit QA variant reproduces the old
            // callback solely to measure its cost in a controlled A/B run.
            if(mode=="profile"&&!bufferedDrivingTraces&&RacingFrameForensics.Variant!="trace-io")return;
            if(bufferedDrivingTraces)
            {
                if(traceBuffer==null || bufferedTraceCount>=traceBuffer.Length){droppedTraces++;return;}
                var buffered=traceBuffer[bufferedTraceCount++];buffered.driver=tracked;buffered.reason=reason;
                buffered.prefix=recovery?"recovery":"edge";buffered.number=++tracked.traceNumber;
                buffered.recoveryNumber=tracked.racer.Recovery.RecoveryCount+(recovery?1:0);
                buffered.count=tracked.count+1;
                for(int i=0;i<tracked.count;i++)buffered.before[i]=tracked.ring[(tracked.cursor-tracked.count+i+tracked.ring.Length)%tracked.ring.Length];
                buffered.before[tracked.count]=Sample(tracked);
                return;
            }
            bool measured=manager!=null&&manager.State==RaceState.Racing;
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            using(TraceIoMarker.Auto())
            {
            var trace = new ClosureRecoveryTrace { seed = currentSeed, racerId = tracked.racer.Identity.racerId,
                name = tracked.racer.Identity.displayName, mmr = tracked.racer.Identity.mmr, reason = reason,
                recoveryNumber = tracked.racer.Recovery.RecoveryCount + (recovery ? 1 : 0) };
            for (int i = 0; i < tracked.count; i++)
                trace.before.Add(tracked.ring[(tracked.cursor - tracked.count + i + tracked.ring.Length) % tracked.ring.Length]);
            trace.before.Add(Sample(tracked));
            string prefix = recovery ? "recovery" : "edge";
            File.WriteAllText(Path.Combine(raceOutput, prefix + "-" + tracked.racer.Identity.gridPosition + "-" + (++tracked.traceNumber) + ".json"), JsonUtility.ToJson(trace, true));
            }
            if(measured){SynchronousTraceWrites++;SynchronousTraceWriteMs+=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;}
        }
        void ExportBufferedTraces()
        {
            if(!bufferedDrivingTraces)return;
            for(int i=0;i<bufferedTraceCount;i++)
            {
                var data=traceBuffer[i];var tracked=data.driver;
                var trace=new ClosureRecoveryTrace{seed=currentSeed,racerId=tracked.racer.Identity.racerId,
                    name=tracked.racer.Identity.displayName,mmr=tracked.racer.Identity.mmr,reason=data.reason,recoveryNumber=data.recoveryNumber};
                trace.before.Capacity=data.count;
                for(int n=0;n<data.count;n++)trace.before.Add(data.before[n]);
                File.WriteAllText(Path.Combine(raceOutput,data.prefix+"-"+tracked.racer.Identity.gridPosition+"-"+data.number+".json"),JsonUtility.ToJson(trace,true));
            }
            File.WriteAllText(Path.Combine(raceOutput,"driving-trace-buffer.json"),
                "{\"bufferedDuringRace\":true,\"exportedAfterMeasurement\":true,\"capacity\":"+(traceBuffer?.Length??0)+",\"count\":"+bufferedTraceCount+",\"dropped\":"+droppedTraces+"}");
        }
        void Click(string name)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (button.gameObject.name != name || !button.interactable || !button.gameObject.activeInHierarchy) continue;
                button.onClick.Invoke();
                return;
            }
            Debug.LogError("Closure UI button not found: " + name);
        }
        void CaptureTextureScene(string name)
        {
            if (!capture) return;
            string path = Path.Combine(output, name + "-size-smoke.png");
#if UNITY_EDITOR
            RacingEvidenceCapture.Capture(path);
#else
            UnityEngine.ScreenCapture.CaptureScreenshot(path);
#endif
        }
        void Capture(string name)
        {
            if (!capture) return;
#if !UNITY_EDITOR
            var rendered=GetComponent<RacingRenderedPlayerProbe>();
            if(mode=="profile"&&rendered!=null&&rendered.IsBound)
            {rendered.SaveFrame(Path.Combine(raceOutput,name+".png"));return;}
#endif
            RacingEvidenceCapture.Capture(Path.Combine(raceOutput, name + ".png"));
        }
        void Finish(bool passed)
        {
            suite.passed = passed;
            File.WriteAllText(Path.Combine(output, "suite.json"), JsonUtility.ToJson(suite, true));
            Application.logMessageReceived -= Log;
            SceneManager.sceneLoaded -= Loaded;
#if UNITY_EDITOR
            SessionState.SetInt("AHR.Racing.Closure.Exit", passed ? 0 : 1);
            SessionState.SetBool("AHR.Racing.Closure.Done", true);
#else
            Application.Quit(passed ? 0 : 1);
#endif
        }
    }
    public static class RacingEvidenceCapture
    {
        public static void Capture(string path)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            foreach (var preview in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (preview.targetTexture != null) preview.Render();
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing=Mathf.Max(1,QualitySettings.antiAliasing);
            target.Create();
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            float aspect = camera.aspect;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var planes = new float[canvases.Length];
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                camera.aspect = 1600f / 900;
                for (int i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; planes[i] = canvases[i].planeDistance;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = .5f;
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = planes[i]; }
                camera.targetTexture = previous; camera.aspect = aspect; RenderTexture.active = active;
                target.Release(); UnityEngine.Object.Destroy(target);
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
#endif
