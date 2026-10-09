using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class AiSkillModel
    {
        // Decisions improve with rating; vehicle physics never consumes these fields.
        // Pure decision-making and route execution intelligence.
        public static AiRacerProfile CreateProfile(int mmr, int personalitySeed)
        {
            float t = Normalize(mmr);
            float apex = Mathf.Pow(Mathf.InverseLerp(.90f, 1f, t), 2f) * .035f;
            var rng = new DeterministicRng(personalitySeed == 0 ? 99991 : personalitySeed);
            float aggression = rng.Range(-.08f, .08f), risk = rng.Range(-.06f, .06f), temperament = rng.Range(-.02f, .02f);
            float variation = Mathf.Lerp(.035f, .003f, t);
            float Skill(float start, float end, float exponent = 1f)
                => Mathf.Clamp(Mathf.Lerp(start, end, Mathf.Pow(t, exponent)) + apex - rng.Range(.001f, variation), 0f, .999f);

            return new AiRacerProfile
            {
                mmr = Mathf.Max(0, mmr),
                personalitySeed = personalitySeed,
                racingLineSkill = Skill(.40f, .958f),
                brakingSkill = Skill(.32f, .958f),
                throttleSkill = Skill(.40f, .958f),
                driftSkill = Skill(.08f, .958f, 1.25f),
                overtakingSkill = Skill(.42f, .998f),
                recoverySkill = Skill(.55f, .999f, .90f),
                shortcutKnowledge = Skill(.05f, .958f, 1.1f),
                shortcutExecution = Skill(.15f, .958f, 1.2f),
                itemKnowledge = Skill(.35f, .999f),
                itemTiming = Skill(.30f, .998f, 1.02f),
                defensiveAwareness = Skill(.30f, .998f),
                offensiveAwareness = Skill(.35f, .999f),
                aggression = Mathf.Clamp(Mathf.Lerp(.40f, .95f, t) + aggression * (1 - .65f * t), .25f, .99f),
                consistency = Mathf.Clamp(Mathf.Lerp(.52f, .958f, t) + apex + temperament * (1 - .85f * t), .40f, .999f),
                riskTolerance = Mathf.Clamp(Mathf.Lerp(.25f, .93f, t) + risk * (1 - .65f * t), .20f, .98f),
                awareness = Mathf.Clamp(Mathf.Lerp(.25f, .958f, t) + apex, .20f, .999f),
                reactionSkill = Mathf.Clamp(Mathf.Lerp(.15f, .958f, t) + apex, .10f, .999f),
                trickSkill = Skill(.01f, .958f, 1.45f),
                chainingSkill = Skill(.01f, .958f, 2.4f),
                style = (AiRacingStyle)(Mathf.Abs(personalitySeed % 6))
            };
        }

        public static float Normalize(int mmr) => Mathf.Clamp01(mmr / 10000f);
    }
}
