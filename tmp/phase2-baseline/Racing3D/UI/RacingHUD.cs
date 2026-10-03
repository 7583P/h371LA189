using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingHUD : MonoBehaviour
    {
        [Header("Manager Binding")]
        [SerializeField] private RaceManager raceManager;

        [Header("UI Text Displays (Optional UI bindings or OnGUI fallback)")]
        [SerializeField] private Text positionText;
        [SerializeField] private Text lapText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text itemText;

        [Header("Mobile Touch Controls")]
        [SerializeField] private GameObject touchControlsRoot;

        private PlayerRacingInput playerInput;
        private RacingVehiclePhysics playerVehicle;
        private TrackProgressTracker progressTracker;
        private ItemInventory playerInventory;
        private RacerProgress playerProgress;

        private void Start()
        {
            if (raceManager == null) raceManager = FindFirstObjectByType<RaceManager>();
            if (raceManager != null)
            {
                raceManager.OnCountdownTick += HandleCountdownTick;
                raceManager.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnCountdownTick -= HandleCountdownTick;
                raceManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            BindPlayerComponents();
            RefreshHUD();
        }

        private void BindPlayerComponents()
        {
            if (playerVehicle != null) return;
            if (raceManager != null && raceManager.PlayerVehicle != null)
            {
                playerVehicle = raceManager.PlayerVehicle;
                playerInput = playerVehicle.GetComponent<PlayerRacingInput>();
                playerInventory = playerVehicle.GetComponent<ItemInventory>();
            }
            if (progressTracker == null && raceManager != null)
            {
                progressTracker = raceManager.ProgressTracker;
            }
            if (progressTracker != null && playerProgress == null)
            {
                playerProgress = progressTracker.GetPlayerProgress();
            }
        }

        private void RefreshHUD()
        {
            if (playerVehicle == null) return;

            // Speed
            int kmh = Mathf.RoundToInt(playerVehicle.SpeedKmh);
            if (speedText != null) speedText.text = $"{kmh} KM/H";

            // Position & Lap
            if (playerProgress != null)
            {
                int pos = playerProgress.RacePosition;
                string suffix = pos == 1 ? "ST" : (pos == 2 ? "ND" : (pos == 3 ? "RD" : "TH"));
                if (positionText != null) positionText.text = $"{pos}{suffix}";

                int lap = Mathf.Min(playerProgress.CurrentLap, raceManager != null ? raceManager.Rules.TotalLaps : 3);
                int totalLaps = raceManager != null ? raceManager.Rules.TotalLaps : 3;
                if (lapText != null)
                {
                    lapText.text = (lap == totalLaps) ? $"FINAL LAP!" : $"LAP {lap}/{totalLaps}";
                }
            }

            // Timer
            if (timerText != null && raceManager != null)
            {
                timerText.text = RacerResultEntry.FormatTime(raceManager.RaceElapsed);
            }

            // Held Item
            if (itemText != null && playerInventory != null)
            {
                if (playerInventory.IsRolling)
                {
                    itemText.text = "???";
                }
                else if (playerInventory.HasItem)
                {
                    itemText.text = playerInventory.HeldItem.ToString().ToUpperInvariant();
                }
                else
                {
                    itemText.text = "";
                }
            }
        }

        private void HandleCountdownTick(int count)
        {
            if (countdownText == null) return;

            if (count > 0)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = count.ToString();
            }
            else
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = "GO!";
                Invoke(nameof(HideCountdown), 1.2f);
            }
        }

        private void HideCountdown()
        {
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }

        private void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Results && touchControlsRoot != null)
            {
                touchControlsRoot.SetActive(false);
            }
        }

        private void OnGUI()
        {
            // OnGUI HUD fallback to guarantee 100% visible HUD even before manual canvas hookup
            if (raceManager == null || playerVehicle == null) return;
            if (raceManager.State == RaceState.Results || raceManager.State == RaceState.Exiting) return;

            GUIStyle styleBox = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            styleBox.normal.textColor = Color.white;

            // 1. Position Badge (Top-Left)
            int pos = playerProgress != null ? playerProgress.RacePosition : 1;
            string suffix = pos == 1 ? "ST" : (pos == 2 ? "ND" : (pos == 3 ? "RD" : "TH"));
            GUI.backgroundColor = pos <= 3 ? new Color(1f, 0.8f, 0.2f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.Box(new Rect(20, 20, 110, 60), $"{pos}{suffix}", styleBox);

            // 2. Lap Display
            int curLap = playerProgress != null ? Mathf.Min(playerProgress.CurrentLap, raceManager.Rules.TotalLaps) : 1;
            int totLaps = raceManager.Rules.TotalLaps;
            string lapStr = (curLap == totLaps) ? "FINAL LAP" : $"LAP {curLap}/{totLaps}";
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            GUI.Box(new Rect(140, 20, 150, 40), lapStr, styleBox);

            // 3. Race Time
            GUI.Box(new Rect(140, 65, 150, 30), RacerResultEntry.FormatTime(raceManager.RaceElapsed), styleBox);

            // 4. Speedometer (Bottom-Right)
            int speedKmh = Mathf.RoundToInt(playerVehicle.SpeedKmh);
            GUI.backgroundColor = playerVehicle.IsBoosting ? new Color(1f, 0.3f, 0.1f, 0.9f) : new Color(0.1f, 0.1f, 0.15f, 0.85f);
            GUI.Box(new Rect(Screen.width - 160, Screen.height - 80, 140, 60), $"{speedKmh} KM/H", styleBox);

            // 5. Held Item Box (Top-Right)
            if (playerInventory != null)
            {
                string itmStr = playerInventory.IsRolling ? "..." : (playerInventory.HasItem ? playerInventory.HeldItem.ToString() : "EMPTY");
                GUI.backgroundColor = playerInventory.HasItem ? new Color(0.2f, 0.7f, 1f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.7f);
                GUI.Box(new Rect(Screen.width - 170, 20, 150, 50), $"ITEM:\n{itmStr}", styleBox);
            }

            // 6. Countdown Overlay
            if (raceManager.State == RaceState.Countdown)
            {
                int count = Mathf.CeilToInt(raceManager.StateTimer);
                string countStr = count > 0 ? count.ToString() : "GO!";
                GUIStyle bigCount = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 54,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                bigCount.normal.textColor = count > 0 ? Color.yellow : Color.green;
                GUI.Label(new Rect((Screen.width - 200) * 0.5f, 140, 200, 100), countStr, bigCount);
            }
        }
    }
}
