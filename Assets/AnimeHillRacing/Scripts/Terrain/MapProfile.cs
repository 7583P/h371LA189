using UnityEngine;
using UnityEngine.Serialization;
using AnimeHillRacing.Core;
using AnimeHillRacing.CameraSystem;

namespace AnimeHillRacing.Terrain
{
    public enum ArenaSegmentType
    {
        Bumpy,
        Ramp,
        TireObstacle,
        Lava,
        FloatingPlatform,
        LongJump,
        TechnicalClimb,
        TechnicalDescent,
        MixedObstacle,
        Recovery
    }

    [System.Serializable]
    public sealed class ArenaSettings
    {
        [Header("Segment Grammar")]
        [Min(70f)] public float segmentLength = 104f;
        [Range(3, 6)] public int recentSegmentHistory = 5;
        [Range(2, 5)] public int maximumTechnicalStreak = 3;
        [Range(4, 10)] public int recoveryInterval = 7;
        [Min(.2f)] public float baseBumpHeight = 1.15f;
        [Min(3f)] public float minimumBumpWavelength = 6.5f;
        [Min(6f)] public float maximumBumpWavelength = 15f;
        [Min(1f)] public float baseRampRise = 3.4f;
        [Min(1f)] public float maximumRampRise = 5.8f;
        [Min(6f)] public float minimumGapLength = 11f;
        [Min(8f)] public float maximumGapLength = 24f;
        [Min(8f)] public float landingSafetyLength = 22f;

        [Header("Physical Tires")]
        public Sprite tireSprite;
        [Range(1, 6)] public int minimumTires = 2;
        [Range(1, 8)] public int maximumTires = 5;
        [Min(.4f)] public float tireRadius = 1.05f;
        [Min(1f)] public float tireMass = 17f;
        [Range(0f, 1f)] public float tireFriction = .82f;
        [Range(0f, .4f)] public float tireBounciness = .035f;

        [Header("Tilting Platform")]
        public Sprite platformSprite;
        [Min(5f)] public float easyPlatformWidth = 13f;
        [Min(4f)] public float hardPlatformWidth = 8.5f;
        [Range(10f, 35f)] public float maximumPlatformTilt = 25f;
        [Min(.1f)] public float platformMass = 34f;
        [Min(0f)] public float platformAngularDamping = 3.2f;
        [Min(0f)] public float platformWeightTorque = 2.2f;
        [Min(0f)] public float platformCenteringTorque = 1.1f;

        [Header("Viscous Lava")]
        public Sprite lavaSprite;
        [Min(4f)] public float minimumLavaLength = 10f;
        [Min(6f)] public float maximumLavaLength = 24f;
        [Min(.2f)] public float lavaVisualDepth = 2.8f;
        [Min(.1f)] public float lavaDragStrength = 7.5f;
        [Min(.5f)] public float lavaMaximumSpeed = 6.5f;
        [Min(.05f)] public float lavaEntrySeconds = .18f;
        [Min(.05f)] public float lavaRecoverySeconds = .85f;
    }

    [System.Serializable]
    public sealed class RainstormSettings
    {
        public bool permanentRain;
        [Range(100, 900)] public int maximumParticles = 520;
        [Range(20f, 300f)] public float emissionRate = 145f;
        public Vector2 fallVelocity = new Vector2(-4.8f, -21f);
        [Range(.2f, 1.5f)] public float streakLength = .72f;
        [Range(.01f, .12f)] public float streakWidth = .035f;
        [Range(.25f, 1f)] public float surfaceGripMultiplier = .58f;
        [Range(.5f, 12f)] public float lightningEventsPerMinute = 4f;
        [Min(.05f)] public float lightningFlashDuration = .38f;
        [Range(.05f, .8f)] public float lightningFlashAlpha = .42f;
    }

    [System.Serializable]
    public sealed class LiquidChallengeSettings
    {
        public const float RequiredCycleLength = 6000f;
        public const int RequiredChallengesPerCycle = 8;

        [Header("Deterministic Layout")]
        public float cycleLength = RequiredCycleLength;
        public int challengesPerCycle = RequiredChallengesPerCycle;
        [Min(100f)] public float cycleStartSafetyMargin = 260f;
        [Tooltip("Run-up starts inside the authored Warmup, Gradual Descent, Bumpy Flat, Staircases, " +
                 "Broken Terrain, Rolling Descent, Double Hill and Tech Bumps sections, respectively.")]
        public float[] structuralRunUpPositions = DefaultStructuralRunUpPositions();

