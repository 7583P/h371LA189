using System;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class PurchasableContentAudit
    {
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private const string CountrysidePath = "Assets/AnimeHillRacing/MapProfiles/Countryside.asset";
        private const string CoinsKey = "AHR.TotalCoins";
        private const string AuditMapId = "codex-purchase-audit-map";
        private const string AuditVehicleId = "codex-purchase-audit-vehicle";

        [MenuItem("Anime Hill Racing/Audit Purchases + Countryside 3000m")]
        public static void RunFromMenu()
        {
            Run();
            Debug.Log("PURCHASABLE_CONTENT_AUDIT_OK");
        }

        public static void RunBatch()
        {
            try
            {
                Run();
                Debug.Log("PURCHASABLE_CONTENT_AUDIT_OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            MapProfile countryside = AssetDatabase.LoadAssetAtPath<MapProfile>(CountrysidePath);
            Require(catalog != null, "GameCatalog is missing.");
            Require(countryside != null, "Countryside profile is missing.");
            ValidateCatalog(catalog);
            ValidatePurchasesWithoutChangingTheSave();
            ValidateCountryside3000m(countryside);
            ValidateAndroidAccessoryShader();
        }

        private static void ValidateCatalog(GameCatalog catalog)
        {
            var mapPrices = new Dictionary<string, int>
            {
                { "countryside", 0 }, { "city", 10000 }, { "neon-city", 25000 },
                { "rainyer", 30000 }, { "mountains", 30000 }, { "space", 40000 },
                { "north-pole", 50000 }
            };
            foreach (KeyValuePair<string, int> expected in mapPrices)
            {
                MapDefinition map = null;
                for (int i = 0; i < (catalog.maps?.Length ?? 0); i++)
                    if (catalog.maps[i] != null && catalog.maps[i].mapId == expected.Key)
                    {
                        map = catalog.maps[i];
                        break;
                    }
                Require(map != null, $"Map {expected.Key} is missing from GameCatalog.");
                Require(map.coinPrice == expected.Value, $"Map {expected.Key} has the wrong coin price.");
                Require(map.unlockedByDefault == (expected.Value == 0),
                    $"Map {expected.Key} has the wrong default lock state.");
            }

            var vehiclePrices = new Dictionary<string, int>
            {
                { "car-01", 0 }, { "hill-adventurer-01", 20000 }, { "buggy-01", 30000 },
                { "rally-01", 50000 }, { "sport-bike-01", 50000 },
                { "aerial-satelite-01", 70000 }, { "formula-01", 100000 }
            };
            foreach (KeyValuePair<string, int> expected in vehiclePrices)
            {
                VehicleDefinition vehicle = catalog.FindVehicle(expected.Key);
                Require(vehicle != null && vehicle.vehicleId == expected.Key,
                    $"Vehicle {expected.Key} is missing from GameCatalog.");
                Require(vehicle.coinPrice == expected.Value,
                    $"Vehicle {expected.Key} has the wrong coin price.");
                Require(vehicle.unlockedByDefault == (expected.Value == 0),
                    $"Vehicle {expected.Key} has the wrong default lock state.");
            }

            int previousMapPrice = -1;
            for (int i = 0; i < (catalog.maps?.Length ?? 0); i++)
            {
                MapDefinition map = catalog.maps[i];
                if (map == null) continue;
                Require(map.coinPrice >= previousMapPrice,
                    $"Track order is not cheapest-to-most-expensive at {map.displayName}.");
                previousMapPrice = map.coinPrice;
            }

            int previousVehiclePrice = -1;
            bool foundFirstVehicle = false;
            for (int i = 0; i < (catalog.vehicles?.Length ?? 0); i++)
            {
                VehicleDefinition vehicle = catalog.vehicles[i];
                if (vehicle == null) continue;
                if (!foundFirstVehicle)
                {
                    Require(vehicle.vehicleId == "car-01", "Jeep must be the first vehicle.");
                    foundFirstVehicle = true;
                }
                Require(vehicle.coinPrice >= previousVehiclePrice,
                    $"Vehicle order is not cheapest-to-most-expensive at {vehicle.displayName}.");
                previousVehiclePrice = vehicle.coinPrice;
            }
        }

        private static void ValidatePurchasesWithoutChangingTheSave()
        {
            string mapUnlockKey = "AHR.Map.Unlock." + AuditMapId;
            string vehicleUnlockKey = "AHR.Vehicle.Unlock." + AuditVehicleId;
            bool hadCoins = PlayerPrefs.HasKey(CoinsKey);
            int savedCoins = PlayerPrefs.GetInt(CoinsKey, 0);
            try
            {
                PlayerPrefs.DeleteKey(mapUnlockKey);
                PlayerPrefs.DeleteKey(vehicleUnlockKey);
                PlayerPrefs.SetInt(CoinsKey, 100);
                MapProfile map = ScriptableObject.CreateInstance<MapProfile>();
                map.mapId = AuditMapId;
                map.unlockedByDefault = false;
                map.coinPrice = 40;
                Require(SaveManager.TryPurchaseMap(map, out CoinUnlockPurchaseResult mapResult) &&
                        mapResult == CoinUnlockPurchaseResult.Success && SaveManager.TotalCoins == 60,
                    "Map purchase did not unlock and debit atomically.");
                Require(!SaveManager.TryPurchaseMap(map, out mapResult) &&
                        mapResult == CoinUnlockPurchaseResult.AlreadyUnlocked && SaveManager.TotalCoins == 60,
                    "Duplicate map purchase was not rejected.");

                VehicleDefinition vehicle = ScriptableObject.CreateInstance<VehicleDefinition>();
                vehicle.vehicleId = AuditVehicleId;
                vehicle.unlockedByDefault = false;
                vehicle.coinPrice = 70;
                Require(!SaveManager.TryPurchaseVehicle(vehicle, out CoinUnlockPurchaseResult vehicleResult) &&
                        vehicleResult == CoinUnlockPurchaseResult.NotEnoughCoins && SaveManager.TotalCoins == 60,
                    "Insufficient vehicle purchase was not rejected.");
                PlayerPrefs.SetInt(CoinsKey, 100);
                Require(SaveManager.TryPurchaseVehicle(vehicle, out vehicleResult) &&
                        vehicleResult == CoinUnlockPurchaseResult.Success && SaveManager.TotalCoins == 30,
                    "Vehicle purchase did not unlock and debit atomically.");
                UnityEngine.Object.DestroyImmediate(map);
                UnityEngine.Object.DestroyImmediate(vehicle);
            }
            finally
            {
                PlayerPrefs.DeleteKey(mapUnlockKey);
                PlayerPrefs.DeleteKey(vehicleUnlockKey);
                if (hadCoins) PlayerPrefs.SetInt(CoinsKey, savedCoins);
                else PlayerPrefs.DeleteKey(CoinsKey);
                PlayerPrefs.Save();
            }
        }

        private static void ValidateCountryside3000m(MapProfile profile)
        {
            Require(profile.useCountrysideSpecialFeatures, "Countryside special feature grammar is disabled.");
            var layouts = new Dictionary<int, CountrysideSpecialFeatureLayout>();
            for (float x = 0f; x <= 3000f; x += .5f)
                if (TerrainGenerator.TryGetCountrysideSpecialFeature(profile, x,
                        out CountrysideSpecialFeatureLayout layout))
                    layouts[layout.index] = layout;

            int flats = 0;
            int slashes = 0;
            foreach (CountrysideSpecialFeatureLayout layout in layouts.Values)
            {
                if (layout.type == CountrysideSpecialFeatureType.LongFlat)
                {
                    flats++;
                    float length = layout.coreEnd - layout.coreStart;
                    Require(Mathf.Abs(length - 150f) < .01f || Mathf.Abs(length - 200f) < .01f,
                        "Countryside long flat is not exactly 150m or 200m.");
                    float a = TerrainGenerator.SampleHeight(profile, layout.coreStart + 2f);
                    float b = TerrainGenerator.SampleHeight(profile, layout.coreEnd - 2f);
                    Require(Mathf.Abs(a - b) < .001f, "Countryside long flat is not physically level.");
                }
                else
                {
                    slashes++;
                    float centre = (layout.slashStart + layout.coreEnd) * .5f;
                    float left = TerrainGenerator.SampleHeight(profile, layout.slashStart);
                    float peak = TerrainGenerator.SampleHeight(profile, centre);
                    float right = TerrainGenerator.SampleHeight(profile, layout.coreEnd);
                    Require(peak > left + 5f && Mathf.Abs(left - right) < .001f,
                        "Countryside diagonal _/\\ feature geometry is invalid.");
                }
            }
            Require(flats >= 2 && slashes >= 1,
                $"The first 3000m need repeated long flats and a diagonal feature; got {flats}/{slashes}.");

            const float chunkLength = 80f;
            const int samples = 48;
            bool hasBoundary = false;
            float boundaryHeight = 0f;
            float boundarySlope = 0f;
            for (int chunk = 0; chunk * chunkLength < 3000f; chunk++)
            {
                var mesh = new Mesh();
                TerrainGenerator.BuildMesh(mesh, profile, chunk * chunkLength, chunkLength, samples, 8f,
                    hasBoundary, boundaryHeight, boundarySlope, out float[] heights,
                    out float endHeight, out float endSlope);
                Require(heights != null && heights.Length == samples + 1, "Terrain chunk sample count is invalid.");
                if (hasBoundary)
                    Require(Mathf.Abs(heights[0] - boundaryHeight) < .001f,
                        $"Terrain seam detected at {chunk * chunkLength:0}m.");
                for (int i = 0; i < heights.Length; i++)
                    Require(!float.IsNaN(heights[i]) && !float.IsInfinity(heights[i]),
                        $"Invalid terrain height at chunk {chunk}, sample {i}.");
                var collider = new Mesh();
                TerrainGenerator.BuildSurfaceColliderMesh(collider, profile, chunk * chunkLength,
                    chunkLength, heights, 8f);
                Require(collider.vertexCount == heights.Length * 2 && collider.triangles.Length > 0,
                    $"Physical surface collider missing at chunk {chunk}.");
                UnityEngine.Object.DestroyImmediate(collider);
                UnityEngine.Object.DestroyImmediate(mesh);
                boundaryHeight = endHeight;
                boundarySlope = endSlope;
                hasBoundary = true;
            }
        }

        private static void ValidateAndroidAccessoryShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/AnimeHillRacing/Resources/AccessoryRainbow.shader");
            Require(shader != null && shader.name == "AnimeHillRacing/Accessory Rainbow",
                "Accessory Rainbow shader is missing.");
            foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader))
                Require(!string.Equals(message.severity.ToString(), "Error",
                        StringComparison.OrdinalIgnoreCase),
                    "Accessory Rainbow shader compiler error: " + message.message);
            string graphicsSettings = File.ReadAllText("ProjectSettings/GraphicsSettings.asset");
            Require(graphicsSettings.Contains("4abc78ff99f840eea81c090082b3388f"),
                "Accessory Rainbow shader is not forced into player builds.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
