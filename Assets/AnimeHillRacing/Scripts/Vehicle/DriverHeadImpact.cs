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
        private float nextAllowedImpact;

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (vehicleBody == null) vehicleBody = GetComponentInParent<Rigidbody>();
            if (vehicleBody2D == null) vehicleBody2D = GetComponentInParent<Rigidbody2D>();
            if (crashDetector == null) crashDetector = GetComponentInParent<CrashDetector>() ?? FindFirstObjectByType<CrashDetector>();
            if (rallyRoofDamage == null) rallyRoofDamage = GetComponentInParent<RallyRoofDamage>();
            if (driver == null) driver = GetComponentInParent<ArticulatedDriver2D>();
        }

        public void Assign(ArticulatedDriver2D owner, Rigidbody body, Rigidbody2D body2D = null)
        {
            driver = owner;
            vehicleBody = body;
            vehicleBody2D = body2D;
            EnsureReferences();
        }

        private void OnTriggerEnter(Collider other) => ProcessImpact3D(other);
        private void OnTriggerStay(Collider other) => ProcessImpact3D(other);
        private void OnTriggerEnter2D(Collider2D other) => ProcessImpact2D(other);
        private void OnTriggerStay2D(Collider2D other) => ProcessImpact2D(other);

        private void ProcessImpact3D(Collider other)
        {
            EnsureReferences();
            Transform root = vehicleBody != null ? vehicleBody.transform : (vehicleBody2D != null ? vehicleBody2D.transform : transform.root);
            if (!GroundContactFilter.IsGround(other, root) || Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .10f;

            float speed = vehicleBody != null ? vehicleBody.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);

            // Always check real chassis upright orientation (NOT the floppy animated head sprite)
            Transform chassisTransform = vehicleBody != null ? vehicleBody.transform : transform.root;
            Vector3 chassisUp = chassisTransform.up;
            float uprightDot = Vector3.Dot(chassisUp, Vector3.up);

            // If the vehicle is upright on its wheels (climbing slopes, driving over hills without rolling over),
            // touching or grazing the terrain slope behind the seat is NEVER a fatal crash.
            if (uprightDot > -0.05f)
            {
                return; // Vehicle is upright on its wheels!
            }

            // If this is a Rally vehicle and the roof is intact, the roof protects the driver.
            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            Debug.Log($"[DriverHeadImpact] Overturned fatal head impact! Hit: {other.name} | ChassisUprightDot: {uprightDot:0.00} | Speed: {speed:0.0}m/s", this);
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

            Transform chassisTransform = vehicleBody2D != null ? vehicleBody2D.transform : transform.root;
            Vector2 chassisUp2D = (Vector2)chassisTransform.up;
            float uprightDot = Vector2.Dot(chassisUp2D, Vector2.up);

            if (uprightDot > -0.05f)
            {
                return; // Vehicle is upright on its wheels!
            }

            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            Debug.Log($"[DriverHeadImpact] 2D Overturned fatal head impact! Hit: {other.name} | ChassisUprightDot: {uprightDot:0.00} | Speed: {speed:0.0}m/s", this);
            TriggerDriverDown();
        }

        private void TriggerDriverDown()
        {
            // Head exposed (roof broken on Rally, or open-cockpit vehicle) -> lethal head impact
            if (crashDetector == null) crashDetector = GetComponentInParent<CrashDetector>();
            if (crashDetector == null) crashDetector = FindFirstObjectByType<CrashDetector>();

            crashDetector?.NotifyDriverHeadGroundImpact();
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
