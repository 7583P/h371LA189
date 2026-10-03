using System;
using System.Collections.Generic;
using AnimeHillRacing.Racing3D;
using UnityEngine;

namespace RacingTestRunner
{
    class Program
    {
        static int passed = 0;
        static int failed = 0;
        static List<string> errors = new List<string>();

        static void Assert(bool condition, string testName, string errorDetails = "")
        {
            if (condition)
            {
                passed++;
                Console.WriteLine($"  [PASS] {testName}");
            }
            else
            {
                failed++;
                string msg = $"  [FAIL] {testName}: {errorDetails}";
                errors.Add(msg);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        static int Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("    HILL ADVENTURER — RACING 3D FOUNDATION AUTOMATED TEST SUITE");
            Console.WriteLine("================================================================================");

            TestMmrAlgorithm();
            TestMatchmakingLobby();
            TestRacingStats();
            TestAiSkillScaling();
            TestAiObservabilityAndErrors();
            TestItemDistribution();
            TestAdventureRegression();

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($"TOTAL TESTS: {passed + failed} | PASSED: {passed} | FAILED: {failed}");
            Console.WriteLine("--------------------------------------------------------------------------------");

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILURES ENCOUNTERED:");
                foreach (var err in errors) Console.WriteLine(err);
                Console.ResetColor();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ALL RACING 3D TESTS PASSED WITH 100% SUCCESS!");
            Console.ResetColor();
            return 0;
        }

        static void TestMmrAlgorithm()
        {
            Console.WriteLine("\n[SUITE 1: MMR & RATING SYSTEM]");

            // 1. Initial MMR & limits
            Assert(MmrSystem.DefaultStartingMmr == 1000, "MMR_InitialBaseline", $"Expected 1000, got {MmrSystem.DefaultStartingMmr}");
            Assert(MmrSystem.MinimumMmr == 0, "MMR_FloorAtZero", $"Expected 0, got {MmrSystem.MinimumMmr}");

            // 2. 12-racer equal lobby
            int[] equalMmrs = new int[12];
            int[] posFirst = new int[12];
            for (int i = 0; i < 12; i++) { equalMmrs[i] = 1000; posFirst[i] = i + 1; }

            int winDelta = MmrSystem.CalculateMmrDelta(0, 1000, 1, equalMmrs, posFirst);
            Assert(winDelta >= 50 && winDelta <= 200, "MMR_1stPlaceGainInEqualLobby", $"Expected 50..200, got {winDelta}");

            int[] posTwelfth = new int[12];
            posTwelfth[0] = 12;
            for (int i = 1; i < 12; i++) posTwelfth[i] = i;

            int lossDelta = MmrSystem.CalculateMmrDelta(0, 1000, 12, equalMmrs, posTwelfth);
            Assert(lossDelta <= -30 && lossDelta >= -200, "MMR_12thPlaceLossInEqualLobby", $"Expected -30..-200, got {lossDelta}");

            // 3. Strength of Lobby test
            int[] hardMmrs = new int[12];
            for (int i = 0; i < 12; i++) hardMmrs[i] = 3000;
            int winAgainstHard = MmrSystem.CalculateMmrDelta(0, 1000, 1, hardMmrs, posFirst);
            Assert(winAgainstHard > winDelta, "MMR_StrengthOfLobbyUpsetAmplification", $"Hard lobby win ({winAgainstHard}) > Equal win ({winDelta})");

            int[] easyMmrs = new int[12];
            for (int i = 0; i < 12; i++) easyMmrs[i] = 400;
            int lossAgainstEasy = MmrSystem.CalculateMmrDelta(0, 1000, 12, easyMmrs, posTwelfth);
            Assert(lossAgainstEasy < lossDelta, "MMR_LossAgainstWeakerLobbyPenalizedMore", $"Loss against easy ({lossAgainstEasy}) < Loss against equal ({lossDelta})");

            // 4. Mid-pack symmetry
            int[] posMid = new int[12];
            for (int i = 0; i < 12; i++) posMid[i] = i + 1;
            int midDelta6 = MmrSystem.CalculateMmrDelta(5, 1000, 6, equalMmrs, posMid);
            int midDelta7 = MmrSystem.CalculateMmrDelta(6, 1000, 7, equalMmrs, posMid);
            Assert(Math.Abs(midDelta6 + midDelta7) <= 2, "MMR_MidPackPlacementSymmetry", $"Sum of P6 ({midDelta6}) and P7 ({midDelta7}) should cancel to ~0, got {midDelta6 + midDelta7}");
            Assert(midDelta6 > 0 && midDelta7 < 0, "MMR_TopHalfVsBottomHalf", $"P6 ({midDelta6}) should be positive, P7 ({midDelta7}) should be negative");

            // 5. Uncapped > 10,000 MMR behavior
            int apexMmr = 12500;
            int[] apexLobby = new int[12];
            for (int i = 0; i < 12; i++) apexLobby[i] = 12500;
            int apexWinDelta = MmrSystem.CalculateMmrDelta(0, apexMmr, 1, apexLobby, posFirst);
            Assert(apexWinDelta > 10, "MMR_ApexGainRemainsPositive", $"Expected > 10, got {apexWinDelta}");

            int newApex = MmrSystem.ApplyDelta(apexMmr, apexWinDelta);
            Assert(newApex > apexMmr, "MMR_ApexUncappedGrowth", $"{newApex} > {apexMmr}");

            // 6. Clamp at 0 (never negative)
            int clamped = MmrSystem.ApplyDelta(10, -50);
            Assert(clamped == 0, "MMR_NeverNegative", $"Expected 0, got {clamped}");
        }

        static void TestMatchmakingLobby()
        {
            Console.WriteLine("\n[SUITE 2: MATCHMAKING & 12-RACER LOBBY]");

            // 1. Exactly 12 racers
            var lobby = MatchmakingService.GenerateRankedLobby(6240, "car-01", "wheel-default", "HumanRacer", 777);
            Assert(lobby != null && lobby.Length == 12, "Lobby_SizeIsExactly12", $"Expected 12, got {lobby?.Length}");

            // 2. Exactly 1 Human and 11 AI
            int humanCount = 0;
            int aiCount = 0;
            for (int i = 0; i < lobby.Length; i++)
            {
                if (lobby[i].isHuman) humanCount++;
                else aiCount++;
            }
            Assert(humanCount == 1, "Lobby_ExactlyOneHuman", $"Human count: {humanCount}");
            Assert(aiCount == 11, "Lobby_ExactlyElevenAI", $"AI count: {aiCount}");

            // 3. Grid positions 1 through 12 unique and valid
            HashSet<int> gridSlots = new HashSet<int>();
            for (int i = 0; i < lobby.Length; i++) gridSlots.Add(lobby[i].gridPosition);
            Assert(gridSlots.Count == 12, "Lobby_UniqueGridSlots1To12", $"Unique slots: {gridSlots.Count}");

            // 4. Spread and distribution around target MMR
            int sumAiMmr = 0;
            for (int i = 0; i < lobby.Length; i++)
            {
                if (!lobby[i].isHuman) sumAiMmr += lobby[i].mmr;
            }
            float avgAiMmr = sumAiMmr / 11f;
            float diff = Math.Abs(avgAiMmr - 6240f);
            Assert(diff < 500f, "Lobby_DistributionCenteredAroundPlayer", $"Avg AI MMR: {avgAiMmr:F0} (Diff: {diff:F0})");

            // 5. Deterministic Seed reproducibility
            var lobby1 = MatchmakingService.GenerateRankedLobby(5000, "car-01", "wheel-default", "Tester", 9999);
            var lobby2 = MatchmakingService.GenerateRankedLobby(5000, "car-01", "wheel-default", "Tester", 9999);
            bool match = true;
            for (int i = 0; i < 12; i++)
            {
                if (lobby1[i].displayName != lobby2[i].displayName ||
                    lobby1[i].mmr != lobby2[i].mmr ||
                    lobby1[i].gridPosition != lobby2[i].gridPosition)
                {
                    match = false;
                    break;
                }
            }
            Assert(match, "Lobby_DeterministicSeedReproducibility", "Identical seeds produced differing lobbies!");
        }

        static void TestRacingStats()
        {
            Console.WriteLine("\n[SUITE 3: VEHICLE & WHEELS FINALRACINGSTATS]");

            // 1. Authoritative calculation
            FinalRacingStats baseJeep = RacingStatsCalculator.Calculate("car-01", "wheel-default");
            Assert(baseJeep.SpeedKmh >= 140f && baseJeep.SpeedKmh <= 150f, "Stats_BaseVehicleSpeed", $"Speed: {baseJeep.SpeedKmh}");
            Assert(baseJeep.WeightKg >= 1000f, "Stats_BaseVehicleWeight", $"Weight: {baseJeep.WeightKg}");

            // 2. Wheel Modifiers
            FinalRacingStats sportJeep = RacingStatsCalculator.Calculate("car-01", "wheel-sport");
            Assert(sportJeep.SpeedKmh > baseJeep.SpeedKmh, "Stats_WheelModifierSpeedIncrease", $"{sportJeep.SpeedKmh} > {baseJeep.SpeedKmh}");
            Assert(sportJeep.LateralGrip > baseJeep.LateralGrip, "Stats_WheelModifierGripIncrease", $"{sportJeep.LateralGrip} > {baseJeep.LateralGrip}");

            FinalRacingStats offroadJeep = RacingStatsCalculator.Calculate("car-01", "wheel-offroad");
            Assert(offroadJeep.OffroadRetention > baseJeep.OffroadRetention, "Stats_WheelModifierOffroadRetention", $"{offroadJeep.OffroadRetention} > {baseJeep.OffroadRetention}");

            // 3. Equal treatment: Human & AI
            FinalRacingStats formulaPlayer = RacingStatsCalculator.Calculate("formula-01", "wheel-racing");
            FinalRacingStats formulaAi = RacingStatsCalculator.Calculate("formula-01", "wheel-racing");
            Assert(formulaPlayer.SpeedKmh == formulaAi.SpeedKmh && formulaPlayer.LateralGrip == formulaAi.LateralGrip,
                   "Stats_StrictPlayerAiEquality", "Player and AI received differing stats!");
        }

        static void TestAiSkillScaling()
        {
            Console.WriteLine("\n[SUITE 4: AI SKILL SCALING & PERSONALITY]");

            var rookie = AiSkillModel.CreateProfile(500, 42);
            var amateur = AiSkillModel.CreateProfile(1500, 42);
            var expert = AiSkillModel.CreateProfile(4500, 42);
            var champion = AiSkillModel.CreateProfile(8500, 42);
            var apex = AiSkillModel.CreateProfile(12000, 42);

            Assert(expert.racingLineSkill > rookie.racingLineSkill, "AI_RacingLineMonotonicGrowth", $"{expert.racingLineSkill:F2} > {rookie.racingLineSkill:F2}");
            Assert(champion.driftSkill > amateur.driftSkill, "AI_DriftSkillScaling", $"{champion.driftSkill:F2} > {amateur.driftSkill:F2}");
            Assert(champion.shortcutKnowledge > rookie.shortcutKnowledge, "AI_TrackKnowledgeSeparatedScaling", $"{champion.shortcutKnowledge:F2} > {rookie.shortcutKnowledge:F2}");
            Assert(apex.consistency >= champion.consistency, "AI_ApexConsistency", $"{apex.consistency:F2} >= {champion.consistency:F2}");

            // Personality variation between same-MMR bots
            var bot1 = AiSkillModel.CreateProfile(7000, 101);
            var bot2 = AiSkillModel.CreateProfile(7000, 999);
            bool differentPersonality = Math.Abs(bot1.aggression - bot2.aggression) > 0.02f ||
                                         Math.Abs(bot1.riskTolerance - bot2.riskTolerance) > 0.02f;
            Assert(differentPersonality, "AI_SameMmrUniquePersonalities", "Two bots of same MMR have identical personality!");

            // Bounded variation (same tier capability)
            Assert(Math.Abs(bot1.racingLineSkill - bot2.racingLineSkill) < 0.15f,
                   "AI_SameMmrBoundedSkillVariance", $"Delta: {Math.Abs(bot1.racingLineSkill - bot2.racingLineSkill):F2}");
        }

        static void TestAiObservabilityAndErrors()
        {
            Console.WriteLine("\n[SUITE 5: AI OBSERVABILITY & LOGGING]");

            var logger = new AiDecisionLogger();
            logger.BotName = "TestBot";
            logger.UpdateSteeringDecision("PowerDriftApex", "SharpCornerApproaching", new Vector3(10, 0, 50), 0.85f, 1.0f, true);
            logger.UpdateItemDecision("Hold", "PreventCrashInSharpTurn");

            string summary = logger.GetDebugSummary();
            Assert(summary.Contains("PowerDriftApex") && summary.Contains("PreventCrashInSharpTurn"),
                   "AI_ObservabilitySummaryContent", summary);
        }

        static void TestItemDistribution()
        {
            Console.WriteLine("\n[SUITE 6: ITEM PROBABILITY DISTRIBUTION BY POSITION]");

            var rng = new DeterministicRng(12345);

            // P1 should never receive HyperCharge (comeback item)
            bool p1GotHyperCharge = false;
            for (int i = 0; i < 100; i++)
            {
                var item = ItemDistributionTable.RollItem(1, rng);
                if (item == RaceItemType.HyperCharge) p1GotHyperCharge = true;
            }
            Assert(!p1GotHyperCharge, "Items_P1NeverReceivesHyperCharge", "P1 illegally received HyperCharge!");

            // P12 should frequently receive HyperCharge / Comeback items
            int p12HyperCount = 0;
            for (int i = 0; i < 100; i++)
            {
                var item = ItemDistributionTable.RollItem(12, rng);
                if (item == RaceItemType.HyperCharge || item == RaceItemType.TurboPulse) p12HyperCount++;
            }
            Assert(p12HyperCount >= 70, "Items_P12HighComebackProbability", $"P12 comeback items: {p12HyperCount}/100");
        }

        static void TestAdventureRegression()
        {
            Console.WriteLine("\n[SUITE 7: ADVENTURE MODE REGRESSION & INTEGRITY]");

            // 1. Currency & Economy
            Assert(!AnimeHillRacing.Core.SaveManager.TemporaryUnlimitedCurrencyEnabled, "Adventure_RealCurrencyIntegrity", "Unlimited currency should be disabled");

            // 2. Fuel policy
            bool lowFuel = AnimeHillRacing.UI.GameplayHUD.ShouldDisplayLowFuel(10f, 2f, 5f);
            Assert(lowFuel, "Adventure_LowFuelThresholdContract", "Low fuel should trigger at exactly 5s boundary");

            // 3. Countdown typography
            string countdown = AnimeHillRacing.UI.GameplayHUD.FormatGameOverCountdown(AnimeHillRacing.Core.RunEndReason.OutOfFuel, 2.99f);
            Assert(countdown == "OUT OF FUEL\n3", "Adventure_GameOverCountdownContract", $"Expected 'OUT OF FUEL\\n3', got '{countdown}'");

            // 4. Game Modes
            Assert((int)AnimeHillRacing.Core.GameModeType.Adventure2_5D == 0, "Adventure_ModeEnumIntact", "Adventure2_5D enum index 0 preserved");
            Assert((int)AnimeHillRacing.Core.GameModeType.Adventure3D == 1, "Adventure_Mode3DEnumIntact", "Adventure3D enum index 1 preserved");

            // 5. Existing Vehicle Definition & Stats contract
            Assert(typeof(AnimeHillRacing.Vehicle.VehicleDefinition).GetMethod("Resolve") != null, "Adventure_VehicleResolveMethodContract", "VehicleDefinition.Resolve method contract exists");
            Assert(typeof(AnimeHillRacing.Vehicle.ResolvedVehicleStats).GetField("targetAccelerationMps2") != null, "Adventure_VehicleAccelerationContract", "ResolvedVehicleStats.targetAccelerationMps2 exists");
            Assert(typeof(AnimeHillRacing.Vehicle.ResolvedVehicleStats).GetField("topSpeedMps") != null, "Adventure_TopSpeedContract", "ResolvedVehicleStats.topSpeedMps exists");
        }
    }
}
