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
            if (SaveManager.DistanceBonusMinimum != 200 || SaveManager.DistanceBonusMaximum != 2500 ||
                SaveManager.DistanceBonusIncrement != 100 || SaveManager.ScaleLegacyDistanceBonus(20) != 200 ||
                SaveManager.ScaleLegacyDistanceBonus(250) != 2500)
                throw new InvalidOperationException("Distance bonus x10 migration contract failed.");

            List<BundleDefinition> bundles = PremiumBundleCatalog.GetAll(gameCatalog);
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleDefinition bundle = bundles[i];
                if (bundle.DisplayName.IndexOf("GHOSTY", StringComparison.OrdinalIgnoreCase) < 0) continue;
                VehicleDefinition vehicle = gameCatalog.FindVehicle(bundle.ScopeId);
                if (vehicle == null || bundle.DisplayName.IndexOf(vehicle.displayName,
                        StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Every Ghosty Wheels bundle must identify its vehicle.");
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
                        blueprintBundleId = "cosmetic:test:Skin:test", blueprintReward = 5
                    }
                }
            };
            ChestSaveData restored = JsonUtility.FromJson<ChestSaveData>(JsonUtility.ToJson(persisted));
            if (restored == null || Mathf.Abs(restored.chestGameplayProgress - 1020f) > .001f ||
                restored.inventory == null || restored.inventory.Count != 1 ||
                restored.inventory[0].chestId != "persistence-test" || restored.inventory[0].coinReward != 17420 ||
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
                $"Shop 10K:+1/50K:+5/100K:+10/1M:+100, tune prices x10, distance bonus x10 validated; " +
                $"Monte Carlo B/S/G/M/L={counts[0]/(float)samples:P2}/{counts[1]/(float)samples:P2}/" +
                $"{counts[2]/(float)samples:P2}/{counts[3]/(float)samples:P2}/{counts[4]/(float)samples:P2}.");
        }

        private static void Grant(ChestType type)
        {
            ChestRecord chest = ChestSystem.Grant(LoadCatalog(), type);
            Debug.Log(chest == null ? "CHEST_DEBUG_GRANT_FAILED" : $"CHEST_DEBUG_GRANTED: {type} {chest.chestId}");
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
