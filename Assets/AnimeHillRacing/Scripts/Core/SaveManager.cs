using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Core
{
    public enum CustomizationPurchaseResult
    {
        Success,
        AlreadyUnlocked,
        NotEnoughGems,
        InvalidConfiguration
    }

    public enum BlueprintPurchaseResult
    {
        Success,
        AlreadyUnlocked,
        NotEnoughCoins,
        InvalidBundle
    }

    public static class SaveManager
    {
        // Temporary development switch. Set this to false to restore the player's real balances.
        public static bool TemporaryUnlimitedCurrencyEnabled => true;
        public const int UnlimitedCurrencyDisplayAmount = 999_999_999;

        public const int DistanceBonusMinimum = 200;
        public const int DistanceBonusMaximum = 2500;
        public const int DistanceBonusIncrement = 100;

        private const string TotalCoinsKey = "AHR.TotalCoins";
        private const string TotalGemsKey = "AHR.TotalGems";
        private const string SelectedModeKey = "AHR.SelectedMode";
        private const string SelectedMapKey = "AHR.SelectedMap";
        private const string SelectedCircuitKey = "AHR.SelectedCircuit";
        private const string SelectedVehicleKey = "AHR.SelectedVehicle";
        private const string SelectedTransmissionKey = "AHR.SelectedTransmission";
        private const string SelectedDriverKey = "AHR.Customize.Driver";
        private const string SelectedCharacterAccessoryKey = "AHR.Customize.CharacterAccessory";
        private const string DistanceBonusValueKey = "AHR.DistanceBonus.CurrentValue";
        private const string DistanceBonusThresholdKey = "AHR.DistanceBonus.NextThreshold";
        private const string DistanceBonusX10MigrationKey = "AHR.Migration.DistanceBonusX10.V1";
        private const string FormulaWheelMigrationKey = "AHR.Migration.FormulaDefaultWheel02.V1";
        private const string FormulaLegacyWheelId = "formula-wheel-rainbow";
        private const string FormulaLuxuryWheelId = "formula-wheel-luxury";
        private const string RallyLegacyWheelId = "rally-wheel-rainbow";
        private const string RallyLuxuryWheelId = "rally-wheel-luxury";

        public static int TotalCoins => TemporaryUnlimitedCurrencyEnabled
            ? UnlimitedCurrencyDisplayAmount : StoredTotalCoins;
        public static int TotalGems => TemporaryUnlimitedCurrencyEnabled
            ? UnlimitedCurrencyDisplayAmount : StoredTotalGems;
        public static int CurrentDistanceBonusValue
        {
            get
            {
                EnsureDistanceBonusX10Migration();
                return Mathf.Clamp(PlayerPrefs.GetInt(DistanceBonusValueKey, DistanceBonusMinimum),
                    DistanceBonusMinimum, DistanceBonusMaximum);
            }
        }
        public static float NextDistanceBonusThreshold => Mathf.Max(300f,
            PlayerPrefs.GetFloat(DistanceBonusThresholdKey, 300f));

        public static int ClaimDistanceBonus()
        {
            int award = CurrentDistanceBonusValue;
            AddCoins(award, false);
            PlayerPrefs.SetInt(DistanceBonusValueKey, Mathf.Min(DistanceBonusMaximum,
                award + DistanceBonusIncrement));
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
            get => TransmissionMode.Auto;
            set { PlayerPrefs.SetInt(SelectedTransmissionKey, (int)TransmissionMode.Auto); PlayerPrefs.Save(); }
        }

        public static string GetSelectedSkinId(VehicleDefinition vehicle) => GetVehicleCustomization(
            vehicle, "Skin", vehicle != null ? vehicle.DefaultSkinId : string.Empty);

        public static string GetSelectedWheelId(VehicleDefinition vehicle)
        {
            if (vehicle == null) return string.Empty;
            string selected = GetVehicleCustomization(vehicle, "Wheel", vehicle.DefaultWheelId);
            string normalized = NormalizeLegacyWheelId(vehicle.vehicleId, selected);
            if (normalized != selected)
            {
                selected = normalized;
                PlayerPrefs.SetString($"AHR.Customize.{vehicle.vehicleId}.Wheel", selected);
                PlayerPrefs.Save();
            }
            if (vehicle.vehicleId == "formula-01" && !PlayerPrefs.HasKey(FormulaWheelMigrationKey))
            {
                bool ghostyWasExplicitlyUnlocked = IsCustomizationUnlocked(vehicle.vehicleId, "Wheels",
                    "formula-wheel-01", false);
                if (selected == "formula-wheel-01" && !ghostyWasExplicitlyUnlocked)
                {
                    selected = vehicle.DefaultWheelId;
                    PlayerPrefs.SetString($"AHR.Customize.{vehicle.vehicleId}.Wheel", selected);
                }
                PlayerPrefs.SetInt(FormulaWheelMigrationKey, 1);
                PlayerPrefs.Save();
            }
            return selected;
        }

        public static void SetSelectedSkinId(VehicleDefinition vehicle, string optionId) =>
            SetVehicleCustomization(vehicle, "Skin", optionId);

        public static void SetSelectedWheelId(VehicleDefinition vehicle, string optionId) =>
            SetVehicleCustomization(vehicle, "Wheel", vehicle != null
                ? NormalizeLegacyWheelId(vehicle.vehicleId, optionId) : optionId);

        public static string GetSelectedDriverId(string defaultDriverId) =>
            PlayerPrefs.GetString(SelectedDriverKey, defaultDriverId ?? string.Empty);

        public static void SetSelectedDriverId(string driverId)
        {
            PlayerPrefs.SetString(SelectedDriverKey, driverId ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static string GetSelectedCharacterAccessoryId(string defaultAccessoryId) =>
            PlayerPrefs.GetString(SelectedCharacterAccessoryKey, defaultAccessoryId ?? "default");

        public static void SetSelectedCharacterAccessoryId(string accessoryId)
        {
            PlayerPrefs.SetString(SelectedCharacterAccessoryKey, accessoryId ?? "default");
            PlayerPrefs.Save();
        }

        public static bool IsCustomizationUnlocked(string scopeId, string category, string optionId,
            bool unlockedByDefault)
        {
            string normalizedId = category == "Wheels" ? NormalizeLegacyWheelId(scopeId, optionId) : optionId;
            string key = $"AHR.Customize.Unlock.{scopeId}.{category}.{normalizedId}";
            if (!PlayerPrefs.HasKey(key))
            {
                string legacyId = LegacyWheelId(scopeId, normalizedId);
                string legacyKey = $"AHR.Customize.Unlock.{scopeId}.{category}.{legacyId}";
                if (!string.IsNullOrEmpty(legacyId) && PlayerPrefs.HasKey(legacyKey))
                {
                    int unlocked = PlayerPrefs.GetInt(legacyKey, 0);
                    PlayerPrefs.SetInt(key, unlocked);
                    PlayerPrefs.Save();
                    return unlocked != 0;
                }
            }
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) != 0 : unlockedByDefault;
        }

        public static void UnlockCustomization(string scopeId, string category, string optionId)
        {
            string normalizedId = category == "Wheels" ? NormalizeLegacyWheelId(scopeId, optionId) : optionId;
            PlayerPrefs.SetInt($"AHR.Customize.Unlock.{scopeId}.{category}.{normalizedId}", 1);
            PlayerPrefs.Save();
        }

        public static void AddGems(int amount)
        {
            int award = Mathf.Max(0, amount);
            if (award == 0) return;
            long total = (long)StoredTotalGems + award;
            PlayerPrefs.SetInt(TotalGemsKey, (int)System.Math.Min(int.MaxValue, total));
            PlayerPrefs.Save();
        }

        public static void AddCoins(int amount, bool persistImmediately = true)
        {
            int award = Mathf.Max(0, amount);
            if (award == 0) return;
            long total = (long)StoredTotalCoins + award;
            PlayerPrefs.SetInt(TotalCoinsKey, (int)System.Math.Min(int.MaxValue, total));
            if (persistImmediately) PlayerPrefs.Save();
        }

        public static bool IsVehicleUnlocked(string vehicleId, bool unlockedByDefault)
        {
            string key = VehicleUnlockKey(vehicleId);
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) != 0 : unlockedByDefault;
        }

        public static bool IsBundleUnlocked(BundleDefinition bundle)
        {
            if (bundle == null) return true;
            return bundle.Type == BundleType.Vehicle
                ? IsVehicleUnlocked(bundle.OptionId, bundle.UnlockedByDefault)
                : IsCustomizationUnlocked(bundle.ScopeId, bundle.Category, bundle.OptionId,
                    bundle.UnlockedByDefault);
        }

        public static int GetBlueprintCount(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId)) return 0;
            string normalizedId = NormalizeLegacyBundleId(bundleId);
            string key = BlueprintKey(normalizedId);
            if (!PlayerPrefs.HasKey(key))
            {
                string legacyId = LegacyBundleId(normalizedId);
                string legacyKey = BlueprintKey(legacyId);
                if (!string.IsNullOrEmpty(legacyId) && PlayerPrefs.HasKey(legacyKey))
                {
                    int count = Mathf.Max(0, PlayerPrefs.GetInt(legacyKey, 0));
                    PlayerPrefs.SetInt(key, count);
                    PlayerPrefs.Save();
                    return count;
                }
            }
            return Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
        }

        public static int AddBlueprints(BundleDefinition bundle, int amount, out bool unlocked,
            bool persistImmediately = true)
        {
            unlocked = IsBundleUnlocked(bundle);
            if (bundle == null || amount <= 0 || unlocked) return bundle == null ? 0 :
                Mathf.Min(GetBlueprintCount(bundle.BundleId), bundle.BlueprintRequirement);
            int total = BlueprintProgressMath.Add(GetBlueprintCount(bundle.BundleId), amount,
                bundle.BlueprintRequirement);
            PlayerPrefs.SetInt(BlueprintKey(NormalizeLegacyBundleId(bundle.BundleId)), total);
            if (total >= bundle.BlueprintRequirement)
            {
                UnlockBundleWithoutSave(bundle);
                unlocked = true;
            }
            if (persistImmediately) PlayerPrefs.Save();
            return total;
        }

        public static bool TryPurchaseBlueprints(GameCatalog catalog, string bundleId,
            out BlueprintPurchaseResult result)
        {
            return TryPurchaseBlueprints(catalog, bundleId, BlueprintPackTier.Hundred, out result);
        }

        public static bool TryPurchaseBlueprints(GameCatalog catalog, string bundleId,
            BlueprintPackTier tier, out BlueprintPurchaseResult result)
        {
            int blueprintAmount = BlueprintPackCatalog.GetBlueprintAmount(tier);
            int coinPrice = BlueprintPackCatalog.GetCoinPrice(tier);
            BundleDefinition bundle = PremiumBundleCatalog.Find(catalog, bundleId);
            if (bundle == null || !BlueprintPackCatalog.IsValid(tier))
            {
                result = BlueprintPurchaseResult.InvalidBundle;
                return false;
            }
            if (IsBundleUnlocked(bundle))
            {
                result = BlueprintPurchaseResult.AlreadyUnlocked;
                return false;
            }
            if (!BlueprintProgressMath.CanPurchase(TotalCoins, coinPrice, false))
            {
                result = BlueprintPurchaseResult.NotEnoughCoins;
                return false;
            }

            if (!TemporaryUnlimitedCurrencyEnabled)
                PlayerPrefs.SetInt(TotalCoinsKey, StoredTotalCoins - coinPrice);
            AddBlueprints(bundle, blueprintAmount, out _, false);
            PlayerPrefs.Save();
            result = BlueprintPurchaseResult.Success;
            return true;
        }

        public static bool CanAffordGems(int availableGems, int gemPrice) =>
            gemPrice > 0 && Mathf.Max(0, availableGems) >= gemPrice;

        public static bool TryPurchaseCustomization(string scopeId, string category, string optionId, int gemPrice,
            out CustomizationPurchaseResult result)
        {
            if (string.IsNullOrEmpty(scopeId) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(optionId) ||
                gemPrice <= 0)
            {
                result = CustomizationPurchaseResult.InvalidConfiguration;
                return false;
            }
            optionId = category == "Wheels" ? NormalizeLegacyWheelId(scopeId, optionId) : optionId;
            string unlockKey = $"AHR.Customize.Unlock.{scopeId}.{category}.{optionId}";
            if (PlayerPrefs.GetInt(unlockKey, 0) != 0)
            {
                result = CustomizationPurchaseResult.AlreadyUnlocked;
                return false;
            }
            if (!CanAffordGems(TotalGems, gemPrice))
            {
                result = CustomizationPurchaseResult.NotEnoughGems;
                return false;
            }
            if (!TemporaryUnlimitedCurrencyEnabled)
                PlayerPrefs.SetInt(TotalGemsKey, StoredTotalGems - gemPrice);
            PlayerPrefs.SetInt(unlockKey, 1);
            PlayerPrefs.Save();
            result = CustomizationPurchaseResult.Success;
            return true;
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
            long total = (long)StoredTotalCoins + Mathf.Max(0, runCoins);
            PlayerPrefs.SetInt(TotalCoinsKey, (int)System.Math.Min(int.MaxValue, total));
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
            VehicleUpgradeState purchased = LoadPurchasedUpgrades(vehicleId, definition);
            var state = new VehicleUpgradeState();
            state.engineTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Engine,
                definition != null ? definition.engineMaxTunes : 14, purchased.engineTune);
            state.accelerationTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Acceleration,
                definition != null ? definition.accelerationMaxTunes : 15, purchased.accelerationTune);
            state.suspensionTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Suspension,
                definition != null ? definition.suspensionMaxTunes : 12, purchased.suspensionTune);
            state.gripTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Grip,
                definition != null ? definition.gripMaxTunes : 12, purchased.gripTune);
            state.airControlTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.AirControl,
                definition != null ? definition.airControlMaxTunes : 0, purchased.airControlTune);
            state.downforceTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Downforce,
                definition != null ? definition.downforceMaxTunes : 0, purchased.downforceTune);
            state.fuelTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Fuel,
                definition != null ? definition.fuelMaxTunes : 15, purchased.fuelTune);
            state.turboTune = LoadEquippedUpgradeLevel(vehicleId, VehicleUpgradeType.Turbo,
                definition != null ? definition.turboMaxTunes : 0, purchased.turboTune);
            state.ClampTo(definition);
            return state;
        }

        public static VehicleUpgradeState LoadPurchasedUpgrades(string vehicleId, VehicleDefinition definition)
        {
            var state = new VehicleUpgradeState();
            state.engineTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Engine,
                definition != null ? definition.engineMaxTunes : 14);
            state.accelerationTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Acceleration,
                definition != null ? definition.accelerationMaxTunes : 15);
            state.suspensionTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Suspension,
                definition != null ? definition.suspensionMaxTunes : 12);
            state.gripTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Grip,
                definition != null ? definition.gripMaxTunes : 12);
            state.airControlTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.AirControl,
                definition != null ? definition.airControlMaxTunes : 0);
            state.downforceTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Downforce,
                definition != null ? definition.downforceMaxTunes : 0);
            state.fuelTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Fuel,
                definition != null ? definition.fuelMaxTunes : 15);
            state.turboTune = LoadOrMigrateUpgradeLevel(vehicleId, VehicleUpgradeType.Turbo,
                definition != null ? definition.turboMaxTunes : 0);
            state.ClampTo(definition);
            return state;
        }

        public static bool TryIncreaseEquippedTune(VehicleDefinition definition, VehicleUpgradeType type,
            UpgradeEconomyDefinition economy, out int resultingTune, out TunePurchaseResult result)
        {
            if (definition == null)
            {
                resultingTune = 1;
                result = TunePurchaseResult.InvalidConfiguration;
                return false;
            }
            int equipped = LoadUpgrades(definition.vehicleId, definition).GetTune(type);
            int purchased = LoadPurchasedUpgrades(definition.vehicleId, definition).GetTune(type);
            if (equipped < purchased)
            {
                resultingTune = equipped + 1;
                SaveEquippedTune(definition, type, resultingTune, purchased);
                result = TunePurchaseResult.Success;
                return true;
            }
            return TryPurchaseTune(definition, type, economy, out resultingTune, out result);
        }

        public static bool TryDecreaseEquippedTune(VehicleDefinition definition, VehicleUpgradeType type,
            out int resultingTune)
        {
            resultingTune = 1;
            if (definition == null || definition.GetMaxTunes(type) <= 0) return false;
            int current = LoadUpgrades(definition.vehicleId, definition).GetTune(type);
            if (current <= 1)
            {
                resultingTune = current;
                return false;
            }
            int purchased = LoadPurchasedUpgrades(definition.vehicleId, definition).GetTune(type);
            resultingTune = current - 1;
            SaveEquippedTune(definition, type, resultingTune, purchased);
            return true;
        }

        public static bool TryPurchaseTune(VehicleDefinition definition, VehicleUpgradeType type,
            UpgradeEconomyDefinition economy, out int resultingTune)
        {
            return TryPurchaseTune(definition, type, economy, out resultingTune, out _);
        }

        public static bool TryPurchaseTune(VehicleDefinition definition, VehicleUpgradeType type,
            UpgradeEconomyDefinition economy, out int resultingTune, out TunePurchaseResult result)
        {
            if (definition == null || economy == null)
            {
                resultingTune = 1;
                result = TunePurchaseResult.InvalidConfiguration;
                return false;
            }
            int maximum = definition.GetMaxTunes(type);
            int current = LoadPurchasedUpgrades(definition.vehicleId, definition).GetTune(type);
            resultingTune = current;
            if (maximum <= 0 || current >= maximum)
            {
                result = TunePurchaseResult.MaximumLevel;
                return false;
            }
            int cost = economy.GetCostForNextTune(definition, type, current);
            if (cost < 0)
            {
                result = TunePurchaseResult.InvalidConfiguration;
                return false;
            }
            if (TotalCoins < cost)
            {
                result = TunePurchaseResult.NotEnoughCoins;
                return false;
            }
            if (!TemporaryUnlimitedCurrencyEnabled)
                PlayerPrefs.SetInt(TotalCoinsKey, StoredTotalCoins - cost);
            resultingTune = current + 1;
            PlayerPrefs.SetInt(UpgradeLevelKey(definition.vehicleId, type), resultingTune);
            PlayerPrefs.SetInt(EquippedLevelKey(definition.vehicleId, type), resultingTune);
            PlayerPrefs.SetInt(OneBasedSchemaKey(definition.vehicleId, type), 1);
            PlayerPrefs.Save();
            result = TunePurchaseResult.Success;
            return true;
        }

        public static int GetTune(string vehicleId, VehicleUpgradeType type, int maximum)
        {
            return LoadOrMigrateUpgradeLevel(vehicleId, type, maximum);
        }

        private static int LoadOrMigrateUpgradeLevel(string vehicleId, VehicleUpgradeType type, int maximum)
        {
            if (maximum <= 0) return 0;
            string levelKey = UpgradeLevelKey(vehicleId, type);
            string oneBasedKey = OneBasedSchemaKey(vehicleId, type);
            string zeroBasedKey = ZeroBasedSchemaKey(vehicleId, type);
            if (PlayerPrefs.HasKey(oneBasedKey))
                return Mathf.Clamp(PlayerPrefs.GetInt(levelKey, 1), 1, maximum);

            int level = 1;
            if (PlayerPrefs.HasKey(zeroBasedKey))
            {
                level = PlayerPrefs.GetInt(levelKey, 0) + 1;
            }
            else
            {
                string tuneKey = TuneKey(vehicleId, type);
                string oldUpgradeKey = UpgradeKey(vehicleId, type);
                if (PlayerPrefs.HasKey(TuneSchemaKey(vehicleId, type)))
                {
                    level = Mathf.Max(1, PlayerPrefs.GetInt(tuneKey, 1));
                }
                else if (PlayerPrefs.HasKey(tuneKey))
                {
                    level = Mathf.Max(1, PlayerPrefs.GetInt(tuneKey, 0) + 1);
                }
                else if (PlayerPrefs.HasKey(oldUpgradeKey))
                {
                    level = Mathf.Max(1, PlayerPrefs.GetInt(oldUpgradeKey, 1));
                }
                else
                {
                    string stat = type.ToString();
                    string[] legacyKeys = { stat + "Level", "AHR." + stat + "Level", "AHR." + vehicleId + "." + stat + "Level" };
                    for (int i = 0; i < legacyKeys.Length; i++)
                    {
                        if (!PlayerPrefs.HasKey(legacyKeys[i])) continue;
                        level = Mathf.Max(1, PlayerPrefs.GetInt(legacyKeys[i], 1));
                        break;
                    }
                }
            }

            level = Mathf.Clamp(level, 1, maximum);
            PlayerPrefs.SetInt(levelKey, level);
            PlayerPrefs.SetInt(oneBasedKey, 1);
            PlayerPrefs.Save();
            return level;
        }

        private static int LoadEquippedUpgradeLevel(string vehicleId, VehicleUpgradeType type, int maximum,
            int purchasedLevel)
        {
            if (maximum <= 0) return 0;
            int purchased = Mathf.Clamp(purchasedLevel, 1, maximum);
            string key = EquippedLevelKey(vehicleId, type);
            int equipped = PlayerPrefs.HasKey(key)
                ? Mathf.Clamp(PlayerPrefs.GetInt(key, purchased), 1, purchased)
                : purchased;
            if (!PlayerPrefs.HasKey(key) || PlayerPrefs.GetInt(key, equipped) != equipped)
            {
                PlayerPrefs.SetInt(key, equipped);
                PlayerPrefs.Save();
            }
            return equipped;
        }

        private static void SaveEquippedTune(VehicleDefinition definition, VehicleUpgradeType type,
            int equippedLevel, int purchasedLevel)
        {
            int maximum = definition.GetMaxTunes(type);
            if (maximum <= 0) return;
            int purchased = Mathf.Clamp(purchasedLevel, 1, maximum);
            PlayerPrefs.SetInt(EquippedLevelKey(definition.vehicleId, type),
                Mathf.Clamp(equippedLevel, 1, purchased));
            PlayerPrefs.Save();
        }

        private static string BestKey(GameModeType mode, string mapId) =>
            $"AHR.Best.{(int)mode}.{mapId}";

        private static string UpgradeKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.Upgrade.{vehicleId}.{(int)type}";

        private static string TuneKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.Tune.{vehicleId}.{(int)type}";

        private static string TuneSchemaKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.TuneSchema.OneBased.{vehicleId}.{(int)type}";

        private static string UpgradeLevelKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.UpgradeLevel.{vehicleId}.{(int)type}";

        private static string EquippedLevelKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.EquippedTuneLevel.{vehicleId}.{(int)type}";

        private static string ZeroBasedSchemaKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.UpgradeSchema.ZeroBasedV2.{vehicleId}.{(int)type}";

        private static string OneBasedSchemaKey(string vehicleId, VehicleUpgradeType type) =>
            $"AHR.UpgradeSchema.OneBasedV3.{vehicleId}.{(int)type}";

        private static string TimeKey(string circuitId) => $"AHR.BestTime.{circuitId}";
        private static string LapKey(string circuitId) => $"AHR.BestLap.{circuitId}";

        private static string BlueprintKey(string bundleId) => $"AHR.Blueprints.{bundleId}";
        private static string VehicleUnlockKey(string vehicleId) => $"AHR.Vehicle.Unlock.{vehicleId}";

        private static string NormalizeLegacyWheelId(string vehicleId, string optionId)
        {
            if (vehicleId == "formula-01" && optionId == FormulaLegacyWheelId) return FormulaLuxuryWheelId;
            if (vehicleId == "rally-01" && optionId == RallyLegacyWheelId) return RallyLuxuryWheelId;
            return optionId ?? string.Empty;
        }

        private static string LegacyWheelId(string vehicleId, string optionId)
        {
            if (vehicleId == "formula-01" && optionId == FormulaLuxuryWheelId) return FormulaLegacyWheelId;
            if (vehicleId == "rally-01" && optionId == RallyLuxuryWheelId) return RallyLegacyWheelId;
            return string.Empty;
        }

        private static string NormalizeLegacyBundleId(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId)) return string.Empty;
            if (bundleId == $"cosmetic:formula-01:Wheels:{FormulaLegacyWheelId}")
                return $"cosmetic:formula-01:Wheels:{FormulaLuxuryWheelId}";
            if (bundleId == $"cosmetic:rally-01:Wheels:{RallyLegacyWheelId}")
                return $"cosmetic:rally-01:Wheels:{RallyLuxuryWheelId}";
            return bundleId;
        }

        private static string LegacyBundleId(string bundleId)
        {
            if (bundleId == $"cosmetic:formula-01:Wheels:{FormulaLuxuryWheelId}")
                return $"cosmetic:formula-01:Wheels:{FormulaLegacyWheelId}";
            if (bundleId == $"cosmetic:rally-01:Wheels:{RallyLuxuryWheelId}")
                return $"cosmetic:rally-01:Wheels:{RallyLegacyWheelId}";
            return string.Empty;
        }

        private static int StoredTotalCoins => Mathf.Max(0, PlayerPrefs.GetInt(TotalCoinsKey, 0));
        private static int StoredTotalGems => Mathf.Max(0, PlayerPrefs.GetInt(TotalGemsKey, 0));

        private static void EnsureDistanceBonusX10Migration()
        {
            if (PlayerPrefs.GetInt(DistanceBonusX10MigrationKey, 0) != 0) return;
            int legacyValue = PlayerPrefs.GetInt(DistanceBonusValueKey, 20);
            PlayerPrefs.SetInt(DistanceBonusValueKey, ScaleLegacyDistanceBonus(legacyValue));
            PlayerPrefs.SetInt(DistanceBonusX10MigrationKey, 1);
            PlayerPrefs.Save();
        }

        public static int ScaleLegacyDistanceBonus(int legacyValue) =>
            Mathf.Clamp(legacyValue, 20, 250) * 10;

        private static void UnlockBundleWithoutSave(BundleDefinition bundle)
        {
            if (bundle.Type == BundleType.Vehicle)
                PlayerPrefs.SetInt(VehicleUnlockKey(bundle.OptionId), 1);
            else
            {
                string optionId = bundle.Category == "Wheels"
                    ? NormalizeLegacyWheelId(bundle.ScopeId, bundle.OptionId) : bundle.OptionId;
                PlayerPrefs.SetInt($"AHR.Customize.Unlock.{bundle.ScopeId}.{bundle.Category}.{optionId}", 1);
            }
        }

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
