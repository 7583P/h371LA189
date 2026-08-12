using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Environment Definition", fileName = "Environment")]
    public sealed class EnvironmentDefinition : ScriptableObject
    {
        public string environmentId = "countryside";
        public Sprite sky;
        public Sprite mountains;
        [Range(.15f, .25f)] public float mountainParallaxX = .20f;
        [Range(.03f, .06f)] public float mountainParallaxY = .04f;
        [Tooltip("Keeps the background at a fixed vertical position in camera space while retaining horizontal parallax.")]
        public bool lockVerticalToCamera;
        [Min(1f)] public float mountainVisualHeight = 17f;
        public float mountainBaseY = -4f;
        [Range(3, 7)] public int pooledMountainSegments = 5;
        [Range(0f, .05f)] public float seamOverlap = .012f;
    }
}
