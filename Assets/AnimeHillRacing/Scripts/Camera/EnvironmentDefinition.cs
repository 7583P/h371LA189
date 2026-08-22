using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Environment Definition", fileName = "Environment")]
    public sealed class EnvironmentDefinition : ScriptableObject
    {
        public string environmentId = "countryside";
        public Sprite sky;
        public Sprite mountains;
        [Header("Optional Cloud Layer")]
        public Sprite[] clouds;
        [Range(.04f, .18f)] public float cloudParallaxX = .09f;
        [Min(8f)] public float cloudSpacing = 18f;
        [Min(0f)] public float cloudSpacingJitter = 4f;
        public Vector2 cloudHeightRange = new Vector2(3.2f, 8.5f);
        public Vector2 cloudVisualWidthRange = new Vector2(3.2f, 6.8f);
        [Tooltip("Slow world-space drift keeps the cloud layer alive even while the camera is stationary.")]
        public Vector2 cloudDriftSpeedRange = new Vector2(.08f, .22f);
        public Vector2 cloudOpacityRange = new Vector2(.58f, .9f);
        [Range(0f, .6f)] public float cloudBobAmplitude = .18f;
        public Vector2 cloudBobPeriodRange = new Vector2(18f, 32f);
        [Range(4, 12)] public int pooledClouds = 8;
        [Range(.01f, .25f)] public float mountainParallaxX = .20f;
        [Range(0f, .08f)] public float mountainParallaxY = .04f;
        [Tooltip("When enabled, horizontal values represent visible screen motion: 0 is fixed to the camera and 1 moves like terrain. Existing environments keep legacy world-follow semantics.")]
        public bool useRelativeParallaxSpeeds;
        [Tooltip("Keeps the background at a fixed vertical position in camera space while retaining horizontal parallax.")]
        public bool lockVerticalToCamera;
        [Tooltip("Scales and centers an opaque panorama so its outer edge remains beyond the camera viewport.")]
        public bool coverViewportVertically;
        [Min(1f)] public float mountainVisualHeight = 17f;
        public float mountainBaseY = -4f;
        [Range(3, 7)] public int pooledMountainSegments = 5;
        [Range(0f, .05f)] public float seamOverlap = .012f;
        [Tooltip("Mirrors alternating panorama cells so consecutive edges match without an obvious copy seam.")]
        public bool mirrorAlternatingMountainTiles;

        [Header("Optional Near Panorama Layer")]
        [Tooltip("An additional transparent panorama rendered in front of the main mountains.")]
        public Sprite nearMountains;
        [Range(.02f, .30f)] public float nearMountainParallaxX = .11f;
        [Range(0f, .10f)] public float nearMountainParallaxY = .05f;
        public bool lockNearMountainsVerticalToCamera = true;
        [Min(1f)] public float nearMountainVisualHeight = 16f;
        public float nearMountainBaseY = -2f;
        [Range(3, 7)] public int pooledNearMountainSegments = 5;
        [Range(0f, .05f)] public float nearMountainSeamOverlap = .01f;
        public bool mirrorAlternatingNearMountainTiles = true;
    }
}
