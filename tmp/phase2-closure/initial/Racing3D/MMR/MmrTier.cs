using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum MmrTier
    {
        Rookie = 0,       // 0–999
        Amateur = 1,      // 1000–1999
        Competent = 2,    // 2000–2999
        Advanced = 3,     // 3000–3999
        Expert = 4,       // 4000–4999
        Elite = 5,        // 5000–5999
        Master = 6,       // 6000–6999
        Grandmaster = 7,  // 7000–7999
        Champion = 8,     // 8000–8999
        Legend = 9,       // 9000–9999
        Apex = 10         // 10000+
    }

    public static class MmrTierUtility
    {
        public static MmrTier GetTier(int mmr)
        {
            if (mmr < 1000) return MmrTier.Rookie;
            if (mmr < 2000) return MmrTier.Amateur;
            if (mmr < 3000) return MmrTier.Competent;
            if (mmr < 4000) return MmrTier.Advanced;
            if (mmr < 5000) return MmrTier.Expert;
            if (mmr < 6000) return MmrTier.Elite;
            if (mmr < 7000) return MmrTier.Master;
            if (mmr < 8000) return MmrTier.Grandmaster;
            if (mmr < 9000) return MmrTier.Champion;
            if (mmr < 10000) return MmrTier.Legend;
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
