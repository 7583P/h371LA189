using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Core
{
    public static class SaveManager
    {
        private const string TotalCoinsKey = "AHR.TotalCoins";
        private const string SelectedModeKey = "AHR.SelectedMode";
        private const string SelectedMapKey = "AHR.SelectedMap";
        private const string SelectedCircuitKey = "AHR.SelectedCircuit";
        private const string SelectedVehicleKey = "AHR.SelectedVehicle";
        private const string SelectedTransmissionKey = "AHR.SelectedTransmission";
        private const string SelectedDriverKey = "AHR.Customize.Driver";
        private const string DistanceBonusValueKey = "AHR.DistanceBonus.CurrentValue";
        private const string DistanceBonusThresholdKey = "AHR.DistanceBonus.NextThreshold";

        public static int TotalCoins => PlayerPrefs.GetInt(TotalCoinsKey, 0);
        public static int CurrentDistanceBonusValue => Mathf.Clamp(PlayerPrefs.GetInt(DistanceBonusValueKey, 20), 20, 250);
        public static float NextDistanceBonusThreshold => Mathf.Max(300f,
            PlayerPrefs.GetFloat(DistanceBonusThresholdKey, 300f));

        public static int ClaimDistanceBonus()
        {
            int award = CurrentDistanceBonusValue;
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins + award);
            PlayerPrefs.SetInt(DistanceBonusValueKey, Mathf.Min(250, award + 10));
            PlayerPrefs.SetFloat(DistanceBonusThresholdKey, NextDistanceBonusThreshold + 300f);
            PlayerPrefs.Save();
            return award;
        }

        public static GameModeType SelectedGameMode
        {
            get => (GameModeType)Mathf.Clamp(PlayerPrefs.GetInt(SelectedModeKey, 0), 0, 2);
            set { PlayerPrefs.SetInt(SelectedModeKey, (int)value); PlayerPrefs.Save(); }
        }

        public static string SelectedMapId
        {
            get => PlayerPrefs.GetString(SelectedMapKey, "countryside");
            set { PlayerPrefs.SetString(SelectedMapKey, value ?? string.Empty); PlayerPrefs.Save(); }
        }

        public static string SelectedCircuitId
        {
            get => PlayerPrefs.GetString(SelectedCircuitKey, "procedural-test-circuit");
            set { PlayerPrefs.SetString(SelectedCircuitKey, value ?? string.Empty); PlayerPrefs.Save(); }
        }

        public static string SelectedVehicleId
        {
            get => PlayerPrefs.GetString(SelectedVehicleKey, "car-01");
            set { PlayerPrefs.SetString(SelectedVehicleKey, value ?? string.Empty); PlayerPrefs.Save(); }
        }

        public static TransmissionMode SelectedTransmission
        {
            get => (TransmissionMode)Mathf.Clamp(PlayerPrefs.GetInt(SelectedTransmissionKey, 0), 0, 1);
            set { PlayerPrefs.SetInt(SelectedTransmissionKey, (int)value); PlayerPrefs.Save(); }
        }

        public static string GetSelectedSkinId(VehicleDefinition vehicle) => GetVehicleCustomization(
            vehicle, "Skin", vehicle != null ? vehicle.DefaultSkinId : string.Empty);

        public static string GetSelectedWheelId(VehicleDefinition vehicle) => GetVehicleCustomization(
            vehicle, "Wheel", vehicle != null ? vehicle.DefaultWheelId : string.Empty);

        public static void SetSelectedSkinId(VehicleDefinition vehicle, string optionId) =>
            SetVehicleCustomization(vehicle, "Skin", optionId);

        public static void SetSelectedWheelId(VehicleDefinition vehicle, string optionId) =>
            SetVehicleCustomization(vehicle, "Wheel", optionId);

        public static string GetSelectedDriverId(string defaultDriverId) =>
            PlayerPrefs.GetString(SelectedDriverKey, defaultDriverId ?? string.Empty);

        public static void SetSelectedDriverId(string driverId)
        {
            PlayerPrefs.SetString(SelectedDriverKey, driverId ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static bool IsCustomizationUnlocked(string scopeId, string category, string optionId,
            bool unlockedByDefault)
        {
            string key = $"AHR.Customize.Unlock.{scopeId}.{category}.{optionId}";
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) != 0 : unlockedByDefault;
        }

        public static void UnlockCustomization(string scopeId, string category, string optionId)
        {
            PlayerPrefs.SetInt($"AHR.Customize.Unlock.{scopeId}.{category}.{optionId}", 1);
            PlayerPrefs.Save();
        }

        public static float GetBestDistance(GameModeType mode, string mapId)
        {
            return PlayerPrefs.GetFloat(BestKey(mode, mapId), 0f);
        }

        public static float SaveRun(float distance, int runCoins, GameModeType mode, string mapId)
        {
            string key = BestKey(mode, mapId);
            float best = Mathf.Max(PlayerPrefs.GetFloat(key, 0f), distance);
            PlayerPrefs.SetFloat(key, best);
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins + Mathf.Max(0, runCoins));
            PlayerPrefs.Save();
            return best;
        }

        public static float GetBestTime(string circuitId)
        {
            return PlayerPrefs.GetFloat(TimeKey(circuitId), 0f);
        }

        public static float GetBestLap(string circuitId)
        {
            return PlayerPrefs.GetFloat(LapKey(circuitId), 0f);
        }

        public static void SaveTimeTrial(string circuitId, float totalTime, float bestLap)
        {
            float previousTime = GetBestTime(circuitId);
            float previousLap = GetBestLap(circuitId);
            if (totalTime > 0f && (previousTime <= 0f || totalTime < previousTime))
                PlayerPrefs.SetFloat(TimeKey(circuitId), totalTime);
            if (bestLap > 0f && (previousLap <= 0f || bestLap < previousLap))
                PlayerPrefs.SetFloat(LapKey(circuitId), bestLap);
            PlayerPrefs.Save();
        }

        public static VehicleUpgradeState LoadUpgrades(string vehicleId, VehicleDefinition definition)
        {
            var state = new VehicleUpgradeState();
            state.engineTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Engine,
                definition != null ? definition.engineMaxTunes : 14);
            state.accelerationTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Acceleration,
                definition != null ? definition.accelerationMaxTunes : 15);
            state.suspensionTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Suspension,
                definition != null ? definition.suspensionMaxTunes : 12);
            state.gripTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Grip,
                definition != null ? definition.gripMaxTunes : 12);
            state.airControlTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.AirControl,
                definition != null ? definition.airControlMaxTunes : 0);
            state.downforceTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Downforce,
                definition != null ? definition.downforceMaxTunes : 0);
            state.fuelTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Fuel,
                definition != null ? definition.fuelMaxTunes : 15);
            state.turboTune = LoadOrMigrateTune(vehicleId, VehicleUpgradeType.Turbo,
                definition != null ? definition.turboMaxTunes : 0);
            state.ClampTo(definition);
            return state;
        }

        public static bool TryPurchaseTune(VehicleDefinition definition, VehicleUpgradeType type,
            UpgradeEconomyDefinition economy, out int resultingTune)
        {
            if (definition == null) { resultingTune = 0; return false; }
            int maximum = definition.GetMaxTunes(type);
            int current = LoadUpgrades(definition.vehicleId, definition).GetTune(type);
            resultingTune = current;
            if (current >= maximum || economy == null)
                return false;
            int cost = economy.GetCostForNextTune(current - 1);
            if (cost < 0 || TotalCoins < cost)
                return false;
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins - cost);
            resultingTune = current + 1;
            PlayerPrefs.SetInt(TuneKey(definition.vehicleId, type), resultingTune);
            PlayerPrefs.Save();
            return true;
        }

        public static int GetTune(string vehicleId, VehicleUpgradeType type, int maximum)
        {
            return LoadOrMigrateTune(vehicleId, type, maximum);
        }

        private static int LoadOrMigrateTune(string vehicleId, VehicleUpgradeType type, int maximum)
        {
            if (maximum <= 0) return 0;
            string newKey = TuneKey(vehicleId, type);
            string oneBasedKey = TuneSchemaKey(vehicleId, type);
            if (PlayerPrefs.HasKey(oneBasedKey))
                return Mathf.Clamp(PlayerPrefs.GetInt(newKey, 1), 1, maximum);

            string oldUpgradeKey = UpgradeKey(vehicleId, type);
            int tune = 1;
            if (PlayerPrefs.HasKey(newKey))
            {
                tune = PlayerPrefs.GetInt(newKey, 0) + 1;
            }
            else if (PlayerPrefs.HasKey(oldUpgradeKey))
            {
                tune = Mathf.Max(1, PlayerPrefs.GetInt(oldUpgradeKey, 1));
            }
            else
            {
                string stat = type.ToString();
                string[] legacyKeys = { stat + "Level", "AHR." + stat + "Level", "AHR." + vehicleId + "." + stat + "Level" };
                for (int i = 0; i < legacyKeys.Length; i++)
                {
                    if (!PlayerPrefs.HasKey(legacyKeys[i])) continue;
                    tune = Mathf.Max(1, PlayerPrefs.GetInt(legacyKeys[i], 1));
                    break;
                }
            }
            tune = Mathf.Clamp(tune, 1, maximum);
            PlayerPrefs.SetInt(newKey, tune);
            PlayerPrefs.SetInt(oneBasedKey, 1);
            PlayerPrefs.Save();
            return tune;
        }

        private static string BestKey(GameModeType mode, string mapId) =>
            $"AHR.Best.{(int)mode}.{mapId}";

        private static string UpgradeKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.Upgrade.{vehicleId}.{(int)type}";

        private static string TuneKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.Tune.{vehicleId}.{(int)type}";

        private static string TuneSchemaKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.TuneSchema.OneBased.{vehicleId}.{(int)type}";

        private static string TimeKey(string circuitId) => $"AHR.BestTime.{circuitId}";
        private static string LapKey(string circuitId) => $"AHR.BestLap.{circuitId}";

        private static string GetVehicleCustomization(VehicleDefinition vehicle, string category, string fallback)
        {
            if (vehicle == null) return fallback ?? string.Empty;
            return PlayerPrefs.GetString($"AHR.Customize.{vehicle.vehicleId}.{category}", fallback ?? string.Empty);
        }

        private static void SetVehicleCustomization(VehicleDefinition vehicle, string category, string optionId)
        {
            if (vehicle == null) return;
            PlayerPrefs.SetString($"AHR.Customize.{vehicle.vehicleId}.{category}", optionId ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
