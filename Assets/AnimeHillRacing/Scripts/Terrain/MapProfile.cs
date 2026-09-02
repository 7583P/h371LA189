using UnityEngine;
using UnityEngine.Serialization;
using AnimeHillRacing.Core;
using AnimeHillRacing.CameraSystem;

namespace AnimeHillRacing.Terrain
{
    [System.Serializable]
    public sealed class MountainsSettings
    {
        [Header("Deterministic Mountain Grammar")]
        [Min(1000f), Tooltip("Distance at which Mountains reaches its full extreme slope grammar.")]
        public float peakDifficultyDistance = 10000f;
        [Min(6f)] public float miniBumpMinimumWavelength = 10f;
        [Min(10f)] public float miniBumpMaximumWavelength = 24f;
        [Min(.05f)] public float initialMiniBumpHeight = .12f;
        [Min(.1f)] public float maximumMiniBumpHeight = 1f;
        [Range(6f, 35f)] public Vector2 easySlopeRange = new Vector2(12f, 24f);
        [Range(10f, 45f)] public Vector2 mediumSlopeRange = new Vector2(18f, 34f);
        [Range(15f, 55f)] public Vector2 hardSlopeRange = new Vector2(26f, 44f);
        [Range(20f, 60f)] public Vector2 extremeSlopeRange = new Vector2(34f, 54f);
        [Range(35f, 62f)] public float exceptionalSlope = 58f;
        [Range(3, 12)] public int exceptionalEveryCells = 9;
        [Range(.01f, .12f)] public float crestWidthFraction = .06f;
        [Range(.03f, .18f)] public float recoveryWidthFraction = .1f;
        [Range(0f, .35f), Tooltip("Chance that a mountain cell becomes a descending valley instead of another positive peak.")]
        public float valleyChance = .22f;
        [Range(0f, .28f), Tooltip("Moves each crest away from the centre to create asymmetric climbs and descents.")]
        public float crestPositionVariation = .22f;
        [Min(0f), Tooltip("Height of the long connected relief below the individual mountain silhouettes.")]
        public float macroReliefHeight = 30f;
        [Min(300f), Tooltip("Approximate wavelength of broad elevation changes that prevent every cell returning to one baseline.")]
        public float macroReliefWavelength = 1150f;
        [Min(1f), Tooltip("Maximum broad hill height during the first 1000 metres.")]
        public float warmupMountainHeight = 4.5f;
        [Min(10f), Tooltip("Maximum mountain or deep-valley height once peak difficulty is reached.")]
        public float peakMountainHeight = 72f;
        [Range(.1f, .6f), Tooltip("Share of early cells reserved for straight bumpy terrain.")]
        public float initialFlatSectionChance = .42f;
        [Range(.05f, .4f), Tooltip("Share of cells that remain straight and bumpy at peak difficulty.")]
        public float peakFlatSectionChance = .16f;
        [Range(0f, .4f), Tooltip("Chance that a straight section becomes an extreme bump course at peak.")]
        public float peakExtremeBumpChance = .28f;
        [Min(.5f), Tooltip("Maximum height of the largest smooth, driveable bump course.")]
        public float peakExtremeBumpHeight = 4.8f;
        [Range(0f, 1f), Tooltip("Chance that a mountain face receives suspension-scale bumps.")]
        public float bumpyClimbChance = .38f;
        [Range(0f, .5f), Tooltip("Chance that a mountain cell receives a difficult bump course over its global slope.")]
        public float hardBumpChance = .18f;
        [Range(0f, 1f), Tooltip("Chance to place one Very Hard Bump inside an eligible cooldown window.")]
        public float veryHardBumpChance = .72f;
        [Min(600f), Tooltip("Minimum centre-to-centre recovery distance between Very Hard Bumps.")]
        public float veryHardBumpCooldownDistance = 1200f;
        [Min(4f), Tooltip("Peak height or valley depth of a Very Hard Bump at full difficulty.")]
        public float veryHardBumpHeight = 18f;
    }

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
        [Min(0f)] public float tireSurfaceClearance = .14f;

        [Header("Tilting Platform")]
        public Sprite platformSprite;
        [Min(5f)] public float easyPlatformWidth = 13f;
        [Min(4f)] public float hardPlatformWidth = 8.5f;
        [Range(10f, 35f)] public float maximumPlatformTilt = 25f;
        [Min(.1f)] public float platformMass = 34f;
        [Min(0f)] public float platformAngularDamping = 3.2f;
        [Min(0f)] public float platformWeightTorque = 2.2f;
        [Min(0f)] public float platformCenteringTorque = 1.1f;
        [Min(0f)] public float platformTakeoffDrop = .65f;
        [Min(0f)] public float platformLandingClearance = 1.1f;

