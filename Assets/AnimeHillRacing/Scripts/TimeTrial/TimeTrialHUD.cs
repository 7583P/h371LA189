using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.TimeTrial
{
    public sealed class TimeTrialHUD : MonoBehaviour
    {
        [SerializeField] private Text timeText, lapText, bestLapText, speedText, rpmText, gearText, transmissionText, debugText;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private VehicleInputController input;
        private bool debug;

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
            if (speedText != null) speedText.text = $"{vehicle.SpeedKmh:0}\n<size=24>KM/H</size>";
            if (rpmText != null) rpmText.text = $"RPM\n{vehicle.EngineRpm:0}";
            if (gearText != null) gearText.text = $"GEAR {vehicle.Transmission.CurrentGear}";
            if (transmissionText != null) transmissionText.text = vehicle.Transmission.Mode.ToString().ToUpperInvariant();
            if (debug && debugText != null)
                debugText.text = $"TIMER {manager.ElapsedTime:0.000}\nLAP {manager.DisplayLap}/{manager.TotalLaps}\n" +
                                 $"EXPECTED {manager.ExpectedCheckpoint}\nLAST VALID {manager.LastValidCheckpoint}\n" +
                                 $"SEED {manager.Seed}\nSHORTCUT {manager.ShortcutState}\nSPEED {vehicle.SpeedKmh:0.0}";
        }

        public void Assign(Text timer, Text lap, Text bestLap, Text speed, Text rpm, Text gear, Text transmission,
            GameObject debugRoot, Text debugLabel, VehicleInputController targetInput)
        {
            timeText = timer; lapText = lap; bestLapText = bestLap; speedText = speed; rpmText = rpm;
            gearText = gear; transmissionText = transmission; debugPanel = debugRoot; debugText = debugLabel; input = targetInput;
        }
    }
}
