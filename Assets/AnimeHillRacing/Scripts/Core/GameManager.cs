using System.Collections;
using System.Collections.Generic;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using AnimeHillRacing.Chests;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Core
{
    public sealed class LiquidExposureTracker
    {
        private readonly HashSet<int> contacts = new HashSet<int>();
        public float TotalSeconds { get; private set; }
        public float LimitSeconds { get; private set; } = 10f;
        public bool IsInside => contacts.Count > 0;
        public bool LimitReached => TotalSeconds >= LimitSeconds;

        public void Reset(float limitSeconds)
        {
            contacts.Clear();
            TotalSeconds = 0f;
            LimitSeconds = Mathf.Max(.01f, limitSeconds);
        }

        public void SetContact(int contactId, bool inside)
        {
            if (inside) contacts.Add(contactId);
            else contacts.Remove(contactId);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInside || LimitReached) return;
            TotalSeconds = Mathf.Min(LimitSeconds, TotalSeconds + Mathf.Max(0f, deltaTime));
        }
    }

    public sealed class GemMilestoneTracker
    {
        public const float MilestoneDistance = 1000f;
        public float NextDistance { get; private set; } = MilestoneDistance;

        public void Reset() => NextDistance = MilestoneDistance;

        public int ClaimThrough(float maximumRunDistance)
        {
            int awards = 0;
            while (maximumRunDistance >= NextDistance)
            {
                awards++;
                NextDistance += MilestoneDistance;
            }
            return awards;
        }
    }

    [DefaultExecutionOrder(-50)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private VehicleRuntimeBase vehicle;
        [SerializeField] private VehicleInputController input;
        [SerializeField] private GameplayHUD hud;
        [SerializeField] private ResultsUI resultsUI;
        [SerializeField] private ResultCaptureService resultCapture;
        [SerializeField] private CanvasGroup gameplayControls;
        [SerializeField, Min(0f)] private float resultsDelay = 3f;
        [SerializeField] private MapProfile mapProfile;
        [SerializeField] private GameCatalog catalog;

        private float startProgress;
        private float maxProgress;
        private float maxSpeedKmh;
        private int runCoins;
        private Coroutine gameOverRoutine;
        private float gameOverDeadline;
        private int lastDistanceBonus;
        private float distanceBonusMessageUntil;
        private readonly LiquidExposureTracker liquidExposure = new LiquidExposureTracker();
        private readonly GemMilestoneTracker gemMilestones = new GemMilestoneTracker();
        private bool liquidCrashRequested;
        private int lastGemAward;
        private float gemMessageUntil;
        private readonly StuntRotationTracker stuntRotation = new StuntRotationTracker();
        private Quaternion previousStuntRotation;
        private bool stuntRotationInitialized;
        private bool resultSnapshotFinalized;
        private float finalizedDistance;
        private int finalizedRunCoins;
        private int finalizedDisplayCoins;
        private int finalizedSavedTotalCoins;

        public const float LogicalDistanceScale = 1f;

        public RunState State { get; private set; } = RunState.Ready;
        public RunEndReason EndReason { get; private set; }
        private float LiveDistance => Mathf.Max(0f, (maxProgress - startProgress) * LogicalDistanceScale);
        public float Distance => resultSnapshotFinalized ? finalizedDistance : LiveDistance;
        public float BestDistance { get; private set; }
        public float MaxSpeedKmh => maxSpeedKmh;
        public int RunCoins => resultSnapshotFinalized ? finalizedRunCoins : runCoins;
        public int RunFlips => stuntRotation.Flips;
        public int RunBackflips => stuntRotation.Backflips;
        public int StuntBonus => stuntRotation.StuntBonus;
        public int DisplayCoins => resultSnapshotFinalized
            ? finalizedDisplayCoins : SaturatingCurrencyAdd(SaveManager.TotalCoins, runCoins);
        public int SavedTotalCoins => resultSnapshotFinalized
            ? finalizedSavedTotalCoins : SaveManager.TotalCoins;
        public int TotalGems => SaveManager.TotalGems;
        public int CurrentDistanceBonusValue => SaveManager.CurrentDistanceBonusValue;
        public float NextDistanceBonusThreshold => SaveManager.NextDistanceBonusThreshold;
        public int LastDistanceBonus => lastDistanceBonus;
        public bool ShowDistanceBonus => Time.unscaledTime < distanceBonusMessageUntil;
        public int LastGemAward => lastGemAward;
        public bool ShowGemAward => Time.unscaledTime < gemMessageUntil;
        public bool SupportsLiquidExposure => mapProfile != null && mapProfile.supportsLiquidExposure;
        public bool IsInsideLiquid => SupportsLiquidExposure && liquidExposure.IsInside;
        public float TotalLiquidExposureThisRun => liquidExposure.TotalSeconds;
        public float LiquidExposureLimit => SupportsLiquidExposure
            ? mapProfile.liquidChallenges.exposureLimitSeconds : 10f;
        public VehicleRuntimeBase Vehicle => vehicle;
        public GameModeType Mode => GameSession.SelectedGameMode;
        public string MapId => GameSession.SelectedMapId;
        public float GameOverRemaining => (State == RunState.CrashPending || State == RunState.OutOfFuelPending)
            ? Mathf.Max(0f, gameOverDeadline - Time.time) : 0f;
        public float OutOfFuelRemaining => GameOverRemaining;

        public void TeleportTo(float targetWorldX)
        {
            TerrainManager terrain = FindFirstObjectByType<TerrainManager>();
            if (terrain != null)
                terrain.TeleportTo(targetWorldX);

            float targetY;
            if (terrain != null && terrain.TrySampleAuthoritativeSurface(targetWorldX,
                    out float surfaceY, out _))
                targetY = surfaceY + 2.5f;
            else
                targetY = mapProfile != null
                    ? TerrainGenerator.SampleHeight(mapProfile, targetWorldX) + 2.5f
                    : 2.5f;

            if (vehicle != null)
            {
                vehicle.TeleportToPosition(new Vector3(targetWorldX, targetY, 0f));
            }

            maxProgress = Mathf.Max(maxProgress, targetWorldX);
            if (State == RunState.OutOfFuelPending || State == RunState.OutOfFuelFinal || State == RunState.Results || State == RunState.CrashPending)
            {
                if (gameOverRoutine != null)
                {
                    StopCoroutine(gameOverRoutine);
                    gameOverRoutine = null;
                }
                State = RunState.Running;
                EndReason = RunEndReason.None;
                gameOverDeadline = 0f;
                if (resultsUI != null) resultsUI.HideImmediate();
                vehicle?.SetDriveEnabled(true);
            }

            if (hud != null)
            {
                hud.ResetDisplayedDistance();
                hud.RefreshImmediate(this);
            }
        }

        public static GameManager Instance { get; private set; }
        public MapProfile MapProfile => mapProfile;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            BestDistance = SaveManager.GetBestDistance(Mode, MapId);
            vehicle?.SetTransmissionMode(GameSession.SelectedTransmission);
            liquidExposure.Reset(LiquidExposureLimit);
            gemMilestones.Reset();
        }

        private void Start()
        {
            if (vehicle == null)
            {
                enabled = false;
                return;
            }
            startProgress = vehicle.ProgressPosition;
            maxProgress = startProgress;
            resultSnapshotFinalized = false;
            ResetRunStunts();
            State = RunState.Running;
            vehicle.SetDriveEnabled(true);
            resultsUI?.HideImmediate();
        }

        private void FixedUpdate()
        {
            if (vehicle == null) return;
            bool runActive = State == RunState.Running;
            bool airborne = runActive && !vehicle.AnyGrounded;
            if (Mode == GameModeType.Adventure2_5D)
            {
                float zAngle = vehicle.Body2D != null
                    ? vehicle.Body2D.rotation
                    : (vehicle.Body != null ? vehicle.Body.rotation.eulerAngles.z : vehicle.transform.rotation.eulerAngles.z);
                stuntRotation.SampleAngle(zAngle, airborne, runActive);
                previousStuntRotation = vehicle.transform.rotation;
                stuntRotationInitialized = true;
                return;
            }

            Rigidbody body = vehicle.Body;
            if (body == null) return;
            Quaternion current = body.rotation;
            if (!stuntRotationInitialized)
            {
                previousStuntRotation = current;
                stuntRotationInitialized = true;
                return;
            }
            float pitchDelta = SignedPitchDeltaDegrees(previousStuntRotation, current,
                -body.transform.right);
            previousStuntRotation = current;
            stuntRotation.SampleDelta(pitchDelta, airborne, runActive);
        }

        private void ResetRunStunts()
        {
            float initialAngle = vehicle != null
                ? (vehicle.Body2D != null ? vehicle.Body2D.rotation : (vehicle.Body != null ? vehicle.Body.rotation.eulerAngles.z : vehicle.transform.rotation.eulerAngles.z))
                : 0f;
            stuntRotation.Reset(initialAngle);
            previousStuntRotation = vehicle != null ? vehicle.transform.rotation : Quaternion.identity;
            stuntRotationInitialized = vehicle != null;
        }

        private static float SignedPitchDeltaDegrees(Quaternion previous, Quaternion current,
            Vector3 pitchAxis)
        {
            Quaternion delta = current * Quaternion.Inverse(previous);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (!float.IsFinite(angle) || axis.sqrMagnitude < .000001f) return 0f;
            if (angle > 180f)
            {
                angle = 360f - angle;
                axis = -axis;
            }
            return angle * Mathf.Clamp(Vector3.Dot(axis.normalized, pitchAxis.normalized), -1f, 1f);
        }

        private void Update()
        {
            if (vehicle == null)
                return;
            if (State == RunState.Running && Time.timeScale > 0f && Application.isFocused && catalog != null)
            {
                List<ChestRecord> granted = ChestSystem.AdvanceActiveGameplay(Time.unscaledDeltaTime, catalog);
                for (int i = 0; i < granted.Count; i++)
                    ChestGameplayNotification.Show(granted[i], catalog.chestCatalog);
            }
            vehicle.Fuel?.TickRun(Time.deltaTime, State == RunState.Running);
            maxProgress = Mathf.Max(maxProgress, vehicle.ProgressPosition);
            maxSpeedKmh = Mathf.Max(maxSpeedKmh, vehicle.SpeedKmh);
            int gemAwards = gemMilestones.ClaimThrough(Distance);
            if (gemAwards > 0)
            {
                SaveManager.AddGems(gemAwards);
                lastGemAward = gemAwards;
                gemMessageUntil = Time.unscaledTime + 2.5f;
            }
            if (SupportsLiquidExposure && (State == RunState.Running || State == RunState.OutOfFuelPending))
            {
                liquidExposure.Tick(Time.deltaTime);
                if (liquidExposure.LimitReached && !liquidCrashRequested)
                {
                    liquidCrashRequested = true;
                    RequestGameOver(RunEndReason.Crashed);
                }
            }
            while (Distance >= SaveManager.NextDistanceBonusThreshold)
            {
                lastDistanceBonus = SaveManager.ClaimDistanceBonus();
                distanceBonusMessageUntil = Time.unscaledTime + 2.5f;
            }
            if (input != null && input.RetryPressedThisFrame)
                Retry();

            if (State == RunState.OutOfFuelPending)
            {
                if (vehicle.FuelNormalized > 0f)
                    CancelOutOfFuelCountdown();
            }
            else if (State == RunState.CrashPending)
            {
                if (vehicle.FuelNormalized <= 0f && EndReason != RunEndReason.DriverDownAndOutOfFuel)
                    EndReason = RunEndReason.DriverDownAndOutOfFuel;
            }
            else if (State == RunState.Running && vehicle.FuelNormalized <= 0f)
            {
                BeginOutOfFuelCountdown();
            }

            hud?.Refresh(this);
        }

        public void AddCoin(int amount)
        {
            if (State == RunState.Running || State == RunState.OutOfFuelPending)
            {
                runCoins += Mathf.Max(0, amount);
                hud?.RefreshImmediate(this);
            }
        }

        public void NotifyLiquidContact(int poolId, bool inside)
        {
            if (!SupportsLiquidExposure) return;
            liquidExposure.SetContact(poolId, inside);
        }

        public void ConfigureMap(MapProfile profile)
        {
            mapProfile = profile;
            liquidExposure.Reset(LiquidExposureLimit);
            liquidCrashRequested = false;
        }

        public void ConfigureChestCatalog(GameCatalog gameCatalog) => catalog = gameCatalog;

        public void RequestGameOver(RunEndReason reason)
        {
            if (reason == RunEndReason.OutOfFuel)
            {
                if (State == RunState.Running)
                {
                    BeginOutOfFuelCountdown();
                }
                else if (State == RunState.CrashPending)
                {
                    EndReason = RunEndReason.DriverDownAndOutOfFuel;
                }
                return;
            }

            if (State == RunState.Results) return;

            bool isDriverDown = reason == RunEndReason.DriverDown || reason == RunEndReason.Crashed;

            if (State == RunState.OutOfFuelPending)
            {
                // Both occurred: Driver crashed while running out of fuel
                EndReason = RunEndReason.DriverDownAndOutOfFuel;
                State = RunState.CrashPending;
                DisableDrivingAndControls();
                // Crucial: Keep the running countdown without resetting it!
                return;
            }

            if (State == RunState.CrashPending)
            {
                if (vehicle != null && vehicle.FuelNormalized <= 0f)
                {
                    EndReason = RunEndReason.DriverDownAndOutOfFuel;
                }
                return;
            }

            // Normal crash start
            State = RunState.CrashPending;
            bool outOfFuel = vehicle != null && vehicle.FuelNormalized <= 0f;
            EndReason = (isDriverDown && outOfFuel) ? RunEndReason.DriverDownAndOutOfFuel : (isDriverDown ? RunEndReason.DriverDown : reason);
            DisableDrivingAndControls();
            gameOverDeadline = Time.time + resultsDelay;
            if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
            gameOverRoutine = StartCoroutine(ShowResultsAfterDeadline());
        }

        private void BeginOutOfFuelCountdown()
        {
            if (State == RunState.CrashPending)
            {
                EndReason = RunEndReason.DriverDownAndOutOfFuel;
                return;
            }
            if (State != RunState.Running) return;
            State = RunState.OutOfFuelPending;
            EndReason = RunEndReason.OutOfFuel;
            gameOverDeadline = Time.time + resultsDelay;
            if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
            gameOverRoutine = StartCoroutine(ShowResultsAfterDeadline());
        }

        private void CancelOutOfFuelCountdown()
        {
            if (State != RunState.OutOfFuelPending) return;
            State = RunState.Running;
            EndReason = RunEndReason.None;
            gameOverDeadline = 0f;
            if (gameOverRoutine != null)
            {
                StopCoroutine(gameOverRoutine);
                gameOverRoutine = null;
            }
        }

        private void FinalizeOutOfFuel()
        {
            if (State != RunState.OutOfFuelPending) return;
            State = RunState.OutOfFuelFinal;
            DisableDrivingAndControls();
        }

        private void DisableDrivingAndControls()
        {
            input?.SetInputEnabled(false);
            if (gameplayControls != null)
            {
                gameplayControls.interactable = false;
                gameplayControls.blocksRaycasts = false;
                gameplayControls.alpha = 0.48f;
            }
            vehicle?.SetDriveEnabled(false);
        }

        private IEnumerator ShowResultsAfterDeadline()
        {
            while (Time.time < gameOverDeadline)
            {
                yield return null;
            }
            DisableDrivingAndControls();
            FinalizeResultSnapshot();
            State = RunState.Results;
            hud?.RefreshImmediate(this);
            if (resultCapture != null)
                yield return resultCapture.Capture(Mode == GameModeType.Adventure2_5D);
            resultsUI?.Show(this);
            Time.timeScale = 0f;
            gameOverRoutine = null;
        }

        private void FinalizeResultSnapshot()
        {
            if (resultSnapshotFinalized) return;

            // Capture the vehicle's last position once, then use one rounded whole-metre value for
            // the HUD, results photo, results statistics and persisted best distance.
            if (vehicle != null) maxProgress = Mathf.Max(maxProgress, vehicle.ProgressPosition);
            finalizedDistance = Mathf.RoundToInt(LiveDistance);
            finalizedRunCoins = runCoins;
            int displayCoinsBeforeSave = SaturatingCurrencyAdd(SaveManager.TotalCoins, finalizedRunCoins);
            BestDistance = SaveManager.SaveRun(finalizedDistance, finalizedRunCoins, Mode, MapId);
            finalizedSavedTotalCoins = SaveManager.TotalCoins;
            finalizedDisplayCoins = SaveManager.TemporaryUnlimitedCurrencyEnabled
                ? displayCoinsBeforeSave : finalizedSavedTotalCoins;
            resultSnapshotFinalized = true;
        }

        private static int SaturatingCurrencyAdd(int balance, int award)
        {
            long total = (long)Mathf.Max(0, balance) + Mathf.Max(0, award);
            return (int)System.Math.Min(int.MaxValue, total);
        }

        public void BeginRun(TransmissionMode mode)
        {
            GameSession.SelectedTransmission = mode;
            vehicle?.SetTransmissionMode(mode);
        }

        public void AssignRuntimeReferences(VehicleRuntimeBase spawnedVehicle, VehicleInputController targetInput,
            GameplayHUD targetHud, ResultsUI targetResults, ResultCaptureService targetCapture, CanvasGroup targetControls)
        {
            vehicle = spawnedVehicle;
            input = targetInput;
            hud = targetHud;
            resultsUI = targetResults;
            resultCapture = targetCapture;
            gameplayControls = targetControls;
        }

        public void MainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("PreStart");
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
