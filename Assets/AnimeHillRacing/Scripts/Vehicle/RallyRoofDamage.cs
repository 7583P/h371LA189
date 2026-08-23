using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class RallyRoofDamage : MonoBehaviour
    {
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private Rigidbody2D vehicleBody2D;
        [SerializeField] private SpriteRenderer[] bodyRenderers;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(.1f)] private float detachedRoofMass = 8f;
        [SerializeField, Min(0f), Tooltip("Small upward launch speed applied when the canopy separates")]
        private float separationImpulse = 1.35f;
        [SerializeField, Tooltip("Local Y height in sprite space where the beltline cut occurs (removes windshield, roof, rear pillar, and rear metal structure)")]
        private float beltlineCutHeight = 0.72f;
        [SerializeField, Tooltip("Required continuous upside down time with ground contact to break roof")]
        private float requiredUpsideDownSeconds = 1.25f;
        [SerializeField, Tooltip("Dot product threshold relative to the terrain normal (< -0.35 means angle > 110 degrees inverted)")]
        private float upsideDownDotThreshold = -0.35f;
        [SerializeField] private Material beltlineMaterial;

        private MaterialPropertyBlock bodyPropertyBlock;
        private static Material s_sharedBeltlineMaterial;
        private float upsideDownContactTimer;
        private float lastRoofGroundContactTime = -10f;
        private Collider roofTriggerCollider;
        private Collider2D roofTriggerCollider2D;
        private readonly Collider[] overlapResults3D = new Collider[12];
        private readonly Collider2D[] overlapResults2D = new Collider2D[12];

        public bool RoofDetached { get; private set; }
        public float UpsideDownContactTimer => upsideDownContactTimer;

        public void Assign(VehicleDefinition vehicleDefinition, Rigidbody body, SpriteRenderer[] renderers,
            Transform targetVisualRoot)
        {
            Assign(vehicleDefinition, body, null, renderers, targetVisualRoot);
        }

        public void Assign(VehicleDefinition vehicleDefinition, Rigidbody body, Rigidbody2D body2D,
            SpriteRenderer[] renderers, Transform targetVisualRoot)
        {
            definition = vehicleDefinition;
            vehicleBody = body;
            vehicleBody2D = body2D;
            bodyRenderers = renderers;
            visualRoot = targetVisualRoot;
            EnsureMaterial();
        }

        private void Awake()
        {
            roofTriggerCollider = GetComponent<Collider>();
            roofTriggerCollider2D = GetComponent<Collider2D>();
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
            if (vehicleBody2D == null) vehicleBody2D = GetComponentInParent<Rigidbody2D>();
            if (definition == null) definition = GetComponentInParent<VehicleRuntimeBase>()?.Definition;
            if (bodyRenderers == null || bodyRenderers.Length == 0) bodyRenderers = GetComponentsInParent<SpriteRenderer>(true);
            EnsureMaterial();
            ResetState();
        }

        private void OnEnable()
        {
            ResetState();
        }

        public void ResetState()
        {
            RoofDetached = false;
            upsideDownContactTimer = 0f;
            lastRoofGroundContactTime = -10f;

            if (roofTriggerCollider != null)
                roofTriggerCollider.enabled = true;
            if (roofTriggerCollider2D != null)
                roofTriggerCollider2D.enabled = true;

            // Reset body renderers to full intact appearance (remove clip)
            if (bodyRenderers != null)
            {
                if (bodyPropertyBlock == null) bodyPropertyBlock = new MaterialPropertyBlock();
                bodyPropertyBlock.SetFloat("_ClipLocalY", 999f); // Disables clipping (shader activates when < 500)
                bodyPropertyBlock.SetFloat("_ClipInvert", 0f);
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    if (bodyRenderers[i] != null)
                        bodyRenderers[i].SetPropertyBlock(bodyPropertyBlock);
                }
            }
        }

        private void EnsureMaterial()
        {
            if (beltlineMaterial == null)
            {
                if (s_sharedBeltlineMaterial == null)
                {
                    Shader shader = Shader.Find("AnimeHillRacing/VehicleBodyBeltline");
                    if (shader == null) shader = Resources.Load<Shader>("VehicleBeltlineCut");
                    if (shader == null) shader = Resources.Load<Shader>("VehicleBodyBeltline");
                    if (shader != null)
                        s_sharedBeltlineMaterial = new Material(shader)
                        {
                            name = "Runtime_RallyBeltlineMaterial",
                            hideFlags = HideFlags.HideAndDontSave
                        };
                }
                beltlineMaterial = s_sharedBeltlineMaterial;
            }

            if (bodyRenderers != null && beltlineMaterial != null)
            {
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    if (bodyRenderers[i] != null)
                    {
                        // Only assign default beltlineMaterial if renderer doesn't already have an active compatible material
                        if (bodyRenderers[i].sharedMaterial == null ||
                            (bodyRenderers[i].sharedMaterial.shader != null &&
                             bodyRenderers[i].sharedMaterial.shader.name != "AnimeHillRacing/VehicleBodyBeltline" &&
                             bodyRenderers[i].sharedMaterial.shader.name != "AnimeHillRacing/VehicleAnimatedLightSweep"))
                        {
                            bodyRenderers[i].sharedMaterial = beltlineMaterial;
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other) => RecordGroundContact3D(other);
        private void OnTriggerStay(Collider other) => RecordGroundContact3D(other);
        private void OnTriggerEnter2D(Collider2D other) => RecordGroundContact2D(other);
        private void OnTriggerStay2D(Collider2D other) => RecordGroundContact2D(other);

        private void RecordGroundContact3D(Collider other)
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally) return;
            Transform rootTransform = vehicleBody != null ? vehicleBody.transform : (vehicleBody2D != null ? vehicleBody2D.transform : transform.root);
            if (GroundContactFilter.IsGround(other, rootTransform))
            {
                lastRoofGroundContactTime = Time.time;
            }
        }

        private void RecordGroundContact2D(Collider2D other)
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally) return;
            Transform rootTransform = vehicleBody2D != null ? vehicleBody2D.transform : (vehicleBody != null ? vehicleBody.transform : transform.root);
            if (GroundContactFilter.IsGround2D(other, rootTransform))
            {
                lastRoofGroundContactTime = Time.time;
            }
        }

        private void FixedUpdate()
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally) return;

            Transform refTransform = vehicleBody != null ? vehicleBody.transform : (vehicleBody2D != null ? vehicleBody2D.transform : transform);
            float upDot = SurfaceRelativeUpDot(refTransform);
            bool isUpsideDown = upDot < upsideDownDotThreshold;
            if (isUpsideDown) RefreshRoofGroundContact();
            bool isRoofGrounded = (Time.time - lastRoofGroundContactTime) <= (Time.fixedDeltaTime * 2.5f);

            // Timer increments ONLY when continuously upside down AND roof is physically touching terrain
            if (isUpsideDown && isRoofGrounded)
            {
                upsideDownContactTimer += Time.fixedDeltaTime;
                if (upsideDownContactTimer >= requiredUpsideDownSeconds)
                {
                    DetachRoof();
                }
            }
            else
            {
                // Reset timer immediately if recovered or airborne
                upsideDownContactTimer = 0f;
            }
        }

        private float SurfaceRelativeUpDot(Transform reference)
        {
            Vector3 surfaceUp = Vector3.up;
            TerrainManager terrain = TerrainManager.Instance;
            if (terrain != null && terrain.TrySampleAuthoritativeSurface(transform.position.x,
                    out _, out float slopeDegrees))
                surfaceUp = Quaternion.Euler(0f, 0f, slopeDegrees) * Vector3.up;
            return Vector3.Dot(reference.up.normalized, surfaceUp.normalized);
        }

        private void RefreshRoofGroundContact()
        {
            Transform vehicleRoot = vehicleBody2D != null ? vehicleBody2D.transform
                : vehicleBody != null ? vehicleBody.transform : transform.root;

            if (roofTriggerCollider2D != null && roofTriggerCollider2D.enabled)
            {
                ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
                int count = roofTriggerCollider2D.Overlap(filter, overlapResults2D);
                for (int i = 0; i < count; i++)
                {
                    if (!GroundContactFilter.IsGround2D(overlapResults2D[i], vehicleRoot)) continue;
                    lastRoofGroundContactTime = Time.time;
                    return;
                }
            }

            if (roofTriggerCollider != null && roofTriggerCollider.enabled)
            {
                Bounds bounds = roofTriggerCollider.bounds;
                Vector3 padding = Vector3.one * Mathf.Max(.025f, Physics.defaultContactOffset);
                int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents + padding,
                    overlapResults3D, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if (!GroundContactFilter.IsGround(overlapResults3D[i], vehicleRoot)) continue;
                    lastRoofGroundContactTime = Time.time;
                    return;
                }
            }

            // Fast rotations can move the thin roof trigger from one side of an EdgeCollider2D to
            // the other between physics steps. Sampling the authoritative surface closes that gap.
            TerrainManager terrain = TerrainManager.Instance;
            if (terrain == null || !terrain.TrySampleAuthoritativeSurface(transform.position.x,
                    out float surfaceY, out float slopeDegrees)) return;

            Bounds roofBounds = roofTriggerCollider2D != null ? roofTriggerCollider2D.bounds
                : roofTriggerCollider != null ? roofTriggerCollider.bounds
                : new Bounds(transform.position, new Vector3(1.6f, .18f, .2f));
            Vector2 surfaceNormal = Quaternion.Euler(0f, 0f, slopeDegrees) * Vector2.up;
            Vector2 surfacePoint = new Vector2(roofBounds.center.x, surfaceY);
            float centerDistance = Vector2.Dot((Vector2)roofBounds.center - surfacePoint, surfaceNormal);
            float projectedExtent = Mathf.Abs(surfaceNormal.x) * roofBounds.extents.x +
                                    Mathf.Abs(surfaceNormal.y) * roofBounds.extents.y;
            float tolerance = Mathf.Max(.06f, Physics2D.defaultContactOffset * 3f);
            if (centerDistance <= projectedExtent + tolerance &&
                centerDistance >= -projectedExtent - tolerance * 2f)
                lastRoofGroundContactTime = Time.time;
        }

        public void DetachRoof()
        {
            if (RoofDetached) return;
            RoofDetached = true;
            upsideDownContactTimer = 0f;

            EnsureMaterial();

            // 1. Cut the vehicle body along the beltline (removes windshield and roof while keeping spoiler & seat)
            if (bodyPropertyBlock == null) bodyPropertyBlock = new MaterialPropertyBlock();
            bodyPropertyBlock.SetFloat("_ClipLocalY", 0f); // Activates beltline clipping
            bodyPropertyBlock.SetFloat("_ClipInvert", 0f); // Keep car body

            Sprite activeSprite = null;
            Material activeMaterial = beltlineMaterial;
            Transform refTransform = transform;

            if (bodyRenderers != null)
            {
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    if (bodyRenderers[i] != null)
                    {
                        if (activeSprite == null && bodyRenderers[i].sprite != null)
                        {
                            activeSprite = bodyRenderers[i].sprite;
                            refTransform = bodyRenderers[i].transform;
                            if (bodyRenderers[i].sharedMaterial != null)
                                activeMaterial = bodyRenderers[i].sharedMaterial;
                        }
                        bodyRenderers[i].SetPropertyBlock(bodyPropertyBlock);
                    }
                }
            }

            if (activeSprite == null && definition != null)
            {
                activeSprite = definition.skins != null && definition.skins.Length > 0
                    ? definition.skins[0].sprite
                    : definition.previewSprite;
            }

            if (activeSprite == null)
            {
                if (roofTriggerCollider != null) roofTriggerCollider.enabled = false;
                if (roofTriggerCollider2D != null) roofTriggerCollider2D.enabled = false;
                return;
            }

            // 2. Instantiate the physical flying upper canopy (windshield + roof)
            GameObject piece = new GameObject("Rally Detached Canopy - Physical");
            piece.transform.position = refTransform.position;
            piece.transform.rotation = refTransform.rotation;
            piece.transform.localScale = refTransform.lossyScale;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = activeSprite;
            renderer.sharedMaterial = activeMaterial;
            renderer.sortingOrder = 13;

            MaterialPropertyBlock canopyBlock = new MaterialPropertyBlock();
            canopyBlock.SetFloat("_ClipLocalY", 0f); // Activates beltline clipping
            canopyBlock.SetFloat("_ClipInvert", 1f); // Keep upper canopy only (windshield + roof)
            renderer.SetPropertyBlock(canopyBlock);

            // 3. Add physical collision & impulse to flying canopy (2D or 3D depending on active body)
            if (vehicleBody2D != null)
            {
                BoxCollider2D detachedCollider2D = piece.AddComponent<BoxCollider2D>();
                detachedCollider2D.size = new Vector2(activeSprite.bounds.size.x * 0.65f,
                    Mathf.Max(0.12f, activeSprite.bounds.size.y * 0.45f));
                detachedCollider2D.offset = new Vector2(0f, beltlineCutHeight * 0.5f + 0.35f);
                IgnoreVehicleCollisions(detachedCollider2D);

                Rigidbody2D roofBody2D = piece.AddComponent<Rigidbody2D>();
                roofBody2D.mass = detachedRoofMass;
                roofBody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                roofBody2D.linearVelocity = vehicleBody2D.GetPointVelocity(transform.position) +
                                            Vector2.up * separationImpulse +
                                            (Vector2)vehicleBody2D.transform.right * .35f;
                roofBody2D.angularVelocity = vehicleBody2D.angularVelocity + 75f;
            }
            else
            {
                BoxCollider detachedCollider = piece.AddComponent<BoxCollider>();
                detachedCollider.size = new Vector3(activeSprite.bounds.size.x * 0.65f,
                    Mathf.Max(0.12f, activeSprite.bounds.size.y * 0.45f), 0.18f);
                detachedCollider.center = new Vector3(0f, beltlineCutHeight * 0.5f + 0.35f, 0f);
                IgnoreVehicleCollisions(detachedCollider);

                Rigidbody roofBody = piece.AddComponent<Rigidbody>();
                roofBody.mass = detachedRoofMass;
                roofBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (vehicleBody != null)
                {
                    roofBody.linearVelocity = vehicleBody.GetPointVelocity(transform.position) +
                                              Vector3.up * separationImpulse +
                                              vehicleBody.transform.right * .35f;
                    roofBody.angularVelocity = vehicleBody.angularVelocity + new Vector3(.25f, 0f, 1.35f);
                }
            }

            if (roofTriggerCollider != null) roofTriggerCollider.enabled = false;
            if (roofTriggerCollider2D != null) roofTriggerCollider2D.enabled = false;
            Destroy(piece, 8f);
        }

        private void IgnoreVehicleCollisions(Collider2D detachedCollider)
        {
            if (detachedCollider == null || vehicleBody2D == null) return;
            Collider2D[] vehicleColliders = vehicleBody2D.transform.root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < vehicleColliders.Length; i++)
            {
                Collider2D vehicleCollider = vehicleColliders[i];
                if (vehicleCollider != null && vehicleCollider != detachedCollider)
                    Physics2D.IgnoreCollision(detachedCollider, vehicleCollider, true);
            }
        }

        private void IgnoreVehicleCollisions(Collider detachedCollider)
        {
            if (detachedCollider == null || vehicleBody == null) return;
            Collider[] vehicleColliders = vehicleBody.transform.root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < vehicleColliders.Length; i++)
            {
                Collider vehicleCollider = vehicleColliders[i];
                if (vehicleCollider != null && vehicleCollider != detachedCollider)
                    Physics.IgnoreCollision(detachedCollider, vehicleCollider, true);
            }
        }
    }
}
