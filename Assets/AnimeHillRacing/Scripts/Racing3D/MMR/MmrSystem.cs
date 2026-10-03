using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class MmrSystem
    {
        public const int DefaultStartingMmr = 1000;
        public const int MinimumMmr = 0;
        public const int TargetMaxMmr = 100000;
        public const int CoreCapMmr = 100000;

        /// <summary>
        /// Calculates the dynamic K-factor based on player MMR.
        /// Early progression is responsive; apex tiers require consistent performance.
        /// </summary>
        public static float GetKFactor(int mmr)
        {
            if (mmr < 2500) return 32f;
            if (mmr < 10000) return 28f;
            if (mmr < 30000) return 24f;
            if (mmr < 60000) return 20f;
            if (mmr < TargetMaxMmr) return 16f;
            // Uncapped > 100,000: smooth damping to maintain competitive stability
            float apexSurplus = mmr - TargetMaxMmr;
            return Mathf.Max(8f, 14f - (apexSurplus / 25000f));
        }

        /// <summary>
        /// Calculates the expected win probability of racer A (mmrA) against racer B (mmrB).
        /// Standard Elo logistic distribution.
        /// </summary>
        public static double CalculateExpectedScore(double mmrA, double mmrB)
        {
            return 1.0 / (1.0 + Math.Pow(10.0, (mmrB - mmrA) / 400.0));
        }

        /// <summary>
        /// Computes MMR delta for a racer based on 12-player tournament placement.
        /// Evaluates 11 pairwise matchups against all other participants in the lobby.
        /// </summary>
        /// <param name="racerIndex">Index of the racer in the results array</param>
        /// <param name="racerMmr">Current MMR of the racer</param>
        /// <param name="racerFinishPosition">Finish position 1 (1st) to 12 (12th)</param>
        /// <param name="allRacersMmr">Array of MMRs for all racers in the race</param>
        /// <param name="allFinishPositions">Array of finish positions for all racers</param>
        /// <returns>Integer MMR delta (+/-)</returns>
        public static int CalculateMmrDelta(
            int racerIndex,
            int racerMmr,
            int racerFinishPosition,
            int[] allRacersMmr,
            int[] allFinishPositions)
        {
            if (allRacersMmr == null || allFinishPositions == null || allRacersMmr.Length <= 1)
                return 0;

            int n = allRacersMmr.Length;
            double totalActualScore = 0.0;
            double totalExpectedScore = 0.0;
            int opponentsCount = 0;

            for (int j = 0; j < n; j++)
            {
                if (j == racerIndex) continue;

                opponentsCount++;
                double expectedAgainstJ = CalculateExpectedScore(racerMmr, allRacersMmr[j]);
                totalExpectedScore += expectedAgainstJ;

                int opponentPosition = allFinishPositions[j];
                double actualAgainstJ;
                if (racerFinishPosition < opponentPosition)
                {
                    actualAgainstJ = 1.0; // Beat opponent
                }
                else if (racerFinishPosition == opponentPosition)
                {
                    actualAgainstJ = 0.5; // Tie
                }
                else
                {
                    actualAgainstJ = 0.0; // Lost to opponent
                }
                totalActualScore += actualAgainstJ;
            }

            if (opponentsCount == 0) return 0;

            float k = GetKFactor(racerMmr);

            // Pairwise Elo sum: net difference between actual points and expected points
            double rawDelta = (totalActualScore - totalExpectedScore) * (k / opponentsCount) * 11.0f;

            // Apply lobby strength bonus / penalty:
            // Compare average lobby MMR to player MMR
            double avgLobbyMmr = 0;
            for (int j = 0; j < n; j++) avgLobbyMmr += allRacersMmr[j];
            avgLobbyMmr /= n;

            double lobbyDifficultyRatio = (avgLobbyMmr - racerMmr) / 1000.0; // e.g. +0.5 if lobby is 500 MMR harder
            // If lobby is tougher and player wins, reward extra; if lobby is easier and player loses, penalize extra
            if (rawDelta > 0 && lobbyDifficultyRatio > 0)
            {
                rawDelta *= (1.0 + Math.Min(0.5, lobbyDifficultyRatio * 0.25));
            }
            else if (rawDelta < 0 && lobbyDifficultyRatio < 0)
            {
                rawDelta *= (1.0 + Math.Min(0.5, Math.Abs(lobbyDifficultyRatio) * 0.25));
            }

            int finalDelta = (int)Math.Round(rawDelta, MidpointRounding.AwayFromZero);

            // Safety check: winning 1st place in any legitimate lobby should never lose MMR
            if (racerFinishPosition == 1 && finalDelta < 15)
            {
                finalDelta = 15;
            }
            // 12th place in any legitimate lobby should never gain MMR
            if (racerFinishPosition == n && finalDelta > -15)
            {
                finalDelta = -15;
            }

            return finalDelta;
        }

        /// <summary>
        /// Applies delta to MMR with safe non-negative clamping and uncapped growth.
        /// </summary>
        public static int ApplyDelta(int currentMmr, int delta)
        {
            long newMmr = (long)currentMmr + delta;
            if (newMmr < MinimumMmr) newMmr = MinimumMmr;
            if (newMmr > int.MaxValue) newMmr = int.MaxValue;
            return (int)newMmr;
        }

        public static float CalculateAverageMmr(int[] mmrs)
        {
            if (mmrs == null || mmrs.Length == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < mmrs.Length; i++) sum += mmrs[i];
            return sum / mmrs.Length;
        }
    }
}
