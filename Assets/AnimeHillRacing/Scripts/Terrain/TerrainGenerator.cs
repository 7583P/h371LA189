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

        // Authored 6000 m macro grammar with variable-length climbs, shelves, technical drops and
        // valley floors. Meso features below add suspension work without turning this into a sine wave.
        private static readonly float[] NeonCycleDistances =
        {
            0f, 200f, 320f, 450f, 600f, 750f, 900f, 1050f, 1200f, 1400f,
            1550f, 1700f, 1810f, 1950f, 2160f, 2300f, 2440f, 2580f, 2750f,
            2890f, 3020f, 3150f, 3300f, 3400f, 3520f, 3750f, 3900f, 4050f,
            4200f, 4330f, 4460f, 4600f, 4740f, 4880f, 5100f, 5240f, 5380f,
            5520f, 5660f, 5800f, 6000f
        };

        private static readonly float[] NeonCycleTemplate =
        {
            -.20f, -.20f, .25f, -.05f, .40f, .10f, .15f, .45f, .75f, .78f,
            .78f, .65f, -.35f, -.85f, -.85f, -.45f, .20f, .55f, .55f,
            .10f, .52f, 0f, .65f, .55f, -.55f, -.90f, -.90f, -.45f,
            .15f, -.10f, .40f, .05f, .35f, .80f, .80f, .35f, -.30f,
            -.50f, -.50f, -.25f, -.20f
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

        // 82-230 m C1-continuous obstacles. Heights scale with the cycle amplitude, producing
        // roughly 1.6-5.1 m features over the audited difficulty range.
        private static readonly NeonMesoFeature[] NeonMesoFeatures =
        {
            new NeonMesoFeature(240f, 96f, .055f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(390f, 82f, -.045f, NeonMesoKind.Bump),
            new NeonMesoFeature(525f, 150f, .075f, NeonMesoKind.Alternating),
            new NeonMesoFeature(720f, 118f, .065f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(890f, 210f, .090f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(1110f, 96f, -.055f, NeonMesoKind.Bump),
            new NeonMesoFeature(1260f, 170f, .075f, NeonMesoKind.Alternating),
            new NeonMesoFeature(1490f, 105f, .080f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(1650f, 135f, -.070f, NeonMesoKind.Bump),
            new NeonMesoFeature(1850f, 190f, .110f, NeonMesoKind.Alternating),
            new NeonMesoFeature(2110f, 88f, .060f, NeonMesoKind.Bump),
            new NeonMesoFeature(2260f, 150f, .080f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(2470f, 210f, -.100f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(2730f, 95f, .070f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(2870f, 160f, .080f, NeonMesoKind.Alternating),
            new NeonMesoFeature(3110f, 120f, -.075f, NeonMesoKind.Bump),
            new NeonMesoFeature(3280f, 220f, .120f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(3560f, 90f, .075f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(3700f, 180f, .100f, NeonMesoKind.Alternating),
            new NeonMesoFeature(3950f, 110f, -.070f, NeonMesoKind.Bump),
            new NeonMesoFeature(4140f, 230f, .130f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(4440f, 100f, .080f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(4600f, 170f, -.090f, NeonMesoKind.Alternating),
            new NeonMesoFeature(4850f, 140f, .100f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(5110f, 220f, -.120f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(5400f, 90f, .070f, NeonMesoKind.Bump),
            new NeonMesoFeature(5530f, 170f, .090f, NeonMesoKind.Alternating),
            new NeonMesoFeature(5770f, 95f, -.050f, NeonMesoKind.Bump)
        };

        public static int NeonMesoFeatureCount => NeonMesoFeatures.Length;
        public const float NeonMesoMinimumLength = 82f;
        public const float NeonMesoMaximumLength = 230f;

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

        public static LiquidChallengeLayout GetLiquidChallenge(MapProfile profile, int cycleIndex,
            int challengeIndex)
        {
            LiquidChallengeSettings settings = profile != null ? profile.liquidChallenges : null;
            float cycleLength = LiquidChallengeSettings.RequiredCycleLength;
            int count = LiquidChallengeSettings.RequiredChallengesPerCycle;
            int safeCycle = Mathf.Max(0, cycleIndex);
            int safeChallenge = Mathf.Clamp(challengeIndex, 0, count - 1);
            float slotLength = cycleLength / count;
            float slotStart = safeCycle * cycleLength + safeChallenge * slotLength;
            uint seed = CombinedSeed(profile);
            int positionHash = safeChallenge * 104729;
            int variationHash = safeCycle * 7919 + safeChallenge * 104729;
            // The eight logical positions repeat every cycle. Pool length may still vary by cycle.
            float jitter01 = Hash01(seed, positionHash + 17) * 2f - 1f;
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
            float jitter = jitter01 * (settings != null ? settings.slotJitter : 120f);
            float latestRunUp = slotStart + slotLength -
                                (runUpLength + transitionLength + rampLength + poolLength + landingLength +
                                 recoveryLength + 70f);
            float runUpStart = Mathf.Clamp(slotStart + 260f + jitter, slotStart + startMargin,
                Mathf.Max(slotStart + startMargin, latestRunUp));
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
            int segment = 0;
            while (segment < NeonCycleDistances.Length - 2 &&
                   localDistance >= NeonCycleDistances[segment + 1])
                segment++;
            float t = Smooth01(Mathf.InverseLerp(NeonCycleDistances[segment],
                NeonCycleDistances[segment + 1], localDistance));
            float shape = Mathf.Lerp(NeonCycleTemplate[segment], NeonCycleTemplate[segment + 1], t) +
                          EvaluateNeonMesoOffset(localDistance);
            float startFlatten = Smooth01(Mathf.Clamp01(distance / 180f));
            return profile.baseHeight + shape * EvaluateNeonAmplitude(profile, distance) * startFlatten;
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
