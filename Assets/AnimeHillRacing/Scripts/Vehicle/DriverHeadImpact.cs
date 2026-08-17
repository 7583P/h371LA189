using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class DriverHeadImpact : MonoBehaviour
    {
        [SerializeField] private ArticulatedDriver2D driver;
        [SerializeField] private Rigidbody vehicleBody;
        private CrashDetector crashDetector;
        private RallyRoofDamage rallyRoofDamage;
        private float nextAllowedImpact;

        public void Assign(ArticulatedDriver2D owner, Rigidbody body)
        {
            driver = owner;
            vehicleBody = body;
            crashDetector = GetComponentInParent<CrashDetector>();
            rallyRoofDamage = GetComponentInParent<RallyRoofDamage>();
        }

        private void OnTriggerEnter(Collider other)
        {
            Transform root = vehicleBody != null ? vehicleBody.transform : transform.root;
            if (!GroundContactFilter.IsGround(other, root) || Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .08f;

            float speed = vehicleBody != null ? vehicleBody.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);

            // If this is a Rally vehicle and the roof is intact, the roof protects the driver.
            rallyRoofDamage ??= GetComponentInParent<RallyRoofDamage>();
            if (rallyRoofDamage != null && !rallyRoofDamage.RoofDetached)
            {
                return; // Intact roof protects driver!
            }

            // Head exposed (roof broken on Rally, or open-cockpit vehicle) -> lethal head impact
            crashDetector ??= GetComponentInParent<CrashDetector>();
            crashDetector?.NotifyDriverHeadGroundImpact();
        }
    }

    public static class GroundContactFilter
    {
        public static bool IsGround(Collider other, Transform vehicleRoot)
        {
            if (other == null || other.isTrigger ||
                (vehicleRoot != null && other.transform.IsChildOf(vehicleRoot))) return false;
            string layer = LayerMask.LayerToName(other.gameObject.layer);
            if (layer == "Terrain" || layer == "Ground") return true;
            string name = other.name.ToLowerInvariant();
            return name.Contains("terrain") || name.Contains("ground") || name.Contains("road") ||
                   name.Contains("track") || name.Contains("shortcut") || name.Contains("ramp");
        }
    }
}
