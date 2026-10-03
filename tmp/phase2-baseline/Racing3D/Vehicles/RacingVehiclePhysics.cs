using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum MiniTurboStage
    {
        None = 0,
        MiniTurbo = 1,      // Blue sparks
        SuperMiniTurbo = 2, // Orange sparks
        UltraMiniTurbo = 3  // Purple sparks
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class RacingVehiclePhysics : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string vehicleId = "car-01";
        [SerializeField] private string wheelId = "wheel-default";

        [Header("Ground Detection")]
        [SerializeField] private float raycastDistance = 1.1f;
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private Transform[] wheelRaycastOrigins;

        private Rigidbody body;
        private IVehicleCommandProvider commandProvider;
        private FinalRacingStats stats;

        // Runtime states
        private bool isGrounded;
        private Vector3 groundNormal = Vector3.up;
        private bool isOffroad;

        // Drift system
        private bool isDrifting;
        private int driftDirection; // -1 = left, +1 = right
        private float driftTime;
        private MiniTurboStage currentMiniTurboStage;

        // Boost system
        private float boostTimer;
        private float boostSpeedMultiplier = 1f;

        // Spin-out system (item hit)
        private float spinoutTimer;

        // Visual smoothing
        private float currentSteeringAngle;
        private float currentVisualRoll;

        public Rigidbody Body => body;
        public FinalRacingStats Stats => stats;
        public float SpeedMps => body != null ? body.linearVelocity.magnitude : 0f;
        public float SpeedKmh => SpeedMps * 3.6f;
        public float ForwardSpeedMps => body != null ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;
        public float ForwardSpeedKmh => ForwardSpeedMps * 3.6f;
        public bool IsGrounded => isGrounded;
        public bool IsDrifting => isDrifting;
        public MiniTurboStage CurrentMiniTurboStage => currentMiniTurboStage;
        public bool IsBoosting => boostTimer > 0f;
        public bool IsSpunOut => spinoutTimer > 0f;
        public bool IsOffroad => isOffroad;
        public VehicleCommands LastCommands { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 950f;
            body.linearDamping = 0.15f;
            body.angularDamping = 2.5f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            // Lower center of mass for rollover stability
            body.centerOfMass = new Vector3(0f, -0.4f, 0f);

            ApplyStats(RacingStatsCalculator.Calculate(vehicleId, wheelId));
        }

        public void Initialize(string vId, string wId, IVehicleCommandProvider provider)
        {
            vehicleId = vId;
            wheelId = wId;
            commandProvider = provider;
            ApplyStats(RacingStatsCalculator.Calculate(vehicleId, wheelId));
        }

        public void SetCommandProvider(IVehicleCommandProvider provider)
        {
            commandProvider = provider;
        }

        public void ApplyStats(FinalRacingStats newStats)
        {
            stats = newStats;
            if (body != null)
            {
                body.mass = stats.WeightKg;
            }
        }

        private void FixedUpdate()
        {
            if (body == null) return;

            // 1. Fetch commands from shared provider (Human or AI)
            VehicleCommands cmd = commandProvider != null ? commandProvider.GetCommands() : VehicleCommands.Idle;
            LastCommands = cmd;

            // 2. Evaluate ground contact
            CheckGroundStatus();

            // 3. Update timers (boost, spin-out)
            UpdateTimers(Time.fixedDeltaTime);

            // 4. If spun out from item impact, handle spin physics
            if (spinoutTimer > 0f)
            {
                ApplySpinoutPhysics();
                return;
            }

            // 5. Update drift mechanics
            UpdateDrift(cmd, Time.fixedDeltaTime);

            // 6. Apply drive forces (throttle, brake, reverse)
            ApplyDriveForces(cmd);

            // 7. Apply steering and cornering friction
            ApplySteeringAndFriction(cmd, Time.fixedDeltaTime);

            // 8. Apply stability downforce and anti-roll
            ApplyStabilityForces();
        }

        private void CheckGroundStatus()
        {
            Vector3 origin = transform.position + Vector3.up * 0.4f;
            Ray ray = new Ray(origin, -transform.up);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance + 0.4f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                isGrounded = true;
                groundNormal = hit.normal;

                // Check surface tag or material for offroad
                isOffroad = hit.collider.CompareTag("Offroad");
            }
            else
            {
                isGrounded = false;
                groundNormal = Vector3.up;
                isOffroad = false;
            }
        }

        private void UpdateTimers(float dt)
        {
            if (boostTimer > 0f)
            {
                boostTimer -= dt;
                if (boostTimer <= 0f)
                {
                    boostTimer = 0f;
                    boostSpeedMultiplier = 1f;
                }
            }

            if (spinoutTimer > 0f)
            {
                spinoutTimer -= dt;
                if (spinoutTimer <= 0f) spinoutTimer = 0f;
            }
        }

        private void UpdateDrift(VehicleCommands cmd, float dt)
        {
            if (!isGrounded)
            {
                // Cannot initiate or hold drift in mid-air
                if (isDrifting) EndDrift(false);
                return;
            }

            // Initiate drift: drift button pressed while actively steering
            if (cmd.Drift && !isDrifting && Mathf.Abs(cmd.Steering) > 0.2f && ForwardSpeedKmh > 35f)
            {
                isDrifting = true;
                driftDirection = cmd.Steering > 0 ? 1 : -1;
                driftTime = 0f;
                currentMiniTurboStage = MiniTurboStage.None;

                // Small hop upon initiating drift
                body.AddForce(Vector3.up * 2.8f, ForceMode.VelocityChange);
            }
            else if (isDrifting)
            {
                if (!cmd.Drift || ForwardSpeedKmh < 20f)
                {
                    // Released drift -> release mini-turbo boost!
                    EndDrift(true);
                }
                else
                {
                    // Accumulate drift time scaled by vehicle MiniTurbo stat
                    driftTime += dt * stats.MiniTurboMultiplier;

                    if (driftTime >= 3.2f)
                        currentMiniTurboStage = MiniTurboStage.UltraMiniTurbo;
                    else if (driftTime >= 1.8f)
                        currentMiniTurboStage = MiniTurboStage.SuperMiniTurbo;
                    else if (driftTime >= 0.8f)
                        currentMiniTurboStage = MiniTurboStage.MiniTurbo;
                    else
                        currentMiniTurboStage = MiniTurboStage.None;
                }
            }
        }

        private void EndDrift(bool triggerBoost)
        {
            if (triggerBoost && currentMiniTurboStage != MiniTurboStage.None)
            {
                float boostDuration;
                float boostPower;

                switch (currentMiniTurboStage)
                {
                    case MiniTurboStage.MiniTurbo:
                        boostDuration = 0.8f;
                        boostPower = 1.15f;
                        break;
                    case MiniTurboStage.SuperMiniTurbo:
                        boostDuration = 1.4f;
                        boostPower = 1.25f;
                        break;
                    case MiniTurboStage.UltraMiniTurbo:
                        boostDuration = 2.0f;
                        boostPower = 1.35f;
                        break;
                    default:
                        boostDuration = 0f;
                        boostPower = 1f;
                        break;
                }

                ApplySpeedBoost(boostPower, boostDuration);
            }

            isDrifting = false;
            driftDirection = 0;
            driftTime = 0f;
            currentMiniTurboStage = MiniTurboStage.None;
        }

        private void ApplyDriveForces(VehicleCommands cmd)
        {
            if (!isGrounded) return;

            float currentSpeed = ForwardSpeedMps;
            float targetMaxSpeedKmh = stats.SpeedKmh;

            // Apply offroad speed penalty (unless currently boosting)
            if (isOffroad && boostTimer <= 0f)
            {
                targetMaxSpeedKmh *= stats.OffroadRetention;
            }

            // Apply boost multiplier
            if (boostTimer > 0f)
            {
                targetMaxSpeedKmh *= boostSpeedMultiplier;
            }

            float targetMaxSpeedMps = targetMaxSpeedKmh / 3.6f;

            if (cmd.Throttle > 0.01f)
            {
                if (currentSpeed < targetMaxSpeedMps)
                {
                    // Acceleration force scaled by throttle
                    float accelForce = stats.AccelerationMps2 * cmd.Throttle;
                    // Taper force near top speed
                    float speedRatio = Mathf.Clamp01(currentSpeed / targetMaxSpeedMps);
                    float taper = 1.0f - (speedRatio * speedRatio * 0.4f);

                    Vector3 forwardDrive = transform.forward * (accelForce * taper * body.mass);
                    body.AddForce(forwardDrive, ForceMode.Force);
                }
            }

            if (cmd.Brake > 0.01f)
            {
                if (currentSpeed > 1f)
                {
                    // Braking
                    float brakeForce = stats.AccelerationMps2 * 1.5f * cmd.Brake * body.mass;
                    body.AddForce(-transform.forward * brakeForce, ForceMode.Force);
                }
                else if (currentSpeed >= -10f)
                {
                    // Reversing
                    float reverseForce = stats.AccelerationMps2 * 0.5f * cmd.Brake * body.mass;
                    body.AddForce(-transform.forward * reverseForce, ForceMode.Force);
                }
            }
        }

        private void ApplySteeringAndFriction(VehicleCommands cmd, float dt)
        {
            float forwardSpeed = ForwardSpeedMps;

            if (isGrounded)
            {
                // Determine effective steering input
                float effectiveSteering = cmd.Steering;

                if (isDrifting)
                {
                    // In drift, base turn is locked in drift direction, counter-steer modulates angle
                    effectiveSteering = driftDirection * 0.85f + (cmd.Steering * 0.35f);
                    effectiveSteering *= stats.DriftMultiplier;
                }

                // Yaw rotation rate scales with speed
                float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 10f);
                float yawRate = effectiveSteering * stats.HandlingRadPerSec * speedFactor * Mathf.Rad2Deg;

                Quaternion turnRotation = Quaternion.Euler(0f, yawRate * dt, 0f);
                body.MoveRotation(body.rotation * turnRotation);

                // Lateral friction: cancel sideways sliding velocity
                Vector3 lateralDir = transform.right;
                float lateralSpeed = Vector3.Dot(body.linearVelocity, lateralDir);

                // During drift, allow controlled slip
                float gripFactor = isDrifting ? (stats.LateralGrip * 0.35f) : stats.LateralGrip;
                Vector3 frictionImpulse = -lateralDir * (lateralSpeed * Mathf.Clamp01(gripFactor * 0.85f));
                body.AddForce(frictionImpulse, ForceMode.VelocityChange);
            }
            else
            {
                // In-air pitch control and passive righting
                if (Mathf.Abs(cmd.Pitch) > 0.05f)
                {
                    body.AddTorque(transform.right * (cmd.Pitch * 8000f), ForceMode.Force);
                }

                // Passive upright stabilization
                Vector3 predictedUp = Quaternion.AngleAxis(
                    body.angularVelocity.magnitude * Mathf.Rad2Deg * 0.1f / 2f,
                    body.angularVelocity
                ) * transform.up;

                Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
                body.AddTorque(torqueVector * (stats.Stability * 4500f), ForceMode.Force);
            }
        }

        private void ApplyStabilityForces()
        {
            if (isGrounded)
            {
                // Aerodynamic downforce proportional to speed
                float downforce = (SpeedMps * SpeedMps) * 0.45f * stats.Stability;
                body.AddForce(-groundNormal * downforce, ForceMode.Force);

                // Align chassis slightly to terrain surface normal
                Vector3 currentUp = transform.up;
                Vector3 torqueAxis = Vector3.Cross(currentUp, groundNormal);
                body.AddTorque(torqueAxis * 6000f, ForceMode.Force);
            }
        }

        private void ApplySpinoutPhysics()
        {
            // Rapid yaw spin + deceleration
            body.AddTorque(Vector3.up * 8000f, ForceMode.Force);
            body.linearVelocity = Vector3.Lerp(body.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2.5f);
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            boostSpeedMultiplier = Mathf.Max(boostSpeedMultiplier, multiplier);
            boostTimer = Mathf.Max(boostTimer, duration);
        }

        public void TriggerSpinout(float duration = 1.2f)
        {
            spinoutTimer = duration;
            EndDrift(false);
        }

        public void ResetVelocities()
        {
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            isDrifting = false;
            boostTimer = 0f;
            spinoutTimer = 0f;
        }
    }
}
