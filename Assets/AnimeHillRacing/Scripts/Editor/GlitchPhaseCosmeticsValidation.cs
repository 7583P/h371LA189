using System;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class GlitchPhaseCosmeticsValidation
    {
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

        private static readonly HashSet<string> SkinVehicleIds = new HashSet<string>
        {
            "hill-adventurer-01", "buggy-01", "aerial-satelite-01",
            "rally-01", "sport-bike-01", "formula-01", "car-01"
        };

        [MenuItem("Anime Hill Racing/Validate Glitch Phase Cosmetics")]
        public static void ValidateMenu()
        {
            ValidateOrThrow();
            Debug.Log("[Glitch Phase] 42 vehicle skins, 42 shared-wheel assignments and the animated " +
                      "Crystal Tide shader are valid.");
        }

        public static void ValidateOrThrow()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(
                "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset");
            Require(catalog != null, "GameCatalog asset is missing.");
            Require(catalog.vehicles != null && catalog.vehicles.Length == 7,
                "Glitch Phase expects the seven-vehicle catalog.");

            var canonicalWheels = new Sprite[Thresholds.Length];
            int skinAssignments = 0;
            int wheelAssignments = 0;
            for (int vehicleIndex = 0; vehicleIndex < catalog.vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = catalog.vehicles[vehicleIndex];
                Require(vehicle != null, $"Vehicle slot {vehicleIndex} is null.");
                bool expectsSkins = SkinVehicleIds.Contains(vehicle.vehicleId);
                for (int phase = 0; phase < Thresholds.Length; phase++)
                {
                    int threshold = Thresholds[phase];
                    if (expectsSkins)
                    {
                        VehicleDefinition.CustomizationOption skin =
                            vehicle.FindSkin($"glitch-phase-{threshold}");
                        ValidateOption(skin, threshold, Names[phase], vehicle.vehicleId, "skin");
                        ValidateSkinGeometry(vehicle, skin, threshold);
                        skinAssignments++;
                    }

                    VehicleDefinition.CustomizationOption wheel =
                        vehicle.FindWheel($"glitch-wheel-{threshold}");
                    ValidateOption(wheel, threshold, Names[phase], vehicle.vehicleId, "wheel");
                    ValidateWheelGeometry(vehicle, wheel, threshold);
                    if (canonicalWheels[phase] == null) canonicalWheels[phase] = wheel.sprite;
                    Require(canonicalWheels[phase] == wheel.sprite,
                        $"{vehicle.vehicleId} does not use the shared {threshold:N0} m wheel sprite.");
                    wheelAssignments++;
                }
            }

            Require(skinAssignments == 42, $"Expected 42 skin assignments, found {skinAssignments}.");
            Require(wheelAssignments == 42, $"Expected 42 wheel assignments, found {wheelAssignments}.");
            Shader crystalTide = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/AnimeHillRacing/Resources/GlitchCrystalTide.shader");
            Require(crystalTide != null && crystalTide.name == "AnimeHillRacing/GlitchCrystalTide",
                "The animated Glitch Crystal Tide shader is missing.");
        }

        private static void ValidateSkinGeometry(VehicleDefinition vehicle,
            VehicleDefinition.CustomizationOption skin, int threshold)
        {
            VehicleDefinition.CustomizationOption reference =
                vehicle.skins != null && vehicle.skins.Length > 0 ? vehicle.skins[0] : null;
            Require(reference?.sprite != null && skin.sprite != null,
                $"{vehicle.vehicleId} Glitch Phase has no default sprite canvas.");

            Vector3 referenceScale = vehicle.BodyCustomizationScale(reference);
            Vector3 selectedScale = vehicle.BodyCustomizationScale(skin);
            Vector2 referenceBox = Vector2.Scale(reference.sprite.bounds.size,
                new Vector2(referenceScale.x, referenceScale.y));
            Vector2 selectedBox = Vector2.Scale(skin.sprite.bounds.size,
                new Vector2(selectedScale.x, selectedScale.y));
            Require(IsFinitePositive(selectedBox),
                $"{vehicle.vehicleId} Glitch Phase {threshold:N0} m has an invalid manual body size.");
            if (!skin.overrideGlitchTransform)
                Require((referenceBox - selectedBox).sqrMagnitude <= .000004f,
                    $"{vehicle.vehicleId} Glitch Phase {threshold:N0} m does not match the default body size.");

            Vector3 referencePosition = vehicle.BodyCustomizationOffset(reference);
            Vector3 selectedPosition = vehicle.BodyCustomizationOffset(skin);
            Require(IsFinite(selectedPosition.x) && IsFinite(selectedPosition.y),
                $"{vehicle.vehicleId} Glitch Phase {threshold:N0} m has an invalid manual body offset.");
            if (!skin.overrideGlitchTransform)
                Require((referencePosition - selectedPosition).sqrMagnitude <= .000001f,
                    $"{vehicle.vehicleId} Glitch Phase {threshold:N0} m does not use the default body height.");
        }

        private static bool IsFinitePositive(Vector2 value) =>
            IsFinite(value.x) && IsFinite(value.y) && value.x > .001f && value.y > .001f;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void ValidateWheelGeometry(VehicleDefinition vehicle,
            VehicleDefinition.CustomizationOption wheel, int threshold)
        {
            VehicleDefinition.CustomizationOption reference =
                vehicle.wheels != null && vehicle.wheels.Length > 0 ? vehicle.wheels[0] : null;
            Require(reference?.sprite != null && wheel.sprite != null,
                $"{vehicle.vehicleId} Glitch Phase has no default wheel canvas.");

            ValidateWheelHeight(vehicle, reference, wheel, vehicle.RearWheelDiameter,
                threshold, "rear", false);
            ValidateWheelHeight(vehicle, reference, wheel, vehicle.FrontWheelDiameter,
                threshold, "front", true);
        }

        private static void ValidateWheelHeight(VehicleDefinition vehicle,
            VehicleDefinition.CustomizationOption reference,
            VehicleDefinition.CustomizationOption wheel, float diameter, int threshold, string axle,
            bool front)
        {
            float referenceHeight = reference.sprite.bounds.size.y *
                                    vehicle.WheelCustomizationScale(reference, diameter, front);
            float selectedHeight = wheel.sprite.bounds.size.y *
                                   vehicle.WheelCustomizationScale(wheel, diameter, front);
            Require(IsFinite(selectedHeight) && selectedHeight > .001f,
                $"{vehicle.vehicleId} Glitch Phase {threshold:N0} m {axle} wheel has an invalid size.");
            if (!wheel.overrideGlitchWheelTransform)
                Require(Mathf.Abs(referenceHeight - selectedHeight) <= .00001f,
                    $"{vehicle.vehicleId} Glitch Phase {threshold:N0} m {axle} wheel " +
                    "does not match the default wheel height.");
        }

        private static void ValidateOption(VehicleDefinition.CustomizationOption option,
            int threshold, string expectedName, string vehicleId, string kind)
        {
            Require(option != null, $"{vehicleId} is missing Glitch Phase {kind} {threshold:N0} m.");
            Require(option.sprite != null,
                $"{vehicleId} Glitch Phase {kind} {threshold:N0} m has no sprite.");
            Require(option.displayName == expectedName,
                $"{vehicleId} Glitch Phase {kind} {threshold:N0} m has the wrong name.");
            Require(Mathf.Approximately(option.requiredBestDistance, threshold),
                $"{vehicleId} Glitch Phase {kind} has the wrong distance requirement.");
            Require(!option.unlockedByDefault && option.gemPrice == 0 &&
                    !option.allowsBlueprintProgression,
                $"{vehicleId} Glitch Phase {kind} must be distance-only.");
            Require(option.hasExplicitRarity && option.rarity == ItemRarity.Mythic,
                $"{vehicleId} Glitch Phase {kind} must be Mythic.");
            Require(kind == "skin"
                    ? GlitchCrystalTideEffect.IsGlitchSkin(option)
                    : GlitchCrystalTideEffect.IsGlitchWheel(option),
                $"{vehicleId} Glitch Phase {kind} is not recognized by the Crystal Tide effect.");

            string path = AssetDatabase.GetAssetPath(option.sprite);
            bool proceduralJeep = vehicleId == "car-01" && kind == "skin" &&
                                   path == GlitchPhaseSpriteImportGuard.JeepSourcePath;
            Require(proceduralJeep ||
                    path.StartsWith(GlitchPhaseSpriteImportGuard.GlitchArtRoot,
                        StringComparison.Ordinal),
                $"{vehicleId} Glitch Phase {kind} points outside the Glitch Phase art folder.");
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Sprite &&
                    importer.alphaIsTransparency && UsesFullRect(importer),
                $"{path} is not configured as a transparent sprite.");
        }

        private static bool UsesFullRect(TextureImporter importer)
        {
            if (importer == null) return false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType == SpriteMeshType.FullRect;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[Glitch Phase] " + message);
        }
    }

    [InitializeOnLoad]
    public static class GlitchPhaseSpriteImportGuard
    {
        public const string GlitchArtRoot = "Assets/Art/Vehicles/Glitch Phase/";
        public const string JeepSourcePath = "Assets/Art/Vehicles/Jeep/Skins/Jeep_Skin_Default.png";
        private static bool running;

        static GlitchPhaseSpriteImportGuard() =>
            EditorApplication.delayCall += EnsureFullRectImport;

        [MenuItem("Anime Hill Racing/Vehicles/Fix Glitch Phase Sprite Cropping")]
        public static void EnsureFullRectImport()
        {
            if (running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            running = true;
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D",
                    new[] { GlitchArtRoot.TrimEnd('/') });
                var paths = new List<string>(guids.Length + 1) { JeepSourcePath };
                for (int i = 0; i < guids.Length; i++)
                    paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));

                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    if (settings.spriteMeshType != SpriteMeshType.FullRect)
                    {
                        settings.spriteMeshType = SpriteMeshType.FullRect;
                        importer.SetTextureSettings(settings);
                        changed = true;
                    }
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (changed) importer.SaveAndReimport();
                }
            }
            finally
            {
                running = false;
            }
        }
    }
}