        [Header("Viscous Lava")]
        public Sprite lavaSprite;
        [Min(4f)] public float minimumLavaLength = 10f;
        [Min(6f)] public float maximumLavaLength = 24f;
        [Min(.2f)] public float lavaVisualDepth = 2.8f;
        [Min(.1f)] public float lavaDragStrength = 7.5f;
        [Min(.5f)] public float lavaMaximumSpeed = 6.5f;
        [Min(.05f)] public float lavaEntrySeconds = .18f;
        [Min(.05f)] public float lavaRecoverySeconds = .85f;
        [Min(0f)] public float lavaBankInset = .35f;
        [Min(.25f)] public float lavaTriggerHeightAboveSurface = 1.25f;
        [Min(0f)] public float pickupObstacleClearance = 4f;
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

    public enum LastHorizonObstacleType
    {
        None,
        Seesaw,
        LiftPlatform,
        Rollers,
        TiltingPlatform,
        Ramp
    }

    public enum LastHorizonSpecialPhaseType
    {
        None,
        RoofTunnel,
        DescendingVoidPlatforms,
        MovingExtremeTerrain,
        RoofTransportCrusher
    }

    [System.Serializable]
    public sealed class LastHorizonSettings
    {
        public const float RequiredCycleLength = 10000f;
        public const float RequiredSystemLength = 20000f;

        [Header("Two-Cycle Terrain Grammar (10 km + 10 km)")]
        [Min(100f)] public float cycleLength = RequiredCycleLength;
        [Min(120f)] public float segmentLength = 250f;
        [Min(40f)] public float launchSafetyLength = 160f;
        [Min(1f)] public float cycleOneMaximumRelief = 58f;
        [Min(1f)] public float cycleTwoMaximumRelief = 72f;
        [Min(0f)] public float macroRelief = 12f;

        [Header("Exclusive Ragnarok Phases")]
        [Range(2, 3)] public int minimumRoofZonesPerCycle = 2;
        [Range(2, 3)] public int maximumRoofZonesPerCycle = 3;
        public Vector2 roofLengthRange = new Vector2(200f, 5000f);
        [Min(4f)] public float roofVehicleClearance = 6.4f;
        [Min(.2f)] public float roofThickness = 2.8f;
        [Min(1f)] public float roofSampleSpacing = 6f;
        [Min(24f)] public float roofTransportGapLength = 52f;
        [Min(6f)] public float roofTransportPlatformWidth = 12f;
        [Min(.2f)] public float roofTransportPlatformThickness = .8f;
        [Range(2, 5)] public int roofCrusherBlockCount = 3;
        [Min(1.5f)] public float roofCrusherBlockSize = 3.2f;
        [Min(5000f)] public float movingTerrainInterval = 6000f;
        public Vector2 movingTerrainLengthRange = new Vector2(360f, 620f);
        [Min(7000f)] public float voidPlatformInterval = 8000f;
        public Vector2 voidPlatformLengthRange = new Vector2(420f, 760f);
        [Min(20f)] public float specialPhaseTransitionLength = 52f;
        [Min(20f)] public float specialPhaseSeparation = 150f;

        [Header("Descending Void Platforms")]
        [Min(4f)] public float voidPlatformWidth = 12f;
        [Min(1f)] public float voidPlatformGap = 6f;
        [Min(.2f)] public float voidPlatformThickness = .7f;
        [Min(.5f)] public float voidPlatformMaximumDrop = 12f;
        [Range(4, 16)] public int voidDecorativePlatformCount = 8;

        [Header("Moving Extreme Terrain")]
        [Min(.1f)] public float movingHorizontalTravel = 1.15f;
        [Min(.1f)] public float movingVerticalTravel = .7f;
        [Min(1f)] public float movingTerrainPeriod = 5.5f;
        [Range(65f, 80f)] public float extremeBumpAngle = 80f;
        [Min(.1f)] public float extremeBumpHeight = .55f;
        [Min(4f)] public float extremeBumpSpacing = 12f;

        [Header("Deterministic Fuel")]
        public Vector2 normalFuelGapRange = new Vector2(200f, 500f);
        [Min(1000f)] public float cycleResetFuelInterval = RequiredSystemLength;
        public Vector2 scarcityFuelGapRange = new Vector2(500f, 1000f);
        [Min(4f)] public float fuelSafetySearchRadius = 30f;
        [Min(.5f)] public float fuelSafetySearchStep = 2f;

