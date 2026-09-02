using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

namespace AnimeHillRacing.Core
{
    /// <summary>
    /// Conservative Saved Games extension for the existing PlayerPrefs save. Billing remains the
    /// authority for real-money entitlements; this service never grants those from cloud data.
    /// </summary>
    public sealed class GooglePlayCloudSaveService : ICloudSaveProvider
    {
        private const int CurrentSaveVersion = 2;
        private const float SyncIntervalSeconds = 60f;
        private const float RequestTimeoutSeconds = 30f;
        private const float RetryDelaySeconds = 10f;
        private const string RevisionKey = "AHR.Cloud.ProgressRevision.V1";
        private const string TimestampKey = "AHR.Cloud.ProgressTimestamp.V1";
        private const string SignatureKey = "AHR.Cloud.ProgressSignature.V1";
        private const string InitializedKey = "AHR.Cloud.Initialized.V1";

        private readonly GameCatalog catalog;
        private readonly StarterPackConfiguration configuration;
        private readonly IAuthenticationProvider authentication;
        private bool wasAuthenticated;
        private bool requestInFlight;
        private float nextAttemptAt;
        private float requestDeadlineAt;
        private int requestSerial;
        private int activeRequestSerial;
        private CloudSaveState state;
        private string lastError = string.Empty;

        public GooglePlayCloudSaveService(GameCatalog catalog,
            StarterPackConfiguration configuration, IAuthenticationProvider authentication)
        {
            this.catalog = catalog;
            this.configuration = configuration;
            this.authentication = authentication;
        }

        public bool IsEnabled => configuration != null && configuration.enableCloudSave &&
                                 catalog != null && IsValidFilename(configuration.cloudSaveFilename);
        public CloudSaveState State => state;
        public string LastError => lastError;
        public event Action StateChanged;

        public void RequestSync()
        {
            if (!IsEnabled)
            {
                SetState(CloudSaveState.Disabled, string.Empty);
                return;
            }
            if (Application.platform != RuntimePlatform.Android || authentication == null ||
                !authentication.IsAuthenticated)
            {
                SetState(CloudSaveState.WaitingForSignIn, string.Empty);
                return;
            }
            if (!requestInFlight)
            {
                nextAttemptAt = Time.unscaledTime + SyncIntervalSeconds;
                Synchronize();
            }
        }

        public void Tick(float unscaledTime)
        {
            if (!IsEnabled)
            {
                SetState(CloudSaveState.Disabled, string.Empty);
                return;
            }
            if (Application.platform != RuntimePlatform.Android) return;
            bool authenticated = authentication != null && authentication.IsAuthenticated;
            if (!authenticated)
            {
                wasAuthenticated = false;
                SetState(CloudSaveState.WaitingForSignIn, string.Empty);
                return;
            }

            if (!wasAuthenticated)
            {
                wasAuthenticated = true;
                nextAttemptAt = unscaledTime;
            }
            if (requestInFlight)
            {
                if (unscaledTime >= requestDeadlineAt)
                {
                    requestInFlight = false;
                    activeRequestSerial = 0;
                    nextAttemptAt = unscaledTime + RetryDelaySeconds;
                    SetState(CloudSaveState.Failed, "CLOUD REQUEST TIMED OUT");
                    Debug.LogWarning("[Cloud Save] Request timed out; progress remains saved locally");
                }
                return;
            }
            if (unscaledTime < nextAttemptAt) return;
            nextAttemptAt = unscaledTime + SyncIntervalSeconds;
            Synchronize();
        }

        private void Synchronize()
        {
#if UNITY_ANDROID
            ISavedGameClient client = PlayGamesPlatform.Instance.SavedGame;
            if (client == null)
            {
                SetState(CloudSaveState.Failed, "SAVED GAMES CLIENT UNAVAILABLE");
                Debug.LogWarning("[Cloud Save] Saved Games client is unavailable");
                return;
            }

            int requestId = ++requestSerial;
            activeRequestSerial = requestId;
            requestInFlight = true;
            requestDeadlineAt = Time.unscaledTime + RequestTimeoutSeconds;
            SetState(CloudSaveState.Synchronizing, string.Empty);
            Debug.Log("[Cloud Save] Synchronization started");
            try
            {
                CloudSnapshot local = CaptureLocalSnapshot(true);
                client.OpenWithManualConflictResolution(configuration.cloudSaveFilename,
                    DataSource.ReadCacheOrNetwork, true,
                    (resolver, original, originalData, unmerged, unmergedData) =>
                        ResolveConflict(requestId, resolver, original, originalData, unmerged,
                            unmergedData),
                    (status, metadata) =>
                        HandleOpenCompleted(requestId, client, local, status, metadata));
            }
            catch (Exception exception)
            {
                FinishWithWarning(requestId,
                    "start failed: " + exception.GetType().Name);
            }
#endif
        }

#if UNITY_ANDROID
        private void ResolveConflict(int requestId, IConflictResolver resolver,
            ISavedGameMetadata original,
            byte[] originalData, ISavedGameMetadata unmerged, byte[] unmergedData)
        {
            if (!IsCurrentRequest(requestId))
            {
                resolver.ChooseMetadata(NewerMetadata(original, unmerged));
                return;
            }

            ParseStatus originalStatus = TryDeserialize(originalData, out CloudSnapshot first);
            ParseStatus unmergedStatus = TryDeserialize(unmergedData, out CloudSnapshot second);

            if (originalStatus == ParseStatus.FutureVersion ||
                unmergedStatus == ParseStatus.FutureVersion)
            {
                // An older client must never rewrite a branch created by a newer save schema.
                ISavedGameMetadata chosen = originalStatus == ParseStatus.FutureVersion &&
                                            unmergedStatus != ParseStatus.FutureVersion
                    ? original : unmergedStatus == ParseStatus.FutureVersion &&
                                 originalStatus != ParseStatus.FutureVersion
                        ? unmerged : NewerMetadata(original, unmerged);
                Debug.LogWarning("[Cloud Save] Future save version detected; preserving it unchanged");
                resolver.ChooseMetadata(chosen);
                return;
            }

            if (!IsReadable(originalStatus) || !IsReadable(unmergedStatus))
            {
                if (IsReadable(originalStatus)) resolver.ChooseMetadata(original);
                else if (IsReadable(unmergedStatus)) resolver.ChooseMetadata(unmerged);
                else resolver.ChooseMetadata(NewerMetadata(original, unmerged));
                Debug.LogWarning("[Cloud Save] Invalid conflict branch preserved without rewriting data");
                return;
            }

            CloudSnapshot merged = Merge(first, second, true, false);
            ISavedGameMetadata baseMetadata = IsNewer(first, second) ? original : unmerged;
            resolver.ResolveConflict(baseMetadata, BuildMetadataUpdate(), Serialize(merged));
        }

