using AnimeHillRacing.Pickups;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class DriverHeadImpact : MonoBehaviour
    {
        [SerializeField] private ArticulatedDriver2D driver;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private Rigidbody2D vehicleBody2D;
        [SerializeField, Range(-.25f, .65f)] private float fatalSurfaceUpDot = .35f;
        private CrashDetector crashDetector;
        private RallyRoofDamage rallyRoofDamage;
        private VehicleRuntimeBase vehicle;
        private Collider headCollider;
        private Collider2D headCollider2D;
        private float nextAllowedImpact;
        private bool crashTriggered;
        private readonly Collider[] overlapResults3D = new Collider[12];
        private readonly Collider2D[] overlapResults2D = new Collider2D[12];

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

        private void FixedUpdate()
        {
            if (crashTriggered || Time.time < nextAllowedImpact) return;
            EnsureReferences();

            // Trigger callbacks can be skipped when a fast rotating head begins a physics step already
            // overlapping a thin EdgeCollider2D. Query the head volume as a fallback so a resting or
            // tunnelling head/terrain overlap is still recognized on the following physics step.
            if (vehicleBody2D != null && headCollider2D != null && headCollider2D.enabled)
            {
                ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
                int count = headCollider2D.Overlap(filter, overlapResults2D);
                for (int i = 0; i < count; i++)
                {
                    Collider2D other = overlapResults2D[i];
                    if (!GroundContactFilter.IsGround2D(other, vehicleBody2D.transform)) continue;
                    ProcessImpact2D(other);
                    break;
                }
                TryProcessSurfaceProximity();
                return;
            }

            if (vehicleBody != null && headCollider != null && headCollider.enabled)
            {
                Bounds bounds = headCollider.bounds;
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                int count = Physics.OverlapSphereNonAlloc(bounds.center, radius, overlapResults3D,
                    ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider other = overlapResults3D[i];
                    if (!GroundContactFilter.IsGround(other, vehicleBody.transform)) continue;
                    ProcessImpact3D(other);
                    break;
                }
            }

            TryProcessSurfaceProximity();
        }

        private void TryProcessSurfaceProximity()
        {
            if (crashTriggered || SurfaceRelativeUpDot() > fatalSurfaceUpDot) return;
            TerrainManager terrain = TerrainManager.Instance;
            if (terrain == null) return;

            Bounds bounds;
            if (headCollider2D != null && headCollider2D.enabled)
                bounds = headCollider2D.bounds;
            else if (headCollider != null && headCollider.enabled)
                bounds = headCollider.bounds;
            else
                return;

            if (!terrain.TrySampleAuthoritativeSurface(bounds.center.x, out float surfaceY, out _))
                return;

            // This fallback must confirm near-contact only. The previous wheel-radius allowance could
            // report DRIVER DOWN while the visible head was still far above the terrain.
            float tolerance = Mathf.Max(.01f, Physics2D.defaultContactOffset * 1.25f);
            float verticalDistance = bounds.min.y - surfaceY;
            if (!HasAuthoritativeSurfaceContact(bounds.min.y, surfaceY, tolerance)) return;

            nextAllowedImpact = Time.time + .10f;
            float speed = vehicleBody2D != null
                ? vehicleBody2D.GetPointVelocity(bounds.center).magnitude
                : vehicleBody != null ? vehicleBody.GetPointVelocity(bounds.center).magnitude : 0f;
            driver?.TriggerHeadImpact(speed);

            Debug.Log($"[DriverHeadImpact] Fatal authoritative-surface proximity | " +
                      $"Vertical distance: {verticalDistance:0.000} | Tolerance: {tolerance:0.000} | " +
                      $"Speed: {speed:0.0}m/s", this);
            TriggerDriverDown();
        }

        public static bool HasAuthoritativeSurfaceContact(float headBottomY, float surfaceY,
            float contactTolerance)
        {
            if (float.IsNaN(headBottomY) || float.IsNaN(surfaceY) ||
                float.IsInfinity(headBottomY) || float.IsInfinity(surfaceY)) return false;
            return headBottomY - surfaceY <= Mathf.Max(.005f, contactTolerance);
        }

        private void ProcessImpact3D(Collider other)
        {
            EnsureReferences();
            Transform root = vehicleBody != null ? vehicleBody.transform : (vehicleBody2D != null ? vehicleBody2D.transform : transform.root);
            if (!GroundContactFilter.IsGround(other, root) || Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .10f;

            float speed = vehicleBody != null ? vehicleBody.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);

            float surfaceUpDot = SurfaceRelativeUpDot();
            if (surfaceUpDot > fatalSurfaceUpDot) return;

            // If this is a Rally vehicle and the roof is intact, the roof protects the driver.
            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            Debug.Log($"[DriverHeadImpact] Fatal head impact! Hit: {other.name} | SurfaceUpDot: {surfaceUpDot:0.00} | Speed: {speed:0.0}m/s", this);
            TriggerDriverDown();
        }

        private void ProcessImpact2D(Collider2D other)
        {
            EnsureReferences();
            Transform root = vehicleBody2D != null ? vehicleBody2D.transform : (vehicleBody != null ? vehicleBody.transform : transform.root);
            if (!GroundContactFilter.IsGround2D(other, root) || Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .10f;

            float speed = vehicleBody2D != null ? vehicleBody2D.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);

            float surfaceUpDot = SurfaceRelativeUpDot();
            if (surfaceUpDot > fatalSurfaceUpDot) return;

            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            Debug.Log($"[DriverHeadImpact] 2D fatal head impact! Hit: {other.name} | SurfaceUpDot: {surfaceUpDot:0.00} | Speed: {speed:0.0}m/s", this);
            TriggerDriverDown();
        }

        private float SurfaceRelativeUpDot()
        {
            Transform chassisTransform = vehicleBody2D != null ? vehicleBody2D.transform
                : vehicleBody != null ? vehicleBody.transform : transform.root;
            Vector3 surfaceUp = Vector3.up;
            TerrainManager terrain = TerrainManager.Instance;
            if (terrain != null && terrain.TrySampleAuthoritativeSurface(transform.position.x,
                    out _, out float slopeDegrees))
                surfaceUp = Quaternion.Euler(0f, 0f, slopeDegrees) * Vector3.up;
            return Vector3.Dot(chassisTransform.up.normalized, surfaceUp.normalized);
        }

        private void TriggerDriverDown()
        {
            if (crashTriggered) return;
            // Head exposed (roof broken on Rally, or open-cockpit vehicle) -> lethal head impact
            if (crashDetector == null) crashDetector = GetComponentInParent<CrashDetector>();
            if (crashDetector == null) crashDetector = FindFirstObjectByType<CrashDetector>();

            if (crashDetector == null) return;
            crashTriggered = crashDetector.NotifyDriverHeadGroundImpact();
        }
    }

    public static class GroundContactFilter
    {
        public static bool IsGround(Collider other, Transform vehicleRoot)
        {
            if (other == null || other.isTrigger) return false;

            // Ignore ANY collider belonging to the vehicle hierarchy (chassis, wheels, scarf, accessories, etc.)
            if (vehicleRoot != null)
            {
                Transform vehicleTop = vehicleRoot.root;
                if (other.transform.IsChildOf(vehicleTop) || other.transform.root == vehicleTop)
                    return false;

                if (other.attachedRigidbody != null && other.attachedRigidbody.transform.IsChildOf(vehicleTop))
                    return false;
            }

            // Ignore pickups, collectibles, triggers
            if (other.GetComponent<PickupBase>() != null)
                return false;

            string objName = other.name.ToLowerInvariant();
            if (objName.Contains("coin") || objName.Contains("fuel") || objName.Contains("chest") || objName.Contains("pickup"))
                return false;

            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer == "Terrain" || layer == "Ground") return true;

            return objName.Contains("terrain") || objName.Contains("ground") || objName.Contains("road") ||
                   objName.Contains("track") || objName.Contains("shortcut") || objName.Contains("ramp") ||
                   objName.Contains("surface") || objName.Contains("chunk");
        }

        public static bool IsGround2D(Collider2D other, Transform vehicleRoot)
        {
            if (other == null || other.isTrigger) return false;

            if (vehicleRoot != null)
            {
                Transform vehicleTop = vehicleRoot.root;
                if (other.transform.IsChildOf(vehicleTop) || other.transform.root == vehicleTop)
                    return false;

                if (other.attachedRigidbody != null && other.attachedRigidbody.transform.IsChildOf(vehicleTop))
                    return false;
            }

            if (other.GetComponent<PickupBase>() != null)
                return false;

            string objName = other.name.ToLowerInvariant();
            if (objName.Contains("coin") || objName.Contains("fuel") || objName.Contains("chest") || objName.Contains("pickup"))
                return false;

            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer == "Terrain" || layer == "Ground") return true;

            return objName.Contains("terrain") || objName.Contains("ground") || objName.Contains("road") ||
                   objName.Contains("track") || objName.Contains("shortcut") || objName.Contains("ramp") ||
                   objName.Contains("surface") || objName.Contains("chunk");
        }
    }
}
