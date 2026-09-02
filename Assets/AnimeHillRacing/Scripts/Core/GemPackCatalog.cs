using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Core
{
    public sealed class GemPackProduct
    {
        public readonly string ProductId;
        public readonly int GemAmount;
        public readonly string ReferenceUsdPrice;
        public readonly string AtlasResourcePath;
        public readonly int AtlasSlot;

        public GemPackProduct(string productId, int gemAmount, string referenceUsdPrice,
            string atlasResourcePath, int atlasSlot)
        {
            ProductId = productId ?? string.Empty;
            GemAmount = Math.Max(0, gemAmount);
            ReferenceUsdPrice = referenceUsdPrice ?? string.Empty;
            AtlasResourcePath = atlasResourcePath ?? string.Empty;
            AtlasSlot = Math.Max(0, Math.Min(3, atlasSlot));
        }
    }

    /// <summary>
    /// Local contract for the consumable Google Play gem products. Reference USD prices document
    /// the base prices to configure in Play Console; players always see store-localized metadata.
    /// </summary>
    public static class GemPackCatalog
    {
        public const int Columns = 4;
        public const string SmallAtlas = "UI/GemPackAtlasSmall";
        public const string LargeAtlas = "UI/GemPackAtlasLarge";

        private static readonly GemPackProduct[] products =
        {
            new GemPackProduct("gems_300", 300, "$0.99", SmallAtlas, 0),
            new GemPackProduct("gems_900", 900, "$2.59", SmallAtlas, 1),
            new GemPackProduct("gems_2000", 2000, "$4.99", SmallAtlas, 2),
            new GemPackProduct("gems_5000", 5000, "$9.99", SmallAtlas, 3),
            new GemPackProduct("gems_10000", 10000, "$15.99", LargeAtlas, 0),
            new GemPackProduct("gems_25000", 25000, "$29.99", LargeAtlas, 1),
            new GemPackProduct("gems_50000", 50000, "$49.99", LargeAtlas, 2),
            new GemPackProduct("gems_100000", 100000, "$79.99", LargeAtlas, 3)
        };

        private static readonly Dictionary<string, GemPackProduct> productsById = BuildIndex();

        public static IReadOnlyList<GemPackProduct> Products => products;

        public static bool TryGet(string productId, out GemPackProduct product)
        {
            if (!string.IsNullOrWhiteSpace(productId) && productsById.TryGetValue(productId, out product))
                return true;
            product = null;
            return false;
        }

        private static Dictionary<string, GemPackProduct> BuildIndex()
        {
            var index = new Dictionary<string, GemPackProduct>(StringComparer.Ordinal);
            for (int i = 0; i < products.Length; i++) index.Add(products[i].ProductId, products[i]);
            return index;
        }
    }
}
