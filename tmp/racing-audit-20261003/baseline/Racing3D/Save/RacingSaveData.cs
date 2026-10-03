using System;
using UnityEngine;
using AnimeHillRacing.Core;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class RacingSaveData
    {
        public int Version = 1;
        public int CurrentMmr = 1000;
        public int HighestMmr = 1000;
        public int RacesCount = 0;
        public int WinsCount = 0;
        public int PodiumsCount = 0;
        public string SelectedVehicleId = "car-01";
        public string SelectedWheelId = "wheel-default";
    }

    public static class RacingSaveManager
    {
        private const string MmrKey = "AHR.Racing.MMR.V1";
        private const string HighestMmrKey = "AHR.Racing.HighestMMR.V1";
        private const string RacesCountKey = "AHR.Racing.RacesCount.V1";
        private const string WinsCountKey = "AHR.Racing.WinsCount.V1";
        private const string PodiumsCountKey = "AHR.Racing.PodiumsCount.V1";
        private const string SelectedVehicleKey = "AHR.Racing.SelectedVehicle.V1";
        private const string SelectedWheelKey = "AHR.Racing.SelectedWheel.V1";
        private const string MigrationV1Key = "AHR.Racing.Migration.V1";
        public static string RecentPerformance => PlayerPrefs.GetString("AHR.Racing.Recent.V1", "NO RACES YET");

        public static int CurrentMmr
        {
            get
            {
                EnsureMigration();
                return Mathf.Max(0, PlayerPrefs.GetInt(MmrKey, 1000));
            }
            set
            {
                EnsureMigration();
                int safeVal = Mathf.Max(0, value);
                PlayerPrefs.SetInt(MmrKey, safeVal);
                if (safeVal > HighestMmr)
                {
                    HighestMmr = safeVal;
                }
                PlayerPrefs.Save();
            }
        }

        public static int HighestMmr
        {
            get
            {
                EnsureMigration();
                return Mathf.Max(CurrentMmr, PlayerPrefs.GetInt(HighestMmrKey, 1000));
            }
            private set
            {
                PlayerPrefs.SetInt(HighestMmrKey, value);
                PlayerPrefs.Save();
            }
        }

        public static int RacesCount
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(RacesCountKey, 0));
            set { PlayerPrefs.SetInt(RacesCountKey, value); PlayerPrefs.Save(); }
        }

        public static int WinsCount
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(WinsCountKey, 0));
            set { PlayerPrefs.SetInt(WinsCountKey, value); PlayerPrefs.Save(); }
        }

        public static int PodiumsCount
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(PodiumsCountKey, 0));
            set { PlayerPrefs.SetInt(PodiumsCountKey, value); PlayerPrefs.Save(); }
        }

        public static string SelectedVehicleId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedVehicleKey, "");
                if (string.IsNullOrEmpty(id) || id == "car-01")
                {
                    id = "formula-01";
                }
                return RacingStatsCalculator.NormalizeVehicleId(id);
            }
            set
            {
                PlayerPrefs.SetString(SelectedVehicleKey, RacingStatsCalculator.NormalizeVehicleId(value));
                PlayerPrefs.Save();
            }
        }

        public static string SelectedWheelId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedWheelKey, "");
                if (string.IsNullOrEmpty(id) || id == "wheel-default")
                {
                    id = "wheel-plasma";
                }
                return RacingStatsCalculator.NormalizeWheelId(id);
            }
            set
            {
                PlayerPrefs.SetString(SelectedWheelKey, RacingStatsCalculator.NormalizeWheelId(value));
                PlayerPrefs.Save();
            }
        }

        public static void RecordRaceResult(int finishPosition, int mmrDelta)
        {
            if (finishPosition < 1 || finishPosition > 12) return;
            EnsureMigration();
            int mmr = MmrSystem.ApplyDelta(CurrentMmr, mmrDelta);
            PlayerPrefs.SetInt(RacesCountKey, RacesCount + 1);
            if (finishPosition == 1) PlayerPrefs.SetInt(WinsCountKey, WinsCount + 1);
            if (finishPosition <= 3) PlayerPrefs.SetInt(PodiumsCountKey, PodiumsCount + 1);
            PlayerPrefs.SetInt(HighestMmrKey, Mathf.Max(HighestMmr, mmr));
            PlayerPrefs.SetInt(MmrKey, mmr);
            string[] recent = PlayerPrefs.GetString("AHR.Racing.Recent.V1", "").Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
            string history = "P" + finishPosition;
            for (int i = 0; i < Mathf.Min(4, recent.Length); i++) history += " / " + recent[i];
            PlayerPrefs.SetString("AHR.Racing.Recent.V1", history);
            PlayerPrefs.Save();
        }

        public static void EnsureMigration()
        {
            if (PlayerPrefs.GetInt(MigrationV1Key, 0) == 1) return;

            // Safe migration: check if legacy Adventure save exists
            // Existing Adventure keys (e.g. AHR.TotalCoins) remain untouched!
            if (!PlayerPrefs.HasKey(MmrKey))
            {
                PlayerPrefs.SetInt(MmrKey, 1000);
            }
            if (!PlayerPrefs.HasKey(HighestMmrKey))
            {
                PlayerPrefs.SetInt(HighestMmrKey, 1000);
            }
            if (!PlayerPrefs.HasKey(SelectedVehicleKey))
            {
                PlayerPrefs.SetString(SelectedVehicleKey, "formula-01");
            }
            if (!PlayerPrefs.HasKey(SelectedWheelKey))
            {
                PlayerPrefs.SetString(SelectedWheelKey, "wheel-plasma");
            }

            PlayerPrefs.SetInt(MigrationV1Key, 1);
            PlayerPrefs.Save();
        }
    }
}
