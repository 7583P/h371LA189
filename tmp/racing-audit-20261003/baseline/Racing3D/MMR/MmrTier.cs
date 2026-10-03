using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum MmrTier
    {
        Rookie = 0,       // 0–9,999
        Amateur = 1,      // 10,000–19,999
        Competent = 2,    // 20,000–29,999
        Advanced = 3,     // 30,000–39,999
        Expert = 4,       // 40,000–49,999
        Elite = 5,        // 50,000–59,999
        Master = 6,       // 60,000–69,999
        Grandmaster = 7,  // 70,000–79,999
        Champion = 8,     // 80,000–89,999
        Legend = 9,       // 90,000–99,999
        Apex = 10         // 100,000+
    }

    public static class MmrTierUtility
    {
        public const int MaxTargetMmr = 100000;

        public static MmrTier GetTier(int mmr)
        {
            if (mmr < 10000) return MmrTier.Rookie;
            if (mmr < 20000) return MmrTier.Amateur;
            if (mmr < 30000) return MmrTier.Competent;
            if (mmr < 40000) return MmrTier.Advanced;
            if (mmr < 50000) return MmrTier.Expert;
            if (mmr < 60000) return MmrTier.Elite;
            if (mmr < 70000) return MmrTier.Master;
            if (mmr < 80000) return MmrTier.Grandmaster;
            if (mmr < 90000) return MmrTier.Champion;
            if (mmr < 100000) return MmrTier.Legend;
            return MmrTier.Apex;
        }

        public static string GetTierDisplayName(MmrTier tier)
        {
            switch (tier)
            {
                case MmrTier.Rookie: return "ROOKIE";
                case MmrTier.Amateur: return "AMATEUR";
                case MmrTier.Competent: return "COMPETENT";
                case MmrTier.Advanced: return "ADVANCED";
                case MmrTier.Expert: return "EXPERT";
                case MmrTier.Elite: return "ELITE";
                case MmrTier.Master: return "MASTER";
                case MmrTier.Grandmaster: return "GRANDMASTER";
                case MmrTier.Champion: return "CHAMPION";
                case MmrTier.Legend: return "LEGEND";
                case MmrTier.Apex: return "APEX";
                default: return "RACER";
            }
        }
    }
}
