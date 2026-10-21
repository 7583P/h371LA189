#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingRcEvidenceBuilder
    {
        [Serializable] sealed class AssetRow
        {
            public string path, type, recommendation, androidFormat, audioLoadType, audioCompression;
            public long sourceBytes, packedBytes;
            public int objects, sourceWidth, sourceHeight, maxSize;
            public bool resources, directlyOrTransitivelyReferencedByScene, referencedThroughResources, readable, mipmaps, androidOverride;
            public string[] scenes;
        }
        [Serializable] sealed class SizeReport
        {
            public string platform, result, output, options, measurementNote;
            public long buildSummaryBytes, outputBytes;
            public AssetRow[] assets;
        }
        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        static string Output => Path.GetFullPath(RacingFrameForensics.Argument("--closure-output", "Logs/RCPass/20261012/build"));
        const string TextureQaCompany = "AHR.TextureQA.PreviousCompany";
        [InitializeOnLoadMethod]
        static void RestoreTextureQaOnExit()
        {
            EditorApplication.quitting += () =>
            {
                string previous = SessionState.GetString(TextureQaCompany, "");
                if (SessionState.GetBool(TextureQaCompany + ".Active", false))
                {
                    PlayerSettings.companyName = previous;
                    SessionState.EraseString(TextureQaCompany);
                    SessionState.SetBool(TextureQaCompany + ".Active", false);
                }
            };
        }
        public static void BeginAndroidTextureSmokeBatch()
        {
            SessionState.SetString(TextureQaCompany, PlayerSettings.companyName);
            SessionState.SetBool(TextureQaCompany + ".Active", true);
            PlayerSettings.companyName = "HillAdventurer.BlacklineQA";
            if (Application.companyName != PlayerSettings.companyName)
                throw new InvalidOperationException("Editor texture QA requires isolated company preferences");
            RacingClosureValidation.Run();
        }
        public static void AuditReleaseConfigurationBatch()
        {
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildAppBundle = true;
            RacingAndroidReleaseAudit.Run();
        }
        public static void FinalizeReleaseChecksBatch()
        {
            AuditReleaseConfigurationBatch();
            RacingRcPassContracts.Run();
        }
        public static void ExportLatestBatch()
        {
            Export(BuildReport.GetLatestReport(), Output);
        }
        public static void BuildWindowsBatch()
        {
            Directory.CreateDirectory(Output);
            string path = Path.GetFullPath(RacingFrameForensics.Argument("--rc-build-path", "Builds/HumanGateLatest/HillAdventurer-HumanGate.exe"));
            if(File.Exists(path))throw new InvalidOperationException("Refusing to overwrite an existing Windows QA build");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bool timing = PlayerSettings.enableFrameTimingStats;
            string company = PlayerSettings.companyName;
            try
            {
                PlayerSettings.enableFrameTimingStats = true;
                PlayerSettings.companyName = "HillAdventurer.BlacklineQA";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = path,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development | BuildOptions.DetailedBuildReport });
                Export(report, Output);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows QA build failed");
            }
            finally { PlayerSettings.enableFrameTimingStats = timing; PlayerSettings.companyName = company; }
        }
        // Release-sized forensic bundle. It cannot be approved or distributed as an RC.
        // With no signing environment it uses the standard debug keystore for local size measurement.
        public static void BuildAndroidSizeBatch()
        {
            Directory.CreateDirectory(Output);
            string path = Path.GetFullPath(RacingFrameForensics.Argument("--rc-build-path", "Builds/AndroidSizeForensics/baseline.aab"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite an existing forensic AAB");
            bool custom = PlayerSettings.Android.useCustomKeystore;
            bool development = EditorUserBuildSettings.development, debugging = EditorUserBuildSettings.allowDebugging;
            bool profiler = EditorUserBuildSettings.connectProfiler, aab = EditorUserBuildSettings.buildAppBundle;
            var backend=PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            var architectures=PlayerSettings.Android.targetArchitectures;
            try
            {
                EditorUserBuildSettings.buildAppBundle = true;
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                // A size audit is never authorized to promote itself to production
                // signing merely because credentials happen to exist in the shell.
                // Use Unity's existing default debug identity; never read/set
                // production credentials or touch the production keystore.
                PlayerSettings.Android.useCustomKeystore = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = path,
                    target = BuildTarget.Android, options = BuildOptions.DetailedBuildReport });
                Export(report, Output);
                File.WriteAllText(Path.Combine(Output, "forensic-only.txt"), "SIZE AUDIT ONLY; NOT FOR INSTALLATION OR RELEASE; productionSigned=False; defaultDebugIdentity=True; no production credentials read\n");
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Android size build failed");
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore = custom;
                EditorUserBuildSettings.development = development; EditorUserBuildSettings.allowDebugging = debugging;
                EditorUserBuildSettings.connectProfiler = profiler; EditorUserBuildSettings.buildAppBundle = aab;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,backend);
                PlayerSettings.Android.targetArchitectures=architectures;
            }
        }
        public static void Export(BuildReport report, string output)
        {
            if (report == null) throw new InvalidOperationException("No actual BuildReport exists");
            Directory.CreateDirectory(output);
            var refs = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var resourceDependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (string resource in AssetDatabase.GetAllAssetPaths())
                if (resource.Contains("/Resources/") && !AssetDatabase.IsValidFolder(resource))
                    foreach (string dependency in AssetDatabase.GetDependencies(resource, true)) resourceDependencies.Add(dependency);
            foreach (string scene in Scenes)
                foreach (string dependency in AssetDatabase.GetDependencies(scene, true))
                {
                    if (!refs.TryGetValue(dependency, out var list)) refs[dependency] = list = new List<string>();
                    list.Add(scene);
                }
            var rows = new Dictionary<string, AssetRow>(StringComparer.Ordinal);
            foreach (var pack in report.packedAssets)
                foreach (var info in pack.contents)
                {
                    string key = info.sourceAssetPath ?? "<built-in>";
                    if (!rows.TryGetValue(key, out var row))
                    {
                        row = new AssetRow { path = key, type = info.type != null ? info.type.ToString() : "unknown",
                            sourceBytes = File.Exists(key) ? new FileInfo(key).Length : 0,
                            resources = key.Contains("/Resources/"), directlyOrTransitivelyReferencedByScene = refs.ContainsKey(key),
                            referencedThroughResources = resourceDependencies.Contains(key),
                            scenes = refs.TryGetValue(key, out var scenes) ? scenes.ToArray() : Array.Empty<string>() };
                        var importer = AssetImporter.GetAtPath(key);
                        if (importer is TextureImporter texture)
                        {
                            texture.GetSourceTextureWidthAndHeight(out row.sourceWidth, out row.sourceHeight);
                            var android = texture.GetPlatformTextureSettings("Android");
                            row.androidOverride = android.overridden; row.androidFormat = android.format.ToString();
                            row.maxSize = android.overridden ? android.maxTextureSize : texture.maxTextureSize;
                            row.readable = texture.isReadable; row.mipmaps = texture.mipmapEnabled;
                            row.recommendation = "Inspect Android format, alpha and actual displayed size; preserve source dimensions until visual evidence exists";
                        }
                        else if (importer is AudioImporter audio)
                        {
                            var settings = audio.ContainsSampleSettingsOverride("Android") ? audio.GetOverrideSampleSettings("Android") : audio.defaultSampleSettings;
                            row.audioCompression = settings.compressionFormat.ToString(); row.audioLoadType = settings.loadType.ToString();
                            row.recommendation = "Check duration and playback use; long PCM is a candidate for measured Vorbis/streaming comparison";
                        }
                        else row.recommendation = "Retain until release dependency and runtime loading audit proves safe exclusion";
                        rows[key] = row;
                    }
                    row.objects++; row.packedBytes += (long)info.packedSize;
                }
            var evidence = new SizeReport { platform = report.summary.platform.ToString(), result = report.summary.result.ToString(),
                output = report.summary.outputPath, options = report.summary.options.ToString(), buildSummaryBytes = (long)report.summary.totalSize,
                outputBytes = File.Exists(report.summary.outputPath) ? new FileInfo(report.summary.outputPath).Length : 0,
                measurementNote = "packedBytes are actual serialized BuildReport attribution before archive/ZIP compression. Per-asset ZIP compressed sizes are not exposed; do not estimate them. Scene references are AssetDatabase transitive dependencies; Resources/runtime references need separate audit.",
                assets = rows.Values.OrderByDescending(r => r.packedBytes).ToArray() };
            File.WriteAllText(Path.Combine(output, "build-size.json"), JsonUtility.ToJson(evidence, true));
            using (var csv = new StreamWriter(Path.Combine(output, "top50-packaged-assets.csv")))
            {
                csv.WriteLine("asset_path,type,source_bytes,build_report_packed_bytes,zip_compressed_bytes,scene_reference,resources,recommendation");
                foreach (var row in evidence.assets.Take(50))
                    csv.WriteLine($"\"{row.path}\",\"{row.type}\",{row.sourceBytes},{row.packedBytes},unavailable,{row.directlyOrTransitivelyReferencedByScene},{row.resources},\"{row.recommendation}\"");
            }
            File.WriteAllText(Path.Combine(output, "build-summary.txt"), $"result={evidence.result}\nplatform={evidence.platform}\nbytes={evidence.buildSummaryBytes}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nseconds={report.summary.totalTime.TotalSeconds}\n");
        }
    }
}
#endif
