#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Ads;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Racing3D.ML;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    // Release-candidate batch: every focused suite runs independently so one
    // failure cannot hide another; then the read-only Android audit.
    public static class RacingRcPassContracts
    {
        [Serializable] sealed class Suite { public string name; public bool passed; public string error; }
        [Serializable] sealed class Summary { public Suite[] suites; public int failed; }

        public static void Run()
        {
            int failed=RunAll();
            if(Application.isBatchMode)EditorApplication.Exit(failed==0?0:1);
        }

        public static int RunAll()
        {
            string output = RacingFrameForensics.Argument("--closure-output");
            Directory.CreateDirectory(output);
            var suites = new System.Collections.Generic.List<Suite>();
            void Step(string name, Action action)
            {
                var suite = new Suite { name = name };
                try { action(); suite.passed = true; }
                catch (Exception error) { suite.error = error.GetBaseException().Message; }
                suites.Add(suite);
            }
            Step("Comeback+Lancer+SkyHop", RacingComebackContracts.Run);
            Step("Balance", RacingBalanceContracts.Run);
            Step("FinalSmoke", RacingFinalSmokeContracts.Run);
            Step("Lancer300", RacingLancer300Contracts.Run);
            Step("SeasonReset", RacingSeasonResetContracts.Run);
            Step("ReleaseFixes", () => ReleaseFixes(output));
            Step("AndroidAudit", RacingAndroidReleaseAudit.Run);
            var summary = new Summary { suites = suites.ToArray() };
            foreach (var s in suites) if (!s.passed) summary.failed++;
            File.WriteAllText(Path.Combine(output, "rc-contracts.json"), JsonUtility.ToJson(summary, true));
            return summary.failed;
        }

        static void ReleaseFixes(string output)
        {
            var report = new ClosureContractReport();
            string folder = Path.Combine(output, "telemetry-retention-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            for (int i = 0; i < 9; i++)
            {
                string path = Path.Combine(folder, "race_fixture_" + i + ".json");
                File.WriteAllText(path, "{}");
                File.SetLastWriteTimeUtc(path, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
            }
            File.WriteAllText(Path.Combine(folder, "unrelated.txt"), "keep");
            LocalTelemetrySink.PruneDeviceSessions(folder);
            var kept = Directory.GetFiles(folder, "race_*.json");
            Array.Sort(kept);
            report.Check(kept.Length == LocalTelemetrySink.RetainedDeviceSessions && Path.GetFileName(kept[0]) == "race_fixture_4.json" &&
                File.Exists(Path.Combine(folder, "unrelated.txt")), "Device telemetry keeps only the five newest race sessions and nothing else is deleted");
            var ads = Resources.Load<AdMobConfiguration>("AdMobConfiguration");
            report.Check(ads != null && !ads.useTestAds && ads.AndroidAppId == ads.androidAppId && ads.AndroidAppId != AdMobConfiguration.AndroidTestAppId,
                "Release Android manifest uses the production AdMob app ID");
            report.Check(ads != null && !ads.androidRewardedAdUnitId.Contains("3940256099942544") && !ads.androidInterstitialAdUnitId.Contains("3940256099942544") &&
                !ads.androidBannerAdUnitId.Contains("3940256099942544"), "Production Android ad-unit IDs are not Google sample units");
            // The Editor is a development context: it must request sample units only.
            report.Check(ads != null && ads.UseSampleAdUnits && ads.RewardedAdUnitId == AdMobConfiguration.AndroidTestRewardedId,
                "Editor/development players request Google sample ad units, never live units");
            var qaOff=RacingMlExecutionPolicy.QaOverride(true,"HillAdventurer.BlacklineQA",new[]{"--racing-ml-mode","Off"});
            report.Check(RacingMlExecutionPolicy.Effective(MlExecutionMode.Shadow,qaOff)==MlExecutionMode.Off,"Explicit QA Off survives the vehicle spawner's later Shadow default");
            report.Check(RacingMlExecutionPolicy.QaOverride(false,"HillAdventurer.BlacklineQA",new[]{"--racing-ml-mode","Shadow"})==null&&
                RacingMlExecutionPolicy.Effective(MlExecutionMode.Authority,MlExecutionMode.Authority)==MlExecutionMode.Shadow,"Production ignores QA overrides and unapproved authority stays Shadow");
            File.WriteAllText(Path.Combine(output, "release-fix-contracts.json"), JsonUtility.ToJson(report, true));
            if (report.failed != 0) throw new InvalidOperationException("Release fix contracts failed: " + report.failed);
        }
    }
}
#endif