        [Header("Pooled Obstacles")]
        [Min(180f)] public float obstacleCellLength = 300f;
        [Range(0f, 1f)] public float cycleOneObstacleChance = .42f;
        [Range(0f, 1f)] public float cycleTwoObstacleChance = .72f;
        [Min(20f)] public float obstacleStartSafety = 260f;
        [Min(3f)] public float safeVehicleLength = 5.5f;
        [Min(2f)] public float safeVehicleHeight = 3.6f;
        [Min(.5f)] public float obstacleSafetyMargin = 2.5f;
        [Min(5f)] public float seesawWidth = 14f;
        [Range(5f, 25f)] public float seesawMaximumAngle = 17f;
        [Min(5f)] public float liftWidth = 12f;
        [Min(.2f)] public float liftTravel = 1.6f;
        [Min(1f)] public float liftPeriod = 5.5f;
        [Min(5f)] public float tiltingPlatformWidth = 13f;
        [Range(3f, 22f)] public float tiltingPlatformMaximumAngle = 12f;
        [Min(1f)] public float tiltingPlatformPeriod = 4.25f;
        [Range(1, 4)] public int maximumRollers = 4;
        [Min(.35f)] public float rollerRadius = .85f;
        public Sprite seesawSprite;
        public Sprite liftPlatformSprite;
        public Sprite rollerSprite;
    }

    [System.Serializable]
    public sealed class CityBridgeSettings
    {
        public const float RequiredCycleLength = 10000f;
        public const int RequiredMinimumZones = 7;
        public const int RequiredMaximumZones = 10;
        public const int RequiredMinimumProgressiveZones = 5;
        public const int RequiredMaximumProgressiveZones = 7;
        public const float RequiredProgressiveGrowth = 10f;
        public const float RequiredBridgeAngle = 45f;
        public const float RequiredTransitionSeconds = 3f;

        [Header("Deterministic 10,000 m Layout")]
        public float cycleLength = RequiredCycleLength;
        public int minimumZonesPerCycle = RequiredMinimumZones;
        public int maximumZonesPerCycle = RequiredMaximumZones;
        public int minimumProgressiveZones = RequiredMinimumProgressiveZones;
        public int maximumProgressiveZones = RequiredMaximumProgressiveZones;
        [Min(100f)] public float cycleStartMargin = 320f;
        [Min(100f)] public float cycleEndMargin = 320f;
        [Min(50f)] public float minimumZoneSpacing = 300f;
        [Min(5f)] public float approachBlendLength = 50f;
        [Min(5f)] public float recoveryBlendLength = 50f;

        [Header("Bridge / Water Geometry")]
        [Min(4f)] public float bridgeLength = 14f;
        [Min(2f)] public float normalWaterLength = 22f;
        [Min(2f)] public float progressiveBaseWaterLength = 22f;
        [Min(0f)] public float progressiveGrowthPerCycle = RequiredProgressiveGrowth;
        [Min(.5f)] public float waterVisualDepth = 10f;
        public float waterSurfaceOffset = -.45f;
        [Range(.05f, 1f)] public float waterVisualAlpha = .5f;
        [Min(.5f)] public float waterBedThickness = 22f;
        [Min(1f)] public float waterEndBankWidth = 3f;
        [Range(.05f, 1f)] public float waterSpeedRetainedPerSecond = .4f;
        [Range(.05f, 1f)] public float waterAngularSpeedRetainedPerSecond = .3f;
        [Min(.1f)] public float waterBuoyancySpring = 4f;
        [Min(.1f)] public float waterVerticalDamping = 3f;
        [Min(.1f)] public float waterMaximumSinkAcceleration = 8f;
        [Min(.1f)] public float waterDriverDownClearance = 5f;
        [Min(.08f)] public float bridgeColliderThickness = .45f;
        [Range(0f, 2f)] public float bridgeGroundOverlap = 1f;

        [Header("Bridge Animation")]
        [Range(0f, 45f)] public float closedAngleDegrees;
        [Range(0f, 45f)] public float openAngleDegrees = RequiredBridgeAngle;
        [Min(.1f)] public float transitionSeconds = RequiredTransitionSeconds;
        [Min(0f)] public float closedHoldSeconds = 4f;
        [Min(0f)] public float openHoldSeconds = 4f;

        [Header("Authored Visuals")]
        public Sprite bridgeSprite;
        public Sprite waterSprite;
        [Tooltip("Slope already painted into Bridge.png; the visual child compensates it while physics stays horizontal at 0 degrees.")]
        public float bridgeArtSlopeDegrees = 16.1f;
        [Tooltip("Moves the compensated bridge art so its painted road surface matches the collider top.")]
        public float bridgeArtSurfaceOffset = -.61f;
        public int waterSortingOrder = 1;
        public int waterBedSortingOrder;
        public int bridgeSortingOrder = 5;
    }

