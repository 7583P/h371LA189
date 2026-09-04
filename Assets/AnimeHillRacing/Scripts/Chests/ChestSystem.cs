using System;
using System.Collections.Generic;
using System.Globalization;
using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.Chests
{
    public static class ChestSystem
    {
        private const string SaveKey = "AHR.Chests.Save.V1";
        private static readonly IChestRandomSource Random = new UnityChestRandomSource();
        private static ChestSaveData data;
        private static float lastFlushRealtime;
        private static bool transactionInFlight;

        public static float GameplayProgress
        {
            get
            {
                EnsureLoaded();
                return Mathf.Clamp(data.chestGameplayProgress, 0f, ChestCatalog.GameplayIntervalSeconds);
            }
        }

        public static IReadOnlyList<ChestRecord> Inventory
        {
            get
            {
                EnsureLoaded();
                return data.inventory;
            }
        }

        public static List<ChestRecord> AdvanceActiveGameplay(float realSeconds, GameCatalog catalog)
        {
            EnsureLoaded();
            var granted = new List<ChestRecord>();
            if (catalog?.chestCatalog == null || realSeconds <= 0f) return granted;
            if (data.inventory.Count >= ChestCatalog.MaximumInventorySize)
            {
                if (data.chestGameplayProgress > 0f)
                {
                    data.chestGameplayProgress = 0f;
                    Flush();
                }
                return granted;
            }

            int availableSlots = ChestCatalog.MaximumInventorySize - data.inventory.Count;
            int awards = ChestProgressMath.Advance(ref data.chestGameplayProgress, realSeconds,
                ChestCatalog.GameplayIntervalSeconds);
            int awardsToGrant = Mathf.Min(awards, availableSlots);
            for (int i = 0; i < awardsToGrant; i++)
            {
                ChestRecord chest = CreateChest(catalog, ChestRaritySelector.Roll(catalog.chestCatalog, Random), Random);
                data.inventory.Add(chest);
                granted.Add(chest);
            }

            // Active play accumulated after storage becomes full must not be banked. Once a
            // chest is claimed, the next five-minute cycle starts from zero again.
            if (awards >= availableSlots && data.inventory.Count >= ChestCatalog.MaximumInventorySize)
                data.chestGameplayProgress = 0f;
            if (granted.Count > 0 || Time.realtimeSinceStartup - lastFlushRealtime >= 1f)
                Flush();
            return granted;
        }

        public static ChestRecord Grant(GameCatalog catalog, ChestType type)
        {
            EnsureLoaded();
            if (data.inventory.Count >= ChestCatalog.MaximumInventorySize ||
                catalog?.chestCatalog?.Find(type) == null) return null;
            ChestRecord chest = CreateChest(catalog, type, Random);
            data.inventory.Add(chest);
            if (data.inventory.Count >= ChestCatalog.MaximumInventorySize)
                data.chestGameplayProgress = 0f;
            Flush();
            return chest;
        }

        public static bool StartOpening(string chestId, GameCatalog catalog, DateTime utcNow)
        {
            EnsureLoaded();
            RefreshOpeningStates(utcNow);
            ChestRecord target = Find(chestId);
            if (target == null || target.state != ChestState.Pending || catalog?.chestCatalog == null) return false;
            for (int i = 0; i < data.inventory.Count; i++)
                if (data.inventory[i].state == ChestState.Opening) return false;
            ChestDefinition definition = catalog.chestCatalog.Find(target.type);
            if (definition == null) return false;
            DateTime start = utcNow.ToUniversalTime();
            target.openStartedUtc = start.ToString("O");
            target.openReadyUtc = start.Add(definition.OpeningDuration).ToString("O");
            target.rewardedAdViews = 0;
            target.state = ChestState.Opening;
            Flush();
            return true;
        }

        public static bool RefreshOpeningStates(DateTime utcNow)
        {
            EnsureLoaded();
            bool changed = false;
            for (int i = 0; i < data.inventory.Count; i++)
                changed |= data.inventory[i].RefreshState(utcNow);
            if (changed) Flush();
            return changed;
        }

        public static ChestRecord GetOpeningChest(DateTime utcNow)
        {
            EnsureLoaded();
            RefreshOpeningStates(utcNow);
            for (int i = 0; i < data.inventory.Count; i++)
                if (data.inventory[i].state == ChestState.Opening) return data.inventory[i];
            return null;
        }

        public static int GetRequiredAdViews(ChestType type)
        {
            switch (type)
            {
                case ChestType.Master: return 2;
                case ChestType.Legendary: return 3;
                default: return 1;
            }
        }

        public static RewardedAdQuota GetCoinAdQuota(DateTime utcNow) =>
            GetAdQuota(true, utcNow);

        public static RewardedAdQuota GetChestAdQuota(DateTime utcNow) =>
            GetAdQuota(false, utcNow);

        public static bool TryClaimCoinAdReward(DateTime utcNow, out int coinReward,
            out RewardedAdQuota quota)
        {
            EnsureLoaded();
            DateTime now = utcNow.ToUniversalTime();
            RefreshAdWindow(ref data.coinAdWindowStartUtc, ref data.coinAdsViewed,
                ChestCatalog.CoinAdLimit, TimeSpan.FromHours(ChestCatalog.CoinAdWindowHours), now);
            if (data.coinAdsViewed >= ChestCatalog.CoinAdLimit)
            {
                coinReward = 0;
                quota = CreateQuota(data.coinAdWindowStartUtc, data.coinAdsViewed,
                    ChestCatalog.CoinAdLimit, TimeSpan.FromHours(ChestCatalog.CoinAdWindowHours), now);
                return false;
            }

            StartAdWindow(ref data.coinAdWindowStartUtc, now);
            data.coinAdsViewed++;
            coinReward = Random.Range(ChestCatalog.CoinAdMinimumReward,
                ChestCatalog.CoinAdMaximumReward + 1);
            SaveManager.AddCoins(coinReward, false);
            quota = CreateQuota(data.coinAdWindowStartUtc, data.coinAdsViewed,
                ChestCatalog.CoinAdLimit, TimeSpan.FromHours(ChestCatalog.CoinAdWindowHours), now);
            Flush();
            return true;
        }

        public static bool TryApplyAdToOpeningChest(DateTime utcNow, out ChestAdProgressResult result)
        {
            EnsureLoaded();
            DateTime now = utcNow.ToUniversalTime();
            RefreshOpeningStates(now);
            RewardedAdQuota quota = GetChestAdQuota(now);
            result = new ChestAdProgressResult { Quota = quota };
            ChestRecord target = GetOpeningChest(now);
            if (target == null || !quota.IsAvailable) return false;

            int required = GetRequiredAdViews(target.type);
            target.rewardedAdViews = Mathf.Clamp(target.rewardedAdViews, 0, required);
            result.Type = target.type;
            result.RequiredViews = required;
            if (target.rewardedAdViews >= required)
            {
                target.state = ChestState.ReadyToClaim;
                target.openReadyUtc = now.ToString("O");
                result.Success = true;
                result.CompletedViews = required;
                result.ReadyToClaim = true;
                Flush();
                return true;
            }

            StartAdWindow(ref data.chestAdWindowStartUtc, now);
            data.chestAdsViewed++;
            target.rewardedAdViews++;
            if (target.rewardedAdViews >= required)
            {
                target.state = ChestState.ReadyToClaim;
                target.openReadyUtc = now.ToString("O");
            }

            result.Success = true;
            result.CompletedViews = target.rewardedAdViews;
            result.ReadyToClaim = target.state == ChestState.ReadyToClaim;
            result.Quota = CreateQuota(data.chestAdWindowStartUtc, data.chestAdsViewed,
                ChestCatalog.ChestAdLimit, TimeSpan.FromHours(ChestCatalog.ChestAdWindowHours), now);
            Flush();
            return true;
        }

        public static bool Claim(string chestId, GameCatalog catalog, DateTime utcNow,
            out ChestClaimResult result)
        {
            result = new ChestClaimResult();
            if (transactionInFlight) return false;
            transactionInFlight = true;
            try
            {
                EnsureLoaded();
                RefreshOpeningStates(utcNow);
                ChestRecord target = Find(chestId);
                if (target == null || target.state != ChestState.ReadyToClaim || target.rewardClaimed)
                    return false;

                // Chests saved by versions before Gem rewards existed receive a valid tier reward
                // when claimed instead of remaining at the legacy serialized value of zero.
                ChestDefinition definition = catalog?.chestCatalog?.Find(target.type);
                if (definition != null)
                {
                    int minimumGems = Mathf.Max(0, definition.minimumGems);
                    int maximumGems = Mathf.Max(minimumGems, definition.maximumGems);
                    if (target.gemReward < minimumGems || target.gemReward > maximumGems)
                        target.gemReward = maximumGems > minimumGems
                            ? Random.Range(minimumGems, maximumGems + 1) : minimumGems;
                }

                BundleDefinition bundle = ResolveClaimBundle(catalog, target);
                target.rewardClaimed = true;
                target.state = ChestState.Claimed;
                SaveManager.AddCoins(target.coinReward, false);
                SaveManager.AddGems(target.gemReward, false);
                int total = 0;
                bool unlocked = false;
                int blueprints = bundle != null ? target.blueprintReward : 0;
                if (bundle != null && blueprints > 0)
                    total = SaveManager.AddBlueprints(bundle, blueprints, out unlocked, false);

                result.Success = true;
                result.Type = target.type;
                result.Coins = target.coinReward;
                result.Gems = target.gemReward;
                result.BundleId = bundle?.BundleId;
                result.BundleName = bundle?.DisplayName;
                result.Blueprints = blueprints;
                result.BlueprintTotal = total;
                result.BlueprintRequirement = bundle?.BlueprintRequirement ?? 0;
                result.BundleUnlocked = unlocked;

                bool inventoryWasFull = data.inventory.Count >= ChestCatalog.MaximumInventorySize;
                data.inventory.Remove(target);
                if (inventoryWasFull) data.chestGameplayProgress = 0f;
                SaveWithoutFlush();
                PlayerPrefs.Save();
                lastFlushRealtime = Time.realtimeSinceStartup;
                return true;
            }
            finally
            {
                transactionInFlight = false;
            }
        }

        public static void MarkSeen(string chestId)
        {
            ChestRecord chest = Find(chestId);
            if (chest == null || chest.seen) return;
            chest.seen = true;
            Flush();
        }

        public static void Flush()
        {
            EnsureLoaded();
            SaveWithoutFlush();
            PlayerPrefs.Save();
            lastFlushRealtime = Time.realtimeSinceStartup;
        }

        internal static string ExportCloudJson()
        {
            EnsureLoaded();
            return JsonUtility.ToJson(data);
        }

        internal static bool TryImportCloudJson(string json, GameCatalog catalog)
        {
            if (string.IsNullOrWhiteSpace(json)) return true;
            try
            {
                ChestSaveData imported = JsonUtility.FromJson<ChestSaveData>(json);
                if (imported == null || imported.version != 1) return false;
                if (imported.inventory == null) imported.inventory = new List<ChestRecord>();
                if (imported.inventory.Count > 100) return false;

                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = imported.inventory.Count - 1; i >= 0; i--)
                {
                    ChestRecord chest = imported.inventory[i];
                    ChestDefinition definition = chest != null && catalog?.chestCatalog != null
                        ? catalog.chestCatalog.Find(chest.type) : null;
                    if (chest == null || definition == null || chest.rewardClaimed ||
                        chest.state == ChestState.Claimed || string.IsNullOrWhiteSpace(chest.chestId) ||
                        !ids.Add(chest.chestId))
                    {
                        imported.inventory.RemoveAt(i);
                        continue;
                    }

                    int minimumCoins = Mathf.Max(0, definition.minimumCoins);
                    int maximumCoins = Mathf.Max(minimumCoins, definition.maximumCoins);
                    int minimumGems = Mathf.Max(0, definition.minimumGems);
                    int maximumGems = Mathf.Max(minimumGems, definition.maximumGems);
                    chest.coinReward = Mathf.Clamp(chest.coinReward, minimumCoins, maximumCoins);
                    chest.gemReward = Mathf.Clamp(chest.gemReward, minimumGems, maximumGems);
                    chest.blueprintReward = Mathf.Clamp(chest.blueprintReward, 0,
                        Mathf.Max(0, definition.blueprintReward));
                    chest.rewardedAdViews = Mathf.Clamp(chest.rewardedAdViews, 0,
                        GetRequiredAdViews(chest.type));
                    if (chest.blueprintReward == 0 ||
                        PremiumBundleCatalog.Find(catalog, chest.blueprintBundleId) == null)
                    {
                        chest.blueprintReward = 0;
                        chest.blueprintBundleId = string.Empty;
                    }
                }

                imported.chestGameplayProgress = Mathf.Repeat(
                    Mathf.Max(0f, imported.chestGameplayProgress), ChestCatalog.GameplayIntervalSeconds);
                data = imported;
                NormalizeAdData(DateTime.UtcNow);
                RefreshOpeningStatesWithoutSaving(DateTime.UtcNow);
                SaveWithoutFlush();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Cloud Save] Invalid chest data ignored: " +
                                 exception.GetType().Name);
                return false;
            }
        }

        public static string FormatRemaining(DateTime readyUtc, DateTime utcNow)
        {
            TimeSpan remaining = readyUtc.ToUniversalTime() - utcNow.ToUniversalTime();
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static List<ChestRecord> DebugAddGameplay(GameCatalog catalog, float seconds) =>
            AdvanceActiveGameplay(seconds, catalog);

        public static bool DebugCompleteOpening()
        {
            EnsureLoaded();
            for (int i = 0; i < data.inventory.Count; i++)
            {
                if (data.inventory[i].state != ChestState.Opening) continue;
                data.inventory[i].openReadyUtc = DateTime.UtcNow.AddSeconds(-1).ToString("O");
                data.inventory[i].state = ChestState.ReadyToClaim;
                Flush();
                return true;
            }
            return false;
        }
#endif

        private static ChestRecord CreateChest(GameCatalog catalog, ChestType type, IChestRandomSource random)
        {
            ChestDefinition definition = catalog.chestCatalog.Find(type);
            int coinReward = random.Range(definition.minimumCoins, definition.maximumCoins + 1);
            int minimumGems = Mathf.Max(0, definition.minimumGems);
            int maximumGems = Mathf.Max(minimumGems, definition.maximumGems);
            int gemReward = maximumGems > minimumGems
                ? random.Range(minimumGems, maximumGems + 1) : minimumGems;
            string bundleId = string.Empty;
            if (definition.blueprintReward > 0)
            {
                List<BundleDefinition> eligible = PremiumBundleCatalog.GetEligible(catalog);
                if (eligible.Count > 0) bundleId = eligible[random.Range(0, eligible.Count)].BundleId;
            }
            return new ChestRecord
            {
                chestId = Guid.NewGuid().ToString("N"),
                type = type,
                state = ChestState.Pending,
                createdUtc = DateTime.UtcNow.ToString("O"),
                coinReward = coinReward,
                gemReward = gemReward,
                blueprintBundleId = bundleId,
                blueprintReward = string.IsNullOrEmpty(bundleId) ? 0 : definition.blueprintReward,
                rewardedAdViews = 0,
                rewardClaimed = false,
                seen = false
            };
        }

        private static BundleDefinition ResolveClaimBundle(GameCatalog catalog, ChestRecord target)
        {
            if (target.blueprintReward <= 0 || catalog == null) return null;
            BundleDefinition bundle = PremiumBundleCatalog.Find(catalog, target.blueprintBundleId);
            if (bundle != null && !SaveManager.IsBundleUnlocked(bundle)) return bundle;
            List<BundleDefinition> eligible = PremiumBundleCatalog.GetEligible(catalog);
            if (eligible.Count == 0)
            {
                target.blueprintBundleId = string.Empty;
                target.blueprintReward = 0;
                return null;
            }
            bundle = eligible[Random.Range(0, eligible.Count)];
            target.blueprintBundleId = bundle.BundleId;
            return bundle;
        }

        private static ChestRecord Find(string chestId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(chestId)) return null;
            for (int i = 0; i < data.inventory.Count; i++)
                if (data.inventory[i].chestId == chestId) return data.inventory[i];
            return null;
        }

        private static void EnsureLoaded()
        {
            if (data != null) return;
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            data = string.IsNullOrEmpty(json) ? new ChestSaveData() : JsonUtility.FromJson<ChestSaveData>(json);
            if (data == null) data = new ChestSaveData();
            if (data.inventory == null) data.inventory = new List<ChestRecord>();
            data.chestGameplayProgress = Mathf.Repeat(Mathf.Max(0f, data.chestGameplayProgress),
                ChestCatalog.GameplayIntervalSeconds);
            data.inventory.RemoveAll(item => item == null || item.rewardClaimed || item.state == ChestState.Claimed);
            for (int i = 0; i < data.inventory.Count; i++)
                data.inventory[i].rewardedAdViews = Mathf.Clamp(data.inventory[i].rewardedAdViews, 0,
                    GetRequiredAdViews(data.inventory[i].type));
            NormalizeAdData(DateTime.UtcNow);
            RefreshOpeningStatesWithoutSaving(DateTime.UtcNow);
        }

        private static RewardedAdQuota GetAdQuota(bool coins, DateTime utcNow)
        {
            EnsureLoaded();
            DateTime now = utcNow.ToUniversalTime();
            bool changed;
            RewardedAdQuota quota;
            if (coins)
            {
                changed = RefreshAdWindow(ref data.coinAdWindowStartUtc, ref data.coinAdsViewed,
                    ChestCatalog.CoinAdLimit, TimeSpan.FromHours(ChestCatalog.CoinAdWindowHours), now);
                quota = CreateQuota(data.coinAdWindowStartUtc, data.coinAdsViewed,
                    ChestCatalog.CoinAdLimit, TimeSpan.FromHours(ChestCatalog.CoinAdWindowHours), now);
            }
            else
            {
                changed = RefreshAdWindow(ref data.chestAdWindowStartUtc, ref data.chestAdsViewed,
                    ChestCatalog.ChestAdLimit, TimeSpan.FromHours(ChestCatalog.ChestAdWindowHours), now);
                quota = CreateQuota(data.chestAdWindowStartUtc, data.chestAdsViewed,
                    ChestCatalog.ChestAdLimit, TimeSpan.FromHours(ChestCatalog.ChestAdWindowHours), now);
            }
            if (changed) Flush();
            return quota;
        }

        private static void NormalizeAdData(DateTime utcNow)
        {
            DateTime now = utcNow.ToUniversalTime();
            RefreshAdWindow(ref data.coinAdWindowStartUtc, ref data.coinAdsViewed,
                ChestCatalog.CoinAdLimit, TimeSpan.FromHours(ChestCatalog.CoinAdWindowHours), now);
            RefreshAdWindow(ref data.chestAdWindowStartUtc, ref data.chestAdsViewed,
                ChestCatalog.ChestAdLimit, TimeSpan.FromHours(ChestCatalog.ChestAdWindowHours), now);
        }

        private static bool RefreshAdWindow(ref string startText, ref int used, int limit,
            TimeSpan duration, DateTime utcNow)
        {
            bool changed = false;
            int clamped = Mathf.Clamp(used, 0, limit);
            if (clamped != used)
            {
                used = clamped;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(startText))
            {
                if (used == 0) return changed;
                used = 0;
                return true;
            }

            if (!TryParseUtc(startText, out DateTime start) || utcNow < start || utcNow >= start.Add(duration))
            {
                startText = string.Empty;
                used = 0;
                return true;
            }
            return changed;
        }

        private static void StartAdWindow(ref string startText, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(startText)) startText = utcNow.ToUniversalTime().ToString("O");
        }

        private static RewardedAdQuota CreateQuota(string startText, int used, int limit,
            TimeSpan duration, DateTime utcNow)
        {
            DateTime reset = TryParseUtc(startText, out DateTime start)
                ? start.Add(duration) : utcNow.ToUniversalTime().Add(duration);
            return new RewardedAdQuota(used, limit, reset);
        }

        private static bool TryParseUtc(string value, out DateTime utc)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                    out DateTime parsed))
            {
                utc = parsed.ToUniversalTime();
                return true;
            }
            utc = default;
            return false;
        }

        private static void RefreshOpeningStatesWithoutSaving(DateTime utcNow)
        {
            for (int i = 0; i < data.inventory.Count; i++) data.inventory[i].RefreshState(utcNow);
        }

        private static void SaveWithoutFlush() =>
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
    }
}
