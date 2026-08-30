#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class ChestDebugTools
    {
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private const string TotalGemsKey = "AHR.TotalGems";

        [MenuItem("Anime Hill Racing/Chests/DEBUG +30 MIN CHEST PROGRESS")]
        public static void AddThirtyMinutes()
        {
            GameCatalog catalog = LoadCatalog();
            List<ChestRecord> granted = ChestSystem.DebugAddGameplay(catalog, 1800f);
            Debug.Log($"CHEST_DEBUG_PROGRESS: granted {granted.Count} chest(s), remainder={ChestSystem.GameplayProgress:0.0}s.");
        }

        [MenuItem("Anime Hill Racing/Chests/DEBUG COMPLETE CURRENT CHEST")]
        public static void CompleteCurrent() =>
            Debug.Log("CHEST_DEBUG_COMPLETE: " + (ChestSystem.DebugCompleteOpening() ? "READY" : "NO OPENING CHEST"));

        [MenuItem("Anime Hill Racing/Chests/DEBUG GRANT MASTER")]
        public static void GrantMaster() => Grant(ChestType.Master);

        [MenuItem("Anime Hill Racing/Chests/DEBUG GRANT LEGENDARY")]
        public static void GrantLegendary() => Grant(ChestType.Legendary);

        [MenuItem("Anime Hill Racing/Chests/DEBUG ADD 95 BLUEPRINTS")]
        public static void AddBlueprints()
        {
            GameCatalog catalog = LoadCatalog();
            List<BundleDefinition> eligible = PremiumBundleCatalog.GetEligible(catalog);
            if (eligible.Count == 0) { Debug.Log("CHEST_DEBUG_BLUEPRINTS: no eligible bundle."); return; }
            int total = SaveManager.AddBlueprints(eligible[0], 95, out bool unlocked);
            Debug.Log($"CHEST_DEBUG_BLUEPRINTS: {eligible[0].DisplayName}={total}/{eligible[0].BlueprintRequirement}, unlocked={unlocked}.");
        }

        [MenuItem("Anime Hill Racing/Chests/Validate Distance Bonus")]
        public static void ValidateDistanceBonus()
        {
            ValidateDistanceBonusContract();
            Debug.Log("DISTANCE_BONUS_SEQUENCE_PASS: every 500m => " +
                      "+100,+200,+300,+400,+500,+1000,+1500,...,+5000; capped at +5000.");
        }

        [MenuItem("Anime Hill Racing/Chests/Validate Chest + Blueprint System")]
        public static void ValidateSystem()
        {
            GameCatalog gameCatalog = LoadCatalog();
            ChestCatalog catalog = gameCatalog.chestCatalog;
            if (catalog == null || catalog.TotalRarityWeight != 221)
                throw new InvalidOperationException("Chest weights must total 221.");
            ValidateDefinition(catalog, ChestType.Bronze, 90, 2, 1200, 1500, 0);
            ValidateDefinition(catalog, ChestType.Silver, 70, 4, 2500, 3700, 0);
            ValidateDefinition(catalog, ChestType.Gold, 50, 6, 5000, 7500, 0);
            ValidateDefinition(catalog, ChestType.Master, 10, 12, 15000, 20000, 5);
            ValidateDefinition(catalog, ChestType.Legendary, 1, 24, 30000, 50000, 10);

            float progress = 1799f;
            if (ChestProgressMath.Advance(ref progress, .9f, 1800f) != 0 ||
                ChestProgressMath.Advance(ref progress, .1f, 1800f) != 1 || Mathf.Abs(progress) > .001f)
                throw new InvalidOperationException("29:59 -> 30:00 milestone contract failed.");
            progress = 1790f;
            if (ChestProgressMath.Advance(ref progress, 25f, 1800f) != 1 || Mathf.Abs(progress - 15f) > .001f)
                throw new InvalidOperationException("Gameplay remainder contract failed.");

            var offline = new ChestRecord
            {
                state = ChestState.Opening,
                openReadyUtc = DateTime.UtcNow.AddHours(-1).ToString("O")
            };
            if (!offline.RefreshState(DateTime.UtcNow) || offline.state != ChestState.ReadyToClaim)
                throw new InvalidOperationException("Offline UTC opening contract failed.");
            if (BlueprintProgressMath.Add(99, 5, 100) != 100 ||
                BlueprintProgressMath.Add(145, 5, 150) != 150 ||
                BlueprintProgressMath.Add(100, 100, 150) != 150)
                throw new InvalidOperationException("Blueprint threshold contract failed.");
            if (BlueprintProgressMath.CanPurchase(999999, false) ||
                !BlueprintProgressMath.CanPurchase(1000000, false) || BlueprintProgressMath.CanPurchase(1000000, true) ||
                BlueprintPackCatalog.GetBlueprintAmount(BlueprintPackTier.One) != 1 ||
                BlueprintPackCatalog.GetCoinPrice(BlueprintPackTier.One) != 10000 ||
                BlueprintPackCatalog.GetBlueprintAmount(BlueprintPackTier.Five) != 5 ||
                BlueprintPackCatalog.GetCoinPrice(BlueprintPackTier.Five) != 50000 ||
                BlueprintPackCatalog.GetBlueprintAmount(BlueprintPackTier.Ten) != 10 ||
                BlueprintPackCatalog.GetCoinPrice(BlueprintPackTier.Ten) != 100000 ||
                BlueprintPackCatalog.GetBlueprintAmount(BlueprintPackTier.Hundred) != 100 ||
                BlueprintPackCatalog.GetCoinPrice(BlueprintPackTier.Hundred) != 1000000)
                throw new InvalidOperationException("Blueprint Shop balance contract failed.");

            UpgradeEconomyDefinition economy = gameCatalog.upgradeEconomy;
            if (economy == null || economy.jeepFirstUpgradeCost != 1000 ||
                economy.formulaFirstUpgradeCost != 2500 || economy.rallyFirstUpgradeCost != 1800 ||
                economy.sportBikeFirstUpgradeCost != 3000 || economy.jeepFinalUpgradeCost != 50000 ||
                economy.formulaFinalUpgradeCost != 150000 || economy.rallyFinalUpgradeCost != 100000 ||
                economy.sportBikeFinalUpgradeCost != 200000 || economy.roundingStep != 250)
                throw new InvalidOperationException("All vehicle tune prices must use the x10 economy.");
            ValidateDistanceBonusContract();

            ValidateGemSaveCompatibility();

            List<BundleDefinition> bundles = PremiumBundleCatalog.GetAll(gameCatalog);
            if (PremiumBundleCatalog.Find(gameCatalog, PremiumBundleCatalog.CosmeticBundleId(
                    "formula-01", "Skin", "formula-skin-hill-adventurer-gold")) != null)
                throw new InvalidOperationException(
                    "Formula Hill Adventurer Gold must not participate in blueprint progression.");
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleDefinition bundle = bundles[i];
                if (bundle.DisplayName.IndexOf("GHOSTY", StringComparison.OrdinalIgnoreCase) < 0) continue;
                VehicleDefinition vehicle = gameCatalog.FindVehicle(bundle.ScopeId);
                if (vehicle == null || bundle.DisplayName.IndexOf(vehicle.displayName,
                        StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Every Ghosty Wheels bundle must identify its vehicle.");
            }

            for (int vehicleIndex = 0; vehicleIndex < gameCatalog.vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = gameCatalog.vehicles[vehicleIndex];
                if (vehicle?.wheels == null) continue;
                for (int wheelIndex = 0; wheelIndex < vehicle.wheels.Length; wheelIndex++)
                {
                    VehicleDefinition.CustomizationOption wheel = vehicle.wheels[wheelIndex];
                    if (wheel == null) continue;
                    bool isDefault = wheel.optionId == vehicle.DefaultWheelId;
                    ItemRarity expected = isDefault ? ItemRarity.Common : ItemRarity.Mythic;
                    if (wheel.ResolveRarity(isDefault) != expected)
                        throw new InvalidOperationException(
                            $"Wheel rarity contract failed: {vehicle.vehicleId}/{wheel.optionId}.");
                }
            }
            if (gameCatalog.drivers != null)
                for (int driverIndex = 0; driverIndex < gameCatalog.drivers.Length; driverIndex++)
                {
                    DriverDefinition driver = gameCatalog.drivers[driverIndex];
                    if (driver?.accessories == null) continue;
                    for (int accessoryIndex = 0; accessoryIndex < driver.accessories.Length; accessoryIndex++)
                    {
                        DriverDefinition.CharacterAccessoryOption accessory = driver.accessories[accessoryIndex];
                        if (accessory == null) continue;
                        bool isDefault = accessory.accessoryId == driver.DefaultAccessoryId;
                        ItemRarity expected = isDefault ? ItemRarity.Common : ItemRarity.Mythic;
                        if (accessory.ResolveRarity(isDefault) != expected)
                            throw new InvalidOperationException(
                                $"Accessory rarity contract failed: {driver.driverId}/{accessory.accessoryId}.");
                    }
                }

            var persisted = new ChestSaveData
            {
                chestGameplayProgress = 1020f,
                inventory = new List<ChestRecord>
                {
                    new ChestRecord
                    {
                        chestId = "persistence-test", type = ChestType.Master, state = ChestState.Opening,
                        openStartedUtc = DateTime.UtcNow.AddHours(-1).ToString("O"),
                        openReadyUtc = DateTime.UtcNow.AddHours(11).ToString("O"), coinReward = 17420,
                        gemReward = 7,
                        blueprintBundleId = "cosmetic:test:Skin:test", blueprintReward = 5
                    }
                }
            };
            ChestSaveData restored = JsonUtility.FromJson<ChestSaveData>(JsonUtility.ToJson(persisted));
            if (restored == null || Mathf.Abs(restored.chestGameplayProgress - 1020f) > .001f ||
                restored.inventory == null || restored.inventory.Count != 1 ||
                restored.inventory[0].chestId != "persistence-test" || restored.inventory[0].coinReward != 17420 ||
                restored.inventory[0].gemReward != 7 ||
                restored.inventory[0].blueprintReward != 5)
                throw new InvalidOperationException("Chest inventory/progress persistence contract failed.");

            int[] counts = new int[5];
            var random = new SeededChestRandomSource(3124);
            const int samples = 221000;
            for (int i = 0; i < samples; i++) counts[(int)ChestRaritySelector.Roll(catalog, random)]++;
            int[] weights = { 90, 70, 50, 10, 1 };
            for (int i = 0; i < counts.Length; i++)
            {
                float actual = counts[i] / (float)samples;
                float expected = weights[i] / 221f;
                if (Mathf.Abs(actual - expected) > .006f)
                    throw new InvalidOperationException($"Monte Carlo distribution failed for {(ChestType)i}: {actual:P2} vs {expected:P2}.");
            }
            Debug.Log($"CHEST_BLUEPRINT_AUDIT_PASS: milestone 29:59/30:00, remainder 15s, UTC offline, " +
                $"coin ranges, Master +5, Legendary +10, normal 100, vehicle 150, " +
                $"Shop 10K:+1/50K:+5/100K:+10/1M:+100, tune prices x10, Gems save/add/spend validated, " +
                $"distance bonus PB-only each 500m, +100..+500 then +500..+5000 validated; " +
                $"Monte Carlo B/S/G/M/L={counts[0]/(float)samples:P2}/{counts[1]/(float)samples:P2}/" +
                $"{counts[2]/(float)samples:P2}/{counts[3]/(float)samples:P2}/{counts[4]/(float)samples:P2}.");
        }

        private static void ValidateDistanceBonusContract()
        {
            var distanceBonuses = new DistanceBonusTracker();
            if (SaveManager.DistanceBonusMinimum != 100 || SaveManager.DistanceBonusMaximum != 5000 ||
                SaveManager.DistanceBonusIncrement != 100 ||
                Mathf.Abs(SaveManager.DistanceBonusIntervalMeters - 500f) > .001f ||
                !distanceBonuses.TryClaimThrough(500f, out int firstDistanceAward, out float firstMilestone) ||
                firstDistanceAward != 100 || Mathf.Abs(firstMilestone - 500f) > .001f ||
                DistanceBonusTracker.GetAwardAtDistance(2500f) != 500 ||
                DistanceBonusTracker.GetAwardAtDistance(3000f) != 1000 ||
                DistanceBonusTracker.GetAwardAtDistance(6500f) != 4500 ||
                DistanceBonusTracker.GetAwardAtDistance(7000f) != 5000 ||
                DistanceBonusTracker.GetAwardAtDistance(7500f) != 5000)
                throw new InvalidOperationException(
                    "Distance bonus must progress +100..+500, then +500 per milestone up to +5000.");
            distanceBonuses.Reset(2750f);
            if (distanceBonuses.NextDistance != 3000f || distanceBonuses.NextAward != 1000 ||
                distanceBonuses.TryClaimThrough(2999f, out _, out _) ||
                !distanceBonuses.TryClaimThrough(3000f, out int newBestAward, out _) ||
                newBestAward != 1000)
                throw new InvalidOperationException("Distance bonus must skip milestones at or below the personal best.");
        }

        private static void Grant(ChestType type)
        {
            ChestRecord chest = ChestSystem.Grant(LoadCatalog(), type);
            Debug.Log(chest == null ? "CHEST_DEBUG_GRANT_FAILED" : $"CHEST_DEBUG_GRANTED: {type} {chest.chestId}");
        }

        private static void ValidateGemSaveCompatibility()
        {
            bool existed = PlayerPrefs.HasKey(TotalGemsKey);
            int original = PlayerPrefs.GetInt(TotalGemsKey, 0);
            try
            {
                // A pre-Gems save has no key and must resolve to zero without touching other progress.
                PlayerPrefs.DeleteKey(TotalGemsKey);
                if (SaveManager.TotalGems != 0)
                    throw new InvalidOperationException("A legacy save without Gems did not migrate to zero.");
                SaveManager.AddGems(1000, false);
                if (SaveManager.TotalGems != 1000 || SaveManager.TrySpendGems(1001, false) ||
                    SaveManager.TotalGems != 1000 || !SaveManager.TrySpendGems(400, false) ||
                    SaveManager.TotalGems != 600)
                    throw new InvalidOperationException("Gems add/spend/non-negative balance contract failed.");
                SaveManager.AddGems(-10, false);
                if (SaveManager.TotalGems != 600)
                    throw new InvalidOperationException("A negative Gems award changed the balance.");
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(TotalGemsKey, original);
                else PlayerPrefs.DeleteKey(TotalGemsKey);
                PlayerPrefs.Save();
            }
        }

        private static GameCatalog LoadCatalog()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("GameCatalog asset is missing.");
            return catalog;
        }

        private static void ValidateDefinition(ChestCatalog catalog, ChestType type, int weight, int hours,
            int minimumCoins, int maximumCoins, int blueprints)
        {
            ChestDefinition value = catalog.Find(type);
            if (value == null || value.rarityWeight != weight || value.openingSeconds != hours * 3600 ||
                value.minimumCoins != minimumCoins || value.maximumCoins != maximumCoins ||
                value.blueprintReward != blueprints || value.closedSprite == null || value.openSprite == null)
                throw new InvalidOperationException($"{type} chest definition is invalid.");
        }
    }
}
#endif
