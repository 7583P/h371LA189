using System;
using UnityEngine;
using AnimeHillRacing.Core;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class RacingSaveData
    {
        public int Version = 3;
        public int CurrentMmr = 0;
        public int HighestMmr = 0;
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
                return RacingProgressionService.State.human.currentMmr;
            }
            set
            {
                EnsureMigration();
                RacingProgressionService.SetHumanMmr(value);
            }
        }

        public static int HighestMmr
        {
            get
            {
                EnsureMigration();
                return RacingProgressionService.State.human.peakMmr;
            }
            private set
            {
                RacingProgressionService.State.human.peakMmr = Mathf.Max(CurrentMmr, value);
                RacingProgressionService.MarkChanged(); RacingProgressionService.Save();
            }
        }

        public static int RacesCount
        {
            get => RacingProgressionService.State.human.races;
            set { RacingProgressionService.State.human.races = Mathf.Max(0, value); RacingProgressionService.MarkChanged(); RacingProgressionService.Save(); }
        }

        public static int WinsCount
        {
            get => RacingProgressionService.State.human.wins;
            set { RacingProgressionService.State.human.wins = Mathf.Max(0, value); RacingProgressionService.MarkChanged(); RacingProgressionService.Save(); }
        }

        public static int PodiumsCount
        {
            get => RacingProgressionService.State.human.podiums;
            set { RacingProgressionService.State.human.podiums = Mathf.Max(0, value); RacingProgressionService.MarkChanged(); RacingProgressionService.Save(); }
        }

        public static string SelectedVehicleId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedVehicleKey, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = "formula-01";
                }
                return RacingStatsCalculator.NormalizeVehicleId(id);
            }
            set
            {
                string id=RacingStatsCalculator.NormalizeVehicleId(value);
                PlayerPrefs.SetString(SelectedVehicleKey, id);
                PlayerPrefs.Save();
                RacingProgressionService.State.human.vehiclePreference=id;
                RacingProgressionService.MarkChanged();RacingProgressionService.Save();
            }
        }

        public static string SelectedWheelId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedWheelKey, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = MetaProgressionConfig.MetaWheelId;
                }
                return RacingStatsCalculator.NormalizeWheelId(id);
            }
            set
            {
                string id=RacingStatsCalculator.NormalizeWheelId(value);
                PlayerPrefs.SetString(SelectedWheelKey, id);
                PlayerPrefs.Save();
                RacingProgressionService.State.human.wheelPreference=id;
                RacingProgressionService.MarkChanged();RacingProgressionService.Save();
            }
        }

        public static void RecordRaceResult(int finishPosition, int mmrDelta)
        {
            if (finishPosition < 1 || finishPosition > 12) return;
            EnsureMigration();
            LocalLeagueEngine.RecordResult(RacingProgressionService.State.human, finishPosition, mmrDelta, 0);
            RacingTitleCatalog.UpdateAchievements(RacingProgressionService.State.human);
            RecordRecentFinish(finishPosition);
            RacingProgressionService.MarkChanged(); RacingProgressionService.Save();
        }

        public static void RecordRecentFinish(int finishPosition)
        {
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
                PlayerPrefs.SetInt(MmrKey, MmrSystem.DefaultStartingMmr);
            }
            if (!PlayerPrefs.HasKey(HighestMmrKey))
            {
                PlayerPrefs.SetInt(HighestMmrKey, MmrSystem.DefaultStartingMmr);
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
