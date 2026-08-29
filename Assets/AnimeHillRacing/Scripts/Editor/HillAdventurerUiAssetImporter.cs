#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.UI;
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
        private const string EconomyArtworkRoot = "Assets/Art/Interface/HillAdventurer/";
        private const string HudRoot = "Assets/Art/Game/HUD/";
        private const string UpgradeStatesPath =
            "Assets/Art/Interface/Garage/Tunes/UpgradeTuneStates.png";
        private const string ChestSlotsPath =
            "Assets/Art/Interface/Adventure/ChestSlots.png";
        private const string RuntimeCatalogPath =
            "Assets/AnimeHillRacing/Resources/HillAdventurerUiAssets.asset";

        private static readonly string[] EconomyArtworkNames =
        {
            "Background_Chest_Payments",
            "Background_Items",
            "Buy_Starter_Pack",
            "Coin_To_Replace_Current",
            "Coins",
            "Equipped",
            "Gem",
            "Gems",
            "Leaderboard",
            "Owned",
            "Ready",
            "Starter_Pack",
            "You_Got"
        };

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
            string artworkName = Path.GetFileNameWithoutExtension(assetPath);
            bool isEconomyArtwork = IsEconomyArtwork(artworkName);
            if (!IsManagedArtwork(assetPath) && !isEconomyArtwork)
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = assetPath == UpgradeStatesPath || assetPath == ChestSlotsPath ||
                                        artworkName == "Background_Items"
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
            importer.maxTextureSize = isEconomyArtwork ? 4096 : 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;

            if (assetPath == UpgradeStatesPath)
                ConfigureUpgradeStateSlices(importer);
            else if (assetPath == ChestSlotsPath)
                ConfigureChestSlotSlice(importer);
            else if (artworkName == "Background_Items")
                ConfigureRaritySlices(importer);
        }

        private static bool IsEconomyArtwork(string artworkName)
        {
            for (int i = 0; i < EconomyArtworkNames.Length; i++)
                if (string.Equals(EconomyArtworkNames[i], artworkName,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
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

        private static void ConfigureRaritySlices(TextureImporter importer)
        {
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width <= 0 || height <= 0) return;

            int columns;
            int rows;
            float aspect = width / (float)height;
            if (aspect >= 4f)
            {
                columns = 6;
                rows = 1;
            }
            else if (aspect <= .25f)
            {
                columns = 1;
                rows = 6;
            }
            else if (width >= height)
            {
                columns = 3;
                rows = 2;
            }
            else
            {
                columns = 2;
                rows = 3;
            }

            string[] names = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            float cellWidth = width / (float)columns;
            float cellHeight = height / (float)rows;
            var slices = new SpriteMetaData[names.Length];
            for (int index = 0; index < slices.Length; index++)
            {
                int column = index % columns;
                int visualRow = index / columns;
                slices[index] = new SpriteMetaData
                {
                    name = names[index],
                    // Sprite coordinates start at the lower-left; source order is read top-left.
                    rect = new Rect(column * cellWidth,
                        height - ((visualRow + 1) * cellHeight), cellWidth, cellHeight),
                    pivot = new Vector2(.5f, .5f),
                    alignment = (int)SpriteAlignment.Center
                };
            }

#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!ContainsEconomyArtwork(importedAssets) && !ContainsEconomyArtwork(movedAssets) &&
                !ContainsEconomyArtwork(deletedAssets))
                return;
            EditorApplication.delayCall -= SyncEconomyArtworkQuietly;
            EditorApplication.delayCall += SyncEconomyArtworkQuietly;
        }

        private static bool ContainsEconomyArtwork(string[] paths)
        {
            if (paths == null) return false;
            for (int i = 0; i < paths.Length; i++)
                if (IsEconomyArtwork(Path.GetFileNameWithoutExtension(paths[i]))) return true;
            return false;
        }

        [InitializeOnLoadMethod]
        private static void ScheduleInitialEconomyArtworkSync()
        {
            EditorApplication.delayCall -= SyncEconomyArtworkQuietly;
            EditorApplication.delayCall += SyncEconomyArtworkQuietly;
        }

        [MenuItem("Hill Adventurer/UI/Sync Economy and Reward Artwork")]
        public static void SyncEconomyArtwork()
        {
            int linked = SyncEconomyArtworkInternal();
            int missing = EconomyArtworkNames.Length - linked;
            Debug.Log($"Hill Adventurer UI: linked {linked}/{EconomyArtworkNames.Length} supplied " +
                      $"art assets. {missing} asset(s) are not imported yet.");
        }

        [MenuItem("Hill Adventurer/UI/Validate Economy and Reward Artwork")]
        public static void ValidateEconomyArtwork()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int linked = SyncEconomyArtworkInternal();
            if (linked != EconomyArtworkNames.Length)
                throw new InvalidOperationException(
                    $"Economy artwork validation linked {linked}/{EconomyArtworkNames.Length} sources.");

            HillAdventurerUiCatalog catalog =
                AssetDatabase.LoadAssetAtPath<HillAdventurerUiCatalog>(RuntimeCatalogPath);
            if (catalog == null)
                throw new InvalidOperationException($"UI catalog is missing: {RuntimeCatalogPath}");

            var sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < EconomyArtworkNames.Length; i++)
            {
                string artworkName = EconomyArtworkNames[i];
                string path = EconomyArtworkRoot + artworkName + ".png";
                if (!File.Exists(path))
                    throw new InvalidOperationException($"Economy artwork is missing: {path}");
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite ||
                    importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp)
                    throw new InvalidOperationException($"Economy artwork import settings are invalid: {path}");
                if (artworkName == "Background_Items" &&
                    importer.spriteImportMode != SpriteImportMode.Multiple)
                    throw new InvalidOperationException("Background_Items must use Multiple sprite mode.");
                if (artworkName != "Background_Items" &&
                    importer.spriteImportMode != SpriteImportMode.Single)
                    throw new InvalidOperationException($"{artworkName} must use Single sprite mode.");

                UnityEngine.Object[] imported = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int assetIndex = 0; assetIndex < imported.Length; assetIndex++)
                    if (imported[assetIndex] is Sprite sprite)
                        sprites[sprite.name] = sprite;
                Sprite main = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (main != null) sprites[artworkName] = main;
            }

            RequireCatalogSprite(catalog.coinCounterIcon, sprites, "Coin_To_Replace_Current");
            RequireCatalogSprite(catalog.coinsRewardIcon, sprites, "Coins");
            RequireCatalogSprite(catalog.gemCounterIcon, sprites, "Gem");
            RequireCatalogSprite(catalog.gemsRewardIcon, sprites, "Gems");
            RequireCatalogSprite(catalog.ownedBadge, sprites, "Owned");
            RequireCatalogSprite(catalog.equippedBadge, sprites, "Equipped");
            RequireCatalogSprite(catalog.readyButton, sprites, "Ready");
            RequireCatalogSprite(catalog.starterPackArtwork, sprites, "Starter_Pack");
            RequireCatalogSprite(catalog.buyStarterPackButton, sprites, "Buy_Starter_Pack");
            RequireCatalogSprite(catalog.chestRewardBackground, sprites, "Background_Chest_Payments");
            RequireCatalogSprite(catalog.youGotHeader, sprites, "You_Got");
            RequireCatalogSprite(catalog.leaderboardButton, sprites, "Leaderboard");

            string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            if (catalog.rarityFrames == null || catalog.rarityFrames.Length != rarityNames.Length)
                throw new InvalidOperationException("Background_Items did not produce all six rarity frames.");
            for (int i = 0; i < rarityNames.Length; i++)
                RequireCatalogSprite(catalog.rarityFrames[i], sprites, rarityNames[i]);

            Debug.Log("HILL_ADVENTURER_ECONOMY_ARTWORK_PASS|sources=13/13|catalogRefs=18/18|" +
                      "rarities=Common,Uncommon,Rare,Epic,Legendary,Mythic");
        }

        private static void RequireCatalogSprite(Sprite actual, Dictionary<string, Sprite> sprites,
            string expectedName)
        {
            if (!sprites.TryGetValue(expectedName, out Sprite expected) || expected == null ||
                actual != expected)
                throw new InvalidOperationException(
                    $"UI catalog does not reference the imported '{expectedName}' sprite.");
        }

        private static void SyncEconomyArtworkQuietly() => SyncEconomyArtworkInternal();

        private static int SyncEconomyArtworkInternal()
        {
            HillAdventurerUiCatalog catalog =
                AssetDatabase.LoadAssetAtPath<HillAdventurerUiCatalog>(RuntimeCatalogPath);
            if (catalog == null) return 0;

            var sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            int linkedArtwork = 0;
            for (int i = 0; i < EconomyArtworkNames.Length; i++)
            {
                string artworkName = EconomyArtworkNames[i];
                string path = FindExactArtworkPath(artworkName);
                if (string.IsNullOrEmpty(path)) continue;
                linkedArtwork++;
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                    if (assets[j] is Sprite sprite)
                        sprites[sprite.name] = sprite;
                Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (mainSprite != null) sprites[artworkName] = mainSprite;
            }

            catalog.coinCounterIcon = GetSprite(sprites, "Coin_To_Replace_Current");
            catalog.coinsRewardIcon = GetSprite(sprites, "Coins");
            catalog.gemCounterIcon = GetSprite(sprites, "Gem");
            catalog.gemsRewardIcon = GetSprite(sprites, "Gems");
            catalog.ownedBadge = GetSprite(sprites, "Owned");
            catalog.equippedBadge = GetSprite(sprites, "Equipped");
            catalog.readyButton = GetSprite(sprites, "Ready");
            catalog.starterPackArtwork = GetSprite(sprites, "Starter_Pack");
            catalog.buyStarterPackButton = GetSprite(sprites, "Buy_Starter_Pack");
            catalog.chestRewardBackground = GetSprite(sprites, "Background_Chest_Payments");
            catalog.youGotHeader = GetSprite(sprites, "You_Got");
            catalog.leaderboardButton = GetSprite(sprites, "Leaderboard");

            if (catalog.rarityFrames == null || catalog.rarityFrames.Length != 6)
                Array.Resize(ref catalog.rarityFrames, 6);
            string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            for (int i = 0; i < rarityNames.Length; i++)
                catalog.rarityFrames[i] = GetSprite(sprites, rarityNames[i]);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return linkedArtwork;
        }

        private static Sprite GetSprite(Dictionary<string, Sprite> sprites, string name) =>
            sprites.TryGetValue(name, out Sprite sprite) ? sprite : null;

        private static string FindExactArtworkPath(string artworkName)
        {
            string canonicalPath = EconomyArtworkRoot + artworkName + ".png";
            if (File.Exists(canonicalPath)) return canonicalPath;
            string[] guids = AssetDatabase.FindAssets(artworkName + " t:Texture2D");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), artworkName,
                        StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
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
                for (int i = 0; i < EconomyArtworkNames.Length; i++)
                    AssetDatabase.ImportAsset(EconomyArtworkRoot + EconomyArtworkNames[i] + ".png",
                        ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            SyncEconomyArtworkInternal();
            Debug.Log($"Hill Adventurer UI: imported {RequiredArtwork.Length + EconomyArtworkNames.Length} " +
                      "supplied PNG files.");
        }

        public static void ValidateRequiredArtwork()
        {
            foreach (string path in RequiredArtwork)
            {
                if (!System.IO.File.Exists(path))
                    throw new InvalidOperationException($"Required supplied artwork is missing: {path}");
            }
            for (int i = 0; i < EconomyArtworkNames.Length; i++)
            {
                string path = EconomyArtworkRoot + EconomyArtworkNames[i] + ".png";
                if (!File.Exists(path))
                    throw new InvalidOperationException($"Required economy artwork is missing: {path}");
            }
        }
    }
}
#endif
