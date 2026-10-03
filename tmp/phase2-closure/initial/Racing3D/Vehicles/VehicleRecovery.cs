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
        private float offTrackTimer;
        private TrackSplineGraph graph;
        public int RecoveryCount { get; private set; }

        public bool IsRecovering => recoveryPenaltyRemaining > 0f;
        public float PenaltySeconds => penaltySeconds;
        public event Action<Vector3> OnVehicleRecovered;

        private void Awake()
        {
            vehiclePhysics = GetComponent<RacingVehiclePhysics>();
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
        }

        private void Start() { graph = FindFirstObjectByType<TrackSplineGraph>(); }

        private void Update()
        {
            if (vehiclePhysics == null || !vehiclePhysics.RaceEnabled) return;
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
            CheckOutsideCourse(Time.deltaTime);
        }

        private void CheckOutsideCourse(float dt)
        {
            if (graph == null || graph.NodeCount == 0 || IsRecovering) return;
            graph.ProjectPosition(transform.position, out _, out float distance, out float lateral);
            TrackNode road = graph.SampleDistance(distance);
            // A supported canyon basin must not keep a fallen racer driving below the course.
            // The delay leaves room for the authored leap and legitimate dirt shelves.
            bool belowRoad = transform.position.y < road.Position.y - 5f;
            bool farFromRoad = Mathf.Abs(lateral) > road.TrackWidth * .5f + 16f;
            bool onShortcut = false;
            if (farFromRoad && !belowRoad)
                foreach (var route in graph.Shortcuts)
                    for (int i = 1; i < route.pathPoints.Length; i++)
                    {
                        Vector3 a = route.pathPoints[i - 1], delta = route.pathPoints[i] - a;
                        float t = Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / Mathf.Max(.01f, delta.sqrMagnitude));
                        if (Vector3.Distance(transform.position, a + delta * t) < 7f) onShortcut = true;
                    }
            offTrackTimer = belowRoad || (farFromRoad && !onShortcut) ? offTrackTimer + dt : 0f;
            if (offTrackTimer > 1.4f) TriggerRecovery("Outside the course");
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
            if (vehiclePhysics == null || IsRecovering) return;

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
            if (vehiclePhysics == null || IsRecovering || !vehiclePhysics.RaceEnabled) return;

            Rigidbody body = vehiclePhysics.Body;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = lastSafePosition;
                body.rotation = lastSafeRotation;
            }

            transform.position = lastSafePosition;
            transform.rotation = lastSafeRotation;

            vehiclePhysics.ResetVelocities();
            flippedTimer = 0f;
            stuckTimer = 0f;
            offTrackTimer = 0f;
            recoveryPenaltyRemaining = penaltySeconds;
            RecoveryCount++;
            if (vehiclePhysics.GetComponent<PlayerRacingInput>() != null) FindFirstObjectByType<RacingCamera3D>()?.SnapToTarget();

            OnVehicleRecovered?.Invoke(lastSafePosition);
        }
    }
}
