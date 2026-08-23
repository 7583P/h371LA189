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

        private TrailRenderer[] shootingStars;
        private SpriteRenderer[] shootingStarHeads;
        private Vector3[] shootingStarVelocities;
        private Vector3[] shootingStarAccelerations;
        private float[] shootingStarStartTimes;
        private float[] shootingStarStopTimes;
        private float[] shootingStarDisableTimes;
        private float[] shootingStarHeadScales;
        private Color[] shootingStarHeadColors;
        private Material shootingStarMaterial;
        private Material spacePanoramaMaterial;
        private float nextShootingStarAt;
        private int shootingStarSequence;

        private const int TwinklingStarCount = 56;
        private SpriteRenderer[] twinklingStars;
        private Vector2[] twinklingStarViewportPositions;
        private float[] twinklingStarPhases;
        private float[] twinklingStarSpeeds;
        private float[] twinklingStarBaseScales;
        private Color[] twinklingStarColors;
        private Texture2D twinklingStarTexture;
        private Sprite twinklingStarSprite;

        private float tileWidth;
        private float spacePanoramaBaseScale;
        private float spacePanoramaWidth;
        private float spacePanoramaHeight;
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
            EnsureTwinklingStarPool();
            EnsureShootingStarPool();
            EnsureCinematicSpaceMaterial();
            if (environment.overrideCameraBackground)
            {
                displayCamera.clearFlags = CameraClearFlags.SolidColor;
                displayCamera.backgroundColor = environment.cameraBackgroundColor;
            }
            lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            if (skyRenderer != null)
            {
                skyRenderer.gameObject.SetActive(environment.sky != null);
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
            float sourceWidthMountain = environment.mountains != null ? environment.mountains.bounds.size.x : 1f;
            float viewHeightMountain = displayCamera.orthographicSize * 2f;
            float viewWidthMountain = viewHeightMountain * displayCamera.aspect;
            float scaleMountain;
            if (UsesCinematicSpacePanorama)
            {
                float coverScale = Mathf.Max(viewWidthMountain / Mathf.Max(.001f, sourceWidthMountain),
                    viewHeightMountain / Mathf.Max(.001f, mountainSourceHeight));
                scaleMountain = coverScale * Mathf.Clamp(environment.spacePanoramaOverscan, 1f, 1.12f);
                spacePanoramaBaseScale = scaleMountain;
                spacePanoramaWidth = sourceWidthMountain * scaleMountain;
                spacePanoramaHeight = mountainSourceHeight * scaleMountain;
            }
            else
            {
                float targetMountainHeight = environment.coverViewportVertically
                    ? Mathf.Max(environment.mountainVisualHeight, displayCamera.orthographicSize * 2.12f)
                    : environment.mountainVisualHeight;
                scaleMountain = targetMountainHeight / Mathf.Max(.001f, mountainSourceHeight);
                spacePanoramaBaseScale = spacePanoramaWidth = spacePanoramaHeight = 0f;
            }
            tileWidth = sourceWidthMountain * scaleMountain * (1f - environment.seamOverlap);
            if (mountainSegments == null) return;
            for (int i = 0; i < mountainSegments.Length; i++)
            {
                if (mountainSegments[i] == null) continue;
                mountainSegments[i].sprite = environment.mountains;
                mountainSegments[i].color = Color.white;
                mountainSegments[i].transform.localScale = Vector3.one * scaleMountain;
                if (UsesCinematicSpacePanorama && spacePanoramaMaterial != null)
                    mountainSegments[i].sharedMaterial = spacePanoramaMaterial;
                else if (mountainSegments[i].sharedMaterial == spacePanoramaMaterial)
                    mountainSegments[i].sharedMaterial = null;
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

        private bool UsesCinematicSpacePanorama => environment != null &&
            environment.environmentId == "space" && environment.useCinematicSpacePanorama;

        private void EnsureCinematicSpaceMaterial()
        {
            if (!UsesCinematicSpacePanorama)
            {
                if (spacePanoramaMaterial != null)
                {
                    if (mountainSegments != null)
                    {
                        for (int i = 0; i < mountainSegments.Length; i++)
                            if (mountainSegments[i] != null &&
                                mountainSegments[i].sharedMaterial == spacePanoramaMaterial)
                                mountainSegments[i].sharedMaterial = null;
                    }
                    DestroyRuntimeObject(spacePanoramaMaterial);
                }
                spacePanoramaMaterial = null;
                return;
            }

            if (spacePanoramaMaterial == null)
            {
                Shader shader = environment.cinematicSpacePanoramaShader != null
                    ? environment.cinematicSpacePanoramaShader
                    : Shader.Find("AnimeHillRacing/Cinematic Space Panorama");
                if (shader != null)
                    spacePanoramaMaterial = new Material(shader) { name = "Runtime Cinematic Space Panorama" };
            }
            if (spacePanoramaMaterial == null) return;
            spacePanoramaMaterial.SetFloat("_FlowStrength", environment.spaceNebulaFlowStrength);
            spacePanoramaMaterial.SetFloat("_FlowSpeed", environment.spaceNebulaFlowSpeed);
            spacePanoramaMaterial.SetFloat("_PulseAmount", environment.spaceNebulaPulse);
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

        private void EnsureShootingStarPool()
        {
            bool enabledForEnvironment = environment != null && environment.environmentId == "space" &&
                                         environment.enableShootingStars;
            int required = enabledForEnvironment
                ? Mathf.Clamp(environment.shootingStarPoolSize, 1, 5) : 0;
            if (shootingStars != null && shootingStars.Length != required)
            {
                for (int i = 0; i < shootingStars.Length; i++)
                    if (shootingStars[i] != null) DestroyRuntimeObject(shootingStars[i].gameObject);
                shootingStars = null;
            }

            if (required == 0)
            {
                if (shootingStarMaterial != null) DestroyRuntimeObject(shootingStarMaterial);
                shootingStarMaterial = null;
                return;
            }

            if (shootingStarMaterial == null)
            {
                Shader shader = Shader.Find("Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    shootingStarMaterial = new Material(shader) { name = "Runtime Shooting Star Trail" };
            }
            if (shootingStars != null) return;

            shootingStars = new TrailRenderer[required];
            shootingStarHeads = new SpriteRenderer[required];
            shootingStarVelocities = new Vector3[required];
            shootingStarAccelerations = new Vector3[required];
            shootingStarStartTimes = new float[required];
            shootingStarStopTimes = new float[required];
            shootingStarDisableTimes = new float[required];
            shootingStarHeadScales = new float[required];
            shootingStarHeadColors = new Color[required];
            Gradient trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(.12f, .32f, .72f), 0f),
                    new GradientColorKey(new Color(.48f, .78f, 1f), .58f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(.16f, .28f),
                    new GradientAlphaKey(.58f, .76f),
                    new GradientAlphaKey(1f, 1f)
                });
            AnimationCurve widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(.18f, .12f), new Keyframe(.72f, .38f),
                new Keyframe(1f, 1f));

            CreateTwinklingStarSprite();

            for (int i = 0; i < required; i++)
            {
                GameObject star = new GameObject($"Space Shooting Star {i + 1} - Pooled");
                star.transform.SetParent(transform, false);
                TrailRenderer trail = star.AddComponent<TrailRenderer>();
                trail.sharedMaterial = shootingStarMaterial;
                trail.sortingLayerID = skyRenderer != null ? skyRenderer.sortingLayerID : 0;
                trail.sortingOrder = -47;
                trail.alignment = LineAlignment.View;
                trail.textureMode = LineTextureMode.Stretch;
                trail.numCornerVertices = 5;
                trail.numCapVertices = 6;
                trail.time = Mathf.Clamp(environment.shootingStarTrailSeconds, .2f, 1.2f);
                trail.widthMultiplier = .075f;
                trail.widthCurve = widthCurve;
                trail.colorGradient = trailGradient;
                trail.minVertexDistance = .035f;
                trail.emitting = false;

                GameObject headObject = new GameObject("Atmospheric Comet Head");
                headObject.transform.SetParent(star.transform, false);
                SpriteRenderer head = headObject.AddComponent<SpriteRenderer>();
                head.sprite = twinklingStarSprite;
                head.sortingLayerID = trail.sortingLayerID;
                head.sortingOrder = -46;
                head.color = Color.clear;
                shootingStarHeads[i] = head;
                star.SetActive(false);
                shootingStars[i] = trail;
            }

            shootingStarSequence = 0;
            nextShootingStarAt = Time.unscaledTime + .75f;
        }

        private void EnsureTwinklingStarPool()
        {
            bool enabledForEnvironment = environment != null && environment.environmentId == "space" &&
                                         environment.starTwinkleAmount > .001f;
            int required = enabledForEnvironment ? TwinklingStarCount : 0;
            if (twinklingStars != null && twinklingStars.Length != required)
            {
                for (int i = 0; i < twinklingStars.Length; i++)
                    if (twinklingStars[i] != null) DestroyRuntimeObject(twinklingStars[i].gameObject);
                twinklingStars = null;
            }

            if (required == 0)
            {
                DestroyTwinklingStarAssets();
                return;
            }
            if (twinklingStars != null) return;

            CreateTwinklingStarSprite();
            twinklingStars = new SpriteRenderer[required];
            twinklingStarViewportPositions = new Vector2[required];
            twinklingStarPhases = new float[required];
            twinklingStarSpeeds = new float[required];
            twinklingStarBaseScales = new float[required];
            twinklingStarColors = new Color[required];

            Color[] palette =
            {
                Color.white,
                new Color(.64f, .82f, 1f, 1f),
                new Color(1f, .82f, .54f, 1f)
            };
            for (int i = 0; i < required; i++)
            {
                GameObject star = new GameObject($"Space Twinkling Star {i + 1} - Pooled");
                star.transform.SetParent(transform, false);
                SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
                renderer.sprite = twinklingStarSprite;
                renderer.sortingLayerID = skyRenderer != null ? skyRenderer.sortingLayerID : 0;
                renderer.sortingOrder = -48;
                twinklingStars[i] = renderer;
                twinklingStarViewportPositions[i] = new Vector2(
                    Mathf.Lerp(.025f, .975f, Hash01(i * 277 + 31)),
                    Mathf.Lerp(.08f, .96f, Hash01(i * 419 + 73)));
                twinklingStarPhases[i] = Hash01(i * 613 + 109) * Mathf.PI * 2f;
                twinklingStarSpeeds[i] = Mathf.Lerp(.72f, 2.45f, Hash01(i * 181 + 151));
                twinklingStarBaseScales[i] = Mathf.Lerp(.018f, .068f, Hash01(i * 331 + 197));
                twinklingStarColors[i] = palette[PositiveModulo(i * 5 + 1, palette.Length)];
            }
        }

        private void CreateTwinklingStarSprite()
        {
            if (twinklingStarSprite != null) return;
            const int size = 16;
            twinklingStarTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Space Twinkling Star",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float center = (size - 1) * .5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                float radial = Mathf.Exp(-(dx * dx + dy * dy) * .34f);
                float horizontalRay = Mathf.Exp(-dy * dy * 1.5f) * Mathf.Exp(-dx * .34f) * .72f;
                float verticalRay = Mathf.Exp(-dx * dx * 1.5f) * Mathf.Exp(-dy * .34f) * .72f;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(
                    Mathf.Max(radial, Mathf.Max(horizontalRay, verticalRay))) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            twinklingStarTexture.SetPixels32(pixels);
            twinklingStarTexture.Apply(false, true);
            twinklingStarSprite = Sprite.Create(twinklingStarTexture, new Rect(0f, 0f, size, size),
                new Vector2(.5f, .5f), size);
            twinklingStarSprite.name = "Runtime Space Twinkling Star";
        }

        private void DestroyTwinklingStarAssets()
        {
            if (twinklingStarSprite != null) DestroyRuntimeObject(twinklingStarSprite);
            if (twinklingStarTexture != null) DestroyRuntimeObject(twinklingStarTexture);
            twinklingStarSprite = null;
            twinklingStarTexture = null;
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
                ? UsesCinematicSpacePanorama ? 1 : Mathf.Clamp(environment.pooledMountainSegments, 3, 7)
                : 0;
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
            if (UsesCinematicSpacePanorama)
                UpdateCinematicSpacePanorama(cameraPosition, y);
            else
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
            UpdateTwinklingStarPositions(cameraPosition);
        }

        private void UpdateCinematicSpacePanorama(Vector3 cameraPosition, float baseY)
        {
            if (mountainSegments == null || mountainSegments.Length == 0 || mountainSegments[0] == null) return;
            SpriteRenderer panorama = mountainSegments[0];
            float viewHeight = displayCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * displayCamera.aspect;
            float availableX = Mathf.Max(0f, (spacePanoramaWidth - viewWidth) * .5f - .04f);
            float period = Mathf.Max(20f, environment.spacePanoramaDriftPeriod);
            float phase = Time.unscaledTime * TwoPi / period;
            float driftX = Mathf.Sin(phase) * Mathf.Min(availableX,
                Mathf.Abs(environment.spacePanoramaDriftAmplitude.x));
            panorama.transform.position = new Vector3(cameraPosition.x + driftX, baseY, 10f);
            panorama.transform.localScale = Vector3.one * spacePanoramaBaseScale;
            panorama.flipX = false;
        }

        private void UpdateTwinklingStarPositions(Vector3 cameraPosition)
        {
            if (twinklingStars == null || displayCamera == null) return;
            float viewHeight = displayCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * displayCamera.aspect;
            float time = Time.unscaledTime;
            for (int i = 0; i < twinklingStars.Length; i++)
            {
                SpriteRenderer renderer = twinklingStars[i];
                if (renderer == null) continue;
                Vector2 viewport = twinklingStarViewportPositions[i];
                float driftPhase = time * Mathf.Lerp(.018f, .045f, Hash01(i * 571 + 43)) +
                                   twinklingStarPhases[i];
                float driftX = Mathf.Sin(driftPhase) * viewWidth * .0018f;
                float driftY = Mathf.Cos(driftPhase * .73f) * viewHeight * .0014f;
                renderer.transform.position = new Vector3(
                    cameraPosition.x + (viewport.x - .5f) * viewWidth + driftX,
                    cameraPosition.y + (viewport.y - .5f) * viewHeight + driftY,
                    12f);
            }
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
            if (skyRenderer != null) skyRenderer.color = Color.white;
            if (mountainSegments != null)
            {
                for (int i = 0; i < mountainSegments.Length; i++)
                {
                    SpriteRenderer renderer = mountainSegments[i];
                    if (renderer == null || !renderer.gameObject.activeSelf) continue;
                    renderer.color = Color.white;
                }
            }
            UpdateTwinklingStars(time, speed, amount);
            UpdateShootingStars(time, Time.unscaledDeltaTime);
        }

        private void UpdateTwinklingStars(float time, float speed, float amount)
        {
            if (twinklingStars == null) return;
            float animationBlend = Mathf.InverseLerp(0f, .5f, amount);
            for (int i = 0; i < twinklingStars.Length; i++)
            {
                SpriteRenderer renderer = twinklingStars[i];
                if (renderer == null) continue;
                float phase = time * speed * twinklingStarSpeeds[i] + twinklingStarPhases[i];
                float primary = Mathf.Pow(Mathf.Clamp01(.5f + .5f * Mathf.Sin(phase)), 5f);
                float secondary = Mathf.Clamp01(.5f + .5f * Mathf.Sin(phase * .37f + i * 2.17f));
                float flash = Mathf.Clamp01(primary * .82f + secondary * .18f);
                Color tint = twinklingStarColors[i];
                tint.a = Mathf.Lerp(.78f, Mathf.Lerp(.16f, 1f, flash), animationBlend);
                renderer.color = tint;
                float scale = twinklingStarBaseScales[i] * Mathf.Lerp(.82f, 1.28f, flash);
                renderer.transform.localScale = Vector3.one * scale;
            }
        }

        private void UpdateShootingStars(float time, float deltaTime)
        {
            if (!environment.enableShootingStars || shootingStars == null || displayCamera == null) return;

            for (int i = 0; i < shootingStars.Length; i++)
            {
                TrailRenderer trail = shootingStars[i];
                if (trail == null || !trail.gameObject.activeSelf) continue;
                if (time < shootingStarStopTimes[i])
                {
                    shootingStarVelocities[i] += shootingStarAccelerations[i] * deltaTime;
                    trail.transform.position += shootingStarVelocities[i] * deltaTime;
                }
                else
                    trail.emitting = false;

                SpriteRenderer head = shootingStarHeads != null ? shootingStarHeads[i] : null;
                if (head != null)
                {
                    float flight = Mathf.InverseLerp(shootingStarStartTimes[i], shootingStarStopTimes[i], time);
                    float fade = time <= shootingStarStopTimes[i]
                        ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(flight * 8f))
                        : 1f - Mathf.InverseLerp(shootingStarStopTimes[i], shootingStarDisableTimes[i], time);
                    float atmosphericFlicker = .90f + .10f * Mathf.Sin(time * 38f + i * 2.31f);
                    Color color = shootingStarHeadColors[i];
                    color.a *= Mathf.Clamp01(fade) * atmosphericFlicker;
                    head.color = color;
                    float pulse = 1f + Mathf.Sin(time * 31f + i) * .07f;
                    head.transform.localScale = Vector3.one * (shootingStarHeadScales[i] * pulse);
                }

                if (time < shootingStarDisableTimes[i]) continue;
                trail.Clear();
                trail.gameObject.SetActive(false);
            }

            if (time < nextShootingStarAt) return;
            int slot = FindAvailableShootingStar();
            SpawnShootingStar(slot, time);
            float intervalT = Hash01(shootingStarSequence * 547 + 83);
            nextShootingStarAt = time + LerpRange(environment.shootingStarIntervalRange,
                intervalT, 2.1f, 4.8f);
        }

        private int FindAvailableShootingStar()
        {
            for (int i = 0; i < shootingStars.Length; i++)
                if (shootingStars[i] != null && !shootingStars[i].gameObject.activeSelf) return i;
            return PositiveModulo(shootingStarSequence, shootingStars.Length);
        }

        private void SpawnShootingStar(int slot, float time)
        {
            TrailRenderer trail = shootingStars[slot];
            if (trail == null) return;

            int sequence = shootingStarSequence++;
            float viewHeight = displayCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * displayCamera.aspect;
            float direction = Hash01(sequence * 193 + 29) > .5f ? 1f : -1f;
            float heightT = Hash01(sequence * 347 + 71);
            float speed = LerpRange(environment.shootingStarSpeedRange,
                Hash01(sequence * 263 + 17), 15f, 23f);
            Vector3 cameraPosition = displayCamera.transform.position;
            trail.transform.position = new Vector3(
                cameraPosition.x - direction * viewWidth * .58f,
                cameraPosition.y + Mathf.Lerp(viewHeight * .12f, viewHeight * .46f, heightT),
                11.8f);
            shootingStarVelocities[slot] = new Vector3(direction * speed,
                -speed * Mathf.Lerp(.16f, .28f, Hash01(sequence * 419 + 101)), 0f);
            shootingStarAccelerations[slot] = new Vector3(-direction * speed * .018f,
                -Mathf.Lerp(.38f, .86f, Hash01(sequence * 601 + 137)), 0f);
            float flightSeconds = viewWidth * 1.18f / Mathf.Max(1f, speed);
            shootingStarStartTimes[slot] = time;
            shootingStarStopTimes[slot] = time + flightSeconds;
            trail.time = Mathf.Clamp(environment.shootingStarTrailSeconds *
                                     Mathf.Lerp(.72f, 1.18f, Hash01(sequence * 467 + 59)), .2f, 1.2f);
            trail.widthMultiplier = Mathf.Lerp(.045f, .095f, Hash01(sequence * 389 + 41));
            shootingStarDisableTimes[slot] = shootingStarStopTimes[slot] + trail.time;
            shootingStarHeadScales[slot] = Mathf.Lerp(.085f, .16f, Hash01(sequence * 719 + 107));
            shootingStarHeadColors[slot] = Color.Lerp(new Color(.60f, .84f, 1f, .96f),
                new Color(1f, .86f, .65f, .98f), Hash01(sequence * 829 + 13));
            trail.gameObject.SetActive(true);
            trail.Clear();
            trail.emitting = true;
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

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private void OnDestroy()
        {
            if (shootingStarMaterial != null) DestroyRuntimeObject(shootingStarMaterial);
            if (spacePanoramaMaterial != null) DestroyRuntimeObject(spacePanoramaMaterial);
            DestroyTwinklingStarAssets();
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
