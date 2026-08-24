using AnimeHillRacing.Vehicle;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.TimeTrial
{
    public sealed class TimeTrialHUD : MonoBehaviour
    {
        [SerializeField] private Text timeText, lapText, bestLapText, debugText;
        [SerializeField] private RectTransform speedNeedle, rpmNeedle;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private VehicleInputController input;
        [SerializeField] private Image lowFuelWarning;
        [SerializeField, Min(0f)] private float lowFuelWarningSeconds = 5f;
        private bool debug;

        private void Awake()
        {
            SetLowFuelWarningVisible(false);
        }

        private void Update()
        {
            if (input != null && input.ToggleDebugPressedThisFrame)
            {
                debug = !debug; if (debugPanel != null) debugPanel.SetActive(debug);
            }
        }

        public void Refresh(TimeTrialManager manager)
        {
            VehicleRuntimeBase vehicle = manager.Vehicle;
            if (vehicle == null) return;
            if (timeText != null) timeText.text = "TIME\n" + TimeTrialManager.FormatTime(manager.ElapsedTime);
            if (lapText != null) lapText.text = $"LAP\n{manager.DisplayLap} / {manager.TotalLaps}";
            if (bestLapText != null) bestLapText.text = "BEST LAP\n" + TimeTrialManager.FormatTime(manager.DisplayBestLap);
            FuelSystem fuel = vehicle.Fuel;
            SetLowFuelWarningVisible(fuel != null && GameplayHUD.ShouldDisplayLowFuel(
                fuel.CurrentFuel, fuel.CurrentConsumptionPerSecond, lowFuelWarningSeconds));
            SetNeedle(speedNeedle, Mathf.Abs(vehicle.SpeedKmh) / 400f, 172f, -63f);
            SetNeedle(rpmNeedle, vehicle.EngineRpm / 10000f, 159f, -49f);
            if (debug && debugText != null)
                debugText.text = $"TIMER {manager.ElapsedTime:0.000}\nLAP {manager.DisplayLap}/{manager.TotalLaps}\n" +
                                 $"EXPECTED {manager.ExpectedCheckpoint}\nLAST VALID {manager.LastValidCheckpoint}\n" +
                                 $"SEED {manager.Seed}\nSHORTCUT {manager.ShortcutState}\nSPEED {vehicle.SpeedKmh:0.0}";
        }

        public void Assign(Text timer, Text lap, Text bestLap, RectTransform targetSpeedNeedle,
            RectTransform targetRpmNeedle, GameObject debugRoot, Text debugLabel,
            VehicleInputController targetInput)
        {
            timeText = timer; lapText = lap; bestLapText = bestLap;
            speedNeedle = targetSpeedNeedle; rpmNeedle = targetRpmNeedle;
            debugPanel = debugRoot; debugText = debugLabel; input = targetInput;
        }

        public void AssignLowFuelWarning(Image warning)
        {
            if (lowFuelWarning != null && lowFuelWarning != warning)
                lowFuelWarning.gameObject.SetActive(false);
            lowFuelWarning = warning;
            SetLowFuelWarningVisible(false);
        }

        private void SetLowFuelWarningVisible(bool visible)
        {
            if (lowFuelWarning != null && lowFuelWarning.gameObject.activeSelf != visible)
                lowFuelWarning.gameObject.SetActive(visible);
        }

        private static void SetNeedle(RectTransform needle, float normalized, float zeroAngle, float fullAngle)
        {
            if (needle == null) return;
            needle.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Lerp(zeroAngle, fullAngle, Mathf.Clamp01(normalized)));
        }
    }
}
