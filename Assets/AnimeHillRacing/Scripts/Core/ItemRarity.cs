using UnityEngine;

namespace AnimeHillRacing.Core
{
    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    public static class ItemRarityUtility
    {
        public const int Count = 6;

        public static ItemRarity Resolve(bool hasExplicitRarity, ItemRarity configuredRarity,
            bool isDefaultItem)
        {
            if (hasExplicitRarity)
                return (ItemRarity)Mathf.Clamp((int)configuredRarity, 0, Count - 1);
            return isDefaultItem ? ItemRarity.Common : ItemRarity.Mythic;
        }

        public static string DisplayName(ItemRarity rarity) =>
            ((ItemRarity)Mathf.Clamp((int)rarity, 0, Count - 1)).ToString().ToUpperInvariant();

        public static Color AccentColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return new Color(.72f, .38f, .16f, 1f);
                case ItemRarity.Rare: return new Color(.50f, .76f, .92f, 1f);
                case ItemRarity.Epic: return new Color(1f, .72f, .12f, 1f);
                case ItemRarity.Legendary: return new Color(.18f, .72f, 1f, 1f);
                case ItemRarity.Mythic: return new Color(.78f, .22f, 1f, 1f);
                default: return new Color(.58f, .62f, .68f, 1f);
            }
        }
    }
}
