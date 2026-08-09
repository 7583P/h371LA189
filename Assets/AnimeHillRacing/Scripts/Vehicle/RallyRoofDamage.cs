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
        [SerializeField, Min(0f)] private float separationImpulse = 1.8f;

        public bool RoofDetached { get; private set; }

        public void Assign(VehicleDefinition vehicleDefinition, Rigidbody body, SpriteRenderer[] renderers,
            Transform targetVisualRoot)
        {
            definition = vehicleDefinition;
            vehicleBody = body;
            bodyRenderers = renderers;
            visualRoot = targetVisualRoot;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (RoofDetached || definition == null || definition.archetype != VehicleArchetype.Rally ||
                !GroundContactFilter.IsGround(other, transform.root)) return;
            DetachRoof();
        }

        public void DetachRoof()
        {
            if (RoofDetached || definition == null || definition.damagedBodySprite == null ||
                definition.detachableRoofSprite == null) return;
            RoofDetached = true;
            if (bodyRenderers != null)
                for (int i = 0; i < bodyRenderers.Length; i++)
                    if (bodyRenderers[i] != null) bodyRenderers[i].sprite = definition.damagedBodySprite;

            GameObject piece = new GameObject("Rally Detached Roof - Physical");
            piece.transform.position = transform.position;
            piece.transform.rotation = visualRoot != null ? visualRoot.rotation : transform.rotation;
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = definition.detachableRoofSprite;
            renderer.sortingOrder = 13;
            float targetWidth = Mathf.Max(1f, definition.visualWidth);
            float sourceWidth = Mathf.Max(.001f, definition.detachableRoofSprite.bounds.size.x);
            piece.transform.localScale = Vector3.one * (targetWidth / sourceWidth);
            BoxCollider roofCollider = piece.AddComponent<BoxCollider>();
            roofCollider.size = new Vector3(definition.detachableRoofSprite.bounds.size.x,
                Mathf.Max(.08f, definition.detachableRoofSprite.bounds.size.y), .12f);
            Rigidbody roofBody = piece.AddComponent<Rigidbody>();
            roofBody.mass = detachedRoofMass;
            roofBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (vehicleBody != null)
            {
                roofBody.linearVelocity = vehicleBody.GetPointVelocity(transform.position) +
                                          Vector3.up * separationImpulse;
                roofBody.angularVelocity = vehicleBody.angularVelocity + new Vector3(0f, 0f, 1.8f);
            }
            Destroy(piece, 8f);
            gameObject.SetActive(false);
        }
    }
}
