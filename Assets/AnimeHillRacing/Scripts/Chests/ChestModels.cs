using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Chests
{
    public enum ChestState
    {
        Pending,
        Opening,
        ReadyToClaim,
        Claimed
    }

    [Serializable]
    public sealed class ChestRecord
    {
        public string chestId;
        public ChestType type;
        public ChestState state;
        public string createdUtc;
        public string openStartedUtc;
        public string openReadyUtc;
        public int coinReward;
        public int gemReward;
        public string blueprintBundleId;
        public int blueprintReward;
        public bool rewardClaimed;
        public bool seen;

        public DateTime ReadyUtc
        {
            get
            {
                return DateTime.TryParse(openReadyUtc, null, System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime value) ? value.ToUniversalTime() : DateTime.MaxValue;
            }
        }

        public bool RefreshState(DateTime utcNow)
        {
            if (state != ChestState.Opening || utcNow.ToUniversalTime() < ReadyUtc) return false;
            state = ChestState.ReadyToClaim;
            return true;
        }
    }

    [Serializable]
    public sealed class ChestSaveData
    {
        public int version = 1;
        public float chestGameplayProgress;
        public List<ChestRecord> inventory = new List<ChestRecord>();
    }

    public sealed class ChestClaimResult
    {
        public bool Success { get; internal set; }
        public ChestType Type { get; internal set; }
        public int Coins { get; internal set; }
        public int Gems { get; internal set; }
        public string BundleId { get; internal set; }
        public string BundleName { get; internal set; }
        public int Blueprints { get; internal set; }
        public int BlueprintTotal { get; internal set; }
        public int BlueprintRequirement { get; internal set; }
        public bool BundleUnlocked { get; internal set; }
    }

    public interface IChestRandomSource
    {
        int Range(int minimumInclusive, int maximumExclusive);
    }

    public sealed class UnityChestRandomSource : IChestRandomSource
    {
        public int Range(int minimumInclusive, int maximumExclusive) =>
            UnityEngine.Random.Range(minimumInclusive, maximumExclusive);
    }

    public sealed class SeededChestRandomSource : IChestRandomSource
    {
        private readonly Random random;
        public SeededChestRandomSource(int seed) => random = new Random(seed);
        public int Range(int minimumInclusive, int maximumExclusive) =>
            random.Next(minimumInclusive, maximumExclusive);
    }

    public static class ChestRaritySelector
    {
        public static ChestType Roll(ChestCatalog catalog, IChestRandomSource random)
        {
            if (catalog == null || random == null || catalog.TotalRarityWeight <= 0)
                throw new InvalidOperationException("Chest catalog has no positive rarity weights.");
            int roll = random.Range(0, catalog.TotalRarityWeight);
            int cursor = 0;
            for (int i = 0; i < catalog.chests.Length; i++)
            {
                ChestDefinition definition = catalog.chests[i];
                if (definition == null) continue;
                cursor += Math.Max(0, definition.rarityWeight);
                if (roll < cursor) return definition.type;
            }
            return catalog.chests[catalog.chests.Length - 1].type;
        }
    }

    public static class ChestProgressMath
    {
        public static int Advance(ref float accumulatedSeconds, float addedSeconds, float intervalSeconds)
        {
            if (addedSeconds <= 0f || intervalSeconds <= 0f) return 0;
            accumulatedSeconds = Math.Max(0f, accumulatedSeconds) + addedSeconds;
            int awards = 0;
            while (accumulatedSeconds >= intervalSeconds)
            {
                accumulatedSeconds -= intervalSeconds;
                awards++;
            }
            return awards;
        }
    }
}
