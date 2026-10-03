using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    /// <summary>A visible bridge kicker with the same impulse for every racer.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RacingJumpPad : MonoBehaviour
    {
        private readonly RacingVehiclePhysics[] racers = new RacingVehiclePhysics[12];
        private readonly float[] lastLaunch = new float[12];
        public int LaunchCount { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            var vehicle = other.GetComponentInParent<RacingVehiclePhysics>();
            if (vehicle == null || !vehicle.RaceEnabled || vehicle.ForwardSpeedMps < 4f) return;
            if (Vector3.Dot(vehicle.transform.forward, transform.forward) < .5f) return;
            int slot = -1;
            for (int i = 0; i < racers.Length; i++)
            {
                if (racers[i] == vehicle) { slot = i; break; }
                if (racers[i] == null && slot < 0) slot = i;
            }
            if (slot < 0 || (racers[slot] == vehicle && Time.time - lastLaunch[slot] < 3f)) return;
            racers[slot] = vehicle;
            lastLaunch[slot] = Time.time;
            float impulse = Mathf.Max(0f, 6.5f - vehicle.Body.linearVelocity.y);
            vehicle.Body.AddForce(Vector3.up * impulse, ForceMode.VelocityChange);
            float currentFwd = Vector3.Dot(vehicle.Body.linearVelocity, transform.forward);
            if (currentFwd < 18f)
            {
                vehicle.Body.AddForce(transform.forward * (18f - currentFwd), ForceMode.VelocityChange);
            }
            LaunchCount++;
            RacingVfxPool.Burst(vehicle.transform.position, RacingDesign.Cyan, 6);
        }
    }
}