        [Header("Challenge Grammar")]
        [Min(10f)] public float runUpLength = 42f;
        [Min(1f)] public float rampTransitionLength = 6f;
        [Min(4f)] public float rampHorizontalLength = 12f;
        [Range(15f, 28f)] public float rampAngleDegrees = 20f;
        [Min(8f)] public float landingLength = 26f;
        [Min(10f)] public float recoveryLength = 48f;
        [Min(10f)] public float postRecoveryBlendLength = 50f;

        [Header("Pool Difficulty")]
        [Min(1f)] public float basePoolLength = 18f;
        [Min(1f)] public float minimumPoolLength = 16f;
        [Min(1f)] public float maximumPoolLength = 34f;
        [Min(0f)] public float cycleGrowth = .85f;
        [Min(.1f)] public float growthSaturation = 2.5f;
        [Range(0f, .5f)] public float deterministicLengthVariation = .12f;
        [Min(1f)] public float visualDepth = 7f;
        [Min(2f)] public float exitRampHorizontalLength = 12f;
        public float surfaceOffset = .15f;

        [Header("Caution Visual")]
        public Sprite cautionSprite;
        [Min(1f)] public float cautionDistanceBeforeRamp = 10f;
        [Min(.2f)] public float cautionBelowSurface = 1.65f;
        [Min(.2f)] public float cautionWidth = 3.2f;
        public int cautionSortingOrder = 4;

        [Header("Liquid Visual / Physics")]
        public Sprite liquidSprite;
        public int liquidSortingOrder = 3;
        [Range(0f, 1f)] public float normalAlpha = 1f;
        [Range(0f, 1f)] public float submergedAlpha = .65f;
        [Min(.01f)] public float alphaLerpSpeed = 7f;
        [Min(.05f)] public float exposureLimitSeconds = 10f;
        [Range(.01f, .5f)] public float entrySpeedRemaining = .10f;
        [Min(.05f)] public float entrySlowdownSeconds = .35f;
        [Min(0f)] public float continuousViscosity = 2.4f;
        [Min(0f)] public float liquidDriveAcceleration = 9f;
        [Min(.1f)] public float maximumSinkAcceleration = 8f;

        public static float[] DefaultStructuralRunUpPositions() =>
            new[] { 430f, 1280f, 2040f, 2770f, 3460f, 4140f, 4980f, 5530f };
    }

    [System.Serializable]
    public sealed class SpaceChallengeSettings
    {
        [Header("Deterministic Void Layout")]
        [Min(250f)] public float eventInterval = 2000f;
        [Min(25f)] public float warningDistance = 100f;
        [Min(120f), Tooltip("Exact flat run-up before the reusable ramp.")]
        public float approachLength = 200f;
        [Min(10f), Tooltip("Smooth transition from procedural terrain into the flat run-up.")]
        public float approachBlendLength = 45f;
        [Min(4f)] public float rampLength = 15f;
        [Range(.05f, .5f)] public float rampFlatFraction = .08f;
        [Min(.2f)] public float rampRise = 5.2f;
        [Min(8f)] public float landingLength = 42f;
        [Min(15f)] public float recoveryLength = 86f;
        [Min(1f)] public float initialGapLength = 10f;
        [Min(0f)] public float gapIncreasePerPhase = 5f;
        [Min(0f)] public float landingDrop = .35f;

        [Header("Black Hole")]
        public Sprite blackHoleSprite;
        [Min(.5f)] public float baseDiameter = 5.5f;
        [Min(0f)] public float diameterGrowthPerGapMetre = .14f;
        [Min(1f)] public float maximumDiameter = 10f;
        public float verticalOffset = -1.8f;
        [Range(.15f, .75f)] public float hazardRadiusFactor = .31f;

        [Header("Reusable Props")]
        public Sprite warningSprite;
        [Min(.2f)] public float warningWidth = 3.2f;
        public Material rampMaterial;
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Map Profile", fileName = "MapProfile")]
    public sealed class MapProfile : MapDefinition
    {
        [Header("Shared Longitudinal Profile")]
        public TerrainElevationProfile elevationProfile;

        [Header("Base Shape")]
        [Min(0f)] public float baseHeight = 0f;
        [Min(0f)] public float amplitude = 7f;
        [Min(0.0001f)] public float frequency = 0.014f;
        [Range(0f, 1f)] public float smoothing = 0.65f;
        [Min(1f)] public float hillSize = 1f;