    public enum SpaceEventType
    {
        Void,
        Planet
    }

    public enum SpacePlanetType
    {
        Mercury,
        Venus,
        Earth,
        Mars,
        Jupiter,
        Saturn,
        Uranus,
        Neptune
    }

    [System.Serializable]
    public sealed class CountrysideSpecialFeatureSettings
    {
        [Header("Progressive Mini Hills")]
        [Min(1000f), Tooltip("Distance at which Countryside mini hills reach their maximum relief.")]
        public float peakDifficultyDistance = 10000f;
        [Min(8f)] public float miniHillMinimumWavelength = 30f;
        [Min(12f)] public float miniHillMaximumWavelength = 96f;
        [Min(.05f), Tooltip("Visible rolling relief near the starting area, after the short spawn blend.")]
        public float initialMiniHillHeight = .45f;
        [Min(.1f), Tooltip("Maximum extra rolling relief at peak terrain difficulty.")]
        public float peakMiniHillHeight = 3.4f;

        [Header("Micro Terrain Variety")]
        [Min(30f), Tooltip("Length of each deterministic micro-terrain section.")]
        public float microSectionLength = 66f;
        [Min(.05f), Tooltip("Height of small bumps, rises and dips near the start.")]
        public float initialMicroHeight = .28f;
        [Min(.1f), Tooltip("Height of small bumps, rises and dips at peak difficulty.")]
        public float peakMicroHeight = 2.4f;
        [Range(0f, 1f), Tooltip("Share of micro sections that stay mostly straight with isolated bumps.")]
        public float miniStraightChance = .36f;
        [Range(2, 6), Tooltip("Number of isolated bumps or small rises in a mini-straight section.")]
        public int miniStraightBumpCount = 3;
        [Range(.1f, .6f), Tooltip("Share of large challenge cells replaced by a true straight or straight descent near the start.")]
        public float initialChallengeStraightChance = .34f;
        [Range(.1f, .6f), Tooltip("Share of large challenge cells replaced by a true straight or straight descent at peak difficulty.")]
        public float peakChallengeStraightChance = .26f;
        [Range(0f, 1f), Tooltip("Chance that a challenge straight is a gentle linear descent instead of horizontal.")]
        public float straightDescentChance = .42f;

        [Header("Progressive Mountain Challenge")]
        [Min(0f)] public float challengeStartDistance = 350f;
        [Min(60f)] public float challengeMinimumCellLength = 135f;
        [Min(80f)] public float challengeMaximumCellLength = 240f;
        [Min(.1f)] public float initialChallengeHeight = 1.5f;
        [Min(1f)] public float peakChallengeHeight = 68f;
        [Range(5f, 60f)] public float initialChallengeSlope = 12f;
        [Range(15f, 62f)] public float peakChallengeSlope = 58f;
        [Range(0f, .5f)] public float initialDeepValleyChance = .12f;
        [Range(.05f, .75f)] public float peakDeepValleyChance = .46f;
        [Range(.5f, 1.5f)] public float valleyDepthMultiplier = 1.08f;

        [Header("Authored Interruption Events")]
        [Min(250f)] public float eventInterval = 500f;
        [Min(0f)] public float firstEventDistance = 260f;
        [Range(0f, 1f)] public float eventChance = .88f;
        [Range(0f, 1f)] public float longFlatChance = .58f;
        [Min(5f)] public float transitionLength = 45f;
        [Tooltip("Minimum and maximum length of the deterministic straight core.")]
        public Vector2 longFlatLengths = new Vector2(150f, 200f);
        [Min(10f)] public float slashFlatLead = 18f;
        [Min(30f)] public float slashLength = 96f;
        [Min(1f)] public float slashHeight = 18f;
        [Min(0f)] public float maximumStartJitter = 70f;
    }

    [System.Serializable]
    public sealed class SpaceChallengeSettings
    {
        [Header("Deterministic Event Layout")]
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
        [Min(1f), Tooltip("Base gap width in logical metres.")]
        public float initialGapLength = 15f;
        [FormerlySerializedAs("gapIncreasePerPhase"), FormerlySerializedAs("gapIncreasePerTier"), Min(0f),
         Tooltip("Horizontal metres added to the void and black hole at every 2000-metre Space event.")]
        public float gapIncreasePerCycle = 5f;
        [Min(0f)] public float landingDrop = .35f;

