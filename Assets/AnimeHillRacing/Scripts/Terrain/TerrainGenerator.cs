using System.Collections.Generic;
using AnimeHillRacing.Core;
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

    public enum CountrysideSpecialFeatureType
    {
        LongFlat,
        DiagonalSlash
    }

    public struct CountrysideSpecialFeatureLayout
    {
        public int index;
        public CountrysideSpecialFeatureType type;
        public float start;
        public float coreStart;
        public float slashStart;
        public float coreEnd;
        public float end;
        public float baselineHeight;
        public float peakHeight;
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

    public struct CityBridgeZoneLayout
    {
        public int cycleIndex;
        public int zoneIndex;
        public int zonesInCycle;
        public int progressiveZonesInCycle;
        public bool isProgressive;
        public float approachStart;
        public float gapStart;
        public float waterStart;
        public float waterEnd;
        public float gapEnd;
        public float recoveryEnd;
        public float baselineHeight;
        public float bridgeLength;
        public float waterLength;

        public bool HasExitBridge => !isProgressive;
    }

    public struct SpaceChallengeLayout
    {
        public int phase;
        public SpaceEventType eventType;
        public SpacePlanetType planetType;
        public float eventDistance;
        public float approachStart;
        public float rampStart;
        public float rampLength;
        public float rampRise;
        public float gapStart;
        public float gapEnd;
        public float landingEnd;
        public float recoveryEnd;
        public float warningX;
        public float baselineHeight;
        public float gapLength;
        public float blackHoleDiameter;
        public float planetRadius;
        public float planetCenterX;
        public float planetCenterY;
        public float landingHeight;

        public bool IsVoid => eventType == SpaceEventType.Void;
        public bool IsPlanet => eventType == SpaceEventType.Planet;
    }

    public struct ArenaSegmentLayout
    {
        public int index;
        public ArenaSegmentType type;
        public float start;
        public float end;
        public float difficulty;
        public float variant;
        public float gapStart;
        public float gapEnd;
        public float platformCenterX;
        public float platformY;
        public float platformWidth;
        public float lavaStart;
        public float lavaEnd;
        public float tireStart;
        public float tireSpacing;
        public int tireCount;
        public bool HasGap => gapEnd > gapStart;
        public bool HasPlatform => platformWidth > 0f;
        public bool HasLava => lavaEnd > lavaStart;
        public bool HasTires => tireCount > 0;
    }

    public struct LastHorizonObstacleLayout
    {
        public int cellIndex;
        public int cycleType;
        public int repetitionIndex;
        public LastHorizonObstacleType type;
        public float centerX;
        public float surfaceY;
        public float width;
        public float phase;
        public int rollerCount;

        public float Start => centerX - width * .5f;
        public float End => centerX + width * .5f;
        public bool IsValid => type != LastHorizonObstacleType.None;
    }

    public struct LastHorizonSpecialPhaseLayout
    {
        public long key;
        public int cycleIndex;
        public int sequenceIndex;
        public LastHorizonSpecialPhaseType type;
        public float start;
        public float end;
        public float phase;
        public float transitionLength;

        public float Length => Mathf.Max(0f, end - start);
        public float GapStart => type == LastHorizonSpecialPhaseType.RoofTransportCrusher
            ? (start + end) * .5f - Mathf.Min(Length * .3f,
                Mathf.Clamp(transitionLength, 24f, 64f)) * .5f
            : start + Mathf.Min(transitionLength, Length * .25f);
        public float GapEnd => type == LastHorizonSpecialPhaseType.RoofTransportCrusher
            ? (start + end) * .5f + Mathf.Min(Length * .3f,
                Mathf.Clamp(transitionLength, 24f, 64f)) * .5f
            : end - Mathf.Min(transitionLength, Length * .25f);
        public bool CreatesTerrainGap => type == LastHorizonSpecialPhaseType.DescendingVoidPlatforms ||
                                         type == LastHorizonSpecialPhaseType.MovingExtremeTerrain ||
                                         type == LastHorizonSpecialPhaseType.RoofTransportCrusher;
        public bool IsValid => type != LastHorizonSpecialPhaseType.None && end > start;
        public bool Contains(float worldX) => worldX >= start && worldX < end;
        public bool ContainsGap(float worldX) => CreatesTerrainGap &&
                                                 worldX >= GapStart && worldX < GapEnd;
    }

    public static class TerrainGenerator
    {
        private const float TwoPi = Mathf.PI * 2f;
        private static readonly Dictionary<long, LastHorizonSpecialPhaseLayout[]>
            LastHorizonSpecialPhaseCache =
                new Dictionary<long, LastHorizonSpecialPhaseLayout[]>();
        // Rainyer, Neon City, Mountains and Space use complete 2048 px Ground modules imported at
        // 100 PPU. Keeping their repeat at the native 20.48 m prevents horizontal stretching and
        // makes the sampled painted contact curve use the same source-pixel scale as the artwork.
        public const float AuthoredTrackModuleWidth = 20.48f;
        public const float AuthoredTrackMaximumVisualExtent = 3f;
        public const float CompactGroundModuleWidth = 45f;
        public const float CompactGroundMaximumVisualExtent = 2.5f;
        public const float NeonCityGroundMaximumVisualExtent = 1.35f;
        // At the 1.35 m authored-band scale the painted contact row needs about 1.274 m
        // above the road. A lower independent cap distorts the contact ratio and makes the
        // visible road float below the collider (the former .65 m cap caused a .320987 m gap).
        public const float NeonCityGroundMaximumVisualRise = 1.28f;
        public const float CountrysideGroundModuleWidth = 13.3f;
        public const float CountrysideGroundMaximumVisualExtent = 1.5f;
        public const float CountrysideGroundMaximumVisualRise = .8f;
        public const float MountainsGroundModuleWidth = 17.38f;
        public const float MountainsGroundMaximumVisualExtent = 2f;
        public const float MountainsGroundMaximumVisualRise = .82f;
        public const float SpaceGroundModuleWidth = 19.55f;
        public const float SpaceGroundMaximumVisualExtent = 2.5f;
        public const float SpaceGroundMaximumVisualRise = 1.05f;
        public const int NeonTemplateSectionCount = 10;
        private static readonly Dictionary<uint, List<ArenaSegmentType>> ArenaSegmentHistory =
            new Dictionary<uint, List<ArenaSegmentType>>();
        private static readonly ArenaSegmentType[] ArenaCandidateTypes =
        {
            ArenaSegmentType.Bumpy, ArenaSegmentType.Ramp, ArenaSegmentType.TireObstacle,
            ArenaSegmentType.Lava, ArenaSegmentType.FloatingPlatform, ArenaSegmentType.LongJump,
            ArenaSegmentType.TechnicalClimb, ArenaSegmentType.TechnicalDescent,
            ArenaSegmentType.MixedObstacle, ArenaSegmentType.Recovery
        };

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
            if (profile != null && profile.useLastHorizonTerrain)
                return SampleLastHorizonHeight(profile, worldX);
            if (profile != null && profile.useMountainsTerrain)
                return SampleMountainsHeight(profile, worldX);
            if (profile != null && profile.useArenaTerrain)
                return SampleArenaHeight(profile, worldX);
            if (profile != null && profile.useCityHighwayTerrain)
                return SampleCityHighwayHeight(profile, worldX);
            if (profile != null && profile.supportsLiquidExposure)
                return SampleNeonHeight(profile, worldX);
            if (profile != null && profile.useSpaceTerrain)
                return SampleSpaceHeight(profile, worldX);
            float general = profile != null && profile.elevationProfile != null
                ? profile.elevationProfile.GetHeightAtDistance(worldX)
                : SampleProceduralProfileHeight(profile, worldX);
            float height = SampleCountrysideSpecialHeight(profile, worldX, general);
            return ApplyCountrysideOpeningTerrain(profile, worldX, height);
        }

        public static float SampleProfileHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            if (profile.useLastHorizonTerrain) return SampleLastHorizonHeight(profile, worldX);
            if (profile.useMountainsTerrain) return SampleMountainsHeight(profile, worldX);
            if (profile.useArenaTerrain) return SampleArenaHeight(profile, worldX);
            if (profile.useCityHighwayTerrain) return SampleCityHighwayHeight(profile, worldX);
            if (profile.supportsLiquidExposure) return SampleNeonHeight(profile, worldX);
            if (profile.useSpaceTerrain) return SampleSpaceHeight(profile, worldX);
            float general = SampleProceduralProfileHeight(profile, worldX);
            float height = SampleCountrysideSpecialHeight(profile, worldX, general);
            return ApplyCountrysideOpeningTerrain(profile, worldX, height);
        }

        private static float ApplyCountrysideOpeningTerrain(MapProfile profile, float worldX,
            float generatedHeight)
        {
            const float unchangedTerrainStart = 2000f;
            if (profile == null || profile.mapId != "countryside" || worldX < 0f ||
                worldX >= unchangedTerrainStart) return generatedHeight;

            float distance = Mathf.Max(0f, worldX);
            float launchEnvelope = Smooth01(Mathf.InverseLerp(0f, 35f, distance));

            // Broad hills use domain-warped noise so their height, spacing and crest position do not
            // repeat as a visible /\ pattern. Peaks are rounded and followed by a readable descent.
            float openingDifficulty = Smooth01(Mathf.InverseLerp(0f, unchangedTerrainStart, distance));
            float broadWarp = (Mathf.PerlinNoise(distance * .0031f + 18.472f, 2.713f) - .5f) * 72f;
            float broadWave = .5f + .5f * Mathf.Sin((distance + broadWarp) * TwoPi / 238f + .38f);
            float broadNoise = Mathf.PerlinNoise(distance * .0073f + 41.83f, 5.17f);
            float broadShape = Mathf.Clamp01(broadWave * .68f + broadNoise * .32f);
            float broadAmplitude = Mathf.Lerp(2.6f, 5.4f, openingDifficulty);
            float authoredOpening = profile.baseHeight +
                                     broadShape * broadAmplitude * launchEnvelope;

            // The first 500 m must already teach throttle and rotation control. Three different,
            // guaranteed shapes prevent the opening from becoming a long sightseeing slope.
            authoredOpening += SampleCountrysideIntroChallenges(distance) * launchEnvelope;

            // 500-2000 m: guarantee a real driving challenge roughly every 157 m. Each mountain has a
            // first climb into a shoulder, a second steeper climb on top of it, then a full descent.
            // Seeded shoulder/apex positions and height scales keep consecutive obstacles distinct.
            float mountainGate = Smooth01(Mathf.InverseLerp(500f, 545f, distance));
            float mountainDifficulty = Smooth01(Mathf.InverseLerp(500f, 1850f, distance));
            float mountainShape = SampleCountrysideStackedMountain(profile, distance);
            float mountainAmplitude = Mathf.Lerp(6f, 15.5f, mountainDifficulty) * mountainGate;
            authoredOpening += mountainShape * mountainAmplitude;
            authoredOpening += SampleCountrysideOpeningMiniBumps(profile, distance);

            // Rejoin the exact pre-existing profile before the protected 2000 m boundary. At and after
            // 2000 m this method is an exact pass-through and cannot modify a single terrain sample.
            float restoreOriginal = Smooth01(Mathf.InverseLerp(1850f, unchangedTerrainStart, distance));
            return Mathf.Lerp(authoredOpening, generatedHeight, restoreOriginal);
        }

        private static float SampleCountrysideIntroChallenges(float distance)
        {
            // 45-165 m: a small climb immediately followed by a deep, visible dip and recovery.
            if (distance >= 45f && distance < 95f)
                return Mathf.Lerp(0f, 3.8f,
                    Smooth01(Mathf.InverseLerp(45f, 95f, distance)));
            if (distance >= 95f && distance < 125f)
                return Mathf.Lerp(3.8f, -3.0f,
                    Smooth01(Mathf.InverseLerp(95f, 125f, distance)));
            if (distance >= 125f && distance < 165f)
                return Mathf.Lerp(-3.0f, 0f,
                    Smooth01(Mathf.InverseLerp(125f, 165f, distance)));

            // 195-295 m: one intentionally recognizable /\ hill, softened only enough to avoid an
            // artificial collider corner at its summit.
            if (distance >= 195f && distance < 295f)
            {
                float t = Mathf.InverseLerp(195f, 295f, distance);
                const float apex = .40f;
                float triangle = t <= apex ? t / apex : 1f - (t - apex) / (1f - apex);
                float naturalTriangle = Mathf.Lerp(triangle, Smooth01(triangle), .32f);
                return naturalTriangle * 6.8f;
            }

            // 330-500 m: climb once, climb again more steeply, then descend below the approach level.
            // The drop is strong enough to unsettle a careless vehicle but remains below later difficulty.
            if (distance >= 330f && distance < 375f)
                return Mathf.Lerp(0f, 3.2f,
                    Smooth01(Mathf.InverseLerp(330f, 375f, distance)));
            if (distance >= 375f && distance < 410f)
                return Mathf.Lerp(3.2f, 7.8f,
                    Smooth01(Mathf.InverseLerp(375f, 410f, distance)));
            if (distance >= 410f && distance < 452f)
                return Mathf.Lerp(7.8f, -3.0f,
                    Smooth01(Mathf.InverseLerp(410f, 452f, distance)));
            if (distance >= 452f && distance < 500f)
                return Mathf.Lerp(-3.0f, 0f,
                    Smooth01(Mathf.InverseLerp(452f, 500f, distance)));
            return 0f;
        }

        private static float SampleCountrysideStackedMountain(MapProfile profile, float distance)
        {
            const float challengeStart = 500f;
            const float challengeLength = 157f;
            if (distance <= challengeStart) return 0f;

            float challengePosition = (distance - challengeStart) / challengeLength;
            int challengeIndex = Mathf.Max(0, Mathf.FloorToInt(challengePosition));
            float t = Mathf.Clamp01(challengePosition - challengeIndex);
            uint seed = CombinedSeed(profile);
            float shoulderEnd = Mathf.Lerp(.42f, .53f,
                Hash01(seed, challengeIndex * 1877 + 401));
            float apex = Mathf.Lerp(.67f, .78f,
                Hash01(seed, challengeIndex * 1889 + 409));
            float shoulderHeight = Mathf.Lerp(.34f, .49f,
                Hash01(seed, challengeIndex * 1901 + 419));
            float heightScale = Mathf.Lerp(.84f, 1.18f,
                Hash01(seed, challengeIndex * 1913 + 431));

            float shape;
            if (t <= shoulderEnd)
            {
                float climb = Smooth01(Mathf.InverseLerp(0f, shoulderEnd, t));
                shape = Mathf.Lerp(0f, shoulderHeight, climb);
            }
            else if (t <= apex)
            {
                float steepClimb = Smooth01(Mathf.InverseLerp(shoulderEnd, apex, t));
                shape = Mathf.Lerp(shoulderHeight, 1f, steepClimb);
            }
            else
            {
                float descent = Smooth01(Mathf.InverseLerp(apex, 1f, t));
                shape = Mathf.Lerp(1f, 0f, descent);
            }
            return shape * heightScale;
        }

        private static float SampleCountrysideOpeningMiniBumps(MapProfile profile, float distance)
        {
            if (distance < 500f || distance >= 1850f) return 0f;

            float startGate = Smooth01(Mathf.InverseLerp(500f, 515f, distance));
            float endGate = 1f - Smooth01(Mathf.InverseLerp(1810f, 1850f, distance));
            float difficulty = Smooth01(Mathf.InverseLerp(500f, 1850f, distance));

            // The first group is guaranteed and immediately visible after 500 m: two pointed bumps
            // separated by noticeable dips, so this boundary can never look like a straight ramp.
            if (distance < 610f)
            {
                float t = Mathf.InverseLerp(500f, 610f, distance);
                float firstGroup = PointedPulse(t, .16f, .12f, 1.35f) * 1.05f -
                                   PointedPulse(t, .39f, .10f, 1.45f) * .72f +
                                   PointedPulse(t, .63f, .13f, 1.32f) * 1.22f -
                                   PointedPulse(t, .84f, .09f, 1.52f) * .58f;
                return firstGroup * 1.45f * startGate;
            }

            // Later groups vary their centres, widths, polarity and strength per cell. The large mountain
            // underneath supplies the main climb while these compact shapes keep its sides technically busy.
            const float cellLength = 91f;
            float localDistance = distance - 610f;
            int cell = Mathf.Max(0, Mathf.FloorToInt(localDistance / cellLength));
            float cellT = Mathf.Repeat(localDistance, cellLength) / cellLength;
            uint seed = CombinedSeed(profile);
            float firstCentre = Mathf.Lerp(.18f, .32f, Hash01(seed, cell * 1733 + 1741));
            float secondCentre = Mathf.Lerp(.45f, .60f, Hash01(seed, cell * 1747 + 1753));
            float thirdCentre = Mathf.Lerp(.72f, .86f, Hash01(seed, cell * 1759 + 1777));
            float firstWidth = Mathf.Lerp(.085f, .145f, Hash01(seed, cell * 1783 + 1787));
            float secondWidth = Mathf.Lerp(.075f, .13f, Hash01(seed, cell * 1789 + 1801));
            float thirdWidth = Mathf.Lerp(.08f, .14f, Hash01(seed, cell * 1811 + 1823));
            float middlePolarity = Hash01(seed, cell * 1831 + 1847) < .68f ? -1f : 1f;
            float lastPolarity = Hash01(seed, cell * 1861 + 1867) < .52f ? -1f : 1f;
            float detail = PointedPulse(cellT, firstCentre, firstWidth, 1.32f) *
                           Mathf.Lerp(.82f, 1.18f, Hash01(seed, cell * 1871 + 1873)) +
                           PointedPulse(cellT, secondCentre, secondWidth, 1.42f) * middlePolarity *
                           Mathf.Lerp(.62f, 1.05f, Hash01(seed, cell * 1877 + 1879)) +
                           PointedPulse(cellT, thirdCentre, thirdWidth, 1.38f) * lastPolarity *
                           Mathf.Lerp(.74f, 1.12f, Hash01(seed, cell * 1889 + 1901));
            return detail * Mathf.Lerp(1.35f, 2.85f, difficulty) * startGate * endGate;
        }

        private static float SampleCountrysideSpecialHeight(MapProfile profile, float worldX,
            float generalHeight)
        {
            if (!TryGetCountrysideSpecialFeature(profile, worldX, out CountrysideSpecialFeatureLayout layout))
                return generalHeight;

            if (worldX < layout.coreStart)
                return Mathf.Lerp(generalHeight, layout.baselineHeight,
                    Smooth01(Mathf.InverseLerp(layout.start, layout.coreStart, worldX)));
            if (worldX > layout.coreEnd)
                return Mathf.Lerp(layout.baselineHeight, generalHeight,
                    Smooth01(Mathf.InverseLerp(layout.coreEnd, layout.end, worldX)));
            if (layout.type == CountrysideSpecialFeatureType.LongFlat)
                return layout.baselineHeight;

            float coreMiniRelief = SampleCountrysideProgressiveMiniHills(profile, worldX) * .22f;
            if (worldX <= layout.slashStart) return layout.baselineHeight + coreMiniRelief;

            float slashLength = Mathf.Max(30f, layout.coreEnd - layout.slashStart);
            float slashT = Mathf.Clamp01((worldX - layout.slashStart) / slashLength);
            float triangle = slashT <= .5f ? slashT * 2f : (1f - slashT) * 2f;
            return layout.baselineHeight + coreMiniRelief * .55f + triangle * layout.peakHeight;
        }

        public static bool TryGetCountrysideSpecialFeature(MapProfile profile, float worldX,
            out CountrysideSpecialFeatureLayout layout)
        {
            layout = default;
            CountrysideSpecialFeatureSettings settings = profile?.countrysideSpecialFeatures;
            if (profile == null || !profile.useCountrysideSpecialFeatures || settings == null || worldX < 0f)
                return false;

            float interval = Mathf.Max(250f, settings.eventInterval);
            float first = Mathf.Max(0f, settings.firstEventDistance);
            int approximateIndex = Mathf.FloorToInt((worldX - first) / interval);
            uint seed = CombinedSeed(profile);
            for (int index = Mathf.Max(0, approximateIndex - 1); index <= approximateIndex + 1; index++)
            {
                if (Hash01(seed, index * 1201 + 307) > Mathf.Clamp01(settings.eventChance)) continue;
                float start = first + index * interval +
                              Hash01(seed, index * 1213 + 311) * Mathf.Max(0f, settings.maximumStartJitter);
                float transition = Mathf.Max(5f, settings.transitionLength);
                bool isFlat = Hash01(seed, index * 1223 + 313) < Mathf.Clamp01(settings.longFlatChance);
                float coreLength;
                float slashStart;
                if (isFlat)
                {
                    float shortLength = Mathf.Max(50f, Mathf.Min(settings.longFlatLengths.x,
                        settings.longFlatLengths.y));
                    float longLength = Mathf.Max(50f, Mathf.Max(settings.longFlatLengths.x,
                        settings.longFlatLengths.y));
                    coreLength = Mathf.Lerp(shortLength, longLength,
                        Hash01(seed, index * 1231 + 317));
                    slashStart = start + transition;
                }
                else
                {
                    coreLength = Mathf.Max(10f, settings.slashFlatLead) +
                                 Mathf.Max(30f, settings.slashLength);
                    slashStart = start + transition + Mathf.Max(10f, settings.slashFlatLead);
                }
                float coreStart = start + transition;
                float coreEnd = coreStart + coreLength;
                float end = coreEnd + transition;
                if (worldX < start || worldX > end) continue;

                float baselineOffset = SampleCountrysideProgressiveMiniHills(profile, start) +
                                       SampleCountrysideMicroTerrain(profile, start) +
                                       SampleCountrysideMountainChallenge(profile, start);
                layout = new CountrysideSpecialFeatureLayout
                {
                    index = index,
                    type = isFlat ? CountrysideSpecialFeatureType.LongFlat :
                        CountrysideSpecialFeatureType.DiagonalSlash,
                    start = start,
                    coreStart = coreStart,
                    slashStart = slashStart,
                    coreEnd = coreEnd,
                    end = end,
                    baselineHeight = profile.baseHeight + baselineOffset,
                    peakHeight = Mathf.Max(1f, settings.slashHeight) *
                                 Mathf.Lerp(.86f, 1.14f, Hash01(seed, index * 1249 + 337))
                };
                return true;
            }
            return false;
        }

        private static float SampleCountrysideProgressiveMiniHills(MapProfile profile, float distance)
        {
            CountrysideSpecialFeatureSettings settings = profile?.countrysideSpecialFeatures;
            if (profile == null || !profile.useCountrysideSpecialFeatures || settings == null) return 0f;

            float peakDistance = Mathf.Max(1000f, settings.peakDifficultyDistance);
            float progress = Smooth01(Mathf.InverseLerp(0f, peakDistance, Mathf.Max(0f, distance)));
            float launchEnvelope = Smooth01(Mathf.InverseLerp(0f, 42f, Mathf.Max(0f, distance)));
            float amplitude = Mathf.Lerp(Mathf.Max(.05f, settings.initialMiniHillHeight),
                Mathf.Max(settings.initialMiniHillHeight, settings.peakMiniHillHeight), progress);
            float minimumWave = Mathf.Max(8f, settings.miniHillMinimumWavelength);
            float maximumWave = Mathf.Max(minimumWave, settings.miniHillMaximumWavelength);
            uint seed = CombinedSeed(profile);
            float phaseA = Hash01(seed, 1301) * TwoPi;
            float phaseB = Hash01(seed, 1303) * TwoPi;
            float phaseC = Hash01(seed, 1307) * TwoPi;
            float warp = SmoothMountainNoise(seed, distance / 173f, 1319) * minimumWave * .58f +
                         SmoothMountainNoise(seed, distance / 419f, 1321) * maximumWave * .16f;
            float x = distance + warp;
            float middleWave = Mathf.Lerp(minimumWave, maximumWave,
                .34f + Hash01(seed, 1327) * .32f);
            float relief = Mathf.Sin(x * TwoPi / minimumWave + phaseA) * .48f +
                           Mathf.Sin(x * TwoPi / middleWave + phaseB) * .34f +
                           Mathf.Sin(x * TwoPi / maximumWave + phaseC) * .18f;
            return relief * amplitude * launchEnvelope;
        }

        private static float SampleCountrysideMicroTerrain(MapProfile profile, float distance)
        {
            CountrysideSpecialFeatureSettings settings = profile?.countrysideSpecialFeatures;
            if (profile == null || !profile.useCountrysideSpecialFeatures || settings == null) return 0f;

            float safeDistance = Mathf.Max(0f, distance);
            float peakDistance = Mathf.Max(1000f, settings.peakDifficultyDistance);
            float progress = Smooth01(Mathf.InverseLerp(0f, peakDistance, safeDistance));
            float launchEnvelope = Smooth01(Mathf.InverseLerp(18f, 70f, safeDistance));
            float amplitude = Mathf.Lerp(Mathf.Max(.05f, settings.initialMicroHeight),
                Mathf.Max(settings.initialMicroHeight, settings.peakMicroHeight), progress);
            float sectionLength = Mathf.Max(30f, settings.microSectionLength);
            int section = Mathf.Max(0, Mathf.FloorToInt(safeDistance / sectionLength));
            float t = Mathf.Repeat(safeDistance, sectionLength) / sectionLength;
            uint seed = CombinedSeed(profile);

            float straightRoll = Hash01(seed, section * 1451 + 1481);
            if (straightRoll < Mathf.Clamp01(settings.miniStraightChance))
            {
                // A quiet short run with isolated suspension-scale bumps. Every pulse returns to zero,
                // leaving genuinely straight pieces between its small rises and dips.
                int bumpCount = Mathf.Clamp(settings.miniStraightBumpCount, 2, 6);
                float detail = 0f;
                for (int bump = 0; bump < bumpCount; bump++)
                {
                    float spacing = (bump + 1f) / (bumpCount + 1f);
                    float centreJitter = (Hash01(seed, section * 1471 + bump * 17 + 1493) - .5f) * .08f;
                    float centre = Mathf.Clamp(spacing + centreJitter, .1f, .9f);
                    float halfWidth = Mathf.Lerp(.035f, .075f,
                        Hash01(seed, section * 1483 + bump * 19 + 1511));
                    float polarity = Hash01(seed, section * 1487 + bump * 23 + 1523) < .68f ? 1f : -.72f;
                    float strength = Mathf.Lerp(.28f, .62f,
                        Hash01(seed, section * 1499 + bump * 29 + 1531));
                    detail += PointedPulse(t, centre, halfWidth, 1.7f) * polarity * strength;
                }
                return detail * amplitude * launchEnvelope;
            }

            // Alternating compact rises and depressions break up long, repeated wave silhouettes without
            // changing the spacing or height of the large Countryside mountain challenge underneath.
            float primaryCentre = Mathf.Lerp(.32f, .66f, Hash01(seed, section * 1511 + 1543));
            float primaryWidth = Mathf.Lerp(.16f, .29f, Hash01(seed, section * 1523 + 1553));
            float primaryPolarity = Hash01(seed, section * 1531 + 1567) < .52f ? 1f : -1f;
            float primary = PointedPulse(t, primaryCentre, primaryWidth, 1.35f) * primaryPolarity;
            float secondaryCentre = primaryCentre < .5f
                ? Mathf.Lerp(.72f, .84f, Hash01(seed, section * 1543 + 1571))
                : Mathf.Lerp(.16f, .28f, Hash01(seed, section * 1553 + 1579));
            float secondary = PointedPulse(t, secondaryCentre, primaryWidth * .43f, 1.65f) *
                              -primaryPolarity * .38f;
            return (primary + secondary) * amplitude * launchEnvelope;
        }

        private static float SampleCountrysideMountainChallenge(MapProfile profile, float distance)
        {
            CountrysideSpecialFeatureSettings settings = profile?.countrysideSpecialFeatures;
            if (profile == null || !profile.useCountrysideSpecialFeatures || settings == null) return 0f;

            float startDistance = Mathf.Max(0f, settings.challengeStartDistance);
            if (distance <= startDistance) return 0f;
            float peakDistance = Mathf.Max(startDistance + 1000f, settings.peakDifficultyDistance);
            float progress = Smooth01(Mathf.InverseLerp(startDistance, peakDistance, distance));
            float cellLength = Mathf.Lerp(Mathf.Max(80f, settings.challengeMaximumCellLength),
                Mathf.Max(60f, settings.challengeMinimumCellLength), progress);
            float localDistance = distance - startDistance;
            int cell = Mathf.Max(0, Mathf.FloorToInt(localDistance / cellLength));
            float t = Mathf.Repeat(localDistance, cellLength) / cellLength;
            uint seed = CombinedSeed(profile);
            float straightChance = Mathf.Lerp(settings.initialChallengeStraightChance,
                settings.peakChallengeStraightChance, progress);
            if (Hash01(seed, cell * 1367 + 1399) < Mathf.Clamp01(straightChance)) return 0f;
            float apex = Mathf.Lerp(.36f, .64f, Hash01(seed, cell * 1373 + 1409));
            float triangle = t <= apex
                ? t / Mathf.Max(.001f, apex)
                : 1f - (t - apex) / Mathf.Max(.001f, 1f - apex);
            triangle = Mathf.Clamp01(triangle);

            float targetSlope = Mathf.Lerp(settings.initialChallengeSlope,
                settings.peakChallengeSlope, progress);
            float availableRun = cellLength * Mathf.Min(apex, 1f - apex);
            float slopeHeight = Mathf.Tan(targetSlope * Mathf.Deg2Rad) * availableRun;
            float heightLimit = Mathf.Lerp(Mathf.Max(.1f, settings.initialChallengeHeight),
                Mathf.Max(settings.initialChallengeHeight, settings.peakChallengeHeight), progress);
            float height = Mathf.Min(slopeHeight, heightLimit) *
                           Mathf.Lerp(.82f, 1.18f, Hash01(seed, cell * 1381 + 1423));
            float valleyChance = Mathf.Lerp(settings.initialDeepValleyChance,
                settings.peakDeepValleyChance, progress);
            bool deepValley = Hash01(seed, cell * 1399 + 1433) < valleyChance;
            float polarity = deepValley ? -Mathf.Max(.5f, settings.valleyDepthMultiplier) : 1f;

            // Linear sides preserve the requested pointed crests and deep V-shaped drops. The mesh
            // safety pass still clamps the final collider to the profile's maximum slope.
            return triangle * height * polarity;
        }

        private static float ApplyCountrysideChallengeStraight(MapProfile profile, float distance,
            float generatedHeight)
        {
            CountrysideSpecialFeatureSettings settings = profile?.countrysideSpecialFeatures;
            if (profile == null || !profile.useCountrysideSpecialFeatures || settings == null)
                return generatedHeight;

            float startDistance = Mathf.Max(0f, settings.challengeStartDistance);
            if (distance <= startDistance) return generatedHeight;
            float peakDistance = Mathf.Max(startDistance + 1000f, settings.peakDifficultyDistance);
            float progress = Smooth01(Mathf.InverseLerp(startDistance, peakDistance, distance));
            float cellLength = Mathf.Lerp(Mathf.Max(80f, settings.challengeMaximumCellLength),
                Mathf.Max(60f, settings.challengeMinimumCellLength), progress);
            float localDistance = distance - startDistance;
            int cell = Mathf.Max(0, Mathf.FloorToInt(localDistance / cellLength));
            float t = Mathf.Repeat(localDistance, cellLength) / cellLength;
            uint seed = CombinedSeed(profile);
            float straightChance = Mathf.Lerp(settings.initialChallengeStraightChance,
                settings.peakChallengeStraightChance, progress);
            if (Hash01(seed, cell * 1367 + 1399) >= Mathf.Clamp01(straightChance))
                return generatedHeight;

            // Blend into a genuinely linear cell. Most of its length stays flat or forms a gentle straight
            // descent, with only a few small suspension bumps. The neighbouring cells retain their full hills.
            float blend = Smooth01(Mathf.InverseLerp(0f, .16f, t)) *
                          Smooth01(Mathf.InverseLerp(1f, .84f, t));
            float baseline = profile.baseHeight + SmoothMountainNoise(seed, cell / 7f, 1621) *
                             Mathf.Lerp(.8f, 3.5f, progress);
            bool descending = Hash01(seed, cell * 1627 + 1637) < Mathf.Clamp01(settings.straightDescentChance);
            float descentHeight = descending ? Mathf.Lerp(1.2f, 5.5f, progress) : 0f;
            float linearTrend = descending ? Mathf.Lerp(descentHeight, -descentHeight, t) : 0f;
            int bumpCount = Mathf.Clamp(settings.miniStraightBumpCount, 2, 6);
            float bumpHeight = Mathf.Lerp(Mathf.Max(.05f, settings.initialMicroHeight),
                Mathf.Max(settings.initialMicroHeight, settings.peakMicroHeight), progress) * .34f;
            float bumps = 0f;
            for (int bump = 0; bump < bumpCount; bump++)
            {
                float centre = Mathf.Lerp(.23f, .77f, bumpCount <= 1 ? .5f : bump / (float)(bumpCount - 1));
                centre += (Hash01(seed, cell * 1643 + bump * 47 + 1657) - .5f) * .055f;
                float halfWidth = Mathf.Lerp(.025f, .055f,
                    Hash01(seed, cell * 1657 + bump * 53 + 1663));
                float polarity = Hash01(seed, cell * 1663 + bump * 59 + 1667) < .82f ? 1f : -.48f;
                bumps += PointedPulse(t, centre, halfWidth, 1.7f) * polarity;
            }
            float straightHeight = baseline + linearTrend + bumps * bumpHeight;
            return Mathf.Lerp(generatedHeight, straightHeight, blend);
        }

        public static float SampleMountainsHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            MountainsSettings settings = profile.mountains ?? new MountainsSettings();
            float distance = Mathf.Max(0f, worldX);
            float rhythm = SampleMountainRockRhythm(profile, settings, distance);
            float authoredIntroEnd = Mathf.Max(600f, profile.introEndDistance);
            float mountainStart = Mathf.Clamp(authoredIntroEnd * .18f, 120f, 220f);
            float easyEnd = Mathf.Max(mountainStart + 1200f, profile.easyEndDistance);
            float normalEnd = Mathf.Max(easyEnd + 1000f, profile.normalEndDistance);
            float peakDistance = Mathf.Max(Mathf.Max(normalEnd + 1000f, profile.hardEndDistance),
                settings.peakDifficultyDistance);
            float progressiveStart = Mathf.Max(normalEnd + 500f, peakDistance - 2500f);
            float easyCellLength = Mathf.Max(150f, (easyEnd - mountainStart) / 10f);
            float mediumCellLength = Mathf.Max(150f, (normalEnd - easyEnd) / 11f);
            float hardCellLength = Mathf.Max(145f, (progressiveStart - normalEnd) / 12f);
            float progressiveCellLength = Mathf.Max(140f, (peakDistance - progressiveStart) / 13f);

            // Keep only the immediate spawn readable. Pronounced, tightly spaced mountain cells take over
            // near the start instead of reserving the first kilometre for a long repeating warm-up wave.
            uint seed = CombinedSeed(profile);
            float initialGrowth = Smooth01(Mathf.InverseLerp(0f, 45f, distance));
            float initialAmplitude = Mathf.Lerp(settings.initialMiniBumpHeight,
                settings.maximumMiniBumpHeight * .72f, Mathf.InverseLerp(0f, mountainStart, distance));
            float warmupFade = 1f - Smooth01(Mathf.InverseLerp(mountainStart - 35f,
                mountainStart + 75f, distance));
            float warmupEnvelope = initialGrowth * warmupFade;
            float warmupPhase = Hash01(seed, 347) * TwoPi;
            float warmupWarp = SmoothMountainNoise(seed, distance / 97f, 349) * 18f;
            float warmupShape = Mathf.Sin((distance + warmupWarp) * TwoPi / 205f + warmupPhase) * .62f +
                                Mathf.Sin((distance + warmupWarp * .45f) * TwoPi / 91f +
                                    warmupPhase * .43f) * .38f;
            float warmupHeight = Mathf.Max(1f, settings.warmupMountainHeight) *
                                 Mathf.Lerp(.28f, 1f,
                                     Smooth01(Mathf.InverseLerp(0f, mountainStart, distance)));
            float initial = profile.baseHeight + SampleMountainMacroRelief(profile, settings, distance) +
                            warmupShape * warmupHeight * warmupEnvelope +
                            rhythm * initialAmplitude * initialGrowth;
            if (distance <= mountainStart - 35f) return initial;

            float mountain;
            if (distance < easyEnd)
                mountain = SampleMountainCell(profile, settings, distance, mountainStart, easyCellLength,
                    settings.easySlopeRange, false);
            else if (distance < normalEnd)
                mountain = SampleMountainCell(profile, settings, distance, easyEnd, mediumCellLength,
                    settings.mediumSlopeRange, false);
            else if (distance < progressiveStart)
                mountain = SampleMountainCell(profile, settings, distance, normalEnd, hardCellLength,
                    settings.hardSlopeRange, false);
            else if (distance < peakDistance)
            {
                float progress = Smooth01(Mathf.InverseLerp(progressiveStart, peakDistance, distance));
                Vector2 progressiveRange = Vector2.Lerp(settings.hardSlopeRange,
                    settings.extremeSlopeRange, progress);
                mountain = SampleMountainCell(profile, settings, distance, progressiveStart,
                    progressiveCellLength,
                    progressiveRange, false);
            }
            else
                mountain = SampleMountainCell(profile, settings, distance, peakDistance, 170f,
                    settings.extremeSlopeRange, true);

            float introBlend = Smooth01(Mathf.InverseLerp(mountainStart - 35f,
                mountainStart + 75f, distance));
            return Mathf.Lerp(initial, mountain, introBlend);
        }

        public static float GetMountainsTargetSlope(MapProfile profile, float distance)
        {
            if (profile == null || !profile.useMountainsTerrain) return 0f;
            MountainsSettings settings = profile.mountains ?? new MountainsSettings();
            float d = Mathf.Max(0f, distance);
            float authoredIntroEnd = Mathf.Max(600f, profile.introEndDistance);
            float mountainStart = Mathf.Clamp(authoredIntroEnd * .18f, 120f, 220f);
            float easyEnd = Mathf.Max(mountainStart + 1200f, profile.easyEndDistance);
            float normalEnd = Mathf.Max(easyEnd + 1000f, profile.normalEndDistance);
            float peakDistance = Mathf.Max(Mathf.Max(normalEnd + 1000f, profile.hardEndDistance),
                settings.peakDifficultyDistance);
            float progressiveStart = Mathf.Max(normalEnd + 500f, peakDistance - 2500f);
            float easyCellLength = Mathf.Max(150f, (easyEnd - mountainStart) / 10f);
            float mediumCellLength = Mathf.Max(150f, (normalEnd - easyEnd) / 11f);
            float hardCellLength = Mathf.Max(145f, (progressiveStart - normalEnd) / 12f);
            float progressiveCellLength = Mathf.Max(140f, (peakDistance - progressiveStart) / 13f);
            if (d < mountainStart) return Mathf.Lerp(12f, Mathf.Min(settings.easySlopeRange.x,
                settings.easySlopeRange.y), Smooth01(Mathf.InverseLerp(0f, mountainStart, d)));
            if (d < easyEnd) return MountainCellAngle(profile, d, mountainStart, easyCellLength,
                settings.easySlopeRange, false, settings);
            if (d < normalEnd) return MountainCellAngle(profile, d, easyEnd, mediumCellLength,
                settings.mediumSlopeRange, false, settings);
            if (d < progressiveStart) return MountainCellAngle(profile, d, normalEnd, hardCellLength,
                settings.hardSlopeRange, false, settings);
            if (d < peakDistance)
            {
                float progress = Smooth01(Mathf.InverseLerp(progressiveStart, peakDistance, d));
                Vector2 progressiveRange = Vector2.Lerp(settings.hardSlopeRange,
                    settings.extremeSlopeRange, progress);
                return MountainCellAngle(profile, d, progressiveStart, progressiveCellLength,
                    progressiveRange, false, settings);
            }
            return MountainCellAngle(profile, d, peakDistance, 170f,
                settings.extremeSlopeRange, true, settings);
        }

        private static float SampleMountainCell(MapProfile profile, MountainsSettings settings, float distance,
            float phaseStart, float cellLength, Vector2 slopeRange, bool allowExceptional)
        {
            float position = Mathf.Max(0f, distance - phaseStart) / cellLength;
            int cell = Mathf.FloorToInt(position);
            float t = position - cell;
            float angle = MountainCellAngle(profile, distance, phaseStart, cellLength, slopeRange,
                allowExceptional, settings);
            uint seed = CombinedSeed(profile);
            int phaseSalt = Mathf.RoundToInt(phaseStart) * 13;
            float difficulty = Smooth01(Mathf.InverseLerp(0f,
                Mathf.Max(1000f, settings.peakDifficultyDistance), distance));
            float recovery = Mathf.Clamp(settings.recoveryWidthFraction, .03f, .18f);
            float leadingRecovery = Mathf.Clamp(recovery * Mathf.Lerp(.55f, 1.55f,
                Hash01(seed, cell * 2027 + phaseSalt + 31)), .025f, .2f);
            float trailingRecovery = Mathf.Clamp(recovery * Mathf.Lerp(.55f, 1.55f,
                Hash01(seed, cell * 2081 + phaseSalt + 43)), .025f, .2f);
            float crestVariation = Mathf.Clamp(settings.crestPositionVariation, 0f, .28f);
            float crestCenter = .5f + (Hash01(seed, cell * 2111 + phaseSalt + 59) * 2f - 1f) *
                                crestVariation;
            float crestHalf = Mathf.Clamp(settings.crestWidthFraction * Mathf.Lerp(.35f, 2.2f,
                Hash01(seed, cell * 2141 + phaseSalt + 71)), .008f, .12f) * .5f;
            float ascentStart = leadingRecovery;
            float ascentEnd = Mathf.Max(ascentStart + .08f, crestCenter - crestHalf);
            float descentEnd = 1f - trailingRecovery;
            float descentStart = Mathf.Min(descentEnd - .08f, crestCenter + crestHalf);
            float ascentRun = Mathf.Max(1f, (ascentEnd - ascentStart) * cellLength);
            float descentRun = Mathf.Max(1f, (descentEnd - descentStart) * cellLength);
            float heightScale = Mathf.Lerp(.74f, 1f, Hash01(seed, cell * 2203 + phaseSalt + 83));
            float rawHeight = Mathf.Tan(angle * Mathf.Deg2Rad) * Mathf.Min(ascentRun, descentRun);
            float maximumHeight = Mathf.Lerp(Mathf.Max(5f, settings.warmupMountainHeight * 1.5f),
                Mathf.Max(10f, settings.peakMountainHeight), difficulty);
            float height = Mathf.Min(rawHeight, maximumHeight);

            // Local variation starts before the main face instead of leaving a perfectly flat platform
            // followed by a ramp. It fades to zero only at cell boundaries, preserving exact continuity.
            float localEnvelope = Smooth01(Mathf.InverseLerp(0f, .065f, t)) *
                                  (1f - Smooth01(Mathf.InverseLerp(.935f, 1f, t)));
            bool hasVeryHardBump = TryGetVeryHardMountainObstacle(profile, settings, phaseStart,
                cell, cellLength, difficulty, out float veryHardCenter, out bool veryHardPeak);
            float veryHardOffset = hasVeryHardBump
                ? SampleVeryHardMountainObstacle(settings, t, veryHardCenter, veryHardPeak,
                    cellLength, difficulty)
                : 0f;
            bool hardBumpCourse = !hasVeryHardBump &&
                                  Hash01(seed, cell * 2393 + phaseSalt + 173) <
                                  Mathf.Lerp(settings.hardBumpChance * .55f,
                                      settings.hardBumpChance, difficulty);
            float flatChance = Mathf.Lerp(settings.initialFlatSectionChance,
                settings.peakFlatSectionChance, difficulty);
            bool isFlatBumpSection = Hash01(seed, cell * 2179 + phaseSalt + 79) < flatChance;
            if (isFlatBumpSection && cell > 0 &&
                Hash01(seed, (cell - 1) * 2179 + phaseSalt + 79) < flatChance)
                isFlatBumpSection = false;
            if (isFlatBumpSection)
            {
                float extremeChance = Mathf.Lerp(.03f, settings.peakExtremeBumpChance, difficulty);
                bool extremeBumps = hardBumpCourse ||
                                    (!hasVeryHardBump &&
                                     Hash01(seed, cell * 2191 + phaseSalt + 81) < extremeChance);
                float bumpAmplitude = extremeBumps
                    ? Mathf.Lerp(.8f, settings.peakExtremeBumpHeight, difficulty)
                    : Mathf.Lerp(.28f, 2.05f, difficulty);
                return profile.baseHeight + SampleMountainMacroRelief(profile, settings, distance) +
                       SampleMountainBumpCourse(profile, distance, difficulty) * bumpAmplitude * localEnvelope +
                       veryHardOffset;
            }

            float shape;
            if (t <= ascentStart || t >= descentEnd) shape = 0f;
            else if (t < ascentEnd) shape = Smooth01((t - ascentStart) /
                                                      Mathf.Max(.001f, ascentEnd - ascentStart));
            else if (t <= descentStart) shape = 1f;
            else shape = Smooth01(1f - (t - descentStart) /
                                  Mathf.Max(.001f, descentEnd - descentStart));

            int topology = ResolveMountainTopology(seed, cell, phaseSalt);
            if (topology == 1)
            {
                // A rounded dome has a softer start and finish than the angular default peak.
                shape = Smooth01(Mathf.Clamp01(shape)) * .64f;
            }
            else if (topology == 2 && shape > 0f)
            {
                // Split the high point into two readable summits without creating a collider spike.
                float notchRadius = Mathf.Max(.045f, crestHalf * 2.8f);
                float notch = 1f - Mathf.Clamp01(Mathf.Abs(t - crestCenter) / notchRadius);
                shape *= 1f - Smooth01(notch) * .2f;
                heightScale *= .82f;
            }
            else if (topology == 3 && shape > 0f)
            {
                // Uneven shoulders interrupt the repeated straight ascent/straight descent silhouette.
                float activeT = Mathf.InverseLerp(ascentStart, descentEnd, t);
                float shoulder = Mathf.Sin(activeT * Mathf.PI * 2f +
                    Hash01(seed, cell * 2269 + phaseSalt + 113) * Mathf.PI) * .075f;
                shape = Mathf.Clamp01(shape + shoulder * shape * (1f - shape));
                heightScale *= .88f;
            }
            else if (topology == 4 && shape > 0f)
            {
                // A low secondary knoll creates a two-stage approach or exit.
                bool placeAfter = Hash01(seed, cell * 2293 + phaseSalt + 127) > .5f;
                float knollCenter = placeAfter ? Mathf.Lerp(descentStart, descentEnd, .62f) :
                    Mathf.Lerp(ascentStart, ascentEnd, .38f);
                float knoll = 1f - Mathf.Clamp01(Mathf.Abs(t - knollCenter) / .09f);
                shape = Mathf.Max(shape * .82f, Smooth01(knoll) * .28f);
                heightScale *= .82f;
            }

            float valleyChance = Mathf.Lerp(settings.valleyChance * .55f,
                settings.valleyChance, difficulty);
            float clampedValleyChance = Mathf.Clamp(valleyChance, 0f, .35f);
            bool isValley = Hash01(seed, cell * 2333 + phaseSalt + 139) < clampedValleyChance;
            if (isValley && cell > 0 &&
                Hash01(seed, (cell - 1) * 2333 + phaseSalt + 139) < clampedValleyChance)
                isValley = false;
            if (isValley)
            {
                float depthScale = Mathf.Lerp(.62f, 1.08f,
                    Hash01(seed, cell * 2357 + phaseSalt + 149));
                shape *= -depthScale;
            }

            bool bumpyClimb = Hash01(seed, cell * 2371 + phaseSalt + 157) <
                              Mathf.Lerp(settings.bumpyClimbChance * .55f,
                                  settings.bumpyClimbChance, difficulty);
            // Some faces stay clean for speed and others carry suspension-scale rock rhythm. The detail
            // remains small compared with the mountain, so it cannot turn a climbable face into a wall.
            float detailAmplitude = bumpyClimb
                ? Mathf.Lerp(.15f, settings.maximumMiniBumpHeight, difficulty)
                : Mathf.Lerp(.09f, .22f, difficulty);
            float hardBumpOffset = hardBumpCourse
                ? SampleMountainBumpCourse(profile, distance, difficulty) *
                  Mathf.Lerp(.9f, settings.peakExtremeBumpHeight * .72f, difficulty) * localEnvelope
                : 0f;
            return profile.baseHeight + SampleMountainMacroRelief(profile, settings, distance) +
                   shape * height * heightScale +
                   SampleMountainRockRhythm(profile, settings, distance) * detailAmplitude * localEnvelope +
                   hardBumpOffset + veryHardOffset;
        }

        private static int ResolveMountainTopology(uint seed, int cell, int phaseSalt)
        {
            const int topologyCount = 5;
            int safeCell = Mathf.Max(0, cell);
            int block = safeCell / topologyCount;
            int slot = safeCell % topologyCount;
            int start = Mathf.Min(topologyCount - 1, Mathf.FloorToInt(Hash01(seed,
                block * 2237 + phaseSalt + 101) * topologyCount));
            int step = 1 + Mathf.Min(topologyCount - 2, Mathf.FloorToInt(Hash01(seed,
                block * 2251 + phaseSalt + 107) * (topologyCount - 1)));

            if (block > 0)
            {
                int previousBlock = block - 1;
                int previousStart = Mathf.Min(topologyCount - 1, Mathf.FloorToInt(Hash01(seed,
                    previousBlock * 2237 + phaseSalt + 101) * topologyCount));
                int previousStep = 1 + Mathf.Min(topologyCount - 2, Mathf.FloorToInt(Hash01(seed,
                    previousBlock * 2251 + phaseSalt + 107) * (topologyCount - 1)));
                int previousLast = (previousStart + (topologyCount - 1) * previousStep) % topologyCount;
                if (start == previousLast) start = (start + 1) % topologyCount;
            }

            return (start + slot * step) % topologyCount;
        }

        private static float MountainCellAngle(MapProfile profile, float distance, float phaseStart,
            float cellLength, Vector2 range, bool allowExceptional, MountainsSettings settings)
        {
            int cell = Mathf.Max(0, Mathf.FloorToInt((Mathf.Max(phaseStart, distance) - phaseStart) / cellLength));
            uint seed = CombinedSeed(profile);
            float variant = Hash01(seed, cell * 1877 + Mathf.RoundToInt(phaseStart) * 13 + 97);
            int interval = Mathf.Max(3, settings.exceptionalEveryCells);
            if (allowExceptional && PositiveModulo(cell + profile.seed, interval) == interval - 1)
                return Mathf.Clamp(settings.exceptionalSlope - (1f - variant) * 1.25f,
                    Mathf.Max(range.x, range.y), 60f);
            return Mathf.Lerp(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y), variant);
        }

        private static float SampleMountainBumpCourse(MapProfile profile, float distance, float difficulty)
        {
            uint seed = CombinedSeed(profile);
            float phaseA = Hash01(seed, 613) * TwoPi;
            float phaseB = Hash01(seed, 617) * TwoPi;
            float mainWave = Mathf.Lerp(36f, 25f, difficulty);
            float secondaryWave = Mathf.Lerp(18f, 12.5f, difficulty);
            float warp = SmoothMountainNoise(seed, distance / 145f, 619) * 4.5f;
            float x = distance + warp;
            return Mathf.Sin(x * TwoPi / mainWave + phaseA) * .72f +
                   Mathf.Sin(x * TwoPi / secondaryWave + phaseB) * .28f;
        }

        private static bool TryGetVeryHardMountainObstacle(MapProfile profile, MountainsSettings settings,
            float phaseStart, int cell, float cellLength, float difficulty, out float centerT,
            out bool isPeak)
        {
            centerT = .5f;
            isPeak = false;
            float chance = Mathf.Clamp01(settings.veryHardBumpChance) * difficulty;
            if (chance <= 0f) return false;

            uint seed = CombinedSeed(profile);
            float cooldown = Mathf.Max(600f, settings.veryHardBumpCooldownDistance);
            float period = cooldown * 2f;
            float cellStart = phaseStart + cell * cellLength;
            float cellEnd = cellStart + cellLength;
            int firstWindow = Mathf.FloorToInt(cellStart / period) - 1;
            int lastWindow = Mathf.FloorToInt(cellEnd / period) + 1;
            float margin = Mathf.Min(cellLength * .18f, 44f);
            for (int window = firstWindow; window <= lastWindow; window++)
            {
                if (window < 0 || Hash01(seed, window * 2411 + 181) >= chance) continue;
                float candidate = window * period + Mathf.Lerp(.25f, .75f,
                    Hash01(seed, window * 2437 + 191)) * period;
                if (candidate <= cellStart + margin || candidate >= cellEnd - margin) continue;
                centerT = Mathf.InverseLerp(cellStart, cellEnd, candidate);
                isPeak = Hash01(seed, window * 2447 + 199) >= .5f;
                return true;
            }
            return false;
        }

        private static float SampleVeryHardMountainObstacle(MountainsSettings settings, float t,
            float centerT, bool isPeak, float cellLength, float difficulty)
        {
            float widthFraction = isPeak ? Mathf.Lerp(.15f, .18f, difficulty) :
                Mathf.Lerp(.18f, .22f, difficulty);
            float halfWidth = Mathf.Max(8f / Mathf.Max(80f, cellLength), widthFraction * .5f);
            float normalizedRadius = Mathf.Abs(t - centerT) / Mathf.Max(.001f, halfWidth);
            if (normalizedRadius >= 1f) return 0f;
            float obstacleShape = Smooth01(1f - normalizedRadius);
            float height = Mathf.Lerp(6f, Mathf.Max(8f, settings.veryHardBumpHeight), difficulty);
            height *= Mathf.Lerp(.84f, 1f,
                Hash01(2166136261u, Mathf.RoundToInt(centerT * 100000f) + (isPeak ? 251 : 257)));
            return obstacleShape * height * (isPeak ? 1f : -1f);
        }

        private static float SampleMountainRockRhythm(MapProfile profile, MountainsSettings settings,
            float distance)
        {
            uint seed = CombinedSeed(profile);
            float phaseA = Hash01(seed, 401) * TwoPi;
            float phaseB = Hash01(seed, 409) * TwoPi;
            float minWave = Mathf.Max(4f, settings.miniBumpMinimumWavelength);
            float maxWave = Mathf.Max(minWave, settings.miniBumpMaximumWavelength);
            float domainWarp = SmoothMountainNoise(seed, distance / 83f, 467) * maxWave * .72f +
                               SmoothMountainNoise(seed, distance / 191f, 479) * maxWave * .38f;
            float warpedDistance = distance + domainWarp;
            float wavelengthBlend = .5f + SmoothMountainNoise(seed, distance / 127f, 487) * .24f;
            float middleWave = Mathf.Lerp(minWave, maxWave, Mathf.Clamp01(wavelengthBlend));
            return Mathf.Sin(warpedDistance * TwoPi / minWave + phaseA) * .44f +
                   Mathf.Sin(warpedDistance * TwoPi / middleWave + phaseB) * .34f +
                   Mathf.Sin(warpedDistance * TwoPi / maxWave + phaseA * .37f) * .16f +
                   SmoothMountainNoise(seed, distance / 31f, 499) * .06f;
        }

        private static float SampleMountainMacroRelief(MapProfile profile, MountainsSettings settings,
            float distance)
        {
            uint seed = CombinedSeed(profile);
            float wavelength = Mathf.Max(300f, settings.macroReliefWavelength);
            float growth = Smooth01(Mathf.InverseLerp(35f,
                Mathf.Max(1200f, settings.peakDifficultyDistance), distance));
            float broad = SmoothMountainNoise(seed, distance / wavelength, 541) * .62f;
            float distant = SmoothMountainNoise(seed, distance / (wavelength * 2.35f), 557) * .38f;
            return (broad + distant) * Mathf.Max(0f, settings.macroReliefHeight) * growth;
        }

        private static float SmoothMountainNoise(uint seed, float position, int salt)
        {
            int lower = Mathf.FloorToInt(position);
            float t = Smooth01(position - lower);
            float a = Hash01(seed, lower * 1597 + salt) * 2f - 1f;
            float b = Hash01(seed, (lower + 1) * 1597 + salt) * 2f - 1f;
            return Mathf.Lerp(a, b, t);
        }

        public static void GetLastHorizonSpecialPhases(MapProfile profile, float start, float end,
            List<LastHorizonSpecialPhaseLayout> results)
        {
            if (results == null) return;
            results.Clear();
            if (profile == null || !profile.useLastHorizonTerrain || profile.lastHorizon == null ||
                end < 0f || end < start) return;

            int firstCycle = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, start) /
                LastHorizonSettings.RequiredCycleLength));
            int lastCycle = Mathf.Max(firstCycle, Mathf.FloorToInt(Mathf.Max(0f, end) /
                LastHorizonSettings.RequiredCycleLength));
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                LastHorizonSpecialPhaseLayout[] phases = GetLastHorizonSpecialPhasesForCycle(profile, cycle);
                for (int i = 0; i < phases.Length; i++)
                    if (phases[i].end >= start && phases[i].start <= end)
                        results.Add(phases[i]);
            }
            results.Sort((a, b) => a.start.CompareTo(b.start));
        }

        public static bool TryGetLastHorizonSpecialPhase(MapProfile profile, float worldX,
            out LastHorizonSpecialPhaseLayout phase)
        {
            phase = default;
            if (profile == null || !profile.useLastHorizonTerrain || profile.lastHorizon == null ||
                worldX < 0f) return false;
            int cycle = Mathf.FloorToInt(worldX / LastHorizonSettings.RequiredCycleLength);
            LastHorizonSpecialPhaseLayout[] phases = GetLastHorizonSpecialPhasesForCycle(profile, cycle);
            for (int i = 0; i < phases.Length; i++)
                if (phases[i].Contains(worldX))
                {
                    phase = phases[i];
                    return true;
                }
            return false;
        }

        public static bool TrySampleLastHorizonSpecialPickupSurface(MapProfile profile, float worldX,
            out float surfaceY, out float pickupClearance)
        {
            surfaceY = 0f;
            pickupClearance = .68f;
            if (!TryGetLastHorizonSpecialPhase(profile, worldX,
                    out LastHorizonSpecialPhaseLayout phase) || !phase.ContainsGap(worldX)) return false;

            LastHorizonSettings settings = profile.lastHorizon;
            switch (phase.type)
            {
                case LastHorizonSpecialPhaseType.DescendingVoidPlatforms:
                {
                    float progress = Mathf.InverseLerp(phase.GapStart, phase.GapEnd, worldX);
                    float entryY = SampleLastHorizonHeight(profile, phase.GapStart);
                    float exitY = SampleLastHorizonHeight(profile, phase.GapEnd);
                    float descent = Mathf.Pow(
                                        Mathf.Max(0f, Mathf.Sin(Mathf.PI * progress)), .78f) *
                                    Mathf.Max(.5f, settings.voidPlatformMaximumDrop);
                    surfaceY = Mathf.Lerp(entryY, exitY, progress) - descent;
                    pickupClearance = 1.35f;
                    return true;
                }
                case LastHorizonSpecialPhaseType.MovingExtremeTerrain:
                    // The moving surface oscillates around the normal height. Extra clearance keeps
                    // the can reachable throughout its full vertical travel and over the small spikes.
                    surfaceY = SampleLastHorizonHeight(profile, worldX);
                    pickupClearance = Mathf.Max(1.6f,
                        settings.movingVerticalTravel + settings.extremeBumpHeight + .45f);
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsLastHorizonSpecialPhaseReserved(MapProfile profile, float worldX,
            float margin = 0f)
        {
            if (profile == null || !profile.useLastHorizonTerrain || profile.lastHorizon == null ||
                worldX < 0f) return false;
            float safeMargin = Mathf.Max(0f, margin);
            int centerCycle = Mathf.FloorToInt(worldX / LastHorizonSettings.RequiredCycleLength);
            for (int cycle = Mathf.Max(0, centerCycle - 1); cycle <= centerCycle + 1; cycle++)
            {
                LastHorizonSpecialPhaseLayout[] phases = GetLastHorizonSpecialPhasesForCycle(profile, cycle);
                for (int i = 0; i < phases.Length; i++)
                    if (worldX >= phases[i].start - safeMargin && worldX <= phases[i].end + safeMargin)
                        return true;
            }
            return false;
        }

        private static LastHorizonSpecialPhaseLayout[] GetLastHorizonSpecialPhasesForCycle(
            MapProfile profile, int cycleIndex)
        {
            long cacheKey = ((long)profile.GetInstanceID() << 32) ^ (uint)cycleIndex;
            if (LastHorizonSpecialPhaseCache.TryGetValue(cacheKey,
                    out LastHorizonSpecialPhaseLayout[] cached)) return cached;

            LastHorizonSettings settings = profile.lastHorizon;
            uint seed = CombinedSeed(profile);
            float cycleStart = cycleIndex * LastHorizonSettings.RequiredCycleLength;
            float cycleEnd = cycleStart + LastHorizonSettings.RequiredCycleLength;
            float transition = Mathf.Max(20f, settings.specialPhaseTransitionLength);
            var phases = new List<LastHorizonSpecialPhaseLayout>(8);

            AddRecurringLastHorizonPhases(phases, seed, cycleIndex, cycleStart, cycleEnd,
                Mathf.Clamp(settings.movingTerrainInterval, 5000f, 7000f), 2000f,
                settings.movingTerrainLengthRange, LastHorizonSpecialPhaseType.MovingExtremeTerrain,
                transition, 4211);
            AddRecurringLastHorizonPhases(phases, seed, cycleIndex, cycleStart, cycleEnd,
                Mathf.Clamp(settings.voidPlatformInterval, 7000f, 10000f), 5000f,
                settings.voidPlatformLengthRange, LastHorizonSpecialPhaseType.DescendingVoidPlatforms,
                transition, 5279);
            phases.Sort((a, b) => a.start.CompareTo(b.start));

            var free = new List<Vector2>(6);
            float cursor = cycleStart + (cycleIndex == 0
                ? Mathf.Max(320f, settings.launchSafetyLength + 140f) : 320f);
            float limit = cycleEnd - 320f;
            float separation = Mathf.Max(20f, settings.specialPhaseSeparation);
            for (int i = 0; i < phases.Count; i++)
            {
                float blockedStart = Mathf.Max(cycleStart, phases[i].start - separation);
                float blockedEnd = Mathf.Min(cycleEnd, phases[i].end + separation);
                if (blockedEnd <= cursor) continue;
                if (blockedStart - cursor >= 240f) free.Add(new Vector2(cursor, blockedStart));
                cursor = Mathf.Max(cursor, blockedEnd);
            }
            if (limit - cursor >= 240f) free.Add(new Vector2(cursor, limit));

            int minimumRoofs = Mathf.Clamp(settings.minimumRoofZonesPerCycle, 2, 3);
            int maximumRoofs = Mathf.Clamp(settings.maximumRoofZonesPerCycle, minimumRoofs, 3);
            int roofCount = minimumRoofs;
            if (maximumRoofs > minimumRoofs && Hash01(seed, cycleIndex * 6151 + 601) >= .45f)
                roofCount++;
            AddLastHorizonRoofPhases(phases, free, settings, seed, cycleIndex, roofCount, transition);
            phases.Sort((a, b) => a.start.CompareTo(b.start));
            cached = phases.ToArray();
            LastHorizonSpecialPhaseCache[cacheKey] = cached;
            return cached;
        }

        private static void AddRecurringLastHorizonPhases(List<LastHorizonSpecialPhaseLayout> phases,
            uint seed, int cycleIndex, float cycleStart, float cycleEnd, float interval, float offset,
            Vector2 lengthRange, LastHorizonSpecialPhaseType type, float transition, int salt)
        {
            float minimumLength = Mathf.Max(transition * 2f + 80f,
                Mathf.Min(lengthRange.x, lengthRange.y));
            float maximumLength = Mathf.Max(minimumLength, Mathf.Max(lengthRange.x, lengthRange.y));
            int first = Mathf.FloorToInt((cycleStart - offset) / interval) - 1;
            int last = Mathf.CeilToInt((cycleEnd - offset) / interval) + 1;
            for (int sequence = Mathf.Max(0, first); sequence <= last; sequence++)
            {
                float phaseStart = offset + sequence * interval;
                if (phaseStart < cycleStart || phaseStart >= cycleEnd) continue;
                float length = Mathf.Lerp(minimumLength, maximumLength,
                    Hash01(seed, sequence * 6203 + salt));
                float phaseEnd = Mathf.Min(cycleEnd - 120f, phaseStart + length);
                if (phaseEnd - phaseStart < minimumLength * .8f) continue;
                phases.Add(new LastHorizonSpecialPhaseLayout
                {
                    key = LastHorizonPhaseKey(type, sequence),
                    cycleIndex = cycleIndex,
                    sequenceIndex = sequence,
                    type = type,
                    start = phaseStart,
                    end = phaseEnd,
                    phase = Hash01(seed, sequence * 6221 + salt + 17) * TwoPi,
                    transitionLength = transition
                });
            }
        }

        private static void AddLastHorizonRoofPhases(List<LastHorizonSpecialPhaseLayout> phases,
            List<Vector2> free, LastHorizonSettings settings, uint seed, int cycleIndex,
            int roofCount, float transition)
        {
            float minimumLength = Mathf.Clamp(Mathf.Min(settings.roofLengthRange.x,
                settings.roofLengthRange.y), 200f, 5000f);
            float maximumLength = Mathf.Clamp(Mathf.Max(settings.roofLengthRange.x,
                settings.roofLengthRange.y), minimumLength, 5000f);
            free.RemoveAll(gap => gap.y - gap.x < minimumLength + 40f);
            free.Sort((a, b) => (b.y - b.x).CompareTo(a.y - a.x));
            int count = Mathf.Min(roofCount, free.Count);
            for (int roof = 0; roof < count; roof++)
            {
                Vector2 gap = free[roof];
                float available = Mathf.Max(minimumLength, gap.y - gap.x - 40f);
                float roofMaximum = Mathf.Min(maximumLength, available);
                float lengthRoll = Hash01(seed, cycleIndex * 6271 + roof * 89 + 701);
                float length = Mathf.Lerp(minimumLength, roofMaximum, lengthRoll * lengthRoll);
                float slack = Mathf.Max(0f, gap.y - gap.x - length - 40f);
                float start = gap.x + 20f + slack *
                    Hash01(seed, cycleIndex * 6287 + roof * 97 + 719);
                int sequence = cycleIndex * 3 + roof;
                LastHorizonSpecialPhaseType type = roof % 2 == 1
                    ? LastHorizonSpecialPhaseType.RoofTransportCrusher
                    : LastHorizonSpecialPhaseType.RoofTunnel;
                phases.Add(new LastHorizonSpecialPhaseLayout
                {
                    key = LastHorizonPhaseKey(type, sequence),
                    cycleIndex = cycleIndex,
                    sequenceIndex = sequence,
                    type = type,
                    start = start,
                    end = start + length,
                    phase = Hash01(seed, cycleIndex * 6301 + roof * 101 + 733) * TwoPi,
                    transitionLength = type == LastHorizonSpecialPhaseType.RoofTransportCrusher
                        ? Mathf.Clamp(settings.roofTransportGapLength, 24f, 64f)
                        : transition
                });
            }
        }

        private static long LastHorizonPhaseKey(LastHorizonSpecialPhaseType type, int sequence) =>
            ((long)(int)type << 56) ^ (uint)sequence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearLastHorizonSpecialPhaseCache() => LastHorizonSpecialPhaseCache.Clear();

        public static int GetLastHorizonCycleType(float distance)
        {
            int cycle = Mathf.FloorToInt(Mathf.Max(0f, distance) /
                                         LastHorizonSettings.RequiredCycleLength);
            return PositiveModulo(cycle, 2);
        }

        public static int GetLastHorizonRepetitionIndex(float distance) => Mathf.Max(0,
            Mathf.FloorToInt(Mathf.Max(0f, distance) / LastHorizonSettings.RequiredSystemLength));

        public static int GetLastHorizonSegmentVariant(MapProfile profile, float distance)
        {
            if (profile == null) return 0;
            LastHorizonSettings settings = profile.lastHorizon ?? new LastHorizonSettings();
            float clampedDistance = Mathf.Max(0f, distance);
            int cycleType = GetLastHorizonCycleType(clampedDistance);
            int repetition = GetLastHorizonRepetitionIndex(clampedDistance);
            float cycleLocal = Mathf.Repeat(clampedDistance, LastHorizonSettings.RequiredCycleLength);
            int localSegment = Mathf.FloorToInt(cycleLocal / Mathf.Max(120f, settings.segmentLength));
            const int segmentsPerChapter = 4;
            const int chapterFamilyCount = 4;
            int chapter = localSegment / segmentsPerChapter;
            int chapterStage = PositiveModulo(localSegment, segmentsPerChapter);
            uint seed = CombinedSeed(profile);

            // A chapter always progresses through approach -> main feature -> variation -> recovery.
            // Its family rotates on every 20 km repetition, so the route is coherent at short range
            // without replaying the same mountain/mesa/valley/playground on the next system pass.
            int previousOffset = -1;
            int selectedOffset = 0;
            for (int occurrence = 0; occurrence <= repetition; occurrence++)
            {
                int salt = cycleType * 100003 + occurrence * 7919 + 1709;
                int offset = Mathf.Min(chapterFamilyCount - 1,
                    Mathf.FloorToInt(Hash01(seed, salt) * chapterFamilyCount));
                if (offset == previousOffset)
                {
                    int rotation = 1 + Mathf.FloorToInt(Hash01(seed, salt + 3571) *
                                                         (chapterFamilyCount - 1));
                    offset = (offset + rotation) % chapterFamilyCount;
                }
                previousOffset = offset;
                selectedOffset = offset;
            }
            int family = PositiveModulo(chapter + selectedOffset, chapterFamilyCount);
            return family * segmentsPerChapter + chapterStage;
        }

        public static float SampleLastHorizonHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            LastHorizonSettings settings = profile.lastHorizon ?? new LastHorizonSettings();
            float distance = Mathf.Max(0f, worldX);
            float cycleLength = LastHorizonSettings.RequiredCycleLength;
            int cycleType = GetLastHorizonCycleType(distance);
            int repetition = GetLastHorizonRepetitionIndex(distance);
            float cycleLocal = Mathf.Repeat(distance, cycleLength);
            float segmentLength = Mathf.Max(120f, settings.segmentLength);
            int localSegment = Mathf.FloorToInt(cycleLocal / segmentLength);
            float segmentT = Mathf.Repeat(cycleLocal, segmentLength) / segmentLength;
            int variant = GetLastHorizonSegmentVariant(profile, distance);
            uint seed = CombinedSeed(profile);
            int segmentSalt = cycleType * 100003 + localSegment * 4099 + repetition * 7919;
            float amplitudeVariation = Mathf.Lerp(.62f, 1.04f,
                Hash01(seed, segmentSalt + 2203));
            float maximumRelief = cycleType == 0
                ? Mathf.Max(1f, settings.cycleOneMaximumRelief)
                : Mathf.Max(1f, settings.cycleTwoMaximumRelief);
            float shape = cycleType == 0
                ? EvaluateLastHorizonCycleOne(variant, segmentT,
                    Hash01(seed, segmentSalt + 2297))
                : EvaluateLastHorizonCycleTwo(variant, segmentT,
                    Hash01(seed, segmentSalt + 2333));

            // Every 10 km identity block receives broad, repetition-specific relief that fades to
            // zero at both cycle boundaries. It changes the skyline without creating a seam at 10/20 km.
            float cycleEnvelope = Mathf.Pow(Mathf.Sin(Mathf.PI * cycleLocal / cycleLength), 2f);
            float phaseA = Hash01(seed, cycleType * 503 + repetition * 881 + 31) * TwoPi;
            float phaseB = Hash01(seed, cycleType * 557 + repetition * 929 + 47) * TwoPi;
            float broad = (Mathf.Sin(cycleLocal * TwoPi / 2180f + phaseA) * .66f +
                           Mathf.Sin(cycleLocal * TwoPi / 970f + phaseB) * .34f) *
                          Mathf.Max(0f, settings.macroRelief) * cycleEnvelope;
            float continuousBase = profile.baseHeight +
                Mathf.Sin((distance + profile.seed * .031f) * TwoPi / 6200f) * 4.5f +
                Mathf.Sin((distance + profile.seed * .071f) * TwoPi / 2670f) * 2f;
            float generated = continuousBase + broad + shape * maximumRelief * amplitudeVariation;

            float launchBlend = Smooth01(Mathf.InverseLerp(0f,
                Mathf.Max(40f, settings.launchSafetyLength), distance));
            float launch = profile.baseHeight + Mathf.Sin(distance * TwoPi / 92f) * .18f;
            return Mathf.Lerp(launch, generated, launchBlend);
        }

        private static float EvaluateLastHorizonCycleOne(int variant, float t, float detail)
        {
            float x = Mathf.Clamp01(t);
            float envelope = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * x)), 1.15f);
            switch (variant)
            {
                // Mountain-pass chapter: foothills, summit, ridgeline and a soft recovery valley.
                case 0:
                    return Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * x)), 1.45f) * .42f;
                case 1: return LastHorizonAsymmetricPulse(x, .62f, .70f);
                case 2:
                    return LastHorizonPlateau(x, .20f, .80f) * .76f +
                           envelope * Mathf.Sin(6f * Mathf.PI * x + detail * TwoPi) * .055f;
                case 3:
                    return -LastHorizonAsymmetricPulse(x, .40f, 1.05f) * .36f +
                           Mathf.Sin(TwoPi * x) * envelope * .08f;

                // Mesa chapter: long ramps, fast plateaus, terraces and a controlled descent.
                case 4: return LastHorizonPlateau(x, .30f, .74f) * .62f;
                case 5:
                    return LastHorizonPlateau(x, .12f, .88f) * .18f +
                           envelope * Mathf.Sin(8f * Mathf.PI * x) * .025f;
                case 6:
                    return LastHorizonTerraces(x) * .70f +
                           envelope * Mathf.Sin(6f * Mathf.PI * x + detail) * .035f;
                case 7: return -LastHorizonAsymmetricPulse(x, .68f, .72f) * .30f;

                // Valley chapter: entry drop, deep bowl, camelbacks and a breathing sprint.
                case 8: return -LastHorizonAsymmetricPulse(x, .70f, .92f) * .38f;
                case 9:
                    return -Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * x)), .78f) * .86f +
                           Mathf.Pow(Mathf.Sin(TwoPi * x), 2f) * .12f;
                case 10: return LastHorizonWhoops(x, 2f, 1.55f) * .58f;
                case 11: return Mathf.Sin(4f * Mathf.PI * x) * envelope * .055f;

                // Playground chapter: whoops, alternating humps, tabletop and obstacle staging flat.
                case 12: return LastHorizonWhoops(x, 5f, 1.65f) * .23f;
                case 13:
                    return envelope * (Mathf.Sin(4f * Mathf.PI * x) * .25f +
                                       Mathf.Sin(8f * Mathf.PI * x + detail * TwoPi) * .10f);
                case 14: return LastHorizonPlateau(x, .24f, .68f) * .68f;
                default: return Mathf.Sin(TwoPi * x) * envelope * .035f;
            }
        }

        private static float EvaluateLastHorizonCycleTwo(int variant, float t, float detail)
        {
            float x = Mathf.Clamp01(t);
            float envelope = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * x)), 1.2f);
            switch (variant)
            {
                // Technical pass: broken foothills, asymmetric summit, narrow ridge, recovery.
                case 0:
                    return envelope * (Mathf.Sin(4f * Mathf.PI * x) * .27f +
                                       Mathf.Sin(10f * Mathf.PI * x + detail * TwoPi) * .09f);
                case 1:
                    return LastHorizonAsymmetricPulse(x, .38f, .48f) * .82f -
                           LastHorizonAsymmetricPulse(x, .78f, 1.25f) * .28f;
                case 2:
                    return LastHorizonPlateau(x, .34f, .63f) * .82f +
                           envelope * Mathf.Sin(8f * Mathf.PI * x) * .06f;
                case 3: return -LastHorizonAsymmetricPulse(x, .56f, .92f) * .32f;

                // Mechanical mesa: steeper tables, split deck, stairs and downhill rhythm.
                case 4: return LastHorizonPlateau(x, .18f, .74f) * .78f;
                case 5:
                    return (LastHorizonPlateau(x, .10f, .43f) * .48f +
                            LastHorizonPlateau(Mathf.Clamp01((x - .42f) / .58f), .22f, .78f) * .62f) *
                           envelope;
                case 6:
                    return LastHorizonTerraces(x) * .82f +
                           LastHorizonWhoops(x, 4f, 2f) * .08f;
                case 7:
                    return -LastHorizonAsymmetricPulse(x, .72f, .58f) * .42f +
                           envelope * Mathf.Sin(6f * Mathf.PI * x) * .10f;

                // Ravine chapter: bowls and counter-crests with safe exits.
                case 8:
                    return -Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * x)), .64f) * .92f;
                case 9:
                    return envelope * (Mathf.Sin(TwoPi * x) * .72f -
                                       Mathf.Sin(4f * Mathf.PI * x + detail) * .16f);
                case 10: return LastHorizonWhoops(x, 2f, 1.25f) * .72f - envelope * .20f;
                case 11: return Mathf.Sin(4f * Mathf.PI * x) * envelope * .09f;

                // Technical playground: denser but bounded obstacles separated by a staging flat.
                case 12: return LastHorizonWhoops(x, 6f, 1.85f) * .28f;
                case 13:
                    return envelope * (Mathf.Sin(5f * Mathf.PI * x) * .32f +
                                       Mathf.Sin(9f * Mathf.PI * x + detail * TwoPi) * .14f);
                case 14:
                    return LastHorizonPlateau(x, .20f, .72f) * .58f +
                           LastHorizonWhoops(x, 3f, 2.2f) * .12f;
                default: return Mathf.Sin(6f * Mathf.PI * x) * envelope * .035f;
            }
        }

        private static float LastHorizonPlateau(float t, float riseEnd, float fallStart)
        {
            float x = Mathf.Clamp01(t);
            float rise = Smooth01(Mathf.InverseLerp(0f, Mathf.Max(.01f, riseEnd), x));
            float fall = 1f - Smooth01(Mathf.InverseLerp(Mathf.Min(.99f, fallStart), 1f, x));
            return rise * fall;
        }

        private static float LastHorizonTerraces(float t)
        {
            float x = Mathf.Clamp01(t);
            float climb = (Smooth01(Mathf.InverseLerp(.05f, .16f, x)) +
                           Smooth01(Mathf.InverseLerp(.20f, .31f, x)) +
                           Smooth01(Mathf.InverseLerp(.35f, .46f, x))) / 3f;
            float descent = ((1f - Smooth01(Mathf.InverseLerp(.54f, .65f, x))) +
                             (1f - Smooth01(Mathf.InverseLerp(.69f, .80f, x))) +
                             (1f - Smooth01(Mathf.InverseLerp(.84f, .95f, x)))) / 3f;
            return Mathf.Min(climb, descent);
        }

        private static float LastHorizonWhoops(float t, float count, float power)
        {
            float x = Mathf.Clamp01(t);
            float envelope = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * x)), 1.15f);
            float wave = Mathf.Abs(Mathf.Sin(Mathf.Max(1f, count) * Mathf.PI * x));
            return Mathf.Pow(wave, Mathf.Max(.25f, power)) * envelope;
        }

        private static float LastHorizonAsymmetricPulse(float t, float centre, float power)
        {
            float distance = t < centre
                ? (centre - t) / Mathf.Max(.001f, centre)
                : (t - centre) / Mathf.Max(.001f, 1f - centre);
            return Mathf.Pow(Mathf.Clamp01(1f - distance), Mathf.Max(.25f, power));
        }

        public static bool TryGetLastHorizonObstacle(MapProfile profile, int cellIndex,
            out LastHorizonObstacleLayout layout)
        {
            layout = default;
            if (profile == null || !profile.useLastHorizonTerrain || profile.lastHorizon == null ||
                cellIndex < 0) return false;
            LastHorizonSettings settings = profile.lastHorizon;
            float cellLength = Mathf.Max(180f, settings.obstacleCellLength);
            uint seed = CombinedSeed(profile);
            float cellStart = cellIndex * cellLength;
            float center = cellStart + cellLength * Mathf.Lerp(.32f, .68f,
                Hash01(seed, cellIndex * 3571 + 101));
            if (center < settings.obstacleStartSafety) return false;
            if (IsLastHorizonSpecialPhaseReserved(profile, center,
                    Mathf.Max(20f, settings.specialPhaseSeparation))) return false;
            float cycleLocal = Mathf.Repeat(center, LastHorizonSettings.RequiredCycleLength);
            if (cycleLocal < 90f || cycleLocal > LastHorizonSettings.RequiredCycleLength - 90f)
                return false;
            int cycleType = GetLastHorizonCycleType(center);
            float chance = cycleType == 0 ? settings.cycleOneObstacleChance : settings.cycleTwoObstacleChance;
            int terrainVariant = GetLastHorizonSegmentVariant(profile, center);
            int chapterStage = PositiveModulo(terrainVariant, 4);
            // Staging/recovery pieces intentionally host more physical interactions. Major summit and
            // ravine pieces keep a lower density so the authored terrain remains readable and fair.
            chance += chapterStage == 3 ? .14f : chapterStage == 1 ? -.08f : 0f;
            if (Hash01(seed, cellIndex * 3581 + 113) >= Mathf.Clamp01(chance)) return false;

            float typeRoll = Hash01(seed, cellIndex * 3593 + 127);
            LastHorizonObstacleType type = cycleType == 0
                ? (typeRoll < .38f ? LastHorizonObstacleType.Seesaw :
                    typeRoll < .60f ? LastHorizonObstacleType.Rollers :
                    typeRoll < .76f ? LastHorizonObstacleType.LiftPlatform :
                    typeRoll < .90f ? LastHorizonObstacleType.TiltingPlatform :
                    LastHorizonObstacleType.Ramp)
                : (typeRoll < .25f ? LastHorizonObstacleType.Seesaw :
                    typeRoll < .47f ? LastHorizonObstacleType.LiftPlatform :
                    typeRoll < .70f ? LastHorizonObstacleType.Rollers :
                    typeRoll < .86f ? LastHorizonObstacleType.TiltingPlatform :
                    LastHorizonObstacleType.Ramp);
            int rollerCount = type == LastHorizonObstacleType.Rollers
                ? 2 + Mathf.FloorToInt(Hash01(seed, cellIndex * 3613 + 139) *
                                       Mathf.Max(1, settings.maximumRollers - 1)) : 0;
            rollerCount = Mathf.Clamp(rollerCount, 0, Mathf.Max(1, settings.maximumRollers));
            float width = type == LastHorizonObstacleType.Seesaw ? settings.seesawWidth :
                type == LastHorizonObstacleType.LiftPlatform ? settings.liftWidth :
                type == LastHorizonObstacleType.TiltingPlatform ? settings.tiltingPlatformWidth :
                type == LastHorizonObstacleType.Ramp ? settings.seesawWidth :
                rollerCount * settings.rollerRadius * 2.35f + settings.safeVehicleLength;
            width = Mathf.Max(width, settings.safeVehicleLength + settings.obstacleSafetyMargin * 2f);
            center = FindLastHorizonObstacleSupport(profile, settings, center, cellStart,
                cellStart + cellLength, width, cycleType);
            float phaseMargin = Mathf.Max(20f, settings.specialPhaseSeparation);
            if (IsLastHorizonSpecialPhaseReserved(profile, center - width * .5f, phaseMargin) ||
                IsLastHorizonSpecialPhaseReserved(profile, center, phaseMargin) ||
                IsLastHorizonSpecialPhaseReserved(profile, center + width * .5f, phaseMargin)) return false;
            layout = new LastHorizonObstacleLayout
            {
                cellIndex = cellIndex,
                cycleType = cycleType,
                repetitionIndex = GetLastHorizonRepetitionIndex(center),
                type = type,
                centerX = center,
                surfaceY = SampleLastHorizonHeight(profile, center),
                width = width,
                phase = Hash01(seed, cellIndex * 3631 + 151) * TwoPi,
                rollerCount = rollerCount
            };
            return true;
        }

        private static float FindLastHorizonObstacleSupport(MapProfile profile,
            LastHorizonSettings settings, float proposedCenter, float cellStart, float cellEnd,
            float width, int cycleType)
        {
            float halfWidth = width * .5f;
            float inset = halfWidth + Mathf.Max(2f, settings.obstacleSafetyMargin);
            float minimum = cellStart + inset;
            float maximum = cellEnd - inset;
            if (maximum <= minimum) return proposedCenter;

            float bestCenter = Mathf.Clamp(proposedCenter, minimum, maximum);
            float bestScore = float.MaxValue;
            const float searchStep = 24f;
            for (int search = 0; search <= 8; search++)
            {
                float offset = search == 0 ? 0f : ((search + 1) / 2) * searchStep *
                    (search % 2 == 1 ? -1f : 1f);
                float candidate = Mathf.Clamp(proposedCenter + offset, minimum, maximum);
                if (GetLastHorizonCycleType(candidate) != cycleType) continue;
                float cycleLocal = Mathf.Repeat(candidate, LastHorizonSettings.RequiredCycleLength);
                if (cycleLocal < 90f + halfWidth ||
                    cycleLocal > LastHorizonSettings.RequiredCycleLength - 90f - halfWidth) continue;

                float left = SampleLastHorizonHeight(profile, candidate - halfWidth);
                float middle = SampleLastHorizonHeight(profile, candidate);
                float right = SampleLastHorizonHeight(profile, candidate + halfWidth);
                float leftAngle = Mathf.Abs(Mathf.Atan2(middle - left, halfWidth) * Mathf.Rad2Deg);
                float rightAngle = Mathf.Abs(Mathf.Atan2(right - middle, halfWidth) * Mathf.Rad2Deg);
                float score = Mathf.Max(leftAngle, rightAngle) + Mathf.Abs(left - right) * 1.5f;
                if (score >= bestScore) continue;
                bestScore = score;
                bestCenter = candidate;
                if (score <= 10f) break;
            }
            return bestCenter;
        }

        public static bool TryGetLastHorizonObstacleNearDistance(MapProfile profile, float worldX,
            out LastHorizonObstacleLayout layout)
        {
            layout = default;
            if (profile == null || !profile.useLastHorizonTerrain || profile.lastHorizon == null)
                return false;
            float cellLength = Mathf.Max(180f, profile.lastHorizon.obstacleCellLength);
            int cell = Mathf.FloorToInt(Mathf.Max(0f, worldX) / cellLength);
            for (int offset = -1; offset <= 1; offset++)
            {
                if (!TryGetLastHorizonObstacle(profile, cell + offset, out LastHorizonObstacleLayout candidate))
                    continue;
                float margin = Mathf.Max(.5f, profile.lastHorizon.obstacleSafetyMargin);
                if (worldX >= candidate.Start - margin && worldX <= candidate.End + margin)
                {
                    layout = candidate;
                    return true;
                }
            }
            return false;
        }

        public static bool IsLastHorizonObstacleReserved(MapProfile profile, float worldX) =>
            TryGetLastHorizonObstacleNearDistance(profile, worldX, out _);

        private static float SampleProceduralProfileHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            float difficulty = profile.EvaluateDifficulty(distance * Core.GameManager.LogicalDistanceScale);
            float amplitudeProgress = profile.introductoryAmplitude +
                                      difficulty * (1f - profile.introductoryAmplitude);
            float featureLength = Mathf.Max(profile.sectionLength, profile.minimumValleyWidth,
                profile.minimumLandingLength * 1.5f);
            float featurePosition = worldX / featureLength;
            int featureIndex = Mathf.FloorToInt(featurePosition);
            float t = featurePosition - featureIndex;
            float featureDifficulty = profile.EvaluateDifficulty(Mathf.Max(0f, (featureIndex + 0.5f) * featureLength) * Core.GameManager.LogicalDistanceScale);
            TerrainFeatureType feature = GetFeatureType(profile, featureIndex, featureDifficulty);
            uint seed = CombinedSeed(profile);
            float variant = Hash01(seed, featureIndex * 271 + 37);
            float featureShape = profile.usePointedTerrainRhythm
                ? EvaluatePointedTerrainFeature(profile, feature, featureIndex, t,
                    variant, Mathf.Clamp01(featureDifficulty))
                : EvaluateFeature(feature, t, variant, Mathf.Clamp01(featureDifficulty),
                    profile.technicalDetailStrength);
            float frequency = profile.frequency / Mathf.Max(0.1f, profile.hillSize);
            float phase = Hash01(seed, 73) * TwoPi;
            float broadVariation = Mathf.Sin(worldX * frequency * 0.38f + phase) * 0.12f;
            float perlinVariation = (Mathf.PerlinNoise(seed * 0.000031f + worldX * frequency * 0.24f, 0.417f) * 2f - 1f) * 0.08f;
            float immediateSpaceRelief = profile.useSpaceTerrain
                ? Mathf.Sin((worldX + 34f) * TwoPi / 172f) * .045f +
                  Mathf.Sin((worldX + 9f) * TwoPi / 69f) * .018f
                : 0f;
            // Other maps retain their gentle intro. Space becomes demanding after its short safe launch.
            float startFlatten = profile.useSpaceTerrain
                ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((worldX + 20f) / 120f));
            float generatedHeight = profile.baseHeight +
                                    (featureShape + broadVariation + perlinVariation + immediateSpaceRelief) *
                                    profile.amplitude * amplitudeProgress * startFlatten;
            if (profile.useCountrysideSpecialFeatures)
            {
                generatedHeight += SampleCountrysideProgressiveMiniHills(profile, distance);
                generatedHeight += SampleCountrysideMicroTerrain(profile, distance);
                generatedHeight += SampleCountrysideMountainChallenge(profile, distance);
                generatedHeight = ApplyCountrysideChallengeStraight(profile, distance, generatedHeight);
            }
            if (!profile.useSpaceTerrain) return generatedHeight;

            float lunarDetail = SampleLunarSurfaceDetail(profile, distance, Mathf.Clamp01(difficulty));
            generatedHeight += lunarDetail;

            // The first 100 metres are deliberately ordinary and low-relief so every vehicle has a
            // reliable launch surface. Difficulty blends back in from 100 to 160 metres without a seam.
            float gentleFadeIn = Smooth01(Mathf.InverseLerp(0f, 14f, distance));
            float gentleHeight = profile.baseHeight + gentleFadeIn *
                                 (Mathf.Sin(distance * TwoPi / 128f) * .52f +
                                  Mathf.Sin(distance * TwoPi / 47f) * .11f + lunarDetail * .24f);
            float difficultTerrainBlend = Smooth01(Mathf.InverseLerp(100f, 160f, distance));
            return Mathf.Lerp(gentleHeight, generatedHeight, difficultTerrainBlend);
        }

        private static float SampleLunarSurfaceDetail(MapProfile profile, float distance, float difficulty)
        {
            uint seed = CombinedSeed(profile);
            float strength = Mathf.Lerp(.32f, 1.35f, difficulty);
            float broadCrater = SampleLunarCraterCell(seed, distance, 61f, 1201, 1.15f * strength);
            float mediumCrater = SampleLunarCraterCell(seed, distance, 27f, 1237, .52f * strength);
            float stones = SampleLunarRockCell(seed, distance, 13f, 1279, .24f * Mathf.Lerp(.65f, 1.4f, difficulty));
            return broadCrater + mediumCrater + stones;
        }

        private static float SampleLunarCraterCell(uint seed, float distance, float cellLength,
            int salt, float depth)
        {
            int cell = Mathf.FloorToInt(distance / cellLength);
            float centre = (cell + Mathf.Lerp(.24f, .76f, Hash01(seed, cell * 2371 + salt))) * cellLength;
            float radius = cellLength * Mathf.Lerp(.15f, .31f, Hash01(seed, cell * 2381 + salt + 7));
            float u = Mathf.Abs(distance - centre) / Mathf.Max(.01f, radius);
            if (u >= 1.28f) return 0f;
            float bowl = u < 1f ? -Mathf.Pow(1f - u * u, 2f) * depth : 0f;
            float rimDistance = Mathf.Abs(u - 1f);
            float rim = (1f - Smooth01(Mathf.Clamp01(rimDistance / .28f))) * depth * .26f;
            return bowl + rim;
        }

        private static float SampleLunarRockCell(uint seed, float distance, float cellLength,
            int salt, float height)
        {
            int cell = Mathf.FloorToInt(distance / cellLength);
            if (Hash01(seed, cell * 1877 + salt) < .28f) return 0f;
            float centre = (cell + Mathf.Lerp(.2f, .8f, Hash01(seed, cell * 1889 + salt + 3))) * cellLength;
            float halfWidth = cellLength * Mathf.Lerp(.08f, .18f, Hash01(seed, cell * 1901 + salt + 9));
            float u = Mathf.Abs(distance - centre) / Mathf.Max(.01f, halfWidth);
            return u < 1f ? Smooth01(1f - u) * height : 0f;
        }

        public static int GetArenaSegmentIndex(MapProfile profile, float worldX)
        {
            float length = profile != null && profile.arena != null
                ? Mathf.Max(70f, profile.arena.segmentLength) : 104f;
            return Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, worldX) / length));
        }

        public static ArenaSegmentType GetArenaSegmentType(MapProfile profile, int segmentIndex)
        {
            if (profile == null) return ArenaSegmentType.Bumpy;
            ArenaSettings settings = profile.arena ?? new ArenaSettings();
            uint key = CombinedSeed(profile) ^ (uint)Mathf.RoundToInt(settings.segmentLength * 100f) * 747796405u;
            if (!ArenaSegmentHistory.TryGetValue(key, out List<ArenaSegmentType> history))
            {
                history = new List<ArenaSegmentType>(64);
                ArenaSegmentHistory.Add(key, history);
            }
            int target = Mathf.Max(0, segmentIndex);
            while (history.Count <= target)
            {
                int index = history.Count;
                float distance = (index + .5f) * Mathf.Max(70f, settings.segmentLength);
                float difficulty = Mathf.Clamp01(profile.EvaluateDifficulty(
                    distance * Core.GameManager.LogicalDistanceScale));
                ArenaSegmentType selected = SelectArenaType(profile, settings, history, index, difficulty);
                history.Add(selected);
            }
            return history[target];
        }

        private static ArenaSegmentType SelectArenaType(MapProfile profile, ArenaSettings settings,
            List<ArenaSegmentType> history, int index, float difficulty)
        {
            if (index == 0) return ArenaSegmentType.Bumpy;
            int technicalStreak = 0;
            for (int i = history.Count - 1; i >= 0 && IsArenaTechnical(history[i]); i--) technicalStreak++;
            int recoveryInterval = Mathf.Clamp(settings.recoveryInterval, 4, 10);
            int recentCount = Mathf.Clamp(settings.recentSegmentHistory, 3, 6);
            bool recoveryRecent = false;
            for (int back = 1; back <= recentCount && history.Count - back >= 0; back++)
                if (history[history.Count - back] == ArenaSegmentType.Recovery)
                {
                    recoveryRecent = true;
                    break;
                }
            bool needsSafety = technicalStreak >= Mathf.Clamp(settings.maximumTechnicalStreak, 2, 5);
            bool scheduledRecovery = index % recoveryInterval == recoveryInterval - 1;
            if ((needsSafety || scheduledRecovery) && !recoveryRecent)
                return ArenaSegmentType.Recovery;
            if (needsSafety)
            {
                ArenaSegmentType[] safeTypes =
                {
                    ArenaSegmentType.Bumpy, ArenaSegmentType.Ramp,
                    ArenaSegmentType.TechnicalClimb, ArenaSegmentType.TechnicalDescent
                };
                for (int safe = 0; safe < safeTypes.Length; safe++)
                {
                    bool recent = false;
                    for (int back = 1; back <= recentCount && history.Count - back >= 0; back++)
                        if (history[history.Count - back] == safeTypes[safe]) { recent = true; break; }
                    if (!recent) return safeTypes[safe];
                }
            }

            uint seed = CombinedSeed(profile);
            for (int attempt = 0; attempt < 40; attempt++)
            {
                float roll = Hash01(seed, index * 977 + attempt * 131 + 41);
                ArenaSegmentType candidate = WeightedArenaCandidate(roll, difficulty);
                if (index < 5 && (candidate == ArenaSegmentType.FloatingPlatform ||
                                  candidate == ArenaSegmentType.LongJump ||
                                  candidate == ArenaSegmentType.Lava ||
                                  candidate == ArenaSegmentType.MixedObstacle)) continue;
                if (difficulty < .42f && candidate == ArenaSegmentType.MixedObstacle) continue;
                bool recent = false;
                for (int back = 1; back <= recentCount && history.Count - back >= 0; back++)
                    if (history[history.Count - back] == candidate) { recent = true; break; }
                if (!recent) return candidate;
            }
            for (int i = 0; i < ArenaCandidateTypes.Length; i++)
            {
                ArenaSegmentType candidate = ArenaCandidateTypes[i];
                if (candidate == ArenaSegmentType.Recovery ||
                    (index < 5 && (candidate == ArenaSegmentType.FloatingPlatform ||
                                   candidate == ArenaSegmentType.LongJump ||
                                   candidate == ArenaSegmentType.Lava ||
                                   candidate == ArenaSegmentType.MixedObstacle)) ||
                    (difficulty < .42f && candidate == ArenaSegmentType.MixedObstacle)) continue;
                bool recent = false;
                for (int back = 1; back <= recentCount && history.Count - back >= 0; back++)
                    if (history[history.Count - back] == candidate) { recent = true; break; }
                if (!recent) return candidate;
            }
            return ArenaSegmentType.Bumpy;
        }

        private static ArenaSegmentType WeightedArenaCandidate(float roll, float difficulty)
        {
            float[] weights =
            {
                1.35f, 1.20f, .80f + difficulty * .40f, .20f + difficulty * .70f,
                .16f + difficulty * .62f, .08f + difficulty * .48f, .95f, .95f,
                .04f + difficulty * .78f, 0f
            };
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            float choice = roll * total;
            for (int i = 0; i < weights.Length; i++)
            {
                choice -= weights[i];
                if (choice <= 0f) return ArenaCandidateTypes[i];
            }
            return ArenaSegmentType.Bumpy;
        }

        private static bool IsArenaTechnical(ArenaSegmentType type) =>
            type == ArenaSegmentType.FloatingPlatform || type == ArenaSegmentType.LongJump ||
            type == ArenaSegmentType.MixedObstacle || type == ArenaSegmentType.TireObstacle ||
            type == ArenaSegmentType.Lava;

        public static ArenaSegmentLayout GetArenaSegment(MapProfile profile, int segmentIndex)
        {
            ArenaSettings settings = profile != null && profile.arena != null
                ? profile.arena : new ArenaSettings();
            int index = Mathf.Max(0, segmentIndex);
            float length = Mathf.Max(70f, settings.segmentLength);
            float start = index * length;
            float difficulty = profile != null ? Mathf.Clamp01(profile.EvaluateDifficulty(
                (start + length * .5f) * Core.GameManager.LogicalDistanceScale)) : 0f;
            uint seed = CombinedSeed(profile);
            float variant = Hash01(seed, index * 1597 + 211);
            ArenaSegmentType type = GetArenaSegmentType(profile, index);
            var layout = new ArenaSegmentLayout
            {
                index = index, type = type, start = start, end = start + length,
                difficulty = difficulty, variant = variant,
                gapStart = -1f, gapEnd = -1f, lavaStart = -1f, lavaEnd = -1f
            };

            if (type == ArenaSegmentType.FloatingPlatform || type == ArenaSegmentType.LongJump ||
                (type == ArenaSegmentType.MixedObstacle && difficulty >= .45f))
            {
                bool mixed = type == ArenaSegmentType.MixedObstacle;
                layout.gapStart = start + length * (mixed ? .38f : Mathf.Lerp(.38f, .33f, difficulty));
                float gapLength = Mathf.Lerp(settings.minimumGapLength, settings.maximumGapLength, difficulty) *
                                  Mathf.Lerp(.88f, 1.08f, variant);
                if (type != ArenaSegmentType.LongJump)
                {
                    layout.platformWidth = Mathf.Lerp(settings.easyPlatformWidth,
                        settings.hardPlatformWidth, difficulty) * Mathf.Lerp(.94f, 1.06f, variant);
                    // Two metres of clear air at each side are enough to expose the precision landing,
                    // while keeping both halves of the jump reachable by the slowest Arena vehicles.
                    gapLength = Mathf.Max(gapLength, layout.platformWidth + 4f);
                }
                layout.gapEnd = Mathf.Min(start + length * (mixed ? .60f : .69f),
                    layout.gapStart + gapLength);
                if (type != ArenaSegmentType.LongJump)
                {
                    float centerMinimum = layout.gapStart + layout.platformWidth * .5f + 1f;
                    float centerMaximum = layout.gapEnd - layout.platformWidth * .5f - 1f;
                    layout.platformCenterX = Mathf.Lerp(centerMinimum, centerMaximum, variant);
                    float rampRise = Mathf.Lerp(settings.baseRampRise, settings.maximumRampRise, difficulty);
                    float takeoffY = SampleArenaBase(profile, layout.gapStart) + rampRise -
                                     settings.platformTakeoffDrop;
                    float landingY = SampleArenaBase(profile, layout.gapEnd) +
                                     settings.platformLandingClearance;
                    layout.platformY = Mathf.Lerp(takeoffY, landingY,
                        Mathf.Lerp(.43f, .60f, variant));
                }
            }

            if (type == ArenaSegmentType.Lava || type == ArenaSegmentType.MixedObstacle)
            {
                float lavaLength = Mathf.Lerp(settings.minimumLavaLength, settings.maximumLavaLength,
                    difficulty) * Mathf.Lerp(.9f, 1.08f, 1f - variant);
                bool mixedWithGap = type == ArenaSegmentType.MixedObstacle && layout.HasGap;
                layout.lavaStart = mixedWithGap
                    ? Mathf.Max(start + length * .79f, layout.gapEnd + settings.landingSafetyLength)
                    : start + length * (type == ArenaSegmentType.MixedObstacle ? .72f : .40f);
                layout.lavaEnd = Mathf.Min(start + length * (mixedWithGap ? .97f : .93f),
                    layout.lavaStart + lavaLength);
            }
            if (type == ArenaSegmentType.TireObstacle || type == ArenaSegmentType.MixedObstacle)
            {
                layout.tireCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(settings.minimumTires,
                    settings.maximumTires, difficulty) + (variant > .72f ? 1f : 0f)),
                    settings.minimumTires, settings.maximumTires);
                layout.tireStart = start + length * (type == ArenaSegmentType.MixedObstacle ? .08f : .43f);
                layout.tireSpacing = settings.tireRadius * Mathf.Lerp(2.05f, 2.38f, variant);
            }
            return layout;
        }

        private static float SampleArenaBase(MapProfile profile, float worldX)
        {
            ArenaSettings settings = profile != null && profile.arena != null
                ? profile.arena : new ArenaSettings();
            float length = Mathf.Max(70f, settings.segmentLength);
            float distance = Mathf.Max(0f, worldX);
            int index = Mathf.FloorToInt(distance / length);
            float t = distance / length - index;
            uint seed = CombinedSeed(profile);
            float anchorA = index == 0 ? profile.baseHeight : profile.baseHeight +
                (Hash01(seed, index * 641 + 73) * 2f - 1f) * 2.1f;
            float anchorB = profile.baseHeight +
                (Hash01(seed, (index + 1) * 641 + 73) * 2f - 1f) * 2.1f;
            return Mathf.Lerp(anchorA, anchorB, Smooth01(t));
        }

        private static float SampleArenaHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            ArenaSettings settings = profile.arena ?? new ArenaSettings();
            float distance = Mathf.Max(0f, worldX);
            int index = GetArenaSegmentIndex(profile, distance);
            ArenaSegmentLayout layout = GetArenaSegment(profile, index);
            float length = Mathf.Max(70f, settings.segmentLength);
            float t = Mathf.Clamp01((distance - layout.start) / length);
            float envelope = Mathf.Sin(t * Mathf.PI);
            envelope *= envelope;
            float baseline = SampleArenaBase(profile, distance);
            float difficulty = layout.difficulty;
            float phase = layout.variant * TwoPi;
            float bumpCycles = Mathf.Lerp(length / settings.maximumBumpWavelength,
                length / settings.minimumBumpWavelength, difficulty);
            float bumps = (Mathf.Sin(t * TwoPi * bumpCycles + phase) * .68f +
                           Mathf.Sin(t * TwoPi * (bumpCycles * .53f + 1f) - phase * .7f) * .32f) *
                          envelope * settings.baseBumpHeight * Mathf.Lerp(.65f, 1.65f, difficulty);
            float shape = 0f;
            switch (layout.type)
            {
                case ArenaSegmentType.Bumpy:
                    shape = bumps;
                    break;
                case ArenaSegmentType.Ramp:
                    shape = Mathf.Pow(envelope, 1.35f) * Mathf.Lerp(settings.baseRampRise,
                        settings.maximumRampRise, difficulty) + bumps * .28f;
                    break;
                case ArenaSegmentType.TireObstacle:
                    shape = bumps * .48f - envelope * .35f;
                    break;
                case ArenaSegmentType.Lava:
                    shape = -envelope * Mathf.Lerp(.8f, 1.7f, difficulty) + bumps * .18f;
                    break;
                case ArenaSegmentType.TechnicalClimb:
                    shape = envelope * (1.5f + t * Mathf.Lerp(2.4f, 4.5f, difficulty)) + bumps * .42f;
                    break;
                case ArenaSegmentType.TechnicalDescent:
                    shape = -envelope * (1.4f + (1f - t) * Mathf.Lerp(2.2f, 4.2f, difficulty)) + bumps * .42f;
                    break;
                case ArenaSegmentType.Recovery:
                    shape = bumps * .08f;
                    break;
                default:
                    shape = bumps * .32f;
                    break;
            }

            if (layout.HasGap)
            {
                float rampStart = layout.gapStart - Mathf.Lerp(17f, 13f, difficulty);
                float rampRise = Mathf.Lerp(settings.baseRampRise, settings.maximumRampRise, difficulty);
                if (distance >= rampStart && distance < layout.gapStart)
                    shape += Smooth01(Mathf.InverseLerp(rampStart, layout.gapStart, distance)) * rampRise;
                else if (distance >= layout.gapStart && distance < layout.gapEnd)
                    shape += rampRise;
                else if (distance >= layout.gapEnd &&
                         distance < layout.gapEnd + settings.landingSafetyLength)
                    shape -= .25f * (1f - Smooth01(Mathf.InverseLerp(layout.gapEnd,
                        layout.gapEnd + settings.landingSafetyLength, distance)));
            }
            float introBlend = Smooth01(Mathf.InverseLerp(12f, 70f, distance));
            return Mathf.Lerp(profile.baseHeight, baseline + shape, introBlend);
        }

        public static bool IsArenaGap(MapProfile profile, float worldX)
        {
            if (profile == null || !profile.useArenaTerrain || profile.arena == null) return false;
            ArenaSegmentLayout layout = GetArenaSegment(profile, GetArenaSegmentIndex(profile, worldX));
            if (!layout.HasGap) return false;
            return worldX >= layout.gapStart && worldX < layout.gapEnd;
        }

        public static bool IsArenaReservedObstacleZone(MapProfile profile, float worldX)
        {
            if (profile == null || !profile.useArenaTerrain || profile.arena == null) return false;
            ArenaSettings settings = profile.arena;
            ArenaSegmentLayout layout = GetArenaSegment(profile, GetArenaSegmentIndex(profile, worldX));
            float clearance = Mathf.Max(0f, settings.pickupObstacleClearance);
            if (layout.HasGap && worldX >= layout.gapStart - 14f - clearance &&
                worldX <= layout.gapEnd + settings.landingSafetyLength + clearance) return true;
            if (layout.HasLava && worldX >= layout.lavaStart - clearance &&
                worldX <= layout.lavaEnd + clearance) return true;
            if (layout.HasTires)
            {
                float lastTire = layout.tireStart + Mathf.Max(0, layout.tireCount - 1) * layout.tireSpacing;
                if (worldX >= layout.tireStart - settings.tireRadius - clearance &&
                    worldX <= lastTire + settings.tireRadius + clearance) return true;
            }
            return false;
        }

        public static float SampleCityHighwayHeight(MapProfile profile, float worldX)
        {
            float general = SampleCityHighwayBaseHeight(profile, worldX);
            if (profile == null || !profile.useCityBridges || profile.cityBridges == null || worldX < 0f ||
                !TryGetCityBridgeZoneNearDistance(profile, worldX, out CityBridgeZoneLayout zone))
                return general;

            if (worldX < zone.gapStart)
            {
                float t = Smooth01(Mathf.InverseLerp(zone.approachStart, zone.gapStart, worldX));
                return Mathf.Lerp(general, zone.baselineHeight, t);
            }
            if (worldX <= zone.gapEnd) return zone.baselineHeight;
            float recoveryT = Smooth01(Mathf.InverseLerp(zone.gapEnd, zone.recoveryEnd, worldX));
            return Mathf.Lerp(zone.baselineHeight, general, recoveryT);
        }

        private static float SampleCityHighwayBaseHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            float featureLength = Mathf.Max(42f, profile.cityFeatureLength);
            float featurePosition = distance / featureLength;
            int featureIndex = Mathf.FloorToInt(featurePosition);
            float t = featurePosition - featureIndex;
            float difficulty = Mathf.Clamp01(profile.EvaluateDifficulty(distance * Core.GameManager.LogicalDistanceScale));
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

        public static int GetCityBridgeZoneCount(MapProfile profile, int cycleIndex)
        {
            uint seed = CombinedSeed(profile);
            int cycle = Mathf.Max(0, cycleIndex);
            float random = Hash01(seed, cycle * 92821 + 1709);
            return CityBridgeSettings.RequiredMinimumZones + Mathf.FloorToInt(random *
                (CityBridgeSettings.RequiredMaximumZones - CityBridgeSettings.RequiredMinimumZones + 1));
        }

        public static int GetCityProgressiveBridgeZoneCount(MapProfile profile, int cycleIndex)
        {
            uint seed = CombinedSeed(profile);
            int cycle = Mathf.Max(0, cycleIndex);
            int total = GetCityBridgeZoneCount(profile, cycle);
            float random = Hash01(seed, cycle * 74149 + 2903);
            int progressive = CityBridgeSettings.RequiredMinimumProgressiveZones + Mathf.FloorToInt(random *
                (CityBridgeSettings.RequiredMaximumProgressiveZones -
                 CityBridgeSettings.RequiredMinimumProgressiveZones + 1));
            // Keep at least one normal two-ended bridge zone in every cycle.
            return Mathf.Clamp(progressive, CityBridgeSettings.RequiredMinimumProgressiveZones,
                Mathf.Min(CityBridgeSettings.RequiredMaximumProgressiveZones, total - 1));
        }

        public static bool IsCityBridgeZoneProgressive(MapProfile profile, int cycleIndex, int zoneIndex)
        {
            int count = GetCityBridgeZoneCount(profile, cycleIndex);
            int slot = Mathf.Clamp(zoneIndex, 0, count - 1);
            int progressiveCount = GetCityProgressiveBridgeZoneCount(profile, cycleIndex);
            uint seed = CombinedSeed(profile);
            float score = Hash01(seed, cycleIndex * 35771 + slot * 1013 + 811);
            int rank = 0;
            for (int candidate = 0; candidate < count; candidate++)
            {
                if (candidate == slot) continue;
                float other = Hash01(seed, cycleIndex * 35771 + candidate * 1013 + 811);
                if (other < score || Mathf.Approximately(other, score) && candidate < slot) rank++;
            }
            return rank < progressiveCount;
        }

        public static CityBridgeZoneLayout GetCityBridgeZone(MapProfile profile, int cycleIndex,
            int zoneIndex)
        {
            CityBridgeSettings settings = profile != null && profile.cityBridges != null
                ? profile.cityBridges : new CityBridgeSettings();
            int cycle = Mathf.Max(0, cycleIndex);
            int count = GetCityBridgeZoneCount(profile, cycle);
            int slot = Mathf.Clamp(zoneIndex, 0, count - 1);
            int progressiveCount = GetCityProgressiveBridgeZoneCount(profile, cycle);
            float bridgeLength = Mathf.Max(4f, settings.bridgeLength);

            float totalReservedWidth = 0f;
            for (int i = 0; i < count; i++)
                totalReservedWidth += GetCityBridgeReservedWidth(profile, cycle, i);

            float cycleLength = CityBridgeSettings.RequiredCycleLength;
            float startMargin = Mathf.Max(100f, settings.cycleStartMargin);
            float endMargin = Mathf.Max(100f, settings.cycleEndMargin);
            float usableForSpacing = Mathf.Max(0f, cycleLength - startMargin - endMargin - totalReservedWidth);
            float spacing = count > 1
                ? Mathf.Min(Mathf.Max(50f, settings.minimumZoneSpacing), usableForSpacing / (count - 1)) : 0f;
            float freeSpace = Mathf.Max(0f, usableForSpacing - spacing * Mathf.Max(0, count - 1));

            uint seed = CombinedSeed(profile);
            float totalGapWeight = 0f;
            for (int gap = 0; gap <= count; gap++)
                totalGapWeight += CityBridgeGapWeight(seed, cycle, gap);

            float cursor = cycle * cycleLength + startMargin;
            for (int i = 0; i <= slot; i++)
            {
                cursor += freeSpace * CityBridgeGapWeight(seed, cycle, i) /
                          Mathf.Max(.001f, totalGapWeight);
                if (i == slot) break;
                cursor += GetCityBridgeReservedWidth(profile, cycle, i) + spacing;
            }

            bool progressive = IsCityBridgeZoneProgressive(profile, cycle, slot);
            float waterLength = progressive
                ? Mathf.Max(2f, settings.progressiveBaseWaterLength) +
                  cycle * CityBridgeSettings.RequiredProgressiveGrowth
                : Mathf.Max(2f, settings.normalWaterLength);
            float approachLength = Mathf.Max(5f, settings.approachBlendLength);
            float recoveryLength = Mathf.Max(5f, settings.recoveryBlendLength);
            float approachStart = cursor;
            float gapStart = approachStart + approachLength;
            float waterStart = gapStart + bridgeLength;
            float waterEnd = waterStart + waterLength;
            float gapEnd = waterEnd + (progressive ? 0f : bridgeLength);
            float baseline = SampleCityHighwayBaseHeight(profile, gapStart);
            return new CityBridgeZoneLayout
            {
                cycleIndex = cycle,
                zoneIndex = slot,
                zonesInCycle = count,
                progressiveZonesInCycle = progressiveCount,
                isProgressive = progressive,
                approachStart = approachStart,
                gapStart = gapStart,
                waterStart = waterStart,
                waterEnd = waterEnd,
                gapEnd = gapEnd,
                recoveryEnd = gapEnd + recoveryLength,
                baselineHeight = baseline,
                bridgeLength = bridgeLength,
                waterLength = waterLength
            };
        }

        public static bool TryGetCityBridgeZoneNearDistance(MapProfile profile, float worldX,
            out CityBridgeZoneLayout zone)
        {
            zone = default;
            if (profile == null || !profile.useCityBridges || profile.cityBridges == null || worldX < 0f)
                return false;
            int centerCycle = Mathf.FloorToInt(worldX / CityBridgeSettings.RequiredCycleLength);
            for (int cycle = Mathf.Max(0, centerCycle - 1); cycle <= centerCycle + 1; cycle++)
            {
                int count = GetCityBridgeZoneCount(profile, cycle);
                for (int slot = 0; slot < count; slot++)
                {
                    CityBridgeZoneLayout candidate = GetCityBridgeZone(profile, cycle, slot);
                    if (worldX < candidate.approachStart || worldX > candidate.recoveryEnd) continue;
                    zone = candidate;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetCityBridgeGapAtDistance(MapProfile profile, float worldX,
            out CityBridgeZoneLayout zone)
        {
            if (TryGetCityBridgeZoneNearDistance(profile, worldX, out zone) &&
                worldX >= zone.gapStart && worldX < zone.gapEnd) return true;
            zone = default;
            return false;
        }

        public static bool IsCityBridgeWater(MapProfile profile, float worldX)
        {
            return TryGetCityBridgeZoneNearDistance(profile, worldX, out CityBridgeZoneLayout zone) &&
                   worldX >= zone.waterStart && worldX < zone.waterEnd;
        }

        private static float GetCityBridgeReservedWidth(MapProfile profile, int cycleIndex, int zoneIndex)
        {
            CityBridgeSettings settings = profile != null && profile.cityBridges != null
                ? profile.cityBridges : new CityBridgeSettings();
            bool progressive = IsCityBridgeZoneProgressive(profile, cycleIndex, zoneIndex);
            float waterLength = progressive
                ? Mathf.Max(2f, settings.progressiveBaseWaterLength) +
                  Mathf.Max(0, cycleIndex) * CityBridgeSettings.RequiredProgressiveGrowth
                : Mathf.Max(2f, settings.normalWaterLength);
            float bridges = Mathf.Max(4f, settings.bridgeLength) * (progressive ? 1f : 2f);
            return Mathf.Max(5f, settings.approachBlendLength) + bridges + waterLength +
                   Mathf.Max(5f, settings.recoveryBlendLength);
        }

        private static float CityBridgeGapWeight(uint seed, int cycleIndex, int gapIndex) =>
            .75f + Hash01(seed, cycleIndex * 61441 + gapIndex * 1877 + 1237) * .5f;

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
            // Liquid grows by exactly +7m per cycle completed (Cycle 0: Base, Cycle 1: Base+7m, Cycle 2: Base+14m, etc.)
            float baseLength = settings != null ? settings.basePoolLength : 21f;
            float poolLength = baseLength + (safeCycle * 7f);
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

        public static bool IsLiquidGap(MapProfile profile, float worldX) => false;

        public static SpaceChallengeLayout GetSpaceChallenge(MapProfile profile, int phase)
        {
            SpaceChallengeSettings settings = profile != null ? profile.spaceChallenges : null;
            int safePhase = Mathf.Max(1, phase);
            float interval = settings != null ? Mathf.Max(250f, settings.eventInterval) : 2000f;
            float eventDistance = safePhase * interval;
            SpaceEventType eventType = SpaceEventType.Void;
            SpacePlanetType planetType = default;
            float logicalDistance = eventDistance * Core.GameManager.LogicalDistanceScale;
            int spaceCycle = safePhase - 1;
            float voidLength = settings != null
                ? settings.initialGapLength + spaceCycle * settings.gapIncreasePerCycle
                : 15f + spaceCycle * 5f;
            float planetRadius = eventType == SpaceEventType.Planet
                ? GetSpacePlanetRadius(settings, planetType) : 0f;
            float launchClearance = settings != null ? Mathf.Max(2f, settings.planetLaunchClearance) : 4.5f;
            float exitClearance = settings != null ? Mathf.Max(2f, settings.planetExitClearance) : 4f;
            float planetArcHeight = settings != null
                ? Mathf.Clamp(settings.planetCenterDrop, .6f, .88f) : .82f;
            float planetArcHalfWidth = planetRadius *
                                       Mathf.Sqrt(Mathf.Max(.01f, 1f - planetArcHeight * planetArcHeight));
            float gapLength = eventType == SpaceEventType.Void
                ? voidLength : launchClearance + planetArcHalfWidth * 2f + exitClearance;
            float baseRampLength = settings != null ? Mathf.Max(4f, settings.rampLength) : 15f;
            float rampLength = baseRampLength + (eventType == SpaceEventType.Void
                ? Mathf.Clamp((gapLength - 15f) * .035f, 0f, 7f)
                : Mathf.Clamp(planetRadius * .22f, .8f, 2.4f));
            float baseRampRise = settings != null ? settings.rampRise : 5.2f;
            float rampRise = eventType == SpaceEventType.Void
                ? baseRampRise + Mathf.Clamp((gapLength - 15f) * .016f, 0f, 2.8f)
                : (settings != null ? settings.planetRampRise : 3.8f) +
                  Mathf.Clamp01(logicalDistance / 10000f) * 1.05f + planetRadius * .055f;
            float rampStart = eventDistance;
            float gapStart = rampStart + rampLength;
            float gapEnd = gapStart + gapLength;
            float landingLength = settings != null ? settings.landingLength : 42f;
            float recoveryLength = settings != null ? settings.recoveryLength : 86f;
            float baseline = SampleProceduralProfileHeight(profile, rampStart);
            float landingDrop = settings != null ? settings.landingDrop : .35f;
            float landingHeight = baseline - (eventType == SpaceEventType.Planet
                ? landingDrop * .65f : landingDrop);
            float planetCenterX = eventType == SpaceEventType.Planet
                ? gapStart + launchClearance + planetArcHalfWidth : 0f;
            float centreDrop = planetArcHeight;
            float planetCenterY = eventType == SpaceEventType.Planet
                ? baseline + rampRise - planetRadius * centreDrop : 0f;
            return new SpaceChallengeLayout
            {
                phase = safePhase,
                eventType = eventType,
                planetType = planetType,
                eventDistance = eventDistance,
                approachStart = rampStart - (settings != null ? settings.approachLength : 200f),
                rampStart = rampStart,
                rampLength = rampLength,
                rampRise = rampRise,
                gapStart = gapStart,
                gapEnd = gapEnd,
                landingEnd = gapEnd + landingLength,
                recoveryEnd = gapEnd + landingLength + recoveryLength,
                warningX = rampStart - (settings != null ? settings.warningDistance : 100f),
                baselineHeight = baseline,
                gapLength = gapLength,
                blackHoleDiameter = eventType == SpaceEventType.Void ? gapLength : 0f,
                planetRadius = planetRadius,
                planetCenterX = planetCenterX,
                planetCenterY = planetCenterY,
                landingHeight = landingHeight
            };
        }

        private static float GetSpacePlanetRadius(SpaceChallengeSettings settings, SpacePlanetType planet)
        {
            float normalized = planet switch
            {
                SpacePlanetType.Mercury => 0f,
                SpacePlanetType.Venus => .34f,
                SpacePlanetType.Earth => .40f,
                SpacePlanetType.Mars => .17f,
                SpacePlanetType.Jupiter => 1f,
                SpacePlanetType.Saturn => .84f,
                SpacePlanetType.Uranus => .61f,
                SpacePlanetType.Neptune => .59f,
                _ => .4f
            };
            float minimum = settings != null ? Mathf.Max(2f, settings.minimumPlanetRadius) : 4.8f;
            float maximum = settings != null ? Mathf.Max(minimum, settings.maximumPlanetRadius) : 10.5f;
            return Mathf.Lerp(minimum, maximum, normalized);
        }

        public static bool TryGetSpaceChallengeNearDistance(MapProfile profile, float distance,
            out SpaceChallengeLayout challenge)
        {
            challenge = default;
            if (profile == null || !profile.useSpaceTerrain || profile.spaceChallenges == null) return false;
            float interval = Mathf.Max(250f, profile.spaceChallenges.eventInterval);
            int center = Mathf.Max(1, Mathf.FloorToInt(distance / interval));
            for (int phase = Mathf.Max(1, center - 1); phase <= center + 2; phase++)
            {
                SpaceChallengeLayout candidate = GetSpaceChallenge(profile, phase);
                float blendLength = profile.spaceChallenges != null
                    ? profile.spaceChallenges.approachBlendLength : 45f;
                if (distance < candidate.approachStart - blendLength ||
                    distance > candidate.recoveryEnd + 40f) continue;
                challenge = candidate;
                return true;
            }
            return false;
        }

        public static bool IsTerrainGap(MapProfile profile, float worldX)
        {
            if (profile != null && profile.useLastHorizonTerrain &&
                TryGetLastHorizonSpecialPhase(profile, worldX,
                    out LastHorizonSpecialPhaseLayout lastHorizonPhase) &&
                lastHorizonPhase.ContainsGap(worldX)) return true;
            if (profile != null && profile.useArenaTerrain) return IsArenaGap(profile, worldX);
            if (profile != null && profile.useCityBridges &&
                TryGetCityBridgeGapAtDistance(profile, worldX, out _)) return true;
            if (profile == null || !profile.useSpaceTerrain || profile.spaceChallenges == null) return false;
            float interval = Mathf.Max(250f, profile.spaceChallenges.eventInterval);
            int phase = Mathf.Max(1, Mathf.FloorToInt(worldX / interval));
            for (int candidatePhase = Mathf.Max(1, phase - 1); candidatePhase <= phase + 1; candidatePhase++)
            {
                SpaceChallengeLayout layout = GetSpaceChallenge(profile, candidatePhase);
                float landingStart = GetSpaceLandingStart(layout);
                // The ramp is part of the continuous terrain. Only the real void loses geometry, and a
                // short landing overlap prevents sample rounding from opening a second invisible gap.
                if (worldX >= layout.gapStart && worldX < landingStart) return true;
            }
            return false;
        }

        private static float SampleSpaceHeight(MapProfile profile, float worldX)
        {
            float general = profile != null && profile.elevationProfile != null
                ? profile.elevationProfile.GetHeightAtDistance(worldX)
                : SampleProceduralProfileHeight(profile, worldX);
            if (!TryGetSpaceChallengeNearDistance(profile, worldX, out SpaceChallengeLayout layout))
                return general;
            SpaceChallengeSettings settings = profile.spaceChallenges;
            float baseline = layout.baselineHeight;
            float blendStart = layout.approachStart - Mathf.Max(10f, settings.approachBlendLength);
            if (worldX < blendStart) return general;
            if (worldX < layout.approachStart)
            {
                float t = Smooth01(Mathf.InverseLerp(blendStart, layout.approachStart, worldX));
                return Mathf.Lerp(general, baseline, t);
            }
            // The final 200m are exactly flat so the sign is readable and the jump setup is reliable.
            if (worldX < layout.rampStart) return baseline;
            if (worldX < layout.gapStart)
            {
                float rampLength = Mathf.Max(.001f, layout.gapStart - layout.rampStart);
                float flatEnd = rampLength * Mathf.Clamp(settings.rampFlatFraction, .05f, .5f);
                float rampDistance = worldX - layout.rampStart;
                if (rampDistance <= flatEnd) return baseline;
                float rampT = Mathf.InverseLerp(flatEnd, rampLength, rampDistance);
                // Zero slope at the join, but a positive final tangent so the vehicle actually launches.
                float launchCurve = rampT * rampT * (2f - rampT);
                return baseline + layout.rampRise * launchCurve;
            }
            float landingStart = GetSpaceLandingStart(layout);
            if (worldX < landingStart) return baseline + layout.rampRise;
            float landingHeight = layout.landingHeight;
            if (worldX < layout.landingEnd) return landingHeight;
            if (worldX < layout.recoveryEnd)
            {
                float t = Smooth01(Mathf.InverseLerp(layout.landingEnd, layout.recoveryEnd, worldX));
                return Mathf.Lerp(landingHeight, general, t);
            }
            return general;
        }

        private static float GetSpaceLandingStart(SpaceChallengeLayout layout)
        {
            float overlap = Mathf.Min(.75f, Mathf.Max(.2f, layout.gapLength * .08f));
            return layout.gapEnd - overlap;
        }

        public static bool IsLiquidPool(MapProfile profile, float worldX) =>
            TryGetLiquidChallengeAtDistance(profile, worldX, out _) || IsCityBridgeWater(profile, worldX);

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
                float depth = settings != null ? Mathf.Max(2f, settings.visualDepth) : 7f;
                float bottomY = baseline - depth;
                float exitRamp = Mathf.Clamp(settings != null ? settings.exitRampHorizontalLength : 12f, 4f, challenge.poolLength * 0.5f);
                if (distance < challenge.poolEnd)
                {
                    float exitStart = challenge.poolEnd - exitRamp;
                    if (distance < exitStart)
                        return bottomY;
                    float exitT = Smooth01(Mathf.InverseLerp(exitStart, challenge.poolEnd, distance));
                    return Mathf.Lerp(bottomY, baseline, exitT);
                }
                if (distance < challenge.landingEnd)
                {
                    return baseline;
                }
                if (distance < challenge.recoveryEnd)
                {
                    return baseline;
                }
                float rejoinT = Smooth01(Mathf.InverseLerp(challenge.recoveryEnd, rejoinEnd, distance));
                float rejoinTarget = SampleNeonBaseHeight(profile, rejoinEnd);
                return Mathf.Lerp(baseline, rejoinTarget, rejoinT);
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
            if (profile != null && profile.useArenaTerrain)
            {
                ArenaSegmentType arenaType = GetArenaSegmentType(profile, Mathf.Max(0, sectionIndex));
                if (arenaType == ArenaSegmentType.Recovery) return TerrainSectionKind.Recovery;
                if (IsArenaTechnical(arenaType)) return TerrainSectionKind.Technical;
                if (arenaType == ArenaSegmentType.Ramp || arenaType == ArenaSegmentType.Bumpy)
                    return TerrainSectionKind.Rolling;
                return TerrainSectionKind.Fast;
            }
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

        private static float EvaluatePointedTerrainFeature(MapProfile profile, TerrainFeatureType feature,
            int featureIndex, float t, float variant, float difficulty)
        {
            float clampedT = Mathf.Clamp01(t);
            uint seed = CombinedSeed(profile);
            float asymmetry = Mathf.Clamp(profile.pointedFeatureAsymmetry, 0f, .3f);
            float centre = .5f + (Hash01(seed, featureIndex * 947 + 211) * 2f - 1f) * asymmetry;
            if (feature == TerrainFeatureType.LongClimb)
                centre = Mathf.Clamp(centre + .10f, .34f, .78f);
            else if (feature == TerrainFeatureType.LongDescent)
                centre = Mathf.Clamp(centre - .10f, .22f, .66f);

            float sharpnessRoll = Hash01(seed, featureIndex * 971 + 223);
            float pointPower = Mathf.Lerp(
                Mathf.Lerp(1.05f, 1.42f, difficulty),
                Mathf.Lerp(1.65f, 2.85f, difficulty), sharpnessRoll);
            float pointedEnvelope = PointedEnvelope(clampedT, centre, pointPower);
            // The range widens with distance: some hills remain deliberately low and gentle while
            // others become substantially taller and steeper as the 10 km peak approaches.
            float heightVariation = Mathf.Lerp(
                Mathf.Lerp(.72f, .48f, difficulty),
                Mathf.Lerp(1.22f, 1.42f, difficulty),
                Hash01(seed, featureIndex * 991 + 227));
            bool positiveHill = true;
            float shape;
            switch (feature)
            {
                case TerrainFeatureType.FlatRecovery:
                    shape = pointedEnvelope * Mathf.Lerp(.055f, .105f, variant);
                    break;
                case TerrainFeatureType.SoftHill:
                    shape = pointedEnvelope * Mathf.Lerp(.28f, .45f, variant);
                    break;
                case TerrainFeatureType.MediumHill:
                    shape = pointedEnvelope * Mathf.Lerp(.54f, .80f, variant);
                    break;
                case TerrainFeatureType.LongClimb:
                    shape = pointedEnvelope * Mathf.Lerp(.48f, .82f, variant);
                    break;
                case TerrainFeatureType.LongDescent:
                    positiveHill = false;
                    shape = -pointedEnvelope * Mathf.Lerp(.48f, .82f, 1f - variant);
                    break;
                case TerrainFeatureType.Valley:
                    positiveHill = false;
                    shape = -pointedEnvelope * Mathf.Lerp(.54f, .82f, variant);
                    break;
                case TerrainFeatureType.Crest:
                    shape = Mathf.Pow(pointedEnvelope, 1.28f) * Mathf.Lerp(.72f, .96f, variant);
                    break;
                case TerrainFeatureType.RollingSection:
                    float firstCentre = Mathf.Clamp(centre - .19f, .25f, .46f);
                    float secondCentre = Mathf.Clamp(centre + .19f, .54f, .75f);
                    shape = PointedPulse(clampedT, firstCentre, .24f, pointPower) *
                            Mathf.Lerp(.42f, .62f, variant) -
                            PointedPulse(clampedT, secondCentre, .24f, pointPower) *
                            Mathf.Lerp(.34f, .56f, 1f - variant);
                    break;
                case TerrainFeatureType.JumpableCrest:
                    shape = Mathf.Pow(pointedEnvelope, 1.42f) * Mathf.Lerp(.66f, .90f, variant);
                    break;
                default:
                    shape = 0f;
                    break;
            }
            shape *= heightVariation;

            float extremeChance = Mathf.Clamp01(profile.pointedExtremeShapeChance) *
                                  Mathf.Lerp(.18f, 1f, difficulty);
            if (feature == TerrainFeatureType.FlatRecovery) extremeChance *= .18f;
            if (Hash01(seed, featureIndex * 997 + 229) < extremeChance)
            {
                float extremeHalfWidth = Mathf.Lerp(.12f, .24f,
                    Hash01(seed, featureIndex * 1019 + 235)) * Mathf.Lerp(1.12f, .86f, difficulty);
                float extremeMargin = extremeHalfWidth + .01f;
                float extremeCentre = Mathf.Lerp(extremeMargin, 1f - extremeMargin,
                    Hash01(seed, featureIndex * 1009 + 231));
                float extremePower = Mathf.Lerp(1.02f, 1.42f,
                    Hash01(seed, featureIndex * 1021 + 237));
                float extremeHeight = Mathf.Clamp(profile.pointedExtremeShapeStrength, .05f, .6f) *
                                      Mathf.Lerp(.72f, 1.28f,
                                          Hash01(seed, featureIndex * 1033 + 241));
                float extremeDirection = Hash01(seed, featureIndex * 1039 + 243) < .5f ? -1f : 1f;
                shape += PointedPulse(clampedT, extremeCentre, extremeHalfWidth, extremePower) *
                         extremeHeight * extremeDirection;
            }

            // Every section receives a different deterministic set of small points. Before a positive
            // crest they are rises; after it they become dips, so the descent is not one repeated smooth
            // ramp. Valleys invert that order to match their descending and ascending sides.
            int peakMiniCount = Mathf.Clamp(profile.pointedPeakMiniFeatureCount, 4, 12);
            int initialMiniCount = Mathf.Max(3, peakMiniCount - 5);
            int countVariation = Mathf.FloorToInt(Hash01(seed, featureIndex * 1049 + 247) * 3f) - 1;
            int miniCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(initialMiniCount, peakMiniCount, difficulty)) + countVariation,
                3, 12);
            float miniStrength = Mathf.Clamp(profile.pointedMiniFeatureStrength, .01f, .15f) *
                                 Mathf.Lerp(.46f, 1.15f, difficulty);
            float miniDetail = 0f;
            for (int mini = 0; mini < miniCount; mini++)
            {
                float evenPosition = (mini + 1f) / (miniCount + 1f);
                float jitter = (Hash01(seed, featureIndex * 1061 + mini * 67 + 251) * 2f - 1f) * .055f;
                float halfWidth = Mathf.Lerp(
                    Mathf.Lerp(.055f, .040f, difficulty),
                    Mathf.Lerp(.105f, .080f, difficulty),
                    Hash01(seed, featureIndex * 1087 + mini * 71 + 257));
                float miniMargin = halfWidth + .01f;
                float miniCentre = Mathf.Clamp(evenPosition + jitter, miniMargin, 1f - miniMargin);
                float miniPower = Mathf.Lerp(1.02f, Mathf.Lerp(1.5f, 1.85f, difficulty),
                    Hash01(seed, featureIndex * 1091 + mini * 73 + 263));
                float miniHeight = Mathf.Lerp(.68f, Mathf.Lerp(1.24f, 1.48f, difficulty),
                    Hash01(seed, featureIndex * 1103 + mini * 79 + 269));
                bool beforeCrest = miniCentre < centre;
                float direction = beforeCrest == positiveHill ? 1f : -1f;
                miniDetail += PointedPulse(clampedT, miniCentre, halfWidth, miniPower) *
                              miniHeight * direction;
            }

            return shape + miniDetail * miniStrength * profile.technicalDetailStrength;
        }

        private static float PointedEnvelope(float t, float centre, float power)
        {
            float safeCentre = Mathf.Clamp(centre, .08f, .92f);
            if (t <= 0f || t >= 1f) return 0f;
            return t < safeCentre
                ? Mathf.Pow(Mathf.Clamp01(t / safeCentre), power)
                : Mathf.Pow(Mathf.Clamp01((1f - t) / (1f - safeCentre)), power);
        }

        private static float PointedPulse(float t, float centre, float halfWidth, float power)
        {
            float radius = Mathf.Abs(t - centre) / Mathf.Max(.001f, halfWidth);
            if (radius >= 1f) return 0f;
            return Mathf.Pow(1f - radius, Mathf.Max(1f, power));
        }

        public static float EvaluateFloat32Precision(float value, float worldX)
        {
            float absX = Mathf.Abs(worldX);
            if (absX < 32768f) return value;
            float exponent = Mathf.Floor(Mathf.Log(absX, 2f));
            float step = Mathf.Pow(2f, exponent - 23f);
            if (step <= 0.000001f) return value;
            return Mathf.Round(value / step) * step;
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
                for (int i = 0; i < sampleCount; i++)
                {
                    float rawY = working[i + padding];
                    surfaceHeights[i] = EvaluateFloat32Precision(rawY, chunkStartX + i * dx);
                }
                previousHeight = surfaceHeights[sampleCount - 1];
                previousSlope = (surfaceHeights[sampleCount - 1] - surfaceHeights[sampleCount - 2]) / dx;
            }
            else
            {
                // Mountains uses this same hard envelope. The authored grammar stays varied, while the
                // final collider can never exceed maxSlopeAngle or form an abrupt vertical wall.
                previousHeight = hasBoundary ? boundaryHeight : working[padding];
                previousSlope = hasBoundary ? boundarySlope :
                    (working[padding + 1] - working[padding]) / dx;
                surfaceHeights[0] = EvaluateFloat32Precision(previousHeight, chunkStartX);
                int firstGeneratedSample = 1;
                if (hasBoundary && sampleCount > 1)
                {
                    float boundaryDelta = Mathf.Clamp(boundarySlope * dx, -maxDelta, maxDelta);
                    float nextH = previousHeight + boundaryDelta;
                    surfaceHeights[1] = EvaluateFloat32Precision(nextH, chunkStartX + dx);
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
                    surfaceHeights[i] = EvaluateFloat32Precision(height, chunkStartX + i * dx);
                    previousHeight = surfaceHeights[i];
                    previousSlope = safeSlope;
                }
            }

            if (profile.useSpaceTerrain && profile.spaceChallenges != null)
            {
                // Reapply authored event geometry after generic smoothing. This keeps the flat approach,
                // integrated launch ramp and landing pad aligned with their visuals and physics.
                for (int i = 0; i < sampleCount; i++)
                {
                    float worldX = chunkStartX + i * dx;
                    if (!TryGetSpaceChallengeNearDistance(profile, worldX, out SpaceChallengeLayout layout))
                        continue;
                    float blendStart = layout.approachStart -
                                       Mathf.Max(10f, profile.spaceChallenges.approachBlendLength);
                    if (worldX < blendStart || worldX > layout.recoveryEnd) continue;
                    surfaceHeights[i] = EvaluateFloat32Precision(SampleSpaceHeight(profile, worldX), worldX);
                }
                previousHeight = surfaceHeights[sampleCount - 1];
                previousSlope = sampleCount > 1
                    ? (surfaceHeights[sampleCount - 1] - surfaceHeights[sampleCount - 2]) / dx : 0f;
            }

            if (profile.useCityBridges && profile.cityBridges != null)
            {
                // Generic smoothing must not bend the authored flat bridge banks. Reapply the City event
                // profile so the visible road, the terrain gap and each rotating bridge share one baseline.
                for (int i = 0; i < sampleCount; i++)
                {
                    float worldX = chunkStartX + i * dx;
                    if (!TryGetCityBridgeZoneNearDistance(profile, worldX, out _)) continue;
                    surfaceHeights[i] = EvaluateFloat32Precision(SampleCityHighwayHeight(profile, worldX), worldX);
                }
                previousHeight = surfaceHeights[sampleCount - 1];
                previousSlope = sampleCount > 1
                    ? (surfaceHeights[sampleCount - 1] - surfaceHeights[sampleCount - 2]) / dx : 0f;
            }

            endHeight = previousHeight;
            endSlope = previousSlope;
            // One terrain chunk can climb by tan(maxSlope) * chunkLength. Add that full possible rise to
            // the ordinary depth so the wall's lower edge remains outside the camera on a continuous climb.
            float maximumChunkRise = Mathf.Tan(Mathf.Clamp(profile.maxSlopeAngle, 0f, 80f) * Mathf.Deg2Rad) *
                                     Mathf.Max(1f, chunkLength);
            float terrainWallDepth = Mathf.Max(5f, profile.terrainVisualDepth + maximumChunkRise);
            float terrainFillDepth = CalculateTerrainFillDepth(profile);
            bool hasAuthoredContactCurve = HasAuthoredSurfaceContactCurve(profile);
            bool sampleContactRowOnTopSurface = profile.useCityHighwayTerrain ||
                                                profile.terrainMaximumVisualExtent > 0f ||
                                                hasAuthoredContactCurve;
            // Six independent vertices keep the three responsibilities separate: deep Terrain wall,
            // visible Ground ribbon and the horizontal driveable surface. Their top Y still comes from
            // the same surfaceHeights array used by the authoritative physics collider.
            Vector3[] vertices = new Vector3[sampleCount * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
            var surfaceTriangles = new List<int>((sampleCount - 1) * 12);
            var skirtTriangles = new List<int>((sampleCount - 1) * 6);
            for (int i = 0; i < sampleCount; i++)
            {
                float rawLocalX = i * dx;
                float worldX = chunkStartX + rawLocalX;
                float localX = rawLocalX;
                float textureU = profile.fitAuthoredTerrainTexture || profile.useMountainsTerrain
                    ? worldX / Mathf.Max(1f, profile.terrainTextureRepeatWidth)
                    : localX * .1f;

                if (Mathf.Abs(worldX) >= 32768f)
                {
                    float exponent = Mathf.Floor(Mathf.Log(Mathf.Abs(worldX), 2f));
                    float step = Mathf.Pow(2f, exponent - 23f);
                    localX = Mathf.Round((chunkStartX + rawLocalX) / step) * step - chunkStartX;

                    float uvStep = step / Mathf.Max(1f, profile.terrainTextureRepeatWidth);
                    if (uvStep > 0.0001f)
                        textureU = Mathf.Round(textureU / uvStep) * uvStep;
                }

                float groundContactV = SampleGroundSurfaceContactV(profile, textureU);
                CalculateGroundVisualExtents(profile, groundContactV,
                    out float groundVisualDepth, out float groundVisualRise);
                float surfaceY = surfaceHeights[i];
                float groundBottomY = EvaluateFloat32Precision(surfaceY - groundVisualDepth, worldX);
                float groundTopY = EvaluateFloat32Precision(surfaceY + groundVisualRise, worldX);
                float terrainTopY = EvaluateFloat32Precision(surfaceY - terrainFillDepth, worldX);
                // The camera follows the vehicle vertically, so a global world-space bottom becomes visible
                // as a horizontal rectangular cut on tall hills. Keep the deep wall below each surface sample.
                float skirtY = EvaluateFloat32Precision(surfaceY - terrainWallDepth, worldX);
                // Ground is a visual overlay. Keep it slightly in front of the deep Terrain wall so
                // both textured submeshes can never become coplanar and hide the authored road.
                float groundVisualZ = -width * .5f - .03f;

                int vertex = i * 6;
                vertices[vertex] = new Vector3(localX, terrainTopY, -width * .5f);        // Terrain fill top
                vertices[vertex + 1] = new Vector3(localX, skirtY, -width * .5f);          // Terrain bottom
                vertices[vertex + 2] = new Vector3(localX, groundTopY, groundVisualZ);     // Ground art top
                vertices[vertex + 3] = new Vector3(localX, groundBottomY, groundVisualZ);  // Ground bottom
                vertices[vertex + 4] = new Vector3(localX, surfaceY, -width * .5f);       // Physical surface front
                vertices[vertex + 5] = new Vector3(localX, surfaceY, width * .5f);         // Surface back
                if (profile.fitAuthoredTerrainTexture || profile.useSpaceTerrain)
                {
                    // Ground's authored visible band is mapped to its side-facing ribbon. The contact V can
                    // sit inside that band so foliage may extend above the road while the continuous turf line
                    // remains exactly on the generated/collider surface. Terrain starts behind the continuous
                    // Ground band so transparent decorative roots cannot expose a gap.
                    uvs[vertex] = new Vector2(textureU, profile.terrainWallUvRange.y);
                    uvs[vertex + 1] = new Vector2(textureU, profile.terrainWallUvRange.x);
                    uvs[vertex + 2] = new Vector2(textureU, profile.terrainSurfaceUvRange.y);
                    uvs[vertex + 3] = new Vector2(textureU, profile.terrainSurfaceUvRange.x);
                    // Compact 2.5D slabs are viewed edge-on by the camera. Sampling their authored
                    // contact row prevents the cap from projecting the vertical wall onto the road.
                    uvs[vertex + 4] = new Vector2(textureU, sampleContactRowOnTopSurface
                        ? groundContactV : profile.terrainSurfaceUvRange.y);
                    uvs[vertex + 5] = new Vector2(textureU, sampleContactRowOnTopSurface
                        ? groundContactV : profile.terrainSurfaceUvRange.x);
                }
                else
                {
                    uvs[vertex] = new Vector2(textureU, 0f);
                    uvs[vertex + 1] = new Vector2(textureU,
                        Mathf.Abs(surfaceHeights[i] - skirtY) * .1f);
                    uvs[vertex + 2] = new Vector2(textureU, 0f);
                    uvs[vertex + 3] = new Vector2(textureU, 0f);
                    uvs[vertex + 4] = new Vector2(textureU, 0f);
                    uvs[vertex + 5] = new Vector2(textureU, 1f);
                }
            }

            for (int i = 0; i < sampleCount - 1; i++)
            {
                float segmentWorldX = chunkStartX + (i + .5f) * dx;
                if (IsTerrainGap(profile, segmentWorldX)) continue;
                int vertex = i * 6;
                int next = vertex + 6;
                if (profile.fitAuthoredTerrainTexture || profile.useSpaceTerrain)
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
            CalculateGroundVisualExtents(profile, ResolveConfiguredSurfaceContactV(profile),
                out depthBelowSurface, out riseAboveSurface);
        }

        private static void CalculateGroundVisualExtents(MapProfile profile, float contactV,
            out float depthBelowSurface, out float riseAboveSurface)
        {
            depthBelowSurface = 0f;
            riseAboveSurface = 0f;
            if (profile == null) return;
            if (profile.useSpaceTerrain && !profile.fitAuthoredTerrainTexture)
            {
                depthBelowSurface = .52f;
                riseAboveSurface = .07f;
                return;
            }
            if (!profile.fitAuthoredTerrainTexture) return;
            Texture texture = profile.terrainEdgeMaterial != null
                ? profile.terrainEdgeMaterial.mainTexture : null;
            if (texture == null || texture.width <= 0 || texture.height <= 0) return;
            float bottomV = profile.terrainSurfaceUvRange.x;
            float topV = profile.terrainSurfaceUvRange.y;
            contactV = Mathf.Clamp(contactV, Mathf.Min(bottomV, topV), Mathf.Max(bottomV, topV));
            float metresPerV = profile.terrainTextureRepeatWidth * texture.height / texture.width;
            // Both extents come from the source pixels and PPU-derived repeat width; there is no visual offset.
            float rawDepth = Mathf.Abs(contactV - bottomV) * metresPerV;
            float rawRise = Mathf.Abs(topV - contactV) * metresPerV;
            float configuredMaximumExtent = profile.terrainMaximumVisualExtent;
            if (configuredMaximumExtent <= 0f && profile.useCityHighwayTerrain)
                configuredMaximumExtent = CompactGroundMaximumVisualExtent;
            if (configuredMaximumExtent > 0f)
            {
                // Compact 2.5D slabs share City's presentation without inheriting City's height grammar.
                // The initial uniform scale preserves the artwork proportions; an optional upper-rise cap
                // can then slim only the part behind the car without moving the painted contact row.
                float uniformScale = Mathf.Min(1f, configuredMaximumExtent /
                    Mathf.Max(.001f, Mathf.Max(rawDepth, rawRise)));
                depthBelowSurface = Mathf.Max(.05f, rawDepth * uniformScale);
                riseAboveSurface = rawRise * uniformScale;
                if (profile.terrainMaximumVisualRiseAboveSurface > 0f)
                    riseAboveSurface = Mathf.Min(riseAboveSurface,
                        profile.terrainMaximumVisualRiseAboveSurface);
                return;
            }
            if (HasAuthoredSurfaceContactCurve(profile))
            {
                // A sampled painted contact path must use one scale for both halves of the ribbon.
                // Clamping depth and rise independently moves the sampled contact away from y=surfaceY,
                // which is exactly what made wheels appear to float over authored wavy terrain.
                // Three metres is also the canonical visible module extent shared by the themed tracks.
                float uniformScale = Mathf.Min(1f, AuthoredTrackMaximumVisualExtent /
                    Mathf.Max(.001f, Mathf.Max(rawDepth, rawRise)));
                depthBelowSurface = Mathf.Max(.05f, rawDepth * uniformScale);
                riseAboveSurface = rawRise * uniformScale;
                return;
            }
            depthBelowSurface = Mathf.Clamp(rawDepth, .05f, 3f);
            riseAboveSurface = Mathf.Clamp(rawRise, 0f, 3f);
        }

        public static bool HasAuthoredSurfaceContactCurve(MapProfile profile) =>
            profile != null && profile.terrainSurfaceContactVSamples != null &&
            profile.terrainSurfaceContactVSamples.Length >= 2;

        public static float SampleGroundSurfaceContactV(MapProfile profile, float textureU)
        {
            float fallback = ResolveConfiguredSurfaceContactV(profile);
            if (!HasAuthoredSurfaceContactCurve(profile)) return fallback;

            float sourceU = ResolveGroundTextureU(profile, textureU);
            float[] samples = profile.terrainSurfaceContactVSamples;
            float scaled = Mathf.Clamp01(sourceU) * (samples.Length - 1);
            int left = Mathf.Min(Mathf.FloorToInt(scaled), samples.Length - 2);
            float contactV = Mathf.Lerp(samples[left], samples[left + 1], scaled - left);
            return Mathf.Clamp(contactV, Mathf.Min(profile.terrainSurfaceUvRange.x,
                    profile.terrainSurfaceUvRange.y),
                Mathf.Max(profile.terrainSurfaceUvRange.x, profile.terrainSurfaceUvRange.y));
        }

        private static float ResolveConfiguredSurfaceContactV(MapProfile profile)
        {
            if (profile == null) return 0f;
            float minimumV = Mathf.Min(profile.terrainSurfaceUvRange.x, profile.terrainSurfaceUvRange.y);
            float maximumV = Mathf.Max(profile.terrainSurfaceUvRange.x, profile.terrainSurfaceUvRange.y);
            return profile.terrainSurfaceContactV >= minimumV && profile.terrainSurfaceContactV <= maximumV
                ? profile.terrainSurfaceContactV : profile.terrainSurfaceUvRange.y;
        }

        private static float ResolveGroundTextureU(MapProfile profile, float textureU)
        {
            Material material = profile != null ? profile.terrainEdgeMaterial : null;
            float transformedU = textureU;
            if (material != null)
                transformedU = textureU * material.mainTextureScale.x + material.mainTextureOffset.x;

            if (material != null && material.HasProperty("_HorizontalSample"))
            {
                float fixedSample = material.GetFloat("_HorizontalSample");
                if (fixedSample >= 0f) return Mathf.Clamp01(fixedSample);
            }

            if (material != null && material.HasProperty("_SeamlessHorizontalTiling") &&
                material.GetFloat("_SeamlessHorizontalTiling") > .5f)
            {
                float tile = Mathf.Floor(transformedU);
                float localU = Mathf.Repeat(transformedU, 1f);
                if (Mathf.Repeat(Mathf.Abs(tile), 2f) >= 1f) localU = 1f - localU;
                // Samples are authored across the material's visible horizontal range after its
                // inset, so the normalized mirrored tile coordinate is the correct curve domain.
                return localU;
            }

            return Mathf.Repeat(transformedU, 1f);
        }

        public static float CalculateTerrainFillDepth(MapProfile profile)
        {
            if (profile == null) return 0f;
            if (profile.useSpaceTerrain && !profile.fitAuthoredTerrainTexture) return .46f;
            if (!profile.fitAuthoredTerrainTexture) return 0f;
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
                if (IsTerrainGap(profile, chunkStartX + (i + .5f) * dx)) continue;
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
