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
        [SerializeField] private float penaltySeconds = 0.85f;

        private RacingVehiclePhysics vehiclePhysics;
        private Vector3 lastSafePosition;
        private Quaternion lastSafeRotation = Quaternion.identity;
        private float flippedTimer;
        private float stuckTimer;
        private float recoveryPenaltyRemaining;
        private float offTrackTimer;
        private TrackSplineGraph graph;
        public int RecoveryCount { get; private set; }
        public string LastRecoveryReason { get; private set; }

        public bool IsRecovering => recoveryPenaltyRemaining > 0f;
        public bool IsFallingIntoVoid => offTrackTimer > 0.15f || (transform.position.y < (lastSafePosition.y - 7.5f));
        public float PenaltySeconds => penaltySeconds;
        public event Action<Vector3> OnVehicleRecovered;
        public event Action<string> OnRecoveryRequested;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (vehiclePhysics == null) vehiclePhysics = GetComponent<RacingVehiclePhysics>();
            if (lastSafePosition == Vector3.zero && transform.position != Vector3.zero)
            {
                lastSafePosition = transform.position;
                lastSafeRotation = transform.rotation;
            }
        }

        private void Start() { EnsureComponents(); graph = FindFirstObjectByType<TrackSplineGraph>(); }

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

            bool onShortcut = false;
            if (graph.Shortcuts != null)
            {
                foreach (var route in graph.Shortcuts)
                {
                    if (route.pathPoints == null || route.pathPoints.Length < 2) continue;
                    for (int i = 1; i < route.pathPoints.Length; i++)
                    {
                        Vector3 a = route.pathPoints[i - 1], delta = route.pathPoints[i] - a;
                        float sqrMag = delta.sqrMagnitude;
                        if (sqrMag < 0.01f) continue;
                        float t = Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / sqrMag);
                        if (Vector3.Distance(transform.position, a + delta * t) < 10f)
                        {
                            onShortcut = true;
                            break;
                        }
                    }
                    if (onShortcut) break;
                }
            }

            bool belowRoad = !onShortcut && (transform.position.y < road.Position.y - 6f);
            bool farFromRoad = !onShortcut && (Mathf.Abs(lateral) > road.TrackWidth * 0.5f + 18f);

            offTrackTimer = (belowRoad || farFromRoad) ? offTrackTimer + dt : 0f;
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
            bool tryingToDrive = cmd.Throttle > 0.25f || cmd.Brake > 0.25f;
            if (tryingToDrive && vehiclePhysics.SpeedMps < 0.8f && vehiclePhysics.IsGrounded)
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
            EnsureComponents();
            if (vehiclePhysics == null || IsRecovering || !vehiclePhysics.RaceEnabled) return;

            LastRecoveryReason = reason;
            OnRecoveryRequested?.Invoke(reason);

            Vector3 spawnPos = lastSafePosition + Vector3.up * 0.35f;
            Quaternion spawnRot = lastSafeRotation;
            Vector3 forwardMomentum = spawnRot * Vector3.forward * 11.5f;

            Rigidbody body = vehiclePhysics.Body;
            if (body != null)
            {
                body.linearVelocity = forwardMomentum;
                body.angularVelocity = Vector3.zero;
                body.position = spawnPos;
                body.rotation = spawnRot;
            }

            transform.position = spawnPos;
            transform.rotation = spawnRot;

            vehiclePhysics.ResetVelocities();
            if (body != null) body.linearVelocity = forwardMomentum;
            vehiclePhysics.ApplySpeedBoost(1.15f, 0.75f);

            flippedTimer = 0f;
            stuckTimer = 0f;
            offTrackTimer = 0f;
            recoveryPenaltyRemaining = penaltySeconds;
            RecoveryCount++;
            if (vehiclePhysics.GetComponent<PlayerRacingInput>() != null) FindFirstObjectByType<RacingCamera3D>()?.SnapToTarget();

            OnVehicleRecovered?.Invoke(spawnPos);
        }
    }
}
