using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>One recyclable Arena gameplay segment containing only its interactive props.</summary>
    public sealed class ArenaSegmentEvent : MonoBehaviour
    {
        private readonly GameObject[] tires = new GameObject[8];
        private SpriteRenderer platformRenderer;
        private Rigidbody2D platformBody;
        private Rigidbody2D platformAnchor;
        private BoxCollider2D platformCollider;
        private HingeJoint2D platformJoint;
        private ArenaTiltingPlatform platformController;
        private SpriteRenderer lavaRenderer;
        private BoxCollider2D lavaTrigger;
        private ArenaLavaZone lavaZone;
        private PhysicsMaterial2D tireMaterial;
        private float lavaPulsePhase;

        public int SegmentIndex { get; private set; }

        private void Awake() => EnsureComponents();

        private Transform GetOrCreate(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null) return child;
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
            return child;
        }

        private void EnsureComponents()
        {
            if (tireMaterial == null)
                tireMaterial = new PhysicsMaterial2D("Arena Tire Physics") { friction = .82f, bounciness = .035f };
            for (int i = 0; i < tires.Length; i++)
            {
                Transform tire = GetOrCreate($"Physical Tire {i + 1}");
                tires[i] = tire.gameObject;
                if (tire.GetComponent<SpriteRenderer>() == null) tire.gameObject.AddComponent<SpriteRenderer>();
                CircleCollider2D collider = tire.GetComponent<CircleCollider2D>();
                if (collider == null) collider = tire.gameObject.AddComponent<CircleCollider2D>();
                collider.sharedMaterial = tireMaterial;
                Rigidbody2D body = tire.GetComponent<Rigidbody2D>();
                if (body == null) body = tire.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                tire.gameObject.SetActive(false);
            }

            Transform anchor = GetOrCreate("Floating Platform Anchor");
            platformAnchor = anchor.GetComponent<Rigidbody2D>();
            if (platformAnchor == null) platformAnchor = anchor.gameObject.AddComponent<Rigidbody2D>();
            platformAnchor.bodyType = RigidbodyType2D.Static;

            Transform platform = GetOrCreate("Tilting Floating Platform");
            platformRenderer = platform.GetComponent<SpriteRenderer>();
            if (platformRenderer == null) platformRenderer = platform.gameObject.AddComponent<SpriteRenderer>();
            platformRenderer.sortingOrder = 5;
            platformBody = platform.GetComponent<Rigidbody2D>();
            if (platformBody == null) platformBody = platform.gameObject.AddComponent<Rigidbody2D>();
            platformBody.bodyType = RigidbodyType2D.Dynamic;
            platformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            platformBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            platformCollider = platform.GetComponent<BoxCollider2D>();
            if (platformCollider == null) platformCollider = platform.gameObject.AddComponent<BoxCollider2D>();
            platformJoint = platform.GetComponent<HingeJoint2D>();
            if (platformJoint == null) platformJoint = platform.gameObject.AddComponent<HingeJoint2D>();
            platformJoint.connectedBody = platformAnchor;
            platformJoint.autoConfigureConnectedAnchor = false;
            platformJoint.anchor = Vector2.zero;
            platformJoint.connectedAnchor = Vector2.zero;
            platformJoint.useLimits = true;
            platformController = platform.GetComponent<ArenaTiltingPlatform>();
            if (platformController == null) platformController = platform.gameObject.AddComponent<ArenaTiltingPlatform>();
            platform.gameObject.SetActive(false);
            anchor.gameObject.SetActive(false);

            Transform lava = GetOrCreate("Viscous Lava Pool");
            lavaRenderer = lava.GetComponent<SpriteRenderer>();
            if (lavaRenderer == null) lavaRenderer = lava.gameObject.AddComponent<SpriteRenderer>();
            lavaRenderer.drawMode = SpriteDrawMode.Tiled;
            lavaRenderer.tileMode = SpriteTileMode.Continuous;
            lavaRenderer.sortingOrder = 4;
            lavaTrigger = lava.GetComponent<BoxCollider2D>();
            if (lavaTrigger == null) lavaTrigger = lava.gameObject.AddComponent<BoxCollider2D>();
            lavaTrigger.isTrigger = true;
            lavaZone = lava.GetComponent<ArenaLavaZone>();
            if (lavaZone == null) lavaZone = lava.gameObject.AddComponent<ArenaLavaZone>();
            lava.gameObject.SetActive(false);
        }

        public void Configure(MapProfile profile, ArenaSegmentLayout layout, float trackWidth)
        {
            EnsureComponents();
            ArenaSettings settings = profile != null && profile.arena != null
                ? profile.arena : new ArenaSettings();
            SegmentIndex = layout.index;
            transform.position = Vector3.zero;
            ConfigureTires(profile, settings, layout, trackWidth);
            ConfigurePlatform(settings, layout, trackWidth);
            ConfigureLava(profile, settings, layout, trackWidth);
            gameObject.name = $"Arena {layout.type} Segment {layout.index} - Pooled";
            gameObject.SetActive(true);
        }

        private void ConfigureTires(MapProfile profile, ArenaSettings settings, ArenaSegmentLayout layout,
            float trackWidth)
        {
            tireMaterial.friction = settings.tireFriction;
            tireMaterial.bounciness = settings.tireBounciness;
            int activeCount = Mathf.Clamp(layout.tireCount, 0, tires.Length);
            int lowerRow = Mathf.CeilToInt(activeCount * .58f);
            for (int i = 0; i < tires.Length; i++)
            {
                GameObject tire = tires[i];
                if (i >= activeCount || settings.tireSprite == null)
                {
                    tire.SetActive(false);
                    continue;
                }
                tire.SetActive(true);
                int row = layout.variant > .58f && i >= lowerRow ? 1 : 0;
                int column = row == 0 ? i : i - lowerRow;
                float x = layout.tireStart + column * layout.tireSpacing + row * layout.tireSpacing * .48f;
                float surface = TerrainGenerator.SampleHeight(profile, x);
                float radius = settings.tireRadius * Mathf.Lerp(.92f, 1.08f,
                    Mathf.Repeat(layout.variant * 3.7f + i * .37f, 1f));
                SpriteRenderer renderer = tire.GetComponent<SpriteRenderer>();
                renderer.sprite = settings.tireSprite;
                renderer.sortingOrder = 6;
                float sourceDiameter = Mathf.Max(.001f, settings.tireSprite.bounds.size.x);
                tire.transform.localScale = Vector3.one * (radius * 2f / sourceDiameter);
                tire.transform.position = new Vector3(x, surface + radius + row * radius * 1.72f,
                    -trackWidth * .5f - .14f);
                tire.transform.rotation = Quaternion.Euler(0f, 0f, layout.variant * 27f + i * 11f);
                CircleCollider2D collider = tire.GetComponent<CircleCollider2D>();
                collider.radius = sourceDiameter * .46f;
                collider.sharedMaterial = tireMaterial;
                Rigidbody2D body = tire.GetComponent<Rigidbody2D>();
                body.simulated = true;
                body.mass = settings.tireMass * Mathf.Lerp(.9f, 1.12f, radius / settings.tireRadius - .9f);
                body.linearDamping = .48f;
                body.angularDamping = .72f;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.WakeUp();
            }
        }

        private void ConfigurePlatform(ArenaSettings settings, ArenaSegmentLayout layout, float trackWidth)
        {
            bool active = layout.HasPlatform && settings.platformSprite != null;
            platformAnchor.gameObject.SetActive(active);
            platformBody.gameObject.SetActive(active);
            if (!active) return;
            Vector2 position = new Vector2(layout.platformCenterX, layout.platformY);
            platformAnchor.position = position;
            platformBody.position = position;
            platformBody.rotation = 0f;
            platformBody.linearVelocity = Vector2.zero;
            platformBody.angularVelocity = 0f;
            platformBody.mass = settings.platformMass;
            platformBody.linearDamping = .35f;
            platformBody.angularDamping = settings.platformAngularDamping;
            platformRenderer.sprite = settings.platformSprite;
            float sourceWidth = Mathf.Max(.001f, settings.platformSprite.bounds.size.x);
            float scale = layout.platformWidth / sourceWidth;
            platformBody.transform.localScale = Vector3.one * scale;
            platformBody.transform.position = new Vector3(position.x, position.y, -trackWidth * .5f - .12f);
            platformCollider.size = new Vector2(settings.platformSprite.bounds.size.x * .96f,
                settings.platformSprite.bounds.size.y * .62f);
            platformCollider.offset = new Vector2(0f, settings.platformSprite.bounds.size.y * .17f);
            float maxTilt = Mathf.Clamp(settings.maximumPlatformTilt, 10f, 35f);
            platformJoint.limits = new JointAngleLimits2D { min = -maxTilt, max = maxTilt };
            platformJoint.enabled = true;
            platformController.Configure(settings);
            platformBody.WakeUp();
        }

        private void ConfigureLava(MapProfile profile, ArenaSettings settings, ArenaSegmentLayout layout,
            float trackWidth)
        {
            bool active = layout.HasLava && settings.lavaSprite != null;
            lavaRenderer.gameObject.SetActive(active);
            if (!active) return;
            float length = layout.lavaEnd - layout.lavaStart;
            float centerX = (layout.lavaStart + layout.lavaEnd) * .5f;
            float surfaceY = TerrainGenerator.SampleHeight(profile, centerX) + .18f;
            lavaRenderer.sprite = settings.lavaSprite;
            lavaRenderer.size = new Vector2(length, settings.lavaVisualDepth);
            lavaRenderer.transform.position = new Vector3(centerX,
                surfaceY - settings.lavaVisualDepth * .47f, -trackWidth * .5f - .16f);
            lavaRenderer.color = Color.white;
            lavaTrigger.size = new Vector2(length, settings.lavaVisualDepth * .88f);
            lavaTrigger.offset = Vector2.zero;
            lavaZone.Configure(settings);
            lavaPulsePhase = layout.variant * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (lavaRenderer == null || !lavaRenderer.gameObject.activeSelf) return;
            float pulse = .88f + Mathf.Sin(Time.time * 2.25f + lavaPulsePhase) * .08f;
            lavaRenderer.color = new Color(1f, pulse, pulse * .82f, 1f);
        }

        public void Deactivate()
        {
            for (int i = 0; i < tires.Length; i++)
            {
                if (tires[i] == null) continue;
                Rigidbody2D body = tires[i].GetComponent<Rigidbody2D>();
                if (body != null) { body.linearVelocity = Vector2.zero; body.angularVelocity = 0f; body.simulated = false; }
                tires[i].SetActive(false);
            }
            if (platformBody != null)
            {
                platformBody.linearVelocity = Vector2.zero;
                platformBody.angularVelocity = 0f;
                platformBody.gameObject.SetActive(false);
            }
            if (platformAnchor != null) platformAnchor.gameObject.SetActive(false);
            if (lavaRenderer != null) lavaRenderer.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public bool AuditPhysicsComponents(ArenaSegmentLayout layout, ArenaSettings settings, out string problem)
        {
            problem = string.Empty;
            if (layout.HasTires)
            {
                int expected = Mathf.Clamp(layout.tireCount, 0, tires.Length);
                for (int i = 0; i < expected; i++)
                {
                    CircleCollider2D collider = tires[i] != null ? tires[i].GetComponent<CircleCollider2D>() : null;
                    Rigidbody2D body = tires[i] != null ? tires[i].GetComponent<Rigidbody2D>() : null;
                    if (tires[i] == null || !tires[i].activeSelf || collider == null || !collider.enabled ||
                        body == null || body.bodyType != RigidbodyType2D.Dynamic || !body.simulated ||
                        body.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
                    {
                        problem = $"physical tire {i + 1} has an incomplete collider/Rigidbody2D setup";
                        return false;
                    }
                }
            }
            if (layout.HasPlatform)
            {
                float limit = Mathf.Clamp(settings.maximumPlatformTilt, 10f, 35f);
                if (platformBody == null || !platformBody.gameObject.activeSelf ||
                    platformBody.bodyType != RigidbodyType2D.Dynamic || platformCollider == null ||
                    !platformCollider.enabled || platformJoint == null || !platformJoint.enabled ||
                    platformJoint.connectedBody != platformAnchor || !platformJoint.useLimits ||
                    Mathf.Abs(platformJoint.limits.min + limit) > .01f ||
                    Mathf.Abs(platformJoint.limits.max - limit) > .01f || platformController == null)
                {
                    problem = "floating platform has an incomplete collider, hinge, limits or weight controller";
                    return false;
                }
            }
            if (layout.HasLava && (lavaRenderer == null || !lavaRenderer.gameObject.activeSelf ||
                                  lavaTrigger == null || !lavaTrigger.enabled || !lavaTrigger.isTrigger ||
                                  lavaZone == null))
            {
                problem = "lava has an incomplete trigger or viscous-drag controller";
                return false;
            }
            return true;
        }

        private void OnDestroy()
        {
            if (tireMaterial == null) return;
            if (Application.isPlaying) Destroy(tireMaterial); else DestroyImmediate(tireMaterial);
        }
    }

    public sealed class ArenaTiltingPlatform : MonoBehaviour
    {
        private Rigidbody2D body;
        private ArenaSettings settings;
        private float pendingWeightMoment;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        public void Configure(ArenaSettings arenaSettings)
        {
            settings = arenaSettings;
            pendingWeightMoment = 0f;
            if (body == null) body = GetComponent<Rigidbody2D>();
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision == null || collision.rigidbody == null) return;
            VehicleRuntimeBase vehicle = collision.rigidbody.GetComponentInParent<VehicleRuntimeBase>();
            if (vehicle == null && collision.rigidbody.GetComponent<Suspension2DController>() == null) return;
            float contactX = 0f;
            int count = Mathf.Max(1, collision.contactCount);
            for (int i = 0; i < collision.contactCount; i++)
                contactX += transform.InverseTransformPoint(collision.GetContact(i).point).x;
            pendingWeightMoment += contactX / count * Mathf.Max(1f, collision.rigidbody.mass);
        }

        private void FixedUpdate()
        {
            if (body == null || settings == null) return;
            float angle = Mathf.DeltaAngle(0f, body.rotation);
            float weightTorque = -pendingWeightMoment * settings.platformWeightTorque;
            float centeringTorque = -angle * settings.platformCenteringTorque;
            body.AddTorque(weightTorque + centeringTorque, ForceMode2D.Force);
            pendingWeightMoment = 0f;
        }
    }

    public sealed class ArenaLavaZone : MonoBehaviour
    {
        private ArenaSettings settings;
        private VehicleRuntimeBase cachedVehicle;

        public void Configure(ArenaSettings arenaSettings) => settings = arenaSettings;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (settings == null || other == null) return;
            VehicleRuntimeBase vehicle = other.GetComponentInParent<VehicleRuntimeBase>();
            bool wheel = other.attachedRigidbody != null &&
                         other.attachedRigidbody.GetComponent<Suspension2DController>() != null;
            if (vehicle == null && !wheel) return;
            if (vehicle == null)
                vehicle = cachedVehicle != null ? cachedVehicle : FindFirstObjectByType<VehiclePhysics2DController>();
            if (vehicle == null || vehicle.Body2D == null) return;
            cachedVehicle = vehicle;
            ArenaLavaDragReceiver receiver = vehicle.GetComponent<ArenaLavaDragReceiver>();
            if (receiver == null) receiver = vehicle.gameObject.AddComponent<ArenaLavaDragReceiver>();
            receiver.Touch(settings);
        }
    }

    public sealed class ArenaLavaDragReceiver : MonoBehaviour
    {
        private Rigidbody2D body;
        private ArenaSettings settings;
        private float lastContactTime = float.NegativeInfinity;
        private float intensity;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        public void Touch(ArenaSettings arenaSettings)
        {
            settings = arenaSettings;
            lastContactTime = Time.fixedTime;
            if (body == null) body = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (body == null || settings == null) return;
            bool touching = Time.fixedTime - lastContactTime <= Time.fixedDeltaTime * 2.5f;
            float seconds = touching ? settings.lavaEntrySeconds : settings.lavaRecoverySeconds;
            intensity = Mathf.MoveTowards(intensity, touching ? 1f : 0f,
                Time.fixedDeltaTime / Mathf.Max(.05f, seconds));
            if (intensity <= .001f) return;
            Vector2 velocity = body.linearVelocity;
            body.AddForce(-velocity * body.mass * settings.lavaDragStrength * intensity, ForceMode2D.Force);
            float cap = Mathf.Max(.5f, settings.lavaMaximumSpeed);
            if (Mathf.Abs(velocity.x) > cap)
            {
                velocity.x = Mathf.MoveTowards(velocity.x, Mathf.Sign(velocity.x) * cap,
                    settings.lavaDragStrength * cap * intensity * Time.fixedDeltaTime);
                body.linearVelocity = velocity;
            }
        }
    }
}
