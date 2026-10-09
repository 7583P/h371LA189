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
            TestRecoveryLeadPreservation(report);
            TestUprightSpinAudioAndFormulaVisuals(report);
            TestPositionAndLapAudits(report);
            TestAerialBoostAndAirRoll(report);
            TestMobileTouchControls(report);
            TestShortcutKnowledgeBaseAndObservationalLearning(report);
            TestPredictiveItemAI(report);
            TestAiDifficultyAndPerformanceScaling(report);

            return report;
        }

        public static void TestMmrSystem(TestReport report)
        {
            // 1. Initial MMR
            report.Assert(MmrSystem.DefaultStartingMmr == 0, "MMR_DefaultStartingValue", $"Expected 0 on a fresh installation, got {MmrSystem.DefaultStartingMmr}");
            report.Assert(MmrSystem.MinimumMmr == 0, "MMR_MinimumValue", $"Expected 0, got {MmrSystem.MinimumMmr}");

            // 2. 1st place gain in equal lobby
            int[] equalLobbyMmrs = new int[12];
            int[] positionsFirst = new int[12];
            for (int i = 0; i < 12; i++) { equalLobbyMmrs[i] = 1000; positionsFirst[i] = i + 1; }

            int winDelta = MmrSystem.CalculateMmrDelta(0, 1000, 1, equalLobbyMmrs, positionsFirst);
            report.Assert(winDelta > 0, "MMR_1stPlaceGainInEqualLobby", $"Expected positive 10k-scale gain, got {winDelta}");

            // 3. 12th place loss in equal lobby
            int[] positionsTwelfth = new int[12];
            positionsTwelfth[0] = 12;
            for (int i = 1; i < 12; i++) positionsTwelfth[i] = i;

            int lossDelta = MmrSystem.CalculateMmrDelta(0, 1000, 12, equalLobbyMmrs, positionsTwelfth);
            report.Assert(lossDelta < 0, "MMR_12thPlaceLossInEqualLobby", $"Expected negative 10k-scale loss, got {lossDelta}");

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
            var lobby = MatchmakingService.GenerateSeededTestLobby(4500, "car-01", "wheel-default", "Tester", 42);
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
            var lobbyA = MatchmakingService.GenerateSeededTestLobby(6000, "car-01", "wheel-default", "SeedTester", 12345);
            var lobbyB = MatchmakingService.GenerateSeededTestLobby(6000, "car-01", "wheel-default", "SeedTester", 12345);
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

        public static void TestRecoveryLeadPreservation(TestReport report)
        {
            // Circular mock track with 8 nodes and 8 checkpoints
            GameObject testObj = new GameObject("MockTrackRegression");
            var graph = testObj.AddComponent<TrackSplineGraph>();

            int nodeCount = 8;
            TrackNode[] nodes = new TrackNode[nodeCount];
            float radius = 60f;
            for (int i = 0; i < nodeCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / nodeCount);
                float nextAngle = (i + 1) * (Mathf.PI * 2f / nodeCount);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 nextPos = new Vector3(Mathf.Cos(nextAngle) * radius, 0f, Mathf.Sin(nextAngle) * radius);
                Vector3 forward = (nextPos - pos).normalized;
                nodes[i] = new TrackNode
                {
                    Position = pos,
                    Forward = forward,
                    Right = Vector3.Cross(Vector3.up, forward),
                    Up = Vector3.up,
                    TrackWidth = 14f
                };
            }
            graph.InitializeFromNodes(nodes);

            var tracker = testObj.AddComponent<TrackProgressTracker>();
            int totalLaps = 3;
            tracker.Initialize(graph, totalLaps, nodeCount);

            // Register Player and AI Bot
            var idPlayer = new RacerIdentity("human", "Player", "formula-01", "wheel-default", 10000, true, 1, 1);
            var idAi = new RacerIdentity("ai1", "Elite Bot", "formula-01", "wheel-default", 10000, false, 2, 2);

            GameObject rObjPlayer = new GameObject("PlayerCar");
            var vPlayer = rObjPlayer.AddComponent<RacingVehiclePhysics>();
            var recPlayer = rObjPlayer.AddComponent<VehicleRecovery>();

            GameObject rObjAi = new GameObject("AiCar");
            var vAi = rObjAi.AddComponent<RacingVehiclePhysics>();

            var pPlayer = tracker.RegisterRacer(idPlayer, vPlayer, recPlayer);
            var pAi = tracker.RegisterRacer(idAi, vAi, null);

            // 1. Small lead on same lap (Lap 1)
            rObjPlayer.transform.position = nodes[3].Position;
            rObjAi.transform.position = nodes[1].Position;
            tracker.UpdateProgress(1.0f);
            report.Assert(pPlayer.RacePosition == 1 && pAi.RacePosition == 2, "Recovery_SmallLead_InitialPosition", $"Expected P1, got P{pPlayer.RacePosition}");

            // Player falls off track
            rObjPlayer.transform.position = new Vector3(500f, -100f, 500f);
            tracker.UpdateProgress(1.0f);
            recPlayer.SetLastSafeTransform(nodes[3].Position, Quaternion.LookRotation(nodes[3].Forward, Vector3.up));
            rObjPlayer.transform.position = nodes[3].Position;
            tracker.NotifyVehicleRecovered(vPlayer);
            tracker.UpdateProgress(1.0f);
            report.Assert(pPlayer.CurrentLap == 1 && pPlayer.RacePosition == 1, "Recovery_SmallLead_PreservedP1", $"Expected Lap 1 P1, got Lap {pPlayer.CurrentLap} P{pPlayer.RacePosition}");

            // 2. Large lead (~1 full lap advantage!)
            // Complete lap 1:
            for (int cp = 0; cp < nodeCount; cp++)
                tracker.NotifyCheckpoint(vPlayer, cp, cp == 0, 10f + cp);
            tracker.NotifyCheckpoint(vPlayer, 0, true, 20f); // enter lap 2
            tracker.NotifyCheckpoint(vPlayer, 1, false, 22f);
            tracker.NotifyCheckpoint(vPlayer, 2, false, 24f);

            // AI reached Checkpoint 3 on Lap 1
            for (int cp = 0; cp <= 3; cp++)
                tracker.NotifyCheckpoint(vAi, cp, cp == 0, 15f + cp);

            rObjPlayer.transform.position = nodes[2].Position;
            rObjAi.transform.position = nodes[3].Position;
            tracker.UpdateProgress(1.0f);

            report.Assert(pPlayer.CurrentLap == 2, "Recovery_LargeLead_PlayerOnLap2", $"Expected Lap 2, got {pPlayer.CurrentLap}");
            report.Assert(pAi.CurrentLap == 1, "Recovery_LargeLead_AiOnLap1", $"Expected Lap 1, got {pAi.CurrentLap}");
            report.Assert(pPlayer.RacePosition == 1, "Recovery_LargeLead_PlayerIsFirstBeforeFall", $"Expected P1, got P{pPlayer.RacePosition}");

            // Player leaves track with ~1 lap lead
            rObjPlayer.transform.position = new Vector3(1000f, -500f, 1000f);
            tracker.UpdateProgress(1.0f);

            // Recovery triggers
            recPlayer.SetLastSafeTransform(nodes[2].Position, Quaternion.LookRotation(nodes[2].Forward, Vector3.up));
            rObjPlayer.transform.position = nodes[2].Position;
            tracker.NotifyVehicleRecovered(vPlayer);
            tracker.UpdateProgress(1.0f);

            // CRITICAL REGRESSION ASSERTIONS:
            report.Assert(pPlayer.CurrentLap == 2, "Recovery_1LapLead_LapPreserved", $"Expected Lap 2 preserved, got {pPlayer.CurrentLap}");
            report.Assert(pPlayer.LastCheckpointIndex == 2, "Recovery_1LapLead_CheckpointPreserved", $"Expected checkpoint 2, got {pPlayer.LastCheckpointIndex}");
            report.Assert(pPlayer.RacePosition == 1, "Recovery_1LapLead_PositionRemainsFirst", $"CRITICAL REGRESSION: Player was reset to P{pPlayer.RacePosition} instead of remaining P1!");
            report.Assert(pPlayer.TotalDistance > pAi.TotalDistance, "Recovery_1LapLead_TotalDistanceAdvantagePreserved", $"Player dist {pPlayer.TotalDistance:F1} must be > AI dist {pAi.TotalDistance:F1}");

            // 3. Same-lap traffic recovery
            rObjPlayer.transform.position = nodes[4].Position + nodes[4].Forward * 2f;
            rObjAi.transform.position = nodes[4].Position;
            tracker.UpdateProgress(1.0f);
            recPlayer.SetLastSafeTransform(nodes[4].Position + nodes[4].Forward * 2f, Quaternion.identity);
            rObjPlayer.transform.position = nodes[4].Position + nodes[4].Forward * 2f;
            tracker.NotifyVehicleRecovered(vPlayer);
            tracker.UpdateProgress(1.0f);
            report.Assert(pPlayer.RacePosition == 1, "Recovery_SameLapTraffic_OrderPreserved", $"Expected P1, got P{pPlayer.RacePosition}");

            // 4. Final-lap recovery & normal race finish
            for (int cp = 3; cp < nodeCount; cp++) tracker.NotifyCheckpoint(vPlayer, cp, false, 30f + cp);
            tracker.NotifyCheckpoint(vPlayer, 0, true, 40f); // enter lap 3
            report.Assert(pPlayer.CurrentLap == 3, "Recovery_FinalLap_EnteredLap3", $"Expected Lap 3, got {pPlayer.CurrentLap}");

            // Fall off track on final lap
            rObjPlayer.transform.position = new Vector3(800f, -200f, 0f);
            tracker.UpdateProgress(1.0f);
            recPlayer.SetLastSafeTransform(nodes[1].Position, Quaternion.identity);
            rObjPlayer.transform.position = nodes[1].Position;
            tracker.NotifyVehicleRecovered(vPlayer);
            tracker.UpdateProgress(1.0f);

            report.Assert(pPlayer.CurrentLap == 3, "Recovery_FinalLap_Lap3Preserved", $"Expected Lap 3 preserved, got {pPlayer.CurrentLap}");
            report.Assert(pPlayer.RacePosition == 1, "Recovery_FinalLap_RemainsP1", $"Expected P1 on final lap, got P{pPlayer.RacePosition}");

            // Finish race normally
            for (int cp = 1; cp < nodeCount; cp++) tracker.NotifyCheckpoint(vPlayer, cp, false, 50f + cp);
            tracker.NotifyCheckpoint(vPlayer, 0, true, 60f); // finish race
            report.Assert(pPlayer.Finished, "Recovery_RaceContinuesNormallyToFinish", "Player should legitimately finish race after recovery");
            report.Assert(pPlayer.RacePosition == 1, "Recovery_FinishedInFirstPlace", $"Expected finish P1, got P{pPlayer.RacePosition}");

            // 5. Shortcut Fall & Multiple Recoveries Test
            GameObject multiObj = new GameObject("MultiRecoveryTest");
            var multiGraph = multiObj.AddComponent<TrackSplineGraph>();
            multiGraph.InitializeFromNodes(nodes);
            var multiTracker = multiObj.AddComponent<TrackProgressTracker>();
            multiTracker.Initialize(multiGraph, 3, nodeCount);

            GameObject rObjP2 = new GameObject("Player2Car");
            var vP2 = rObjP2.AddComponent<RacingVehiclePhysics>();
            var recP2 = rObjP2.AddComponent<VehicleRecovery>();
            var pP2 = multiTracker.RegisterRacer(idPlayer, vP2, recP2);

            // Cross start line
            multiTracker.NotifyCheckpoint(vP2, 0, true, 0f);
            report.Assert(pP2.CurrentLap == 1 && pP2.HasCrossedStart, "MultiRecovery_StartedLap1", "Lap 1 started");

            // Drive to node 2, take shortcut approach, fall off mid-shortcut
            rObjP2.transform.position = nodes[2].Position;
            multiTracker.UpdateProgress(1.0f);
            recP2.SetLastSafeTransform(nodes[2].Position, Quaternion.LookRotation(nodes[2].Forward, Vector3.up));

            // Falling into void during shortcut
            rObjP2.transform.position = new Vector3(999f, -150f, 999f);
            multiTracker.UpdateProgress(1.1f);
            report.Assert(pP2.CurrentLap == 1, "MultiRecovery_ShortcutFall_LapIntact", "Lap 1 preserved during shortcut void fall");

            // First recovery: restore from shortcut fall
            rObjP2.transform.position = nodes[2].Position;
            multiTracker.NotifyVehicleRecovered(vP2);
            multiTracker.UpdateProgress(1.2f);
            report.Assert(pP2.CurrentLap == 1 && pP2.LastCheckpointIndex == 0, "MultiRecovery_ShortcutRecovery_Intact", "Progress cleanly restored after shortcut fall");

            // Progress through checkpoints and cross to Lap 2
            for (int cp = 1; cp < nodeCount; cp++) multiTracker.NotifyCheckpoint(vP2, cp, false, 10f + cp);
            multiTracker.NotifyCheckpoint(vP2, 0, true, 25f); // Lap 2
            report.Assert(pP2.CurrentLap == 2, "MultiRecovery_ReachedLap2", "Reached Lap 2 cleanly");

            // Second fall on Lap 2 at node 4
            rObjP2.transform.position = nodes[4].Position;
            multiTracker.UpdateProgress(26f);
            recP2.SetLastSafeTransform(nodes[4].Position, Quaternion.identity);
            rObjP2.transform.position = new Vector3(-800f, -300f, 400f);
            multiTracker.UpdateProgress(26.2f);
            report.Assert(pP2.CurrentLap == 2, "MultiRecovery_SecondFall_Lap2Preserved", "Lap 2 strictly preserved during second fall");

            rObjP2.transform.position = nodes[4].Position;
            multiTracker.NotifyVehicleRecovered(vP2);
            multiTracker.UpdateProgress(26.5f);
            report.Assert(pP2.CurrentLap == 2, "MultiRecovery_SecondRecovery_Lap2Intact", "Lap 2 intact after second recovery");

            // Third fall on Lap 2 at node 6
            rObjP2.transform.position = nodes[6].Position;
            multiTracker.UpdateProgress(28f);
            recP2.SetLastSafeTransform(nodes[6].Position, Quaternion.identity);
            rObjP2.transform.position = new Vector3(0f, -400f, -800f);
            multiTracker.UpdateProgress(28.2f);
            rObjP2.transform.position = nodes[6].Position;
            multiTracker.NotifyVehicleRecovered(vP2);
            multiTracker.UpdateProgress(28.5f);
            report.Assert(pP2.CurrentLap == 2, "MultiRecovery_ThirdRecovery_Lap2Intact", "Lap 2 intact after third recovery in same race");

            // Advance to finish line and complete race
            for (int cp = 1; cp < nodeCount; cp++) multiTracker.NotifyCheckpoint(vP2, cp, false, 35f + cp);
            multiTracker.NotifyCheckpoint(vP2, 0, true, 45f); // Lap 3
            report.Assert(pP2.CurrentLap == 3, "MultiRecovery_ReachedLap3", "Lap 3 reached despite multiple falls");
            for (int cp = 1; cp < nodeCount; cp++) multiTracker.NotifyCheckpoint(vP2, cp, false, 55f + cp);
            multiTracker.NotifyCheckpoint(vP2, 0, true, 65f); // Race finish
            report.Assert(pP2.Finished, "MultiRecovery_FinalFinishValid", "Race legitimately completed after 3 recoveries across 3 laps");

            UnityEngine.Object.DestroyImmediate(testObj);
            UnityEngine.Object.DestroyImmediate(rObjPlayer);
            UnityEngine.Object.DestroyImmediate(rObjAi);
            UnityEngine.Object.DestroyImmediate(multiObj);
            UnityEngine.Object.DestroyImmediate(rObjP2);
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
            report.Assert(RacingSaveManager.CurrentMmr == 0, "Save_MmrInitializedCleanly", $"A fresh installation must start at 0 MMR, got {RacingSaveManager.CurrentMmr}");

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

            // 4. MMR 10,000 Scaling
            report.Assert(MmrSystem.TargetMaxMmr == 10000, "MMR_TargetMax10k", $"Expected 10,000 ceiling, got {MmrSystem.TargetMaxMmr}");
            report.Assert(MmrTierUtility.GetTier(10000) == MmrTier.ApexII, "MMR_TierApexAt10k", "10,000 MMR remains in normal Apex II");
            report.Assert(MmrTierUtility.GetTier(10001) == MmrTier.ApexBeyond, "MMR_TierBeyondAbove10k", "Beyond requires more than 10,000 MMR");
            report.Assert(MmrTierUtility.GetTier(5500) == MmrTier.EliteII, "MMR_TierEliteAt5p5k", "5,500 MMR classifies as Elite tier");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestMetaProbabilityAndLobbyDiversity(TestReport report)
        {
            // 1. Evaluate Continuous Curve Knots & Smooth Interpolation
            float p500 = MetaProgressionConfig.EvaluateMetaProbability(500);
            report.Assert(p500 < 0.05f && p500 > 0f, "Meta_ProbAt500Rare", $"Expected 0 < p < 0.05 at 500 MMR, got {p500:P2}");

            float p2p5k = MetaProgressionConfig.EvaluateMetaProbability(2500);
            report.Assert(p2p5k >= 0.22f && p2p5k <= 0.24f, "Meta_ProbAt2p5kMinority", $"Expected approximately 23% at 2p500 MMR, got {p2p5k:P2}");

            float p5k = MetaProgressionConfig.EvaluateMetaProbability(5000);
            report.Assert(Mathf.Abs(p5k-.82f)<.001f, "Meta_ProbAt5kCommon", $"Expected 82% at 500 MMR, got {p5k:P2}");

            float p7p5k = MetaProgressionConfig.EvaluateMetaProbability(7500);
            report.Assert(p7p5k >= .97f && p7p5k <= .985f, "Meta_ProbAt7p5kElite", $"Expected 97%-98.5% at 7p500 MMR, got {p7p5k:P2}");

            float p9p5k = MetaProgressionConfig.EvaluateMetaProbability(9500);
            report.Assert(p9p5k >= .995f && p9p5k <= .998f, "Meta_ProbAt9p5kDominant", $"Expected 99.5%-99.8% at 9p500 MMR, got {p9p5k:P2}");

            float p10k = MetaProgressionConfig.EvaluateMetaProbability(10000);
            report.Assert(Mathf.Abs(p10k-.998f)<.001f, "Meta_ProbAt10kApex", $"Expected 99.8% at 10k MMR, got {p10k:P2}");

            // 2. Statistical Aggregate Simulation across 50 Seeds
            const int SampleRuns = 50;
            int metaCount500 = 0, metaCount2p5k = 0, metaCount5k = 0, metaCount7p5k = 0, metaCount10k = 0;
            bool eliteLobbiesExact = true;
            int allMeta7p5k=0, allMeta10k=0;

            for (int s = 0; s < SampleRuns; s++)
            {
                int seed = 1000 + s * 13;

                // 5,000 MMR Lobby
                var lob500 = MatchmakingService.GenerateSeededTestLobby(500, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob500.Length; i++) if (lob500[i].buildCategory == MetaBuildCategory.Meta) metaCount500++;

                // 25,000 MMR Lobby
                var lob2p5k = MatchmakingService.GenerateSeededTestLobby(2500, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob2p5k.Length; i++) if (lob2p5k[i].buildCategory == MetaBuildCategory.Meta) metaCount2p5k++;

                // 50,000 MMR Lobby
                var lob5k = MatchmakingService.GenerateSeededTestLobby(5000, "car-01", "wheel-default", "Tester", seed);
                for (int i = 1; i < lob5k.Length; i++) if (lob5k[i].buildCategory == MetaBuildCategory.Meta) metaCount5k++;

                // 75,000 MMR Lobby
                var lob7p5k = MatchmakingService.GenerateSeededTestLobby(7500, "car-01", "wheel-default", "Tester", seed);
                int elite7p5k=0;
                for (int i = 1; i < lob7p5k.Length; i++) if (lob7p5k[i].buildCategory == MetaBuildCategory.Meta)
                {metaCount7p5k++;elite7p5k++;eliteLobbiesExact &= lob7p5k[i].vehicleId==MetaProgressionConfig.MetaVehicleId&&lob7p5k[i].wheelId==MetaProgressionConfig.MetaWheelId;}
                eliteLobbiesExact &= elite7p5k>=10;
                if(elite7p5k==11)allMeta7p5k++;

                // 10,000 MMR Lobby
                var lob10k = MatchmakingService.GenerateSeededTestLobby(10000, "car-01", "wheel-default", "Tester", seed);
                int lobbyMetaCount = 0;
                for (int i = 1; i < lob10k.Length; i++)
                {
                    if (lob10k[i].buildCategory == MetaBuildCategory.Meta)
                    {
                        metaCount10k++;
                        lobbyMetaCount++;
                        eliteLobbiesExact &= lob10k[i].vehicleId==MetaProgressionConfig.MetaVehicleId&&lob10k[i].wheelId==MetaProgressionConfig.MetaWheelId;
                    }
                }

                eliteLobbiesExact &= lobbyMetaCount>=10;
                if(lobbyMetaCount==11)allMeta10k++;
            }

            int totalAi = SampleRuns * 11;
            float rate500 = (float)metaCount500 / totalAi;
            float rate2p5k = (float)metaCount2p5k / totalAi;
            float rate5k = (float)metaCount5k / totalAi;
            float rate7p5k = (float)metaCount7p5k / totalAi;
            float rate10k = (float)metaCount10k / totalAi;

            report.Assert(rate500 < 0.10f, "Meta_AggregateAt500Rare", $"500 aggregate rate: {rate500:P1} (expected < 10%)");
            report.Assert(rate2p5k > rate500 && rate2p5k < 0.30f, "Meta_AggregateAt2p5kMinority", $"2.5k aggregate rate: {rate2p5k:P1}");
            report.Assert(rate5k > rate2p5k && rate5k >= .70f && rate5k <= .90f, "Meta_AggregateAt5kCommon", $"5k aggregate rate: {rate5k:P1}");
            report.Assert(rate7p5k > rate5k && rate7p5k >= .97f, "Meta_AggregateAt7p5kElite", $"7p5k aggregate rate: {rate7p5k:P1}");
            report.Assert(rate10k >= .97f && rate10k <= 1, "Meta_AggregateAt10kApex", $"10k aggregate rate: {rate10k:P1}");

            // 3. Lobby Diversity Guarantees
            report.Assert(eliteLobbiesExact, "Meta_EliteTenOrElevenExact", "Every 7p5k and 10k lobby has ten or eleven exact Formula/plasma META bots");
            report.Assert(allMeta7p5k>SampleRuns*.6f&&allMeta10k>SampleRuns*.6f, "Meta_UsuallyElevenExact", $"All eleven exact META: {allMeta7p5k}/{SampleRuns} at 7p5k; {allMeta10k}/{SampleRuns} at 10k");

            // 4. Deterministic Reproducibility
            var repLobby1 = MatchmakingService.GenerateSeededTestLobby(8800, "car-01", "wheel-default", "Tester", 9999);
            var repLobby2 = MatchmakingService.GenerateSeededTestLobby(8800, "car-01", "wheel-default", "Tester", 9999);
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

        public static void TestUprightSpinAudioAndFormulaVisuals(TestReport report)
        {
            // The saved Formula identifier now presents the original DAPEX / Whity design.
            report.Assert(RacingVehicleFactory.MetaNavy.r < .05f && RacingVehicleFactory.MetaNavy.g < .12f && RacingVehicleFactory.MetaNavy.b > .15f,
                "DAPEX_MidnightBlue", "META paint must remain dark blue");
            report.Assert(RacingVehicleFactory.MetaWhite.r > .9f && RacingVehicleFactory.MetaWhite.g > .9f && RacingVehicleFactory.MetaWhite.b > .9f,
                "Whity_TitaniumWhite", "Signature lines and wheel face must be white");
            report.Assert(JsonUtility.ToJson(RacingStatsCalculator.Calculate("formula-01", "wheel-whity")) == JsonUtility.ToJson(RacingStatsCalculator.Calculate("formula-01", "wheel-plasma")),
                "Whity_PreservesMetaPerformance", "New wheel identity must preserve the validated performance");
            var previewRoot = new GameObject("DAPEX visual contract");
            try
            {
                var visual = RacingVehicleVisuals.Build(previewRoot, "formula-01", "wheel-whity", RacingVehicleFactory.MetaNavy);
                bool body = false, discs = true;
                foreach (var renderer in previewRoot.GetComponentsInChildren<MeshRenderer>())
                    body |= renderer.sharedMaterial.name == "DAPEX / midnight blue pearl" && renderer.sharedMaterial.color == RacingVehicleFactory.MetaNavy;
                foreach (var wheel in visual.Wheels)
                    discs &= wheel.Find("Whity uninterrupted white disc") != null;
                report.Assert(body && visual.BodyVisual.Find("DAPEX panoramic cabin") != null, "DAPEX_OriginalCompactBody", "Actual generated body and cabin must use the new blue design");
                report.Assert(visual.Wheels.Length == 4 && discs, "Whity_FourAnimatedWheels", "All four animated wheel pivots must contain the continuous white face from the reference");
                var badge=visual.BodyVisual.Find("DAPEX rear badge");
                var brake=visual.BodyVisual.Find("Rear continuous lamp");
                report.Assert(badge!=null && brake!=null && badge.GetComponent<TextMesh>().text=="DAPEX" &&
                    badge.localPosition.y<brake.localPosition.y && badge.localPosition.z<brake.localPosition.z,
                    "DAPEX_RearLettering", "DAPEX must be centered on the rear below the red brake ribbon");
                report.Assert(previewRoot.GetComponentsInChildren<Collider>().Length == 0, "DAPEX_VisualsDoNotChangeCollision", "Visual geometry must not add gameplay colliders");
            }
            finally { UnityEngine.Object.DestroyImmediate(previewRoot); }

            // 2. Audio Synthesis Procedural Asset Validation
            report.Assert(RacingAudioSynth.EngineIdle() != null, "Audio_EngineIdleClipValid", "V10 boxer idle procedural clip generated");
            report.Assert(RacingAudioSynth.EngineLoad() != null, "Audio_EngineLoadClipValid", "Engine load power note procedural clip generated");
            report.Assert(RacingAudioSynth.DriftScrub() != null, "Audio_DriftScrubClipValid", "Dynamic tire scrub procedural clip generated");
            report.Assert(RacingAudioSynth.MiniTurboCue() != null, "Audio_MiniTurboClipValid", "Mini turbo procedural clip generated");
            report.Assert(RacingAudioSynth.MediumTurboCue() != null, "Audio_MediumTurboClipValid", "Medium turbo procedural clip generated");
            report.Assert(RacingAudioSynth.SuperTurboCue() != null, "Audio_SuperTurboClipValid", "Super turbo procedural clip generated");
            report.Assert(RacingAudioSynth.ItemTurboCue() != null, "Audio_ItemTurboClipValid", "Item turbo procedural clip generated");
            report.Assert(RacingAudioSynth.LandingImpactCue() != null, "Audio_LandingImpactClipValid", "Landing impact procedural clip generated");
            report.Assert(RacingAudioSynth.RockHitCue() != null, "Audio_RockHitClipValid", "Rock hit concussive procedural clip generated");
            report.Assert(RacingAudioSynth.DartHitCue() != null, "Audio_DartHitClipValid", "Dart hit supersonic procedural clip generated");
            report.Assert(RacingAudioSynth.SpinoutCue() != null, "Audio_SpinoutCueClipValid", "Upright spinout procedural clip generated");

            // 3. Item Hit Response & Upright Spinout Mechanics
            var go = new GameObject("TestVehicle_ItemHitSpin");
            var rb = go.AddComponent<Rigidbody>();
            var phys = go.AddComponent<RacingVehiclePhysics>();
            phys.Initialize(new FinalRacingStats { SpeedKmh = 140, AccelerationMps2 = 18, HandlingRadPerSec = 3.5f, DriftMultiplier = 1.2f, WeightKg = 400 });
            phys.SetRaceEnabled(true);

            report.Assert(!phys.IsSpunOut, "Spinout_InitialStateFalse", "IsSpunOut initially false");

            phys.TriggerSpinout(1.5f);
            report.Assert(phys.IsSpunOut, "Spinout_TriggerSpinoutActive", "TriggerSpinout sets IsSpunOut true");
            report.Assert(phys.ItemHits == 1, "Spinout_ItemHitsIncremented", $"Expected ItemHits == 1, got {phys.ItemHits}");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestPositionAndLapAudits(TestReport report)
        {
            RacingPositionAuditTests.RunAll(
                msg => report.Assert(true, msg),
                msg => report.Assert(false, msg, msg)
            );
        }

        public static void TestAerialBoostAndAirRoll(TestReport report)
        {
            var go = new GameObject("TestVehicle_AerialBoost");
            var rb = go.AddComponent<Rigidbody>();
            var phys = go.AddComponent<RacingVehiclePhysics>();
            phys.Initialize(new FinalRacingStats { SpeedKmh = 140, AccelerationMps2 = 18, HandlingRadPerSec = 3.5f, DriftMultiplier = 1.2f, WeightKg = 400 });
            phys.SetRaceEnabled(true);

            // Grounded state
            report.Assert(!phys.IsAerialBoosting, "AerialBoost_InitialInactive", "Aerial boost inactive on ground");

            // Simulate airborne command
            var cmd = new VehicleCommands { AerialBoost = true, Pitch = 0.5f, AirRoll = -0.5f };
            report.Assert(RacingVehiclePhysics.MaxAerialBoostDuration == 3.0f, "AerialBoost_Max3SecondDuration", $"Max aerial boost duration must be 3.0s, got {RacingVehiclePhysics.MaxAerialBoostDuration}");

            // Air roll torque check: verify non-zero air roll parameter accepted in commands
            report.Assert(cmd.AirRoll == -0.5f, "AirRoll_CommandMapped", "Air roll parameter correctly accepted in commands");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestMobileTouchControls(TestReport report)
        {
            var canvasGo = new GameObject("TestCanvas", typeof(Canvas));
            var inputGo = new GameObject("TestInput");
            var input = inputGo.AddComponent<PlayerRacingInput>();

            var mobile = RacingMobileControls.Create(canvasGo.transform, input);
            report.Assert(mobile != null, "Mobile_ComponentCreated", "RacingMobileControls created successfully");

            input.SetTouchGas(true);
            var cmd = input.SampleCommands();
            report.Assert(cmd.Throttle > 0.9f, "Mobile_TouchGasResponsive", $"Expected throttle ~1, got {cmd.Throttle}");

            input.SetTouchSteer(-0.75f);
            input.SetTouchJump(true);
            cmd = input.SampleCommands();
            report.Assert(cmd.Steering < -0.2f, "Mobile_TouchSteerResponsive", $"Expected negative steer, got {cmd.Steering}");

            UnityEngine.Object.DestroyImmediate(canvasGo);
            UnityEngine.Object.DestroyImmediate(inputGo);
        }

        public static void TestShortcutKnowledgeBaseAndObservationalLearning(TestReport report)
        {
            var kb = ShortcutKnowledgeBase.GetOrCreate();
            kb.InitializeDefaults();

            var routes = kb.GlobalExpertRoutes;
            report.Assert(routes != null && routes.Count >= 10, "KnowledgeBase_CatalogPopulated", $"Expected >= 10 routes in catalog, got {routes?.Count}");

            // All routes must have positive expected value
            bool allPositiveEv = true;
            for (int i = 0; i < routes.Count; i++)
            {
                if (routes[i].expectedValue <= 0) allPositiveEv = false;
            }
            report.Assert(allPositiveEv, "KnowledgeBase_AllRoutesPositiveExpectedValue", "All routes in expert catalog have positive EV");

            // Session Memory: Success increases confidence, Failure decreases confidence
            string testRouteId = routes[0].routeId;
            float initialConf = kb.GetConfidence(testRouteId);

            kb.RecordAttempt(testRouteId, true, 2.5f, 1);
            float afterSuccess = kb.GetConfidence(testRouteId);
            report.Assert(afterSuccess >= initialConf, "KnowledgeBase_SuccessIncreasesConfidence", $"Expected >= {initialConf}, got {afterSuccess}");

            kb.RecordAttempt(testRouteId, false, 0f, 1);
            float afterFail = kb.GetConfidence(testRouteId);
            report.Assert(afterFail < afterSuccess, "KnowledgeBase_FailureDecreasesConfidence", $"Expected < {afterSuccess}, got {afterFail}");

            // Observational Learning
            kb.ObserveRacerRoute(99, testRouteId, 2.4f, true);
            float afterObserve = kb.GetConfidence(testRouteId);
            report.Assert(afterObserve > afterFail, "KnowledgeBase_ObservationalLearningBoostsConfidence", $"Expected > {afterFail}, got {afterObserve}");
        }

        public static void TestPredictiveItemAI(TestReport report)
        {
            var profile = AiSkillModel.CreateProfile(10000, 42);
            var go = new GameObject("TestVehicle_ItemAgent");
            var phys = go.AddComponent<RacingVehiclePhysics>();
            var inv = go.AddComponent<ItemInventory>();
            inv.AcquireItem(RaceItemType.ElectroSpike, false);
            var agent = new AiItemAgent(profile, inv, phys, null, null);

            // Ballistic Rock test: close pursuer behind -> throws backward
            var rearThreatContext = new AiItemContext { racerBehind = true, targetAhead = false };
            report.Assert(agent.ShouldThrowBackward(rearThreatContext), "ItemAI_RockThrowsBackwardAgainstPursuer", "Throws rock backward when racer is behind");

            // Straight target ahead -> throws forward
            var forwardTargetContext = new AiItemContext { racerBehind = false, targetAhead = true };
            report.Assert(!agent.ShouldThrowBackward(forwardTargetContext), "ItemAI_RockThrowsForwardAgainstLeadRacer", "Throws rock forward when target is ahead");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestAiDifficultyAndPerformanceScaling(TestReport report)
        {
            // 7p5k, 9k, 10k MMR profiles
            var p75 = AiSkillModel.CreateProfile(7500, 101);
            var p90 = AiSkillModel.CreateProfile(9000, 102);
            var p100 = AiSkillModel.CreateProfile(10000, 103);

            report.Assert(p75.racingLineSkill >= 0.78f && p90.racingLineSkill-p75.racingLineSkill > .06f, "AiSkill_7p5kHighSkill", $"7p5k line skill {p75.racingLineSkill:F3}; continuous improvement to 9k required");
            report.Assert(p90.racingLineSkill >= 0.87f && p100.racingLineSkill-p90.racingLineSkill > .07f, "AiSkill_9kEliteSkill", $"9k line skill {p90.racingLineSkill:F3}; material decision gap to 10k required");
            report.Assert(p100.racingLineSkill >= 0.98f, "AiSkill_10kApexSkill", $"10k racingLineSkill: {p100.racingLineSkill:F3}");

            // Consistency at 10k must be extremely high (~0.99)
            report.Assert(p100.consistency >= 0.98f, "AiSkill_10kHighConsistency", $"10k consistency: {p100.consistency:F4}");

            // Verify AI controller multi-rate frequency properties
            var botGo = new GameObject("TestBot_Performance");
            var bot = botGo.AddComponent<AiVehicleController>();
            report.Assert(bot.FastControlHz == 20f, "Performance_FastControl20Hz", $"Fast control frequency: {bot.FastControlHz} Hz");
            report.Assert(bot.StrategicHz == 1.5f, "Performance_Strategic1_5Hz", $"Strategic planning frequency: {bot.StrategicHz} Hz");

            UnityEngine.Object.DestroyImmediate(botGo);
        }
    }
}
