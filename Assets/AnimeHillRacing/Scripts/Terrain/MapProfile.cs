using UnityEngine;
using UnityEngine.Serialization;
using AnimeHillRacing.Core;
using AnimeHillRacing.CameraSystem;

namespace AnimeHillRacing.Terrain
{
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
        }
    }
}
