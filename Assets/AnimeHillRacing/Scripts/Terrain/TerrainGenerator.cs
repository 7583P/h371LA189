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
        BrokenTerrain,
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
        public const int NeonTemplateSectionCount = 10;

        // Exact authored order inside every 6000 m cycle:
        // Warmup -> Rolling Hills -> Gradual Descent -> Bumpy Flat -> Staircases -> Broken Terrain ->
        // Rolling Descent -> Double Hill -> Tech Bumps -> Recovery.
        private static readonly float[] NeonSectionBoundaries =
        {
            0f, 550f, 1250f, 1900f, 2450f, 3100f, 3700f, 4350f, 5050f, 5700f, 6000f
        };

        private static readonly float[][] NeonSectionDistances =
        {
            new[] { 0f, 100f, 185f, 270f, 355f, 445f, 550f },
            new[] { 550f, 635f, 720f, 810f, 900f, 1010f, 1120f, 1250f },
            new[] { 1250f, 1340f, 1450f, 1540f, 1660f, 1740f, 1830f, 1900f },
            new[] { 1900f, 1970f, 2045f, 2125f, 2200f, 2280f, 2360f, 2450f },
            new[] { 2450f, 2520f, 2580f, 2660f, 2720f, 2800f, 2870f, 2950f, 3020f, 3070f, 3100f },
            new[] { 3100f, 3170f, 3240f, 3320f, 3390f, 3470f, 3540f, 3620f, 3700f },
            new[] { 3700f, 3780f, 3870f, 3960f, 4050f, 4140f, 4250f, 4350f },
            new[] { 4350f, 4420f, 4510f, 4590f, 4680f, 4760f, 4850f, 4930f, 5000f, 5050f },
            new[] { 5050f, 5120f, 5190f, 5260f, 5330f, 5400f, 5470f, 5540f, 5620f, 5700f },
            new[] { 5700f, 5780f, 5860f, 5930f, 6000f }
        };

        private static readonly float[][] NeonSectionHeights =
        {
            new[] { 0f, .015f, -.010f, .035f, .015f, .070f, .10f },
            new[] { .10f, .18f, .45f, .32f, .58f, .28f, .48f, .36f },
            new[] { .36f, .40f, .30f, .22f, .26f, .05f, -.08f, -.18f },
            new[] { -.18f, -.14f, -.22f, -.13f, -.21f, -.16f, -.24f, -.18f },
            new[] { -.18f, -.18f, .02f, .02f, .24f, .24f, .46f, .46f, .62f, .62f, .38f },
            new[] { .38f, .15f, .32f, -.08f, .12f, -.24f, .06f, -.32f, -.12f },
            new[] { -.12f, .16f, .02f, .28f, .05f, -.12f, -.34f, -.42f },
            new[] { -.42f, -.28f, .42f, .34f, -.30f, -.18f, .55f, .38f, -.26f, -.18f },
            new[] { -.18f, .08f, -.22f, .14f, -.26f, .12f, -.30f, .10f, -.24f, -.08f },
            new[] { -.08f, -.06f, -.03f, .01f, 0f }
        };

        private enum NeonMesoKind
        {
            Bump,
            DoubleBump,
            Alternating,
            SoftStep,
            SharpCrest,
            Pothole,
            Whoops,
            TripleBump,
            ShortPlateau,
            BrokenRhythm
        }

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

        // 28-70 m C1-continuous authored obstacles. They are deliberately placed only in normal terrain
        // windows between the eight liquid structures. There is no Perlin/micro noise: each bump, dip,
        // plateau and broken-rhythm group is large enough to read visually and affect suspension/momentum.
        private static readonly NeonMesoFeature[] NeonMesoFeatures =
        {
            new NeonMesoFeature(120f, 60f, .035f, NeonMesoKind.Bump),
            new NeonMesoFeature(205f, 28f, .055f, NeonMesoKind.Pothole),
            new NeonMesoFeature(265f, 55f, .040f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(345f, 38f, .045f, NeonMesoKind.Alternating),

            new NeonMesoFeature(650f, 48f, .050f, NeonMesoKind.Whoops),
            new NeonMesoFeature(715f, 28f, .065f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(760f, 42f, .055f, NeonMesoKind.Pothole),
            new NeonMesoFeature(825f, 60f, .045f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(905f, 34f, .060f, NeonMesoKind.Bump),
            new NeonMesoFeature(955f, 52f, .050f, NeonMesoKind.Alternating),
            new NeonMesoFeature(1025f, 28f, .070f, NeonMesoKind.Pothole),
            new NeonMesoFeature(1070f, 68f, .050f, NeonMesoKind.TripleBump),
            new NeonMesoFeature(1160f, 38f, .070f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(1210f, 34f, .055f, NeonMesoKind.Pothole),

            new NeonMesoFeature(1495f, 48f, .045f, NeonMesoKind.Alternating),
            new NeonMesoFeature(1555f, 32f, .060f, NeonMesoKind.Pothole),
            new NeonMesoFeature(1600f, 66f, .050f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(1680f, 28f, .075f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(1725f, 52f, .040f, NeonMesoKind.ShortPlateau),
            new NeonMesoFeature(1795f, 36f, .075f, NeonMesoKind.Pothole),
            new NeonMesoFeature(1845f, 70f, .045f, NeonMesoKind.TripleBump),
            new NeonMesoFeature(1930f, 32f, .070f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(1975f, 38f, .060f, NeonMesoKind.Pothole),

            new NeonMesoFeature(2255f, 55f, .050f, NeonMesoKind.Whoops),
            new NeonMesoFeature(2325f, 32f, .075f, NeonMesoKind.Pothole),
            new NeonMesoFeature(2370f, 44f, .055f, NeonMesoKind.Bump),
            new NeonMesoFeature(2425f, 62f, .060f, NeonMesoKind.Alternating),
            new NeonMesoFeature(2498f, 38f, .070f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(2548f, 66f, .050f, NeonMesoKind.TripleBump),
            new NeonMesoFeature(2622f, 40f, .080f, NeonMesoKind.Pothole),
            new NeonMesoFeature(2670f, 60f, .060f, NeonMesoKind.BrokenRhythm),

            new NeonMesoFeature(2985f, 65f, .050f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(3060f, 44f, .055f, NeonMesoKind.Bump),
            new NeonMesoFeature(3115f, 60f, .055f, NeonMesoKind.SoftStep),
            new NeonMesoFeature(3185f, 30f, .075f, NeonMesoKind.Pothole),
            new NeonMesoFeature(3225f, 70f, .060f, NeonMesoKind.ShortPlateau),
            new NeonMesoFeature(3305f, 42f, .055f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(3358f, 58f, .060f, NeonMesoKind.SoftStep),

            new NeonMesoFeature(3670f, 48f, .055f, NeonMesoKind.Alternating),
            new NeonMesoFeature(3730f, 30f, .075f, NeonMesoKind.Pothole),
            new NeonMesoFeature(3775f, 58f, .055f, NeonMesoKind.Whoops),
            new NeonMesoFeature(3845f, 36f, .075f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(3895f, 66f, -.055f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(3970f, 55f, .060f, NeonMesoKind.BrokenRhythm),
            new NeonMesoFeature(4035f, 32f, .080f, NeonMesoKind.Pothole),
            new NeonMesoFeature(4075f, 37f, .060f, NeonMesoKind.Bump),

            new NeonMesoFeature(4355f, 60f, .055f, NeonMesoKind.Alternating),
            new NeonMesoFeature(4425f, 34f, .080f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(4470f, 52f, .070f, NeonMesoKind.Pothole),
            new NeonMesoFeature(4535f, 68f, .060f, NeonMesoKind.DoubleBump),
            new NeonMesoFeature(4615f, 30f, .085f, NeonMesoKind.Pothole),
            new NeonMesoFeature(4655f, 45f, .055f, NeonMesoKind.ShortPlateau),
            new NeonMesoFeature(4710f, 60f, .050f, NeonMesoKind.TripleBump),
            new NeonMesoFeature(4785f, 38f, .075f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(4835f, 55f, .060f, NeonMesoKind.Alternating),
            new NeonMesoFeature(4900f, 42f, .075f, NeonMesoKind.Pothole),

            new NeonMesoFeature(5195f, 36f, .095f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(5240f, 42f, .085f, NeonMesoKind.Pothole),
            new NeonMesoFeature(5292f, 55f, .065f, NeonMesoKind.Whoops),
            new NeonMesoFeature(5355f, 36f, .085f, NeonMesoKind.SharpCrest),
            new NeonMesoFeature(5398f, 44f, .090f, NeonMesoKind.Pothole),
            new NeonMesoFeature(5450f, 52f, .070f, NeonMesoKind.BrokenRhythm),

            new NeonMesoFeature(5745f, 50f, .025f, NeonMesoKind.Bump),
            new NeonMesoFeature(5810f, 38f, .025f, NeonMesoKind.Pothole),
            new NeonMesoFeature(5860f, 58f, .022f, NeonMesoKind.Alternating),
            new NeonMesoFeature(5930f, 50f, .020f, NeonMesoKind.Bump)
        };

        public static int NeonMesoFeatureCount => NeonMesoFeatures.Length;
        public static int NeonSharpCrestCount => CountNeonMesoFeatures(NeonMesoKind.SharpCrest);
        public static int NeonPotholeCount => CountNeonMesoFeatures(NeonMesoKind.Pothole);
        public static int NeonWhoopsCount => CountNeonMesoFeatures(NeonMesoKind.Whoops);
        public static int NeonSoftStepCount => CountNeonMesoFeatures(NeonMesoKind.SoftStep);
        public static int NeonTripleBumpCount => CountNeonMesoFeatures(NeonMesoKind.TripleBump);
        public static int NeonPlateauCount => CountNeonMesoFeatures(NeonMesoKind.ShortPlateau);
        public static int NeonBrokenRhythmCount => CountNeonMesoFeatures(NeonMesoKind.BrokenRhythm);
        public const float NeonMesoMinimumLength = 28f;
        public const float NeonMesoMaximumLength = 70f;
        public const float NeonMesoMinimumNormalizedHeight = .020f;
        public const float NeonMesoMaximumNormalizedHeight = .095f;

        private static int CountNeonMesoFeatures(NeonMesoKind kind)
        {
            int count = 0;
            for (int i = 0; i < NeonMesoFeatures.Length; i++)
                if (NeonMesoFeatures[i].kind == kind) count++;
            return count;
        }

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
            if (profile != null && profile.useCityHighwayTerrain)
                return SampleCityHighwayHeight(profile, worldX);
            if (profile != null && profile.supportsLiquidExposure)
                return SampleNeonHeight(profile, worldX);
            return profile != null && profile.elevationProfile != null
                ? profile.elevationProfile.GetHeightAtDistance(worldX)
                : SampleProfileHeight(profile, worldX);
        }

        public static float SampleProfileHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            if (profile.useCityHighwayTerrain) return SampleCityHighwayHeight(profile, worldX);
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

        public static float SampleCityHighwayHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            float featureLength = Mathf.Max(42f, profile.cityFeatureLength);
            float featurePosition = distance / featureLength;
            int featureIndex = Mathf.FloorToInt(featurePosition);
            float t = featurePosition - featureIndex;
            float difficulty = Mathf.Clamp01(profile.EvaluateDifficulty(distance));
            float amplitude = profile.amplitude * Mathf.Lerp(profile.introductoryAmplitude, 1f, difficulty);
            uint seed = CombinedSeed(profile);
            float p0 = CityAnchor(seed, featureIndex - 1);
            float p1 = CityAnchor(seed, featureIndex);
            float p2 = CityAnchor(seed, featureIndex + 1);
            float p3 = CityAnchor(seed, featureIndex + 2);
            float backbone = CatmullRom(p0, p1, p2, p3, t);

            float envelope = Mathf.Sin(t * Mathf.PI);
            envelope *= envelope;
            float rhythmVariant = Hash01(seed, featureIndex * 619 + 157);
            float cycles = rhythmVariant < .34f ? 1f : rhythmVariant < .72f ? 1.5f : 2f;
            float phase = Hash01(seed, featureIndex * 811 + 43) > .5f ? 0f : Mathf.PI;
            float miniRhythm = Mathf.Sin(t * TwoPi * cycles + phase) * envelope *
                               profile.cityMiniRhythmStrength * Mathf.Lerp(.35f, 1f, difficulty);
            float broadWave = Mathf.Sin(distance * TwoPi / 220f + Hash01(seed, 991) * TwoPi) * .12f;
            float startBlend = Smooth01(Mathf.InverseLerp(10f, 110f, distance));
            return profile.baseHeight + (backbone + miniRhythm + broadWave) * amplitude * startBlend;
        }

        private static float CityAnchor(uint seed, int index)
        {
            float previous = Hash01(seed, (index - 1) * 503 + 19) * 2f - 1f;
            float current = Hash01(seed, index * 503 + 19) * 2f - 1f;
            float next = Hash01(seed, (index + 1) * 503 + 19) * 2f - 1f;
            return previous * .20f + current * .60f + next * .20f;
        }

        private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return .5f * ((2f * p1) + (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
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
                    case NeonMesoKind.SharpCrest:
                        shape = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 3f);
                        break;
                    case NeonMesoKind.Pothole:
                        shape = -envelope;
                        break;
                    case NeonMesoKind.Whoops:
                        shape = Mathf.Sin(t * Mathf.PI * 3f) * envelope;
                        break;
                    case NeonMesoKind.DoubleBump:
                        shape = Mathf.Sin(t * Mathf.PI * 2f);
                        shape *= shape;
                        break;
                    case NeonMesoKind.TripleBump:
                        shape = Mathf.Sin(t * Mathf.PI * 3f);
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
                    case NeonMesoKind.ShortPlateau:
                        float plateauRise = Smooth01(Mathf.InverseLerp(0f, .18f, t));
                        float plateauFall = 1f - Smooth01(Mathf.InverseLerp(.72f, 1f, t));
                        shape = plateauRise * plateauFall;
                        break;
                    case NeonMesoKind.BrokenRhythm:
                        shape = (Mathf.Sin(t * Mathf.PI * 2f) * .72f +
                                 Mathf.Sin(t * Mathf.PI * 4f) * .28f) * envelope;
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
            CalculateGroundVisualExtents(profile, out float groundVisualDepth, out float groundVisualRise);
            float terrainFillDepth = CalculateTerrainFillDepth(profile);
            // Six independent vertices keep the three responsibilities separate: deep Terrain wall,
            // visible Ground ribbon and the horizontal driveable surface. Their top Y still comes from
            // the same surfaceHeights array used by the authoritative physics collider.
            Vector3[] vertices = new Vector3[sampleCount * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
            var surfaceTriangles = new List<int>((sampleCount - 1) * 12);
            var skirtTriangles = new List<int>((sampleCount - 1) * 6);
            for (int i = 0; i < sampleCount; i++)
            {
                float localX = i * dx;
                float textureU = profile.fitAuthoredTerrainTexture
                    ? (chunkStartX + localX) / Mathf.Max(1f, profile.terrainTextureRepeatWidth)
                    : localX * .1f;
                float surfaceY = surfaceHeights[i];
                float groundBottomY = surfaceY - groundVisualDepth;
                float groundTopY = surfaceY + groundVisualRise;
                float terrainTopY = surfaceY - terrainFillDepth;
                int vertex = i * 6;
                vertices[vertex] = new Vector3(localX, terrainTopY, -width * .5f);        // Terrain fill top
                vertices[vertex + 1] = new Vector3(localX, skirtBottom, -width * .5f);    // Terrain bottom
                vertices[vertex + 2] = new Vector3(localX, groundTopY, -width * .5f);     // Ground art top
                vertices[vertex + 3] = new Vector3(localX, groundBottomY, -width * .5f);  // Ground bottom
                vertices[vertex + 4] = new Vector3(localX, surfaceY, -width * .5f);       // Physical surface front
                vertices[vertex + 5] = new Vector3(localX, surfaceY, width * .5f);         // Surface back
                if (profile.fitAuthoredTerrainTexture)
                {
                    // Ground's authored visible band is mapped to its side-facing ribbon. The contact V can
                    // sit inside that band so foliage may extend above the road while the continuous turf line
                    // remains exactly on the generated/collider surface. Terrain starts behind the continuous
                    // Ground band so transparent decorative roots cannot expose a gap.
                    uvs[vertex] = new Vector2(textureU, profile.terrainWallUvRange.y);
                    uvs[vertex + 1] = new Vector2(textureU, profile.terrainWallUvRange.x);
                    uvs[vertex + 2] = new Vector2(textureU, profile.terrainSurfaceUvRange.y);
                    uvs[vertex + 3] = new Vector2(textureU, profile.terrainSurfaceUvRange.x);
                    uvs[vertex + 4] = new Vector2(textureU, profile.terrainSurfaceUvRange.y);
                    uvs[vertex + 5] = new Vector2(textureU, profile.terrainSurfaceUvRange.x);
                }
                else
                {
                    uvs[vertex] = new Vector2(textureU, 0f);
                    uvs[vertex + 1] = new Vector2(textureU,
                        Mathf.Abs(surfaceHeights[i] - skirtBottom) * .1f);
                    uvs[vertex + 2] = new Vector2(textureU, 0f);
                    uvs[vertex + 3] = new Vector2(textureU, 0f);
                    uvs[vertex + 4] = new Vector2(textureU, 0f);
                    uvs[vertex + 5] = new Vector2(textureU, 1f);
                }
            }

            for (int i = 0; i < sampleCount - 1; i++)
            {
                float segmentWorldX = chunkStartX + (i + .5f) * dx;
                if (IsLiquidGap(profile, segmentWorldX)) continue;
                int vertex = i * 6;
                int next = vertex + 6;
                if (profile.fitAuthoredTerrainTexture)
                {
                    surfaceTriangles.Add(vertex + 2);
                    surfaceTriangles.Add(next + 2);
                    surfaceTriangles.Add(vertex + 3);
                    surfaceTriangles.Add(next + 2);
                    surfaceTriangles.Add(next + 3);
                    surfaceTriangles.Add(vertex + 3);
                }
                surfaceTriangles.Add(vertex + 4);
                surfaceTriangles.Add(vertex + 5);
                surfaceTriangles.Add(next + 4);
                surfaceTriangles.Add(vertex + 5);
                surfaceTriangles.Add(next + 5);
                surfaceTriangles.Add(next + 4);
                skirtTriangles.Add(vertex);
                skirtTriangles.Add(next);
                skirtTriangles.Add(vertex + 1);
                skirtTriangles.Add(next);
                skirtTriangles.Add(next + 1);
                skirtTriangles.Add(vertex + 1);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            if ((profile.supportsLiquidExposure || profile.useSeparateTerrainMaterials) &&
                profile.terrainEdgeMaterial != null)
            {
                // Submesh 0 is the deep Terrain wall. Submesh 1 is the Ground ribbon/top surface.
                // Physics is generated separately from the same surfaceHeights source of truth.
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

        public static float CalculateGroundVisualDepth(MapProfile profile)
        {
            CalculateGroundVisualExtents(profile, out float depth, out _);
            return depth;
        }

        public static void CalculateGroundVisualExtents(MapProfile profile, out float depthBelowSurface,
            out float riseAboveSurface)
        {
            depthBelowSurface = 0f;
            riseAboveSurface = 0f;
            if (profile == null || !profile.fitAuthoredTerrainTexture) return;
            Texture texture = profile.terrainEdgeMaterial != null
                ? profile.terrainEdgeMaterial.mainTexture : null;
            if (texture == null || texture.width <= 0 || texture.height <= 0) return;
            float bottomV = profile.terrainSurfaceUvRange.x;
            float topV = profile.terrainSurfaceUvRange.y;
            float minimumV = Mathf.Min(bottomV, topV);
            float maximumV = Mathf.Max(bottomV, topV);
            float contactV = profile.terrainSurfaceContactV >= minimumV &&
                             profile.terrainSurfaceContactV <= maximumV
                ? profile.terrainSurfaceContactV : topV;
            float metresPerV = profile.terrainTextureRepeatWidth * texture.height / texture.width;
            // Both extents come from the source pixels and PPU-derived repeat width; there is no visual offset.
            depthBelowSurface = Mathf.Clamp(Mathf.Abs(contactV - bottomV) * metresPerV, .05f, 3f);
            riseAboveSurface = Mathf.Clamp(Mathf.Abs(topV - contactV) * metresPerV, 0f, 3f);
        }

        public static float CalculateTerrainFillDepth(MapProfile profile)
        {
            if (profile == null || !profile.fitAuthoredTerrainTexture) return 0f;
            Texture texture = profile.terrainEdgeMaterial != null
                ? profile.terrainEdgeMaterial.mainTexture : null;
            if (texture == null || texture.width <= 0 || texture.height <= 0) return 0f;
            float bottomV = profile.terrainSurfaceUvRange.x;
            float topV = profile.terrainSurfaceUvRange.y;
            float minimumV = Mathf.Min(bottomV, topV);
            float maximumV = Mathf.Max(bottomV, topV);
            float contactV = profile.terrainSurfaceContactV >= minimumV &&
                             profile.terrainSurfaceContactV <= maximumV
                ? profile.terrainSurfaceContactV : topV;
            float fillV = profile.terrainFillStartV >= minimumV && profile.terrainFillStartV <= maximumV
                ? profile.terrainFillStartV : bottomV;
            float metresPerV = profile.terrainTextureRepeatWidth * texture.height / texture.width;
            return Mathf.Clamp(Mathf.Abs(contactV - fillV) * metresPerV, 0f,
                CalculateGroundVisualDepth(profile));
        }

        public static void BuildSurfaceColliderMesh(Mesh target, MapProfile profile, float chunkStartX,
            float chunkLength, float[] heights, float width)
        {
            if (target == null || heights == null || heights.Length < 2) return;
            int sampleCount = heights.Length;
            float dx = chunkLength / Mathf.Max(1, sampleCount - 1);
            var vertices = new Vector3[sampleCount * 2];
            var triangles = new List<int>((sampleCount - 1) * 6);
            for (int i = 0; i < sampleCount; i++)
            {
                float x = i * dx;
                vertices[i * 2] = new Vector3(x, heights[i], -width * .5f);
                vertices[i * 2 + 1] = new Vector3(x, heights[i], width * .5f);
            }
            for (int i = 0; i < sampleCount - 1; i++)
            {
                if (IsLiquidGap(profile, chunkStartX + (i + .5f) * dx)) continue;
                int vertex = i * 2;
                int next = vertex + 2;
                triangles.Add(vertex);
                triangles.Add(vertex + 1);
                triangles.Add(next);
                triangles.Add(vertex + 1);
                triangles.Add(next + 1);
                triangles.Add(next);
            }
            target.Clear();
            target.vertices = vertices;
            target.SetTriangles(triangles, 0);
            target.RecalculateNormals();
            target.RecalculateBounds();
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
