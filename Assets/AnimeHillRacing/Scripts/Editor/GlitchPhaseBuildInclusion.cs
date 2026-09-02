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
            GlitchPhaseCosmeticsValidation.ValidateOrThrow();
            EnsureRegistry();
        }

        [MenuItem("Anime Hill Racing/Vehicles/Rebuild Glitch Phase APK Registry")]
        public static void RebuildMenu()
        {
            GlitchPhaseCosmeticsValidation.ValidateOrThrow();
            EnsureRegistry();
            Debug.Log("[Glitch Phase] Android inclusion registry rebuilt successfully.");
        }

        private static void EnsureRegistryIfSafe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            try
            {
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
}
#endif