        [Header("City Highway Terrain")]
        public bool useCityHighwayTerrain;
        [Min(42f)] public float cityFeatureLength = 120f;
        [Range(0f, .35f)] public float cityMiniRhythmStrength = .18f;

        [Header("Progressive Difficulty")]
        public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [FormerlySerializedAs("difficulty"), Range(0f, 2f)] public float maximumDifficulty = 1f;
        [Range(0f, 1f)] public float introductoryAmplitude = 0.12f;

        [Header("Gameplay Rhythm")]
        [Min(40f)] public float sectionLength = 90f;
        [Range(2, 12)] public int recoveryEverySections = 5;
        [Range(0f, 0.75f)] public float technicalSectionChance = 0.24f;
        [Range(0f, 1f)] public float technicalDetailStrength = 0.55f;
        [Range(0f, 1f)] public float flatFeatureWeight = 0.20f;
        [Range(0f, 1f)] public float hillFeatureWeight = 0.45f;
        [Range(0f, 1f)] public float valleyFeatureWeight = 0.20f;
        [Range(0f, 1f)] public float technicalFeatureWeight = 0.15f;

        [Header("Geometric Safety")]
        [FormerlySerializedAs("maximumSlopeDegrees"), Range(5f, 50f)] public float maxSlopeAngle = 18f;
        [Min(0.05f)] public float maxHeightDeltaPerSample = 1.05f;
        [Min(8f)] public float minimumValleyWidth = 32f;
        [Min(8f)] public float minimumLandingLength = 24f;
        [Range(0.5f, 20f)] public float maximumCrestSharpness = 5f;
        [Range(0, 8)] public int smoothingPasses = 2;

        [Header("Pickups")]
        [Min(0f)] public float coinDensityPer100m = 9f;
        [Min(1f)] public float initialFuelGap = 5f;
        [Min(0f)] public float fuelGapIncrease = 5f;
        [Min(0f)] public float fuelPlacementSearchRadius = 18f;
        [Min(1f)] public float fuelPlacementSearchStep = 3f;
        [Range(1f, 45f)] public float fuelMaximumPlacementSlope = 16f;

        [Header("Presentation")]
        public EnvironmentDefinition environment;
        public Material terrainMaterial;
        public Material terrainEdgeMaterial;
        public bool useSeparateTerrainMaterials;
        [Tooltip("Maps authored transparent terrain art only across its visible pixel band.")]
        public bool fitAuthoredTerrainTexture;
        [Min(1f)] public float terrainTextureRepeatWidth = 40f;
        public Vector2 terrainWallUvRange = new Vector2(0f, 1f);
        public Vector2 terrainSurfaceUvRange = new Vector2(0f, 1f);
        [Tooltip("V coordinate inside terrainSurfaceUvRange that represents the physical driving surface. " +
                 "Negative values keep the legacy behavior and use the upper edge.")]
        [Range(-1f, 1f)] public float terrainSurfaceContactV = -1f;
        [Tooltip("V coordinate where the deep Terrain fill starts behind the Ground artwork. " +
                 "Negative values use the lower edge of the authored Ground band.")]
        [Range(-1f, 1f)] public float terrainFillStartV = -1f;
        public PhysicsMaterial groundPhysicsMaterial;
        public PhysicsMaterial2D groundPhysicsMaterial2D;
        [Range(0.04f, 0.5f)] public float terrainEdgeWidth = 0.18f;
        [Min(5f)] public float terrainVisualDepth = 20f;

        [Header("Optional Liquid Challenges")]
        public bool supportsLiquidExposure;
        [Min(0f)] public float neonMaximumAmplitude = 28f;
        [Min(.1f)] public float neonAmplitudeSaturationCycles = 3f;
        public LiquidChallengeSettings liquidChallenges = new LiquidChallengeSettings();

        [Header("Optional Space Void Challenges")]
        public bool useSpaceTerrain;
        public SpaceChallengeSettings spaceChallenges = new SpaceChallengeSettings();

        [Header("Optional Arena Stunt Terrain")]
        public bool useArenaTerrain;
        public ArenaSettings arena = new ArenaSettings();

        [Header("Optional Rainstorm")]
        public RainstormSettings rainstorm = new RainstormSettings();

        public float EvaluateDifficulty(float distance)
        {
            float bandProgress = EvaluateDifficultyBands(distance);
            float evaluated = difficultyCurve != null ? difficultyCurve.Evaluate(bandProgress) : bandProgress;
            return Mathf.Max(0f, evaluated) * maximumDifficulty;
        }

