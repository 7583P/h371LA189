using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public static class AiSkillModel
    {
        // Decisions improve with rating; vehicle physics never consumes these fields.
        public static AiRacerProfile CreateProfile(int mmr, int personalitySeed)
        {
            float t = ExecutionCurve(mmr);
            float apex = mmr > 100000 ? .0015f * (1 - Mathf.Exp(-(mmr - 100000f) / 60000f)) : 0;
            var rng = new DeterministicRng(personalitySeed == 0 ? 99991 : personalitySeed);
            float aggression = rng.Range(-.14f, .14f), risk = rng.Range(-.10f, .10f), temperament = rng.Range(-.04f, .04f);
            float variation = Mathf.Lerp(.045f, .002f, t);
            float Skill(float start, float end, float exponent = 1f)
                => Mathf.Clamp(Mathf.Lerp(start, end, Mathf.Pow(t, exponent)) + apex + rng.Range(-variation, variation), .02f, .999f);
            return new AiRacerProfile
            {
                mmr = Mathf.Max(0, mmr), personalitySeed = personalitySeed,
                racingLineSkill = Skill(.12f, .996f), brakingSkill = Skill(.10f, .997f), throttleSkill = Skill(.12f, .998f),
                driftSkill = Skill(.02f, .995f, 1.08f), overtakingSkill = Skill(.08f, .996f), recoverySkill = Skill(.18f, .997f, .85f),
                shortcutKnowledge = Skill(.01f, .997f, 1.08f), shortcutExecution = Skill(.04f, .995f, 1.08f),
                itemKnowledge = Skill(.10f, .997f), itemTiming = Skill(.06f, .995f, 1.08f),
                defensiveAwareness = Skill(.06f, .995f), offensiveAwareness = Skill(.07f, .996f),
                aggression = Mathf.Clamp(Mathf.Lerp(.30f, .94f, t) + aggression * (1 - .75f * t), .15f, .985f),
                consistency = Mathf.Clamp(Mathf.Lerp(.30f, .996f, t) + apex + temperament * (1 - .95f * t), .20f, .999f),
                riskTolerance = Mathf.Clamp(Mathf.Lerp(.20f, .94f, t) + risk * (1 - .8f * t), .10f, .98f),
                awareness = Mathf.Clamp(Mathf.Lerp(.18f, .997f, t) + apex, .10f, .999f),
                reactionSkill = Mathf.Clamp(Mathf.Lerp(.20f, .997f, t) + apex, .10f, .999f),
                trickSkill = Skill(.03f, .995f, 1.08f), chainingSkill = Skill(.02f, .995f, 1.08f)
            };
        }

        static float ExecutionCurve(int mmr)
        {
            if (mmr <= 1000) return Mathf.Lerp(0, .06f, Mathf.Clamp01(mmr / 1000f));
            if (mmr <= 10000) return Mathf.Lerp(.06f, .28f, (mmr - 1000f) / 9000f);
            if (mmr <= 25000) return Mathf.Lerp(.28f, .55f, (mmr - 10000f) / 15000f);
            if (mmr <= 50000) return Mathf.Lerp(.55f, .92f, (mmr - 25000f) / 25000f);
            if (mmr <= 75000) return Mathf.Lerp(.92f, .985f, (mmr - 50000f) / 25000f);
            return Mathf.Lerp(.985f, 1, Mathf.Clamp01((mmr - 75000f) / 25000f));
        }
    }
}
