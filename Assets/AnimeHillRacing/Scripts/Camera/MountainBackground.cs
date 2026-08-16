using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    public sealed class MountainBackground : MonoBehaviour
    {
        [SerializeField] private Camera displayCamera;
        [SerializeField] private EnvironmentDefinition environment;
        [SerializeField] private SpriteRenderer skyRenderer;
        [SerializeField] private SpriteRenderer[] mountainSegments;
        [SerializeField] private SpriteRenderer[] cloudSegments;

        private float tileWidth;
        private float initialCameraY;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private int[] cloudCells;

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
            EnsureCloudPool();
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
            float targetMountainHeight = environment.coverViewportVertically
                ? Mathf.Max(environment.mountainVisualHeight, displayCamera.orthographicSize * 2.12f)
                : environment.mountainVisualHeight;
            float scaleMountain = targetMountainHeight / Mathf.Max(.001f, mountainSourceHeight);
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

        private void EnsureCloudPool()
        {
            int required = environment.clouds != null && environment.clouds.Length > 0
                ? Mathf.Clamp(environment.pooledClouds, 4, 12) : 0;
            if (cloudSegments == null) cloudSegments = new SpriteRenderer[0];
            if (cloudSegments.Length < required)
            {
                var expanded = new SpriteRenderer[required];
                for (int i = 0; i < cloudSegments.Length; i++) expanded[i] = cloudSegments[i];
                for (int i = cloudSegments.Length; i < required; i++)
                {
                    GameObject cloud = new GameObject($"City Cloud {i + 1} - Pooled");
                    cloud.transform.SetParent(transform, false);
                    expanded[i] = cloud.AddComponent<SpriteRenderer>();
                    expanded[i].sortingOrder = -75;
                }
                cloudSegments = expanded;
            }
            if (cloudCells == null || cloudCells.Length != cloudSegments.Length)
            {
                cloudCells = new int[cloudSegments.Length];
                for (int i = 0; i < cloudCells.Length; i++) cloudCells[i] = int.MinValue;
            }
            for (int i = 0; i < cloudSegments.Length; i++)
                if (cloudSegments[i] != null)
                    cloudSegments[i].gameObject.SetActive(i < required);
        }

        private void UpdatePositions()
        {
            if (displayCamera == null || environment == null || tileWidth <= .001f) return;
            Vector3 cameraPosition = displayCamera.transform.position;
            if (skyRenderer != null)
                skyRenderer.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 14f);

            float parallaxOriginX = cameraPosition.x * environment.mountainParallaxX;
            int centerIndex = Mathf.RoundToInt((cameraPosition.x - parallaxOriginX) / tileWidth);
            // A camera-space lock must move the renderer with the camera in world space. This makes
            // its vertical position visually constant while horizontal parallax continues normally.
            float y = environment.coverViewportVertically
                ? cameraPosition.y : CalculateVerticalPosition(environment, cameraPosition.y, initialCameraY);
            int count = mountainSegments != null ? mountainSegments.Length : 0;
            int half = count / 2;
            for (int i = 0; i < count; i++)
            {
                if (mountainSegments[i] == null) continue;
                int cell = centerIndex + i - half;
                float x = parallaxOriginX + cell * tileWidth;
                mountainSegments[i].transform.position = new Vector3(x, y, 10f);
                // A mirrored panorama joins right-to-right and left-to-left at alternating cells. This
                // keeps one authored background running indefinitely without a visible copy/reset seam.
                mountainSegments[i].flipX = environment.mirrorAlternatingMountainTiles &&
                                             PositiveModulo(cell, 2) != 0;
            }
            UpdateCloudPositions(cameraPosition);
        }

        private void UpdateCloudPositions(Vector3 cameraPosition)
        {
            if (cloudSegments == null || environment.clouds == null || environment.clouds.Length == 0) return;
            float spacing = Mathf.Max(8f, environment.cloudSpacing);
            float referenceDrift = Time.time * AverageRange(environment.cloudDriftSpeedRange, .14f);
            float referenceParallax = Mathf.Clamp(environment.cloudParallaxX, .04f, .18f);
            float parallaxOriginX = cameraPosition.x * referenceParallax;
            int centerCell = Mathf.RoundToInt((cameraPosition.x - parallaxOriginX - referenceDrift) / spacing);
            int activeCount = Mathf.Min(Mathf.Clamp(environment.pooledClouds, 4, 12), cloudSegments.Length);
            int half = activeCount / 2;
            for (int offset = 0; offset < activeCount; offset++)
            {
                int cell = centerCell + offset - half;
                // A stable cell-to-slot mapping recycles only the cloud that left the screen. Mapping
                // renderer i directly to centerCell+i would make the entire sky jump one spacing at once.
                int slot = PositiveModulo(cell, activeCount);
                SpriteRenderer renderer = cloudSegments[slot];
                if (renderer == null) continue;
                float depth = Hash01(cell * 401 + 211);
                float cloudParallax = Mathf.Lerp(referenceParallax * .58f, referenceParallax * 1.28f, depth);
                float driftSpeed = LerpRange(environment.cloudDriftSpeedRange, depth, .08f, .22f);
                float horizontalJitter = (Hash01(cell * 97 + 31) * 2f - 1f) *
                                         Mathf.Max(0f, environment.cloudSpacingJitter);
                // Far clouds sit higher, move more slowly and remain slightly fainter. Near clouds are
                // larger and drift independently, avoiding the rigid conveyor-belt look of one layer.
                float randomHeight = Hash01(cell * 193 + 71);
                float heightBlend = Mathf.Clamp01(Mathf.Lerp(.72f, .22f, depth) + (randomHeight - .5f) * .56f);
                float height = Mathf.Lerp(environment.cloudHeightRange.x, environment.cloudHeightRange.y,
                    heightBlend);
                float bobPeriod = Mathf.Max(8f, LerpRange(environment.cloudBobPeriodRange,
                    Hash01(cell * 149 + 17), 18f, 32f));
                float bob = Mathf.Sin(Time.time * TwoPi / bobPeriod + Hash01(cell * 509 + 89) * TwoPi) *
                            environment.cloudBobAmplitude * Mathf.Lerp(.55f, 1f, depth);
                float x = cameraPosition.x * cloudParallax + cell * spacing + horizontalJitter +
                          Time.time * driftSpeed;
                renderer.transform.position = new Vector3(x, cameraPosition.y + height + bob,
                    Mathf.Lerp(12.8f, 11.4f, depth));
                if (cloudCells[slot] == cell) continue;
                cloudCells[slot] = cell;
                // Seven is coprime with the 15 authored City clouds, so the sequence visits every cutout
                // before repeating instead of cycling through only a small subset.
                int spriteIndex = PositiveModulo(cell * 7 + 3, environment.clouds.Length);
                Sprite sprite = environment.clouds[spriteIndex];
                renderer.sprite = sprite;
                float targetWidth = Mathf.Lerp(environment.cloudVisualWidthRange.x,
                    environment.cloudVisualWidthRange.y, Hash01(cell * 277 + 113)) *
                                    Mathf.Lerp(.72f, 1.08f, depth);
                float sourceWidth = sprite != null ? sprite.bounds.size.x : 1f;
                renderer.transform.localScale = Vector3.one * (targetWidth / Mathf.Max(.001f, sourceWidth));
                float opacity = LerpRange(environment.cloudOpacityRange, depth, .58f, .9f);
                renderer.color = new Color(1f, 1f, 1f, opacity);
                renderer.flipX = Hash01(cell * 733 + 23) > .5f;
                renderer.sortingOrder = depth < .5f ? -82 : -72;
            }
        }

        private const float TwoPi = Mathf.PI * 2f;

        private static float AverageRange(Vector2 range, float fallback)
        {
            if (range.x <= 0f && range.y <= 0f) return fallback;
            return Mathf.Max(0f, (range.x + range.y) * .5f);
        }

        private static float LerpRange(Vector2 range, float t, float fallbackMin, float fallbackMax)
        {
            if (range.x <= 0f && range.y <= 0f) return Mathf.Lerp(fallbackMin, fallbackMax, t);
            float minimum = Mathf.Min(range.x, range.y);
            float maximum = Mathf.Max(range.x, range.y);
            return Mathf.Lerp(minimum, maximum, t);
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                uint x = (uint)value;
                x ^= x >> 16;
                x *= 0x7feb352du;
                x ^= x >> 15;
                x *= 0x846ca68bu;
                x ^= x >> 16;
                return (x & 0x00ffffffu) / 16777215f;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        public static float CalculateVerticalPosition(EnvironmentDefinition definition,
            float cameraY, float cameraStartY)
        {
            if (definition == null) return 0f;
            float cameraDelta = cameraY - cameraStartY;
            return definition.mountainBaseY + cameraDelta *
                   (definition.lockVerticalToCamera ? 1f : definition.mountainParallaxY);
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
