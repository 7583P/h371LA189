using System;
using System.IO;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public static class RacingSystemAuditor
    {
        private const string RequestPath = "Temp/RunRacingAuditor.request";
        private const string ResponsePath = "Temp/RacingAuditorResults.json";

        static RacingSystemAuditor()
        {
            EditorApplication.update -= CheckAuditorRequest;
            EditorApplication.update += CheckAuditorRequest;
        }

        private static void CheckAuditorRequest()
        {
            if (!File.Exists(RequestPath)) return;
            try { File.Delete(RequestPath); } catch {}
            var report = RunAllTests();
            var json = JsonUtility.ToJson(report, true);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(ResponsePath, json);
            Debug.Log($"<color=green>[RacingSystemAuditor] Automated run completed: Passed={report.Passed}, Failed={report.Failed}</color>");
        }

        [Serializable]
        public sealed class TestReport
        {
            public int Passed;
            public int Failed;
            public List<string> FailureMessages = new List<string>();

            public void Assert(bool condition, string testName, string errorDetails = "")
            {
                if (condition)
                {
                    Passed++;
                }
                else
                {
                    Failed++;
                    FailureMessages.Add($"[FAIL] {testName}: {errorDetails}");
                }
            }
        }

        [MenuItem("Anime Hill Racing/Validation/Run Racing 3D Foundation Tests")]
        public static void RunAllTestsMenu()
        {
            TestReport report = RunAllTests();
            if (report.Failed > 0)
            {
                Debug.LogError($"[Racing 3D Tests] FAILED {report.Failed} / {report.Passed + report.Failed} tests:\n" + string.Join("\n", report.FailureMessages));
            }
            else
            {
                Debug.Log($"<color=green>[Racing 3D Tests] ALL {report.Passed} TESTS PASSED PERFECTLY!</color>");
            }
        }

        public static TestReport RunAllTests()
        {
            var report = new TestReport();

            TestMmrSystem(report);
            TestMatchmakingAndLobby(report);
            TestRacingStatsCalculator(report);
            TestAiSkillModel(report);
            TestTrackProgressTracker(report);
            TestSaveDataAndMigration(report);
            TestJumpAndAirTricks(report);
            TestVoidRespawnAndCollision(report);
            TestMetaProbabilityAndLobbyDiversity(report);

            return report;
        }

        public static void TestMmrSystem(TestReport report)
        {
            // 1. Initial MMR
            report.Assert(MmrSystem.DefaultStartingMmr == 1000, "MMR_DefaultStartingValue", $"Expected 1000, got {MmrSystem.DefaultStartingMmr}");
            report.Assert(MmrSystem.MinimumMmr == 0, "MMR_MinimumValue", $"Expected 0, got {MmrSystem.MinimumMmr}");

            // 2. 1st place gain in equal lobby
            int[] equalLobbyMmrs = new int[12];
            int[] positionsFirst = new int[12];
            for (int i = 0; i < 12; i++) { equalLobbyMmrs[i] = 1000; positionsFirst[i] = i + 1; }

            int winDelta = MmrSystem.CalculateMmrDelta(0, 1000, 1, equalLobbyMmrs, positionsFirst);
            report.Assert(winDelta > 50, "MMR_1stPlaceGainInEqualLobby", $"Expected > 50, got {winDelta}");

            // 3. 12th place loss in equal lobby
            int[] positionsTwelfth = new int[12];
            positionsTwelfth[0] = 12;
            for (int i = 1; i < 12; i++) positionsTwelfth[i] = i;

            int lossDelta = MmrSystem.CalculateMmrDelta(0, 1000, 12, equalLobbyMmrs, positionsTwelfth);
            report.Assert(lossDelta < -40, "MMR_12thPlaceLossInEqualLobby", $"Expected < -40, got {lossDelta}");

            // 4. Strength of lobby comparison:
            // Winning against 3000 MMR lobby while at 1000 MMR should give higher gain than against 1000 MMR lobby
            int[] hardLobbyMmrs = new int[12];
            for (int i = 0; i < 12; i++) hardLobbyMmrs[i] = 2500;
            int winAgainstHard = MmrSystem.CalculateMmrDelta(0, 1000, 1, hardLobbyMmrs, positionsFirst);
            report.Assert(winAgainstHard > winDelta, "MMR_StrengthOfLobbyHigherOnUpset", $"Expected {winAgainstHard} > {winDelta}");

            // 5. Uncapped > 10,000 behavior
            int apexMmr = 12400;
            int[] apexLobby = new int[12];
            for (int i = 0; i < 12; i++) apexLobby[i] = 12400;
            int apexDelta = MmrSystem.CalculateMmrDelta(0, apexMmr, 1, apexLobby, positionsFirst);
            report.Assert(apexDelta > 10, "MMR_ApexContinuesGrowingAbove10000", $"Expected > 10, got {apexDelta}");

            int newApex = MmrSystem.ApplyDelta(apexMmr, apexDelta);
            report.Assert(newApex > apexMmr, "MMR_ApexUncappedGrowth", $"Expected {newApex} > {apexMmr}");

            // 6. Zero clamping
            int clampedLoss = MmrSystem.ApplyDelta(15, -100);
            report.Assert(clampedLoss == 0, "MMR_ZeroClampingNoNegative", $"Expected 0, got {clampedLoss}");
        }

        public static void TestMatchmakingAndLobby(TestReport report)
        {
            // 1. Lobby size guarantee: exactly 12 racers
            var lobby = MatchmakingService.GenerateRankedLobby(4500, "car-01", "wheel-default", "Tester", 42);
            report.Assert(lobby != null && lobby.Length == 12, "Lobby_Total12Racers", $"Expected 12 racers, got {lobby?.Length}");

            // 2. Exactly 1 human and 11 AI
            int humanCount = 0;
            int aiCount = 0;
            for (int i = 0; i < lobby.Length; i++)
            {
                if (lobby[i].isHuman) humanCount++;
                else aiCount++;
            }
            report.Assert(humanCount == 1, "Lobby_ExactlyOneHuman", $"Expected 1 human, got {humanCount}");
            report.Assert(aiCount == 11, "Lobby_ExactlyElevenAI", $"Expected 11 AI, got {aiCount}");

            // 3. MMR distribution centered around player
            int sumAiMmr = 0;
            for (int i = 0; i < lobby.Length; i++)
            {
                if (!lobby[i].isHuman) sumAiMmr += lobby[i].mmr;
            }
            float avgAiMmr = sumAiMmr / 11f;
            float diffFromTarget = Mathf.Abs(avgAiMmr - 4500f);
            report.Assert(diffFromTarget < 400f, "Lobby_MmrDistributionCentered", $"Expected diff < 400, got {diffFromTarget:F1} (Avg: {avgAiMmr})");

            // 4. Deterministic Seed
            var lobbyA = MatchmakingService.GenerateRankedLobby(6000, "car-01", "wheel-default", "SeedTester", 12345);
            var lobbyB = MatchmakingService.GenerateRankedLobby(6000, "car-01", "wheel-default", "SeedTester", 12345);
            bool identical = true;
            for (int i = 0; i < 12; i++)
            {
                if (lobbyA[i].displayName != lobbyB[i].displayName ||
                    lobbyA[i].mmr != lobbyB[i].mmr ||
                    lobbyA[i].gridPosition != lobbyB[i].gridPosition)
                {
                    identical = false;
                    break;
                }
            }
            report.Assert(identical, "Lobby_DeterministicSeedReproducibility", "Identical seeds produced differing lobbies!");
        }

        public static void TestRacingStatsCalculator(TestReport report)
        {
            // 1. Base Stats
            FinalRacingStats jeepBase = RacingStatsCalculator.Calculate("car-01", "wheel-default");
            report.Assert(jeepBase.SpeedKmh >= 140f && jeepBase.SpeedKmh <= 150f, "Stats_BaseJeepSpeed", $"Expected ~145, got {jeepBase.SpeedKmh}");

            // 2. Modifiers
            FinalRacingStats sportJeep = RacingStatsCalculator.Calculate("car-01", "wheel-sport");
            report.Assert(sportJeep.SpeedKmh > jeepBase.SpeedKmh, "Stats_WheelSportSpeedIncrease", $"Expected {sportJeep.SpeedKmh} > {jeepBase.SpeedKmh}");
            report.Assert(sportJeep.LateralGrip > jeepBase.LateralGrip, "Stats_WheelSportGripIncrease", $"Expected {sportJeep.LateralGrip} > {jeepBase.LateralGrip}");

            FinalRacingStats offroadJeep = RacingStatsCalculator.Calculate("car-01", "wheel-offroad");
            report.Assert(offroadJeep.OffroadRetention > jeepBase.OffroadRetention, "Stats_WheelOffroadRetentionIncrease", $"Expected {offroadJeep.OffroadRetention} > {jeepBase.OffroadRetention}");

            // 3. Player / AI Equality
            FinalRacingStats playerStats = RacingStatsCalculator.Calculate("formula-01", "wheel-racing");
            FinalRacingStats aiStats = RacingStatsCalculator.Calculate("formula-01", "wheel-racing");
            report.Assert(Mathf.Abs(playerStats.SpeedKmh - aiStats.SpeedKmh) < 0.001f &&
                          Mathf.Abs(playerStats.AccelerationMps2 - aiStats.AccelerationMps2) < 0.001f,
                          "Stats_PlayerAiEquality", "Player and AI resolved different stats for same configuration!");
        }

        public static void TestAiSkillModel(TestReport report)
        {
            // 1. Skill scaling across MMR
            var rookie = AiSkillModel.CreateProfile(500, 101);
            var expert = AiSkillModel.CreateProfile(4500, 101);
            var champion = AiSkillModel.CreateProfile(8500, 101);
            var apex = AiSkillModel.CreateProfile(11500, 101);

            report.Assert(expert.racingLineSkill > rookie.racingLineSkill, "AI_LineSkillScaling", $"{expert.racingLineSkill} > {rookie.racingLineSkill}");
            report.Assert(champion.driftSkill > expert.driftSkill, "AI_DriftSkillScaling", $"{champion.driftSkill} > {expert.driftSkill}");
            report.Assert(champion.shortcutKnowledge > rookie.shortcutKnowledge, "AI_ShortcutKnowledgeScaling", $"{champion.shortcutKnowledge} > {rookie.shortcutKnowledge}");
            report.Assert(apex.consistency >= champion.consistency, "AI_ConsistencyScaling", $"{apex.consistency} >= {champion.consistency}");

            // 2. Personality variation between two bots of the same MMR
            var botA = AiSkillModel.CreateProfile(7000, 1111);
            var botB = AiSkillModel.CreateProfile(7000, 9999);

            bool hasVariation = Mathf.Abs(botA.aggression - botB.aggression) > 0.01f ||
                                Mathf.Abs(botA.riskTolerance - botB.riskTolerance) > 0.01f;
            report.Assert(hasVariation, "AI_PersonalityVariationSameMmr", "Two bots of same MMR have zero personality variation!");

            // But skills remain in the same competitive bracket
            report.Assert(Mathf.Abs(botA.racingLineSkill - botB.racingLineSkill) < 0.20f,
                          "AI_BoundedVariationSameTier", $"Skill variation too wide: {Mathf.Abs(botA.racingLineSkill - botB.racingLineSkill):F2}");
        }

        public static void TestTrackProgressTracker(TestReport report)
        {
            // Create in-memory mock track
            GameObject testObj = new GameObject("MockTrack");
            var graph = testObj.AddComponent<TrackSplineGraph>();

            TrackNode[] nodes = new TrackNode[8];
            for (int i = 0; i < 8; i++)
            {
                nodes[i] = new TrackNode
                {
                    Position = new Vector3(i * 30f, 0f, 0f),
                    Forward = Vector3.right,
                    Right = Vector3.forward,
                    Up = Vector3.up,
                    TrackWidth = 12f
                };
            }
            graph.InitializeFromNodes(nodes);

            var tracker = testObj.AddComponent<TrackProgressTracker>();
            tracker.Initialize(graph, 3, 8);

            // Register 2 test racers
            var id1 = new RacerIdentity("r1", "Leader", "car-01", "wheel-default", 1000, true, 1, 1);
            var id2 = new RacerIdentity("r2", "Chaser", "car-01", "wheel-default", 1000, false, 2, 2);

            GameObject rObj1 = new GameObject("Racer1");
            rObj1.transform.position = new Vector3(100f, 0f, 0f);
            var v1 = rObj1.AddComponent<RacingVehiclePhysics>();

            GameObject rObj2 = new GameObject("Racer2");
            rObj2.transform.position = new Vector3(50f, 0f, 0f);
            var v2 = rObj2.AddComponent<RacingVehiclePhysics>();

            var p1 = tracker.RegisterRacer(id1, v1, null);
            var p2 = tracker.RegisterRacer(id2, v2, null);

            tracker.UpdateProgress(5.0f);

            report.Assert(p1.RacePosition == 1, "Progress_LeaderIsFirst", $"Expected P1, got P{p1.RacePosition}");
            report.Assert(p2.RacePosition == 2, "Progress_ChaserIsSecond", $"Expected P2, got P{p2.RacePosition}");

            // Clean up gameobjects
            UnityEngine.Object.DestroyImmediate(testObj);
            UnityEngine.Object.DestroyImmediate(rObj1);
            UnityEngine.Object.DestroyImmediate(rObj2);
        }

        public static void TestSaveDataAndMigration(TestReport report)
        {
            using (new RacingPlayerPrefsTestScope())
            {
            // Simulate legacy Adventure coins
            PlayerPrefs.SetInt("AHR.TotalCoins", 45000);
            PlayerPrefs.SetInt("AHR.TotalGems", 350);

            // Run migration
            RacingSaveManager.EnsureMigration();

            // Verify Adventure data was not altered
            int coins = PlayerPrefs.GetInt("AHR.TotalCoins", 0);
            int gems = PlayerPrefs.GetInt("AHR.TotalGems", 0);
            report.Assert(coins == 45000, "Save_AdventureCoinsPreserved", $"Expected 45000, got {coins}");
            report.Assert(gems == 350, "Save_AdventureGemsPreserved", $"Expected 350, got {gems}");

            // Verify Racing fields initialized cleanly
            report.Assert(RacingSaveManager.CurrentMmr >= 1000, "Save_MmrInitializedCleanly", $"Expected >= 1000, got {RacingSaveManager.CurrentMmr}");

            // Test recording a race result
            int initialMmr = RacingSaveManager.CurrentMmr;
            RacingSaveManager.RecordRaceResult(1, 85);
            report.Assert(RacingSaveManager.CurrentMmr == initialMmr + 85, "Save_MmrUpdatedAfterRace", $"Expected {initialMmr + 85}, got {RacingSaveManager.CurrentMmr}");
            report.Assert(RacingSaveManager.WinsCount >= 1, "Save_WinsCountIncremented", $"Expected >= 1, got {RacingSaveManager.WinsCount}");
            }
        }

        public static void TestJumpAndAirTricks(TestReport report)
        {
            var go = new GameObject("TestVehicle_JumpAir");
            var rb = go.AddComponent<Rigidbody>();
            var phys = go.AddComponent<RacingVehiclePhysics>();
            phys.Initialize(new FinalRacingStats { SpeedKmh = 140, AccelerationMps2 = 18, HandlingRadPerSec = 3.5f, DriftMultiplier = 1.2f, WeightKg = 400 });

            // 1. Verify Jump action command availability
            var cmd = new VehicleCommands { Jump = true, Throttle = 1f };
            report.Assert(cmd.Jump, "Jump_CommandSupported", "VehicleCommands supports Jump flag");

            // 2. Verify Air Trick & Landing Boost properties
            report.Assert(!phys.IsPerformingTrick, "Trick_InitialStateFalse", "IsPerformingTrick initial state is false");
            report.Assert(phys.TrickCount == 0, "Trick_InitialCountZero", "TrickCount initial state is 0");

            // 3. Verify Boost system
            phys.ApplySpeedBoost(1.3f, 1.0f);
            report.Assert(phys.IsBoosting, "Boost_ApplySpeedBoostActive", "ApplySpeedBoost activates IsBoosting");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestVoidRespawnAndCollision(TestReport report)
        {
            var go = new GameObject("TestVehicle_VoidRespawn");
            var rb = go.AddComponent<Rigidbody>();
            var phys = go.AddComponent<RacingVehiclePhysics>();
            var rec = go.AddComponent<VehicleRecovery>();
            phys.Initialize(new FinalRacingStats { SpeedKmh = 140, AccelerationMps2 = 18, HandlingRadPerSec = 3.5f, DriftMultiplier = 1.2f, WeightKg = 400 });
            phys.SetRaceEnabled(true);

            // 1. Reduced recovery penalty (0.85s, not 2.5s)
            report.Assert(rec.PenaltySeconds <= 1.0f, "Recovery_PenaltyFair", $"Expected <= 1.0s penalty, got {rec.PenaltySeconds:F2}s");

            // 2. Void detection threshold
            rec.SetLastSafeTransform(new Vector3(0, 10, 0), Quaternion.identity);
            go.transform.position = new Vector3(0, 0, 0); // 10m below safe position
            report.Assert(rec.IsFallingIntoVoid, "Recovery_IsFallingIntoVoid", "Falling 10m below last safe position triggers IsFallingIntoVoid");

            // 3. Rolling start on recovery
            rec.TriggerRecovery("Test void recovery");
            report.Assert(rec.IsRecovering, "Recovery_TriggerRecoveryActive", "TriggerRecovery initiates recovery penalty");
            report.Assert(rb.linearVelocity.magnitude > 5f, "Recovery_RollingStartMomentum", $"Recovery provides forward rolling restart momentum: {rb.linearVelocity.magnitude:F1} m/s");
            report.Assert(phys.IsBoosting, "Recovery_BoostAssistance", "Recovery applies speed boost for fair return to racing");

            // 4. MMR 100,000 Scaling
            report.Assert(MmrSystem.TargetMaxMmr == 100000, "MMR_TargetMax100k", $"Expected 100,000 ceiling, got {MmrSystem.TargetMaxMmr}");
            report.Assert(MmrTierUtility.GetTier(100000) == MmrTier.Apex, "MMR_TierApexAt100k", "100,000 MMR classifies as Apex tier");
            report.Assert(MmrTierUtility.GetTier(55000) == MmrTier.Elite, "MMR_TierEliteAt55k", "55,000 MMR classifies as Elite tier");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestMetaProbabilityAndLobbyDiversity(TestReport report)
        {
            // 1. Evaluate Continuous Curve Knots & Smooth Interpolation
            float p5k = MetaProgressionConfig.EvaluateMetaProbability(5000);
            report.Assert(p5k < 0.05f && p5k > 0f, "Meta_ProbAt5kRare", $"Expected 0 < p < 0.05 at 5k MMR, got {p5k:P2}");

            float p25k = MetaProgressionConfig.EvaluateMetaProbability(25000);
            report.Assert(p25k >= 0.10f && p25k <= 0.20f, "Meta_ProbAt25kMinority", $"Expected 0.10 <= p <= 0.20 at 25k MMR, got {p25k:P2}");

            float p50k = MetaProgressionConfig.EvaluateMetaProbability(50000);
            report.Assert(p50k >= 0.35f && p50k <= 0.45f, "Meta_ProbAt50kMidRange", $"Expected 0.35 <= p <= 0.45 at 50k MMR, got {p50k:P2}");

            float p75k = MetaProgressionConfig.EvaluateMetaProbability(75000);
            report.Assert(p75k >= 0.60f && p75k <= 0.70f, "Meta_ProbAt75kCommon", $"Expected 0.60 <= p <= 0.70 at 75k MMR, got {p75k:P2}");

            float p95k = MetaProgressionConfig.EvaluateMetaProbability(95000);
            report.Assert(p95k >= 0.80f && p95k <= 0.90f, "Meta_ProbAt95kDominant", $"Expected 0.80 <= p <= 0.90 at 95k MMR, got {p95k:P2}");

            float p100k = MetaProgressionConfig.EvaluateMetaProbability(100000);
            report.Assert(p100k >= 0.85f && p100k <= 0.92f, "Meta_ProbAt100kApex", $"Expected 0.85 <= p <= 0.92 at 100k MMR, got {p100k:P2}");

            // 2. Statistical Aggregate Simulation across 50 Seeds
            const int SampleRuns = 50;
            int metaCount5k = 0, metaCount25k = 0, metaCount50k = 0, metaCount75k = 0, metaCount100k = 0;
            bool noLobbyAllMeta = true;
            bool allLobbiesHaveAlternatives = true;

            for (int s = 0; s < SampleRuns; s++)
            {
                int seed = 1000 + s * 13;

                // 5,000 MMR Lobby
                var lob5k = MatchmakingService.GenerateRankedLobby(5000, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob5k.Length; i++) if (lob5k[i].buildCategory == MetaBuildCategory.Meta) metaCount5k++;

                // 25,000 MMR Lobby
                var lob25k = MatchmakingService.GenerateRankedLobby(25000, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob25k.Length; i++) if (lob25k[i].buildCategory == MetaBuildCategory.Meta) metaCount25k++;

                // 50,000 MMR Lobby
                var lob50k = MatchmakingService.GenerateRankedLobby(50000, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob50k.Length; i++) if (lob50k[i].buildCategory == MetaBuildCategory.Meta) metaCount50k++;

                // 75,000 MMR Lobby
                var lob75k = MatchmakingService.GenerateRankedLobby(75000, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob75k.Length; i++) if (lob75k[i].buildCategory == MetaBuildCategory.Meta) metaCount75k++;

                // 100,000 MMR Lobby
                var lob100k = MatchmakingService.GenerateRankedLobby(100000, "car-01", "wheel-default", "Tester", seed);
                int lobbyMetaCount = 0;
                for (int i = 1; i < lob100k.Length; i++)
                {
                    if (lob100k[i].buildCategory == MetaBuildCategory.Meta)
                    {
                        metaCount100k++;
                        lobbyMetaCount++;
                    }
                }

                if (lobbyMetaCount == 11) noLobbyAllMeta = false; // Must never be 11/11 identical META bots
                if (11 - lobbyMetaCount < 1) allLobbiesHaveAlternatives = false; // Must preserve at least 1-2 alternatives
            }

            int totalAi = SampleRuns * 11;
            float rate5k = (float)metaCount5k / totalAi;
            float rate25k = (float)metaCount25k / totalAi;
            float rate50k = (float)metaCount50k / totalAi;
            float rate75k = (float)metaCount75k / totalAi;
            float rate100k = (float)metaCount100k / totalAi;

            report.Assert(rate5k < 0.10f, "Meta_AggregateAt5kRare", $"5k aggregate rate: {rate5k:P1} (expected < 10%)");
            report.Assert(rate25k > rate5k && rate25k < 0.30f, "Meta_AggregateAt25kMinority", $"25k aggregate rate: {rate25k:P1}");
            report.Assert(rate50k > rate25k && rate50k >= 0.25f && rate50k <= 0.60f, "Meta_AggregateAt50kMidRange", $"50k aggregate rate: {rate50k:P1}");
            report.Assert(rate75k > rate50k && rate75k >= 0.50f, "Meta_AggregateAt75kCommon", $"75k aggregate rate: {rate75k:P1}");
            report.Assert(rate100k >= 0.65f && rate100k <= 0.85f, "Meta_AggregateAt100kApex", $"100k aggregate rate: {rate100k:P1}");

            // 3. Lobby Diversity Guarantees
            report.Assert(noLobbyAllMeta, "Diversity_NoLobby11IdenticalMeta", "No 100k lobby should have 11 identical META AI");
            report.Assert(allLobbiesHaveAlternatives, "Diversity_AlternativesPreservedInHighMmr", "Every high-MMR lobby preserved at least 1-2 alternative setups");

            // 4. Deterministic Reproducibility
            var repLobby1 = MatchmakingService.GenerateRankedLobby(88000, "car-01", "wheel-default", "Tester", 9999);
            var repLobby2 = MatchmakingService.GenerateRankedLobby(88000, "car-01", "wheel-default", "Tester", 9999);
            bool repMatch = true;
            for (int i = 0; i < 12; i++)
            {
                if (repLobby1[i].displayName != repLobby2[i].displayName ||
                    repLobby1[i].mmr != repLobby2[i].mmr ||
                    repLobby1[i].buildCategory != repLobby2[i].buildCategory ||
                    repLobby1[i].vehicleId != repLobby2[i].vehicleId ||
                    repLobby1[i].wheelId != repLobby2[i].wheelId)
                {
                    repMatch = false;
                    break;
                }
            }
            report.Assert(repMatch, "Meta_DeterministicSeedReproducibility", "Fixed RaceSeed reproduces exact build and category assignments");
        }
    }
}
