using AnimeHillRacing.Vehicle;
using AnimeHillRacing.Terrain;
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

    public enum CoinUnlockPurchaseResult
    {
        Success,
        AlreadyUnlocked,
        NotEnoughCoins,
        InvalidConfiguration
    }

    public static class SaveManager
    {
        // Keep the switch visible to the existing economy code, but ship against real balances.
        public static bool TemporaryUnlimitedCurrencyEnabled => false;
        public const int UnlimitedCurrencyDisplayAmount = 999_999_999;
#if UNITY_EDITOR
        // Editor progression is granted as a real local balance by EditorMaximumProgressionGrant.
        // Keeping this override disabled makes spending and balance labels behave normally in tests.
        public static bool TemporaryMillionCoinsEnabled => false;
#else
        public static bool TemporaryMillionCoinsEnabled => false;
#endif
        public const int TemporaryCoinDisplayAmount = 1_000_000;
        private static bool HasTemporaryCoins =>
            TemporaryUnlimitedCurrencyEnabled || TemporaryMillionCoinsEnabled;
        public const string DefaultMenuBackgroundId = "default";
        public const string DefaultGarageBackgroundId = "default";

        public const int DistanceBonusMinimum = DistanceBonusTracker.StartingCoins;
        public const int DistanceBonusMaximum = DistanceBonusTracker.MaximumCoins;
        public const int DistanceBonusIncrement = DistanceBonusTracker.IncrementCoins;
        public const float DistanceBonusIntervalMeters = DistanceBonusTracker.IntervalMeters;

#if UNITY_EDITOR
        public const int EditorMaximumCurrencyAmount = 999_999_999;

        public readonly struct EditorMaximumProgressionResult
        {
            public readonly int Maps;
            public readonly int Vehicles;
            public readonly int Drivers;
            public readonly int Skins;
            public readonly int Wheels;
            public readonly int Accessories;
            public readonly int TuneCategories;
            public readonly int BlueprintBundles;

            public EditorMaximumProgressionResult(int maps, int vehicles, int drivers, int skins,
                int wheels, int accessories, int tuneCategories, int blueprintBundles)
            {
                Maps = maps;
                Vehicles = vehicles;
                Drivers = drivers;
                Skins = skins;
                Wheels = wheels;
                Accessories = accessories;
                TuneCategories = tuneCategories;
                BlueprintBundles = blueprintBundles;
            }

            public override string ToString() =>
                $"coins={EditorMaximumCurrencyAmount:N0}, gems={EditorMaximumCurrencyAmount:N0}, " +
                $"maps={Maps}, vehicles={Vehicles}, drivers={Drivers}, skins={Skins}, wheels={Wheels}, " +
                $"accessories={Accessories}, maxed tune categories={TuneCategories}, " +
                $"completed blueprint bundles={BlueprintBundles}, starter pack=owned";
        }
#endif

        private const string TotalCoinsKey = "AHR.TotalCoins";
        private const string TotalGemsKey = "AHR.TotalGems";
        private const string SelectedModeKey = "AHR.SelectedMode";
        private const string SelectedMapKey = "AHR.SelectedMap";
        private const string SelectedCircuitKey = "AHR.SelectedCircuit";
        private const string SelectedVehicleKey = "AHR.SelectedVehicle";
        private const string SelectedTransmissionKey = "AHR.SelectedTransmission";
        private const string SelectedDriverKey = "AHR.Customize.Driver";
        private const string SelectedCharacterAccessoryKey = "AHR.Customize.CharacterAccessory";
        private const string CharacterAccessoryFlipKeyPrefix = "AHR.Customize.CharacterAccessory.Flip.";
        private const string SelectedMenuBackgroundKey = "AHR.Menu.Background";
        private const string SelectedGarageBackgroundKey = "AHR.Garage.Background";
        private const string MasterAudioEnabledKey = "AHR.Settings.MasterAudioEnabled";
        private const string HighestAdventureBestDistanceKey = "AHR.Progress.HighestAdventureBestDistance.V1";
        private const string FormulaWheelMigrationKey = "AHR.Migration.FormulaDefaultWheel02.V1";
        private const string PurchasableContentMigrationKey = "AHR.Migration.PurchasableContent.V1";
        private const string EconomyMigrationKey = "AHR.Migration.Economy.Gems.V1";
        private const string StarterPackOwnedKey = "AHR.Store.StarterPack.Owned.V1";
        private const string StarterPackRewardsGrantedKey =
            "AHR.Store.StarterPack.InitialRewardsGranted.V1";
        private const string PendingPaidCurrencyCloudSyncKey =
            "AHR.Cloud.PendingPaidCurrencySync.V1";
        private const string CompletedRunsSinceInterstitialKey =
            "AHR.Ads.CompletedRunsSinceInterstitial.V1";
        private const string PurchasedGemTransactionKeyPrefix =
            "AHR.Store.Gems.Transaction.V1.";
        private const string FormulaLegacyWheelId = "formula-wheel-rainbow";
        private const string FormulaLuxuryWheelId = "formula-wheel-luxury";
        private const string RallyLegacyWheelId = "rally-wheel-rainbow";
        private const string RallyLuxuryWheelId = "rally-wheel-luxury";

        public static int TotalCoins => TemporaryMillionCoinsEnabled
            ? TemporaryCoinDisplayAmount
            : TemporaryUnlimitedCurrencyEnabled ? UnlimitedCurrencyDisplayAmount : StoredTotalCoins;
        public static int TotalGems => TemporaryUnlimitedCurrencyEnabled
            ? UnlimitedCurrencyDisplayAmount : StoredTotalGems;
        public static bool IsStarterPackOwned => PlayerPrefs.GetInt(StarterPackOwnedKey, 0) != 0;
        public static bool IsStarterPackInitialRewardsGranted =>
            PlayerPrefs.GetInt(StarterPackRewardsGrantedKey, 0) != 0;
        public static float HighestAdventureBestDistance =>
            Mathf.Max(0f, PlayerPrefs.GetFloat(HighestAdventureBestDistanceKey, 0f));
        public static event System.Action CurrencyChanged;

        public static int GetCompletedRunsSinceInterstitial(int frequency)
        {
            int maximumProgress = Mathf.Max(1, frequency) - 1;
            return Mathf.Clamp(PlayerPrefs.GetInt(CompletedRunsSinceInterstitialKey, 0),
                0, maximumProgress);
        }

        /// <summary>
        /// Persists local ad-frequency progress and returns the just-completed run number.
        /// A due run returns <paramref name="frequency"/> while storing zero for the next cycle.
        /// </summary>
        public static int RegisterCompletedRunForInterstitial(int frequency)
        {
            frequency = Mathf.Max(1, frequency);
            int completed = GetCompletedRunsSinceInterstitial(frequency) + 1;
            PlayerPrefs.SetInt(CompletedRunsSinceInterstitialKey,
                completed >= frequency ? 0 : completed);
            PlayerPrefs.Save();
            return completed;
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

        public static string SelectedGarageBackgroundId
        {
            get
            {
                string backgroundId = PlayerPrefs.GetString(SelectedGarageBackgroundKey,
                    DefaultGarageBackgroundId);
                return string.IsNullOrEmpty(backgroundId) ? DefaultGarageBackgroundId : backgroundId;
            }
            set
            {
                PlayerPrefs.SetString(SelectedGarageBackgroundKey,
                    string.IsNullOrEmpty(value) ? DefaultGarageBackgroundId : value);
                PlayerPrefs.Save();
            }
        }

        public static string SelectedMenuBackgroundId
        {
            get
            {
                string backgroundId = PlayerPrefs.GetString(SelectedMenuBackgroundKey,
                    DefaultMenuBackgroundId);
                return string.IsNullOrEmpty(backgroundId) ? DefaultMenuBackgroundId : backgroundId;
            }
            set
            {
                PlayerPrefs.SetString(SelectedMenuBackgroundKey,
                    string.IsNullOrEmpty(value) ? DefaultMenuBackgroundId : value);
                PlayerPrefs.Save();
            }
        }

        public static bool MasterAudioEnabled
        {
            get => PlayerPrefs.GetInt(MasterAudioEnabledKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(MasterAudioEnabledKey, value ? 1 : 0);
                AudioListener.volume = value ? 1f : 0f;
                PlayerPrefs.Save();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyPersistedAudioSetting()
        {
            AudioListener.volume = MasterAudioEnabled ? 1f : 0f;
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

        public static string GetSelectedCharacterAccessoryId(string defaultAccessoryId)
        {
            string selected = PlayerPrefs.GetString(SelectedCharacterAccessoryKey,
                defaultAccessoryId ?? "default");
            if (selected != "baseball-cap-forward" && selected != "baseball-cap-backward")
                return selected;

            // Merge saves created while both cap orientations were separate store entries.
            bool backwards = selected == "baseball-cap-backward";
            selected = "baseball-cap";
            PlayerPrefs.SetString(SelectedCharacterAccessoryKey, selected);
            PlayerPrefs.SetInt(CharacterAccessoryFlipKey(selected), backwards ? 1 : 0);
            PlayerPrefs.Save();
            return selected;
        }

        public static void SetSelectedCharacterAccessoryId(string accessoryId)
        {
            PlayerPrefs.SetString(SelectedCharacterAccessoryKey, accessoryId ?? "default");
            PlayerPrefs.Save();
        }

        public static bool GetCharacterAccessoryFlipX(string accessoryId, bool defaultValue = false)
        {
            string key = CharacterAccessoryFlipKey(accessoryId);
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) != 0 : defaultValue;
        }

        public static bool ToggleCharacterAccessoryFlipX(string accessoryId, bool defaultValue = false)
        {
            bool next = !GetCharacterAccessoryFlipX(accessoryId, defaultValue);
            PlayerPrefs.SetInt(CharacterAccessoryFlipKey(accessoryId), next ? 1 : 0);
            PlayerPrefs.Save();
            return next;
        }

        private static string CharacterAccessoryFlipKey(string accessoryId)
        {
            // Both cap products intentionally share one orientation, so flipping either card is
            // reflected immediately by the free and Rainbow previews.
            string orientationId = accessoryId == "baseball-cap-rainbow" ||
                accessoryId == "baseball-cap-forward" || accessoryId == "baseball-cap-backward"
                    ? "baseball-cap" : accessoryId;
            return CharacterAccessoryFlipKeyPrefix + (orientationId ?? "default");
        }

        public static bool IsCustomizationUnlocked(string scopeId, string category, string optionId,
            bool unlockedByDefault, float requiredBestDistance = 0f)
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
            bool permanentlyUnlocked = PlayerPrefs.HasKey(key)
                ? PlayerPrefs.GetInt(key, 0) != 0 : unlockedByDefault;
            return permanentlyUnlocked || requiredBestDistance > 0f &&
                HighestAdventureBestDistance >= requiredBestDistance;
        }

        public static void UnlockCustomization(string scopeId, string category, string optionId)
        {
            string normalizedId = category == "Wheels" ? NormalizeLegacyWheelId(scopeId, optionId) : optionId;
            PlayerPrefs.SetInt($"AHR.Customize.Unlock.{scopeId}.{category}.{normalizedId}", 1);
            PlayerPrefs.Save();
        }

        public static void AddGems(int amount, bool persistImmediately = true)
        {
            int award = Mathf.Max(0, amount);
            if (award == 0) return;
            long total = (long)StoredTotalGems + award;
            PlayerPrefs.SetInt(TotalGemsKey, (int)System.Math.Min(int.MaxValue, total));
            if (persistImmediately) PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
        }

        public static bool TryGrantPurchasedGems(string productId, string transactionId, int amount,
            out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(transactionId) || amount <= 0)
            {
                error = "GEM PURCHASE TRANSACTION IS INVALID";
                return false;
            }

            string transactionKey = PurchasedGemTransactionKeyPrefix +
                Hash128.Compute(productId + "|" + transactionId);
            // Google Play may redeliver an unacknowledged order after an interruption. Returning
            // success lets IAP confirm it, while this ledger prevents a second currency grant.
            if (PlayerPrefs.GetInt(transactionKey, 0) != 0) return true;

            long total = (long)StoredTotalGems + amount;
            PlayerPrefs.SetInt(TotalGemsKey, (int)System.Math.Min(int.MaxValue, total));
            PlayerPrefs.SetInt(transactionKey, 1);
            PlayerPrefs.SetInt(PendingPaidCurrencyCloudSyncKey, 1);
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
            return true;
        }

        public static void AddCoins(int amount, bool persistImmediately = true)
        {
            int award = Mathf.Max(0, amount);
            if (award == 0) return;
            long total = (long)StoredTotalCoins + award;
            PlayerPrefs.SetInt(TotalCoinsKey, (int)System.Math.Min(int.MaxValue, total));
            if (persistImmediately) PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
        }

        public static bool TrySpendGems(int amount, bool persistImmediately = true)
        {
            if (amount <= 0 || TemporaryUnlimitedCurrencyEnabled) return amount > 0;
            int balance = StoredTotalGems;
            if (balance < amount) return false;
            PlayerPrefs.SetInt(TotalGemsKey, balance - amount);
            if (persistImmediately) PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
            return true;
        }

        public static bool TryPurchaseCoinPack(int coinAmount, int gemCost, out string error)
        {
            error = string.Empty;
            if (coinAmount <= 0 || gemCost <= 0)
            {
                error = "COIN PACK CONFIGURATION IS INVALID";
                return false;
            }
            if (!TemporaryUnlimitedCurrencyEnabled && StoredTotalGems < gemCost)
            {
                error = "NOT ENOUGH GEMS";
                return false;
            }

            if (!TemporaryUnlimitedCurrencyEnabled)
                PlayerPrefs.SetInt(TotalGemsKey, StoredTotalGems - gemCost);
            long coins = (long)StoredTotalCoins + coinAmount;
            PlayerPrefs.SetInt(TotalCoinsKey, (int)System.Math.Min(int.MaxValue, coins));
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
            return true;
        }

        public static bool TrySpendCoins(int amount, bool persistImmediately = true)
        {
            if (amount <= 0 || HasTemporaryCoins) return amount > 0;
            int balance = StoredTotalCoins;
            if (balance < amount) return false;
            PlayerPrefs.SetInt(TotalCoinsKey, balance - amount);
            if (persistImmediately) PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
            return true;
        }

        public static bool TryGrantStarterPack(GameCatalog catalog, StarterPackConfiguration configuration,
            out string error)
        {
            error = string.Empty;
            if (!TryResolveStarterPackContent(catalog, configuration, out VehicleDefinition vehicle,
                    out VehicleDefinition.CustomizationOption skin,
                    out VehicleDefinition.CustomizationOption wheel,
                    out DriverDefinition.CharacterAccessoryOption accessory,
                    out BundleDefinition legacyBundle,
                    out error)) return false;

            bool grantInitialRewards = !IsStarterPackInitialRewardsGranted;
            if (grantInitialRewards)
            {
                long coins = (long)StoredTotalCoins + Mathf.Max(0, configuration.coinReward);
                long gems = (long)StoredTotalGems + Mathf.Max(0, configuration.gemReward);
                PlayerPrefs.SetInt(TotalCoinsKey, (int)System.Math.Min(int.MaxValue, coins));
                PlayerPrefs.SetInt(TotalGemsKey, (int)System.Math.Min(int.MaxValue, gems));
                PlayerPrefs.SetInt(StarterPackRewardsGrantedKey, 1);
                PlayerPrefs.SetInt(PendingPaidCurrencyCloudSyncKey, 1);
            }
            UnlockStarterPackContentWithoutSave(vehicle, skin, wheel, accessory, legacyBundle);
            PlayerPrefs.SetInt(StarterPackOwnedKey, 1);
            PlayerPrefs.Save();
            if (grantInitialRewards) CurrencyChanged?.Invoke();
            return true;
        }

        public static bool IsStarterPackContentConfigured(GameCatalog catalog,
            StarterPackConfiguration configuration, out string error) =>
            TryResolveStarterPackContent(catalog, configuration, out _, out _, out _, out _, out _, out error);

        public static bool RestoreStarterPackOwnership(GameCatalog catalog,
            StarterPackConfiguration configuration, out string error)
        {
            if (!TryResolveStarterPackContent(catalog, configuration, out VehicleDefinition vehicle,
                    out VehicleDefinition.CustomizationOption skin,
                    out VehicleDefinition.CustomizationOption wheel,
                    out DriverDefinition.CharacterAccessoryOption accessory,
                    out BundleDefinition legacyBundle,
                    out error)) return false;

            UnlockStarterPackContentWithoutSave(vehicle, skin, wheel, accessory, legacyBundle);
            PlayerPrefs.SetInt(StarterPackOwnedKey, 1);
            // A store restore proves permanent ownership, but it must never replay the pack's
            // one-time currency payload on a reinstall or another device.
            PlayerPrefs.SetInt(StarterPackRewardsGrantedKey, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static bool GrantPermanentCustomization(string scopeId, string category, string optionId,
            bool persistImmediately = true)
        {
            if (string.IsNullOrWhiteSpace(scopeId) || string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(optionId)) return false;
            string normalizedId = category == "Wheels"
                ? NormalizeLegacyWheelId(scopeId, optionId) : optionId;
            PlayerPrefs.SetInt($"AHR.Customize.Unlock.{scopeId}.{category}.{normalizedId}", 1);
            if (persistImmediately) PlayerPrefs.Save();
            return true;
        }

        internal static bool GrantPermanentVehicle(string vehicleId, bool persistImmediately = true)
        {
            if (string.IsNullOrWhiteSpace(vehicleId)) return false;
            PlayerPrefs.SetInt(VehicleUnlockKey(vehicleId), 1);
            if (persistImmediately) PlayerPrefs.Save();
            return true;
        }

        internal static bool GrantPermanentMap(string mapId, bool persistImmediately = true)
        {
            if (string.IsNullOrWhiteSpace(mapId)) return false;
            PlayerPrefs.SetInt(MapUnlockKey(mapId), 1);
            if (persistImmediately) PlayerPrefs.Save();
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Grants a complete local progression profile for development inside the Unity Editor.
        /// This code is removed from player builds and deliberately leaves race records untouched.
        /// </summary>
        public static EditorMaximumProgressionResult GrantEditorMaximumProgression(GameCatalog catalog)
        {
            if (catalog == null) throw new System.ArgumentNullException(nameof(catalog));

            PlayerPrefs.SetInt(TotalCoinsKey, EditorMaximumCurrencyAmount);
            PlayerPrefs.SetInt(TotalGemsKey, EditorMaximumCurrencyAmount);

            int mapCount = 0;
            if (catalog.maps != null)
                for (int i = 0; i < catalog.maps.Length; i++)
                {
                    MapDefinition map = catalog.maps[i];
                    if (map != null && GrantPermanentMap(map.mapId, false)) mapCount++;
                }

            int vehicleCount = 0;
            int skinCount = 0;
            int wheelCount = 0;
            int tuneCategoryCount = 0;
            if (catalog.vehicles != null)
                for (int i = 0; i < catalog.vehicles.Length; i++)
                {
                    VehicleDefinition vehicle = catalog.vehicles[i];
                    if (vehicle == null || !GrantPermanentVehicle(vehicle.vehicleId, false)) continue;
                    vehicleCount++;
                    skinCount += GrantEditorOptions(vehicle.vehicleId, "Skin", vehicle.skins);
                    wheelCount += GrantEditorOptions(vehicle.vehicleId, "Wheels", vehicle.wheels);

                    foreach (VehicleUpgradeType type in
                             System.Enum.GetValues(typeof(VehicleUpgradeType)))
                    {
                        int maximum = vehicle.GetMaxTunes(type);
                        if (maximum <= 0) continue;
                        SetCloudTuneLevels(vehicle, type, maximum, maximum);
                        tuneCategoryCount++;
                    }
                }

            int driverCount = 0;
            int accessoryCount = 0;
            if (catalog.drivers != null)
                for (int i = 0; i < catalog.drivers.Length; i++)
                {
                    DriverDefinition driver = catalog.drivers[i];
                    if (driver == null) continue;
                    if (GrantPermanentCustomization("global", "Driver", driver.driverId, false))
                        driverCount++;
                    if (driver.accessories == null) continue;
                    for (int j = 0; j < driver.accessories.Length; j++)
                    {
                        DriverDefinition.CharacterAccessoryOption accessory = driver.accessories[j];
                        if (accessory != null && GrantPermanentCustomization("global",
                                "CharacterAccessory", accessory.accessoryId, false)) accessoryCount++;
                    }
                }

            System.Collections.Generic.List<BundleDefinition> bundles =
                PremiumBundleCatalog.GetAll(catalog);
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleDefinition bundle = bundles[i];
                SetCloudBlueprintCount(bundle.BundleId, bundle.BlueprintRequirement,
                    bundle.BlueprintRequirement);
            }

            PlayerPrefs.SetInt(StarterPackOwnedKey, 1);
            PlayerPrefs.SetInt(StarterPackRewardsGrantedKey, 1);
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();

            return new EditorMaximumProgressionResult(mapCount, vehicleCount, driverCount,
                skinCount, wheelCount, accessoryCount, tuneCategoryCount, bundles.Count);
        }

        private static int GrantEditorOptions(string vehicleId, string category,
            VehicleDefinition.CustomizationOption[] options)
        {
            if (options == null) return 0;
            int count = 0;
            for (int i = 0; i < options.Length; i++)
            {
                VehicleDefinition.CustomizationOption option = options[i];
                if (option != null && GrantPermanentCustomization(vehicleId, category,
                        option.optionId, false)) count++;
            }
            return count;
        }
#endif

        internal static void SetCloudCurrencyBalances(int coins, int gems)
        {
            PlayerPrefs.SetInt(TotalCoinsKey, Mathf.Max(0, coins));
            PlayerPrefs.SetInt(TotalGemsKey, Mathf.Max(0, gems));
        }

        internal static float MergeCloudBestDistance(float distance, GameModeType mode, string mapId)
        {
            string key = BestKey(mode, mapId);
            float best = Mathf.Max(PlayerPrefs.GetFloat(key, 0f), Mathf.Max(0f, distance));
            PlayerPrefs.SetFloat(key, best);
            RecordHighestAdventureBestDistance(best, mode);
            LeaderboardSyncService.RecordLocalBest(mode, mapId, best);
            return best;
        }

        internal static void CommitCloudMerge(bool currencyChanged)
        {
            PlayerPrefs.Save();
            if (currencyChanged) CurrencyChanged?.Invoke();
        }

        internal static bool HasPendingPaidCurrencyCloudSync =>
            PlayerPrefs.GetInt(PendingPaidCurrencyCloudSyncKey, 0) != 0;

        internal static void ConfirmPaidCurrencyCloudSync()
        {
            PlayerPrefs.DeleteKey(PendingPaidCurrencyCloudSyncKey);
            PlayerPrefs.Save();
        }

        internal static void SetCloudSelections(GameModeType mode, string mapId, string circuitId,
            string vehicleId, string driverId, string accessoryId, string menuBackgroundId,
            string garageBackgroundId)
        {
            PlayerPrefs.SetInt(SelectedModeKey, (int)mode);
            PlayerPrefs.SetString(SelectedMapKey, mapId ?? string.Empty);
            PlayerPrefs.SetString(SelectedCircuitKey, circuitId ?? string.Empty);
            PlayerPrefs.SetString(SelectedVehicleKey, vehicleId ?? string.Empty);
            PlayerPrefs.SetString(SelectedDriverKey, driverId ?? string.Empty);
            PlayerPrefs.SetString(SelectedCharacterAccessoryKey, accessoryId ?? "default");
            PlayerPrefs.SetString(SelectedMenuBackgroundKey,
                string.IsNullOrEmpty(menuBackgroundId) ? DefaultMenuBackgroundId : menuBackgroundId);
            PlayerPrefs.SetString(SelectedGarageBackgroundKey,
                string.IsNullOrEmpty(garageBackgroundId) ? DefaultGarageBackgroundId : garageBackgroundId);
        }

        internal static void SetCloudVehicleCustomization(VehicleDefinition vehicle, string skinId,
            string wheelId)
        {
            if (vehicle == null) return;
            PlayerPrefs.SetString($"AHR.Customize.{vehicle.vehicleId}.Skin", skinId ?? string.Empty);
            PlayerPrefs.SetString($"AHR.Customize.{vehicle.vehicleId}.Wheel",
                NormalizeLegacyWheelId(vehicle.vehicleId, wheelId));
        }

        internal static void SetCloudCharacterAccessoryFlip(string accessoryId, bool flipX)
        {
            if (string.IsNullOrWhiteSpace(accessoryId)) return;
            PlayerPrefs.SetInt(CharacterAccessoryFlipKey(accessoryId), flipX ? 1 : 0);
        }

        internal static void SetCloudTuneLevels(VehicleDefinition vehicle, VehicleUpgradeType type,
            int purchasedLevel, int equippedLevel)
        {
            if (vehicle == null) return;
            int maximum = vehicle.GetMaxTunes(type);
            if (maximum <= 0) return;
            int purchased = Mathf.Clamp(purchasedLevel, 1, maximum);
            int equipped = Mathf.Clamp(equippedLevel, 1, purchased);
            PlayerPrefs.SetInt(UpgradeLevelKey(vehicle.vehicleId, type), purchased);
            PlayerPrefs.SetInt(EquippedLevelKey(vehicle.vehicleId, type), equipped);
            PlayerPrefs.SetInt(OneBasedSchemaKey(vehicle.vehicleId, type), 1);
        }

        internal static void SetCloudBlueprintCount(string bundleId, int count, int maximum)
        {
            if (string.IsNullOrWhiteSpace(bundleId) || maximum <= 0) return;
            PlayerPrefs.SetInt(BlueprintKey(NormalizeLegacyBundleId(bundleId)),
                Mathf.Clamp(count, 0, maximum));
        }

        internal static void MergeCloudTimeTrial(string circuitId, float bestTime, float bestLap)
        {
            if (string.IsNullOrWhiteSpace(circuitId)) return;
            MergeLowerPositive(TimeKey(circuitId), bestTime);
            MergeLowerPositive(LapKey(circuitId), bestLap);
        }

        private static void MergeLowerPositive(string key, float candidate)
        {
            if (candidate <= 0f || float.IsNaN(candidate) || float.IsInfinity(candidate)) return;
            float current = PlayerPrefs.GetFloat(key, 0f);
            if (current <= 0f || candidate < current) PlayerPrefs.SetFloat(key, candidate);
        }

        public static bool IsVehicleUnlocked(string vehicleId, bool unlockedByDefault)
        {
            EnsurePurchasableContentMigration();
            string key = VehicleUnlockKey(vehicleId);
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) != 0 : unlockedByDefault;
        }

        public static float SynchronizeHighestAdventureBestDistance(GameCatalog catalog)
        {
            float previous = HighestAdventureBestDistance;
            float highest = previous;
            if (catalog?.maps != null)
                for (int i = 0; i < catalog.maps.Length; i++)
                {
                    MapDefinition map = catalog.maps[i];
                    if (map == null || map.supportedGameMode == GameModeType.TimeTrial3D ||
                        string.IsNullOrWhiteSpace(map.mapId)) continue;
                    highest = Mathf.Max(highest, GetBestDistance(map.supportedGameMode, map.mapId));
                }
            if (highest > previous)
            {
                PlayerPrefs.SetFloat(HighestAdventureBestDistanceKey, highest);
                PlayerPrefs.Save();
            }
            return highest;
        }

        public static bool IsMapUnlocked(string mapId, bool unlockedByDefault)
        {
            EnsurePurchasableContentMigration();
            string key = MapUnlockKey(mapId);
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) != 0 : unlockedByDefault;
        }

        public static bool TryPurchaseMap(MapDefinition map, out CoinUnlockPurchaseResult result)
        {
            if (map == null || string.IsNullOrWhiteSpace(map.mapId) || map.coinPrice <= 0)
            {
                result = CoinUnlockPurchaseResult.InvalidConfiguration;
                return false;
            }
            return TryPurchaseCoinUnlock(MapUnlockKey(map.mapId),
                IsMapUnlocked(map.mapId, map.unlockedByDefault), map.coinPrice, out result);
        }

        public static bool TryPurchaseVehicle(VehicleDefinition vehicle,
            out CoinUnlockPurchaseResult result)
        {
            if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.vehicleId) || vehicle.coinPrice <= 0)
            {
                result = CoinUnlockPurchaseResult.InvalidConfiguration;
                return false;
            }
            return TryPurchaseCoinUnlock(VehicleUnlockKey(vehicle.vehicleId),
                IsVehicleUnlocked(vehicle.vehicleId, vehicle.unlockedByDefault), vehicle.coinPrice, out result);
        }

        private static bool TryPurchaseCoinUnlock(string unlockKey, bool alreadyUnlocked, int price,
            out CoinUnlockPurchaseResult result)
        {
            if (alreadyUnlocked)
            {
                result = CoinUnlockPurchaseResult.AlreadyUnlocked;
                return false;
            }
            if (price <= 0 || string.IsNullOrEmpty(unlockKey))
            {
                result = CoinUnlockPurchaseResult.InvalidConfiguration;
                return false;
            }
            if (TotalCoins < price)
            {
                result = CoinUnlockPurchaseResult.NotEnoughCoins;
                return false;
            }

            if (!HasTemporaryCoins)
                PlayerPrefs.SetInt(TotalCoinsKey, StoredTotalCoins - price);
            PlayerPrefs.SetInt(unlockKey, 1);
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
            result = CoinUnlockPurchaseResult.Success;
            return true;
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

            if (!HasTemporaryCoins)
                PlayerPrefs.SetInt(TotalCoinsKey, StoredTotalCoins - coinPrice);
            AddBlueprints(bundle, blueprintAmount, out _, false);
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
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
            CurrencyChanged?.Invoke();
            result = CustomizationPurchaseResult.Success;
            return true;
        }

        public static float GetBestDistance(GameModeType mode, string mapId)
        {
            return PlayerPrefs.GetFloat(BestKey(mode, mapId), 0f);
        }

        public static float SaveBestDistance(float distance, GameModeType mode, string mapId)
        {
            string key = BestKey(mode, mapId);
            float best = Mathf.Max(PlayerPrefs.GetFloat(key, 0f), Mathf.Max(0f, distance));
            PlayerPrefs.SetFloat(key, best);
            RecordHighestAdventureBestDistance(best, mode);
            PlayerPrefs.Save();
            LeaderboardSyncService.RecordLocalBest(mode, mapId, best);
            return best;
        }

        public static float SaveRun(float distance, int runCoins, GameModeType mode, string mapId)
        {
            string key = BestKey(mode, mapId);
            float best = Mathf.Max(PlayerPrefs.GetFloat(key, 0f), distance);
            PlayerPrefs.SetFloat(key, best);
            RecordHighestAdventureBestDistance(best, mode);
            long total = (long)StoredTotalCoins + Mathf.Max(0, runCoins);
            PlayerPrefs.SetInt(TotalCoinsKey, (int)System.Math.Min(int.MaxValue, total));
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
            LeaderboardSyncService.RecordLocalBest(mode, mapId, best);
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
            if (!HasTemporaryCoins)
                PlayerPrefs.SetInt(TotalCoinsKey, StoredTotalCoins - cost);
            resultingTune = current + 1;
            PlayerPrefs.SetInt(UpgradeLevelKey(definition.vehicleId, type), resultingTune);
            PlayerPrefs.SetInt(EquippedLevelKey(definition.vehicleId, type), resultingTune);
            PlayerPrefs.SetInt(OneBasedSchemaKey(definition.vehicleId, type), 1);
            PlayerPrefs.Save();
            CurrencyChanged?.Invoke();
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
        private static string MapUnlockKey(string mapId) => $"AHR.Map.Unlock.{mapId}";

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

        private static void RecordHighestAdventureBestDistance(float distance, GameModeType mode)
        {
            if (mode == GameModeType.TimeTrial3D) return;
            float highest = Mathf.Max(HighestAdventureBestDistance, Mathf.Max(0f, distance));
            PlayerPrefs.SetFloat(HighestAdventureBestDistanceKey, highest);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureStarterPackGrantMigration()
        {
            // Phase 1 had a single ownership key and granted the currency in the same write. Treat
            // every existing owned save as already rewarded so an IAP restore cannot duplicate it.
            if (!IsStarterPackOwned) return;
            if (!PlayerPrefs.HasKey(StarterPackRewardsGrantedKey))
                PlayerPrefs.SetInt(StarterPackRewardsGrantedKey, 1);

            // Starter Pack contents can expand between versions. Existing legitimate owners receive
            // newly added permanent cosmetics, but the one-time currency payload is never replayed.
            StarterPackConfiguration configuration = StarterPackConfiguration.Instance;
            GameCatalog catalog = configuration != null ? configuration.gameCatalog : null;
            if (TryResolveStarterPackContent(catalog, configuration, out VehicleDefinition vehicle,
                    out VehicleDefinition.CustomizationOption skin,
                    out VehicleDefinition.CustomizationOption wheel,
                    out DriverDefinition.CharacterAccessoryOption accessory,
                    out BundleDefinition legacyBundle, out _))
                UnlockStarterPackContentWithoutSave(vehicle, skin, wheel, accessory, legacyBundle);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureEconomyMigration()
        {
            if (PlayerPrefs.GetInt(EconomyMigrationKey, 0) != 0) return;
            // PlayerPrefs already returns zero for a missing key. Persisting it explicitly makes the
            // schema migration visible without touching any existing balance or progression key.
            if (!PlayerPrefs.HasKey(TotalGemsKey)) PlayerPrefs.SetInt(TotalGemsKey, 0);
            PlayerPrefs.SetInt(EconomyMigrationKey, 1);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsurePurchasableContentMigration()
        {
            if (PlayerPrefs.GetInt(PurchasableContentMigrationKey, 0) != 0) return;

            // Before direct coin purchases existed every catalog map and vehicle was available.
            // Preserve only the content a returning player had selected before this one-time marker;
            // later selection changes must never grant an unlock implicitly.
            if (PlayerPrefs.HasKey(SelectedVehicleKey))
            {
                string vehicleId = PlayerPrefs.GetString(SelectedVehicleKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(vehicleId))
                    PlayerPrefs.SetInt(VehicleUnlockKey(vehicleId), 1);
            }
            if (PlayerPrefs.HasKey(SelectedMapKey))
            {
                string mapId = PlayerPrefs.GetString(SelectedMapKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(mapId))
                    PlayerPrefs.SetInt(MapUnlockKey(mapId), 1);
            }

            PlayerPrefs.SetInt(PurchasableContentMigrationKey, 1);
            PlayerPrefs.Save();
        }

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

        private static bool TryResolveStarterPackContent(GameCatalog catalog,
            StarterPackConfiguration configuration, out VehicleDefinition vehicle,
            out VehicleDefinition.CustomizationOption skin,
            out VehicleDefinition.CustomizationOption wheel,
            out DriverDefinition.CharacterAccessoryOption accessory,
            out BundleDefinition legacyBundle,
            out string error)
        {
            vehicle = null;
            skin = null;
            wheel = null;
            accessory = null;
            legacyBundle = null;
            error = string.Empty;
            if (catalog == null || configuration == null)
            {
                error = "STARTER PACK CONFIGURATION NOT AVAILABLE";
                return false;
            }

            bool usesDirectContent = !string.IsNullOrWhiteSpace(configuration.exclusiveVehicleId) ||
                                     !string.IsNullOrWhiteSpace(configuration.exclusiveSkinId) ||
                                     !string.IsNullOrWhiteSpace(configuration.exclusiveWheelId) ||
                                     !string.IsNullOrWhiteSpace(configuration.exclusiveAccessoryId);
            if (usesDirectContent)
            {
                if (string.IsNullOrWhiteSpace(configuration.exclusiveVehicleId) ||
                    string.IsNullOrWhiteSpace(configuration.exclusiveSkinId))
                {
                    error = "STARTER PACK VEHICLE/SKIN IDS ARE INCOMPLETE";
                    return false;
                }

                if (catalog.vehicles != null)
                    for (int i = 0; i < catalog.vehicles.Length; i++)
                        if (catalog.vehicles[i] != null &&
                            catalog.vehicles[i].vehicleId == configuration.exclusiveVehicleId)
                        {
                            vehicle = catalog.vehicles[i];
                            break;
                        }
                if (vehicle == null)
                {
                    error = "STARTER PACK VEHICLE WAS NOT FOUND";
                    return false;
                }

                if (vehicle.skins != null)
                    for (int i = 0; i < vehicle.skins.Length; i++)
                        if (vehicle.skins[i] != null &&
                            vehicle.skins[i].optionId == configuration.exclusiveSkinId)
                        {
                            skin = vehicle.skins[i];
                            break;
                        }
                if (skin == null)
                {
                    error = "STARTER PACK SKIN WAS NOT FOUND ON ITS VEHICLE";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(configuration.exclusiveWheelId))
                {
                    if (vehicle.wheels != null)
                        for (int i = 0; i < vehicle.wheels.Length; i++)
                            if (vehicle.wheels[i] != null &&
                                vehicle.wheels[i].optionId == configuration.exclusiveWheelId)
                            {
                                wheel = vehicle.wheels[i];
                                break;
                            }
                    if (wheel == null)
                    {
                        error = "STARTER PACK WHEEL WAS NOT FOUND ON ITS VEHICLE";
                        return false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(configuration.exclusiveAccessoryId))
                {
                    if (catalog.drivers != null)
                        for (int driverIndex = 0; driverIndex < catalog.drivers.Length && accessory == null;
                             driverIndex++)
                        {
                            DriverDefinition driver = catalog.drivers[driverIndex];
                            if (driver?.accessories == null) continue;
                            for (int optionIndex = 0; optionIndex < driver.accessories.Length; optionIndex++)
                                if (driver.accessories[optionIndex] != null &&
                                    driver.accessories[optionIndex].accessoryId ==
                                    configuration.exclusiveAccessoryId)
                                {
                                    accessory = driver.accessories[optionIndex];
                                    break;
                                }
                        }
                    if (accessory == null)
                    {
                        error = "STARTER PACK CHARACTER ACCESSORY WAS NOT FOUND";
                        return false;
                    }
                }
                return true;
            }

            if (string.IsNullOrWhiteSpace(configuration.exclusiveBundleId))
            {
                error = "EXCLUSIVE STARTER PACK CONTENT NOT CONFIGURED";
                return false;
            }
            legacyBundle = PremiumBundleCatalog.Find(catalog, configuration.exclusiveBundleId);
            if (legacyBundle != null) return true;
            error = "EXCLUSIVE STARTER PACK BUNDLE WAS NOT FOUND";
            return false;
        }

        private static void UnlockStarterPackContentWithoutSave(VehicleDefinition vehicle,
            VehicleDefinition.CustomizationOption skin, VehicleDefinition.CustomizationOption wheel,
            DriverDefinition.CharacterAccessoryOption accessory, BundleDefinition legacyBundle)
        {
            if (legacyBundle != null)
            {
                UnlockBundleWithoutSave(legacyBundle);
                return;
            }
            if (vehicle == null || skin == null) return;
            PlayerPrefs.SetInt(VehicleUnlockKey(vehicle.vehicleId), 1);
            PlayerPrefs.SetInt($"AHR.Customize.Unlock.{vehicle.vehicleId}.Skin.{skin.optionId}", 1);
            if (wheel != null)
                PlayerPrefs.SetInt($"AHR.Customize.Unlock.{vehicle.vehicleId}.Wheels.{wheel.optionId}", 1);
            if (accessory != null)
                PlayerPrefs.SetInt($"AHR.Customize.Unlock.global.CharacterAccessory.{accessory.accessoryId}", 1);
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
