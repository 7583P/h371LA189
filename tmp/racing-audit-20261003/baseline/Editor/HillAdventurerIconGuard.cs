#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    /// <summary>Keeps the Android launcher artwork tied to the Play Store icon file.</summary>
    public sealed class HillAdventurerIconGuard : IPreprocessBuildWithReport
    {
        private const string IconPath = "Assets/Branding/HillAdventurerIcon.png";
        private const string PackageName = "com.matias.hilladventurer";

        public int callbackOrder => 0;

        [MenuItem("Anime Hill Racing/Publishing/Apply Google Play Icon")]
        public static void Apply()
        {
            Texture2D artwork = LoadArtwork();
            SetIcons(AndroidPlatformIconKind.Adaptive, artwork);
#pragma warning disable CS0618 // Unity still builds round and legacy slots for older Android devices.
            SetIcons(AndroidPlatformIconKind.Round, artwork);
            SetIcons(AndroidPlatformIconKind.Legacy, artwork);
#pragma warning restore CS0618
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("Hill Adventurer Android icons now use " + IconPath);
        }

        [MenuItem("Anime Hill Racing/Publishing/Verify Google Play Icon")]
        public static void Verify()
        {
            Texture2D artwork = LoadArtwork();
            if (PlayerSettings.productName != "Hill Adventurer")
                throw new InvalidOperationException("The installed app name must match the Play listing: Hill Adventurer.");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != PackageName)
                throw new InvalidOperationException("Unexpected Android application ID.");

            VerifyIcons(AndroidPlatformIconKind.Adaptive, artwork);
#pragma warning disable CS0618 // Check every launcher variant included in the Android build.
            VerifyIcons(AndroidPlatformIconKind.Round, artwork);
            VerifyIcons(AndroidPlatformIconKind.Legacy, artwork);
#pragma warning restore CS0618
            Debug.Log("HILL_ADVENTURER_PLAY_ICON_OK");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
                Verify();
        }

        private static Texture2D LoadArtwork()
        {
            Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (artwork == null || artwork.width != 512 || artwork.height != 512)
                throw new InvalidOperationException("Play Store icon must be a 512 x 512 PNG at " + IconPath);

            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, IconPath);
            if (new FileInfo(path).Length > 1024 * 1024)
                throw new InvalidOperationException("Play Store icon exceeds 1 MB: " + IconPath);
            return artwork;
        }

        private static void SetIcons(PlatformIconKind kind, Texture2D artwork)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (PlatformIcon icon in icons)
            {
                var layers = new Texture2D[icon.maxLayerCount];
                for (int layer = 0; layer < layers.Length; layer++)
                    layers[layer] = artwork;
                icon.SetTextures(layers);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        private static void VerifyIcons(PlatformIconKind kind, Texture2D artwork)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            if (icons == null || icons.Length == 0)
                throw new InvalidOperationException("Android icon slots are missing for " + kind);

            foreach (PlatformIcon icon in icons)
            {
                Texture2D[] layers = icon.GetTextures();
                if (layers == null || layers.Length != icon.maxLayerCount)
                    throw new InvalidOperationException("Android icon layers are missing for " + kind);
                foreach (Texture2D layer in layers)
                    if (layer != artwork)
                        throw new InvalidOperationException("Android launcher icon differs from " + IconPath);
            }
        }
    }
}
#endif
