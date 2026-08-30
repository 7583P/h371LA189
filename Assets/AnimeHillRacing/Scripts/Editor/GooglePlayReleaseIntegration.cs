#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using GooglePlayGames.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class GooglePlayReleaseIntegration
    {
        private const string AppId = "1059931844415";
        private const string Package = "com.matias.hilladventurer";
        private const string StarterProduct = "starter_pack";

        private static readonly Dictionary<string, string> Leaderboards =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Countryside", "CgkIv97xxuweEAIQAQ" },
                { "City", "CgkIv97xxuweEAIQAg" },
                { "NeonCity", "CgkIv97xxuweEAIQAw" },
                { "Mountains", "CgkIv97xxuweEAIQBA" },
                { "Rainyer", "CgkIv97xxuweEAIQBQ" },
                { "Space", "CgkIv97xxuweEAIQBg" },
                { "NorthPole", "CgkIv97xxuweEAIQBw" }
            };

        public static void ConfigureAndValidateBatch()
        {
            Configure();
            ValidateOrThrow();
            Debug.Log("GOOGLE_PLAY_LOCAL_INTEGRATION_OK");
        }

        public static void BuildReleaseAabBatch()
        {
            ValidateOrThrow();
            if (PlayerSettings.Android.bundleVersionCode != 11)
                throw new InvalidOperationException("Expected release versionCode 11; refusing another increment.");
            string keystorePassword = Environment.GetEnvironmentVariable(
                "HILL_ADVENTURER_KEYSTORE_PASSWORD");
            string keyaliasPassword = Environment.GetEnvironmentVariable(
                "HILL_ADVENTURER_KEYALIAS_PASSWORD");
            if (string.IsNullOrEmpty(keystorePassword) || string.IsNullOrEmpty(keyaliasPassword))
                throw new InvalidOperationException("Release signing passwords were not provided in the " +
                    "HILL_ADVENTURER_KEYSTORE_PASSWORD and HILL_ADVENTURER_KEYALIAS_PASSWORD environment variables.");
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasPass = keyaliasPassword;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            string root = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(root, "Builds", "Android");
            Directory.CreateDirectory(outputDirectory);
            string output = Path.Combine(outputDirectory,
                "HillAdventurer-v" + PlayerSettings.Android.bundleVersionCode + ".aab");
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);

            AssetDatabase.SaveAssets();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("AAB build failed: " + report.summary.result +
                    ", errors=" + report.summary.totalErrors);
            Debug.Log("HILL_ADVENTURER_RELEASE_AAB_OK: " + output + " bytes=" +
                report.summary.totalSize + " versionCode=" + PlayerSettings.Android.bundleVersionCode);
        }

        private static void Configure()
        {
            if (!GPGSAndroidSetupUI.PerformSetup(string.Empty, AppId, null))
                throw new InvalidOperationException("Official GPGS Android setup failed.");

            foreach (KeyValuePair<string, string> entry in Leaderboards)
            {
                string path = "Assets/AnimeHillRacing/MapProfiles/" + entry.Key + ".asset";
                MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(path);
                if (profile == null) throw new InvalidOperationException("Missing MapProfile: " + path);
                SerializedObject serialized = new SerializedObject(profile);
                SerializedProperty id = serialized.FindProperty("leaderboardId");
                if (id == null) throw new InvalidOperationException("Missing leaderboardId: " + path);
                id.stringValue = entry.Value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
            }

            const string starterPath = "Assets/AnimeHillRacing/Resources/StarterPackConfiguration.asset";
            StarterPackConfiguration starter =
                AssetDatabase.LoadAssetAtPath<StarterPackConfiguration>(starterPath);
            if (starter == null) throw new InvalidOperationException("Missing Starter Pack configuration.");
            SerializedObject starterSerialized = new SerializedObject(starter);
            starterSerialized.FindProperty("googlePlayProductId").stringValue = StarterProduct;
            starterSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(starter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ValidateOrThrow()
        {
            if (PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) != Package)
                throw new InvalidOperationException("Unexpected Android package.");
            GooglePlayGames.PlayGamesSettings settings =
                GooglePlayGames.PlayGamesSettings.LoadInstance();
            if (settings == null || settings.AppId != AppId)
                throw new InvalidOperationException("GPGS App ID was not persisted.");
            foreach (KeyValuePair<string, string> entry in Leaderboards)
            {
                MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(
                    "Assets/AnimeHillRacing/MapProfiles/" + entry.Key + ".asset");
                SerializedProperty id = new SerializedObject(profile).FindProperty("leaderboardId");
                if (id == null || id.stringValue != entry.Value)
                    throw new InvalidOperationException("Leaderboard mismatch: " + entry.Key);
            }
            StarterPackConfiguration starter = AssetDatabase.LoadAssetAtPath<StarterPackConfiguration>(
                "Assets/AnimeHillRacing/Resources/StarterPackConfiguration.asset");
            if (starter == null || starter.googlePlayProductId != StarterProduct)
                throw new InvalidOperationException("Starter Pack Product ID mismatch.");
            if (!File.Exists("Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml"))
                throw new InvalidOperationException("Generated GPGS Android manifest missing.");
        }
    }
}
#endif
