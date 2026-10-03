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
            get => PlayerPrefs.GetInt(RacesCountKey, 0);
            set { PlayerPrefs.SetInt(RacesCountKey, value); PlayerPrefs.Save(); }
        }

        public static int WinsCount
        {
            get => PlayerPrefs.GetInt(WinsCountKey, 0);
            set { PlayerPrefs.SetInt(WinsCountKey, value); PlayerPrefs.Save(); }
        }

        public static int PodiumsCount
        {
            get => PlayerPrefs.GetInt(PodiumsCountKey, 0);
            set { PlayerPrefs.SetInt(PodiumsCountKey, value); PlayerPrefs.Save(); }
        }

        public static string SelectedVehicleId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedVehicleKey, "");
                if (string.IsNullOrEmpty(id))
                {
                    // Fall back to general SaveManager vehicle or default
                    id = SaveManager.SelectedVehicleId;
                    if (string.IsNullOrEmpty(id)) id = "car-01";
                }
                return id;
            }
            set
            {
                PlayerPrefs.SetString(SelectedVehicleKey, value);
                PlayerPrefs.Save();
            }
        }

        public static string SelectedWheelId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedWheelKey, "wheel-default");
                return string.IsNullOrEmpty(id) ? "wheel-default" : id;
            }
            set
            {
                PlayerPrefs.SetString(SelectedWheelKey, value);
                PlayerPrefs.Save();
            }
        }

        public static void RecordRaceResult(int finishPosition, int mmrDelta)
        {
            RacesCount++;
            if (finishPosition == 1) WinsCount++;
            if (finishPosition <= 3) PodiumsCount++;

            CurrentMmr = MmrSystem.ApplyDelta(CurrentMmr, mmrDelta);
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
                PlayerPrefs.SetString(SelectedVehicleKey, SaveManager.SelectedVehicleId ?? "car-01");
            }
            if (!PlayerPrefs.HasKey(SelectedWheelKey))
            {
                PlayerPrefs.SetString(SelectedWheelKey, "wheel-default");
            }

            PlayerPrefs.SetInt(MigrationV1Key, 1);
            PlayerPrefs.Save();
        }
    }
}
