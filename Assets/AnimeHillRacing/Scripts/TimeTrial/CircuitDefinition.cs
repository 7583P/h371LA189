using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.TimeTrial
{
    public enum CircuitDifficulty { Easy, Normal, Hard }
    public enum CircuitFeatureType
    {
        Straight, FastCurve, MediumCurve, Hairpin, SCurve, ElevationRise, ElevationDrop,
        TechnicalSection, RecoveryStraight, ShortcutOpportunity
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Circuit Definition", fileName = "CircuitDefinition")]
    public sealed class CircuitDefinition : MapDefinition
    {
        [Min(200f)] public float targetLength = 900f;
        [Min(5f)] public float roadWidth = 10f;
        [Range(64, 512)] public int samples = 192;
        [Range(1, 10)] public int laps = 1;
        public bool fuelEnabled;
        public CircuitDifficulty difficulty = CircuitDifficulty.Normal;
        [Range(0f, 1f)] public float shortcutChance = 0.65f;
        [Range(0f, 1f)] public float techChance = 0.18f;
        [Min(0f)] public float respawnPenaltySeconds = 3f;
        public Material roadMaterial;
        public Material shortcutMaterial;

        private void OnValidate()
        {
            supportedGameMode = GameModeType.TimeTrial3D;
            laps = 1;
            samples = Mathf.Max(64, samples);
        }
    }
}
