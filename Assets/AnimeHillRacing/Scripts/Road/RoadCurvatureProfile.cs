using UnityEngine;

namespace AnimeHillRacing.Road
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Road Curvature Profile", fileName = "RoadCurvatureProfile")]
    public sealed class RoadCurvatureProfile : ScriptableObject
    {
        public int seed = 18472;
        [Min(0f)] public float intensity = 10f;
        [Min(0.0001f)] public float frequency = 0.006f;

        public float GetCenterOffsetAtDistance(float distanceAlongMap, float difficulty)
        {
            float phase = seed * 0.017f;
            return (Mathf.Sin(distanceAlongMap * frequency + phase) * 0.68f +
                    Mathf.Sin(distanceAlongMap * frequency * 0.43f + phase * 1.7f) * 0.32f) *
                   intensity * Mathf.Clamp01(difficulty);
        }
    }
}
