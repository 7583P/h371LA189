using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class DriverHeadImpact : MonoBehaviour
    {
        [SerializeField] private ArticulatedDriver2D driver;
        [SerializeField] private Rigidbody vehicleBody;
        private CrashDetector crashDetector;
        private float nextAllowedImpact;

        public void Assign(ArticulatedDriver2D owner, Rigidbody body)
        {
            driver = owner;
            vehicleBody = body;
            crashDetector = GetComponentInParent<CrashDetector>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!GroundContactFilter.IsGround(other, transform.root) || Time.time < nextAllowedImpact) return;
            nextAllowedImpact = Time.time + .08f;
            float speed = vehicleBody != null ? vehicleBody.GetPointVelocity(transform.position).magnitude : 3f;
            driver?.TriggerHeadImpact(speed);
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
