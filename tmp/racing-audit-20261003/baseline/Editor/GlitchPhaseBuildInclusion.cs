#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public sealed class GlitchPhaseBuildInclusion : IPreprocessBuildWithReport
    {
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private const string RegistryPath =
            "Assets/AnimeHillRacing/Resources/GlitchPhaseBuildRegistry.asset";

        static GlitchPhaseBuildInclusion() => EditorApplication.delayCall += EnsureRegistryIfSafe;

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            GlitchPhaseCatalogInstaller.EnsureConfigured();
            GlitchPhaseCosmeticsValidation.ValidateOrThrow();
            EnsureRegistry();
        }

        [MenuItem("Anime Hill Racing/Vehicles/Rebuild Glitch Phase APK Registry")]
        public static void RebuildMenu()
        {
            GlitchPhaseCatalogInstaller.EnsureConfigured();
            GlitchPhaseCosmeticsValidation.ValidateOrThrow();
            EnsureRegistry();
            Debug.Log("[Glitch Phase] Android inclusion registry rebuilt successfully.");
        }

        private static void EnsureRegistryIfSafe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            try
            {
                GlitchPhaseCatalogInstaller.EnsureConfigured();
                EnsureRegistry();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Glitch Phase] Could not refresh the APK registry yet: " +
                                 exception.Message);
            }
        }

        private static void EnsureRegistry()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("GameCatalog asset is missing.");

            var unique = new HashSet<Sprite>();
            if (catalog.vehicles != null)
                for (int vehicleIndex = 0; vehicleIndex < catalog.vehicles.Length; vehicleIndex++)
                {
                    VehicleDefinition vehicle = catalog.vehicles[vehicleIndex];
                    AddGlitchSprites(vehicle?.skins, "glitch-phase-", unique);
                    AddGlitchSprites(vehicle?.wheels, "glitch-wheel-", unique);
                }

            if (unique.Count == 0)
                throw new InvalidOperationException("No Glitch Phase sprite dependencies were found.");

            GlitchPhaseBuildRegistry registry =
                AssetDatabase.LoadAssetAtPath<GlitchPhaseBuildRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<GlitchPhaseBuildRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            var ordered = new List<Sprite>(unique);
            ordered.Sort((left, right) => string.CompareOrdinal(
                AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right)));
            registry.sprites = ordered.ToArray();
            registry.crystalTideShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/AnimeHillRacing/Resources/GlitchCrystalTide.shader");
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private static void AddGlitchSprites(VehicleDefinition.CustomizationOption[] options,
            string idPrefix, HashSet<Sprite> destination)
        {
            if (options == null) return;
            for (int i = 0; i < options.Length; i++)
            {
                VehicleDefinition.CustomizationOption option = options[i];
                if (option?.sprite != null && option.optionId != null &&
                    option.optionId.StartsWith(idPrefix, StringComparison.Ordinal))
                    destination.Add(option.sprite);
            }
        }
    }

    /// <summary>
    /// Repairs the data-driven Glitch Phase catalog before Unity calculates build dependencies.
    /// The artwork alone is not enough: an unreferenced PNG is omitted from Android builds, and
    /// historically only Buggy and Aerial Satelite had serialized customization entries.
    /// </summary>
    internal static class GlitchPhaseCatalogInstaller
    {
        private const string CatalogAssetPath =
            "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private static readonly int[] Thresholds =
            { 32768, 65536, 131072, 262144, 524288, 1048576 };

        private static readonly string[] Names =
        {
            "Glitch Phase 32767-65536",
            "Glitch Phase 65536-131072",
            "Glitch Phase 131072-262144",
            "Glitch Phase 262144-524288",
            "Glitch Phase 524288-1048576",
            "Glitch Phase 1048576-2097152"
        };

        private const string ArtRoot = "Assets/Art/Vehicles/Glitch Phase/";

        private static readonly Dictionary<string, string> SkinFolders =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "hill-adventurer-01", "HillAdventurer4x4/HillAdventurer4x4" },
                { "buggy-01", "Buggy/Buggy" },
                { "rally-01", "Rally/Rally" },
                { "sport-bike-01", "SportBike/SportBike" },
                { "aerial-satelite-01", "AerialSatelite/AerialSatelite" },
                { "formula-01", "Formula/Formula" }
            };

        public static bool EnsureConfigured()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogAssetPath);
            if (catalog == null) throw new InvalidOperationException("GameCatalog asset is missing.");
            if (catalog.vehicles == null || catalog.vehicles.Length == 0)
                throw new InvalidOperationException("GameCatalog has no vehicles.");

            bool anyChanged = false;
            for (int vehicleIndex = 0; vehicleIndex < catalog.vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = catalog.vehicles[vehicleIndex];
                if (vehicle == null) throw new InvalidOperationException(
                    $"GameCatalog vehicle slot {vehicleIndex} is null.");

                bool changed = EnsureVehicle(vehicle);
                if (!changed) continue;
                anyChanged = true;
                EditorUtility.SetDirty(vehicle);
            }

            if (anyChanged) AssetDatabase.SaveAssets();
            return anyChanged;
        }

        private static bool EnsureVehicle(VehicleDefinition vehicle)
        {
            if (vehicle.skins == null || vehicle.skins.Length == 0 || vehicle.skins[0]?.sprite == null)
                throw new InvalidOperationException($"{vehicle.vehicleId} has no default skin sprite.");
            if (vehicle.wheels == null || vehicle.wheels.Length == 0 || vehicle.wheels[0]?.sprite == null)
                throw new InvalidOperationException($"{vehicle.vehicleId} has no default wheel sprite.");

            bool changed = false;
            for (int phase = 0; phase < Thresholds.Length; phase++)
            {
                int threshold = Thresholds[phase];
                Sprite skinSprite = LoadSkinSprite(vehicle, threshold);
                Sprite wheelSprite = LoadSprite(
                    $"{ArtRoot}SharedWheels/SharedWheel_GlitchPhase_{threshold}.png");
                changed |= EnsureOption(ref vehicle.skins, $"glitch-phase-{threshold}",
                    Names[phase], threshold, skinSprite);
                changed |= EnsureOption(ref vehicle.wheels, $"glitch-wheel-{threshold}",
                    Names[phase], threshold, wheelSprite);
            }
            return changed;
        }

        private static Sprite LoadSkinSprite(VehicleDefinition vehicle, int threshold)
        {
            // The recovered Jeep has no dedicated Glitch PNG. Its six phases deliberately reuse
            // the canonical body sprite and receive their distinct animated treatment at runtime.
            if (vehicle.vehicleId == "car-01") return vehicle.skins[0].sprite;
            if (!SkinFolders.TryGetValue(vehicle.vehicleId, out string relativeStem))
                throw new InvalidOperationException(
                    $"No Glitch Phase artwork mapping exists for {vehicle.vehicleId}.");
            return LoadSprite($"{ArtRoot}{relativeStem}_GlitchPhase_{threshold}.png");
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException(
                $"Required Glitch Phase sprite is missing or not imported as Sprite: {path}");
            return sprite;
        }

        private static bool EnsureOption(ref VehicleDefinition.CustomizationOption[] options,
            string optionId, string displayName, float requiredDistance, Sprite sprite)
        {
            var list = options != null
                ? new List<VehicleDefinition.CustomizationOption>(options)
                : new List<VehicleDefinition.CustomizationOption>();
            VehicleDefinition.CustomizationOption option = null;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && string.Equals(list[i].optionId, optionId,
                        StringComparison.Ordinal))
                {
                    option = list[i];
                    break;
                }

            bool changed = false;
            if (option == null)
            {
                option = new VehicleDefinition.CustomizationOption();
                list.Add(option);
                options = list.ToArray();
                changed = true;
            }

            changed |= Set(ref option.optionId, optionId);
            changed |= Set(ref option.displayName, displayName);
            if (option.sprite != sprite) { option.sprite = sprite; changed = true; }
            if (option.unlockedByDefault) { option.unlockedByDefault = false; changed = true; }
            if (!Mathf.Approximately(option.requiredBestDistance, requiredDistance))
            {
                option.requiredBestDistance = requiredDistance;
                changed = true;
            }
            if (option.gemPrice != 0) { option.gemPrice = 0; changed = true; }
            if (option.purchaseCurrency != CustomizationPurchaseCurrency.Gems)
            {
                option.purchaseCurrency = CustomizationPurchaseCurrency.Gems;
                changed = true;
            }
            changed |= Set(ref option.googlePlayProductId, string.Empty);
            if (option.bundleType != BundleType.Cosmetic)
            {
                option.bundleType = BundleType.Cosmetic;
                changed = true;
            }
            if (option.allowsBlueprintProgression)
            {
                option.allowsBlueprintProgression = false;
                changed = true;
            }
            if (!option.hasExplicitRarity) { option.hasExplicitRarity = true; changed = true; }
            if (option.rarity != ItemRarity.Mythic)
            {
                option.rarity = ItemRarity.Mythic;
                changed = true;
            }
            return changed;
        }

        private static bool Set(ref string destination, string value)
        {
            if (string.Equals(destination, value, StringComparison.Ordinal)) return false;
            destination = value;
            return true;
        }
    }
}
#endif
