using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.UI
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/UI Asset Catalog",
        fileName = "HillAdventurerUiAssets")]
    public sealed class HillAdventurerUiCatalog : ScriptableObject
    {
        [Header("Economy")]
        [Tooltip("Coin_To_Replace_Current: single coin used only by balance counters.")]
        public Sprite coinCounterIcon;
        [Tooltip("Coins: coin pile used for offers and rewards.")]
        public Sprite coinsRewardIcon;
        [Tooltip("Gem: single gem used by counters and the physical pickup.")]
        public Sprite gemCounterIcon;
        [Tooltip("Gems: gem pile used for offers and rewards.")]
        public Sprite gemsRewardIcon;

        [Header("Garage")]
        public Sprite ownedBadge;
        public Sprite equippedBadge;
        public Sprite readyButton;

        [Header("Shop")]
        public Sprite starterPackArtwork;
        public Sprite buyStarterPackButton;

        [Header("Chest Rewards")]
        public Sprite chestRewardBackground;
        public Sprite youGotHeader;
        [Tooltip("Common, Uncommon, Rare, Epic, Legendary, Mythic in that order.")]
        public Sprite[] rarityFrames = new Sprite[ItemRarityUtility.Count];

        [Header("Track Selection")]
        public Sprite leaderboardButton;

        private static HillAdventurerUiCatalog cached;

        public static HillAdventurerUiCatalog Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<HillAdventurerUiCatalog>("HillAdventurerUiAssets");
                return cached;
            }
        }

        public Sprite GetRarityFrame(ItemRarity rarity)
        {
            int index = Mathf.Clamp((int)rarity, 0, ItemRarityUtility.Count - 1);
            return rarityFrames != null && index < rarityFrames.Length ? rarityFrames[index] : null;
        }

        private void OnValidate()
        {
            if (rarityFrames == null || rarityFrames.Length != ItemRarityUtility.Count)
                System.Array.Resize(ref rarityFrames, ItemRarityUtility.Count);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cached = null;
    }
}