        private void HandleOpenCompleted(int requestId, ISavedGameClient client,
            CloudSnapshot local, SavedGameRequestStatus status, ISavedGameMetadata metadata)
        {
            if (!IsCurrentRequest(requestId))
            {
                return;
            }
            if (status != SavedGameRequestStatus.Success || metadata == null)
            {
                FinishWithWarning(requestId, "open failed: " + status);
                return;
            }

            client.ReadBinaryData(metadata, (readStatus, data) =>
            {
                if (!IsCurrentRequest(requestId))
                {
                    return;
                }
                if (readStatus != SavedGameRequestStatus.Success)
                {
                    FinishWithWarning(requestId, "read failed: " + readStatus);
                    return;
                }

                ParseStatus parseStatus = TryDeserialize(data, out CloudSnapshot remote);
                if (!IsReadable(parseStatus))
                {
                    FinishWithWarning(requestId, parseStatus == ParseStatus.FutureVersion
                        ? "newer save version preserved; update the app before syncing"
                        : "cloud data is invalid and was not overwritten");
                    return;
                }

                bool firstSuccessfulSync = PlayerPrefs.GetInt(InitializedKey, 0) == 0;
                bool pendingPaidCurrency = SaveManager.HasPendingPaidCurrencyCloudSync;
                bool preferRemoteOnFirstSync = firstSuccessfulSync && HasMeaningfulProgress(remote) &&
                                               !pendingPaidCurrency;
                CloudSnapshot merged = Merge(local, remote, false, preferRemoteOnFirstSync);
                if (firstSuccessfulSync && pendingPaidCurrency)
                {
                    merged.coins = Mathf.Max(local.coins, remote.coins);
                    merged.gems = Mathf.Max(local.gems, remote.gems);
                    merged.revision = NextRevision(Mathf.Max(local.revision, remote.revision));
                    merged.updatedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                string mergedSignature = ContentSignature(merged);

                ApplyMergedSnapshot(merged);
                StoreLocalMetadata(merged, mergedSignature);
                // CommitUpdate is also the close operation for the open metadata handle. The
                // plugin exposes no discard/close API, so even unchanged data must be committed;
                // otherwise every later attempt can wait forever on the still-open file.
                client.CommitUpdate(metadata, BuildMetadataUpdate(), Serialize(merged),
                    (commitStatus, _) =>
                    {
                        if (!CompleteRequest(requestId)) return;
                        if (commitStatus == SavedGameRequestStatus.Success)
                        {
                            MarkInitialized();
                            SetState(CloudSaveState.Synchronized, string.Empty);
                            Debug.Log("[Cloud Save] Progress synchronized");
                        }
                        else
                        {
                            SetState(CloudSaveState.Failed, "CLOUD COMMIT FAILED");
                            Debug.LogWarning("[Cloud Save] Commit failed: " + commitStatus);
                        }
                    });
            });
        }

        private static ISavedGameMetadata NewerMetadata(ISavedGameMetadata first,
            ISavedGameMetadata second) => first.LastModifiedTimestamp >= second.LastModifiedTimestamp
            ? first : second;

        private static SavedGameMetadataUpdate BuildMetadataUpdate() =>
            new SavedGameMetadataUpdate.Builder()
                .WithUpdatedDescription("Hill Adventurer progress v1")
                .Build();
#endif

        private CloudSnapshot CaptureLocalSnapshot(bool detectLocalChanges)
        {
            var snapshot = NewSnapshot();
            snapshot.coins = SaveManager.TotalCoins;
            snapshot.gems = SaveManager.TotalGems;
            snapshot.revision = Mathf.Max(0, PlayerPrefs.GetInt(RevisionKey, 0));
            long.TryParse(PlayerPrefs.GetString(TimestampKey, "0"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out snapshot.updatedUnixSeconds);
            snapshot.selectedMode = (int)SaveManager.SelectedGameMode;
            snapshot.selectedMapId = SaveManager.SelectedMapId;
            snapshot.selectedCircuitId = SaveManager.SelectedCircuitId;
            snapshot.selectedVehicleId = SaveManager.SelectedVehicleId;
            snapshot.selectedMenuBackgroundId = SaveManager.SelectedMenuBackgroundId;
            snapshot.selectedGarageBackgroundId = SaveManager.SelectedGarageBackgroundId;

            DriverDefinition defaultDriver = catalog.drivers != null && catalog.drivers.Length > 0
                ? catalog.drivers[0] : null;
            snapshot.selectedDriverId = SaveManager.GetSelectedDriverId(
                defaultDriver != null ? defaultDriver.driverId : string.Empty);
            DriverDefinition selectedDriver = FindDriver(snapshot.selectedDriverId) ?? defaultDriver;
            snapshot.selectedAccessoryId = SaveManager.GetSelectedCharacterAccessoryId(
                selectedDriver != null ? selectedDriver.DefaultAccessoryId : "default");

            if (catalog.maps != null)
                for (int i = 0; i < catalog.maps.Length; i++)
                {
                    MapDefinition map = catalog.maps[i];
                    if (map == null || string.IsNullOrWhiteSpace(map.mapId)) continue;
                    long meters = ToMeters(SaveManager.GetBestDistance(map.supportedGameMode, map.mapId));
                    if (meters > 0)
                        snapshot.bestDistances.Add(new DistanceEntry
                        {
                            mode = (int)map.supportedGameMode,
                            mapId = map.mapId,
                            meters = meters
                        });
                    if (!map.unlockedByDefault && SaveManager.IsMapUnlocked(map.mapId, false))
                        snapshot.unlockedMaps.Add(map.mapId);
                    float bestTime = SaveManager.GetBestTime(map.mapId);
                    float bestLap = SaveManager.GetBestLap(map.mapId);
                    if (bestTime > 0f || bestLap > 0f)
                        snapshot.timeTrials.Add(new TimeTrialEntry
                        {
                            circuitId = map.mapId,
                            bestTime = SafePositive(bestTime),
                            bestLap = SafePositive(bestLap)
                        });
                }

            if (catalog.vehicles != null)
                for (int i = 0; i < catalog.vehicles.Length; i++)
                {
                    VehicleDefinition vehicle = catalog.vehicles[i];
                    if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.vehicleId)) continue;
                    if (!vehicle.unlockedByDefault && !IsStarterVehicle(vehicle.vehicleId) &&
                        SaveManager.IsVehicleUnlocked(vehicle.vehicleId, false))
                        snapshot.unlockedVehicles.Add(vehicle.vehicleId);
                    CaptureOptions(snapshot, vehicle, vehicle.skins, "Skin");
                    CaptureOptions(snapshot, vehicle, vehicle.wheels, "Wheels");
                    snapshot.equippedCustomizations.Add(new EquippedCustomizationEntry
                    {
                        vehicleId = vehicle.vehicleId,
                        skinId = SaveManager.GetSelectedSkinId(vehicle),
                        wheelId = SaveManager.GetSelectedWheelId(vehicle)
                    });
                    VehicleUpgradeState purchased = SaveManager.LoadPurchasedUpgrades(
                        vehicle.vehicleId, vehicle);
                    VehicleUpgradeState equipped = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
                    for (int typeValue = (int)VehicleUpgradeType.Engine;
                         typeValue <= (int)VehicleUpgradeType.Turbo; typeValue++)
                    {
                        var type = (VehicleUpgradeType)typeValue;
                        if (vehicle.GetMaxTunes(type) <= 0) continue;
                        snapshot.vehicleTunes.Add(new VehicleTuneEntry
                        {
                            vehicleId = vehicle.vehicleId,
                            type = typeValue,
                            purchasedLevel = purchased.GetTune(type),
                            equippedLevel = equipped.GetTune(type)
                        });
                    }
                }

            CaptureDriverAccessories(snapshot);
            List<BundleDefinition> bundles = PremiumBundleCatalog.GetAll(catalog);
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleDefinition bundle = bundles[i];
                if (!IsSafeInternalBundle(bundle)) continue;
                int count = SaveManager.GetBlueprintCount(bundle.BundleId);
                if (count > 0)
                    snapshot.blueprints.Add(new BlueprintEntry
                    {
                        bundleId = bundle.BundleId,
                        count = Mathf.Min(count, bundle.BlueprintRequirement)
                    });
            }

