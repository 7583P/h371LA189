using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    public enum TerrainSectionKind
    {
        Fast,
        Rolling,
        Technical,
        Recovery
    }

    public enum TerrainFeatureType
    {
        FlatRecovery,
        SoftHill,
        MediumHill,
        LongClimb,
        LongDescent,
        Valley,
        Crest,
        RollingSection,
        JumpableCrest
    }

    public enum NeonTemplateSection
    {
        Warmup,
        RollingHills,
        GradualDescent,
        BumpyFlat,
        Staircases,
        RollingDescent,
        DoubleHill,
        TechBumps,
        Recovery
    }

    public struct LiquidChallengeLayout
    {
        public int cycleIndex;
        public int challengeIndex;
        public float runUpStart;
        public float transitionStart;
        public float rampStart;
        public float poolStart;
        public float poolEnd;
        public float landingEnd;
        public float recoveryEnd;
        public float baselineHeight;
        public float rampHeight;
        public float poolLength;
        public float liquidSurfaceY;
    }

    public static class TerrainGenerator
    {
        private const float TwoPi = Mathf.PI * 2f;
        public const int NeonTemplateSectionCount = 9;

        // Exact authored order inside every 6000 m cycle:
        // Warmup -> Rolling Hills -> Gradual Descent -> Bumpy Flat -> Staircases ->
        // Rolling Descent -> Double Hill -> Tech Bumps -> Recovery.
        private static readonly float[] NeonSectionBoundaries =
        {
            0f, 600f, 1400f, 2250f, 3000f, 3700f, 4350f, 5050f, 5700f, 6000f
        };

        private static readonly float[][] NeonSectionDistances =
        {
            new[] { 0f, 140f, 260f, 390f, 520f, 600f },
            new[] { 600f, 700f, 800f, 900f, 1000f, 1110f, 1220f, 1400f },
            new[] { 1400f, 1510f, 1610f, 1730f, 1850f, 1970f, 2070f, 2250f },
            new[] { 2250f, 2340f, 2430f, 2520f, 2610f, 2700f, 2790f, 2880f, 3000f },
            new[] { 3000f, 3070f, 3130f, 3200f, 3260f, 3330f, 3390f, 3460f, 3520f, 3590f, 3650f, 3700f },
            new[] { 3700f, 3780f, 3870f, 3960f, 4060f, 4160f, 4250f, 4350f },
            new[] { 4350f, 4430f, 4510f, 4590f, 4670f, 4750f, 4830f, 4910f, 4990f, 5050f },
            new[] { 5050f, 5120f, 5190f, 5260f, 5330f, 5400f, 5470f, 5540f, 5610f, 5700f },
            new[] { 5700f, 5780f, 5860f, 5930f, 6000f }
        };

        private static readonly float[][] NeonSectionHeights =
        {
            new[] { 0f, .025f, -.018f, .05f, .025f, .10f },
            new[] { .10f, .58f, -.08f, .70f, -.16f, .75f, -.22f, .42f },
            new[] { .42f, .08f, .48f, -.12f, .28f, -.38f, .06f, -.58f },
            new[] { -.58f, -.34f, -.62f, -.30f, -.66f, -.26f, -.64f, -.28f, -.50f },
            new[] { -.50f, -.50f, -.17f, -.17f, .18f, .18f, .55f, .55f, .90f, .90f, .90f, .48f },
            new[] { .48f, .05f, .55f, -.18f, .40f, -.42f, .18f, -.50f },
            new[] { -.50f, -.32f, .40f, .40f, -.34f, -.34f, .38f, .38f, -.24f, -.18f },
            new[] { -.18f, .28f, -.32f, .32f, -.34f, .32f, -.36f, .30f, -.28f, -.12f },
            new[] { -.12f, -.08f, -.03f, .01f, 0f }
        };

        private enum NeonMesoKind { Bump, DoubleBump, Alternating, SoftStep }

        private readonly struct NeonMesoFeature
        {
            public readonly float start;
            public readonly float length;
            public readonly float normalizedHeight;
            public readonly NeonMesoKind kind;

            public NeonMesoFeature(float featureStart, float featureLength, float height,
                NeonMesoKind featureKind)
            {
                start = featureStart;
                length = featureLength;
                normalizedHeight = height;
                kind = featureKind;
            }
        }

        // 60-150 m C1-continuous authored features. They create medium bumps and dips with enough
        // vertices for stable wheel contact; there is no Perlin/micro noise in the Neon template.
        private static readonly NeonMesoFeature[] NeonMesoFeatures =
        {
            new NeonMesoFeature(170f, 120f, .015f, NeonMesoKind.Bump),
            new NeonMesoFeature(410f, 100f, -.018f, NeonMesoKind.Alternating),
            new NeonMesoFeature(680f, 100f, .035f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(980f, 120f, -.025f, NeonMesoKind.Alternating),
            new NeonMesoFeature(1250f, 100f, .030f, NeonMesoKind.Bump),
            new NeonMesoFeature(1500f, 130f, .025f, NeonMesoKind.Alternating),
            new NeonMesoFeature(1820f, 150f, -.030f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(2090f, 100f, .025f, NeonMesoKind.Bump),
            new NeonMesoFeature(2290f, 80f, .045f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(2440f, 70f, -.040f, NeonMesoKind.Bump),
            new NeonMesoFeature(2600f, 95f, .050f, NeonMesoKind.Alternating),
            new NeonMesoFeature(2780f, 80f, .040f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(2900f, 60f, -.035f, NeonMesoKind.Bump),
            new NeonMesoFeature(3320f, 60f, .025f, NeonMesoKind.Bump),
            new NeonMesoFeature(3780f, 80f, .040f, NeonMesoKind.Alternating),
            new NeonMesoFeature(3990f, 90f, -.035f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(4200f, 70f, .040f, NeonMesoKind.Bump),
            new NeonMesoFeature(4630f, 70f, .025f, NeonMesoKind.Bump),
            new NeonMesoFeature(4820f, 80f, -.030f, NeonMesoKind.Alternating),
            new NeonMesoFeature(5080f, 80f, .040f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(5190f, 80f, -.035f, NeonMesoKind.Bump),
            new NeonMesoFeature(5300f, 80f, .040f, NeonMesoKind.Alternating),
            new NeonMesoFeature(5450f, 80f, .040f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(5560f, 80f, -.040f, NeonMesoKind.Alternating),
            new NeonMesoFeature(5620f, 70f, .035f, NeonMesoKind.Bump),
            new NeonMesoFeature(5760f, 100f, .018f, NeonMesoKind.Alternating)
        };

        public static int NeonMesoFeatureCount => NeonMesoFeatures.Length;
        public const float NeonMesoMinimumLength = 60f;
        public const float NeonMesoMaximumLength = 150f;

        public static Vector2 GetNeonSectionRange(NeonTemplateSection section)
        {
            int index = Mathf.Clamp((int)section, 0, NeonTemplateSectionCount - 1);
            return new Vector2(NeonSectionBoundaries[index], NeonSectionBoundaries[index + 1]);
        }

        public static NeonTemplateSection GetNeonTemplateSection(float localDistance)
        {
            float local = Mathf.Clamp(localDistance, 0f,
                LiquidChallengeSettings.RequiredCycleLength - .001f);
            for (int section = 0; section < NeonTemplateSectionCount - 1; section++)
                if (local < NeonSectionBoundaries[section + 1]) return (NeonTemplateSection)section;
            return NeonTemplateSection.Recovery;
        }

        public static float SampleHeight(MapProfile profile, float worldX)
        {
            if (profile != null && profile.supportsLiquidExposure)
                return SampleNeonHeight(profile, worldX);
            return profile != null && profile.elevationProfile != null
                ? profile.elevationProfile.GetHeightAtDistance(worldX)
                : SampleProfileHeight(profile, worldX);
        }

        public static float SampleProfileHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            if (profile.supportsLiquidExposure) return SampleNeonHeight(profile, worldX);
            float distance = Mathf.Max(0f, worldX);
            float difficulty = profile.EvaluateDifficulty(distance);
            float amplitudeProgress = profile.introductoryAmplitude +
                                      difficulty * (1f - profile.introductoryAmplitude);
            float featureLength = Mathf.Max(profile.sectionLength, profile.minimumValleyWidth,
                profile.minimumLandingLength * 1.5f);
            float featurePosition = worldX / featureLength;
            int featureIndex = Mathf.FloorToInt(featurePosition);
            float t = featurePosition - featureIndex;
            float featureDifficulty = profile.EvaluateDifficulty(Mathf.Max(0f, (featureIndex + 0.5f) * featureLength));
            TerrainFeatureType feature = GetFeatureType(profile, featureIndex, featureDifficulty);
            uint seed = CombinedSeed(profile);
            float variant = Hash01(seed, featureIndex * 271 + 37);
            float featureShape = EvaluateFeature(feature, t, variant, Mathf.Clamp01(featureDifficulty),
                profile.technicalDetailStrength);
            float frequency = profile.frequency / Mathf.Max(0.1f, profile.hillSize);
            float phase = Hash01(seed, 73) * TwoPi;
            float broadVariation = Mathf.Sin(worldX * frequency * 0.38f + phase) * 0.12f;
            float perlinVariation = (Mathf.PerlinNoise(seed * 0.000031f + worldX * frequency * 0.24f, 0.417f) * 2f - 1f) * 0.08f;
            float startFlatten = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((worldX + 20f) / 120f));
            return profile.baseHeight + (featureShape + broadVariation + perlinVariation) *
                   profile.amplitude * amplitudeProgress * startFlatten;
        }

        public static int GetCycleIndex(float distance) => Mathf.FloorToInt(
            Mathf.Max(distance, 0f) / LiquidChallengeSettings.RequiredCycleLength);

        public static float GetNeonLiquidLocalRunUp(MapProfile profile, int challengeIndex)
        {
            int index = Mathf.Clamp(challengeIndex, 0,
                LiquidChallengeSettings.RequiredChallengesPerCycle - 1);
            float[] authored = profile != null && profile.liquidChallenges != null
                ? profile.liquidChallenges.structuralRunUpPositions : null;
            if (authored != null && authored.Length == LiquidChallengeSettings.RequiredChallengesPerCycle)
                return authored[index];
            switch (index)
            {
                case 0: return 430f;
                case 1: return 1280f;
                case 2: return 2040f;
                case 3: return 2770f;
                case 4: return 3460f;
                case 5: return 4140f;
                case 6: return 4980f;
                default: return 5530f;
            }
        }

        public static LiquidChallengeLayout GetLiquidChallenge(MapProfile profile, int cycleIndex,
            int challengeIndex)
        {
            LiquidChallengeSettings settings = profile != null ? profile.liquidChallenges : null;
            float cycleLength = LiquidChallengeSettings.RequiredCycleLength;
            int count = LiquidChallengeSettings.RequiredChallengesPerCycle;
            int safeCycle = Mathf.Max(0, cycleIndex);
            int safeChallenge = Mathf.Clamp(challengeIndex, 0, count - 1);
            uint seed = CombinedSeed(profile);
            int variationHash = safeCycle * 7919 + safeChallenge * 104729;
            // Structural positions repeat exactly. Only pool length varies deterministically by cycle.
            float variation01 = Hash01(seed, variationHash + 53) * 2f - 1f;
            float growth = settings != null
                ? 1f + settings.cycleGrowth * safeCycle / (safeCycle + settings.growthSaturation)
                : 1f;
            float baseLength = settings != null ? settings.basePoolLength : 18f;
            float variation = 1f + variation01 * (settings != null ? settings.deterministicLengthVariation : .12f);
            float minimumLength = settings != null ? settings.minimumPoolLength : 16f;
            float maximumLength = settings != null ? settings.maximumPoolLength : 34f;
            float poolLength = Mathf.Clamp(baseLength * growth * variation, minimumLength, maximumLength);
            float runUpLength = settings != null ? settings.runUpLength : 42f;
            float transitionLength = settings != null ? settings.rampTransitionLength : 6f;
            float rampLength = settings != null ? settings.rampHorizontalLength : 12f;
            float landingLength = settings != null ? settings.landingLength : 26f;
            float recoveryLength = settings != null ? settings.recoveryLength : 48f;
            float startMargin = safeChallenge == 0 && safeCycle == 0
                ? (settings != null ? settings.cycleStartSafetyMargin : 260f) : 130f;
            float cycleStart = safeCycle * cycleLength;
            float latestLocalRunUp = cycleLength -
                                     (runUpLength + transitionLength + rampLength + poolLength + landingLength +
                                      recoveryLength + 80f);
            float localRunUp = Mathf.Clamp(GetNeonLiquidLocalRunUp(profile, safeChallenge), startMargin,
                Mathf.Max(startMargin, latestLocalRunUp));
            float runUpStart = cycleStart + localRunUp;
            float transitionStart = runUpStart + runUpLength;
            float rampStart = transitionStart + transitionLength;
            float poolStart = rampStart + rampLength;
            float poolEnd = poolStart + poolLength;
            float landingEnd = poolEnd + landingLength;
            float recoveryEnd = landingEnd + recoveryLength;
            float baseline = SampleNeonBaseHeight(profile, runUpStart);
            float slope = Mathf.Tan((settings != null ? settings.rampAngleDegrees : 20f) * Mathf.Deg2Rad);
            float rampHeight = slope * (rampLength + transitionLength * .5f);
            return new LiquidChallengeLayout
            {
                cycleIndex = safeCycle,
                challengeIndex = safeChallenge,
                runUpStart = runUpStart,
                transitionStart = transitionStart,
                rampStart = rampStart,
                poolStart = poolStart,
                poolEnd = poolEnd,
                landingEnd = landingEnd,
                recoveryEnd = recoveryEnd,
                baselineHeight = baseline,
                rampHeight = rampHeight,
                poolLength = poolLength,
                liquidSurfaceY = baseline + (settings != null ? settings.surfaceOffset : .15f)
            };
        }

        public static bool TryGetLiquidChallengeAtDistance(MapProfile profile, float distance,
            out LiquidChallengeLayout challenge)
        {
            challenge = default;
            if (profile == null || !profile.supportsLiquidExposure || distance < 0f) return false;
            int cycle = GetCycleIndex(distance);
            for (int i = 0; i < LiquidChallengeSettings.RequiredChallengesPerCycle; i++)
            {
                LiquidChallengeLayout candidate = GetLiquidChallenge(profile, cycle, i);
                if (distance < candidate.poolStart || distance >= candidate.poolEnd) continue;
                challenge = candidate;
                return true;
            }
            return false;
        }

        public static bool IsLiquidGap(MapProfile profile, float worldX) =>
            TryGetLiquidChallengeAtDistance(profile, worldX, out _);

        private static float SampleNeonHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            int cycle = GetCycleIndex(distance);
            float general = SampleNeonBaseHeight(profile, distance);
            for (int i = 0; i < LiquidChallengeSettings.RequiredChallengesPerCycle; i++)
            {
                LiquidChallengeLayout challenge = GetLiquidChallenge(profile, cycle, i);
                float blendStart = challenge.runUpStart - 18f;
                LiquidChallengeSettings settings = profile.liquidChallenges;
                float rejoinEnd = challenge.recoveryEnd + settings.postRecoveryBlendLength;
                if (distance < blendStart || distance > rejoinEnd) continue;
                float baseline = challenge.baselineHeight;
                if (distance < challenge.runUpStart)
                {
                    float t = Smooth01(Mathf.InverseLerp(blendStart, challenge.runUpStart, distance));
                    return Mathf.Lerp(general, baseline, t);
                }
                if (distance < challenge.transitionStart)
                {
                    float t = Mathf.InverseLerp(challenge.runUpStart, challenge.transitionStart, distance);
                    return baseline + Mathf.Sin(t * Mathf.PI) * .06f;
                }
                float slope = Mathf.Tan(settings.rampAngleDegrees * Mathf.Deg2Rad);
                if (distance < challenge.rampStart)
                {
                    float transitionDistance = distance - challenge.transitionStart;
                    float transitionT = transitionDistance / Mathf.Max(.001f, settings.rampTransitionLength);
                    return baseline + .5f * slope * settings.rampTransitionLength * transitionT * transitionT;
                }
                if (distance < challenge.poolStart)
                {
                    float transitionRise = .5f * slope * settings.rampTransitionLength;
                    return baseline + transitionRise + slope * (distance - challenge.rampStart);
                }
                if (distance < challenge.poolEnd)
                    return challenge.liquidSurfaceY;
                if (distance < challenge.landingEnd)
                {
                    float t = Smooth01(Mathf.InverseLerp(challenge.poolEnd, challenge.landingEnd, distance));
                    return Mathf.Lerp(baseline + .70f, baseline - .35f, t);
                }
                if (distance < challenge.recoveryEnd)
                {
                    float recoveryT = Smooth01(Mathf.InverseLerp(challenge.landingEnd,
                        challenge.recoveryEnd, distance));
                    return Mathf.Lerp(baseline - .35f, baseline - .55f, recoveryT);
                }
                float rejoinT = Smooth01(Mathf.InverseLerp(challenge.recoveryEnd, rejoinEnd, distance));
                float rejoinTarget = SampleNeonBaseHeight(profile, rejoinEnd);
                return Mathf.Lerp(baseline - .55f, rejoinTarget, rejoinT);
            }
            return general;
        }

        public static float EvaluateNeonAmplitude(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            float cycleProgress = distance / LiquidChallengeSettings.RequiredCycleLength;
            float saturation = Mathf.Max(.1f, profile.neonAmplitudeSaturationCycles);
            float difficulty = cycleProgress / (cycleProgress + saturation);
            return Mathf.Lerp(profile.amplitude, Mathf.Max(profile.amplitude, profile.neonMaximumAmplitude),
                difficulty);
        }

        public static float SampleNeonTemplateHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            float localDistance = Mathf.Repeat(distance, LiquidChallengeSettings.RequiredCycleLength);
            NeonTemplateSection section = GetNeonTemplateSection(localDistance);
            float shape = EvaluateNeonSectionShape(section, localDistance) +
                          EvaluateNeonMesoOffset(localDistance);
            return profile.baseHeight + shape * EvaluateNeonAmplitude(profile, distance);
        }

        private static float EvaluateNeonSectionShape(NeonTemplateSection section, float localDistance)
        {
            int sectionIndex = Mathf.Clamp((int)section, 0, NeonTemplateSectionCount - 1);
            float[] distances = NeonSectionDistances[sectionIndex];
            float[] heights = NeonSectionHeights[sectionIndex];
            int segment = 0;
            while (segment < distances.Length - 2 && localDistance >= distances[segment + 1]) segment++;
            float t = Smooth01(Mathf.InverseLerp(distances[segment], distances[segment + 1], localDistance));
            return Mathf.Lerp(heights[segment], heights[segment + 1], t);
        }

        private static float EvaluateNeonMesoOffset(float localDistance)
        {
            float result = 0f;
            for (int i = 0; i < NeonMesoFeatures.Length; i++)
            {
                NeonMesoFeature feature = NeonMesoFeatures[i];
                if (localDistance < feature.start || localDistance > feature.start + feature.length) continue;
                float t = Mathf.InverseLerp(feature.start, feature.start + feature.length, localDistance);
                float envelope = Mathf.Sin(t * Mathf.PI);
                envelope *= envelope;
                float shape;
                switch (feature.kind)
                {
                    case NeonMesoKind.DoubleBump:
                        shape = Mathf.Sin(t * Mathf.PI * 2f);
                        shape *= shape;
                        break;
                    case NeonMesoKind.Alternating:
                        shape = Mathf.Sin(t * Mathf.PI * 2f) * envelope;
                        break;
                    case NeonMesoKind.SoftStep:
                        float rise = Smooth01(Mathf.InverseLerp(0f, .22f, t));
                        float fall = 1f - Smooth01(Mathf.InverseLerp(.78f, 1f, t));
                        shape = rise * fall;
                        break;
                    default:
                        shape = envelope;
                        break;
                }
                result += shape * feature.normalizedHeight;
            }
            return result;
        }

        private static float SampleNeonBaseHeight(MapProfile profile, float worldX) =>
            SampleNeonTemplateHeight(profile, worldX);

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        public static TerrainSectionKind GetSectionKind(MapProfile profile, int sectionIndex)
        {
            float distance = Mathf.Max(0f, sectionIndex * profile.sectionLength);
            TerrainFeatureType feature = GetFeatureType(profile, sectionIndex, profile.EvaluateDifficulty(distance));
            if (feature == TerrainFeatureType.FlatRecovery)
                return TerrainSectionKind.Recovery;
            if (feature == TerrainFeatureType.RollingSection || feature == TerrainFeatureType.JumpableCrest)
                return TerrainSectionKind.Technical;
            if (feature == TerrainFeatureType.SoftHill)
                return TerrainSectionKind.Fast;
            return TerrainSectionKind.Rolling;
        }

        public static TerrainFeatureType GetFeatureType(MapProfile profile, int featureIndex, float difficulty)
        {
            uint seed = CombinedSeed(profile);
            int recoveryInterval = Mathf.Max(2, profile.recoveryEverySections);
            int recoveryOffset = (int)(seed % (uint)recoveryInterval);
            if (PositiveModulo(featureIndex + recoveryOffset, recoveryInterval) == recoveryInterval - 1 ||
                (featureIndex > 0 && IsTechnicalFeatureCandidate(profile, seed, featureIndex - 1, difficulty)))
                return TerrainFeatureType.FlatRecovery;

            float difficulty01 = Mathf.Clamp01(difficulty);
            if (IsTechnicalFeatureCandidate(profile, seed, featureIndex, difficulty))
                return Hash01(seed, featureIndex * 431 + 83) < 0.58f
                    ? TerrainFeatureType.RollingSection : TerrainFeatureType.JumpableCrest;
            float flat = Mathf.Max(0f, profile.flatFeatureWeight * Mathf.Lerp(1.15f, 0.72f, difficulty01));
            float hills = Mathf.Max(0f, profile.hillFeatureWeight);
            float valleys = Mathf.Max(0f, profile.valleyFeatureWeight);
            float technical = Mathf.Max(0f, profile.technicalFeatureWeight * Mathf.Lerp(0.45f, 1.35f, difficulty01));
            float total = Mathf.Max(0.001f, flat + hills + valleys + technical);
            float choice = Hash01(seed, featureIndex * 193 + 101) * total;
            if (choice < flat)
                return TerrainFeatureType.FlatRecovery;
            choice -= flat;
            if (choice < hills)
            {
                float variant = Hash01(seed, featureIndex * 389 + 59);
                if (difficulty01 < 0.18f)
                    return variant < 0.58f ? TerrainFeatureType.SoftHill : TerrainFeatureType.MediumHill;
                if (variant < 0.24f) return TerrainFeatureType.SoftHill;
                if (variant < 0.52f) return TerrainFeatureType.MediumHill;
                if (variant < 0.69f) return TerrainFeatureType.LongClimb;
                if (variant < 0.86f) return TerrainFeatureType.LongDescent;
                return TerrainFeatureType.Crest;
            }
            choice -= hills;
            if (choice < valleys)
                return TerrainFeatureType.Valley;
            return Hash01(seed, featureIndex * 431 + 83) < 0.62f
                ? TerrainFeatureType.RollingSection
                : TerrainFeatureType.JumpableCrest;
        }

        private static float EvaluateFeature(TerrainFeatureType feature, float t, float variant, float difficulty,
            float technicalDetailStrength)
        {
            float clampedT = Mathf.Clamp01(t);
            float envelope = Mathf.Sin(clampedT * Mathf.PI);
            envelope *= envelope;
            float shape;
            switch (feature)
            {
                case TerrainFeatureType.FlatRecovery:
                    shape = Mathf.Sin(clampedT * TwoPi) * envelope * 0.08f;
                    break;
                case TerrainFeatureType.SoftHill:
                    shape = envelope * Mathf.Lerp(0.30f, 0.42f, variant);
                    break;
                case TerrainFeatureType.MediumHill:
                    shape = envelope * Mathf.Lerp(0.58f, 0.76f, variant);
                    break;
                case TerrainFeatureType.LongClimb:
                    shape = envelope * (0.42f + clampedT * 0.36f);
                    break;
                case TerrainFeatureType.LongDescent:
                    shape = -envelope * (0.78f - clampedT * 0.34f);
                    break;
                case TerrainFeatureType.Valley:
                    shape = -envelope * Mathf.Lerp(0.55f, 0.78f, variant);
                    break;
                case TerrainFeatureType.Crest:
                    shape = envelope * envelope * Mathf.Lerp(0.72f, 0.9f, variant);
                    break;
                case TerrainFeatureType.RollingSection:
                    shape = Mathf.Sin(clampedT * TwoPi) * envelope * Mathf.Lerp(0.46f, 0.62f, variant);
                    break;
                case TerrainFeatureType.JumpableCrest:
                    shape = Mathf.Pow(envelope, 1.65f) * Mathf.Lerp(0.68f, 0.84f, variant);
                    break;
                default:
                    shape = 0f;
                    break;
            }

            float technicalFactor = feature == TerrainFeatureType.RollingSection ||
                                    feature == TerrainFeatureType.JumpableCrest ||
                                    feature == TerrainFeatureType.Crest || feature == TerrainFeatureType.Valley
                ? 1f : 0.32f;
            float detailCycles = Mathf.Lerp(1.15f, 2.25f, difficulty);
            float detail = Mathf.Sin((clampedT * detailCycles + variant * 0.17f) * TwoPi) * envelope *
                           technicalDetailStrength * technicalFactor * Mathf.Lerp(0f, 0.20f, difficulty);
            return shape + detail;
        }

        public static void BuildMesh(Mesh mesh, MapProfile profile, float chunkStartX, float chunkLength,
            int samples, float width, bool hasBoundary, float boundaryHeight, float boundarySlope,
            out float[] surfaceHeights, out float endHeight, out float endSlope)
        {
            int sampleCount = Mathf.Max(2, samples + 1);
            float dx = chunkLength / (sampleCount - 1);
            int padding = Mathf.Max(2, profile.smoothingPasses + 2);
            float[] working = new float[sampleCount + padding * 2];
            float[] scratch = new float[working.Length];
            for (int i = 0; i < working.Length; i++)
                working[i] = SampleHeight(profile, chunkStartX + (i - padding) * dx);

            int passes = Mathf.Clamp(profile.smoothingPasses, 0, 8);
            for (int pass = 0; pass < passes; pass++)
            {
                scratch[0] = working[0];
                scratch[working.Length - 1] = working[working.Length - 1];
                for (int i = 1; i < working.Length - 1; i++)
                    scratch[i] = working[i - 1] * 0.25f + working[i] * 0.5f + working[i + 1] * 0.25f;
                float[] swap = working;
                working = scratch;
                scratch = swap;
            }

            surfaceHeights = new float[sampleCount];
            float maxSlopeDelta = Mathf.Tan(profile.maxSlopeAngle * Mathf.Deg2Rad) * dx;
            float maxDelta = Mathf.Min(maxSlopeDelta, profile.maxHeightDeltaPerSample);
            float previousHeight;
            float previousSlope;
            if (profile.supportsLiquidExposure)
            {
                for (int i = 0; i < sampleCount; i++) surfaceHeights[i] = working[i + padding];
                previousHeight = surfaceHeights[sampleCount - 1];
                previousSlope = (surfaceHeights[sampleCount - 1] - surfaceHeights[sampleCount - 2]) / dx;
            }
            else
            {
                previousHeight = hasBoundary ? boundaryHeight : working[padding];
                previousSlope = hasBoundary ? boundarySlope :
                    (working[padding + 1] - working[padding]) / dx;
                surfaceHeights[0] = previousHeight;
                int firstGeneratedSample = 1;
                if (hasBoundary && sampleCount > 1)
                {
                    float boundaryDelta = Mathf.Clamp(boundarySlope * dx, -maxDelta, maxDelta);
                    surfaceHeights[1] = previousHeight + boundaryDelta;
                    previousHeight = surfaceHeights[1];
                    previousSlope = boundaryDelta / dx;
                    firstGeneratedSample = 2;
                }
                for (int i = firstGeneratedSample; i < sampleCount; i++)
                {
                    float desiredHeight = working[i + padding];
                    float desiredDelta = Mathf.Clamp(desiredHeight - previousHeight, -maxDelta, maxDelta);
                    float desiredSlope = desiredDelta / dx;
                    float previousAngle = Mathf.Atan(previousSlope) * Mathf.Rad2Deg;
                    float desiredAngle = Mathf.Atan(desiredSlope) * Mathf.Rad2Deg;
                    float safeAngle = Mathf.MoveTowards(previousAngle, desiredAngle, profile.maximumCrestSharpness);
                    float safeSlope = Mathf.Tan(safeAngle * Mathf.Deg2Rad);
                    float height = previousHeight + safeSlope * dx;
                    surfaceHeights[i] = height;
                    previousHeight = height;
                    previousSlope = safeSlope;
                }
            }

            endHeight = previousHeight;
            endSlope = previousSlope;
            float skirtBottom = profile.baseHeight - profile.amplitude *
                (Mathf.Max(1f, profile.maximumDifficulty) + 0.5f) - profile.terrainVisualDepth;
            Vector3[] vertices = new Vector3[sampleCount * 3];
            Vector2[] uvs = new Vector2[vertices.Length];
            var surfaceTriangles = new List<int>((sampleCount - 1) * 6);
            var skirtTriangles = new List<int>((sampleCount - 1) * 6);
            for (int i = 0; i < sampleCount; i++)
            {
                float localX = i * dx;
                int vertex = i * 3;
                vertices[vertex] = new Vector3(localX, surfaceHeights[i], -width * 0.5f);
                vertices[vertex + 1] = new Vector3(localX, surfaceHeights[i], width * 0.5f);
                vertices[vertex + 2] = new Vector3(localX, skirtBottom, -width * 0.5f);
                uvs[vertex] = new Vector2(localX * 0.1f, 0f);
                uvs[vertex + 1] = new Vector2(localX * 0.1f, 1f);
                uvs[vertex + 2] = new Vector2(localX * 0.1f, Mathf.Abs(surfaceHeights[i] - skirtBottom) * 0.1f);
            }

            for (int i = 0; i < sampleCount - 1; i++)
            {
                float segmentWorldX = chunkStartX + (i + .5f) * dx;
                if (IsLiquidGap(profile, segmentWorldX)) continue;
                int vertex = i * 3;
                int next = vertex + 3;
                surfaceTriangles.Add(vertex);
                surfaceTriangles.Add(vertex + 1);
                surfaceTriangles.Add(next);
                surfaceTriangles.Add(vertex + 1);
                surfaceTriangles.Add(next + 1);
                surfaceTriangles.Add(next);
                skirtTriangles.Add(vertex);
                skirtTriangles.Add(next);
                skirtTriangles.Add(vertex + 2);
                skirtTriangles.Add(next);
                skirtTriangles.Add(next + 2);
                skirtTriangles.Add(vertex + 2);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            if (profile.supportsLiquidExposure && profile.terrainEdgeMaterial != null)
            {
                // Neon uses Terrain.png for the visible wall and Ground.png for the driveable top.
                // Both visual submeshes share the same vertices and the one authoritative collider.
                mesh.subMeshCount = 2;
                mesh.SetTriangles(skirtTriangles, 0);
                mesh.SetTriangles(surfaceTriangles, 1);
            }
            else
            {
                skirtTriangles.AddRange(surfaceTriangles);
                mesh.subMeshCount = 1;
                mesh.SetTriangles(skirtTriangles, 0);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static bool IsTechnicalFeatureCandidate(MapProfile profile, uint seed, int index, float difficulty)
        {
            float chance = Mathf.Clamp01(profile.technicalSectionChance *
                                         Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(difficulty)));
            return Hash01(seed, index * 313 + 17) < chance;
        }

        private static uint CombinedSeed(MapProfile profile)
        {
            uint hash = 2166136261u;
            if (profile == null) return hash;
            string id = profile.mapId ?? string.Empty;
            for (int i = 0; i < id.Length; i++)
                hash = (hash ^ id[i]) * 16777619u;
            return hash ^ (uint)profile.seed * 2246822519u;
        }

        private static float Hash01(uint seed, int value)
        {
            uint x = seed ^ (uint)value * 374761393u;
            x = (x ^ (x >> 13)) * 1274126177u;
            x ^= x >> 16;
            return (x & 0x00FFFFFFu) / 16777215f;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
