using System;
using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.Chests
{
    public enum ChestType
    {
        Bronze,
        Silver,
        Gold,
        Master,
        Legendary
    }

    [Serializable]
    public sealed class ChestDefinition
    {
        public ChestType type;
        public string displayName;
        [Min(1)] public int rarityWeight = 1;
        [Min(1)] public int openingSeconds = 1;
        [Min(0)] public int minimumCoins;
        [Min(0)] public int maximumCoins;
        [Min(0)] public int minimumGems;
        [Min(0)] public int maximumGems;
        [Min(0)] public int blueprintReward;
        [Tooltip("When disabled, reward rarity follows the chest tier without changing serialized chest assets.")]
        public bool hasExplicitRewardRarity;
        public ItemRarity rewardRarity;
        public Sprite closedSprite;
        public Sprite openSprite;
        public Color accentColor = Color.white;

        public TimeSpan OpeningDuration => TimeSpan.FromSeconds(Mathf.Max(1, openingSeconds));

        public ItemRarity ResolveRewardRarity()
        {
            if (hasExplicitRewardRarity) return ItemRarityUtility.Resolve(true, rewardRarity, false);
            switch (type)
            {
                case ChestType.Bronze: return ItemRarity.Uncommon;
                case ChestType.Silver: return ItemRarity.Rare;
                case ChestType.Gold: return ItemRarity.Epic;
                case ChestType.Master: return ItemRarity.Legendary;
                case ChestType.Legendary: return ItemRarity.Mythic;
                default: return ItemRarity.Common;
            }
        }
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Chest Catalog", fileName = "ChestCatalog")]
    public sealed class ChestCatalog : ScriptableObject
    {
        public const float GameplayIntervalSeconds = 300f;
        public const int MaximumInventorySize = 6;
        public const int CoinAdLimit = 3;
        public const int CoinAdWindowHours = 6;
        public const int CoinAdMinimumReward = 5000;
        public const int CoinAdMaximumReward = 10000;
        public const int ChestAdLimit = 6;
        public const int ChestAdWindowHours = 12;
        public ChestDefinition[] chests;
        public Sprite shelfSprite;
        public Sprite coinsChestSprite;

        public ChestDefinition Find(ChestType type)
        {
            if (chests == null) return null;
            for (int i = 0; i < chests.Length; i++)
                if (chests[i] != null && chests[i].type == type) return chests[i];
            return null;
        }

        public int TotalRarityWeight
        {
            get
            {
                int total = 0;
                if (chests == null) return total;
                for (int i = 0; i < chests.Length; i++)
                    if (chests[i] != null) total += Mathf.Max(0, chests[i].rarityWeight);
                return total;
            }
        }
    }
}
