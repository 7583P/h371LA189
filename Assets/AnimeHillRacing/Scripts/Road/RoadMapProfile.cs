using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Road
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Road Map Profile", fileName = "RoadMapProfile")]
    public sealed class RoadMapProfile : MapDefinition
    {
        [Min(4f)] public float roadWidth = 10f;
        [Min(20f)] public float chunkLength = 100f;
        [Range(8, 64)] public int samplesPerChunk = 32;
        [Min(0f)] public float curveIntensity = 7f;
        [Min(0f)] public float elevationIntensity = 2.2f;
        [Min(0.0001f)] public float curveFrequency = 0.006f;
        [Min(0.0001f)] public float elevationFrequency = 0.009f;
        public AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1f);
        [Min(1f)] public float initialFuelGap = 5f;
        [Min(0f)] public float fuelGapIncrease = 5f;
        public Material roadMaterial;
        public Material shoulderMaterial;
        public PhysicsMaterial groundPhysicsMaterial;
        [Header("Shared deterministic profiles")]
        public TerrainElevationProfile elevationProfile;
        public RoadCurvatureProfile curvatureProfile;

        private void OnEnable()
        {
            supportedGameMode = GameModeType.Adventure3D;
            ClampDifficultyBands();
            initialFuelGap = Mathf.Max(1f, initialFuelGap);
            fuelGapIncrease = Mathf.Max(0f, fuelGapIncrease);
        }

        public float Difficulty(float distance)
        {
            float t = EvaluateDifficultyBands(distance);
            return difficultyCurve != null ? Mathf.Max(0f, difficultyCurve.Evaluate(t)) : t;
        }
    }
}
