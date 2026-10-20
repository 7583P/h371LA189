#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Ads;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    // Read-only release configuration evidence. It never changes settings, builds,
    // signs, uploads or reads keystore passwords.
    public static class RacingAndroidReleaseAudit
    {
        [Serializable] sealed class Evidence
        {
            public string applicationId, versionName, scriptingBackend, architectures, il2cppConfiguration, targetSdk, minSdk, strippingLevel;
            public int versionCode;
            public bool arm64, il2cpp, developmentBuildFlag, scriptDebuggingFlag, connectProfilerFlag, buildAppBundle, customKeystore, keystoreFilePresent, keyAliasSet;
            public bool adsUseTestFlag, adAppIdIsGoogleSample, adUnitsConfigured, releaseUsesProductionUnits;
            public string adMobAndroidAppId, enabledScenes;
        }

        public static void Run()
        {
            string output = RacingFrameForensics.Argument("--closure-output");
            Directory.CreateDirectory(output);
            var android = NamedBuildTarget.Android;
            var e = new Evidence
            {
                applicationId = PlayerSettings.GetApplicationIdentifier(android),
                versionName = PlayerSettings.bundleVersion,
                versionCode = PlayerSettings.Android.bundleVersionCode,
                scriptingBackend = PlayerSettings.GetScriptingBackend(android).ToString(),
                architectures = PlayerSettings.Android.targetArchitectures.ToString(),
                il2cppConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(android).ToString(),
                targetSdk = PlayerSettings.Android.targetSdkVersion.ToString(),
                minSdk = PlayerSettings.Android.minSdkVersion.ToString(),
                strippingLevel = PlayerSettings.GetManagedStrippingLevel(android).ToString(),
                developmentBuildFlag = EditorUserBuildSettings.development,
                scriptDebuggingFlag = EditorUserBuildSettings.allowDebugging,
                connectProfilerFlag = EditorUserBuildSettings.connectProfiler,
                buildAppBundle = EditorUserBuildSettings.buildAppBundle,
                customKeystore = PlayerSettings.Android.useCustomKeystore,
                keyAliasSet = !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName),
            };
            e.arm64 = (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
            e.il2cpp = PlayerSettings.GetScriptingBackend(android) == ScriptingImplementation.IL2CPP;
            string keystore = PlayerSettings.Android.keystoreName ?? "";
            e.keystoreFilePresent = !string.IsNullOrEmpty(keystore) && File.Exists(Path.GetFullPath(keystore));
            var ads = Resources.Load<AdMobConfiguration>("AdMobConfiguration");
            if (ads != null)
            {
                e.adsUseTestFlag = ads.useTestAds;
                e.adMobAndroidAppId = ads.AndroidAppId;
                e.adAppIdIsGoogleSample = ads.AndroidAppId == AdMobConfiguration.AndroidTestAppId;
                e.adUnitsConfigured = !string.IsNullOrWhiteSpace(ads.androidRewardedAdUnitId) && !string.IsNullOrWhiteSpace(ads.androidInterstitialAdUnitId) &&
                    !string.IsNullOrWhiteSpace(ads.androidBannerAdUnitId);
                e.releaseUsesProductionUnits = !ads.useTestAds && e.adUnitsConfigured && !ads.androidRewardedAdUnitId.Contains("3940256099942544");
            }
            foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled) e.enabledScenes += Path.GetFileNameWithoutExtension(scene.path) + ";";
            File.WriteAllText(Path.Combine(output, "android-release-audit.json"), JsonUtility.ToJson(e, true));
        }
    }
}
#endif
