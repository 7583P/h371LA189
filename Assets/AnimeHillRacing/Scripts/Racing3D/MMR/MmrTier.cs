using System;

namespace AnimeHillRacing.Racing3D
{
    public enum MmrTier
    {
        RookieI, RookieII, ScoutI, ScoutII, ChallengerI, ChallengerII,
        ProI, ProII, ExpertI, ExpertII, EliteI, EliteII, MasterI, MasterII,
        GrandmasterI, GrandmasterII, TitanI, TitanII, ApexI, ApexII, ApexBeyond
    }

    public static class MmrTierUtility
    {
        public const int MaxTargetMmr = 10000;

        public const int DivisionWidth = 500;
        static readonly string[] Names = {
            "ROOKIE I", "ROOKIE II", "SCOUT I", "SCOUT II", "CHALLENGER I", "CHALLENGER II",
            "PRO I", "PRO II", "EXPERT I", "EXPERT II", "ELITE I", "ELITE II", "MASTER I", "MASTER II",
            "GRANDMASTER I", "GRANDMASTER II", "TITAN I", "TITAN II", "APEX I", "APEX II", "APEX BEYOND 10K"
        };

        // Saves store rating rather than enum ordinals, so legacy rank names cannot survive migration.
        public static MmrTier GetTier(int mmr) => (MmrTier)(mmr > MaxTargetMmr ? 20 : Math.Min(19, Math.Max(0, mmr) / DivisionWidth));
        public static string GetTierDisplayName(MmrTier tier) => Names[Math.Max(0, Math.Min(20, (int)tier))];
        public static string GetDisplayName(int mmr) => GetTierDisplayName(GetTier(mmr));
        public static int GetBeyondCounter(int mmr) => mmr <= MaxTargetMmr ? 0 : (mmr - MaxTargetMmr) / 1000;
        public static float GetDivisionProgress(int mmr) => mmr > MaxTargetMmr ? ((mmr - MaxTargetMmr) % 1000) / 1000f : mmr == MaxTargetMmr ? 1f : Math.Max(0, mmr) % DivisionWidth / (float)DivisionWidth;
    }
}
