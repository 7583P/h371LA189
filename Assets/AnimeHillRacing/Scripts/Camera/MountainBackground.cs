using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    public sealed class MountainBackground : MonoBehaviour
    {
        [SerializeField] private Camera displayCamera;
        [SerializeField] private EnvironmentDefinition environment;
        [SerializeField] private SpriteRenderer skyRenderer;
        [SerializeField] private SpriteRenderer[] mountainSegments;

        private float tileWidth;
        private float initialCameraY;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Start()
        {
            if (displayCamera == null) displayCamera = Camera.main;
            initialCameraY = displayCamera != null ? displayCamera.transform.position.y : 0f;
            ApplyAssetsAndScale();
            UpdatePositions();
        }

        private void LateUpdate()
        {
            if (displayCamera == null || environment == null) return;
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight) ApplyAssetsAndScale();
            UpdatePositions();
        }

        private void ApplyAssetsAndScale()
        {
            if (displayCamera == null || environment == null) return;
            lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            if (skyRenderer != null)
            {
                skyRenderer.sprite = environment.sky;
                float sourceWidth = environment.sky != null ? environment.sky.bounds.size.x : 1f;
                float sourceHeight = environment.sky != null ? environment.sky.bounds.size.y : 1f;
                float viewHeight = displayCamera.orthographicSize * 2.08f;
                float viewWidth = viewHeight * displayCamera.aspect;
                float scale = Mathf.Max(viewWidth / Mathf.Max(.001f, sourceWidth),
                    viewHeight / Mathf.Max(.001f, sourceHeight));
                skyRenderer.transform.localScale = Vector3.one * scale;
            }
            float mountainSourceHeight = environment.mountains != null ? environment.mountains.bounds.size.y : 1f;
            float scaleMountain = environment.mountainVisualHeight / Mathf.Max(.001f, mountainSourceHeight);
            float sourceWidthMountain = environment.mountains != null ? environment.mountains.bounds.size.x : 1f;
            tileWidth = sourceWidthMountain * scaleMountain * (1f - environment.seamOverlap);
            if (mountainSegments == null) return;
            for (int i = 0; i < mountainSegments.Length; i++)
            {
                if (mountainSegments[i] == null) continue;
                mountainSegments[i].sprite = environment.mountains;
                mountainSegments[i].transform.localScale = Vector3.one * scaleMountain;
            }
        }

        private void UpdatePositions()
        {
            if (displayCamera == null || environment == null || tileWidth <= .001f) return;
            Vector3 cameraPosition = displayCamera.transform.position;
            if (skyRenderer != null)
                skyRenderer.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 14f);

            float parallaxOriginX = cameraPosition.x * environment.mountainParallaxX;
            int centerIndex = Mathf.RoundToInt((cameraPosition.x - parallaxOriginX) / tileWidth);
            float y = environment.mountainBaseY + (cameraPosition.y - initialCameraY) * environment.mountainParallaxY;
            int count = mountainSegments != null ? mountainSegments.Length : 0;
            int half = count / 2;
            for (int i = 0; i < count; i++)
            {
                if (mountainSegments[i] == null) continue;
                float x = parallaxOriginX + (centerIndex + i - half) * tileWidth;
                mountainSegments[i].transform.position = new Vector3(x, y, 10f);
            }
        }

        public void Assign(Camera camera, EnvironmentDefinition definition, SpriteRenderer sky,
            SpriteRenderer[] segments)
        {
            displayCamera = camera; environment = definition; skyRenderer = sky; mountainSegments = segments;
        }

        public void ApplyEnvironment(EnvironmentDefinition definition)
        {
            if (definition == null) return;
            environment = definition;
            ApplyAssetsAndScale();
            UpdatePositions();
        }
    }
}
