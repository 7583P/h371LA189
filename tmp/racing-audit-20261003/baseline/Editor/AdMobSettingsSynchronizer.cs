using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using AnimeHillRacing.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeHillRacing.Editor
{
    /// <summary>
    /// Keeps the plugin's manifest-time app IDs derived from the single game configuration asset.
    /// Ad-unit IDs stay runtime-only in AdMobConfiguration.
    /// </summary>
    [InitializeOnLoad]
    public sealed class AdMobSettingsSynchronizer : IPreprocessBuildWithReport
    {
        private const string ConfigurationResourceName = "AdMobConfiguration";
        private const string PluginSettingsTypeName =
            "GoogleMobileAds.Editor.GoogleMobileAdsSettings";
        private const string AndroidManifestAssetPath =
            "Assets/Plugins/Android/HillAdventurerPrivacy.androidlib/AndroidManifest.xml";
        private const string AdMobApplicationIdMetadata =
            "com.google.android.gms.ads.APPLICATION_ID";
        private static readonly XNamespace AndroidXmlNamespace =
            "http://schemas.android.com/apk/res/android";

        static AdMobSettingsSynchronizer()
        {
            EditorApplication.delayCall += SyncIfPluginIsAvailable;
        }

        public int callbackOrder => -1000;

        [MenuItem("Tools/Hill Adventurer/AdMob/Sync Google Mobile Ads Settings")]
        public static void SyncFromMenu()
        {
            if (TrySync(out string error, true))
                Debug.Log("[AdMob] Google Mobile Ads app IDs are synchronized");
            else
                Debug.LogError("[AdMob] " + error);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            AdMobConfiguration configuration =
                Resources.Load<AdMobConfiguration>(ConfigurationResourceName);
            if (configuration == null)
                throw new BuildFailedException("AdMobConfiguration is missing from Resources.");

            if (!ValidateBuildTarget(configuration, report.summary.platform, out string validationError))
                throw new BuildFailedException(validationError);
            if (!TrySync(out string syncError, true))
                throw new BuildFailedException(syncError);
        }

        private static void SyncIfPluginIsAvailable()
        {
            TrySync(out _, false);
        }

        private static bool TrySync(out string error, bool requirePlugin)
        {
            AdMobConfiguration configuration =
                Resources.Load<AdMobConfiguration>(ConfigurationResourceName);
            if (configuration == null)
            {
                error = "AdMobConfiguration is missing from Resources";
                return false;
            }

            Type settingsType = FindType(PluginSettingsTypeName);
            if (settingsType == null)
            {
                error = "Google Mobile Ads Unity Plugin is not installed or has not finished importing";
                return !requirePlugin;
            }

            MethodInfo loadInstance = settingsType.GetMethod("LoadInstance",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (loadInstance == null)
            {
                error = "Installed Google Mobile Ads settings do not expose LoadInstance";
                return false;
            }

            Object settings;
            try
            {
                settings = loadInstance.Invoke(null, null) as Object;
            }
            catch (Exception exception)
            {
                error = "Google Mobile Ads settings could not be loaded: " + exception.GetBaseException().Message;
                return false;
            }
            if (settings == null)
            {
                error = "Google Mobile Ads settings could not be loaded";
                return false;
            }

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty android = serializedSettings.FindProperty("adMobAndroidAppId");
            SerializedProperty ios = serializedSettings.FindProperty("adMobIOSAppId");
            if (android == null || ios == null)
            {
                error = "Installed Google Mobile Ads settings schema is incompatible";
                return false;
            }

            string androidId = configuration.AndroidAppId ?? string.Empty;
            string iosId = configuration.IosAppId ?? string.Empty;
            bool changed = android.stringValue != androidId || ios.stringValue != iosId;
            if (changed)
            {
                android.stringValue = androidId;
                ios.stringValue = iosId;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log(configuration.useTestAds
                    ? "[AdMob] Plugin settings synchronized to TEST app IDs"
                    : "[AdMob] Plugin settings synchronized to PRODUCTION app IDs");
            }

            if (!TrySyncAndroidManifest(configuration.AndroidAppId, out error)) return false;

            error = string.Empty;
            return true;
        }

        private static bool TrySyncAndroidManifest(string androidAppId, out string error)
        {
            string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                AndroidManifestAssetPath);
            if (!File.Exists(manifestPath))
            {
                error = "Hill Adventurer Android library manifest is missing";
                return false;
            }

            try
            {
                XDocument document = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
                XElement application = document.Root?.Element("application");
                if (application == null)
                {
                    application = new XElement("application");
                    document.Root?.Add(application);
                }
                if (application == null)
                {
                    error = "Hill Adventurer Android library manifest is invalid";
                    return false;
                }

                XElement appIdMetadata = null;
                foreach (XElement metadata in application.Elements("meta-data"))
                {
                    if ((string)metadata.Attribute(AndroidXmlNamespace + "name") ==
                        AdMobApplicationIdMetadata)
                    {
                        appIdMetadata = metadata;
                        break;
                    }
                }

                bool changed = false;
                if (appIdMetadata == null)
                {
                    appIdMetadata = new XElement("meta-data",
                        new XAttribute(AndroidXmlNamespace + "name", AdMobApplicationIdMetadata),
                        new XAttribute(AndroidXmlNamespace + "value", androidAppId ?? string.Empty));
                    application.Add(appIdMetadata);
                    changed = true;
                }
                else if ((string)appIdMetadata.Attribute(AndroidXmlNamespace + "value") != androidAppId)
                {
                    appIdMetadata.SetAttributeValue(AndroidXmlNamespace + "value",
                        androidAppId ?? string.Empty);
                    changed = true;
                }

                if (changed)
                {
                    document.Save(manifestPath);
                    AssetDatabase.ImportAsset(AndroidManifestAssetPath,
                        ImportAssetOptions.ForceSynchronousImport);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not synchronize the Android AdMob app ID: " + exception.Message;
                return false;
            }
        }

        private static bool ValidateBuildTarget(AdMobConfiguration configuration,
            BuildTarget target, out string error)
        {
            if (configuration.useTestAds)
            {
                error = string.Empty;
                return true;
            }

            bool ios = target == BuildTarget.iOS;
            bool android = target == BuildTarget.Android;
            if (!ios && !android)
            {
                error = string.Empty;
                return true;
            }

            string platform = ios ? "iOS" : "Android";
            string appId = ios ? configuration.iosAppId : configuration.androidAppId;
            string rewarded = ios ? configuration.iosRewardedAdUnitId : configuration.androidRewardedAdUnitId;
            string interstitial = ios ? configuration.iosInterstitialAdUnitId : configuration.androidInterstitialAdUnitId;
            string banner = ios ? configuration.iosBannerAdUnitId : configuration.androidBannerAdUnitId;
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(rewarded) ||
                string.IsNullOrWhiteSpace(interstitial) || string.IsNullOrWhiteSpace(banner))
            {
                error = $"Production ads are selected, but one or more {platform} AdMob IDs are empty.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }
    }
}
