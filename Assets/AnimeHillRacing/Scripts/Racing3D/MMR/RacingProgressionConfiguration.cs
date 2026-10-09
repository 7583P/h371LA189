using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Racing 3D/Local Progression Configuration")]
    public sealed class RacingProgressionConfiguration : ScriptableObject
    {
        public LocalLeagueConfiguration league = new LocalLeagueConfiguration();
        public TitleDefinition[] titles;
    }
}
