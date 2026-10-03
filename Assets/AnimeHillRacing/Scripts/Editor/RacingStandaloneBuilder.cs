#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnimeHillRacing.EditorScripts
{
    public static class RacingStandaloneBuilder
    {
        private const string OutputRelativePath = "Builds/StandaloneWindows64/HillAdventurer.exe";

        [MenuItem("Hill Adventurer/Build/Windows 64 Standalone (.exe)")]
        public static bool BuildWindows()
        {
            string projectDir = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(projectDir, OutputRelativePath);
            string outputDirectory = Path.GetDirectoryName(outputPath);

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[RacingStandaloneBuilder] No enabled scenes found in EditorBuildSettings!");
                return false;
            }

            Debug.Log($"[RacingStandaloneBuilder] Starting build for StandaloneWindows64. Scenes count: {scenes.Length}");
            foreach (var s in scenes)
            {
                Debug.Log($"[RacingStandaloneBuilder] - Scene: {s}");
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            bool success = summary.result == BuildResult.Succeeded;
            if (success)
            {
                Debug.Log($"[RacingStandaloneBuilder] BUILD SUCCESS! Output: {outputPath} ({summary.totalSize} bytes, duration: {summary.totalTime.TotalSeconds:F1}s)");
            }
            else
            {
                Debug.LogError($"[RacingStandaloneBuilder] BUILD FAILED: {summary.result}, Errors: {summary.totalErrors}");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(success ? 0 : 1);
            }

            return success;
        }
    }
}
#endif
