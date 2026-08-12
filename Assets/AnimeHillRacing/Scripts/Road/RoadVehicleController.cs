using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Road
{
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(Rigidbody), typeof(EngineController), typeof(TransmissionController))]
    [RequireComponent(typeof(FuelSystem))]
    public sealed class RoadVehicleController : VehicleRuntimeBase
    {
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private VehicleInputController input;
        [SerializeField] private VehicleUpgradeState upgrades = new VehicleUpgradeState();

        [Header("Chassis")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.38f, 0f);
        [SerializeField, Min(0f)] private float linearDamping = 0.04f;
        [SerializeField, Min(0f)] private float angularDamping = 2.2f;
        [SerializeField, Min(1f)] private float collisionAngularVelocityLimit = 8f;

        [Header("Suspension")]
        [SerializeField, Min(0.1f)] private float suspensionRestLength = 0.56f;
        [SerializeField, Min(0f)] private float antiRollStrength = 5200f;

        // Four road wheels share the same two-axle load used by the 2D controller. Each road wheel therefore
        // receives exactly half of the per-axle spring and damping values, preserving identical static sag.
        private const float PerRoadWheelSuspensionScale = .5f;

        [Header("Steering")]
        [SerializeField, Range(5f, 45f)] private float maximumSteeringAngle = 32f;
        [SerializeField] private AnimationCurve steeringBySpeed = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.45f, 0.58f), new Keyframe(1f, 0.27f));
        [SerializeField, Min(0f)] private float yawAssist = 58f;

        [Header("AWD Traction")]
        [SerializeField, Min(0.1f)] private float forwardFriction = 1.12f;
        [SerializeField, Min(0.1f)] private float sidewaysFriction = 1.18f;
        [SerializeField, Min(0.1f)] private float lateralGripResponse = 7.5f;
        [SerializeField, Min(0f)] private float brakeForce = 14500f;
        [SerializeField, Range(2f, 5f)] private float reverseEngageSpeedThresholdKmh = 4f;
        [SerializeField] private float initialReverseBoundary = -50f;
        [SerializeField, Min(1f)] private float maxReverseDistanceFromProgress = 150f;
        [SerializeField, Min(1f)] private float reverseBoundaryDeceleration = 35f;
        [SerializeField, Range(10f, 60f)] private float maxReverseSpeedKmh = 40f;
        [SerializeField, Range(0.02f, 0.2f)] private float airborneGraceSeconds = 0.08f;

        [Header("Aerodynamics")]
        [SerializeField, Min(0f)] private float downforceCoefficient = 4.2f;
        [SerializeField, Min(0f)] private float maximumDownforce = 11500f;

        private readonly Vector3[] wheelLocalPoints =
        {
            new Vector3(-0.78f, 0f, -1.15f), new Vector3(0.78f, 0f, -1.15f),
            new Vector3(-0.78f, 0f, 1.15f), new Vector3(0.78f, 0f, 1.15f)
        };
        private readonly WheelState[] wheels = new WheelState[4];
        private readonly RaycastHit[] suspensionHits = new RaycastHit[8];

        private Rigidbody body;
        private EngineController engine;
        private TransmissionController transmission;
        private FuelSystem fuel;
        private ResolvedVehicleStats stats;
        private bool driveEnabled = true;
        private bool grounded;
        private float wheelSlip;
        private float steeringInput;
        private float actualSteeringAngle;
        private float trackDistance;
        private float furthestTrackDistance;
        private float gripMultiplier;
        private float currentDownforce;
        private Vector3 previousPosition;
        private DriveDirection driveDirection = DriveDirection.Forward;
        private float lastGroundedTime = float.NegativeInfinity;

        private struct WheelState
        {
            public bool grounded;
            public RaycastHit hit;
            public float normalForce;
            public float compression01;
            public float suspensionLength;
        }

        public override float SpeedMps => body != null ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;
        public override float ProgressPosition => trackDistance;
        public float SteeringInput => steeringInput;
        public float ActualSteeringAngle => actualSteeringAngle;
        public float LateralVelocity => body != null ? Vector3.Dot(body.linearVelocity, transform.right) : 0f;
        public float ForwardVelocity => SpeedMps;
        public float GripMultiplier => gripMultiplier;
        public float CurrentDownforce => currentDownforce;
        public DriveDirection Direction => driveDirection;
        public override float FuelNormalized => fuel != null ? fuel.NormalizedFuel : 0f;
        public override float MotorTorque => engine != null ? engine.CurrentTorqueNm : 0f;
        public override float AppliedThrottle => engine != null ? engine.AppliedThrottle : 0f;
        public override float EngineRpm => engine != null ? engine.CurrentRpm : 0f;
        public override float WheelSlip => wheelSlip;
        public override bool AnyGrounded => grounded;
        public override TransmissionController Transmission => transmission;
        public override VehicleDefinition Definition => definition;
        public override ResolvedVehicleStats Stats => stats;
        public override FuelSystem Fuel => fuel;
        public override Rigidbody Body => body;

        public float GetWheelSuspensionLength(int index)
        {
            if (index < 0 || index >= wheels.Length)
                return suspensionRestLength;
            float length = wheels[index].suspensionLength;
            return length > .001f ? length : suspensionRestLength;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            engine = GetComponent<EngineController>();
            transmission = GetComponent<TransmissionController>();
            fuel = GetComponent<FuelSystem>();
            if (definition == null) { enabled = false; return; }
            float halfTrack = Mathf.Max(0.1f, definition.halfTrack);
            float rearAnchor = definition.RearWheelAnchor;
            float frontAnchor = definition.FrontWheelAnchor;
            suspensionRestLength = definition.suspension.restLength;
            wheelLocalPoints[0] = new Vector3(-halfTrack, 0f, rearAnchor);
            wheelLocalPoints[1] = new Vector3(halfTrack, 0f, rearAnchor);
            wheelLocalPoints[2] = new Vector3(-halfTrack, 0f, frontAnchor);
            wheelLocalPoints[3] = new Vector3(halfTrack, 0f, frontAnchor);
            upgrades.CopyFrom(SaveManager.LoadUpgrades(definition.vehicleId, definition));
            upgrades.ClampTo(definition);

            stats = definition.Resolve(upgrades);
            body.mass = Mathf.Max(50f, definition.mass);
            body.centerOfMass = definition.centerOfMass;
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.None;
            body.maxAngularVelocity = collisionAngularVelocityLimit;
            fuel.Initialize(definition, stats);
            engine.Initialize(definition, stats, fuel);
            transmission.Initialize(definition, GameSession.SelectedTransmission);
            previousPosition = body.position;
            trackDistance = 0f;
            furthestTrackDistance = 0f;
        }

        private void FixedUpdate()
        {
            VehicleInputState state = input != null ? input.State : default;
            int groundedWheels = SimulateSuspension();
            if (groundedWheels > 0) lastGroundedTime = Time.fixedTime;
            grounded = groundedWheels > 0 || Time.fixedTime - lastGroundedTime <= airborneGraceSeconds;

            float gas = driveEnabled ? state.Gas : 0f;
            float brake = driveEnabled ? state.BrakeRaw : 0f;
            UpdateDriveDirection(gas, brake);
            bool hasFuel = fuel != null && fuel.NormalizedFuel > 0f;
            float engineThrottle = hasFuel && driveDirection == DriveDirection.Forward ? gas : 0f;
            transmission.Tick(Time.fixedDeltaTime, Mathf.Abs(SpeedMps), engineThrottle, grounded, false);
            float torque = engine.Tick(Time.fixedDeltaTime, engineThrottle, transmission.Rpm, transmission.TorqueScale);

            UpdateSteering(driveEnabled ? state.Steer : 0f);
            SimulateTires(torque, gas, brake, groundedWheels, hasFuel);
            ApplyAntiRoll();
            bool hasWheelContact = groundedWheels > 0;
            ApplyAerodynamics(hasWheelContact);
            ApplyAirControl(state, hasWheelContact);
            AccumulateTrackDistance();
        }

        private int SimulateSuspension()
        {
            int count = 0;
            SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                out float maximumCompression, out float maximumExtension);
            float rayLength = suspensionRestLength + maximumExtension + definition.wheelRadius;
            float spring = stats.spring * PerRoadWheelSuspensionScale;
            float damping = stats.damping * PerRoadWheelSuspensionScale;
            for (int i = 0; i < wheels.Length; i++)
            {
                Vector3 origin = transform.TransformPoint(wheelLocalPoints[i]);
                bool hitGround = SuspensionPhysics.TryRaycastGround(origin, -transform.up, rayLength, ~0,
                    transform, suspensionHits, out RaycastHit hit);
                WheelState wheel = wheels[i];
                wheel.grounded = hitGround;
                wheel.hit = hit;
                wheel.normalForce = 0f;
                wheel.compression01 = 0f;
                wheel.suspensionLength = suspensionRestLength + maximumExtension;
                if (hitGround)
                {
                    count++;
                    float measuredLength = Mathf.Max(0f, hit.distance - definition.wheelRadius);
                    float rawCompression = suspensionRestLength - measuredLength;
                    wheel.suspensionLength = SuspensionPhysics.ClampLength(suspensionRestLength,
                        maximumCompression, maximumExtension, measuredLength);
                    float suspensionVelocity = Vector3.Dot(body.GetPointVelocity(origin), transform.up);
                    float maximumForce = body.mass * definition.suspension.maximumAcceleration / wheels.Length;
                    wheel.normalForce = SuspensionPhysics.CalculateNormalForce(rawCompression, suspensionVelocity,
                        spring, damping, maximumCompression, maximumExtension,
                        definition.suspension.bumpStopStart, definition.suspension.progressiveSpringMultiplier,
                        definition.suspension.hardStopSpringMultiplier, maximumForce);
                    wheel.normalForce = SuspensionPhysics.LimitRecoveryForce(wheel.normalForce, suspensionVelocity,
                        body.mass / wheels.Length, Time.fixedDeltaTime,
                        definition.suspension.maximumReboundAcceleration);
                    wheel.compression01 = SuspensionPhysics.CalculateSag01(
                        suspensionRestLength - wheel.suspensionLength, maximumCompression, maximumExtension);
                    body.AddForceAtPosition(transform.up * wheel.normalForce, origin, ForceMode.Force);
                }
                wheels[i] = wheel;
            }
            return count;
        }

        private void UpdateSteering(float requestedSteering)
        {
            steeringInput = Mathf.Clamp(requestedSteering, -1f, 1f);
            float speed01 = Mathf.InverseLerp(0f, Mathf.Max(1f, stats.topSpeedMps), Mathf.Abs(SpeedMps));
            float authority = Mathf.Clamp(steeringBySpeed.Evaluate(speed01), 0.12f, 1f);
            actualSteeringAngle = steeringInput * maximumSteeringAngle * authority;
        }

        private void SimulateTires(float engineTorque, float gas, float brake, int groundedWheels, bool hasFuel)
        {
            wheelSlip = 0f;
            gripMultiplier = stats.grip;
            if (groundedWheels <= 0) return;

            float driveForce;
            if (!hasFuel)
                driveForce = Mathf.Abs(SpeedMps) > .05f ? -Mathf.Sign(SpeedMps) * brakeForce * brake : 0f;
            else switch (driveDirection)
            {
                case DriveDirection.BrakingToReverse:
                    driveForce = -brakeForce * brake;
                    break;
                case DriveDirection.Reverse:
                    driveForce = brake > 0f && SpeedMps > -maxReverseSpeedKmh / 3.6f
                        ? -definition.reverseForce * brake : 0f;
                    break;
                case DriveDirection.BrakingToForward:
                    driveForce = brakeForce * gas;
                    break;
                default:
                    float forwardSpeed = Mathf.Max(0f, SpeedMps);
                    float resistanceCompensation = definition.grip.aerodynamicDrag * forwardSpeed * forwardSpeed +
                                                   definition.mass * Physics.gravity.magnitude *
                                                   definition.grip.rollingResistance;
                    driveForce = (body.mass * stats.targetAccelerationMps2 + resistanceCompensation) * gas;
                    if (SpeedMps >= stats.topSpeedMps) driveForce = 0f;
                    break;
            }
            float reverseBoundary = Mathf.Max(initialReverseBoundary,
                furthestTrackDistance - maxReverseDistanceFromProgress);
            bool atReverseBoundary = trackDistance <= reverseBoundary + .5f;
            if (atReverseBoundary && driveForce < 0f) driveForce = 0f;
            if (atReverseBoundary && SpeedMps < 0f)
            {
                float stopAcceleration = Mathf.Min(reverseBoundaryDeceleration,
                    -SpeedMps / Mathf.Max(.001f, Time.fixedDeltaTime));
                body.AddForce(transform.forward * body.mass * stopAcceleration, ForceMode.Force);
            }
            float drivePerWheel = driveForce / groundedWheels;
            float slipSum = 0f;

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelState wheel = wheels[i];
                if (!wheel.grounded) continue;
                bool front = i >= 2;
                Vector3 tireForward = front
                    ? Quaternion.AngleAxis(actualSteeringAngle, transform.up) * transform.forward
                    : transform.forward;
                tireForward = Vector3.ProjectOnPlane(tireForward, wheel.hit.normal).normalized;
                Vector3 tireRight = Vector3.Cross(wheel.hit.normal, tireForward).normalized;
                Vector3 pointVelocity = body.GetPointVelocity(wheel.hit.point);
                float lateralSpeed = Vector3.Dot(pointVelocity, tireRight);

                float longitudinal = drivePerWheel;
                PhysicsMaterial surfaceMaterial = wheel.hit.collider != null
                    ? wheel.hit.collider.sharedMaterial : null;
                float surfaceFriction = surfaceMaterial != null
                    ? Mathf.Max(0f, surfaceMaterial.dynamicFriction) : 1f;
                // The same tire coefficient and terrain coefficient used by the physical 2D wheels
                // define the virtual 3D tire budget. Fixed force floors used to mask low Grip ratings.
                float longitudinalLimit = Mathf.Max(1f,
                    wheel.normalForce * stats.grip * surfaceFriction * forwardFriction);
                float appliedLongitudinal = Mathf.Clamp(longitudinal, -longitudinalLimit, longitudinalLimit);
                float requestedLateral = -lateralSpeed * (body.mass / 4f) * lateralGripResponse;
                float lateralLimit = Mathf.Max(1f,
                    wheel.normalForce * stats.grip * surfaceFriction * sidewaysFriction);
                float appliedLateral = Mathf.Clamp(requestedLateral, -lateralLimit, lateralLimit);
                // Virtual-wheel propulsion must not add a false pitch/roll lever at high speed. Keep lateral tire
                // response at the contact patch, but apply the longitudinal component through the chassis.
                body.AddForce(tireForward * appliedLongitudinal, ForceMode.Force);
                body.AddForceAtPosition(tireRight * appliedLateral, wheel.hit.point, ForceMode.Force);

                float longitudinalSlip = Mathf.Abs(longitudinal) > 1f
                    ? Mathf.Clamp01(1f - Mathf.Abs(appliedLongitudinal / longitudinal)) : 0f;
                float lateralSlip = Mathf.Abs(requestedLateral) > 1f
                    ? Mathf.Clamp01(1f - Mathf.Abs(appliedLateral / requestedLateral)) : 0f;
                slipSum += Mathf.Max(longitudinalSlip, lateralSlip);
            }

            wheelSlip = Mathf.Clamp01(slipSum / groundedWheels);
            float yawSpeedScale = Mathf.Clamp(Mathf.Abs(SpeedMps), 0f, 24f);
            body.AddTorque(transform.up * steeringInput * yawSpeedScale * yawAssist * (1f - wheelSlip * 0.55f),
                ForceMode.Force);
        }

        private void UpdateDriveDirection(float gas, float brake)
        {
            if (!driveEnabled) { driveDirection = DriveDirection.Forward; return; }
            float speed = SpeedMps;
            float threshold = reverseEngageSpeedThresholdKmh / 3.6f;
            switch (driveDirection)
            {
                case DriveDirection.Forward:
                    if (brake > 0f)
                        driveDirection = speed > threshold ? DriveDirection.BrakingToReverse : DriveDirection.Reverse;
                    break;
                case DriveDirection.BrakingToReverse:
                    if (brake <= 0f) driveDirection = DriveDirection.Forward;
                    else if (speed <= threshold) driveDirection = DriveDirection.Reverse;
                    break;
                case DriveDirection.Reverse:
                    if (gas > 0f)
                        driveDirection = speed < -threshold ? DriveDirection.BrakingToForward : DriveDirection.Forward;
                    else if (speed > threshold)
                        driveDirection = DriveDirection.Forward;
                    break;
                case DriveDirection.BrakingToForward:
                    if (gas <= 0f) driveDirection = speed < 0f ? DriveDirection.Reverse : DriveDirection.Forward;
                    else if (speed >= -threshold) driveDirection = DriveDirection.Forward;
                    break;
            }
        }

        private void ApplyAntiRoll()
        {
            ApplyAntiRollAxle(0, 1);
            ApplyAntiRollAxle(2, 3);
        }

        private void ApplyAntiRollAxle(int left, int right)
        {
            if (!wheels[left].grounded && !wheels[right].grounded) return;
            float force = (wheels[left].compression01 - wheels[right].compression01) * antiRollStrength;
            if (wheels[left].grounded)
                body.AddForceAtPosition(transform.up * force, transform.TransformPoint(wheelLocalPoints[left]), ForceMode.Force);
            if (wheels[right].grounded)
                body.AddForceAtPosition(-transform.up * force, transform.TransformPoint(wheelLocalPoints[right]), ForceMode.Force);
        }

        private void ApplyAerodynamics(bool hasWheelContact)
        {
            Vector3 velocity = body.linearVelocity;
            float speedSquared = velocity.sqrMagnitude;
            float contactDownforce = hasWheelContact
                ? Mathf.Min(maximumDownforce, speedSquared * downforceCoefficient) : 0f;
            float tunedAcceleration = VehicleController.EvaluateAerodynamicDownforceAcceleration(
                definition, stats, velocity.magnitude);
            float tunedDownforce = body.mass * tunedAcceleration;
            currentDownforce = contactDownforce + tunedDownforce;
            if (contactDownforce > 0f)
                body.AddForce(-transform.up * contactDownforce, ForceMode.Force);
            if (tunedAcceleration > 0f)
                body.AddForce(Vector3.down * tunedAcceleration, ForceMode.Acceleration);
            if (speedSquared < 0.001f) return;
            float drag = definition.grip.aerodynamicDrag * speedSquared;
            float rolling = hasWheelContact ? definition.mass * Physics.gravity.magnitude * definition.grip.rollingResistance : 0f;
            body.AddForce(-velocity.normalized * (drag + rolling), ForceMode.Force);
        }

        private void ApplyAirControl(VehicleInputState state, bool hasWheelContact)
        {
            body.angularDamping = hasWheelContact ? angularDamping : definition.airAngularDamping;
            body.maxAngularVelocity = hasWheelContact ? collisionAngularVelocityLimit : definition.maxAirAngularVelocity;
            if (hasWheelContact || !driveEnabled) return;
            float pitchInput = state.Gas - state.BrakeRaw;
            if (Mathf.Abs(pitchInput) < .001f) return;
            float authority = definition.airControlTorque > .001f
                ? Mathf.Clamp(stats.airControlTorque / definition.airControlTorque, .5f, 1.2f) : 1f;
            Vector3 pitchAxis = -transform.right;
            float current = Vector3.Dot(body.angularVelocity, pitchAxis);
            float target = pitchInput * definition.maxAirAngularVelocity * authority;
            float acceleration = Mathf.Clamp((target - current) / Mathf.Max(.001f, Time.fixedDeltaTime),
                -definition.airAngularAcceleration * authority, definition.airAngularAcceleration * authority);
            body.AddTorque(pitchAxis * acceleration, ForceMode.Acceleration);
        }

        private void AccumulateTrackDistance()
        {
            Vector3 delta = body.position - previousPosition;
            trackDistance += Vector3.Dot(delta, transform.forward);
            furthestTrackDistance = Mathf.Max(furthestTrackDistance, trackDistance);
            previousPosition = body.position;
        }

        public override void SetDriveEnabled(bool value)
        {
            driveEnabled = value;
            if (!value) driveDirection = DriveDirection.Forward;
            engine?.SetEnabled(value);
        }

        public override void AddFuel(float normalizedAmount) => fuel?.AddFuel(normalizedAmount);

        public void Assign(VehicleDefinition vehicleDefinition, VehicleInputController targetInput)
        {
            definition = vehicleDefinition;
            input = targetInput;
        }
    }
}
