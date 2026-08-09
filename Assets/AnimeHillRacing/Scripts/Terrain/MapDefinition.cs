using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnimeHillRacing.Terrain
{
    public abstract class MapDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string mapId = "map";
        public string displayName = "MAP";
        public GameModeType supportedGameMode;
        public int seed = 1;
        [Header("Deterministic Pickups")]
        public CoinRouteDefinition coinRoute;

        [Header("Difficulty Bands")]
        [FormerlySerializedAs("difficultyStartDistance"), Min(0f)] public float introEndDistance = 150f;
        [Min(1f)] public float easyEndDistance = 500f;
        [Min(1f)] public float normalEndDistance = 1200f;
        [FormerlySerializedAs("difficultyMaxDistance"), Min(1f)] public float hardEndDistance = 2500f;

        public float EvaluateDifficultyBands(float distance)
        {
            float value = Mathf.Max(0f, distance);
            if (value <= introEndDistance) return 0f;
            if (value <= easyEndDistance)
                return Mathf.Lerp(0f, 0.25f, Mathf.InverseLerp(introEndDistance, easyEndDistance, value));
            if (value <= normalEndDistance)
                return Mathf.Lerp(0.25f, 0.60f, Mathf.InverseLerp(easyEndDistance, normalEndDistance, value));
            if (value <= hardEndDistance)
                return Mathf.Lerp(0.60f, 1f, Mathf.InverseLerp(normalEndDistance, hardEndDistance, value));
            return 1f;
        }

        protected void ClampDifficultyBands()
        {
            introEndDistance = Mathf.Max(0f, introEndDistance);
            easyEndDistance = Mathf.Max(introEndDistance + 1f, easyEndDistance);
            normalEndDistance = Mathf.Max(easyEndDistance + 1f, normalEndDistance);
            hardEndDistance = Mathf.Max(normalEndDistance + 1f, hardEndDistance);
        }
    }
}
