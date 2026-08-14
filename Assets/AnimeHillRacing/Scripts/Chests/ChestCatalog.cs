using System;
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
        [Min(0)] public int blueprintReward;
        public Sprite closedSprite;
        public Sprite openSprite;
        public Color accentColor = Color.white;

        public TimeSpan OpeningDuration => TimeSpan.FromSeconds(Mathf.Max(1, openingSeconds));
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Chest Catalog", fileName = "ChestCatalog")]
    public sealed class ChestCatalog : ScriptableObject
    {
        public const float GameplayIntervalSeconds = 1800f;
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
