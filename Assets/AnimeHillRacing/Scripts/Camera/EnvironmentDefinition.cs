using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Environment Definition", fileName = "Environment")]
    public sealed class EnvironmentDefinition : ScriptableObject
    {
        public string environmentId = "countryside";
        public Sprite sky;
        public Sprite mountains;
        [Header("Camera Backdrop")]
        public bool overrideCameraBackground;
        public Color cameraBackgroundColor = new Color(.005f, .004f, .012f, 1f);
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
        [Tooltip("Vertical camera-space offset used when the panorama covers the viewport.")]
        public float coverViewportVerticalOffset;
        [Min(1f)] public float mountainVisualHeight = 17f;
        public float mountainBaseY = -4f;
        [Range(1, 7)] public int pooledMountainSegments = 5;
        [Range(0f, .05f)] public float seamOverlap = .012f;
        [Tooltip("Mirrors alternating panorama cells so consecutive edges match without an obvious copy seam.")]
        public bool mirrorAlternatingMountainTiles;

        [Header("Optional Far Panorama Layer")]
        [Tooltip("A faint panorama behind the main mountains. Useful for three-depth parallax environments.")]
        public Sprite farMountains;
        [Range(.01f, .20f)] public float farMountainParallaxX = .08f;
        [Range(0f, .08f)] public float farMountainParallaxY = .025f;
        public bool lockFarMountainsVerticalToCamera = true;
        [Min(1f)] public float farMountainVisualHeight = 18f;
        public float farMountainBaseY = -1f;
        [Range(3, 7)] public int pooledFarMountainSegments = 5;
        [Range(0f, .05f)] public float farMountainSeamOverlap = .01f;
        public bool mirrorAlternatingFarMountainTiles = true;

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

        [Header("Optional Space Celestials")]
        public Sprite sun;
        public Sprite earth;
        public Vector2 sunCameraOffset = new Vector2(-1f, 3.5f);
        public Vector2 earthCameraOffset = new Vector2(7f, 2.2f);
        [Range(0f, .05f)] public float sunRelativeParallax = .003f;
        [Range(0f, .08f)] public float earthRelativeParallax = .009f;
        [Min(2f)] public float sunVisualWidth = 6.5f;
        [Min(1f)] public float earthVisualWidth = 5.2f;
        [Min(24f)] public float celestialRepeatSpan = 72f;

        [Header("Optional Space Star Animation")]
        [Range(.25f, 6f)] public float starTwinkleSpeed = 2.15f;
        [Range(0f, .5f)] public float starTwinkleAmount = .26f;

        [Header("Optional Cinematic Space Panorama")]
        [Tooltip("Uses a camera-locked panorama that scrolls continuously and recycles beyond the screen edges.")]
        public bool useCinematicSpacePanorama;
        public Shader cinematicSpacePanoramaShader;
        [Tooltip("Adds a small safe overscan after covering the camera vertically.")]
        [Range(1f, 1.12f)] public float spacePanoramaOverscan = 1.04f;
        [Tooltip("Horizontal world units travelled per second. The panorama wraps seamlessly at its edges.")]
        [Range(.01f, .5f)] public float spacePanoramaScrollSpeed = .075f;
        [Tooltip("Mirrors alternating copies so adjacent image edges join without a hard reset seam.")]
        public bool mirrorSpacePanoramaTiles = true;
        [Range(0f, .01f)] public float spaceNebulaFlowStrength = .0018f;
        [Range(.01f, .5f)] public float spaceNebulaFlowSpeed = .075f;
        [Range(0f, .2f)] public float spaceNebulaPulse = .055f;

        [Header("Optional Space Shooting Stars")]
        public bool enableShootingStars;
        public Vector2 shootingStarIntervalRange = new Vector2(2.1f, 4.8f);
        public Vector2 shootingStarSpeedRange = new Vector2(15f, 23f);
        [Range(.2f, 1.2f)] public float shootingStarTrailSeconds = .58f;
        [Range(1, 5)] public int shootingStarPoolSize = 3;
    }
}
