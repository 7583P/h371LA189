using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Terrain Elevation Profile", fileName = "TerrainElevationProfile")]
    public sealed class TerrainElevationProfile : ScriptableObject
    {
        [SerializeField] private MapProfile sourceProfile;

        public float GetHeightAtDistance(float distanceAlongMap)
        {
            return sourceProfile != null ? TerrainGenerator.SampleProfileHeight(sourceProfile, distanceAlongMap) : 0f;
        }

        public void AssignSource(MapProfile profile) => sourceProfile = profile;
    }
}