        [Header("Driveable Planets")]
        [Min(2f)] public float planetLaunchClearance = 4.5f;
        [Min(2f)] public float planetExitClearance = 4f;
        [Min(2f)] public float minimumPlanetRadius = 4.8f;
        [Min(3f)] public float maximumPlanetRadius = 10.5f;
        [Min(0f)] public float planetRampRise = 3.8f;
        [Range(.6f, .88f), Tooltip("Height of each upper-arc endpoint above the planet centre, as a radius fraction.")]
        public float planetCenterDrop = .82f;

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
        public bool useCityBridges;
        public CityBridgeSettings cityBridges = new CityBridgeSettings();

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

        [Header("Optional Pointed Terrain Rhythm")]
        [Tooltip("Uses deterministic asymmetric peaks plus small pointed rises and dips inside each section.")]
        public bool usePointedTerrainRhythm;
        [Range(.01f, .15f)] public float pointedMiniFeatureStrength = .07f;
        [Range(0f, .3f)] public float pointedFeatureAsymmetry = .18f;
        [Range(4, 12), Tooltip("Approximate number of small pointed rises/dips per section at peak difficulty.")]
        public int pointedPeakMiniFeatureCount = 8;
        [Range(0f, 1f), Tooltip("Peak-difficulty chance of adding a larger V valley or inverted-V crest.")]
        public float pointedExtremeShapeChance = .55f;
        [Range(.05f, .6f)] public float pointedExtremeShapeStrength = .32f;

        [Header("Optional Countryside Special Features")]
        public bool useCountrysideSpecialFeatures;
        public CountrysideSpecialFeatureSettings countrysideSpecialFeatures =
            new CountrysideSpecialFeatureSettings();

        [Header("Geometric Safety")]
        [FormerlySerializedAs("maximumSlopeDegrees"), Range(5f, 82f)] public float maxSlopeAngle = 18f;
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
        [Tooltip("Optional full-card artwork used only by the track selection UI. Gameplay backgrounds remain unchanged.")]
        public Sprite selectionPreview;
        [Tooltip("Clips vehicle sprites at the authoritative terrain contact line while keeping their " +
                 "visible portion in front of the 2.5D terrain.")]
        public bool useVehicleContactLineOcclusion;
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
        [Tooltip("Optional contact V samples across one authored Ground tile. When present, the visual " +
                 "ribbon is warped so a painted wavy driving line remains exactly on the physical collider.")]
        public float[] terrainSurfaceContactVSamples = new float[0];
        [Tooltip("V coordinate where the deep Terrain fill starts behind the Ground artwork. " +
                 "Negative values use the lower edge of the authored Ground band.")]
        [Range(-1f, 1f)] public float terrainFillStartV = -1f;
        public PhysicsMaterial groundPhysicsMaterial;
        public PhysicsMaterial2D groundPhysicsMaterial2D;
        [Range(0.04f, 0.5f)] public float terrainEdgeWidth = 0.18f;
        [Tooltip("Optional maximum size, in metres, for either half of the authored Ground ribbon. " +
                 "Zero keeps the legacy per-map behavior. A positive value scales the painted rise and " +
                 "depth uniformly, so the configured contact row stays exactly on the collider.")]
        [Min(0f)] public float terrainMaximumVisualExtent;
        [Tooltip("Optional independent limit for the painted Ground above the wheel-contact line. " +
                 "Zero keeps the rise produced by the shared extent scale.")]
        [Min(0f)] public float terrainMaximumVisualRiseAboveSurface;
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

        [Header("Optional Extreme Mountains Terrain")]
        public bool useMountainsTerrain;
        public MountainsSettings mountains = new MountainsSettings();

