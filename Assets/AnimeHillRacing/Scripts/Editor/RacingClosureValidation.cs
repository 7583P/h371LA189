#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public static class RacingClosureValidation
    {
        const string Prefix = "AHR.Racing.Closure.";
        static RacingClosureValidation() { EditorApplication.update += Tick; }
        public static void Run()
        { Configure(null, null); }
        public static void RunMeasuredBenchmark() { Configure("benchmark", null); }
        public static void RunMeasuredTelemetry(int mmr) { Configure("races", mmr); }
        static void Configure(string forcedMode, int? forcedMmr)
        {
            typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries")?.GetMethod("Clear",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public)?.Invoke(null,null);
            string[] args = Environment.GetCommandLineArgs();
            string Value(string key, string fallback)
            {
                int index = Array.IndexOf(args, key);
                return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
            }
            SessionState.SetString(Prefix + "Output", Value("--closure-output", "Logs/RacingPhase2/closure/default"));
            SessionState.SetString(Prefix + "Mode", forcedMode ?? Value("--closure-mode", "races"));
            SessionState.SetString(Prefix + "Seeds", Value("--closure-seeds", "4207"));
            SessionState.SetFloat(Prefix + "TimeScale", float.Parse(Value("--closure-timescale", "4"), System.Globalization.CultureInfo.InvariantCulture));
            SessionState.SetInt(Prefix + "Mmr", forcedMmr ?? int.Parse(Value("--closure-mmr", "1000")));
            SessionState.SetBool(Prefix + "Capture", Array.IndexOf(args, "--closure-capture") >= 0);
            SessionState.SetBool(Prefix + "Started", false);
            SessionState.SetBool(Prefix + "Done", false);
            SessionState.SetBool(Prefix + "Active", true);
            Directory.CreateDirectory(SessionState.GetString(Prefix + "Output", ""));
            if (SessionState.GetString(Prefix + "Mode", "") == "build") { BuildPlayer(); return; }
            if (SessionState.GetString(Prefix + "Mode", "") == "adventure")
            {
                SessionState.SetBool(Prefix + "Active",false);
                Directory.CreateDirectory("Temp");
                // The Adventure runner defaults to Countryside. Request the full
                // vehicle/map matrix explicitly without changing its physics fixture.
                File.WriteAllText("Temp/RunAdventurePhysics2DValidation.request",
                    "car-01,hill-adventurer-01,formula-01,rally-01,sport-bike-01|countryside,rainyer,neon-city,mountains,space");
                return;
            }
            if (SessionState.GetString(Prefix + "Mode", "") == "adventure-maps")
            {
                SessionState.SetBool(Prefix + "Active",false);
                Directory.CreateDirectory("Temp");
                File.WriteAllText("Temp/RunAdventurePhysics2DValidation.request","car-01|countryside,rainyer,neon-city,mountains,space");
                return;
            }
            if (SessionState.GetString(Prefix + "Mode", "") == "adventure-mountains") {
                SessionState.SetBool(Prefix+"Active",false);Directory.CreateDirectory("Temp");
                File.WriteAllText("Temp/RunAdventurePhysics2DValidation.request","car-01,rally-01,sport-bike-01|mountains");return;
            }
            if (SessionState.GetString(Prefix + "Mode", "") == "adventure-landing")
            {
                SessionState.SetBool(Prefix + "Active",false);
                Directory.CreateDirectory("Temp");
                File.WriteAllText("Temp/RunAdventurePhysics2DValidation.request","car-01|mountains");
                return;
            }
            if (SessionState.GetString(Prefix + "Mode", "") == "contracts")
            { RacingClosureTests.Run(SessionState.GetString(Prefix + "Output", ""), BeginPlay); return; }
            if (SessionState.GetString(Prefix + "Mode", "") == "cli-tests")
            { RacingClosureTests.Run(SessionState.GetString(Prefix + "Output", ""),()=>EditorApplication.Exit(SessionState.GetBool(Prefix+"ContractsPassed",false)?0:1)); return; }
            BeginPlay();
        }
        static void BeginPlay()
        {
            EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            AdventurePlayModeStartScene.UseSceneForNextPlayMode("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            EditorApplication.isPlaying = true;
        }
        static void BuildPlayer()
        {
            SessionState.SetBool(Prefix + "Active",false);
            var scenes=new System.Collections.Generic.List<string>();
            foreach(var scene in EditorBuildSettings.scenes)if(scene.enabled)scenes.Add(scene.path);
            string path=Path.GetFullPath("Builds/HumanGateLatest/HillAdventurer-HumanGate.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bool frameTiming=PlayerSettings.enableFrameTimingStats;
            string company=PlayerSettings.companyName;
            int exitCode=1;
            try
            {
                PlayerSettings.enableFrameTimingStats=true;
                PlayerSettings.companyName="HillAdventurer.BlacklineQA";
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes.ToArray(),locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                File.WriteAllText(Path.Combine(SessionState.GetString(Prefix + "Output",""),"build.txt"),$"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nbytes={report.summary.totalSize}\nseconds={report.summary.totalTime.TotalSeconds}\n");
                exitCode=report.summary.result==BuildResult.Succeeded?0:1;
            }
            finally { PlayerSettings.enableFrameTimingStats=frameTiming;PlayerSettings.companyName=company; }
            EditorApplication.Exit(exitCode);
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Prefix + "Active", false)) return;
            if (EditorApplication.isPlaying && !SessionState.GetBool(Prefix + "Started", false))
            {
                SessionState.SetBool(Prefix + "Started", true);
                if(SessionState.GetString(Prefix+"Mode","")=="product") {
                    new GameObject("Native product integration validation").AddComponent<RacingProductProbe>().Begin(SessionState.GetString(Prefix+"Output",""));return;
                }
                new GameObject("Phase 2 closure validation").AddComponent<RacingClosureProbe>().Begin(
                    SessionState.GetString(Prefix + "Output", ""),
                    SessionState.GetString(Prefix + "Mode", "races"),
                    SessionState.GetString(Prefix + "Seeds", "4207"),
                    SessionState.GetFloat(Prefix + "TimeScale", 4),
                    SessionState.GetInt(Prefix + "Mmr", 1000),
                    SessionState.GetBool(Prefix + "Capture", false));
            }
            if (!SessionState.GetBool(Prefix + "Done", false)) return;
            SessionState.SetBool(Prefix + "Active", false);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(SessionState.GetInt(Prefix + "Exit", 1));
        }
    }
}
#endif
