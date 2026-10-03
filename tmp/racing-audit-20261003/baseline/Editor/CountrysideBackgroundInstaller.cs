using System;
using System.IO;
using AnimeHillRacing.CameraSystem;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class CountrysideBackgroundInstaller
    {
        private const string SessionKey = "AHR.CountrysideBackgroundInstaller.v1";
        private const string ArtRoot = "Assets/Art/Backgrounds/Countryside";
        private const string MountainsPath = ArtRoot + "/Countryside_Mountains.png";
        private const string SunPath = ArtRoot + "/Countryside_Sun.png";
        private const string SkyPath = ArtRoot + "/Countryside_Sky.png";
        private const string EnvironmentPath = "Assets/AnimeHillRacing/MapProfiles/CountrysideEnvironment.asset";

        [InitializeOnLoadMethod]
        private static void QueueInstall()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Anime Hill Racing/Content/Install Countryside Background & Sun")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(SessionKey, false);
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureSprite(MountainsPath, 2048, TextureWrapMode.Clamp, true);
            ConfigureSprite(SunPath, 2048, TextureWrapMode.Clamp, true);
            ConfigureSprite(SkyPath, 2048, TextureWrapMode.Clamp, true);

            Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>(SkyPath);
            Sprite mountains = AssetDatabase.LoadAssetAtPath<Sprite>(MountainsPath);
            Sprite sun = AssetDatabase.LoadAssetAtPath<Sprite>(SunPath);
            EnvironmentDefinition environment = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(EnvironmentPath);

            if (mountains == null || environment == null)
            {
                Debug.LogWarning("[CountrysideBackgroundInstaller] Assets not yet ready for installation.");
                return;
            }

            environment.environmentId = "countryside";
            if (sky != null) environment.sky = sky;
            environment.mountains = mountains;
            environment.sun = null;
            environment.sunCameraOffset = new Vector2(-3f, 3.8f);
            environment.sunVisualWidth = 3.5f;
            environment.sunRelativeParallax = 0.003f;
            environment.mountainVisualHeight = 19f;
            environment.mountainBaseY = 4f;
            environment.mountainParallaxX = 0.18f;
            environment.mountainParallaxY = 0.04f;
            environment.lockVerticalToCamera = true;
            environment.mirrorAlternatingMountainTiles = true;
            environment.pooledMountainSegments = 5;
            environment.seamOverlap = 0.004f;

            EditorUtility.SetDirty(environment);
            AssetDatabase.SaveAssets();

            Debug.Log($"COUNTRYSIDE_BACKGROUND_INSTALL_OK|mountains={MountainsPath}|sun={SunPath}|sunWidth={environment.sunVisualWidth:0.0}|offset={environment.sunCameraOffset.x:0.0},{environment.sunCameraOffset.y:0.0}");
        }

        private static void ConfigureSprite(string path, int maxSize, TextureWrapMode wrapMode, bool alphaIsTransparency)
        {
            if (!File.Exists(path)) return;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.alphaIsTransparency != alphaIsTransparency) { importer.alphaIsTransparency = alphaIsTransparency; dirty = true; }
            if (importer.wrapMode != wrapMode) { importer.wrapMode = wrapMode; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.maxTextureSize != maxSize) { importer.maxTextureSize = maxSize; dirty = true; }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
