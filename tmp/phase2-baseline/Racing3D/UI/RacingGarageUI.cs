using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingGarageUI : MonoBehaviour
    {
        private static readonly string[] VehicleIds = new[]
        {
            "car-01", "rally-01", "formula-01", "buggy-01", "sport-bike-01", "hill-adventurer", "aerial-satelite-01"
        };

        private static readonly string[] VehicleNames = new[]
        {
            "CLASSIC JEEP", "RALLY TURBO", "FORMULA APEX", "DESERT BUGGY", "SPORT BIKE", "HILL ADVENTURER", "AERIAL SATELITE"
        };

        private static readonly string[] WheelIds = new[]
        {
            "wheel-default", "wheel-sport", "wheel-offroad", "wheel-racing", "wheel-carbon", "wheel-plasma"
        };

        private static readonly string[] WheelNames = new[]
        {
            "STANDARD ALL-TERRAIN", "SPORT SLICK", "HEAVY OFFROAD", "PRO RACING", "LIGHT CARBON", "PLASMA TURBO"
        };

        private int vehicleIndex;
        private int wheelIndex;
        private FinalRacingStats previewStats;

        private void Start()
        {
            RacingSaveManager.EnsureMigration();

            string currentVehicle = RacingSaveManager.SelectedVehicleId;
            vehicleIndex = Array.FindIndex(VehicleIds, id => id.Equals(currentVehicle, StringComparison.OrdinalIgnoreCase));
            if (vehicleIndex < 0) vehicleIndex = 0;

            string currentWheel = RacingSaveManager.SelectedWheelId;
            wheelIndex = Array.FindIndex(WheelIds, id => id.Equals(currentWheel, StringComparison.OrdinalIgnoreCase));
            if (wheelIndex < 0) wheelIndex = 0;

            RecalculateStats();
        }

        public void NextVehicle()
        {
            vehicleIndex = (vehicleIndex + 1) % VehicleIds.Length;
            SaveSelection();
        }

        public void PreviousVehicle()
        {
            vehicleIndex = (vehicleIndex - 1 + VehicleIds.Length) % VehicleIds.Length;
            SaveSelection();
        }

        public void NextWheel()
        {
            wheelIndex = (wheelIndex + 1) % WheelIds.Length;
            SaveSelection();
        }

        public void PreviousWheel()
        {
            wheelIndex = (wheelIndex - 1 + WheelIds.Length) % WheelIds.Length;
            SaveSelection();
        }

        private void SaveSelection()
        {
            RacingSaveManager.SelectedVehicleId = VehicleIds[vehicleIndex];
            RacingSaveManager.SelectedWheelId = WheelIds[wheelIndex];
            RecalculateStats();
        }

        private void RecalculateStats()
        {
            previewStats = RacingStatsCalculator.Calculate(VehicleIds[vehicleIndex], WheelIds[wheelIndex]);
        }

        public void StartRankedRace()
        {
            SaveSelection();
            SceneManager.LoadScene("Racing3D");
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("PreStart");
        }

        private void OnGUI()
        {
            float panelWidth = Mathf.Min(640f, Screen.width - 40f);
            float panelHeight = Mathf.Min(560f, Screen.height - 40f);
            Rect rect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.backgroundColor = new Color(0.07f, 0.08f, 0.11f, 0.95f);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            GUILayout.Space(14);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label("RACING GARAGE — BUILD & TUNING", titleStyle);

            int mmr = RacingSaveManager.CurrentMmr;
            var tier = MmrTierUtility.GetTier(mmr);
            GUIStyle mmrStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            mmrStyle.normal.textColor = Color.cyan;
            GUILayout.Label($"CURRENT RANK: {MmrTierUtility.GetTierDisplayName(tier)} ({mmr} MMR)", mmrStyle);
            GUILayout.Space(12);

            // Vehicle Selector
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(50), GUILayout.Height(36))) PreviousVehicle();
            GUIStyle selStyle = new GUIStyle(GUI.skin.box) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUILayout.Box($"VEHICLE: {VehicleNames[vehicleIndex]}", selStyle, GUILayout.Height(36), GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", GUILayout.Width(50), GUILayout.Height(36))) NextVehicle();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Wheel Selector
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(50), GUILayout.Height(36))) PreviousWheel();
            GUILayout.Box($"WHEELS: {WheelNames[wheelIndex]}", selStyle, GUILayout.Height(36), GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", GUILayout.Width(50), GUILayout.Height(36))) NextWheel();
            GUILayout.EndHorizontal();

            GUILayout.Space(14);

            // Stats Breakdown
            GUILayout.Label("<b>FINAL RACING PERFORMANCE STATS</b>", GUI.skin.label);
            DrawStatBar("Top Speed", previewStats.SpeedKmh, 100f, 200f, $"{previewStats.SpeedKmh:F0} km/h");
            DrawStatBar("Acceleration", previewStats.AccelerationMps2, 5f, 22f, $"{previewStats.AccelerationMps2:F1} m/s²");
            DrawStatBar("Handling / Yaw", previewStats.HandlingRadPerSec, 1.2f, 4.0f, $"{previewStats.HandlingRadPerSec:F2}");
            DrawStatBar("Drift Power", previewStats.DriftMultiplier, 0.5f, 2.0f, $"{previewStats.DriftMultiplier:F2}x");
            DrawStatBar("Lateral Grip", previewStats.LateralGrip, 0.6f, 2.0f, $"{previewStats.LateralGrip:F2}");
            DrawStatBar("Weight", previewStats.WeightKg, 500f, 1500f, $"{previewStats.WeightKg:F0} kg");
            DrawStatBar("Mini Turbo", previewStats.MiniTurboMultiplier, 0.5f, 2.0f, $"{previewStats.MiniTurboMultiplier:F2}x");
            DrawStatBar("Offroad Grip", previewStats.OffroadRetention * 100f, 20f, 100f, $"{previewStats.OffroadRetention * 100f:F0}%");
            DrawStatBar("Stability", previewStats.Stability, 0.5f, 2.0f, $"{previewStats.Stability:F2}");

            GUILayout.FlexibleSpace();

            // Bottom Buttons
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.4f, 0.45f);
            if (GUILayout.Button("BACK", GUILayout.Width(140), GUILayout.Height(42)))
            {
                BackToMainMenu();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.1f, 0.75f, 0.3f);
            if (GUILayout.Button("START RANKED (12 RACERS)", GUILayout.Width(260), GUILayout.Height(42)))
            {
                StartRankedRace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.EndArea();
        }

        private void DrawStatBar(string label, float val, float min, float max, string displayVal)
        {
            float pct = Mathf.Clamp01((val - min) / (max - min));
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(130));
            Rect barRect = GUILayoutUtility.GetRect(160, 16, GUILayout.ExpandWidth(true));
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
            GUI.Box(barRect, "");
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * pct, barRect.height);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            GUI.Box(fillRect, "");
            GUILayout.Label(displayVal, GUILayout.Width(80));
            GUILayout.EndHorizontal();
        }
    }
}
