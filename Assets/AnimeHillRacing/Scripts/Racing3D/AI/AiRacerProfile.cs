using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class AiRacerProfile
    {
        public int mmr;
        public int personalitySeed;

        [Header("Driving Skills (0.0 to 1.0)")]
        [Range(0f, 1f)] public float racingLineSkill;
        [Range(0f, 1f)] public float brakingSkill;
        [Range(0f, 1f)] public float throttleSkill;
        [Range(0f, 1f)] public float driftSkill;
        [Range(0f, 1f)] public float overtakingSkill;
        [Range(0f, 1f)] public float recoverySkill;

        [Header("Track Knowledge")]
        [Range(0f, 1f)] public float shortcutKnowledge;
        [Range(0f, 1f)] public float shortcutExecution;

        [Header("Item Intelligence")]
        [Range(0f, 1f)] public float itemKnowledge;
        [Range(0f, 1f)] public float itemTiming;
        [Range(0f, 1f)] public float defensiveAwareness;
        [Range(0f, 1f)] public float offensiveAwareness;

        [Header("Personality Traits")]
        [Range(0f, 1f)] public float aggression;
        [Range(0f, 1f)] public float consistency;
        [Range(0f, 1f)] public float riskTolerance;
        [Range(0f, 1f)] public float awareness;
        [Range(0f, 1f)] public float reactionSkill;

        public override string ToString() =>
            $"MMR: {mmr} | Line: {racingLineSkill:F2} | Drift: {driftSkill:F2} | Shortcuts: {shortcutKnowledge:F2} | Consistency: {consistency:F2}";
    }
}
