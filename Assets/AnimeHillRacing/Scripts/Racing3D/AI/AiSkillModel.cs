using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public static class AiSkillModel
    {
        // Decisions improve with rating; vehicle physics never consumes these fields.
        public static AiRacerProfile CreateProfile(int mmr, int personalitySeed)
        {
            float t = Mathf.Pow(Mathf.Clamp01(Mathf.Max(0, mmr) / 100000f), .86f);
            float apex = mmr > 100000 ? .025f * (1 - Mathf.Exp(-(mmr - 100000f) / 60000f)) : 0;
            var rng = new DeterministicRng(personalitySeed == 0 ? 99991 : personalitySeed);
            float aggression = rng.Range(-.14f, .14f), risk = rng.Range(-.10f, .10f), temperament = rng.Range(-.04f, .04f);
            float variation = Mathf.Lerp(.045f, .012f, t);
            float Skill(float start, float end, float exponent = 1f)
                => Mathf.Clamp(Mathf.Lerp(start, end, Mathf.Pow(t, exponent)) + apex + rng.Range(-variation, variation), .02f, .985f);
            return new AiRacerProfile
            {
                mmr = Mathf.Max(0, mmr), personalitySeed = personalitySeed,
                racingLineSkill = Skill(.12f, .96f), brakingSkill = Skill(.10f, .97f), throttleSkill = Skill(.12f, .98f),
                driftSkill = Skill(.02f, .97f, 1.2f), overtakingSkill = Skill(.08f, .96f, 1.08f), recoverySkill = Skill(.18f, .97f, .85f),
                shortcutKnowledge = Skill(.01f, .97f, 1.35f), shortcutExecution = Skill(.04f, .97f, 1.25f),
                itemKnowledge = Skill(.10f, .97f), itemTiming = Skill(.06f, .97f, 1.25f),
                defensiveAwareness = Skill(.06f, .96f, 1.18f), offensiveAwareness = Skill(.07f, .96f, 1.12f),
                aggression = Mathf.Clamp(Mathf.Lerp(.30f, .70f, t) + aggression, .15f, .90f),
                consistency = Mathf.Clamp(Mathf.Lerp(.30f, .965f, t) + apex + temperament * (1 - .8f * t), .20f, .985f),
                riskTolerance = Mathf.Clamp(Mathf.Lerp(.20f, .76f, t) + risk, .10f, .90f),
                awareness = Mathf.Clamp(Mathf.Lerp(.18f, .97f, t) + apex, .10f, .985f),
                reactionSkill = Mathf.Clamp(Mathf.Lerp(.20f, .97f, t) + apex, .10f, .985f),
                trickSkill = Skill(.03f, .96f, 1.15f), chainingSkill = Skill(.02f, .96f, 1.3f)
            };
        }
    }
}