        [Header("Optional Last Horizon Terrain")]
        public bool useLastHorizonTerrain;
        public LastHorizonSettings lastHorizon = new LastHorizonSettings();

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
            cityBridges ??= new CityBridgeSettings();
            spaceChallenges ??= new SpaceChallengeSettings();
            arena ??= new ArenaSettings();
            mountains ??= new MountainsSettings();
            lastHorizon ??= new LastHorizonSettings();
            rainstorm ??= new RainstormSettings();
            countrysideSpecialFeatures ??= new CountrysideSpecialFeatureSettings();
            if (useCityBridges)
            {
                useCityHighwayTerrain = true;
                cityBridges.cycleLength = CityBridgeSettings.RequiredCycleLength;
                cityBridges.minimumZonesPerCycle = CityBridgeSettings.RequiredMinimumZones;
                cityBridges.maximumZonesPerCycle = CityBridgeSettings.RequiredMaximumZones;
                cityBridges.minimumProgressiveZones = CityBridgeSettings.RequiredMinimumProgressiveZones;
                cityBridges.maximumProgressiveZones = CityBridgeSettings.RequiredMaximumProgressiveZones;
                cityBridges.progressiveGrowthPerCycle = CityBridgeSettings.RequiredProgressiveGrowth;
                cityBridges.closedAngleDegrees = 0f;
                cityBridges.openAngleDegrees = CityBridgeSettings.RequiredBridgeAngle;
                cityBridges.transitionSeconds = CityBridgeSettings.RequiredTransitionSeconds;
                cityBridges.bridgeLength = Mathf.Max(4f, cityBridges.bridgeLength);
                cityBridges.normalWaterLength = Mathf.Max(2f, cityBridges.normalWaterLength);
                cityBridges.progressiveBaseWaterLength = Mathf.Max(2f,
                    cityBridges.progressiveBaseWaterLength);
                cityBridges.waterVisualDepth = Mathf.Max(.5f, cityBridges.waterVisualDepth);
                cityBridges.waterVisualAlpha = Mathf.Clamp(cityBridges.waterVisualAlpha, .05f, 1f);
                cityBridges.waterBedThickness = Mathf.Max(.5f, cityBridges.waterBedThickness);
                cityBridges.waterEndBankWidth = Mathf.Max(1f, cityBridges.waterEndBankWidth);
                cityBridges.waterSpeedRetainedPerSecond = Mathf.Clamp(
                    cityBridges.waterSpeedRetainedPerSecond, .05f, 1f);
                cityBridges.waterAngularSpeedRetainedPerSecond = Mathf.Clamp(
                    cityBridges.waterAngularSpeedRetainedPerSecond, .05f, 1f);
                cityBridges.waterBuoyancySpring = Mathf.Max(.1f, cityBridges.waterBuoyancySpring);
                cityBridges.waterVerticalDamping = Mathf.Max(.1f, cityBridges.waterVerticalDamping);
                cityBridges.waterMaximumSinkAcceleration = Mathf.Max(.1f,
                    cityBridges.waterMaximumSinkAcceleration);
                cityBridges.waterDriverDownClearance = Mathf.Max(.1f,
                    cityBridges.waterDriverDownClearance);
                cityBridges.minimumZoneSpacing = Mathf.Max(50f, cityBridges.minimumZoneSpacing);
                cityBridges.approachBlendLength = Mathf.Max(5f, cityBridges.approachBlendLength);
                cityBridges.recoveryBlendLength = Mathf.Max(5f, cityBridges.recoveryBlendLength);
            }
            countrysideSpecialFeatures.eventInterval = Mathf.Max(250f,
                countrysideSpecialFeatures.eventInterval);
            countrysideSpecialFeatures.peakDifficultyDistance = Mathf.Max(1000f,
                countrysideSpecialFeatures.peakDifficultyDistance);
            countrysideSpecialFeatures.miniHillMinimumWavelength = Mathf.Max(8f,
                countrysideSpecialFeatures.miniHillMinimumWavelength);
            countrysideSpecialFeatures.miniHillMaximumWavelength = Mathf.Max(
                countrysideSpecialFeatures.miniHillMinimumWavelength,
                countrysideSpecialFeatures.miniHillMaximumWavelength);
            countrysideSpecialFeatures.initialMiniHillHeight = Mathf.Max(.05f,
                countrysideSpecialFeatures.initialMiniHillHeight);
            countrysideSpecialFeatures.peakMiniHillHeight = Mathf.Max(
                countrysideSpecialFeatures.initialMiniHillHeight,
                countrysideSpecialFeatures.peakMiniHillHeight);
            countrysideSpecialFeatures.challengeStartDistance = Mathf.Max(0f,
                countrysideSpecialFeatures.challengeStartDistance);
            countrysideSpecialFeatures.challengeMinimumCellLength = Mathf.Max(60f,
                countrysideSpecialFeatures.challengeMinimumCellLength);
            countrysideSpecialFeatures.challengeMaximumCellLength = Mathf.Max(
                countrysideSpecialFeatures.challengeMinimumCellLength,
                countrysideSpecialFeatures.challengeMaximumCellLength);
            countrysideSpecialFeatures.initialChallengeHeight = Mathf.Max(.1f,
                countrysideSpecialFeatures.initialChallengeHeight);
            countrysideSpecialFeatures.peakChallengeHeight = Mathf.Max(
                countrysideSpecialFeatures.initialChallengeHeight,
                countrysideSpecialFeatures.peakChallengeHeight);
            countrysideSpecialFeatures.peakChallengeSlope = Mathf.Max(
                countrysideSpecialFeatures.initialChallengeSlope,
                countrysideSpecialFeatures.peakChallengeSlope);
            countrysideSpecialFeatures.peakDeepValleyChance = Mathf.Max(
                countrysideSpecialFeatures.initialDeepValleyChance,
                countrysideSpecialFeatures.peakDeepValleyChance);
            countrysideSpecialFeatures.transitionLength = Mathf.Max(5f,
                countrysideSpecialFeatures.transitionLength);
            countrysideSpecialFeatures.longFlatLengths.x = Mathf.Max(50f,
                countrysideSpecialFeatures.longFlatLengths.x);
            countrysideSpecialFeatures.longFlatLengths.y = Mathf.Max(50f,
                countrysideSpecialFeatures.longFlatLengths.y);
            countrysideSpecialFeatures.slashLength = Mathf.Max(30f,
                countrysideSpecialFeatures.slashLength);
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
                spaceChallenges.initialGapLength = Mathf.Max(1f, spaceChallenges.initialGapLength);
                spaceChallenges.gapIncreasePerCycle = Mathf.Max(0f, spaceChallenges.gapIncreasePerCycle);
                spaceChallenges.maximumPlanetRadius = Mathf.Max(spaceChallenges.minimumPlanetRadius,
                    spaceChallenges.maximumPlanetRadius);
                spaceChallenges.planetLaunchClearance = Mathf.Max(2f, spaceChallenges.planetLaunchClearance);
                spaceChallenges.planetExitClearance = Mathf.Max(2f, spaceChallenges.planetExitClearance);
                spaceChallenges.planetCenterDrop = Mathf.Clamp(spaceChallenges.planetCenterDrop, .6f, .88f);
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
            if (useLastHorizonTerrain)
            {
                useSpaceTerrain = false;
                supportsLiquidExposure = false;
                useCityHighwayTerrain = false;
                useCityBridges = false;
                useArenaTerrain = false;
                useMountainsTerrain = false;
                lastHorizon.cycleLength = LastHorizonSettings.RequiredCycleLength;
                lastHorizon.segmentLength = Mathf.Max(120f, lastHorizon.segmentLength);
                lastHorizon.launchSafetyLength = Mathf.Max(40f, lastHorizon.launchSafetyLength);
                lastHorizon.safeVehicleLength = Mathf.Max(3f,
                    lastHorizon.safeVehicleLength);
                lastHorizon.safeVehicleHeight = Mathf.Max(2f,
                    lastHorizon.safeVehicleHeight);
                lastHorizon.minimumRoofZonesPerCycle = Mathf.Clamp(
                    lastHorizon.minimumRoofZonesPerCycle, 2, 3);
                lastHorizon.maximumRoofZonesPerCycle = Mathf.Clamp(
                    lastHorizon.maximumRoofZonesPerCycle,
                    lastHorizon.minimumRoofZonesPerCycle, 3);
                lastHorizon.roofLengthRange = OrderedRange(lastHorizon.roofLengthRange,
                    200f, 5000f, 200f, 5000f);
                lastHorizon.roofVehicleClearance = Mathf.Max(lastHorizon.safeVehicleHeight + .8f,
                    lastHorizon.roofVehicleClearance);
                lastHorizon.roofThickness = Mathf.Max(.2f, lastHorizon.roofThickness);
                lastHorizon.roofSampleSpacing = Mathf.Max(1f, lastHorizon.roofSampleSpacing);
                lastHorizon.roofTransportGapLength = Mathf.Clamp(
                    lastHorizon.roofTransportGapLength, 24f, 64f);
                lastHorizon.roofTransportPlatformWidth = Mathf.Clamp(
                    lastHorizon.roofTransportPlatformWidth, 6f,
                    lastHorizon.roofTransportGapLength - 4f);
                lastHorizon.roofTransportPlatformThickness = Mathf.Max(.2f,
                    lastHorizon.roofTransportPlatformThickness);
                lastHorizon.roofCrusherBlockCount = Mathf.Clamp(
                    lastHorizon.roofCrusherBlockCount, 2, 5);
                lastHorizon.roofCrusherBlockSize = Mathf.Max(1.5f,
                    lastHorizon.roofCrusherBlockSize);
                lastHorizon.movingTerrainInterval = Mathf.Clamp(lastHorizon.movingTerrainInterval,
                    5000f, 7000f);
                lastHorizon.movingTerrainLengthRange = OrderedRange(
                    lastHorizon.movingTerrainLengthRange, 360f, 620f, 240f, 900f);
                lastHorizon.voidPlatformInterval = Mathf.Clamp(lastHorizon.voidPlatformInterval,
                    7000f, 10000f);
                lastHorizon.voidPlatformLengthRange = OrderedRange(
                    lastHorizon.voidPlatformLengthRange, 420f, 760f, 300f, 1200f);
                lastHorizon.specialPhaseTransitionLength = Mathf.Max(20f,
                    lastHorizon.specialPhaseTransitionLength);
                lastHorizon.specialPhaseSeparation = Mathf.Max(20f,
                    lastHorizon.specialPhaseSeparation);
                lastHorizon.voidPlatformWidth = Mathf.Max(4f, lastHorizon.voidPlatformWidth);
                lastHorizon.voidPlatformGap = Mathf.Max(1f, lastHorizon.voidPlatformGap);
                lastHorizon.voidPlatformThickness = Mathf.Max(.2f,
                    lastHorizon.voidPlatformThickness);
                lastHorizon.voidPlatformMaximumDrop = Mathf.Max(.5f,
                    lastHorizon.voidPlatformMaximumDrop);
                lastHorizon.movingHorizontalTravel = Mathf.Max(.1f,
                    lastHorizon.movingHorizontalTravel);
                lastHorizon.movingVerticalTravel = Mathf.Max(.1f,
                    lastHorizon.movingVerticalTravel);
                lastHorizon.movingTerrainPeriod = Mathf.Max(1f,
                    lastHorizon.movingTerrainPeriod);
                lastHorizon.extremeBumpAngle = Mathf.Clamp(lastHorizon.extremeBumpAngle, 65f, 80f);
                lastHorizon.extremeBumpHeight = Mathf.Max(.1f,
                    lastHorizon.extremeBumpHeight);
                lastHorizon.extremeBumpSpacing = Mathf.Max(4f,
                    lastHorizon.extremeBumpSpacing);
                lastHorizon.normalFuelGapRange = OrderedRange(lastHorizon.normalFuelGapRange, 200f, 500f,
                    1f, 1000f);
                lastHorizon.cycleResetFuelInterval = LastHorizonSettings.RequiredSystemLength;
                lastHorizon.scarcityFuelGapRange = OrderedRange(lastHorizon.scarcityFuelGapRange,
                    500f, 1000f, 1f, 1000f);
                lastHorizon.scarcityFuelGapRange.y = Mathf.Min(1000f,
                    lastHorizon.scarcityFuelGapRange.y);
                lastHorizon.fuelSafetySearchStep = Mathf.Max(.5f,
                    lastHorizon.fuelSafetySearchStep);
                lastHorizon.obstacleCellLength = Mathf.Max(180f,
                    lastHorizon.obstacleCellLength);
                lastHorizon.seesawWidth = Mathf.Max(lastHorizon.safeVehicleLength * 2f +
                    lastHorizon.obstacleSafetyMargin, lastHorizon.seesawWidth);
                lastHorizon.liftWidth = Mathf.Max(lastHorizon.safeVehicleLength * 1.5f +
                    lastHorizon.obstacleSafetyMargin, lastHorizon.liftWidth);
                lastHorizon.tiltingPlatformWidth = Mathf.Max(lastHorizon.safeVehicleLength * 1.5f +
                    lastHorizon.obstacleSafetyMargin, lastHorizon.tiltingPlatformWidth);
                lastHorizon.maximumRollers = Mathf.Clamp(lastHorizon.maximumRollers, 1, 4);
            }
            if (useMountainsTerrain)
            {
                useSpaceTerrain = false;
                useArenaTerrain = false;
                supportsLiquidExposure = false;
                useCityHighwayTerrain = false;
                maxSlopeAngle = Mathf.Clamp(maxSlopeAngle, 5f, 82f);
                mountains.miniBumpMaximumWavelength = Mathf.Max(mountains.miniBumpMinimumWavelength,
                    mountains.miniBumpMaximumWavelength);
                mountains.peakDifficultyDistance = Mathf.Max(hardEndDistance,
                    mountains.peakDifficultyDistance);
                mountains.exceptionalSlope = Mathf.Clamp(mountains.exceptionalSlope, 35f, 62f);
            }
        }

        private static Vector2 OrderedRange(Vector2 value, float fallbackMin, float fallbackMax,
            float absoluteMin, float absoluteMax)
        {
            float minimum = value.x > 0f ? value.x : fallbackMin;
            float maximum = value.y > 0f ? value.y : fallbackMax;
            minimum = Mathf.Clamp(minimum, absoluteMin, absoluteMax);
            maximum = Mathf.Clamp(maximum, minimum, absoluteMax);
            return new Vector2(minimum, maximum);
        }
    }
}
