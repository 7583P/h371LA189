using AnimeHillRacing.Pickups;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class DriverHeadImpact : MonoBehaviour
    {
        [SerializeField] private ArticulatedDriver2D driver;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private Rigidbody2D vehicleBody2D;
        private CrashDetector crashDetector;
        private RallyRoofDamage rallyRoofDamage;
        private VehicleRuntimeBase vehicle;
        private Collider headCollider;
        private Collider2D headCollider2D;
        private float nextAllowedImpact;
        private bool crashTriggered;

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
            if (vehicleBody2D == null) vehicleBody2D = GetComponentInParent<Rigidbody2D>();
            if (vehicle == null) vehicle = GetComponentInParent<VehicleRuntimeBase>();
            if (crashDetector == null) crashDetector = GetComponentInParent<CrashDetector>() ?? FindFirstObjectByType<CrashDetector>();
            if (rallyRoofDamage == null)
            {
                rallyRoofDamage = GetComponentInParent<RallyRoofDamage>();
                VehicleRuntimeBase owner = GetComponentInParent<VehicleRuntimeBase>();
                if (rallyRoofDamage == null && owner != null)
                    rallyRoofDamage = owner.GetComponentInChildren<RallyRoofDamage>(true);
            }
            if (driver == null) driver = GetComponentInParent<ArticulatedDriver2D>();
            if (headCollider == null) headCollider = GetComponent<Collider>();
            if (headCollider2D == null) headCollider2D = GetComponent<Collider2D>();
        }

        public void Assign(ArticulatedDriver2D owner, Rigidbody body, Rigidbody2D body2D = null)
        {
            driver = owner;
            vehicleBody = body;
            vehicleBody2D = body2D;
            EnsureReferences();
        }

        public void ConfigureHeadCollider(SpriteRenderer headRenderer)
        {
            EnsureReferences();
            Sprite sprite = headRenderer != null ? headRenderer.sprite : null;
            if (sprite == null) return;

            Bounds spriteBounds = sprite.bounds;
            float radius = Mathf.Max(.1f, Mathf.Min(spriteBounds.extents.x, spriteBounds.extents.y) * .82f);
            Vector2 center = spriteBounds.center;
            if (headCollider2D is CircleCollider2D circle2D)
            {
                circle2D.offset = center;
                circle2D.radius = radius;
            }
            if (headCollider is SphereCollider sphere)
            {
                sphere.center = new Vector3(center.x, center.y, spriteBounds.center.z);
                sphere.radius = radius;
            }
        }

        private void OnTriggerEnter(Collider other) => ProcessImpact3D(other);
        private void OnTriggerStay(Collider other) => ProcessImpact3D(other);
        private void OnTriggerEnter2D(Collider2D other) => ProcessImpact2D(other);
        private void OnTriggerStay2D(Collider2D other) => ProcessImpact2D(other);
        private void OnCollisionEnter(Collision collision) =>
            ProcessImpact3D(OtherCollider(collision));
        private void OnCollisionStay(Collision collision) =>
            ProcessImpact3D(OtherCollider(collision));
        private void OnCollisionEnter2D(Collision2D collision) =>
            ProcessImpact2D(OtherCollider2D(collision));
        private void OnCollisionStay2D(Collision2D collision) =>
            ProcessImpact2D(OtherCollider2D(collision));

        private Collider OtherCollider(Collision collision)
        {
            return collision != null ? collision.collider : null;
        }

        private Collider2D OtherCollider2D(Collision2D collision)
        {
            if (collision == null) return null;
            return collision.collider == headCollider2D ? collision.otherCollider : collision.collider;
        }

        private void ProcessImpact3D(Collider other)
        {
            EnsureReferences();
            if (crashTriggered) return;
            Transform root = vehicleBody != null ? vehicleBody.transform : (vehicleBody2D != null ? vehicleBody2D.transform : transform.root);
            if (!GroundContactFilter.IsExternalSolid(other, root) ||
                Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .10f;

            float speed = vehicleBody != null ? vehicleBody.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);

            // If this is a Rally vehicle and the roof is intact, the roof protects the driver.
            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            Debug.Log($"[DriverHeadImpact] Exact fatal head contact! Hit: {other.name} | " +
                      $"Speed: {speed:0.0}m/s", this);
            TriggerDriverDown(ResolveDriverDownHeadAngle(other.ClosestPoint(transform.position)));
        }

        private void ProcessImpact2D(Collider2D other)
        {
            EnsureReferences();
            if (crashTriggered) return;
            Transform root = vehicleBody2D != null ? vehicleBody2D.transform : (vehicleBody != null ? vehicleBody.transform : transform.root);
            if (headCollider2D == null || other == headCollider2D ||
                !GroundContactFilter.IsExternalSolid2D(other, root) ||
                Time.time < nextAllowedImpact) return;

            // DriverDown is allowed only while the dedicated head trigger is physically overlapping
            // the reported solid collider. No terrain-height or proximity approximation is accepted.
            ColliderDistance2D contact = headCollider2D.Distance(other);
            if (!contact.isValid || !contact.isOverlapped) return;
            nextAllowedImpact = Time.time + .10f;

            float speed = vehicleBody2D != null ? vehicleBody2D.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);

            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            Debug.Log($"[DriverHeadImpact] Exact 2D fatal head contact! Hit: {other.name} | " +
                      $"Speed: {speed:0.0}m/s", this);
            TriggerDriverDown(ResolveDriverDownHeadAngle(other.ClosestPoint(transform.position)));
        }

        private float ResolveDriverDownHeadAngle(Vector3 contactPoint)
        {
            Transform vehicleTransform = vehicleBody2D != null ? vehicleBody2D.transform
                : vehicleBody != null ? vehicleBody.transform : transform.root;
            Transform facingTransform = driver != null ? driver.transform : vehicleTransform;
            Vector3 pointVelocity = vehicleBody2D != null
                ? (Vector3)vehicleBody2D.GetPointVelocity(transform.position)
                : vehicleBody != null ? vehicleBody.GetPointVelocity(transform.position) : Vector3.zero;
            float longitudinalSpeed = Vector3.Dot(pointVelocity, facingTransform.right);
            if (Mathf.Abs(longitudinalSpeed) < .15f)
            {
                float contactSide = Vector3.Dot(contactPoint - transform.position,
                    facingTransform.right);
                if (Mathf.Abs(contactSide) >= .02f)
                    longitudinalSpeed = contactSide;
                else
                {
                    float angular = vehicleBody2D != null ? vehicleBody2D.angularVelocity
                        : vehicleBody != null
                            ? Vector3.Dot(vehicleBody.angularVelocity, facingTransform.forward) * Mathf.Rad2Deg
                            : 0f;
                    longitudinalSpeed = Mathf.Abs(angular) >= .1f ? -angular : 1f;
                }
            }

            // The driver art faces local +X. Clockwise (-Z) bends the head forward;
            // counter-clockwise (+Z) bends it backward.
            return longitudinalSpeed >= 0f
                ? -ArticulatedDriver2D.DriverDownHeadAngle
                : ArticulatedDriver2D.DriverDownHeadAngle;
        }

        private void TriggerDriverDown(float signedHeadAngle)
        {
            if (crashTriggered) return;
            // Head exposed (roof broken on Rally, or open-cockpit vehicle) -> lethal head impact
            if (crashDetector == null) crashDetector = GetComponentInParent<CrashDetector>();
            if (crashDetector == null) crashDetector = FindFirstObjectByType<CrashDetector>();

            if (crashDetector == null) return;
            crashTriggered = crashDetector.NotifyDriverHeadGroundImpact();
            if (crashTriggered)
                driver?.TriggerDriverDownReaction(signedHeadAngle);
        }
    }

    public static class GroundContactFilter
    {
        public static bool IsGround(Collider other, Transform vehicleRoot)
        {
            if (!IsExternalSolid(other, vehicleRoot)) return false;
            string objName = other.name.ToLowerInvariant();

            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer == "Terrain" || layer == "Ground") return true;

            return objName.Contains("terrain") || objName.Contains("ground") || objName.Contains("road") ||
                   objName.Contains("track") || objName.Contains("shortcut") || objName.Contains("ramp") ||
                   objName.Contains("surface") || objName.Contains("chunk");
        }

        public static bool IsGround2D(Collider2D other, Transform vehicleRoot)
        {
            if (!IsExternalSolid2D(other, vehicleRoot)) return false;
            string objName = other.name.ToLowerInvariant();

            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer == "Terrain" || layer == "Ground") return true;

            return objName.Contains("terrain") || objName.Contains("ground") || objName.Contains("road") ||
                   objName.Contains("track") || objName.Contains("shortcut") || objName.Contains("ramp") ||
                   objName.Contains("surface") || objName.Contains("chunk");
        }

        public static bool IsExternalSolid(Collider other, Transform vehicleRoot)
        {
            if (other == null || other.isTrigger || BelongsToVehicle(other.transform,
                    other.attachedRigidbody != null ? other.attachedRigidbody.transform : null,
                    vehicleRoot)) return false;
            return !IsPickup(other, other.name);
        }

        public static bool IsExternalSolid2D(Collider2D other, Transform vehicleRoot)
        {
            if (other == null || other.isTrigger || BelongsToVehicle(other.transform,
                    other.attachedRigidbody != null ? other.attachedRigidbody.transform : null,
                    vehicleRoot)) return false;
            return !IsPickup(other, other.name);
        }

        private static bool BelongsToVehicle(Transform colliderTransform, Transform bodyTransform,
            Transform vehicleRoot)
        {
            if (vehicleRoot == null) return false;
            Transform vehicleTop = vehicleRoot.root;
            return colliderTransform.IsChildOf(vehicleTop) || colliderTransform.root == vehicleTop ||
                   bodyTransform != null && bodyTransform.IsChildOf(vehicleTop);
        }

        private static bool IsPickup(Component component, string objectName)
        {
            if (component.GetComponent<PickupBase>() != null) return true;
            string lowerName = objectName.ToLowerInvariant();
            return lowerName.Contains("coin") || lowerName.Contains("fuel") ||
                   lowerName.Contains("chest") || lowerName.Contains("pickup");
        }
    }
}
