using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class MmrSystem
    {
        public const int DefaultStartingMmr = 0;
        public const int MinimumMmr = 0;
        public const int TargetMaxMmr = 10000;
        public const int CoreCapMmr = 10000;

        /// <summary>
        /// Calculates the dynamic K-factor based on player MMR.
        /// Early progression is responsive; apex tiers require consistent performance.
        /// </summary>
        public static float GetKFactor(int mmr)
        {
            if (mmr < 250) return 32f;
            if (mmr < 1000) return 28f;
            if (mmr < 3000) return 24f;
            if (mmr < 6000) return 20f;
            if (mmr < TargetMaxMmr) return 16f;
            // Uncapped > 10,000: smooth damping to maintain competitive stability
            float apexSurplus = mmr - TargetMaxMmr;
            return Mathf.Max(8f, 14f - (apexSurplus / 2500f));
        }

        /// <summary>
        /// Calculates the expected win probability of racer A (mmrA) against racer B (mmrB).
        /// Standard Elo logistic distribution.
        /// </summary>
        public static double CalculateExpectedScore(double mmrA, double mmrB)
        {
            return 1.0 / (1.0 + Math.Pow(10.0, (mmrB - mmrA) / 1100.0));
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
            if (allRacersMmr == null || allFinishPositions == null || allRacersMmr.Length < 2 ||
                allFinishPositions.Length != allRacersMmr.Length || racerIndex < 0 || racerIndex >= allRacersMmr.Length) return 0;
            int[] ratings = (int[])allRacersMmr.Clone(), positions = (int[])allFinishPositions.Clone(), output = new int[ratings.Length];
            ratings[racerIndex] = racerMmr; positions[racerIndex] = racerFinishPosition;
            float[] uncertainty = new float[ratings.Length];
            for (int i = 0; i < uncertainty.Length; i++) uncertainty[i] = .5f;
            LocalLeagueEngine.CalculateDeltas(ratings, positions, uncertainty, new LocalLeagueConfiguration(), output);
            return output[racerIndex];
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
