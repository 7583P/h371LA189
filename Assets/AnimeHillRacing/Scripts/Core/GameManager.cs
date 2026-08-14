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
        private float outOfFuelDeadline;
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

        public RunState State { get; private set; } = RunState.Ready;
        public RunEndReason EndReason { get; private set; }
        public float Distance => Mathf.Max(0f, maxProgress - startProgress);
        public float BestDistance { get; private set; }
        public float MaxSpeedKmh => maxSpeedKmh;
        public int RunCoins => runCoins;
        public int RunFlips => stuntRotation.Flips;
        public int RunBackflips => stuntRotation.Backflips;
        public int StuntBonus => stuntRotation.StuntBonus;
        public int DisplayCoins => SaveManager.TotalCoins + runCoins;
        public int SavedTotalCoins => SaveManager.TotalCoins;
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
        public float OutOfFuelRemaining => State == RunState.OutOfFuelPending
            ? Mathf.Max(0f, outOfFuelDeadline - Time.time) : 0f;

        private void Awake()
        {
            Time.timeScale = 1f;
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
            ResetRunStunts();
            State = RunState.Running;
            vehicle.SetDriveEnabled(true);
            resultsUI?.HideImmediate();
        }

        private void FixedUpdate()
        {
            Rigidbody body = vehicle != null ? vehicle.Body : null;
            if (body == null) return;
            bool runActive = State == RunState.Running;
            bool airborne = runActive && !vehicle.AnyGrounded;
            if (Mode == GameModeType.Adventure2_5D)
            {
                stuntRotation.SampleAngle(body.rotation.eulerAngles.z, airborne, runActive);
                previousStuntRotation = body.rotation;
                stuntRotationInitialized = true;
                return;
            }

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
            Rigidbody body = vehicle != null ? vehicle.Body : null;
            float initialAngle = body != null ? body.rotation.eulerAngles.z : 0f;
            stuntRotation.Reset(initialAngle);
            previousStuntRotation = body != null ? body.rotation : Quaternion.identity;
            stuntRotationInitialized = body != null;
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
                else if (Time.time >= outOfFuelDeadline)
                    FinalizeOutOfFuel();
            }
            else if (State == RunState.Running && vehicle.FuelNormalized <= 0f)
                BeginOutOfFuelCountdown();
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
                if (State == RunState.Running) BeginOutOfFuelCountdown();
                return;
            }
            if (State != RunState.Running && State != RunState.OutOfFuelPending) return;
            State = RunState.CrashPending;
            EndReason = reason;
            DisableDrivingAndControls();
            gameOverRoutine = StartCoroutine(ShowResultsAfterDelay(resultsDelay));
        }

        private void BeginOutOfFuelCountdown()
        {
            if (State != RunState.Running) return;
            State = RunState.OutOfFuelPending;
            EndReason = RunEndReason.OutOfFuel;
            outOfFuelDeadline = Time.time + resultsDelay;
        }

        private void CancelOutOfFuelCountdown()
        {
            State = RunState.Running;
            EndReason = RunEndReason.None;
            outOfFuelDeadline = 0f;
        }

        private void FinalizeOutOfFuel()
        {
            if (State != RunState.OutOfFuelPending) return;
            State = RunState.OutOfFuelFinal;
            DisableDrivingAndControls();
            gameOverRoutine = StartCoroutine(ShowResultsAfterDelay(0f));
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

        private IEnumerator ShowResultsAfterDelay(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (resultCapture != null)
                yield return resultCapture.Capture(Mode == GameModeType.Adventure2_5D);
            BestDistance = SaveManager.SaveRun(Distance, runCoins, Mode, MapId);
            State = RunState.Results;
            resultsUI?.Show(this);
            Time.timeScale = 0f;
            gameOverRoutine = null;
        }

        public void BeginRun(TransmissionMode mode)
        {
            GameSession.SelectedTransmission = mode;
            vehicle?.SetTransmissionMode(mode);
        }

        public void Retry()
        {
            if (gameOverRoutine != null)
                StopCoroutine(gameOverRoutine);
            RestoreTime();
            resultCapture?.Release();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void MainMenu()
        {
            RestoreTime();
            resultCapture?.Release();
            SceneManager.LoadScene("MainMenu");
        }
        public void Menu() => MainMenu();

        public void AssignRuntimeReferences(VehicleRuntimeBase targetVehicle, VehicleInputController targetInput,
            GameplayHUD targetHud, ResultsUI targetResults, ResultCaptureService capture = null,
            CanvasGroup controls = null)
        {
            vehicle = targetVehicle;
            input = targetInput;
            hud = targetHud;
            resultsUI = targetResults;
            resultCapture = capture;
            gameplayControls = controls;
        }

        private static void RestoreTime() => Time.timeScale = 1f;

        private void OnDestroy()
        {
            ChestSystem.Flush();
            if (State == RunState.Results) RestoreTime();
        }

        private void OnApplicationPause(bool paused) { if (paused) ChestSystem.Flush(); }
        private void OnApplicationQuit() => ChestSystem.Flush();
    }
}
