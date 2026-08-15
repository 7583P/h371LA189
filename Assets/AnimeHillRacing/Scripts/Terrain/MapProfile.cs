using UnityEngine;
using UnityEngine.Serialization;
using AnimeHillRacing.Core;
using AnimeHillRacing.CameraSystem;

namespace AnimeHillRacing.Terrain
{
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
        public PhysicsMaterial groundPhysicsMaterial;
        [Range(0.04f, 0.5f)] public float terrainEdgeWidth = 0.18f;
        [Min(5f)] public float terrainVisualDepth = 20f;

        [Header("Optional Liquid Challenges")]
        public bool supportsLiquidExposure;
        [Min(0f)] public float neonMaximumAmplitude = 28f;
        [Min(.1f)] public float neonAmplitudeSaturationCycles = 3f;
        public LiquidChallengeSettings liquidChallenges = new LiquidChallengeSettings();

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
        }
    }
}
