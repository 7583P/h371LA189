using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class RallyRoofDamage : MonoBehaviour
    {
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private SpriteRenderer[] bodyRenderers;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(.1f)] private float detachedRoofMass = 14f;
        [SerializeField, Min(0f)] private float separationImpulse = 2.4f;
        [SerializeField, Tooltip("Local Y height in sprite space where the beltline cut occurs (removes windshield, roof, rear pillar, and rear metal structure)")]
        private float beltlineCutHeight = 0.72f;
        [SerializeField, Tooltip("Optional slope angle across the beltline")]
        private float beltlineCutSlope = 0.0f;
        [SerializeField] private Material beltlineMaterial;

        private MaterialPropertyBlock bodyPropertyBlock;
        private static Material s_sharedBeltlineMaterial;

        public bool RoofDetached { get; private set; }

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
            EnsureMaterial();
        }

        private void EnsureMaterial()
        {
            if (beltlineMaterial == null)
            {
                if (s_sharedBeltlineMaterial == null)
                {
                    Shader shader = Shader.Find("AnimeHillRacing/VehicleBodyBeltline");
                    if (shader != null)
                        s_sharedBeltlineMaterial = new Material(shader) { name = "Runtime_RallyBeltlineMaterial" };
                }
                beltlineMaterial = s_sharedBeltlineMaterial;
            }

            if (bodyRenderers != null && beltlineMaterial != null)
            {
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    if (bodyRenderers[i] != null && bodyRenderers[i].sharedMaterial != beltlineMaterial)
                    {
                        bodyRenderers[i].sharedMaterial = beltlineMaterial;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally ||
                !GroundContactFilter.IsGround(other, transform.root)) return;
            DetachRoof();
        }

        public void DetachRoof()
        {
            if (RoofDetached) return;
            RoofDetached = true;

            EnsureMaterial();

            // 1. Cut the vehicle body along the beltline (keeps only lower chassis, removes windshield and roof while keeping spoiler & seat)
            if (bodyPropertyBlock == null) bodyPropertyBlock = new MaterialPropertyBlock();
            bodyPropertyBlock.SetFloat("_ClipLocalY", 0f); // Activates beltline clipping
            bodyPropertyBlock.SetFloat("_ClipInvert", 0f); // Keep car body (including spoiler and seat)

            Sprite activeSprite = null;
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
                gameObject.SetActive(false);
                return;
            }

            // 2. Instantiate the physical flying upper canopy (windshield + roof)
            GameObject piece = new GameObject("Rally Detached Canopy - Physical");
            piece.transform.position = refTransform.position;
            piece.transform.rotation = refTransform.rotation;
            piece.transform.localScale = refTransform.lossyScale;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = activeSprite;
            renderer.sharedMaterial = beltlineMaterial;
            renderer.sortingOrder = 13;

            MaterialPropertyBlock canopyBlock = new MaterialPropertyBlock();
            canopyBlock.SetFloat("_ClipLocalY", 0f); // Activates beltline clipping
            canopyBlock.SetFloat("_ClipInvert", 1f); // Keep upper canopy only (windshield + roof)
            renderer.SetPropertyBlock(canopyBlock);

            // 3. Add physical collision & impulse to flying canopy
            BoxCollider roofCollider = piece.AddComponent<BoxCollider>();
            roofCollider.size = new Vector3(activeSprite.bounds.size.x * 0.65f,
                Mathf.Max(0.12f, activeSprite.bounds.size.y * 0.45f), 0.18f);
            roofCollider.center = new Vector3(0f, beltlineCutHeight * 0.5f + 0.35f, 0f);

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

            Destroy(piece, 8f);
            gameObject.SetActive(false);
        }
    }
}