            snapshot.chestSaveJson = ChestSystem.ExportCloudJson();
            snapshot.hasChestProgress = ChestSystem.GameplayProgress > 0f ||
                                        ChestSystem.Inventory.Count > 0;

            Normalize(snapshot);
            string signature = ContentSignature(snapshot);
            if (detectLocalChanges)
            {
                string previous = PlayerPrefs.GetString(SignatureKey, string.Empty);
                bool firstObservation = string.IsNullOrEmpty(previous);
                bool changed = !firstObservation && !string.Equals(previous, signature,
                    StringComparison.Ordinal);
                bool initialized = PlayerPrefs.GetInt(InitializedKey, 0) != 0;
                if (initialized && (changed || firstObservation && HasMeaningfulProgress(snapshot)))
                {
                    snapshot.revision = NextRevision(snapshot.revision);
                    snapshot.updatedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                PlayerPrefs.SetString(SignatureKey, signature);
                PlayerPrefs.SetInt(RevisionKey, snapshot.revision);
                PlayerPrefs.SetString(TimestampKey,
                    snapshot.updatedUnixSeconds.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }
            return snapshot;
        }

        private void CaptureOptions(CloudSnapshot snapshot, VehicleDefinition vehicle,
            VehicleDefinition.CustomizationOption[] options, string category)
        {
            if (options == null) return;
            for (int i = 0; i < options.Length; i++)
            {
                VehicleDefinition.CustomizationOption option = options[i];
                if (option == null || option.unlockedByDefault ||
                    option.requiredBestDistance > 0f ||
                    string.IsNullOrWhiteSpace(option.optionId) ||
                    option.purchaseCurrency == CustomizationPurchaseCurrency.RealMoney ||
                    IsStarterCustomization(vehicle.vehicleId, category, option.optionId)) continue;
                if (SaveManager.IsCustomizationUnlocked(vehicle.vehicleId, category,
                        option.optionId, false, option.requiredBestDistance))
                    snapshot.unlockedCustomizations.Add(new CustomizationEntry
                    {
                        scopeId = vehicle.vehicleId,
                        category = category,
                        optionId = option.optionId
                    });
            }
        }

        private void CaptureDriverAccessories(CloudSnapshot snapshot)
        {
            if (catalog.drivers == null) return;
            for (int driverIndex = 0; driverIndex < catalog.drivers.Length; driverIndex++)
            {
                DriverDefinition driver = catalog.drivers[driverIndex];
                if (driver?.accessories == null) continue;
                for (int optionIndex = 0; optionIndex < driver.accessories.Length; optionIndex++)
                {
                    DriverDefinition.CharacterAccessoryOption option = driver.accessories[optionIndex];
                    if (option == null || string.IsNullOrWhiteSpace(option.accessoryId)) continue;
                    if (!option.unlockedByDefault && !IsStarterAccessory(option.accessoryId) &&
                        SaveManager.IsCustomizationUnlocked("global",
                            "CharacterAccessory", option.accessoryId, false))
                        snapshot.unlockedCustomizations.Add(new CustomizationEntry
                        {
                            scopeId = "global",
                            category = "CharacterAccessory",
                            optionId = option.accessoryId
                        });
                    if (option.supportsFlipVariant)
                        snapshot.accessoryFlips.Add(new AccessoryFlipEntry
                        {
                            accessoryId = option.accessoryId,
                            flipX = SaveManager.GetCharacterAccessoryFlipX(option.accessoryId,
                                option.flipX)
                        });
                }
            }
        }

        private bool IsSafeInternalBundle(BundleDefinition bundle)
        {
            if (bundle == null) return false;
            if (bundle.Type == BundleType.Vehicle)
                return FindVehicle(bundle.OptionId) != null && !IsStarterVehicle(bundle.OptionId);
            return IsSafeInternalCustomization(new CustomizationEntry
            {
                scopeId = bundle.ScopeId,
                category = bundle.Category,
                optionId = bundle.OptionId
            });
        }

        private void ApplyMergedSnapshot(CloudSnapshot snapshot)
        {
            int previousCoins = SaveManager.TotalCoins;
            int previousGems = SaveManager.TotalGems;
            SaveManager.SetCloudCurrencyBalances(snapshot.coins, snapshot.gems);

            for (int i = 0; i < snapshot.bestDistances.Count; i++)
            {
                DistanceEntry entry = snapshot.bestDistances[i];
                MapDefinition map = FindMap(entry.mode, entry.mapId);
                if (map != null)
                    SaveManager.MergeCloudBestDistance((float)Math.Max(0L, entry.meters),
                        map.supportedGameMode, map.mapId);
            }
            for (int i = 0; i < snapshot.unlockedMaps.Count; i++)
                if (FindMap(-1, snapshot.unlockedMaps[i]) != null)
                    SaveManager.GrantPermanentMap(snapshot.unlockedMaps[i], false);
            for (int i = 0; i < snapshot.unlockedVehicles.Count; i++)
                if (FindVehicle(snapshot.unlockedVehicles[i]) != null &&
                    !IsStarterVehicle(snapshot.unlockedVehicles[i]))
                    SaveManager.GrantPermanentVehicle(snapshot.unlockedVehicles[i], false);
            for (int i = 0; i < snapshot.unlockedCustomizations.Count; i++)
            {
                CustomizationEntry entry = snapshot.unlockedCustomizations[i];
                if (IsSafeInternalCustomization(entry))
                    SaveManager.GrantPermanentCustomization(entry.scopeId, entry.category,
                        entry.optionId, false);
            }

            for (int i = 0; i < snapshot.timeTrials.Count; i++)
            {
                TimeTrialEntry entry = snapshot.timeTrials[i];
                MapDefinition map = FindMap(-1, entry.circuitId);
                if (map != null && map.supportedGameMode == GameModeType.TimeTrial3D)
                    SaveManager.MergeCloudTimeTrial(entry.circuitId, entry.bestTime, entry.bestLap);
            }
            for (int i = 0; i < snapshot.vehicleTunes.Count; i++)
            {
                VehicleTuneEntry entry = snapshot.vehicleTunes[i];
                VehicleDefinition vehicle = FindVehicle(entry.vehicleId);
                if (vehicle == null || entry.type < (int)VehicleUpgradeType.Engine ||
                    entry.type > (int)VehicleUpgradeType.Turbo) continue;
                SaveManager.SetCloudTuneLevels(vehicle, (VehicleUpgradeType)entry.type,
                    entry.purchasedLevel, entry.equippedLevel);
            }
            for (int i = 0; i < snapshot.blueprints.Count; i++)
            {
                BlueprintEntry entry = snapshot.blueprints[i];
                BundleDefinition bundle = PremiumBundleCatalog.Find(catalog, entry.bundleId);
                if (IsSafeInternalBundle(bundle))
                    SaveManager.SetCloudBlueprintCount(bundle.BundleId, entry.count,
                        bundle.BlueprintRequirement);
            }

            if (!ChestSystem.TryImportCloudJson(snapshot.chestSaveJson, catalog))
                Debug.LogWarning("[Cloud Save] Chest section was ignored");
            ApplyCloudSelections(snapshot);

            SaveManager.CommitCloudMerge(previousCoins != snapshot.coins || previousGems != snapshot.gems);
        }

        private void ApplyCloudSelections(CloudSnapshot snapshot)
        {
            GameModeType mode = snapshot.selectedMode >= (int)GameModeType.Adventure2_5D &&
                                snapshot.selectedMode <= (int)GameModeType.TimeTrial3D
                ? (GameModeType)snapshot.selectedMode : SaveManager.SelectedGameMode;
            string mapId = SaveManager.SelectedMapId;
            MapDefinition selectedMap = FindMap((int)mode, snapshot.selectedMapId);
            if (selectedMap != null && SaveManager.IsMapUnlocked(selectedMap.mapId,
                    selectedMap.unlockedByDefault)) mapId = selectedMap.mapId;

            string circuitId = SaveManager.SelectedCircuitId;
            MapDefinition selectedCircuit = FindMap((int)GameModeType.TimeTrial3D,
                snapshot.selectedCircuitId);
            if (selectedCircuit != null && SaveManager.IsMapUnlocked(selectedCircuit.mapId,
                    selectedCircuit.unlockedByDefault)) circuitId = selectedCircuit.mapId;

            string vehicleId = SaveManager.SelectedVehicleId;
            VehicleDefinition selectedVehicle = FindVehicle(snapshot.selectedVehicleId);
            if (selectedVehicle != null && SaveManager.IsVehicleUnlocked(selectedVehicle.vehicleId,
                    selectedVehicle.unlockedByDefault)) vehicleId = selectedVehicle.vehicleId;

            DriverDefinition driver = FindDriver(snapshot.selectedDriverId) ??
                                      FindDriver(SaveManager.GetSelectedDriverId(string.Empty));
            string driverId = driver != null ? driver.driverId : string.Empty;
            string accessoryId = SaveManager.GetSelectedCharacterAccessoryId(
                driver != null ? driver.DefaultAccessoryId : "default");
            DriverDefinition.CharacterAccessoryOption accessory = FindAccessory(
                snapshot.selectedAccessoryId);
            if (accessory != null && SaveManager.IsCustomizationUnlocked("global",
                    "CharacterAccessory", accessory.accessoryId, accessory.unlockedByDefault))
                accessoryId = accessory.accessoryId;

            string menuBackground = snapshot.selectedMenuBackgroundId == "neon"
                ? "neon" : SaveManager.DefaultMenuBackgroundId;
            string garageBackground = snapshot.selectedGarageBackgroundId == "neon" ||
                                      snapshot.selectedGarageBackgroundId == "anime"
                ? snapshot.selectedGarageBackgroundId : SaveManager.DefaultGarageBackgroundId;
            SaveManager.SetCloudSelections(mode, mapId, circuitId, vehicleId, driverId, accessoryId,
                menuBackground, garageBackground);

            for (int i = 0; i < snapshot.equippedCustomizations.Count; i++)
            {
                EquippedCustomizationEntry entry = snapshot.equippedCustomizations[i];
                VehicleDefinition vehicle = FindVehicle(entry.vehicleId);
                if (vehicle == null) continue;
                VehicleDefinition.CustomizationOption skin = vehicle.FindSkin(entry.skinId);
                VehicleDefinition.CustomizationOption wheel = vehicle.FindWheel(entry.wheelId);
                string skinId = skin != null && SaveManager.IsCustomizationUnlocked(vehicle.vehicleId,
                    "Skin", skin.optionId, skin.unlockedByDefault, skin.requiredBestDistance)
                    ? skin.optionId : SaveManager.GetSelectedSkinId(vehicle);
                string wheelId = wheel != null && SaveManager.IsCustomizationUnlocked(vehicle.vehicleId,
                    "Wheels", wheel.optionId, wheel.unlockedByDefault, wheel.requiredBestDistance)
                    ? wheel.optionId : SaveManager.GetSelectedWheelId(vehicle);
                SaveManager.SetCloudVehicleCustomization(vehicle, skinId, wheelId);
            }
            for (int i = 0; i < snapshot.accessoryFlips.Count; i++)
            {
                AccessoryFlipEntry entry = snapshot.accessoryFlips[i];
                DriverDefinition.CharacterAccessoryOption option = FindAccessory(entry.accessoryId);
                if (option != null && option.supportsFlipVariant)
                    SaveManager.SetCloudCharacterAccessoryFlip(option.accessoryId, entry.flipX);
            }
        }

        private MapDefinition FindMap(int mode, string mapId)
        {
            if (catalog?.maps == null || string.IsNullOrWhiteSpace(mapId)) return null;
            for (int i = 0; i < catalog.maps.Length; i++)
            {
                MapDefinition map = catalog.maps[i];
                if (map != null && map.mapId == mapId &&
                    (mode < 0 || (int)map.supportedGameMode == mode)) return map;
            }
            return null;
        }

        private VehicleDefinition FindVehicle(string vehicleId)
        {
            if (catalog?.vehicles == null || string.IsNullOrWhiteSpace(vehicleId)) return null;
            for (int i = 0; i < catalog.vehicles.Length; i++)
                if (catalog.vehicles[i] != null && catalog.vehicles[i].vehicleId == vehicleId)
                    return catalog.vehicles[i];
            return null;
        }

        private DriverDefinition FindDriver(string driverId)
        {
            if (catalog?.drivers == null || string.IsNullOrWhiteSpace(driverId)) return null;
            for (int i = 0; i < catalog.drivers.Length; i++)
                if (catalog.drivers[i] != null && catalog.drivers[i].driverId == driverId)
                    return catalog.drivers[i];
            return null;
        }

        private DriverDefinition.CharacterAccessoryOption FindAccessory(string accessoryId)
        {
            if (catalog?.drivers == null || string.IsNullOrWhiteSpace(accessoryId)) return null;
            for (int driverIndex = 0; driverIndex < catalog.drivers.Length; driverIndex++)
            {
                DriverDefinition driver = catalog.drivers[driverIndex];
                if (driver?.accessories == null) continue;
                for (int optionIndex = 0; optionIndex < driver.accessories.Length; optionIndex++)
                    if (driver.accessories[optionIndex] != null &&
                        driver.accessories[optionIndex].accessoryId == accessoryId)
                        return driver.accessories[optionIndex];
            }
            return null;
        }

        private bool IsSafeInternalCustomization(CustomizationEntry entry)
        {
            if (entry == null) return false;
            if (entry.scopeId == "global" && entry.category == "CharacterAccessory")
                return !IsStarterAccessory(entry.optionId) && FindAccessory(entry.optionId) != null;
            VehicleDefinition vehicle = FindVehicle(entry.scopeId);
            if (vehicle == null || IsStarterCustomization(entry.scopeId, entry.category,
                    entry.optionId)) return false;
            VehicleDefinition.CustomizationOption[] options = entry.category == "Skin"
                ? vehicle.skins : entry.category == "Wheels" ? vehicle.wheels : null;
            if (options == null) return false;
            for (int i = 0; i < options.Length; i++)
                if (options[i] != null && options[i].optionId == entry.optionId)
                    return options[i].purchaseCurrency != CustomizationPurchaseCurrency.RealMoney;
            return false;
        }

        private bool IsStarterVehicle(string vehicleId) => configuration != null &&
            !string.IsNullOrWhiteSpace(configuration.exclusiveVehicleId) &&
            configuration.exclusiveVehicleId == vehicleId;

        private bool IsStarterCustomization(string vehicleId, string category, string optionId)
        {
            if (configuration == null || configuration.exclusiveVehicleId != vehicleId) return false;
            return category == "Skin" && configuration.exclusiveSkinId == optionId ||
                   category == "Wheels" && configuration.exclusiveWheelId == optionId;
        }

        private bool IsStarterAccessory(string accessoryId) => configuration != null &&
            !string.IsNullOrWhiteSpace(configuration.exclusiveAccessoryId) &&
            configuration.exclusiveAccessoryId == accessoryId;

        private static CloudSnapshot Merge(CloudSnapshot first, CloudSnapshot second,
            bool forceNewRevision, bool preferSecondMutable)
        {
            Normalize(first);
            Normalize(second);
            CloudSnapshot newer = preferSecondMutable ? second : IsNewer(first, second) ? first : second;
            var result = NewSnapshot();
            CopyMutableState(newer, result);
            result.bestDistances = MergeDistances(first.bestDistances, second.bestDistances);
            result.unlockedMaps = MergeStrings(first.unlockedMaps, second.unlockedMaps);
            result.unlockedVehicles = MergeStrings(first.unlockedVehicles, second.unlockedVehicles);
            result.unlockedCustomizations = MergeCustomizations(first.unlockedCustomizations,
                second.unlockedCustomizations);
            result.timeTrials = MergeTimeTrials(first.timeTrials, second.timeTrials);
            result.vehicleTunes = MergeVehicleTunes(first.vehicleTunes, second.vehicleTunes,
                newer.vehicleTunes);
            result.blueprints = MergeBlueprints(first.blueprints, second.blueprints);

            bool contentChanged = ContentSignature(result) != ContentSignature(newer);
            result.revision = Mathf.Max(first.revision, second.revision);
            result.updatedUnixSeconds = Math.Max(first.updatedUnixSeconds, second.updatedUnixSeconds);
            if (contentChanged || forceNewRevision)
            {
                result.revision = NextRevision(result.revision);
                result.updatedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            return result;
        }

        private static void CopyMutableState(CloudSnapshot source, CloudSnapshot target)
        {
            target.coins = source.coins;
            target.gems = source.gems;
            target.selectedMode = source.selectedMode;
            target.selectedMapId = source.selectedMapId;
            target.selectedCircuitId = source.selectedCircuitId;
            target.selectedVehicleId = source.selectedVehicleId;
            target.selectedDriverId = source.selectedDriverId;
            target.selectedAccessoryId = source.selectedAccessoryId;
            target.selectedMenuBackgroundId = source.selectedMenuBackgroundId;
            target.selectedGarageBackgroundId = source.selectedGarageBackgroundId;
            target.equippedCustomizations = CloneEquippedCustomizations(
                source.equippedCustomizations);
            target.accessoryFlips = CloneAccessoryFlips(source.accessoryFlips);
            target.chestSaveJson = source.chestSaveJson;
            target.hasChestProgress = source.hasChestProgress;
        }

        private static List<TimeTrialEntry> MergeTimeTrials(List<TimeTrialEntry> first,
            List<TimeTrialEntry> second)
        {
            var values = new Dictionary<string, TimeTrialEntry>(StringComparer.Ordinal);
            AddTimeTrials(values, first);
            AddTimeTrials(values, second);
            var result = new List<TimeTrialEntry>(values.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.circuitId, right.circuitId));
            return result;
        }

        private static void AddTimeTrials(Dictionary<string, TimeTrialEntry> target,
            List<TimeTrialEntry> source)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                TimeTrialEntry entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.circuitId)) continue;
                float bestTime = SafePositive(entry.bestTime);
                float bestLap = SafePositive(entry.bestLap);
                if (!target.TryGetValue(entry.circuitId, out TimeTrialEntry current))
                {
                    target[entry.circuitId] = new TimeTrialEntry
                    {
                        circuitId = entry.circuitId,
                        bestTime = bestTime,
                        bestLap = bestLap
                    };
                    continue;
                }
                current.bestTime = LowerPositive(current.bestTime, bestTime);
                current.bestLap = LowerPositive(current.bestLap, bestLap);
            }
        }

        private static List<VehicleTuneEntry> MergeVehicleTunes(List<VehicleTuneEntry> first,
            List<VehicleTuneEntry> second, List<VehicleTuneEntry> preferred)
        {
            var purchased = new Dictionary<string, VehicleTuneEntry>(StringComparer.Ordinal);
            AddVehicleTunes(purchased, first);
            AddVehicleTunes(purchased, second);
            var preferredByKey = new Dictionary<string, VehicleTuneEntry>(StringComparer.Ordinal);
            AddVehicleTunes(preferredByKey, preferred);
            foreach (KeyValuePair<string, VehicleTuneEntry> pair in purchased)
            {
                VehicleTuneEntry value = pair.Value;
                if (preferredByKey.TryGetValue(pair.Key, out VehicleTuneEntry chosen))
                    value.equippedLevel = Mathf.Clamp(chosen.equippedLevel, 1,
                        value.purchasedLevel);
            }
            var result = new List<VehicleTuneEntry>(purchased.Values);
            result.Sort((left, right) => string.CompareOrdinal(TuneKey(left), TuneKey(right)));
            return result;
        }

        private static void AddVehicleTunes(Dictionary<string, VehicleTuneEntry> target,
            List<VehicleTuneEntry> source)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                VehicleTuneEntry entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.vehicleId) || entry.type < 0 ||
                    entry.type > (int)VehicleUpgradeType.Turbo) continue;
                string key = TuneKey(entry);
                int level = Mathf.Max(1, entry.purchasedLevel);
                if (!target.TryGetValue(key, out VehicleTuneEntry current))
                    target[key] = new VehicleTuneEntry
                    {
                        vehicleId = entry.vehicleId,
                        type = entry.type,
                        purchasedLevel = level,
                        equippedLevel = Mathf.Clamp(entry.equippedLevel, 1, level)
                    };
                else if (level > current.purchasedLevel)
                {
                    current.purchasedLevel = level;
                    current.equippedLevel = Mathf.Clamp(current.equippedLevel, 1, level);
                }
            }
        }

        private static string TuneKey(VehicleTuneEntry entry) => entry.vehicleId + "|" + entry.type;

        private static List<BlueprintEntry> MergeBlueprints(List<BlueprintEntry> first,
            List<BlueprintEntry> second)
        {
            var values = new Dictionary<string, BlueprintEntry>(StringComparer.Ordinal);
            AddBlueprints(values, first);
            AddBlueprints(values, second);
            var result = new List<BlueprintEntry>(values.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.bundleId, right.bundleId));
            return result;
        }

        private static void AddBlueprints(Dictionary<string, BlueprintEntry> target,
            List<BlueprintEntry> source)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                BlueprintEntry entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.bundleId) || entry.count <= 0)
                    continue;
                if (!target.TryGetValue(entry.bundleId, out BlueprintEntry current) ||
                    entry.count > current.count)
                    target[entry.bundleId] = new BlueprintEntry
                    {
                        bundleId = entry.bundleId,
                        count = entry.count
                    };
            }
        }

        private static List<EquippedCustomizationEntry> CloneEquippedCustomizations(
            List<EquippedCustomizationEntry> source)
        {
            var values = new Dictionary<string, EquippedCustomizationEntry>(StringComparer.Ordinal);
            if (source != null)
                for (int i = 0; i < source.Count; i++)
                {
                    EquippedCustomizationEntry entry = source[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.vehicleId)) continue;
                    values[entry.vehicleId] = new EquippedCustomizationEntry
                    {
                        vehicleId = entry.vehicleId,
                        skinId = entry.skinId ?? string.Empty,
                        wheelId = entry.wheelId ?? string.Empty
                    };
                }
            var result = new List<EquippedCustomizationEntry>(values.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.vehicleId, right.vehicleId));
            return result;
        }

        private static List<AccessoryFlipEntry> CloneAccessoryFlips(List<AccessoryFlipEntry> source)
        {
            var values = new Dictionary<string, AccessoryFlipEntry>(StringComparer.Ordinal);
            if (source != null)
                for (int i = 0; i < source.Count; i++)
                {
                    AccessoryFlipEntry entry = source[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.accessoryId)) continue;
                    values[entry.accessoryId] = new AccessoryFlipEntry
                    {
                        accessoryId = entry.accessoryId,
                        flipX = entry.flipX
                    };
                }
            var result = new List<AccessoryFlipEntry>(values.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.accessoryId, right.accessoryId));
            return result;
        }

        private static List<DistanceEntry> MergeDistances(List<DistanceEntry> first,
            List<DistanceEntry> second)
        {
            var values = new Dictionary<string, DistanceEntry>(StringComparer.Ordinal);
            AddDistances(values, first);
            AddDistances(values, second);
            var result = new List<DistanceEntry>(values.Values);
            result.Sort((left, right) =>
            {
                int mode = left.mode.CompareTo(right.mode);
                return mode != 0 ? mode : string.CompareOrdinal(left.mapId, right.mapId);
            });
            return result;
        }

        private static void AddDistances(Dictionary<string, DistanceEntry> target,
            List<DistanceEntry> source)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                DistanceEntry entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.mapId) || entry.meters <= 0)
                    continue;
                string key = entry.mode.ToString(CultureInfo.InvariantCulture) + "|" + entry.mapId;
                if (!target.TryGetValue(key, out DistanceEntry existing) ||
                    entry.meters > existing.meters)
                    target[key] = new DistanceEntry
                    {
                        mode = entry.mode,
                        mapId = entry.mapId,
                        meters = entry.meters
                    };
            }
        }

        private static List<string> MergeStrings(List<string> first, List<string> second)
        {
            var values = new HashSet<string>(StringComparer.Ordinal);
            AddStrings(values, first);
            AddStrings(values, second);
            var result = new List<string>(values);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static void AddStrings(HashSet<string> target, List<string> source)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
                if (!string.IsNullOrWhiteSpace(source[i])) target.Add(source[i]);
        }

        private static List<CustomizationEntry> MergeCustomizations(
            List<CustomizationEntry> first, List<CustomizationEntry> second)
        {
            var values = new Dictionary<string, CustomizationEntry>(StringComparer.Ordinal);
            AddCustomizations(values, first);
            AddCustomizations(values, second);
            var result = new List<CustomizationEntry>(values.Values);
            result.Sort((left, right) => string.CompareOrdinal(CustomizationKey(left),
                CustomizationKey(right)));
            return result;
        }

        private static void AddCustomizations(Dictionary<string, CustomizationEntry> target,
            List<CustomizationEntry> source)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                CustomizationEntry entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.scopeId) ||
                    string.IsNullOrWhiteSpace(entry.category) ||
                    string.IsNullOrWhiteSpace(entry.optionId)) continue;
                target[CustomizationKey(entry)] = new CustomizationEntry
                {
                    scopeId = entry.scopeId,
                    category = entry.category,
                    optionId = entry.optionId
                };
            }
        }

        private static string CustomizationKey(CustomizationEntry entry) =>
            entry.scopeId + "|" + entry.category + "|" + entry.optionId;

        private static bool IsNewer(CloudSnapshot first, CloudSnapshot second)
        {
            if (first.updatedUnixSeconds != second.updatedUnixSeconds)
                return first.updatedUnixSeconds > second.updatedUnixSeconds;
            return first.revision >= second.revision;
        }

        private static void Normalize(CloudSnapshot snapshot)
        {
            if (snapshot == null) return;
            snapshot.coins = Mathf.Max(0, snapshot.coins);
            snapshot.gems = Mathf.Max(0, snapshot.gems);
            snapshot.revision = Mathf.Max(0, snapshot.revision);
            snapshot.updatedUnixSeconds = Math.Max(0L, snapshot.updatedUnixSeconds);
            snapshot.selectedMapId = snapshot.selectedMapId ?? string.Empty;
            snapshot.selectedCircuitId = snapshot.selectedCircuitId ?? string.Empty;
            snapshot.selectedVehicleId = snapshot.selectedVehicleId ?? string.Empty;
            snapshot.selectedDriverId = snapshot.selectedDriverId ?? string.Empty;
            snapshot.selectedAccessoryId = snapshot.selectedAccessoryId ?? string.Empty;
            snapshot.selectedMenuBackgroundId = snapshot.selectedMenuBackgroundId ?? string.Empty;
            snapshot.selectedGarageBackgroundId = snapshot.selectedGarageBackgroundId ?? string.Empty;
            snapshot.chestSaveJson = snapshot.chestSaveJson ?? string.Empty;
            snapshot.bestDistances = MergeDistances(snapshot.bestDistances, null);
            snapshot.unlockedMaps = MergeStrings(snapshot.unlockedMaps, null);
            snapshot.unlockedVehicles = MergeStrings(snapshot.unlockedVehicles, null);
            snapshot.unlockedCustomizations = MergeCustomizations(
                snapshot.unlockedCustomizations, null);
            snapshot.timeTrials = MergeTimeTrials(snapshot.timeTrials, null);
            snapshot.vehicleTunes = MergeVehicleTunes(snapshot.vehicleTunes, null,
                snapshot.vehicleTunes);
            snapshot.blueprints = MergeBlueprints(snapshot.blueprints, null);
            snapshot.equippedCustomizations = CloneEquippedCustomizations(
                snapshot.equippedCustomizations);
            snapshot.accessoryFlips = CloneAccessoryFlips(snapshot.accessoryFlips);
        }

        private static bool HasMeaningfulProgress(CloudSnapshot snapshot) =>
            snapshot.coins > 0 || snapshot.gems > 0 || snapshot.bestDistances.Count > 0 ||
            snapshot.unlockedMaps.Count > 0 || snapshot.unlockedVehicles.Count > 0 ||
            snapshot.unlockedCustomizations.Count > 0 || snapshot.timeTrials.Count > 0 ||
            snapshot.blueprints.Count > 0 || snapshot.hasChestProgress ||
            HasPurchasedTune(snapshot.vehicleTunes) || HasFlippedAccessory(snapshot.accessoryFlips);

        private static bool HasPurchasedTune(List<VehicleTuneEntry> tunes)
        {
            for (int i = 0; i < tunes.Count; i++)
                if (tunes[i].purchasedLevel > 1) return true;
            return false;
        }

        private static bool HasFlippedAccessory(List<AccessoryFlipEntry> flips)
        {
            for (int i = 0; i < flips.Count; i++)
                if (flips[i].flipX) return true;
            return false;
        }

        private static string ContentSignature(CloudSnapshot snapshot)
        {
            Normalize(snapshot);
            var content = new StringBuilder(256);
            content.Append(snapshot.coins).Append('|').Append(snapshot.gems);
            content.Append("|S:").Append(snapshot.selectedMode).Append(':')
                .Append(snapshot.selectedMapId).Append(':').Append(snapshot.selectedCircuitId)
                .Append(':').Append(snapshot.selectedVehicleId).Append(':')
                .Append(snapshot.selectedDriverId).Append(':').Append(snapshot.selectedAccessoryId)
                .Append(':').Append(snapshot.selectedMenuBackgroundId).Append(':')
                .Append(snapshot.selectedGarageBackgroundId);
            for (int i = 0; i < snapshot.bestDistances.Count; i++)
            {
                DistanceEntry entry = snapshot.bestDistances[i];
                content.Append("|D:").Append(entry.mode).Append(':').Append(entry.mapId)
                    .Append(':').Append(entry.meters);
            }
            for (int i = 0; i < snapshot.unlockedMaps.Count; i++)
                content.Append("|M:").Append(snapshot.unlockedMaps[i]);
            for (int i = 0; i < snapshot.unlockedVehicles.Count; i++)
                content.Append("|V:").Append(snapshot.unlockedVehicles[i]);
            for (int i = 0; i < snapshot.unlockedCustomizations.Count; i++)
                content.Append("|C:").Append(CustomizationKey(snapshot.unlockedCustomizations[i]));
            for (int i = 0; i < snapshot.timeTrials.Count; i++)
            {
                TimeTrialEntry entry = snapshot.timeTrials[i];
                content.Append("|T:").Append(entry.circuitId).Append(':')
                    .Append(entry.bestTime.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                    .Append(entry.bestLap.ToString("R", CultureInfo.InvariantCulture));
            }
            for (int i = 0; i < snapshot.vehicleTunes.Count; i++)
            {
                VehicleTuneEntry entry = snapshot.vehicleTunes[i];
                content.Append("|U:").Append(TuneKey(entry)).Append(':')
                    .Append(entry.purchasedLevel).Append(':').Append(entry.equippedLevel);
            }
            for (int i = 0; i < snapshot.blueprints.Count; i++)
                content.Append("|B:").Append(snapshot.blueprints[i].bundleId).Append(':')
                    .Append(snapshot.blueprints[i].count);
            for (int i = 0; i < snapshot.equippedCustomizations.Count; i++)
            {
                EquippedCustomizationEntry entry = snapshot.equippedCustomizations[i];
                content.Append("|E:").Append(entry.vehicleId).Append(':').Append(entry.skinId)
                    .Append(':').Append(entry.wheelId);
            }
            for (int i = 0; i < snapshot.accessoryFlips.Count; i++)
                content.Append("|F:").Append(snapshot.accessoryFlips[i].accessoryId).Append(':')
                    .Append(snapshot.accessoryFlips[i].flipX ? '1' : '0');
            content.Append("|H:").Append(snapshot.hasChestProgress ? '1' : '0').Append(':')
                .Append(snapshot.chestSaveJson);

            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < content.Length; i++)
            {
                hash ^= content[i];
                hash *= 1099511628211UL;
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        private static byte[] Serialize(CloudSnapshot snapshot)
        {
            Normalize(snapshot);
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(snapshot));
        }

        private static ParseStatus TryDeserialize(byte[] data, out CloudSnapshot snapshot)
        {
            snapshot = NewSnapshot();
            if (data == null || data.Length == 0) return ParseStatus.Empty;
            try
            {
                CloudSnapshot parsed = JsonUtility.FromJson<CloudSnapshot>(Encoding.UTF8.GetString(data));
                if (parsed == null) return ParseStatus.Invalid;
                if (parsed.saveVersion > CurrentSaveVersion) return ParseStatus.FutureVersion;
                if (parsed.saveVersion < 1) return ParseStatus.Invalid;
                snapshot = parsed;
                Normalize(snapshot);
                return ParseStatus.Valid;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Cloud Save] Could not parse snapshot: " + exception.GetType().Name);
                return ParseStatus.Invalid;
            }
        }

        private static bool IsReadable(ParseStatus status) =>
            status == ParseStatus.Empty || status == ParseStatus.Valid;

        private static CloudSnapshot NewSnapshot() => new CloudSnapshot
        {
            saveVersion = CurrentSaveVersion,
            bestDistances = new List<DistanceEntry>(),
            unlockedMaps = new List<string>(),
            unlockedVehicles = new List<string>(),
            unlockedCustomizations = new List<CustomizationEntry>(),
            timeTrials = new List<TimeTrialEntry>(),
            vehicleTunes = new List<VehicleTuneEntry>(),
            blueprints = new List<BlueprintEntry>(),
            equippedCustomizations = new List<EquippedCustomizationEntry>(),
            accessoryFlips = new List<AccessoryFlipEntry>()
        };

        private static int NextRevision(int current) => current >= int.MaxValue ? int.MaxValue : current + 1;

        private static long ToMeters(float distance)
        {
            if (distance <= 0f || float.IsNaN(distance)) return 0L;
            const double safeLongMaximum = 9_000_000_000_000_000_000d;
            return (long)Math.Round(Math.Min((double)distance, safeLongMaximum),
                MidpointRounding.AwayFromZero);
        }

        private static float SafePositive(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;

        private static float LowerPositive(float first, float second)
        {
            first = SafePositive(first);
            second = SafePositive(second);
            if (first <= 0f) return second;
            if (second <= 0f) return first;
            return Mathf.Min(first, second);
        }

        private static bool IsValidFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename) || filename.Length > 100) return false;
            for (int i = 0; i < filename.Length; i++)
            {
                char value = filename[i];
                if (!(value >= 'a' && value <= 'z') && !(value >= 'A' && value <= 'Z') &&
                    !(value >= '0' && value <= '9') && value != '-' && value != '.' &&
                    value != '_' && value != '~') return false;
            }
            return true;
        }

        private void StoreLocalMetadata(CloudSnapshot snapshot, string signature)
        {
            PlayerPrefs.SetInt(RevisionKey, snapshot.revision);
            PlayerPrefs.SetString(TimestampKey,
                snapshot.updatedUnixSeconds.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(SignatureKey, signature);
            PlayerPrefs.Save();
        }

        private bool IsCurrentRequest(int requestId) =>
            requestInFlight && activeRequestSerial == requestId;

        private bool CompleteRequest(int requestId)
        {
            if (!IsCurrentRequest(requestId)) return false;
            requestInFlight = false;
            activeRequestSerial = 0;
            return true;
        }

        private void FinishWithWarning(int requestId, string reason)
        {
            if (!CompleteRequest(requestId)) return;
            SetState(CloudSaveState.Failed, reason.ToUpperInvariant());
            Debug.LogWarning("[Cloud Save] " + reason);
        }

        private static void MarkInitialized()
        {
            PlayerPrefs.SetInt(InitializedKey, 1);
            PlayerPrefs.Save();
            SaveManager.ConfirmPaidCurrencyCloudSync();
        }

        private void SetState(CloudSaveState next, string error)
        {
            string normalizedError = error ?? string.Empty;
            bool changed = state != next || lastError != normalizedError;
            state = next;
            lastError = normalizedError;
            if (changed) StateChanged?.Invoke();
        }

        private enum ParseStatus
        {
            Empty,
            Valid,
            FutureVersion,
            Invalid
        }

        [Serializable]
        private sealed class CloudSnapshot
        {
            public int saveVersion;
            public int revision;
            public long updatedUnixSeconds;
            public int coins;
            public int gems;
            public int selectedMode;
            public string selectedMapId;
            public string selectedCircuitId;
            public string selectedVehicleId;
            public string selectedDriverId;
            public string selectedAccessoryId;
            public string selectedMenuBackgroundId;
            public string selectedGarageBackgroundId;
            public string chestSaveJson;
            public bool hasChestProgress;
            public List<DistanceEntry> bestDistances;
            public List<string> unlockedMaps;
            public List<string> unlockedVehicles;
            public List<CustomizationEntry> unlockedCustomizations;
            public List<TimeTrialEntry> timeTrials;
            public List<VehicleTuneEntry> vehicleTunes;
            public List<BlueprintEntry> blueprints;
            public List<EquippedCustomizationEntry> equippedCustomizations;
            public List<AccessoryFlipEntry> accessoryFlips;
        }

        [Serializable]
        private sealed class DistanceEntry
        {
            public int mode;
            public string mapId;
            public long meters;
        }

        [Serializable]
        private sealed class CustomizationEntry
        {
            public string scopeId;
            public string category;
            public string optionId;
        }

        [Serializable]
        private sealed class TimeTrialEntry
        {
            public string circuitId;
            public float bestTime;
            public float bestLap;
        }

        [Serializable]
        private sealed class VehicleTuneEntry
        {
            public string vehicleId;
            public int type;
            public int purchasedLevel;
            public int equippedLevel;
        }

        [Serializable]
        private sealed class BlueprintEntry
        {
            public string bundleId;
            public int count;
        }

        [Serializable]
        private sealed class EquippedCustomizationEntry
        {
            public string vehicleId;
            public string skinId;
            public string wheelId;
        }

        [Serializable]
        private sealed class AccessoryFlipEntry
        {
            public string accessoryId;
            public bool flipX;
        }
    }
}
