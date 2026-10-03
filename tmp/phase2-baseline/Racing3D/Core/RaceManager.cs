using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                progressTracker.Initialize(trackGraph, rules.TotalLaps);
                progressTracker.OnRacerFinished += HandleRacerFinished;
            }

            // 2. Prepare 12-Racer Lobby
            SetState(RaceState.Preparing);
            BuildLobby();

            // 3. Spawn and position 12 vehicles
            SpawnRacers();

            yield return new WaitForSeconds(0.5f);

            // 4. Begin Countdown
            StartCoroutine(CountdownSequence());
        }

        private void BuildLobby()
        {
            int playerMmr = RacingSaveManager.CurrentMmr;
            string playerVehicle = RacingSaveManager.SelectedVehicleId;
            string playerWheel = RacingSaveManager.SelectedWheelId;

            lobbyRacers = MatchmakingService.GenerateRankedLobby(
                playerMmr,
                playerVehicle,
                playerWheel,
                "PLAYER",
                rules.RaceSeed
            );
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
                    racerObj = CreateProceduralKart(identity.displayName, identity.isHuman);
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
                    playerInput = racerObj.GetComponent<PlayerRacingInput>();
                    if (playerInput == null) playerInput = racerObj.AddComponent<PlayerRacingInput>();
                    playerInput.SetInputEnabled(false);
                    vehicle.Initialize(identity.vehicleId, identity.wheelId, playerInput);

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
                    aiController.Initialize(profile, trackGraph, progressTracker, identity.displayName);
                    vehicle.Initialize(identity.vehicleId, identity.wheelId, aiController);
                }

                if (progressTracker != null)
                {
                    progressTracker.RegisterRacer(identity, vehicle, recovery);
                }

                spawnedVehicles.Add(vehicle);
            }
        }

        private GameObject CreateProceduralKart(string displayName, bool isPlayer)
        {
            GameObject root = new GameObject(displayName);

            // Chassis
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.transform.SetParent(root.transform, false);
            chassis.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 0.5f, 2.8f);

            var mat = chassis.GetComponent<Renderer>()?.material;
            if (mat != null)
            {
                mat.color = isPlayer ? new Color(0.2f, 0.75f, 1.0f) : new Color(0.85f, 0.35f, 0.25f);
            }

            // Nose cone
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.transform.SetParent(root.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.32f, 1.5f);
            nose.transform.localScale = new Vector3(1.1f, 0.35f, 0.9f);
            if (mat != null && nose.GetComponent<Renderer>() != null) nose.GetComponent<Renderer>().material.color = mat.color;

            // Wheels (visuals)
            Vector3[] wheelOffsets = new[]
            {
                new Vector3(-0.95f, 0.35f, 0.95f),
                new Vector3(0.95f, 0.35f, 0.95f),
                new Vector3(-0.95f, 0.42f, -0.95f),
                new Vector3(0.95f, 0.42f, -0.95f)
            };

            for (int i = 0; i < wheelOffsets.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.transform.SetParent(root.transform, false);
                wheel.transform.localPosition = wheelOffsets[i];
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
                var wheelMat = wheel.GetComponent<Renderer>()?.material;
                if (wheelMat != null) wheelMat.color = new Color(0.12f, 0.12f, 0.14f);
            }

            return root;
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
            if (racer.Identity.isHuman && currentState == RaceState.Racing)
            {
                SetState(RaceState.PlayerFinished);
                stateTimer = rules.PostPlayerFinishTimeoutSeconds;
                if (playerInput != null) playerInput.SetInputEnabled(false);
            }

            // If all 12 finished, conclude immediately
            if (finishPos >= rules.TotalRacers)
            {
                TransitionToResults();
            }
        }

        public void TransitionToResults()
        {
            if (currentState == RaceState.Results || currentState == RaceState.Exiting) return;

            SetState(RaceState.Results);
            BuildResults();

            // Persist human player MMR
            var playerEntry = finalResults.GetPlayerEntry();
            if (playerEntry != null)
            {
                RacingSaveManager.RecordRaceResult(playerEntry.finishPosition, playerEntry.mmrDelta);
            }

            OnRaceCompleted?.Invoke(finalResults);
        }

        private void BuildResults()
        {
            finalResults = new RaceResults
            {
                totalRaceTime = raceElapsed,
                trackId = "test_circuit_01",
                trackDisplayName = "HILL CIRCUIT 3D"
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

            for (int i = 0; i < count; i++)
            {
                RacerProgress r = leaderboard[i];
                int delta = MmrSystem.CalculateMmrDelta(i, r.Identity.mmr, r.RacePosition, allMmrs, allPositions);
                int newMmr = MmrSystem.ApplyDelta(r.Identity.mmr, delta);

                finalResults.entries[i] = new RacerResultEntry
                {
                    finishPosition = r.RacePosition,
                    racerId = r.Identity.racerId,
                    displayName = r.Identity.displayName,
                    isHuman = r.Identity.isHuman,
                    vehicleId = r.Identity.vehicleId,
                    wheelId = r.Identity.wheelId,
                    totalTime = r.Finished ? r.FinishTime : raceElapsed + (r.RacePosition * 1.5f),
                    bestLapTime = r.GetDisplayBestLap(),
                    oldMmr = r.Identity.mmr,
                    mmrDelta = delta,
                    newMmr = newMmr
                };
            }
        }

        private void SetState(RaceState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void RetryRace()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ExitToMenu()
        {
            SetState(RaceState.Exiting);
            SceneManager.LoadScene("PreStart");
        }
    }
}
