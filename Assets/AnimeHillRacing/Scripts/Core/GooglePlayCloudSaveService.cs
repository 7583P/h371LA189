using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    public sealed class GooglePlayCloudSaveService
    {
        private const int CurrentSaveVersion = 1;
        private const float SyncIntervalSeconds = 300f;
        private const string RevisionKey = "AHR.Cloud.ProgressRevision.V1";
        private const string TimestampKey = "AHR.Cloud.ProgressTimestamp.V1";
        private const string SignatureKey = "AHR.Cloud.ProgressSignature.V1";

        private readonly GameCatalog catalog;
        private readonly StarterPackConfiguration configuration;
        private readonly IAuthenticationProvider authentication;
        private bool wasAuthenticated;
        private bool requestInFlight;
        private float nextAttemptAt;

        public GooglePlayCloudSaveService(GameCatalog catalog,
            StarterPackConfiguration configuration, IAuthenticationProvider authentication)
        {
            this.catalog = catalog;
            this.configuration = configuration;
            this.authentication = authentication;
        }

        public bool IsEnabled => configuration != null && configuration.enableCloudSave &&
                                 catalog != null && IsValidFilename(configuration.cloudSaveFilename);

        public void Tick(float unscaledTime)
        {
            if (!IsEnabled || Application.platform != RuntimePlatform.Android) return;
            bool authenticated = authentication != null && authentication.IsAuthenticated;
            if (!authenticated)
            {
                wasAuthenticated = false;
                return;
            }

            if (!wasAuthenticated)
            {
                wasAuthenticated = true;
                nextAttemptAt = unscaledTime;
            }
            if (requestInFlight || unscaledTime < nextAttemptAt) return;
            nextAttemptAt = unscaledTime + SyncIntervalSeconds;
            Synchronize();
        }

        private void Synchronize()
        {
#if UNITY_ANDROID
            ISavedGameClient client = PlayGamesPlatform.Instance.SavedGame;
            if (client == null)
            {
                Debug.LogWarning("[Cloud Save] Saved Games client is unavailable");
                return;
            }

            requestInFlight = true;
            CloudSnapshot local = CaptureLocalSnapshot(true);
            client.OpenWithManualConflictResolution(configuration.cloudSaveFilename,
                DataSource.ReadCacheOrNetwork, true, ResolveConflict,
                (status, metadata) => HandleOpenCompleted(client, local, status, metadata));
#endif
        }

#if UNITY_ANDROID
        private void ResolveConflict(IConflictResolver resolver, ISavedGameMetadata original,
            byte[] originalData, ISavedGameMetadata unmerged, byte[] unmergedData)
        {
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

            CloudSnapshot merged = Merge(first, second, true);
            ISavedGameMetadata baseMetadata = IsNewer(first, second) ? original : unmerged;
            resolver.ResolveConflict(baseMetadata, BuildMetadataUpdate(), Serialize(merged));
        }

        private void HandleOpenCompleted(ISavedGameClient client, CloudSnapshot local,
            SavedGameRequestStatus status, ISavedGameMetadata metadata)
        {
            if (status != SavedGameRequestStatus.Success || metadata == null)
            {
                FinishWithWarning("open failed: " + status);
                return;
            }

            client.ReadBinaryData(metadata, (readStatus, data) =>
            {
                if (readStatus != SavedGameRequestStatus.Success)
                {
                    FinishWithWarning("read failed: " + readStatus);
                    return;
                }

                ParseStatus parseStatus = TryDeserialize(data, out CloudSnapshot remote);
                if (!IsReadable(parseStatus))
                {
                    FinishWithWarning(parseStatus == ParseStatus.FutureVersion
                        ? "newer save version preserved; update the app before syncing"
                        : "cloud data is invalid and was not overwritten");
                    return;
                }

                CloudSnapshot merged = Merge(local, remote, false);
                string remoteSignature = ContentSignature(remote);
                string mergedSignature = ContentSignature(merged);
                bool mustCommit = !string.Equals(remoteSignature, mergedSignature,
                    StringComparison.Ordinal);

                ApplyMergedSnapshot(merged);
                StoreLocalMetadata(merged, mergedSignature);
                if (!mustCommit)
                {
                    requestInFlight = false;
                    Debug.Log("[Cloud Save] Local progress is synchronized");
                    return;
                }

                client.CommitUpdate(metadata, BuildMetadataUpdate(), Serialize(merged),
                    (commitStatus, _) =>
                    {
                        requestInFlight = false;
                        if (commitStatus == SavedGameRequestStatus.Success)
                            Debug.Log("[Cloud Save] Progress synchronized");
                        else
                            Debug.LogWarning("[Cloud Save] Commit failed: " + commitStatus);
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
                }

            Normalize(snapshot);
            string signature = ContentSignature(snapshot);
            if (detectLocalChanges)
            {
                string previous = PlayerPrefs.GetString(SignatureKey, string.Empty);
                bool firstObservation = string.IsNullOrEmpty(previous);
                bool changed = !firstObservation && !string.Equals(previous, signature,
                    StringComparison.Ordinal);
                if (changed || firstObservation && HasMeaningfulProgress(snapshot))
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
                    string.IsNullOrWhiteSpace(option.optionId) ||
                    option.purchaseCurrency == CustomizationPurchaseCurrency.RealMoney ||
                    IsStarterCustomization(vehicle.vehicleId, category, option.optionId)) continue;
                if (SaveManager.IsCustomizationUnlocked(vehicle.vehicleId, category,
                        option.optionId, false))
                    snapshot.unlockedCustomizations.Add(new CustomizationEntry
                    {
                        scopeId = vehicle.vehicleId,
                        category = category,
                        optionId = option.optionId
                    });
            }
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

            SaveManager.CommitCloudMerge(previousCoins != snapshot.coins || previousGems != snapshot.gems);
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

        private bool IsSafeInternalCustomization(CustomizationEntry entry)
        {
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

        private bool IsStarterCustomization(string vehicleId, string category, string optionId) =>
            category == "Skin" && configuration != null &&
            configuration.exclusiveVehicleId == vehicleId &&
            configuration.exclusiveSkinId == optionId;

        private static CloudSnapshot Merge(CloudSnapshot first, CloudSnapshot second,
            bool forceNewRevision)
        {
            Normalize(first);
            Normalize(second);
            CloudSnapshot newer = IsNewer(first, second) ? first : second;
            var result = NewSnapshot();
            result.coins = newer.coins;
            result.gems = newer.gems;
            result.bestDistances = MergeDistances(first.bestDistances, second.bestDistances);
            result.unlockedMaps = MergeStrings(first.unlockedMaps, second.unlockedMaps);
            result.unlockedVehicles = MergeStrings(first.unlockedVehicles, second.unlockedVehicles);
            result.unlockedCustomizations = MergeCustomizations(first.unlockedCustomizations,
                second.unlockedCustomizations);

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
            snapshot.coins = Mathf.Max(0, snapshot.coins);
            snapshot.gems = Mathf.Max(0, snapshot.gems);
            snapshot.revision = Mathf.Max(0, snapshot.revision);
            snapshot.updatedUnixSeconds = Math.Max(0L, snapshot.updatedUnixSeconds);
            snapshot.bestDistances = MergeDistances(snapshot.bestDistances, null);
            snapshot.unlockedMaps = MergeStrings(snapshot.unlockedMaps, null);
            snapshot.unlockedVehicles = MergeStrings(snapshot.unlockedVehicles, null);
            snapshot.unlockedCustomizations = MergeCustomizations(
                snapshot.unlockedCustomizations, null);
        }

        private static bool HasMeaningfulProgress(CloudSnapshot snapshot) =>
            snapshot.coins > 0 || snapshot.gems > 0 || snapshot.bestDistances.Count > 0 ||
            snapshot.unlockedMaps.Count > 0 || snapshot.unlockedVehicles.Count > 0 ||
            snapshot.unlockedCustomizations.Count > 0;

        private static string ContentSignature(CloudSnapshot snapshot)
        {
            Normalize(snapshot);
            var content = new StringBuilder(256);
            content.Append(snapshot.coins).Append('|').Append(snapshot.gems);
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
            unlockedCustomizations = new List<CustomizationEntry>()
        };

        private static int NextRevision(int current) => current >= int.MaxValue ? int.MaxValue : current + 1;

        private static long ToMeters(float distance)
        {
            if (distance <= 0f || float.IsNaN(distance)) return 0L;
            const double safeLongMaximum = 9_000_000_000_000_000_000d;
            return (long)Math.Round(Math.Min((double)distance, safeLongMaximum),
                MidpointRounding.AwayFromZero);
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

        private void FinishWithWarning(string reason)
        {
            requestInFlight = false;
            Debug.LogWarning("[Cloud Save] " + reason);
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
            public List<DistanceEntry> bestDistances;
            public List<string> unlockedMaps;
            public List<string> unlockedVehicles;
            public List<CustomizationEntry> unlockedCustomizations;
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
    }
}
