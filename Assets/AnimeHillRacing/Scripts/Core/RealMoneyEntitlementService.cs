using System;
using System.Collections.Generic;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using AnimeHillRacing.Ads;

namespace AnimeHillRacing.Core
{
    public enum StoreProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    public readonly struct RealMoneyProductDefinition
    {
        public readonly string ProductId;
        public readonly StoreProductType ProductType;

        public RealMoneyProductDefinition(string productId, StoreProductType productType)
        {
            ProductId = productId ?? string.Empty;
            ProductType = productType;
        }
    }

    /// <summary>Maps store-owned products onto the existing local ownership systems.</summary>
    public sealed class RealMoneyEntitlementService
    {
        private enum EntitlementKind
        {
            StarterPack,
            Skin,
            GemPack,
            Vip
        }

        private sealed class Entitlement
        {
            public EntitlementKind Kind;
            public string VehicleId;
            public string SkinId;
            public int GemAmount;
            public StoreProductType ProductType;
        }

        private readonly GameCatalog catalog;
        private readonly StarterPackConfiguration starterPack;
        private readonly Dictionary<string, Entitlement> entitlements =
            new Dictionary<string, Entitlement>(StringComparer.Ordinal);
        private readonly List<RealMoneyProductDefinition> products =
            new List<RealMoneyProductDefinition>();

        public IReadOnlyList<RealMoneyProductDefinition> Products => products;

        public RealMoneyEntitlementService(GameCatalog gameCatalog,
            StarterPackConfiguration starterPackConfiguration)
        {
            catalog = gameCatalog;
            starterPack = starterPackConfiguration;
            BuildMappings();
        }

        public bool Contains(string productId) =>
            !string.IsNullOrWhiteSpace(productId) && entitlements.ContainsKey(productId);

        public bool IsConsumable(string productId) =>
            !string.IsNullOrWhiteSpace(productId) &&
            entitlements.TryGetValue(productId, out Entitlement entitlement) &&
            entitlement.ProductType == StoreProductType.Consumable;

        public bool IsVip(string productId) => !string.IsNullOrEmpty(productId) && productId == AdEntitlementService.VipProductId;

        public bool TryFulfillNewPurchase(string productId, string transactionId, out string error)
        {
            if (!TryGet(productId, out Entitlement entitlement, out error)) return false;
            if (string.IsNullOrWhiteSpace(transactionId)) { error = "STORE TRANSACTION ID IS MISSING"; return false; }
            bool fulfilled;
            if (entitlement.Kind == EntitlementKind.StarterPack)
                fulfilled = SaveManager.TryGrantStarterPack(catalog, starterPack, out error);
            else if (entitlement.Kind == EntitlementKind.GemPack)
                fulfilled = SaveManager.TryGrantPurchasedGems(productId, transactionId,
                    entitlement.GemAmount, out error);
            else if (entitlement.Kind == EntitlementKind.Vip) fulfilled = true;
            else {
                fulfilled = SaveManager.GrantPermanentCustomization(entitlement.VehicleId, "Skin", entitlement.SkinId);
                if (!fulfilled) error = "IAP SKIN ENTITLEMENT CONFIGURATION IS INVALID";
            }
            if (fulfilled) AdEntitlementService.CompleteConfirmedPurchase(transactionId);
            return fulfilled;
        }

        public bool TryRestorePermanentEntitlement(string productId, out string error)
        {
            if (!TryGet(productId, out Entitlement entitlement, out error)) return false;
            if (entitlement.ProductType == StoreProductType.Consumable)
            {
                error = "CONSUMABLE PRODUCTS ARE NOT RESTORED";
                return false;
            }
            if (entitlement.Kind == EntitlementKind.StarterPack)
                return SaveManager.RestoreStarterPackOwnership(catalog, starterPack, out error);
            if (entitlement.Kind == EntitlementKind.Vip) return true;
            if (SaveManager.GrantPermanentCustomization(entitlement.VehicleId, "Skin",
                    entitlement.SkinId)) return true;
            error = "IAP SKIN ENTITLEMENT CONFIGURATION IS INVALID";
            return false;
        }

        private void BuildMappings()
        {
            if (!string.IsNullOrWhiteSpace(AdEntitlementService.VipProductId))
                Add(AdEntitlementService.VipProductId, new Entitlement { Kind = EntitlementKind.Vip, ProductType = StoreProductType.Subscription }, StoreProductType.Subscription);
            if (starterPack != null && !string.IsNullOrWhiteSpace(starterPack.googlePlayProductId))
                Add(starterPack.googlePlayProductId, new Entitlement
                {
                    Kind = EntitlementKind.StarterPack,
                    ProductType = StoreProductType.NonConsumable
                }, StoreProductType.NonConsumable);

            IReadOnlyList<GemPackProduct> gemPacks = GemPackCatalog.Products;
            for (int packIndex = 0; packIndex < gemPacks.Count; packIndex++)
            {
                GemPackProduct pack = gemPacks[packIndex];
                Add(pack.ProductId, new Entitlement
                {
                    Kind = EntitlementKind.GemPack,
                    GemAmount = pack.GemAmount,
                    ProductType = StoreProductType.Consumable
                }, StoreProductType.Consumable);
            }

            if (catalog?.vehicles == null) return;
            for (int vehicleIndex = 0; vehicleIndex < catalog.vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = catalog.vehicles[vehicleIndex];
                if (vehicle?.skins == null) continue;
                for (int skinIndex = 0; skinIndex < vehicle.skins.Length; skinIndex++)
                {
                    VehicleDefinition.CustomizationOption skin = vehicle.skins[skinIndex];
                    if (skin == null ||
                        skin.purchaseCurrency != CustomizationPurchaseCurrency.RealMoney) continue;
                    if (string.IsNullOrWhiteSpace(skin.googlePlayProductId))
                    {
                        Debug.LogWarning($"[IAP] Real-money skin has no Product ID: " +
                                         $"{vehicle.vehicleId}/{skin.optionId}");
                        continue;
                    }
                    Add(skin.googlePlayProductId, new Entitlement
                    {
                        Kind = EntitlementKind.Skin,
                        VehicleId = vehicle.vehicleId,
                        SkinId = skin.optionId,
                        ProductType = StoreProductType.NonConsumable
                    }, StoreProductType.NonConsumable);
                }
            }
        }

        private void Add(string productId, Entitlement entitlement, StoreProductType productType)
        {
            if (entitlements.ContainsKey(productId))
            {
                Debug.LogError($"[IAP] Duplicate Google Play Product ID in local configuration: " +
                               productId);
                return;
            }
            entitlements.Add(productId, entitlement);
            products.Add(new RealMoneyProductDefinition(productId, productType));
        }

        private bool TryGet(string productId, out Entitlement entitlement, out string error)
        {
            if (!string.IsNullOrWhiteSpace(productId) &&
                entitlements.TryGetValue(productId, out entitlement))
            {
                error = string.Empty;
                return true;
            }
            entitlement = null;
            error = "STORE PRODUCT IS NOT MAPPED TO A LOCAL ENTITLEMENT";
            return false;
        }
    }
}
