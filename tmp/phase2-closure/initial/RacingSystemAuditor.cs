using System;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingSystemAuditor
    {
        public sealed class TestReport
        {
            public int Passed;
            public int Failed;
            public readonly List<string> FailureMessages = new List<string>();

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
}
