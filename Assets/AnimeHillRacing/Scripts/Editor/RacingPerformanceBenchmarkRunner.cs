#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    // Compatibility entry points now launch the real asynchronous Play Mode harness.
    // The former manual loop neither rendered frames nor measured laps. Its reports
    // and synthetic "human" sessions must never be accepted as release evidence.
    [InitializeOnLoad]
    public static class RacingPerformanceBenchmarkRunner
    {
        static RacingPerformanceBenchmarkRunner() { EditorApplication.update += CheckRequests; }
        static void CheckRequests()
        {
            if(File.Exists("Temp/RunBenchmark.request"))
            { File.Delete("Temp/RunBenchmark.request"); ExecuteFullBenchmark(); }
            if(File.Exists("Temp/RunTelemetry.request"))
            {
                string value=File.ReadAllText("Temp/RunTelemetry.request").Trim();
                File.Delete("Temp/RunTelemetry.request");
                int rating=int.TryParse(value,out int parsed)?Mathf.Clamp(parsed,0,10000):8500;
                File.WriteAllText("Temp/RunTelemetry.response",RecordRealTelemetryRace(rating));
            }
        }
        [MenuItem("Anime Hill Racing/AI/Run Full Performance & Difficulty Benchmark")]
        public static void ExecuteFullBenchmark()
        {
            Debug.Log("[Benchmark] Real 12-racer shared-physics Play Mode benchmark. Laps come from checkpoints. Accelerated editor timing is not rendered FPS; use the Development Player profiler for FPS.");
            RacingClosureValidation.RunMeasuredBenchmark();
        }
        public static string RecordRealTelemetryRace(int playerMmr=8500,float durationSeconds=55f)
        {
            RacingClosureValidation.RunMeasuredTelemetry(playerMmr);
            return "SCHEDULED: measured full race, scripted Unity physics provenance. Await suite-results.json and telemetry under the closure output directory. No human pace claim.";
        }
    }
}
#endif
