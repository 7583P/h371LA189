using System.Collections;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Core
{
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

        private float startProgress;
        private float maxProgress;
        private float maxSpeedKmh;
        private int runCoins;
        private Coroutine gameOverRoutine;
        private float outOfFuelDeadline;
        private int lastDistanceBonus;
        private float distanceBonusMessageUntil;

        public RunState State { get; private set; } = RunState.Ready;
        public RunEndReason EndReason { get; private set; }
        public float Distance => Mathf.Max(0f, maxProgress - startProgress);
        public float BestDistance { get; private set; }
        public float MaxSpeedKmh => maxSpeedKmh;
        public int RunCoins => runCoins;
        public int DisplayCoins => SaveManager.TotalCoins + runCoins;
        public int SavedTotalCoins => SaveManager.TotalCoins;
        public int CurrentDistanceBonusValue => SaveManager.CurrentDistanceBonusValue;
        public float NextDistanceBonusThreshold => SaveManager.NextDistanceBonusThreshold;
        public int LastDistanceBonus => lastDistanceBonus;
        public bool ShowDistanceBonus => Time.unscaledTime < distanceBonusMessageUntil;
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
            State = RunState.Running;
            vehicle.SetDriveEnabled(true);
            resultsUI?.HideImmediate();
        }

        private void Update()
        {
            if (vehicle == null)
                return;
            vehicle.Fuel?.TickRun(Time.deltaTime, State == RunState.Running);
            maxProgress = Mathf.Max(maxProgress, vehicle.ProgressPosition);
            maxSpeedKmh = Mathf.Max(maxSpeedKmh, vehicle.SpeedKmh);
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
            if (State == RunState.Results) RestoreTime();
        }
    }
}
