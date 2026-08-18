using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class RallyRoofDamage : MonoBehaviour
    {
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private SpriteRenderer[] bodyRenderers;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(.1f)] private float detachedRoofMass = 8f;
        [SerializeField, Min(0f)] private float separationImpulse = 2.4f;
        [SerializeField, Tooltip("Local Y height in sprite space where the beltline cut occurs (removes windshield, roof, rear pillar, and rear metal structure)")]
        private float beltlineCutHeight = 0.72f;
        [SerializeField, Tooltip("Required continuous upside down time with ground contact to break roof")]
        private float requiredUpsideDownSeconds = 3.0f;
        [SerializeField, Tooltip("Dot product threshold for upside-down detection (< -0.45 means angle > 117 degrees inverted)")]
        private float upsideDownDotThreshold = -0.45f;
        [SerializeField] private Material beltlineMaterial;

        private MaterialPropertyBlock bodyPropertyBlock;
        private static Material s_sharedBeltlineMaterial;
        private float upsideDownContactTimer;
        private float lastRoofGroundContactTime = -10f;
        private Collider roofTriggerCollider;

        public bool RoofDetached { get; private set; }
        public float UpsideDownContactTimer => upsideDownContactTimer;

        public void Assign(VehicleDefinition vehicleDefinition, Rigidbody body, SpriteRenderer[] renderers,
            Transform targetVisualRoot)
        {
            definition = vehicleDefinition;
            vehicleBody = body;
            bodyRenderers = renderers;
            visualRoot = targetVisualRoot;
            EnsureMaterial();
        }

        private void Awake()
        {
            roofTriggerCollider = GetComponent<Collider>();
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

        private void OnTriggerEnter(Collider other)
        {
            RecordGroundContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            RecordGroundContact(other);
        }

        private void RecordGroundContact(Collider other)
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally) return;
            Transform rootTransform = vehicleBody != null ? vehicleBody.transform : transform.root;
            if (GroundContactFilter.IsGround(other, rootTransform))
            {
                lastRoofGroundContactTime = Time.time;
            }
        }

        private void FixedUpdate()
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally) return;

            Transform refTransform = vehicleBody != null ? vehicleBody.transform : transform;
            float upDot = Vector3.Dot(refTransform.up, Vector3.up);
            bool isUpsideDown = upDot < upsideDownDotThreshold;
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

            // 3. Add physical collision & impulse to flying canopy
            BoxCollider detachedCollider = piece.AddComponent<BoxCollider>();
            detachedCollider.size = new Vector3(activeSprite.bounds.size.x * 0.65f,
                Mathf.Max(0.12f, activeSprite.bounds.size.y * 0.45f), 0.18f);
            detachedCollider.center = new Vector3(0f, beltlineCutHeight * 0.5f + 0.35f, 0f);

            Rigidbody roofBody = piece.AddComponent<Rigidbody>();
            roofBody.mass = detachedRoofMass;
            roofBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (vehicleBody != null)
            {
                roofBody.linearVelocity = vehicleBody.GetPointVelocity(transform.position) +
                                          Vector3.up * separationImpulse +
                                          vehicleBody.transform.forward * 1.5f;
                roofBody.angularVelocity = vehicleBody.angularVelocity + new Vector3(0.5f, 0f, 3.2f);
            }

            if (roofTriggerCollider != null) roofTriggerCollider.enabled = false;
            Destroy(piece, 8f);
        }
    }
}
