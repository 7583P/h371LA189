using AnimeHillRacing.Pickups;
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
            ProcessImpact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            ProcessImpact(other);
        }

        private void ProcessImpact(Collider other)
        {
            Transform root = vehicleBody != null ? vehicleBody.transform : transform.root;
            if (!GroundContactFilter.IsGround(other, root) || Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .10f;

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
            if (crashDetector == null)
                crashDetector = FindFirstObjectByType<CrashDetector>();

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
    }
}
