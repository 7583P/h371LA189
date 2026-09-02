using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>One reusable physical obstacle slot for the deterministic Last Horizon route.</summary>
    public sealed class LastHorizonObstacleEvent : MonoBehaviour
    {
        private GameObject platformObject;
        private Rigidbody2D platformBody;
        private BoxCollider2D platformCollider;
        private HingeJoint2D platformHinge;
        private SpriteRenderer platformRenderer;
        private LethalObstacleContact platformLethal;
        private readonly GameObject[] rollerObjects = new GameObject[4];
        private readonly Rigidbody2D[] rollerBodies = new Rigidbody2D[4];
        private readonly CircleCollider2D[] rollerColliders = new CircleCollider2D[4];
        private readonly SpriteRenderer[] rollerRenderers = new SpriteRenderer[4];

        private LastHorizonObstacleLayout layout;
        private LastHorizonSettings settings;
        private float liftBaseY;

        public int CellIndex => layout.cellIndex;
        public LastHorizonObstacleType Type => layout.type;

        public void Configure(MapProfile profile, LastHorizonObstacleLayout obstacleLayout)
        {
            if (profile == null || profile.lastHorizon == null || !obstacleLayout.IsValid)
            {
                Deactivate();
                return;
            }

            EnsureObjects();
            settings = profile.lastHorizon;
            layout = obstacleLayout;
            TerrainManager manager = GetComponentInParent<TerrainManager>();
            if (manager != null && manager.TrySampleAuthoritativeSurface(layout.centerX,
                    out float processedSurfaceY, out _))
                layout.surfaceY = processedSurfaceY;
            gameObject.name = $"Last Horizon {layout.type} - Cell {layout.cellIndex}";
            gameObject.SetActive(true);
            ConfigurePlatform(false);
            ConfigureRollers(0);

            switch (layout.type)
            {
                case LastHorizonObstacleType.Seesaw:
                    ConfigureSeesaw();
                    break;
                case LastHorizonObstacleType.LiftPlatform:
                    ConfigureLift();
                    break;
                case LastHorizonObstacleType.Rollers:
                    ConfigureRollers(layout.rollerCount);
                    break;
                case LastHorizonObstacleType.TiltingPlatform:
                    ConfigureTiltingPlatform();
                    break;
                case LastHorizonObstacleType.Ramp:
                    ConfigureRamp();
                    break;
            }
        }

        private void EnsureObjects()
        {
            if (platformObject == null)
            {
                platformObject = new GameObject("Platform");
                platformObject.transform.SetParent(transform, false);
                platformBody = platformObject.AddComponent<Rigidbody2D>();
                platformCollider = platformObject.AddComponent<BoxCollider2D>();
                platformHinge = platformObject.AddComponent<HingeJoint2D>();
                platformRenderer = platformObject.AddComponent<SpriteRenderer>();
                platformLethal = platformObject.AddComponent<LethalObstacleContact>();
                platformRenderer.sortingOrder = 12;
                platformBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                platformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            for (int i = 0; i < rollerObjects.Length; i++)
            {
                if (rollerObjects[i] != null) continue;
                GameObject roller = new GameObject($"Roller {i + 1}");
                roller.transform.SetParent(transform, false);
                rollerObjects[i] = roller;
                rollerBodies[i] = roller.AddComponent<Rigidbody2D>();
                rollerColliders[i] = roller.AddComponent<CircleCollider2D>();
                rollerRenderers[i] = roller.AddComponent<SpriteRenderer>();
                rollerRenderers[i].sortingOrder = 12;
                rollerBodies[i].interpolation = RigidbodyInterpolation2D.Interpolate;
                rollerBodies[i].collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void ConfigureSeesaw()
        {
            ConfigurePlatform(true);
            float width = Mathf.Max(settings.seesawWidth, layout.width);
            float y = layout.surfaceY + .34f;
            platformBody.bodyType = RigidbodyType2D.Dynamic;
            platformBody.mass = 38f;
            platformBody.linearDamping = .35f;
            platformBody.angularDamping = 3.8f;
            platformBody.position = new Vector2(layout.centerX, y);
            platformBody.rotation = 0f;
            platformBody.linearVelocity = Vector2.zero;
            platformBody.angularVelocity = 0f;
            platformHinge.enabled = true;
            platformHinge.connectedBody = null;
            platformHinge.autoConfigureConnectedAnchor = false;
            platformHinge.anchor = Vector2.zero;
            platformHinge.connectedAnchor = new Vector2(layout.centerX, y);
            platformHinge.useLimits = true;
            platformHinge.limits = new JointAngleLimits2D
            {
                min = -Mathf.Abs(settings.seesawMaximumAngle),
                max = Mathf.Abs(settings.seesawMaximumAngle)
            };
            ApplySprite(platformRenderer, settings.seesawSprite, width, 1.35f);
            platformCollider.size = UnscaledBoxSize(platformObject.transform, width, .58f);
        }

        private void ConfigureLift()
        {
            ConfigurePlatform(true);
            float width = Mathf.Max(settings.liftWidth, layout.width);
            liftBaseY = layout.surfaceY + .36f;
            platformBody.bodyType = RigidbodyType2D.Kinematic;
            platformBody.position = new Vector2(layout.centerX, liftBaseY);
            platformBody.rotation = 0f;
            platformBody.linearVelocity = Vector2.zero;
            platformBody.angularVelocity = 0f;
            platformHinge.enabled = false;
            ApplySprite(platformRenderer, settings.liftPlatformSprite, width, 1.25f);
            platformCollider.size = UnscaledBoxSize(platformObject.transform, width, .58f);
        }

        private void ConfigureTiltingPlatform()
        {
            ConfigurePlatform(true);
            float width = Mathf.Max(settings.tiltingPlatformWidth, layout.width);
            float y = layout.surfaceY + .38f;
            platformBody.bodyType = RigidbodyType2D.Kinematic;
            platformBody.position = new Vector2(layout.centerX, y);
            platformBody.rotation = 0f;
            platformBody.linearVelocity = Vector2.zero;
            platformBody.angularVelocity = 0f;
            platformHinge.enabled = false;
            ApplySprite(platformRenderer, settings.seesawSprite, width, 1.28f);
            platformCollider.size = UnscaledBoxSize(platformObject.transform, width, .58f);
        }

        private void ConfigureRamp()
        {
            ConfigurePlatform(true);
            float width = Mathf.Max(settings.seesawWidth, layout.width);
            float angle = layout.phase < Mathf.PI ? 31f : -31f;
            float y = layout.surfaceY + Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad) * width * .25f;
            platformBody.bodyType = RigidbodyType2D.Kinematic;
            platformBody.position = new Vector2(layout.centerX, y);
            platformBody.rotation = angle;
            platformBody.linearVelocity = Vector2.zero;
            platformBody.angularVelocity = 0f;
            platformHinge.enabled = false;
            ApplySprite(platformRenderer, settings.seesawSprite, width, 1.28f);
            platformCollider.size = UnscaledBoxSize(platformObject.transform, width, .58f);
        }

        private void ConfigurePlatform(bool active)
        {
            platformObject.SetActive(active);
            if (!active) return;
            if (platformLethal == null) platformLethal = platformObject.GetComponent<LethalObstacleContact>();
            platformLethal?.Configure(null, true);
            platformRenderer.color = Color.white;
            platformRenderer.sprite = null;
            platformObject.transform.localScale = Vector3.one;
        }

        private void ConfigureRollers(int requestedCount)
        {
            int count = Mathf.Clamp(requestedCount, 0, rollerObjects.Length);
            float radius = settings != null ? Mathf.Max(.35f, settings.rollerRadius) : .85f;
            float spacing = radius * 2.35f;
            float firstX = layout.centerX - (count - 1) * spacing * .5f;
            for (int i = 0; i < rollerObjects.Length; i++)
            {
                bool active = i < count;
                rollerObjects[i].SetActive(active);
                if (!active) continue;
                float x = firstX + i * spacing;
                TerrainManager manager = GetComponentInParent<TerrainManager>();
                float surfaceY = manager != null &&
                                 manager.TrySampleAuthoritativeSurface(x, out float processedY, out _)
                    ? processedY : TerrainGenerator.SampleLastHorizonHeight(FindProfile(), x);
                rollerBodies[i].bodyType = RigidbodyType2D.Dynamic;
                rollerBodies[i].mass = 10f;
                rollerBodies[i].linearDamping = .18f;
                rollerBodies[i].angularDamping = .12f;
                rollerBodies[i].position = new Vector2(x, surfaceY + radius + .08f);
                rollerBodies[i].rotation = 0f;
                rollerBodies[i].linearVelocity = Vector2.zero;
                rollerBodies[i].angularVelocity = 0f;
                ApplySprite(rollerRenderers[i], settings.rollerSprite, radius * 2f, radius * 2f);
                float maximumScale = Mathf.Max(.001f,
                    Mathf.Abs(rollerObjects[i].transform.localScale.x),
                    Mathf.Abs(rollerObjects[i].transform.localScale.y));
                rollerColliders[i].radius = radius / maximumScale;
            }
        }

        private MapProfile FindProfile()
        {
            TerrainManager manager = GetComponentInParent<TerrainManager>();
            return manager != null ? manager.ActiveProfile : null;
        }

        private static void ApplySprite(SpriteRenderer renderer, Sprite sprite, float width, float height)
        {
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.transform.position = new Vector3(renderer.transform.position.x,
                renderer.transform.position.y, -5.72f);
            if (sprite == null)
            {
                renderer.transform.localScale = Vector3.one;
                return;
            }
            Vector2 source = sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / Mathf.Max(.01f, source.x),
                height / Mathf.Max(.01f, source.y), 1f);
        }

        private static Vector2 UnscaledBoxSize(Transform target, float worldWidth, float worldHeight)
        {
            Vector3 scale = target.localScale;
            return new Vector2(worldWidth / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                worldHeight / Mathf.Max(.001f, Mathf.Abs(scale.y)));
        }

        private void FixedUpdate()
        {
            if (settings == null || platformBody == null || platformObject == null ||
                !platformObject.activeSelf) return;
            if (layout.type == LastHorizonObstacleType.LiftPlatform)
            {
                float period = Mathf.Max(1f, settings.liftPeriod);
                float wave = .5f + .5f * Mathf.Sin(Time.fixedTime * Mathf.PI * 2f / period + layout.phase);
                float y = liftBaseY + wave * Mathf.Max(.2f, settings.liftTravel);
                platformBody.MovePosition(new Vector2(layout.centerX, y));
                return;
            }
            if (layout.type == LastHorizonObstacleType.TiltingPlatform)
            {
                float period = Mathf.Max(1f, settings.tiltingPlatformPeriod);
                float wave = Mathf.Sin(Time.fixedTime * Mathf.PI * 2f / period + layout.phase);
                platformBody.MoveRotation(wave * Mathf.Abs(settings.tiltingPlatformMaximumAngle));
            }
        }

        public void Deactivate()
        {
            settings = null;
            layout = default;
            if (platformObject != null) platformObject.SetActive(false);
            for (int i = 0; i < rollerObjects.Length; i++)
                if (rollerObjects[i] != null) rollerObjects[i].SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
