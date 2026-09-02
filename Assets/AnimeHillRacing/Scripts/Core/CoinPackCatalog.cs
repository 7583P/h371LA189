using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Core
{
    public sealed class CoinPackProduct
    {
        public readonly int CoinAmount;
        public readonly int GemCost;
        public readonly string SpriteResourcePath;

        public CoinPackProduct(int coinAmount, int gemCost, string spriteResourcePath)
        {
            CoinAmount = Math.Max(0, coinAmount);
            GemCost = Math.Max(0, gemCost);
            SpriteResourcePath = spriteResourcePath ?? string.Empty;
        }
    }

    /// <summary>Coin bundles purchased with the player's earned or purchased Gems.</summary>
    public static class CoinPackCatalog
    {
        public const int Columns = 4;

        private static readonly CoinPackProduct[] products =
        {
            new CoinPackProduct(10000, 50, "UI/CoinPacks/Coins_10000"),
            new CoinPackProduct(50000, 250, "UI/CoinPacks/Coins_50000"),
            new CoinPackProduct(100000, 450, "UI/CoinPacks/Coins_100000"),
            new CoinPackProduct(250000, 900, "UI/CoinPacks/Coins_250000"),
            new CoinPackProduct(500000, 1700, "UI/CoinPacks/Coins_500000"),
            new CoinPackProduct(1000000, 3000, "UI/CoinPacks/Coins_1000000"),
            new CoinPackProduct(2000000, 5000, "UI/CoinPacks/Coins_2000000")
        };

        public static IReadOnlyList<CoinPackProduct> Products => products;
    }
}
