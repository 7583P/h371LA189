using System;
using System.Collections.Generic;
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
            int awards = ChestProgressMath.Advance(ref data.chestGameplayProgress, realSeconds,
                ChestCatalog.GameplayIntervalSeconds);
            for (int i = 0; i < awards; i++)
            {
                ChestRecord chest = CreateChest(catalog, ChestRaritySelector.Roll(catalog.chestCatalog, Random), Random);
                data.inventory.Add(chest);
                granted.Add(chest);
            }
            if (granted.Count > 0 || Time.realtimeSinceStartup - lastFlushRealtime >= 1f)
                Flush();
            return granted;
        }

        public static ChestRecord Grant(GameCatalog catalog, ChestType type)
        {
            EnsureLoaded();
            if (catalog?.chestCatalog?.Find(type) == null) return null;
            ChestRecord chest = CreateChest(catalog, type, Random);
            data.inventory.Add(chest);
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

                data.inventory.Remove(target);
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
            RefreshOpeningStatesWithoutSaving(DateTime.UtcNow);
        }

        private static void RefreshOpeningStatesWithoutSaving(DateTime utcNow)
        {
            for (int i = 0; i < data.inventory.Count; i++) data.inventory[i].RefreshState(utcNow);
        }

        private static void SaveWithoutFlush() =>
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
    }
}
