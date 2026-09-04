using System.Collections;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Ads;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.TimeTrial
{
    public sealed class TimeTrialManager : MonoBehaviour
    {
        [SerializeField] private CircuitDefinition circuit;
        [SerializeField] private VehicleRuntimeBase vehicle;
        [SerializeField] private VehicleInputController input;
        [SerializeField] private TimeTrialCircuitGenerator generator;
        [SerializeField] private TimeTrialHUD hud;
        [SerializeField] private ResultsUI results;
        [SerializeField] private ResultCaptureService capture;
        [SerializeField] private CanvasGroup controls;
        [SerializeField] private GameCatalog catalog;

        private int currentNode;
        private int lap = 1;
        private float elapsed;
        private float lapStarted;
        private float bestLap;
        private float maxSpeed;
        private Transform lastValidTransform;
        private bool finished;
        private float nextHorizontalJumpTime;
        private const float HorizontalJumpHeightMeters = 2f;

        public VehicleRuntimeBase Vehicle => vehicle;
        public float ElapsedTime => elapsed;
        public int DisplayLap => Mathf.Min(lap, TotalLaps);
        public int TotalLaps => 1;
        public float DisplayBestLap => bestLap > 0f ? bestLap : SaveManager.GetBestLap(CircuitId);
        public int ExpectedCheckpoint => currentNode == 2 ? 3 : currentNode == 103 ? 4 : (currentNode + 1) % 8;
        public int LastValidCheckpoint => currentNode;
        public int Seed => circuit != null ? circuit.seed : 12345;
        public string ShortcutState { get; private set; } = "NORMAL";
        public float MaxSpeedKmh => maxSpeed;
        public string CircuitId => circuit != null ? circuit.mapId : "procedural-test-circuit";
        public string CircuitName => circuit != null ? circuit.displayName : "PROCEDURAL TEST CIRCUIT";
        public float BestLap => bestLap;

        private void Awake()
        {
            AdMobManager.SetGameplayActive(true);
            Time.timeScale = 1f;
        }

        private void Start()
        {
            input?.SetInputEnabled(true);
            vehicle?.SetTransmissionMode(GameSession.SelectedTransmission);
            if (vehicle?.Fuel != null) vehicle.Fuel.SetConsumptionEnabled(circuit != null && circuit.fuelEnabled);
            results?.HideImmediate();
        }

        private void Update()
        {
            if (finished || vehicle == null) return;
            if (Time.timeScale > 0f && Application.isFocused && catalog != null)
            {
                var granted = ChestSystem.AdvanceActiveGameplay(Time.unscaledDeltaTime, catalog);
                for (int i = 0; i < granted.Count; i++)
                    ChestGameplayNotification.Show(granted[i], catalog.chestCatalog);
            }
            elapsed += Time.deltaTime;
            maxSpeed = Mathf.Max(maxSpeed, vehicle.SpeedKmh);
            hud?.Refresh(this);
            if (input != null && input.RetryPressedThisFrame) Retry();
        }

        public void NotifyCheckpoint(int nodeId, Transform checkpointTransform)
        {
            if (finished || generator == null || generator.Graph == null) return;
            if (!generator.Graph.IsValidTransition(currentNode, nodeId))
            {
                Respawn();
                return;
            }
            currentNode = nodeId;
            lastValidTransform = checkpointTransform;
            if (nodeId == 103) ShortcutState = "TECH / SHORTCUT VALID";
            if (nodeId != 0) return;
            float lapTime = elapsed - lapStarted;
            if (bestLap <= 0f || lapTime < bestLap) bestLap = lapTime;
            lapStarted = elapsed;
            if (lap >= TotalLaps) StartCoroutine(Finish());
            else lap++;
        }

        private IEnumerator Finish()
        {
            finished = true;
            input?.SetInputEnabled(false);
            vehicle?.SetDriveEnabled(false);
            if (controls != null) { controls.interactable = false; controls.blocksRaycasts = false; controls.alpha = 0.48f; }
            SaveManager.SaveTimeTrial(CircuitId, elapsed, bestLap);
            yield return new WaitForSeconds(0.75f);
            if (capture != null) yield return capture.Capture(false);
            results?.ShowTimeTrial(this);
            Time.timeScale = 0f;
            AdMobManager.NotifyRunCompleted();
        }

        public void Respawn()
        {
            if (finished || vehicle == null) return;
            Transform point = lastValidTransform;
            if (point == null && generator != null) generator.TryGetCheckpoint(0, out point);
            if (point == null) return;
            Rigidbody body = vehicle.Body;
            body.position = point.position + point.forward * 4f + Vector3.up * 1.2f;
            body.rotation = Quaternion.LookRotation(point.forward, Vector3.up);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            elapsed += circuit != null ? circuit.respawnPenaltySeconds : 3f;
        }

        public void HorizontalJump()
        {
            if (finished || vehicle == null || !vehicle.AnyGrounded || Time.unscaledTime < nextHorizontalJumpTime) return;
            nextHorizontalJumpTime = Time.unscaledTime + .22f;
            Rigidbody body = vehicle.Body;
            if (body == null) return;
            Vector3 velocity = body.linearVelocity;
            float takeoffSpeed = Mathf.Sqrt(2f * Physics.gravity.magnitude * HorizontalJumpHeightMeters);
            velocity.y = Mathf.Max(0f, velocity.y) + takeoffSpeed;
            body.linearVelocity = velocity;
        }

        public void Retry()
        {
            Time.timeScale = 1f; capture?.Release(); SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void MainMenu()
        {
            Time.timeScale = 1f; capture?.Release(); SceneManager.LoadScene("PreStart");
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f) return "--:--.---";
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - minutes * 60f;
            return $"{minutes:00}:{remainder:00.000}";
        }

        public void Assign(CircuitDefinition definition, VehicleRuntimeBase targetVehicle, VehicleInputController targetInput,
            TimeTrialCircuitGenerator targetGenerator, TimeTrialHUD targetHud, ResultsUI targetResults,
            ResultCaptureService targetCapture, CanvasGroup targetControls)
        {
            circuit = definition; vehicle = targetVehicle; input = targetInput; generator = targetGenerator;
            hud = targetHud; results = targetResults; capture = targetCapture; controls = targetControls;
        }

        public void ConfigureChestCatalog(GameCatalog gameCatalog) => catalog = gameCatalog;

        private void OnDestroy() { ChestSystem.Flush(); if (finished) Time.timeScale = 1f; }
        private void OnApplicationPause(bool paused) { if (paused) ChestSystem.Flush(); }
        private void OnApplicationQuit() => ChestSystem.Flush();
    }
}
