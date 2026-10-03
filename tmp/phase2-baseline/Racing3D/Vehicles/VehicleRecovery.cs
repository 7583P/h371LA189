using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class VehicleRecovery : MonoBehaviour
    {
        [Header("Thresholds")]
        [SerializeField] private float fallKillPlaneY = -30f;
        [SerializeField] private float flippedDurationThreshold = 1.8f;
        [SerializeField] private float stuckDurationThreshold = 4.0f;
        [SerializeField] private float penaltySeconds = 2.5f;

        private RacingVehiclePhysics vehiclePhysics;
        private Vector3 lastSafePosition;
        private Quaternion lastSafeRotation = Quaternion.identity;
        private float flippedTimer;
        private float stuckTimer;
        private float recoveryPenaltyRemaining;

        public bool IsRecovering => recoveryPenaltyRemaining > 0f;
        public float PenaltySeconds => penaltySeconds;
        public event Action<Vector3> OnVehicleRecovered;

        private void Awake()
        {
            vehiclePhysics = GetComponent<RacingVehiclePhysics>();
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
        }

        private void Update()
        {
            if (recoveryPenaltyRemaining > 0f)
            {
                recoveryPenaltyRemaining -= Time.deltaTime;
                if (recoveryPenaltyRemaining <= 0f)
                {
                    recoveryPenaltyRemaining = 0f;
                }
            }

            CheckFlipped(Time.deltaTime);
            CheckStuck(Time.deltaTime);
            CheckFallPlane();
        }

        public void SetLastSafeTransform(Vector3 position, Quaternion rotation)
        {
            lastSafePosition = position;
            lastSafeRotation = rotation;
        }

        private void CheckFlipped(float dt)
        {
            // Up vector pointing downward (pitch/roll > 100 degrees)
            if (Vector3.Dot(transform.up, Vector3.up) < -0.2f)
            {
                flippedTimer += dt;
                if (flippedTimer >= flippedDurationThreshold)
                {
                    TriggerRecovery("Vehicle flipped");
                }
            }
            else
            {
                flippedTimer = 0f;
            }
        }

        private void CheckStuck(float dt)
        {
            if (vehiclePhysics == null) return;

            VehicleCommands cmd = vehiclePhysics.LastCommands;
            // Throttle pressed but barely moving and grounded
            if (cmd.Throttle > 0.5f && vehiclePhysics.SpeedMps < 0.5f && vehiclePhysics.IsGrounded)
            {
                stuckTimer += dt;
                if (stuckTimer >= stuckDurationThreshold)
                {
                    TriggerRecovery("Vehicle stuck");
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        private void CheckFallPlane()
        {
            if (transform.position.y < fallKillPlaneY)
            {
                TriggerRecovery("Fell off track");
            }
        }

        public void TriggerRecovery(string reason = "")
        {
            if (vehiclePhysics == null) return;

            Rigidbody body = vehiclePhysics.Body;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = lastSafePosition + Vector3.up * 1.0f;
                body.rotation = lastSafeRotation;
            }

            transform.position = lastSafePosition + Vector3.up * 1.0f;
            transform.rotation = lastSafeRotation;

            vehiclePhysics.ResetVelocities();
            flippedTimer = 0f;
            stuckTimer = 0f;
            recoveryPenaltyRemaining = penaltySeconds;

            OnVehicleRecovered?.Invoke(lastSafePosition);
        }
    }
}