        private void OnValidate()
        {
            supportedGameMode = GameModeType.Adventure2_5D;
            ClampDifficultyBands();
            sectionLength = Mathf.Max(sectionLength, minimumLandingLength * 2f, minimumValleyWidth * 2f);
            fuelPlacementSearchStep = Mathf.Max(0.5f, fuelPlacementSearchStep);
            initialFuelGap = Mathf.Max(1f, initialFuelGap);
            liquidChallenges ??= new LiquidChallengeSettings();
            spaceChallenges ??= new SpaceChallengeSettings();
            arena ??= new ArenaSettings();
            rainstorm ??= new RainstormSettings();
            rainstorm.maximumParticles = Mathf.Clamp(rainstorm.maximumParticles, 100, 900);
            rainstorm.emissionRate = Mathf.Clamp(rainstorm.emissionRate, 20f, 300f);
            rainstorm.surfaceGripMultiplier = Mathf.Clamp(rainstorm.surfaceGripMultiplier, .25f, 1f);
            rainstorm.lightningEventsPerMinute = Mathf.Clamp(rainstorm.lightningEventsPerMinute, .5f, 12f);
            rainstorm.lightningFlashDuration = Mathf.Max(.05f, rainstorm.lightningFlashDuration);
            if (supportsLiquidExposure)
            {
                neonMaximumAmplitude = Mathf.Max(amplitude, neonMaximumAmplitude);
                neonAmplitudeSaturationCycles = Mathf.Max(.1f, neonAmplitudeSaturationCycles);
                liquidChallenges.cycleLength = LiquidChallengeSettings.RequiredCycleLength;
                liquidChallenges.challengesPerCycle = LiquidChallengeSettings.RequiredChallengesPerCycle;
                if (liquidChallenges.structuralRunUpPositions == null ||
                    liquidChallenges.structuralRunUpPositions.Length !=
                    LiquidChallengeSettings.RequiredChallengesPerCycle)
                    liquidChallenges.structuralRunUpPositions =
                        LiquidChallengeSettings.DefaultStructuralRunUpPositions();
                liquidChallenges.rampAngleDegrees = Mathf.Clamp(liquidChallenges.rampAngleDegrees, 15f, 28f);
                liquidChallenges.maximumPoolLength = Mathf.Max(liquidChallenges.minimumPoolLength,
                    liquidChallenges.maximumPoolLength);
                liquidChallenges.recoveryLength = Mathf.Clamp(liquidChallenges.recoveryLength, 30f, 70f);
                liquidChallenges.postRecoveryBlendLength = Mathf.Max(10f,
                    liquidChallenges.postRecoveryBlendLength);
            }
            if (useSpaceTerrain)
            {
                supportsLiquidExposure = false;
                spaceChallenges.eventInterval = Mathf.Max(250f, spaceChallenges.eventInterval);
                spaceChallenges.warningDistance = Mathf.Max(25f, spaceChallenges.warningDistance);
                spaceChallenges.maximumDiameter = Mathf.Max(spaceChallenges.baseDiameter,
                    spaceChallenges.maximumDiameter);
                spaceChallenges.approachLength = Mathf.Max(120f, spaceChallenges.approachLength);
                spaceChallenges.approachBlendLength = Mathf.Max(10f, spaceChallenges.approachBlendLength);
                spaceChallenges.rampFlatFraction = Mathf.Clamp(spaceChallenges.rampFlatFraction, .05f, .5f);
            }
            if (useArenaTerrain)
            {
                useSpaceTerrain = false;
                supportsLiquidExposure = false;
                useCityHighwayTerrain = false;
                arena.segmentLength = Mathf.Max(70f, arena.segmentLength);
                arena.maximumGapLength = Mathf.Max(arena.minimumGapLength, arena.maximumGapLength);
                arena.maximumBumpWavelength = Mathf.Max(arena.minimumBumpWavelength,
                    arena.maximumBumpWavelength);
                arena.maximumTires = Mathf.Max(arena.minimumTires, arena.maximumTires);
                arena.hardPlatformWidth = Mathf.Min(arena.easyPlatformWidth,
                    Mathf.Max(4f, arena.hardPlatformWidth));
                arena.maximumLavaLength = Mathf.Max(arena.minimumLavaLength, arena.maximumLavaLength);
            }
        }
    }
}
