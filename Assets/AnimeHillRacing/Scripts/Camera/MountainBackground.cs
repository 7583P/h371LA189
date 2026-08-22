using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    public sealed class MountainBackground : MonoBehaviour
    {
        [SerializeField] private Camera displayCamera;
        [SerializeField] private EnvironmentDefinition environment;
        [SerializeField] private SpriteRenderer skyRenderer;
        [SerializeField] private SpriteRenderer[] mountainSegments;
        [SerializeField] private SpriteRenderer[] farMountainSegments;
        [SerializeField] private SpriteRenderer[] nearMountainSegments;
        [SerializeField] private SpriteRenderer[] cloudSegments;
        [SerializeField] private SpriteRenderer sunRenderer;
        [SerializeField] private SpriteRenderer earthRenderer;

        private float tileWidth;
        private float farTileWidth;
        private float nearTileWidth;
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
            UpdateSpaceAnimation();
        }

        private void ApplyAssetsAndScale()
        {
            if (displayCamera == null || environment == null) return;
            EnsureCloudPool();
            EnsureMountainPool();
            EnsureFarMountainPool();
            EnsureNearMountainPool();
            EnsureCelestials();
            if (environment.overrideCameraBackground)
            {
                displayCamera.clearFlags = CameraClearFlags.SolidColor;
                displayCamera.backgroundColor = environment.cameraBackgroundColor;
            }
            lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            if (skyRenderer != null)
            {
                skyRenderer.sprite = environment.sky;
                skyRenderer.color = Color.white;
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
                mountainSegments[i].color = Color.white;
                mountainSegments[i].transform.localScale = Vector3.one * scaleMountain;
            }

            float farSourceHeight = environment.farMountains != null
                ? environment.farMountains.bounds.size.y : 1f;
            float farScale = environment.farMountainVisualHeight / Mathf.Max(.001f, farSourceHeight);
            float farSourceWidth = environment.farMountains != null
                ? environment.farMountains.bounds.size.x : 1f;
            farTileWidth = farSourceWidth * farScale * (1f - environment.farMountainSeamOverlap);
            if (farMountainSegments != null)
            {
                for (int i = 0; i < farMountainSegments.Length; i++)
                {
                    if (farMountainSegments[i] == null) continue;
                    farMountainSegments[i].sprite = environment.farMountains;
                    farMountainSegments[i].transform.localScale = Vector3.one * farScale;
                }
            }

            float nearSourceHeight = environment.nearMountains != null
                ? environment.nearMountains.bounds.size.y : 1f;
            float nearScale = environment.nearMountainVisualHeight /
                              Mathf.Max(.001f, nearSourceHeight);
            float nearSourceWidth = environment.nearMountains != null
                ? environment.nearMountains.bounds.size.x : 1f;
            nearTileWidth = nearSourceWidth * nearScale *
                            (1f - environment.nearMountainSeamOverlap);
            if (nearMountainSegments == null) return;
            for (int i = 0; i < nearMountainSegments.Length; i++)
            {
                if (nearMountainSegments[i] == null) continue;
                nearMountainSegments[i].sprite = environment.nearMountains;
                nearMountainSegments[i].transform.localScale = Vector3.one * nearScale;
            }
        }

        private void EnsureCelestials()
        {
            sunRenderer = EnsureCelestialRenderer(sunRenderer, "Space Sun - Pooled", -46);
            earthRenderer = EnsureCelestialRenderer(earthRenderer, "Space Earth - Pooled", -45);
            ConfigureCelestial(sunRenderer, environment.sun, environment.sunVisualWidth);
            ConfigureCelestial(earthRenderer, environment.earth, environment.earthVisualWidth);
            if (sunRenderer != null)
            {
                sunRenderer.color = Color.white;
            }
            if (earthRenderer != null) earthRenderer.color = Color.white;
        }

        private SpriteRenderer EnsureCelestialRenderer(SpriteRenderer renderer, string objectName, int order)
        {
            if (renderer == null)
            {
                Transform child = transform.Find(objectName);
                if (child == null)
                {
                    child = new GameObject(objectName).transform;
                    child.SetParent(transform, false);
                }
                renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void ConfigureCelestial(SpriteRenderer renderer, Sprite sprite, float visualWidth)
        {
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.gameObject.SetActive(sprite != null);
            float sourceWidth = sprite != null ? sprite.bounds.size.x : 1f;
            renderer.transform.localScale = Vector3.one * (visualWidth / Mathf.Max(.001f, sourceWidth));
        }

        private void EnsureNearMountainPool()
        {
            int required = environment.nearMountains != null
                ? Mathf.Clamp(environment.pooledNearMountainSegments, 3, 7) : 0;
            if (nearMountainSegments == null) nearMountainSegments = new SpriteRenderer[0];
            if (nearMountainSegments.Length < required)
            {
                var expanded = new SpriteRenderer[required];
                for (int i = 0; i < nearMountainSegments.Length; i++)
                    expanded[i] = nearMountainSegments[i];
                for (int i = nearMountainSegments.Length; i < required; i++)
                {
                    GameObject segment = new GameObject($"Near Mountain Segment {i + 1} - Pooled");
                    segment.transform.SetParent(transform, false);
                    expanded[i] = segment.AddComponent<SpriteRenderer>();
                    expanded[i].sortingOrder = -35;
                }
                nearMountainSegments = expanded;
            }
            for (int i = 0; i < nearMountainSegments.Length; i++)
                if (nearMountainSegments[i] != null)
                    nearMountainSegments[i].gameObject.SetActive(i < required);
        }

        private void EnsureMountainPool()
        {
            int required = environment.mountains != null
                ? Mathf.Clamp(environment.pooledMountainSegments, 3, 7) : 0;
            if (mountainSegments == null) mountainSegments = new SpriteRenderer[0];
            if (mountainSegments.Length < required)
            {
                var expanded = new SpriteRenderer[required];
                for (int i = 0; i < mountainSegments.Length; i++)
                    expanded[i] = mountainSegments[i];
                for (int i = mountainSegments.Length; i < required; i++)
                {
                    GameObject segment = new GameObject($"Mountain Segment {i + 1} - Pooled");
                    segment.transform.SetParent(transform, false);
                    expanded[i] = segment.AddComponent<SpriteRenderer>();
                    expanded[i].sortingOrder = -50;
                }
                mountainSegments = expanded;
            }
            for (int i = 0; i < mountainSegments.Length; i++)
                if (mountainSegments[i] != null)
                    mountainSegments[i].gameObject.SetActive(i < required);
        }

        private void EnsureFarMountainPool()
        {
            int required = environment.farMountains != null
                ? Mathf.Clamp(environment.pooledFarMountainSegments, 3, 7) : 0;
            if (farMountainSegments == null) farMountainSegments = new SpriteRenderer[0];
            if (farMountainSegments.Length < required)
            {
                var expanded = new SpriteRenderer[required];
                for (int i = 0; i < farMountainSegments.Length; i++) expanded[i] = farMountainSegments[i];
                for (int i = farMountainSegments.Length; i < required; i++)
                {
                    GameObject segment = new GameObject($"Far Mountain Segment {i + 1} - Pooled");
                    segment.transform.SetParent(transform, false);
                    expanded[i] = segment.AddComponent<SpriteRenderer>();
                    expanded[i].sortingOrder = -60;
                }
                farMountainSegments = expanded;
            }
            for (int i = 0; i < farMountainSegments.Length; i++)
                if (farMountainSegments[i] != null)
                    farMountainSegments[i].gameObject.SetActive(i < required);
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

            // A camera-space lock must move the renderer with the camera in world space. This makes
            // its vertical position visually constant while horizontal parallax continues normally.
            float y = environment.coverViewportVertically
                ? cameraPosition.y + environment.coverViewportVerticalOffset
                : CalculateVerticalPosition(environment, cameraPosition.y, initialCameraY);
            UpdatePanoramaPositions(mountainSegments, cameraPosition.x,
                WorldFollowFactor(environment.mountainParallaxX, environment.useRelativeParallaxSpeeds),
                tileWidth, y, 10f, environment.mirrorAlternatingMountainTiles);

            float farY = CalculateLayerVerticalPosition(environment.farMountainBaseY, cameraPosition.y,
                initialCameraY, environment.farMountainParallaxY,
                environment.lockFarMountainsVerticalToCamera);
            UpdatePanoramaPositions(farMountainSegments, cameraPosition.x,
                WorldFollowFactor(environment.farMountainParallaxX, environment.useRelativeParallaxSpeeds),
                farTileWidth, farY, 11f, environment.mirrorAlternatingFarMountainTiles);

            float nearY = CalculateLayerVerticalPosition(environment.nearMountainBaseY, cameraPosition.y,
                initialCameraY, environment.nearMountainParallaxY,
                environment.lockNearMountainsVerticalToCamera);
            UpdatePanoramaPositions(nearMountainSegments, cameraPosition.x,
                WorldFollowFactor(environment.nearMountainParallaxX, environment.useRelativeParallaxSpeeds),
                nearTileWidth, nearY, 9f,
                environment.mirrorAlternatingNearMountainTiles);
            UpdateCloudPositions(cameraPosition);
            UpdateCelestialPosition(sunRenderer, cameraPosition, environment.sunCameraOffset,
                environment.sunRelativeParallax, environment.celestialRepeatSpan, 12.3f);
            UpdateCelestialPosition(earthRenderer, cameraPosition, environment.earthCameraOffset,
                environment.earthRelativeParallax, environment.celestialRepeatSpan, 12.2f);
        }

        private static void UpdateCelestialPosition(SpriteRenderer renderer, Vector3 cameraPosition,
            Vector2 authoredOffset, float relativeParallax, float repeatSpan, float z)
        {
            if (renderer == null || !renderer.gameObject.activeSelf) return;
            double span = System.Math.Max(24.0, repeatSpan);
            double relativeX = authoredOffset.x - (double)cameraPosition.x * Mathf.Clamp01(relativeParallax);
            relativeX -= System.Math.Floor((relativeX + span * .5) / span) * span;
            renderer.transform.position = new Vector3((float)(cameraPosition.x + relativeX),
                cameraPosition.y + authoredOffset.y, z);
        }

        private void UpdateSpaceAnimation()
        {
            if (environment == null || environment.environmentId != "space") return;

            float time = Time.unscaledTime;
            float speed = Mathf.Max(.25f, environment.starTwinkleSpeed);
            float amount = Mathf.Clamp(environment.starTwinkleAmount, 0f, .5f);
            if (skyRenderer != null)
                skyRenderer.color = TwinkleColor(time * speed * .73f + .8f, amount * .7f);
            if (mountainSegments != null)
            {
                for (int i = 0; i < mountainSegments.Length; i++)
                {
                    SpriteRenderer renderer = mountainSegments[i];
                    if (renderer == null || !renderer.gameObject.activeSelf) continue;
                    renderer.color = TwinkleColor(time * speed * (1f + i * .07f) + i * 1.71f, amount);
                }
            }

        }

        private static Color TwinkleColor(float phase, float amount)
        {
            // The fourth power creates short, independent flashes instead of a uniform breathing effect.
            float flash = Mathf.Pow(Mathf.Clamp01(.5f + .5f * Mathf.Sin(phase)), 4f);
            float brightness = 1f - amount + amount * flash;
            return new Color(brightness, brightness, Mathf.Min(1f, brightness + .035f), 1f);
        }

        private static void UpdatePanoramaPositions(SpriteRenderer[] segments, float cameraX,
            float worldFollow, float width, float y, float z, bool mirrorAlternating)
        {
            if (segments == null || segments.Length == 0 || width <= .001f) return;

            // Use doubles for the cell math so a recycled layer remains stable after long teleports.
            // The final Transform is still a float, but no whole tile is selected from rounded float math.
            double originX = (double)cameraX * worldFollow;
            int centerIndex = (int)System.Math.Round(((double)cameraX - originX) / width);
            int half = segments.Length / 2;
            for (int i = 0; i < segments.Length; i++)
            {
                SpriteRenderer renderer = segments[i];
                if (renderer == null || !renderer.gameObject.activeSelf) continue;
                int cell = centerIndex + i - half;
                float x = (float)(originX + cell * (double)width);
                renderer.transform.position = new Vector3(x, y, z);
                // A mirrored panorama joins right-to-right and left-to-left at alternating cells. This
                // keeps one authored background running indefinitely without a visible copy/reset seam.
                renderer.flipX = mirrorAlternating && PositiveModulo(cell, 2) != 0;
            }
        }

        private static float WorldFollowFactor(float configuredSpeed, bool useRelativeSpeed)
        {
            float speed = Mathf.Clamp01(configuredSpeed);
            return useRelativeSpeed ? 1f - speed : speed;
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
            return CalculateLayerVerticalPosition(definition.mountainBaseY, cameraY, cameraStartY,
                definition.mountainParallaxY, definition.lockVerticalToCamera);
        }

        private static float CalculateLayerVerticalPosition(float baseY, float cameraY,
            float cameraStartY, float parallaxY, bool lockToCamera)
        {
            float cameraDelta = cameraY - cameraStartY;
            return baseY + cameraDelta * (lockToCamera ? 1f : parallaxY);
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
