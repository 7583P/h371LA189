using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class AiSkillModel
    {
        /// <summary>
        /// Continuously evaluates and creates an AiRacerProfile for a given MMR and personality seed.
        /// Handles smooth interpolation from 0 MMR to 100,000 MMR and uncapped 100,000+.
        /// </summary>
        public static AiRacerProfile CreateProfile(int mmr, int personalitySeed)
        {
            float safeMmr = Mathf.Max(0f, mmr);

            // Normalized baseline progression across 0 to 100,000 MMR
            float t = Mathf.Clamp01(Mathf.Pow(safeMmr / 100000f, 0.42f));

            // Uncapped apex factor above 100,000
            float apexFactor = 0f;
            if (safeMmr > 100000f)
            {
                apexFactor = Mathf.Min(0.08f, (safeMmr - 100000f) / 100000f);
            }

            // Deterministic pseudo-random personality offsets
            var rng = new DeterministicRng(personalitySeed == 0 ? 99991 : personalitySeed);

            // Base continuous skill curves
            float baseLine = Mathf.Pow(t, 0.80f) + apexFactor;
            float baseBrake = Mathf.Pow(t, 0.90f) + apexFactor;
            float baseThrottle = Mathf.Pow(t, 0.85f) + apexFactor;
            float baseDrift = Mathf.Pow(t, 1.20f) + apexFactor; // Drifting emerges at mid MMR
            float baseOvertake = Mathf.Pow(t, 0.95f) + apexFactor;
            float baseRecovery = Mathf.Pow(t, 0.75f) + apexFactor;

            // Track knowledge (separated from driving mechanics)
            float baseShortcutKnowledge = Mathf.Pow(t, 1.45f) + apexFactor;
            float baseShortcutExecution = Mathf.Pow(t, 1.25f) + apexFactor;

            // Item intelligence curves
            float baseItemKnowledge = Mathf.Pow(t, 0.90f) + apexFactor;
            float baseItemTiming = Mathf.Pow(t, 1.35f) + apexFactor;
            float baseDefensive = Mathf.Pow(t, 1.30f) + apexFactor;
            float baseOffensive = Mathf.Pow(t, 1.05f) + apexFactor;

            // Bounded personality variations (+/- 0.08 variance)
            float aggressionVar = rng.Range(-0.15f, 0.15f);
            float riskVar = rng.Range(-0.12f, 0.12f);
            float consistencyVar = rng.Range(-0.08f, 0.08f);

            // High MMR bots have naturally higher consistency (fewer erratic mistakes)
            float baseConsistency = Mathf.Lerp(0.35f, 0.96f, t) + apexFactor + consistencyVar;
            float baseAggression = Mathf.Clamp01(0.5f + aggressionVar);
            float baseRisk = Mathf.Clamp01(0.4f + riskVar);
            float baseAwareness = Mathf.Lerp(0.30f, 0.95f, t) + apexFactor;
            float baseReaction = Mathf.Lerp(0.35f, 0.98f, t) + apexFactor;

            return new AiRacerProfile
            {
                mmr = mmr,
                personalitySeed = personalitySeed,

                racingLineSkill = Mathf.Clamp(baseLine + rng.Range(-0.04f, 0.04f), 0.05f, 1f),
                brakingSkill = Mathf.Clamp(baseBrake + rng.Range(-0.04f, 0.04f), 0.05f, 1f),
                throttleSkill = Mathf.Clamp(baseThrottle + rng.Range(-0.04f, 0.04f), 0.05f, 1f),
                driftSkill = Mathf.Clamp(baseDrift + rng.Range(-0.05f, 0.05f), 0.02f, 1f),
                overtakingSkill = Mathf.Clamp(baseOvertake + rng.Range(-0.05f, 0.05f), 0.05f, 1f),
                recoverySkill = Mathf.Clamp(baseRecovery + rng.Range(-0.04f, 0.04f), 0.10f, 1f),

                shortcutKnowledge = Mathf.Clamp(baseShortcutKnowledge + rng.Range(-0.06f, 0.06f), 0.0f, 1f),
                shortcutExecution = Mathf.Clamp(baseShortcutExecution + rng.Range(-0.05f, 0.05f), 0.05f, 1f),

                itemKnowledge = Mathf.Clamp(baseItemKnowledge + rng.Range(-0.04f, 0.04f), 0.10f, 1f),
                itemTiming = Mathf.Clamp(baseItemTiming + rng.Range(-0.06f, 0.06f), 0.05f, 1f),
                defensiveAwareness = Mathf.Clamp(baseDefensive + rng.Range(-0.05f, 0.05f), 0.05f, 1f),
                offensiveAwareness = Mathf.Clamp(baseOffensive + rng.Range(-0.05f, 0.05f), 0.05f, 1f),

                aggression = Mathf.Clamp01(baseAggression),
                consistency = Mathf.Clamp01(baseConsistency),
                riskTolerance = Mathf.Clamp01(baseRisk),
                awareness = Mathf.Clamp01(baseAwareness),
                reactionSkill = Mathf.Clamp01(baseReaction)
            };
        }
    }
}
