#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    /// <summary>
    /// Keeps the supplied Hill Adventurer menu artwork on a single, Android-safe
    /// import profile. The source PNG files live outside the project; only their
    /// organized copies below Assets/Art are processed here.
    /// </summary>
    public sealed class HillAdventurerUiAssetImporter : AssetPostprocessor
    {
        private const string ButtonsRoot = "Assets/Art/Buttons/MainNavigation/";
        private const string MenuBackgroundsRoot = "Assets/Art/Backgrounds/Menu/";
        private const string GarageBackgroundsRoot = "Assets/Art/Backgrounds/Garage/";
        private const string AdventureInterfaceRoot = "Assets/Art/Interface/Adventure/";
        private const string CommonInterfaceRoot = "Assets/Art/Interface/Common/";
        private const string GarageInterfaceRoot = "Assets/Art/Interface/Garage/";
        private const string HudRoot = "Assets/Art/Game/HUD/";
        private const string UpgradeStatesPath =
            "Assets/Art/Interface/Garage/Tunes/UpgradeTuneStates.png";
        private const string ChestSlotsPath =
            "Assets/Art/Interface/Adventure/ChestSlots.png";

        private static readonly string[] RequiredArtwork =
        {
            "Assets/Art/Buttons/MainNavigation/AdventureButton.png",
            "Assets/Art/Buttons/MainNavigation/GarageButton.png",
            "Assets/Art/Buttons/MainNavigation/StoreButton.png",
            "Assets/Art/Backgrounds/Menu/Default_Background.png",
            "Assets/Art/Backgrounds/Menu/Neon_Background.png",
            "Assets/Art/Backgrounds/Garage/Default_Garage.png",
            "Assets/Art/Backgrounds/Garage/Anime_Garage_Background.png",
            "Assets/Art/Interface/Common/SettingsIcon.png",
            "Assets/Art/Interface/Adventure/ChestSlots.png",
            "Assets/Art/Interface/Garage/SkinsIcon.png",
            "Assets/Art/Interface/Garage/Tunes/AccelerationTune.png",
            "Assets/Art/Interface/Garage/Tunes/DownforceTune.png",
            "Assets/Art/Interface/Garage/Tunes/FuelTune.png",
            "Assets/Art/Interface/Garage/Tunes/GripTune.png",
            "Assets/Art/Interface/Garage/Tunes/InclinationBuggyTune.png",
            "Assets/Art/Interface/Garage/Tunes/MotorTune.png",
            "Assets/Art/Interface/Garage/Tunes/SuspensionTune.png",
            "Assets/Art/Interface/Garage/Tunes/ThrustersTune.png",
            UpgradeStatesPath,
            "Assets/Art/Game/HUD/LowFuel.png"
        };

        private void OnPreprocessTexture()
        {
            if (!IsManagedArtwork(assetPath))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = assetPath == UpgradeStatesPath || assetPath == ChestSlotsPath
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;

            if (assetPath == UpgradeStatesPath)
                ConfigureUpgradeStateSlices(importer);
            else if (assetPath == ChestSlotsPath)
                ConfigureChestSlotSlice(importer);
        }

        private static bool IsManagedArtwork(string path)
        {
            return path.StartsWith(ButtonsRoot, StringComparison.Ordinal) ||
                   path.StartsWith(MenuBackgroundsRoot, StringComparison.Ordinal) ||
                   path.StartsWith(GarageBackgroundsRoot, StringComparison.Ordinal) ||
                   path.StartsWith(AdventureInterfaceRoot, StringComparison.Ordinal) ||
                   path.StartsWith(CommonInterfaceRoot, StringComparison.Ordinal) ||
                   path.StartsWith(GarageInterfaceRoot, StringComparison.Ordinal) ||
                   path.StartsWith(HudRoot, StringComparison.Ordinal);
        }

        private static void ConfigureUpgradeStateSlices(TextureImporter importer)
        {
#pragma warning disable 618
            importer.spritesheet = new[]
            {
                new SpriteMetaData
                {
                    name = "UpgradeAffordable",
                    rect = new Rect(0f, 0f, 768f, 1024f),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                },
                new SpriteMetaData
                {
                    name = "UpgradeUnaffordable",
                    rect = new Rect(768f, 0f, 768f, 1024f),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                }
            };
#pragma warning restore 618
        }

        private static void ConfigureChestSlotSlice(TextureImporter importer)
        {
#pragma warning disable 618
            // Reuse the supplied upper frame four times so the UI has four honest visual slots
            // instead of placing four hit targets over the source sheet's three-frame composition.
            importer.spritesheet = new[]
            {
                new SpriteMetaData
                {
                    name = "ChestSlotFrame",
                    // Unity sprite rectangles start at the texture's bottom edge. The supplied
                    // three-slot composition overlaps its rows, so crop the largest closed upper
                    // frame that excludes the lower row's decorative bars.
                    rect = new Rect(380f, 505f, 1015f, 382f),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                }
            };
#pragma warning restore 618
        }

        [MenuItem("Hill Adventurer/UI/Reimport Supplied Artwork")]
        public static void ReimportSuppliedArtwork()
        {
            ValidateRequiredArtwork();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in RequiredArtwork)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Hill Adventurer UI: imported {RequiredArtwork.Length} supplied PNG files.");
        }

        public static void ValidateRequiredArtwork()
        {
            foreach (string path in RequiredArtwork)
            {
                if (!System.IO.File.Exists(path))
                    throw new InvalidOperationException($"Required supplied artwork is missing: {path}");
            }
        }
    }
}
#endif
