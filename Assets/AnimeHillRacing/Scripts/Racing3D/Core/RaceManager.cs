using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnimeHillRacing.Racing3D.ML;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RaceManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TrackSplineGraph trackGraph;
        [SerializeField] private StartingGrid startingGrid;
        [SerializeField] private TrackProgressTracker progressTracker;
        [SerializeField] private RacingCamera3D racingCamera;
        [SerializeField] private RaceRules rules = RaceRules.CreateRanked();

        [Header("Prefabs / Templates (Optional)")]
        [SerializeField] private GameObject vehiclePrefab3D;

        // State Machine
        private RaceState currentState = RaceState.Loading;
        private float stateTimer;
        private float raceElapsed;
        private RacerIdentity[] lobbyRacers;
        private readonly List<RacingVehiclePhysics> spawnedVehicles = new List<RacingVehiclePhysics>(12);
        private RacingVehiclePhysics playerVehicle;
        private PlayerRacingInput playerInput;
        private RaceResults finalResults;
        private CheckpointTrigger3D[] checkpoints;
        public RacerIdentity[] LobbyRacers => lobbyRacers;
        public bool PersistResults { get; set; } = true;
        // CPPS RaceId of the atomically reserved ranked lobby (empty when unrated).
        public string RankedRaceId { get; private set; } = "";
        bool rankedResolved;
        public int EffectiveSeed { get; private set; }
        public int LobbyRevealCount { get; private set; }
        public void Configure(RaceRules configuration) { rules = configuration ?? RaceRules.CreateRanked(); }

        public RaceState State => currentState;
        public float StateTimer => stateTimer;
        public float RaceElapsed => raceElapsed;
        public RaceRules Rules => rules;
        public TrackProgressTracker ProgressTracker => progressTracker;
        public RacingVehiclePhysics PlayerVehicle => playerVehicle;
        public RaceResults FinalResults => finalResults;
        public IReadOnlyList<RacingVehiclePhysics> SpawnedVehicles => spawnedVehicles;

        public event Action<RaceState> OnStateChanged;
        public event Action<int> OnCountdownTick; // 3, 2, 1, 0 (GO)
        public event Action<RaceResults> OnRaceCompleted;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (progressTracker == null) progressTracker = GetComponent<TrackProgressTracker>();
            if (startingGrid == null) startingGrid = GetComponentInChildren<StartingGrid>();
            if (trackGraph == null) trackGraph = GetComponentInChildren<TrackSplineGraph>();
            if (racingCamera == null) racingCamera = FindFirstObjectByType<RacingCamera3D>();
        }

        private void Start()
        {
            StartCoroutine(StartupSequence());
        }

        private IEnumerator StartupSequence()
        {
            SetState(RaceState.Loading);
            yield return null;

            if (trackGraph == null) trackGraph = FindFirstObjectByType<TrackSplineGraph>();
            if (startingGrid == null) startingGrid = FindFirstObjectByType<StartingGrid>();
            if (progressTracker == null) progressTracker = FindFirstObjectByType<TrackProgressTracker>();
            if (racingCamera == null) racingCamera = FindFirstObjectByType<RacingCamera3D>();

            // 1. Setup track and progress tracker
            if (trackGraph != null) trackGraph.RecomputeDistances();
            if (progressTracker != null)
            {
                checkpoints = FindObjectsByType<CheckpointTrigger3D>(FindObjectsSortMode.None);
                progressTracker.Initialize(trackGraph, rules.TotalLaps, checkpoints.Length);
                progressTracker.OnRacerFinished += HandleRacerFinished;
                foreach (var checkpoint in checkpoints) checkpoint.Bind(progressTracker, this);
            }
            if (trackGraph == null || startingGrid == null || progressTracker == null || checkpoints.Length < 2)
            {
                Debug.LogError("[Racing] Track, grid or checkpoint configuration is incomplete.");
                yield break;
            }
            startingGrid.Initialize(trackGraph);

            // 2. Prepare 12-Racer Lobby
            SetState(RaceState.Preparing);
            BuildLobby();
            for (int i = 0; i < lobbyRacers.Length; i++)
            {
                LobbyRevealCount = i + 1;
                yield return new WaitForSeconds(.065f);
            }

            // 3. Spawn and position 12 vehicles
            SpawnRacers();
            racingCamera?.PlayIntroduction(trackGraph, 2.6f);
            RacingFeedback.Music(RacingMusicPhase.PreRace);
            yield return new WaitForSeconds(2.6f);

            // 4. Begin Countdown
            StartCoroutine(CountdownSequence());
        }

        private void BuildLobby()
        {
            int playerMmr = RacingTestSession.Active ? RacingTestSession.Mmr : RacingSaveManager.CurrentMmr;
            if (RacingTestSession.Active) PersistResults = false;
            string playerVehicle = RacingSaveManager.SelectedVehicleId;
            string playerWheel = RacingSaveManager.SelectedWheelId;

            bool diagnosticLobby = RacingTestSession.Active || rules.EqualBuildBenchmark;
            int lobbySeed = EffectiveSeed = RacingTestSession.Active ? RacingTestSession.Seed : rules.RaceSeed != 0 ? rules.RaceSeed : UnityEngine.Random.Range(1, int.MaxValue);
            lobbyRacers = diagnosticLobby ? MatchmakingService.GenerateSeededTestLobby(playerMmr, playerVehicle, playerWheel, "PLAYER", lobbySeed) : MatchmakingService.GenerateRankedLobby(
                playerMmr,
                playerVehicle,
                playerWheel,
                "PLAYER",
                lobbySeed
            );
            RankedRaceId = diagnosticLobby ? "" : MatchmakingService.LastRankedRaceId;
            if (rules.EqualBuildBenchmark)
                foreach (var racer in lobbyRacers)
                { racer.vehicleId = MetaProgressionConfig.MetaVehicleId; racer.wheelId = MetaProgressionConfig.MetaWheelId;if(rules.BenchmarkMmr>=0)racer.mmr=rules.BenchmarkMmr; }
        }

        private void SpawnRacers()
        {
            spawnedVehicles.Clear();

            for (int i = 0; i < lobbyRacers.Length; i++)
            {
                RacerIdentity identity = lobbyRacers[i];
                startingGrid.GetSlotSpawn(identity.gridPosition, out Vector3 spawnPos, out Quaternion spawnRot);

                GameObject racerObj;
                if (vehiclePrefab3D != null)
                {
                    racerObj = Instantiate(vehiclePrefab3D, spawnPos, spawnRot);
                }
                else
                {
                    // Procedural stylized race kart
                    racerObj = RacingVehicleFactory.Create(identity);
                    racerObj.transform.position = spawnPos;
                    racerObj.transform.rotation = spawnRot;
                }

                racerObj.name = $"Racer_{identity.gridPosition:00}_{identity.displayName}";

                var vehicle = racerObj.GetComponent<RacingVehiclePhysics>();
                if (vehicle == null) vehicle = racerObj.AddComponent<RacingVehiclePhysics>();

                var recovery = racerObj.GetComponent<VehicleRecovery>();
                if (recovery == null) recovery = racerObj.AddComponent<VehicleRecovery>();
                recovery.SetLastSafeTransform(spawnPos, spawnRot);

                var inventory = racerObj.GetComponent<ItemInventory>();
                if (inventory == null) inventory = racerObj.AddComponent<ItemInventory>();

                if (identity.isHuman)
                {
                    playerVehicle = vehicle;
                    racerObj.AddComponent<RacingTechniqueObserver>().Initialize(vehicle,trackGraph);
                    playerInput = racerObj.GetComponent<PlayerRacingInput>();
                    if (playerInput == null) playerInput = racerObj.AddComponent<PlayerRacingInput>();
                    playerInput.SetInputEnabled(false);
                    vehicle.Initialize(identity.vehicleId, identity.wheelId, playerInput);

                    var recorder = racerObj.GetComponent<RacingTelemetryRecorder>() ?? racerObj.AddComponent<RacingTelemetryRecorder>();
                    string trackId = FindFirstObjectByType<RacingTrackInfo>()?.TrackId ?? "NightCityGrandCircuit";
                    recorder.StartSession(trackId, EffectiveSeed, identity.mmr, true);

                    if (racingCamera != null)
                    {
                        racingCamera.SetTarget(playerVehicle.transform, playerVehicle);
                    }
                }
                else
                {
                    var aiController = racerObj.GetComponent<AiVehicleController>();
                    if (aiController == null) aiController = racerObj.AddComponent<AiVehicleController>();

                    AiRacerProfile profile = AiSkillModel.CreateProfile(identity.mmr, identity.personalitySeed);
                    if (identity.hasPersistentStyle) profile.style = identity.preferredStyle;
                    aiController.Initialize(profile, trackGraph, progressTracker, identity.displayName);
                    vehicle.Initialize(identity.vehicleId, identity.wheelId, aiController);

                    var mlAdapter = racerObj.GetComponent<RacingMlInferenceAdapter>() ?? racerObj.AddComponent<RacingMlInferenceAdapter>();
                    // Shadow comparisons are only collected by QA/development players;
                    // a release player has no sink for them, so it skips the inference
                    // cost on phones. Never an authority change: Off and Shadow both
                    // leave the heuristic driving.
                    mlAdapter.executionMode = Debug.isDebugBuild ? MlExecutionMode.Shadow : MlExecutionMode.Off;
                    mlAdapter.mmr = identity.mmr;
                }

                if (progressTracker != null)
                {
                    progressTracker.RegisterRacer(identity, vehicle, recovery);
                    inventory.BindRaceProgress(progressTracker);
                }

                spawnedVehicles.Add(vehicle);
                vehicle.SetRaceEnabled(false);
                inventory.SetSeed(EffectiveSeed ^ identity.personalitySeed);
                racerObj.AddComponent<RacingVehicleAudio>().Initialize(identity.isHuman);
                racerObj.AddComponent<RacingVehicleVfx>().Initialize();
            }
        }

        private IEnumerator CountdownSequence()
        {
            SetState(RaceState.Countdown);

            for (int i = 3; i >= 1; i--)
            {
                stateTimer = i;
                OnCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1.0f);
            }

            stateTimer = 0f;
            OnCountdownTick?.Invoke(0); // GO!

            // Unlock inputs
            if (playerInput != null) playerInput.SetInputEnabled(true);
            foreach (var vehicle in spawnedVehicles) vehicle.SetRaceEnabled(true);
            // GO: from now on leaving is an abandonment for a rated race.
            if (!string.IsNullOrEmpty(RankedRaceId) && PersistResults) RacingProgressionService.MarkRankedStarted(RankedRaceId);
            SetState(RaceState.Racing);
        }

        private void Update()
        {
            if (currentState == RaceState.Racing || currentState == RaceState.PlayerFinished || currentState == RaceState.RaceFinishing)
            {
                raceElapsed += Time.deltaTime;
                if (progressTracker != null)
                {
                    progressTracker.UpdateProgress(raceElapsed);
                }

                if (currentState == RaceState.PlayerFinished)
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        TransitionToResults();
                    }
                }
            }
        }

        private void HandleRacerFinished(RacerProgress racer, int finishPos)
        {
            racer.Vehicle.SetFinished();
            if (racer.Identity.isHuman && currentState == RaceState.Racing)
            {
                SetState(RaceState.PlayerFinished);
                stateTimer = rules.PostPlayerFinishTimeoutSeconds;
                if (playerInput != null) playerInput.SetInputEnabled(false);
            }

            if (finishPos >= rules.TotalRacers)
            {
                TransitionToResults();
            }
        }

        public void TransitionToResults()
        {
            if (currentState == RaceState.Results || currentState == RaceState.Exiting) return;
            if (progressTracker?.GetPlayerProgress()?.Finished != true) return;

            SetState(RaceState.Results);
            // Coin items already updated the shared wallet. Flush once outside active
            // gameplay, rather than synchronously writing preferences on every pickup.
            playerVehicle?.GetComponent<ItemInventory>()?.FlushCoinRewards();
            BuildResults();

            // Persist human player MMR
            var playerEntry = finalResults.GetPlayerEntry();
            finalResults.rankedRaceId = RankedRaceId;
            if (playerEntry != null && PersistResults)
            {
                RacingProgressionService.RecordRankedResults(finalResults);
                RacingSaveManager.RecordRecentFinish(playerEntry.finishPosition);
                rankedResolved = true;
            }
            else ResolveUnfinishedRankedRace();

            // End telemetry recording session
            var recorder = playerVehicle?.GetComponent<RacingTelemetryRecorder>();
            if (recorder != null && playerEntry != null)
            {
                var progress=progressTracker.FindRacer(playerVehicle);
                float bestLap = progress!=null?progress.GetDisplayBestLap():0;
                recorder.EndSession(playerEntry.finishPosition, progress!=null&&progress.Finished?progress.FinishTime:finalResults.totalRaceTime, bestLap, progress!=null?progress.MeasuredLapTimes.ToArray():Array.Empty<float>());
            }

            OnRaceCompleted?.Invoke(finalResults);
            foreach (var vehicle in spawnedVehicles) vehicle.SetRaceEnabled(false);
        }

        private void BuildResults()
        {
            finalResults = new RaceResults
            {
                totalRaceTime = raceElapsed,
                trackId = FindFirstObjectByType<RacingTrackInfo>()?.TrackId ?? "test_circuit_01",
                trackDisplayName = FindFirstObjectByType<RacingTrackInfo>()?.DisplayName ?? "HILL CIRCUIT 3D"
            };

            var leaderboard = progressTracker != null ? progressTracker.Leaderboard : null;
            int count = leaderboard != null ? leaderboard.Count : 0;
            finalResults.entries = new RacerResultEntry[count];

            int[] allMmrs = new int[count];
            int[] allPositions = new int[count];

            for (int i = 0; i < count; i++)
            {
                allMmrs[i] = leaderboard[i].Identity.mmr;
                allPositions[i] = leaderboard[i].RacePosition;
            }

            var deviations = new float[count]; var ratingDeltas = new int[count];
            for (int i = 0; i < count; i++) deviations[i] = PersistResults ? RacingProgressionService.Find(leaderboard[i].Identity.racerId)?.uncertainty ?? .5f : .5f;
            if (count > 1) LocalLeagueEngine.CalculateDeltas(allMmrs, allPositions, deviations, RacingProgressionService.Engine.Configuration, ratingDeltas);

            for (int i = 0; i < count; i++)
            {
                RacerProgress r = leaderboard[i];
                int delta = ratingDeltas[i];
                int newMmr = MmrSystem.ApplyDelta(r.Identity.mmr, delta);

                finalResults.entries[i] = new RacerResultEntry
                {
                    finishPosition = r.RacePosition,
                    racerId = r.Identity.racerId,
                    displayName = r.Identity.displayName,
                    isHuman = r.Identity.isHuman,
                    vehicleId = r.Identity.vehicleId,
                    wheelId = r.Identity.wheelId,
                    totalTime = r.Finished ? r.FinishTime : 0f,
                    finished = r.Finished,
                    bestLapTime = r.GetDisplayBestLap(),
                    oldMmr = r.Identity.mmr,
                    mmrDelta = delta,
                    newMmr = newMmr,
                    titleId = r.Identity.titleId,
                    shortcutSuccesses = r.Identity.isHuman ? r.Vehicle.GetComponent<RacingTechniqueObserver>()?.ShortcutSuccesses ?? 0 : r.Vehicle.GetComponent<AiVehicleController>()?.ShortcutCompletions ?? 0,
                    aerialSuccesses = r.Identity.isHuman ? r.Vehicle.GetComponent<RacingTechniqueObserver>()?.AerialSuccesses ?? 0 : r.Vehicle.JumpTrickLandingChains,
                    landingSuccesses = r.Vehicle.LandingBoosts
                };
            }
        }

        private void SetState(RaceState newState)
        {
            currentState = newState;
            RacingProgressionService.SetRaceActive(newState == RaceState.Preparing || newState == RaceState.Countdown || newState == RaceState.Racing || newState == RaceState.PlayerFinished);
            OnStateChanged?.Invoke(newState);
        }

        public void RetryRace()
        {
            RacingLoadingScreen.Load(SceneManager.GetActiveScene().name,"BACK TO THE STARTING GRID");
        }

        public void ExitToMenu()
        {
            SetState(RaceState.Exiting);
            RacingLoadingScreen.Load("RacingSelect","RACING HUB");
        }
        private void OnDestroy()
        {
            if (progressTracker != null) progressTracker.OnRacerFinished -= HandleRacerFinished;
            RacingProgressionService.SetRaceActive(false);
            ResolveUnfinishedRankedRace();
        }

        // Any exit without settled results (menu, retry, scene change). A rated
        // race after GO is an abandonment; before GO, or for unrated test races,
        // the reserved bots are simply released.
        void ResolveUnfinishedRankedRace()
        {
            if (rankedResolved || string.IsNullOrEmpty(RankedRaceId)) return;
            rankedResolved = true;
            if (PersistResults) RacingProgressionService.AbandonRankedRace(RankedRaceId);
            else RacingProgressionService.CancelRankedRace(RankedRaceId);
        }
    }
}

