using System.Collections.Generic;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Core
{
    public enum BundleType
    {
        Cosmetic,
        Vehicle
    }

    public sealed class BundleDefinition
    {
        public string BundleId { get; }
        public string DisplayName { get; }
        public BundleType Type { get; }
        public Sprite Thumbnail { get; }
        public int GemPrice { get; }
        public string ScopeId { get; }
        public string Category { get; }
        public string OptionId { get; }
        public bool UnlockedByDefault { get; }
        public int BlueprintRequirement => Type == BundleType.Vehicle ? 150 : 100;

        public BundleDefinition(string bundleId, string displayName, BundleType type, Sprite thumbnail,
            int gemPrice, string scopeId, string category, string optionId, bool unlockedByDefault)
        {
            BundleId = bundleId;
            DisplayName = displayName;
            Type = type;
            Thumbnail = thumbnail;
            GemPrice = Mathf.Max(0, gemPrice);
            ScopeId = scopeId;
            Category = category;
            OptionId = optionId;
            UnlockedByDefault = unlockedByDefault;
        }
    }

    public static class PremiumBundleCatalog
    {
        public static List<BundleDefinition> GetAll(GameCatalog catalog)
        {
            var bundles = new List<BundleDefinition>();
            if (catalog?.vehicles == null) return bundles;
            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition vehicle = catalog.vehicles[i];
                if (vehicle == null) continue;
                if (vehicle.allowsBlueprintProgression && vehicle.gemPrice > 0)
                {
                    bundles.Add(new BundleDefinition(
                        VehicleBundleId(vehicle.vehicleId), vehicle.displayName + " VEHICLE BUNDLE",
                        BundleType.Vehicle, vehicle.previewSprite, vehicle.gemPrice, vehicle.vehicleId,
                        "Vehicle", vehicle.vehicleId, vehicle.unlockedByDefault));
                }
                AppendCosmetics(bundles, vehicle, vehicle.skins, "Skin");
                AppendCosmetics(bundles, vehicle, vehicle.wheels, "Wheels");
            }
            return bundles;
        }

        public static List<BundleDefinition> GetEligible(GameCatalog catalog)
        {
            List<BundleDefinition> all = GetAll(catalog);
            all.RemoveAll(SaveManager.IsBundleUnlocked);
            return all;
        }

        public static BundleDefinition Find(GameCatalog catalog, string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId)) return null;
            List<BundleDefinition> all = GetAll(catalog);
            for (int i = 0; i < all.Count; i++)
                if (all[i].BundleId == bundleId) return all[i];
            return null;
        }

        public static string CosmeticBundleId(string vehicleId, string category, string optionId) =>
            $"cosmetic:{vehicleId}:{category}:{optionId}";

        public static string VehicleBundleId(string vehicleId) => $"vehicle:{vehicleId}";

        private static void AppendCosmetics(List<BundleDefinition> target, VehicleDefinition vehicle,
            VehicleDefinition.CustomizationOption[] options, string category)
        {
            if (options == null) return;
            for (int i = 0; i < options.Length; i++)
            {
                VehicleDefinition.CustomizationOption option = options[i];
                if (option == null || !option.allowsBlueprintProgression || option.gemPrice <= 0) continue;
                string displayName = option.displayName ?? string.Empty;
                if (displayName.IndexOf(vehicle.displayName, System.StringComparison.OrdinalIgnoreCase) < 0)
                    displayName += " - " + vehicle.displayName;
                target.Add(new BundleDefinition(
                    CosmeticBundleId(vehicle.vehicleId, category, option.optionId),
                    displayName,
                    option.bundleType, option.sprite, option.gemPrice, vehicle.vehicleId, category,
                    option.optionId, option.unlockedByDefault));
            }
        }
    }

    public static class BlueprintProgressMath
    {
        public const int FullPackPrice = 1000000;

        public static int Add(int current, int amount, int requirement) =>
            Mathf.Clamp(Mathf.Max(0, current) + Mathf.Max(0, amount), 0, Mathf.Max(0, requirement));

        public static bool CanPurchase(int coins, int price, bool unlocked) =>
            !unlocked && price > 0 && coins >= price;

        public static bool CanPurchase(int coins, bool unlocked) =>
            CanPurchase(coins, FullPackPrice, unlocked);
    }

    public enum BlueprintPackTier
    {
        One = 0,
        Five = 1,
        Ten = 2,
        Hundred = 3
    }

    public static class BlueprintPackCatalog
    {
        public const int Count = 4;

        public static int GetBlueprintAmount(BlueprintPackTier tier)
        {
            switch (tier)
            {
                case BlueprintPackTier.One: return 1;
                case BlueprintPackTier.Five: return 5;
                case BlueprintPackTier.Ten: return 10;
                case BlueprintPackTier.Hundred: return 100;
                default: return 0;
            }
        }

        public static int GetCoinPrice(BlueprintPackTier tier)
        {
            switch (tier)
            {
                case BlueprintPackTier.One: return 10000;
                case BlueprintPackTier.Five: return 50000;
                case BlueprintPackTier.Ten: return 100000;
                case BlueprintPackTier.Hundred: return 1000000;
                default: return 0;
            }
        }

        public static bool IsValid(BlueprintPackTier tier) =>
            GetBlueprintAmount(tier) > 0 && GetCoinPrice(tier) > 0;
    }
}
