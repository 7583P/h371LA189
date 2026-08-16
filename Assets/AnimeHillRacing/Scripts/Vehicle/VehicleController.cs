using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [System.Flags]
    public enum ChassisContactRegion
    {
        None = 0,
        Front = 1 << 0,
        Bottom = 1 << 1,
        Rear = 1 << 2,
        Roof = 1 << 3
    }

    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(Rigidbody), typeof(EngineController), typeof(TransmissionController))]
    [RequireComponent(typeof(FuelSystem))]
    public sealed class VehicleController : VehicleRuntimeBase
    {
        private const float TopSpeedGovernorResponseSeconds = .45f;
        private const float TopSpeedGovernorMaximumCorrectionAcceleration = 18f;
        private const float OneWheelPitchControlAuthority = .14f;

        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private VehicleUpgradeState upgrades = new VehicleUpgradeState();
        [SerializeField] private VehicleInputController input;
        [SerializeField] private SuspensionController[] suspensionPoints;
        [SerializeField] private float initialReverseBoundary = -50f;
        [SerializeField, Min(1f)] private float maxReverseDistanceFromProgress = 150f;
        [SerializeField, Min(1f)] private float reverseBoundaryDeceleration = 35f;
        [SerializeField, Range(2f, 5f)] private float reverseEngageSpeedThresholdKmh = 4f;
        [SerializeField, Range(10f, 60f)] private float maxReverseSpeedKmh = 40f;
        [SerializeField, Range(0.02f, 0.2f)] private float airborneGraceSeconds = 0.08f;
        [Header("Optional physics diagnostics")]
        [SerializeField] private bool logPhysicsDiagnostics;
        [SerializeField] private bool drawPhysicsGizmos;
        [SerializeField, Min(.00001f)] private float gizmoForceScale = .00003f;

        private Rigidbody body;
        private EngineController engine;
        private TransmissionController transmission;
        private FuelSystem fuel;
        private ResolvedVehicleStats stats;
        private bool driveEnabled = true;
        private float wheelSlip;
        private bool anyGrounded;
        private float furthestDistanceReached;
        private DriveDirection driveDirection = DriveDirection.Forward;
        private float lastGroundedTime = float.NegativeInfinity;
        private bool pitchInertiaConfigured;
        private int frontSuspensionIndex = -1;
        private int rearSuspensionIndex = -1;
        private bool frontWheelGrounded;
        private bool rearWheelGrounded;
        private bool anyWheelGrounded;
        private bool bothWheelsGrounded;
        private bool chassisGrounded;
        private bool chassisContactSinceLastStep;
        private Vector3 chassisContactNormal = Vector3.up;
        private ChassisContactRegion chassisContactRegions;
        private ChassisContactRegion chassisContactRegionsSinceLastStep;
        private float chassisImpactVelocity;
        private float chassisImpactVelocitySinceLastStep;
        private int chassisContactCount;
        private int chassisContactCountSinceLastStep;
        private float nextPhysicsDiagnosticTime;
        private float surfaceGripMultiplier = 1f;

        public override float SpeedMps => body != null ? EvaluateProgressSpeed(body.linearVelocity) : 0f;
        public override float ProgressPosition => body != null ? body.position.x : 0f;
        public float FurthestDistanceReached => furthestDistanceReached;
        public float ReverseBoundaryX => Mathf.Max(initialReverseBoundary,
            furthestDistanceReached - maxReverseDistanceFromProgress);
        public DriveDirection Direction => driveDirection;
        public override float FuelNormalized => fuel != null ? fuel.FuelNormalized : 0f;
        public override float MotorTorque => engine != null ? engine.CurrentTorqueNm : 0f;
        public override float AppliedThrottle => engine != null ? engine.AppliedThrottle : 0f;
        public override float EngineRpm => engine != null ? engine.CurrentRpm : 0f;
        public override float WheelSlip => wheelSlip;
        public override bool AnyGrounded => anyGrounded;
        public override bool FrontWheelGrounded => frontWheelGrounded;
        public override bool RearWheelGrounded => rearWheelGrounded;
        public override bool AnyWheelGrounded => anyWheelGrounded;
        public override bool BothWheelsGrounded => bothWheelsGrounded;
        public override bool ChassisGrounded => chassisGrounded;
        public override bool FullyAirborne => !anyWheelGrounded && !chassisGrounded;
        public override bool Airborne => FullyAirborne;
        public override TransmissionController Transmission => transmission;
        public override VehicleDefinition Definition => definition;
        public override ResolvedVehicleStats Stats => stats;
        public override FuelSystem Fuel => fuel;
        public override Rigidbody Body => body;
        public ChassisContactRegion ChassisContactRegions => chassisContactRegions;
        public Vector3 ChassisContactNormal => chassisContactNormal;
        public float ChassisImpactVelocity => chassisImpactVelocity;
        public int ChassisContactCount => chassisContactCount;
        public float SurfaceGripMultiplier => surfaceGripMultiplier;
        public SuspensionController FrontSuspension => SuspensionAt(frontSuspensionIndex);
        public SuspensionController RearSuspension => SuspensionAt(rearSuspensionIndex);

        public void SetSurfaceGripMultiplier(float multiplier) =>
            surfaceGripMultiplier = Mathf.Clamp(multiplier, .25f, 1f);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            engine = GetComponent<EngineController>();
            transmission = GetComponent<TransmissionController>();
            fuel = GetComponent<FuelSystem>();
            if (fuel == null)
                fuel = gameObject.AddComponent<FuelSystem>();
            if (definition == null)
            {
                enabled = false;
                return;
            }

            upgrades.CopyFrom(AnimeHillRacing.Core.SaveManager.LoadUpgrades(definition.vehicleId, definition));
            upgrades.ClampTo(definition);

            stats = definition.Resolve(upgrades);
            body.mass = definition.mass;
            body.centerOfMass = definition.centerOfMass;
            if (!pitchInertiaConfigured)
            {
                body.ResetInertiaTensor();
                Vector3 inertia = body.inertiaTensor;
                inertia.z *= Mathf.Clamp(definition.collisionPitchInertiaMultiplier, 1f, 16f);
                body.inertiaTensor = inertia;
                pitchInertiaConfigured = true;
            }
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxDepenetrationVelocity = definition.maximumDepenetrationVelocity;
            body.solverIterations = Mathf.Max(body.solverIterations, 12);
            body.solverVelocityIterations = Mathf.Max(body.solverVelocityIterations, 8);
            body.angularDamping = definition.twoWheelAngularDamping;
            body.maxAngularVelocity = Mathf.Max(definition.maxAirAngularVelocity,
                definition.maximumPassiveAngularVelocity);
            fuel.Initialize(definition, stats);
            engine.Initialize(definition, stats, fuel);
            transmission.Initialize(definition, TransmissionMode.Auto);
            furthestDistanceReached = body.position.x;

            if (suspensionPoints == null || suspensionPoints.Length == 0)
                suspensionPoints = GetComponentsInChildren<SuspensionController>();
            ResolveAxleIndices();
            for (int i = 0; i < suspensionPoints.Length; i++)
                suspensionPoints[i].Initialize(body, definition, stats, suspensionPoints.Length);
        }

        private void FixedUpdate()
        {
            if (definition == null)
                return;

            furthestDistanceReached = Mathf.Max(furthestDistanceReached, body.position.x);

            VehicleInputState inputState = input != null ? input.State : default;
            for (int i = 0; i < suspensionPoints.Length; i++)
                suspensionPoints[i].Simulate();

            bool rawGrounded = false;
            for (int i = 0; i < suspensionPoints.Length; i++)
                rawGrounded |= suspensionPoints[i].IsGrounded;
            frontWheelGrounded = IsSuspensionGrounded(frontSuspensionIndex);
            rearWheelGrounded = IsSuspensionGrounded(rearSuspensionIndex);
            anyWheelGrounded = rawGrounded;
            bothWheelsGrounded = frontWheelGrounded && rearWheelGrounded;
            chassisGrounded = chassisContactSinceLastStep;
            chassisContactRegions = chassisContactRegionsSinceLastStep;
            chassisImpactVelocity = chassisImpactVelocitySinceLastStep;
            chassisContactCount = chassisContactCountSinceLastStep;
            chassisContactSinceLastStep = false;
            chassisContactRegionsSinceLastStep = ChassisContactRegion.None;
            chassisImpactVelocitySinceLastStep = 0f;
            chassisContactCountSinceLastStep = 0;
            if (rawGrounded) lastGroundedTime = Time.fixedTime;
            anyGrounded = rawGrounded || Time.fixedTime - lastGroundedTime <= airborneGraceSeconds;

            float gas = driveEnabled ? inputState.Gas : 0f;
            float brake = driveEnabled ? inputState.BrakeRaw : 0f;
            UpdateDriveDirection(gas, brake);
            bool hasFuel = fuel != null && fuel.FuelNormalized > 0f;
            float engineThrottle = hasFuel && driveDirection == DriveDirection.Forward ? gas : 0f;
            transmission.Tick(Time.fixedDeltaTime, Mathf.Abs(SpeedMps), engineThrottle, AnyGrounded, false);
            float normalizedSpeed = Mathf.Max(0f, SpeedMps) / Mathf.Max(1f, stats.topSpeedMps);
            engine.Tick(Time.fixedDeltaTime, engineThrottle, transmission.Rpm,
                transmission.TorqueScale, normalizedSpeed);

            ApplyDrive(gas, brake, hasFuel);
            ApplyDrag();
            ApplyAerodynamicDownforce();
            ApplyTopSpeedGovernor();
            ApplyAirControl(inputState);
            LogPhysicsDiagnostics();
        }

        private void ApplyAerodynamicDownforce()
        {
            if (stats.airborneDownforceAcceleration <= .001f) return;
            float acceleration = EvaluateAerodynamicDownforceAcceleration(definition, stats,
                Mathf.Abs(SpeedMps));
            if (acceleration > .001f)
            {
                Vector3 downforce = EvaluateAppliedDownforceVector(transform.up, acceleration,
                    anyWheelGrounded, chassisGrounded);
                if (downforce.sqrMagnitude > .000001f)
                    body.AddForce(downforce, ForceMode.Acceleration);
            }
        }

        public static float EvaluateAerodynamicDownforceAcceleration(VehicleDefinition source,
            ResolvedVehicleStats resolved, float speedMps)
        {
            if (source == null) return 0f;
            float speed01 = Mathf.Clamp01(Mathf.Max(0f, speedMps) /
                                          Mathf.Max(1f, source.downforceReferenceSpeedMps));
            return Mathf.Min(source.maximumDownforceAcceleration,
                       Mathf.Max(0f, resolved.airborneDownforceAcceleration)) * speed01 * speed01;
        }

        public static Vector3 EvaluateAerodynamicDownforceVector(Vector3 chassisUp,
            float acceleration)
        {
            Vector3 safeUp = chassisUp.sqrMagnitude > .000001f ? chassisUp.normalized : Vector3.up;
            return -safeUp * Mathf.Max(0f, acceleration);
        }

        public static Vector3 EvaluateAppliedDownforceVector(Vector3 chassisUp, float acceleration,
            bool hasWheelContact, bool hasChassisContact)
        {
            float safeAcceleration = Mathf.Max(0f, acceleration);
            if (safeAcceleration <= .001f || hasChassisContact && !hasWheelContact) return Vector3.zero;
            // Tire contact loads the Formula along its aerodynamic lift axis. During true flight, world-down
            // shortens the jump as the Downforce tune promises. A chassis-only bounce keeps natural gravity.
            return hasWheelContact
                ? EvaluateAerodynamicDownforceVector(chassisUp, safeAcceleration)
                : Vector3.down * safeAcceleration;
        }

        private void ApplyDrive(float gas, float brake, bool hasFuel)
        {
            float speed = SpeedMps;
            float requestedWheelTorque = 0f;
            float requestedBrakeTorque = 0f;
            if (!hasFuel)
                requestedBrakeTorque = definition.brakeForce * definition.wheelRadius * brake;
            else switch (driveDirection)
            {
                case DriveDirection.BrakingToReverse:
                    requestedBrakeTorque = definition.brakeForce * definition.wheelRadius * brake;
                    break;
                case DriveDirection.Reverse:
                    requestedWheelTorque = brake > 0f && speed > -maxReverseSpeedKmh / 3.6f
                        ? -definition.reverseForce * definition.wheelRadius * brake : 0f;
                    break;
                case DriveDirection.BrakingToForward:
                    requestedBrakeTorque = definition.brakeForce * definition.wheelRadius * gas;
                    break;
                default:
                    float forwardSpeed = Mathf.Max(0f, speed);
                    float resistanceCompensation = definition.grip.aerodynamicDrag *
                                                   forwardSpeed * forwardSpeed +
                                                   definition.mass * Physics.gravity.magnitude *
                                                   definition.grip.rollingResistance;
                    // Physical wheel/joint leverage differs from the direct-force road drivetrain. Keep its
                    // measured correction local to 2.5D so the shared tune still means the same 0-100 time.
                    float calibratedAcceleration = stats.targetAccelerationMps2 *
                                                   definition.accelerationForceCalibration;
                    float driveAcceleration = EvaluateDriveAcceleration(calibratedAcceleration,
                        forwardSpeed, stats.topSpeedMps);
                    // The authored acceleration remains exact through 100 km/h. Beyond it, a separate cruise
                    // phase prevents aggressive maximum tunes from carrying their full 0-100 force to Vmax.
                    float requestedDriveForce = (body.mass * driveAcceleration +
                                                 resistanceCompensation) * gas;
                    requestedWheelTorque = requestedDriveForce * definition.wheelRadius;
                    break;
            }

            float reverseBoundary = ReverseBoundaryX;
            bool atReverseBoundary = body.position.x <= reverseBoundary + 0.5f;
            if (atReverseBoundary && requestedWheelTorque < 0f)
                requestedWheelTorque = 0f;
            if (atReverseBoundary && speed < 0f)
            {
                float stopAcceleration = Mathf.Min(reverseBoundaryDeceleration,
                    -speed / Mathf.Max(0.001f, Time.fixedDeltaTime));
                body.AddForce(Vector3.right * body.mass * stopAcceleration, ForceMode.Force);
            }

            int wheelCount = Mathf.Max(1, suspensionPoints.Length);
            float perWheelTorque = requestedWheelTorque / wheelCount;
            float perWheelBrakeTorque = requestedBrakeTorque / wheelCount;
            float slip = 0f;
            int groundedCount = 0;
            for (int i = 0; i < suspensionPoints.Length; i++)
            {
                suspensionPoints[i].ApplyMotorTorque(perWheelTorque, stats.grip * surfaceGripMultiplier);
                suspensionPoints[i].ApplyBrakeTorque(perWheelBrakeTorque);
                if (suspensionPoints[i].IsGrounded)
                {
                    slip += suspensionPoints[i].Slip01;
                    groundedCount++;
                }
            }
            wheelSlip = groundedCount > 0 ? slip / groundedCount : 0f;
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

        private void ApplyDrag()
        {
            Vector3 velocity = body.linearVelocity;
            float speed = velocity.magnitude;
            if (speed < 0.01f)
                return;

            float dragForce = definition.grip.aerodynamicDrag * speed * speed;
            float rollingForce = AnyWheelGrounded
                ? definition.mass * Physics.gravity.magnitude * definition.grip.rollingResistance : 0f;
            body.AddForce(-velocity.normalized * (dragForce + rollingForce), ForceMode.Force);
        }

        private void ApplyTopSpeedGovernor()
        {
            // The tune is an actual maximum, not merely a motor cut-off. Keep the correction force-based so
            // impacts and airborne momentum are never rewritten. Adventure 2.5D speed is horizontal progress,
            // so a pitched chassis cannot hide overspeed from either the tune or the HUD.
            float forwardSpeed = SpeedMps;
            if (forwardSpeed <= stats.topSpeedMps) return;

            float downforceAcceleration = EvaluateAerodynamicDownforceAcceleration(definition, stats,
                Mathf.Abs(forwardSpeed));
            Vector3 externalDownAcceleration = Physics.gravity +
                EvaluateAppliedDownforceVector(transform.up, downforceAcceleration,
                    anyWheelGrounded, chassisGrounded);
            bool physicallySupported = anyGrounded || chassisGrounded;
            float downhillAcceleration = physicallySupported
                ? Mathf.Max(0f, Vector3.Dot(externalDownAcceleration, transform.right)) *
                  Mathf.Abs(Vector3.Dot(transform.right, Vector3.right))
                : 0f;
            float maximumGovernorAcceleration = externalDownAcceleration.magnitude +
                                                TopSpeedGovernorMaximumCorrectionAcceleration;
            float governorAcceleration = EvaluateTopSpeedGovernorAcceleration(forwardSpeed,
                stats.topSpeedMps, downhillAcceleration, maximumGovernorAcceleration);
            if (governorAcceleration > 0f)
                body.AddForce(Vector3.left * governorAcceleration, ForceMode.Acceleration);
        }

        public static float EvaluateProgressSpeed(Vector3 linearVelocity) => linearVelocity.x;

        public static float EvaluateTopSpeedDriveAuthority(float forwardSpeed, float topSpeed)
        {
            float safeTopSpeed = Mathf.Max(1f, topSpeed);
            float safeForwardSpeed = Mathf.Max(0f, forwardSpeed);
            if (safeForwardSpeed >= safeTopSpeed) return 0f;

            const float hundredKmhMps = 100f / 3.6f;
            if (safeForwardSpeed <= hundredKmhMps || safeTopSpeed <= hundredKmhMps)
                return 1f;

            // Acceleration tune is authored specifically as a 0-100 km/h time, so it retains full
            // authority through that phase. Above 100 km/h a separate soft phase progressively reduces
            // propulsion, making the tuned maximum arrive substantially later without changing its value.
            const float minimumHighSpeedAuthority = .08f;
            float highSpeed01 = Mathf.InverseLerp(hundredKmhMps, safeTopSpeed, safeForwardSpeed);
            float smoothHighSpeed = Mathf.SmoothStep(0f, 1f, highSpeed01);
            return Mathf.Lerp(1f, minimumHighSpeedAuthority, smoothHighSpeed);
        }

        public static float EvaluateDriveAcceleration(float zeroToHundredAcceleration,
            float forwardSpeed, float topSpeed)
        {
            float tunedAcceleration = Mathf.Max(0f, zeroToHundredAcceleration);
            float safeTopSpeed = Mathf.Max(1f, topSpeed);
            float safeForwardSpeed = Mathf.Max(0f, forwardSpeed);
            if (safeForwardSpeed >= safeTopSpeed) return 0f;

            const float hundredKmhMps = 100f / 3.6f;
            if (safeForwardSpeed <= hundredKmhMps || safeTopSpeed <= hundredKmhMps)
                return tunedAcceleration;

            // 3.5 m/s2 is the maximum sustained acceleration after 100 km/h. A short smooth transition
            // removes any torque step while ensuring a 2-second 0-100 tune cannot also reach 300 km/h in
            // a few more seconds. Acceleration tune still owns 0-100; Speed still owns the unchanged Vmax.
            const float maximumCruiseAcceleration = 3.5f;
            const float transitionFraction = .12f;
            float highSpeed01 = Mathf.InverseLerp(hundredKmhMps, safeTopSpeed, safeForwardSpeed);
            float transition = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(highSpeed01 / transitionFraction));
            float cruiseAcceleration = Mathf.Min(tunedAcceleration, maximumCruiseAcceleration);
            float phaseAcceleration = Mathf.Lerp(tunedAcceleration, cruiseAcceleration, transition);
            return phaseAcceleration * EvaluateTopSpeedDriveAuthority(safeForwardSpeed, safeTopSpeed);
        }

        public static float EvaluateTopSpeedGovernorAcceleration(float forwardSpeed, float topSpeed,
            float downhillAcceleration, float maximumAcceleration)
        {
            float overspeed = Mathf.Max(0f, forwardSpeed - Mathf.Max(1f, topSpeed));
            if (overspeed <= 0f) return 0f;
            float requested = Mathf.Max(0f, downhillAcceleration) +
                              overspeed / TopSpeedGovernorResponseSeconds;
            return Mathf.Min(requested, Mathf.Max(0f, maximumAcceleration));
        }

        private void ApplyAirControl(VehicleInputState inputState)
        {
            body.angularDamping = EvaluatePitchAngularDamping(frontWheelGrounded, rearWheelGrounded,
                chassisGrounded, definition.oneWheelAngularDamping, definition.twoWheelAngularDamping,
                definition.airAngularDamping);
            // maxAirAngularVelocity is the player's control target, not a clamp on collision momentum.
            // Keeping a separate passive limit prevents a landing-generated rotation from being truncated
            // as soon as the last tire leaves the ground.
            body.maxAngularVelocity = Mathf.Max(definition.maxAirAngularVelocity,
                definition.maximumPassiveAngularVelocity);
            if (!driveEnabled) return;
            float contactAuthority = EvaluatePitchControlAuthority(frontWheelGrounded, rearWheelGrounded,
                chassisGrounded, definition.archetype == VehicleArchetype.Formula,
                definition.chassisAirControlMultiplier);
            if (contactAuthority <= 0f) return;
            float control = inputState.Gas - inputState.BrakeRaw;
            if (Mathf.Abs(control) < .001f) return;
            float authority = definition.airControlTorque > .001f
                ? Mathf.Clamp(stats.airControlTorque / definition.airControlTorque, .5f, 1.2f) : 1f;
            float current = body.angularVelocity.z;
            float target = control * definition.maxAirAngularVelocity * authority * contactAuthority;
            float acceleration = Mathf.Clamp((target - current) / Mathf.Max(.001f, Time.fixedDeltaTime),
                -definition.airAngularAcceleration * authority * contactAuthority,
                definition.airAngularAcceleration * authority * contactAuthority);
            body.AddTorque(Vector3.forward * acceleration, ForceMode.Acceleration);
        }

        public static float EvaluatePitchAngularDamping(bool frontGrounded, bool rearGrounded,
            bool chassisContact, float oneWheelDamping, float twoWheelDamping, float airborneDamping)
        {
            if (frontGrounded && rearGrounded)
                return Mathf.Max(0f, twoWheelDamping);
            if (frontGrounded ^ rearGrounded)
                return Mathf.Max(0f, oneWheelDamping);
            if (chassisContact)
                return Mathf.Max(0f, oneWheelDamping) * .75f;
            return Mathf.Max(0f, airborneDamping);
        }

        public static float EvaluatePitchControlAuthority(bool frontGrounded, bool rearGrounded,
            bool chassisContact, bool allowOneWheelControl, float chassisAuthority)
        {
            if (frontGrounded && rearGrounded) return 0f;
            if (chassisContact) return Mathf.Clamp(chassisAuthority, .05f, .15f);
            if (frontGrounded ^ rearGrounded)
                return allowOneWheelControl ? OneWheelPitchControlAuthority : 0f;
            return 1f;
        }

        private void ResolveAxleIndices()
        {
            frontSuspensionIndex = rearSuspensionIndex = -1;
            float frontX = float.NegativeInfinity;
            float rearX = float.PositiveInfinity;
            for (int i = 0; i < (suspensionPoints != null ? suspensionPoints.Length : 0); i++)
            {
                if (suspensionPoints[i] == null) continue;
                float x = transform.InverseTransformPoint(suspensionPoints[i].transform.position).x;
                if (x > frontX) { frontX = x; frontSuspensionIndex = i; }
                if (x < rearX) { rearX = x; rearSuspensionIndex = i; }
            }
        }

        private bool IsSuspensionGrounded(int index) => suspensionPoints != null && index >= 0 &&
            index < suspensionPoints.Length && suspensionPoints[index] != null && suspensionPoints[index].IsGrounded;

        private SuspensionController SuspensionAt(int index) => suspensionPoints != null && index >= 0 &&
            index < suspensionPoints.Length ? suspensionPoints[index] : null;

        private void OnCollisionEnter(Collision collision) => CaptureChassisContact(collision);
        private void OnCollisionStay(Collision collision) => CaptureChassisContact(collision);

        private void CaptureChassisContact(Collision collision)
        {
            if (body == null || collision == null || collision.collider == null || collision.collider.isTrigger ||
                collision.collider.transform.IsChildOf(transform)) return;
            chassisContactSinceLastStep = true;
            chassisContactCountSinceLastStep += collision.contactCount;
            float bestImpact = chassisImpactVelocitySinceLastStep;
            Vector3 bestNormal = chassisContactNormal;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                Vector3 normal = contact.normal;
                float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
                if (impact > bestImpact)
                {
                    bestImpact = impact;
                    bestNormal = normal;
                }
                chassisContactRegionsSinceLastStep |= ClassifyChassisContact(contact);
            }
            chassisImpactVelocitySinceLastStep = bestImpact;
            chassisContactNormal = bestNormal;
        }

        private ChassisContactRegion ClassifyChassisContact(ContactPoint contact)
        {
            Vector3 localPoint = transform.InverseTransformPoint(contact.point);
            Vector3 localNormal = transform.InverseTransformDirection(contact.normal);
            ChassisContactRegion region = ChassisContactRegion.None;
            float longitudinalThreshold = definition.halfWheelbase * .55f;
            if (localNormal.y < -.25f) region |= ChassisContactRegion.Roof;
            if (localNormal.y > .15f && localPoint.y < .18f) region |= ChassisContactRegion.Bottom;
            if (localPoint.x > longitudinalThreshold) region |= ChassisContactRegion.Front;
            if (localPoint.x < -longitudinalThreshold) region |= ChassisContactRegion.Rear;
            if (region == ChassisContactRegion.None)
                region = localPoint.y >= .18f ? ChassisContactRegion.Roof : ChassisContactRegion.Bottom;
            return region;
        }

        private void LogPhysicsDiagnostics()
        {
            if (!logPhysicsDiagnostics || Time.unscaledTime < nextPhysicsDiagnosticTime) return;
            nextPhysicsDiagnosticTime = Time.unscaledTime + .25f;
            float rearCompression = rearSuspensionIndex >= 0 ? suspensionPoints[rearSuspensionIndex].Compression01 : 0f;
            float frontCompression = frontSuspensionIndex >= 0 ? suspensionPoints[frontSuspensionIndex].Compression01 : 0f;
            float rearRpm = rearSuspensionIndex >= 0 ? suspensionPoints[rearSuspensionIndex].WheelRpm : 0f;
            float frontRpm = frontSuspensionIndex >= 0 ? suspensionPoints[frontSuspensionIndex].WheelRpm : 0f;
            SuspensionController front = FrontSuspension;
            SuspensionController rear = RearSuspension;
            PhysicsMaterial chassisMaterial = GetComponent<Collider>()?.sharedMaterial;
            float evaluatedDownforce = EvaluateAerodynamicDownforceAcceleration(definition, stats,
                Mathf.Abs(SpeedMps));
            float extraVerticalAcceleration = -EvaluateAppliedDownforceVector(transform.up, evaluatedDownforce,
                anyWheelGrounded, chassisGrounded).y;
            Debug.Log($"{definition.displayName} PHYSICS | front={frontWheelGrounded}, rear={rearWheelGrounded}, " +
                      $"chassis={chassisGrounded}({chassisContactRegions}), airborne={Airborne}, " +
                      $"speed={SpeedMps:0.00}, velocity={body.linearVelocity}, wZ={body.angularVelocity.z:0.00}, " +
                      $"compression F/R={frontCompression:0.00}/{rearCompression:0.00}, " +
                      $"contactPoint F/R={front?.ContactPoint}/{rear?.ContactPoint}, " +
                      $"contactNormal F/R={front?.ContactNormal}/{rear?.ContactNormal}, " +
                      $"suspensionForce F/R={front?.LastSuspensionForce:0}/{rear?.LastSuspensionForce:0}N, " +
                      $"rpm F/R={frontRpm:0}/{rearRpm:0}, spring={stats.spring:0}, " +
                      $"compressionDamper={stats.compressionDampingMultiplier:0.00}, " +
                      $"reboundDamper={stats.reboundDampingMultiplier:0.00}, gravity={Physics.gravity}, " +
                      $"extraDown={extraVerticalAcceleration:0.00}m/s2, mass={body.mass:0}, " +
                      $"COM={body.centerOfMass}, physicsRadius={definition.wheelRadius:0.000}, " +
                      $"visualDiameter F/R={definition.FrontWheelDiameter:0.000}/" +
                      $"{definition.RearWheelDiameter:0.000}, chassisFriction=" +
                      $"{(chassisMaterial != null ? chassisMaterial.dynamicFriction : 0f):0.00}/" +
                      $"{(chassisMaterial != null ? chassisMaterial.staticFriction : 0f):0.00}, " +
                      $"impact={chassisImpactVelocity:0.00}, normal={chassisContactNormal}, " +
                      $"angularDamping={body.angularDamping:0.000}, contacts={chassisContactCount}, " +
                      $"fixedDt={Time.fixedDeltaTime:0.000}", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawPhysicsGizmos) return;
            Rigidbody sourceBody = body != null ? body : GetComponent<Rigidbody>();
            if (sourceBody != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(sourceBody.worldCenterOfMass, .08f);
                Gizmos.DrawLine(sourceBody.worldCenterOfMass,
                    sourceBody.worldCenterOfMass + sourceBody.transform.up * .35f);
            }

            SuspensionController[] points = suspensionPoints != null && suspensionPoints.Length > 0
                ? suspensionPoints : GetComponentsInChildren<SuspensionController>(true);
            for (int i = 0; i < points.Length; i++)
            {
                SuspensionController point = points[i];
                if (point == null) continue;
                Gizmos.color = point.IsGrounded ? Color.green : new Color(1f, .55f, 0f);
                Gizmos.DrawSphere(point.ContactPoint, .055f);
                Gizmos.DrawLine(point.ContactPoint, point.ContactPoint + point.ContactNormal * .5f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(point.SuspensionMountPoint,
                    point.SuspensionMountPoint + point.AppliedSuspensionForce * gizmoForceScale);
            }
        }

        public override void SetTransmissionMode(TransmissionMode mode)
        {
            if (transmission == null)
                transmission = GetComponent<TransmissionController>();
            if (definition != null && transmission.CurrentRatio == 0f)
                transmission.Initialize(definition, mode);
            else
                transmission.SetMode(mode);
        }

        public override void SetDriveEnabled(bool value)
        {
            driveEnabled = value;
            if (!value) driveDirection = DriveDirection.Forward;
            engine?.SetEnabled(value);
        }

        public override void AddFuel(float normalizedAmount) => fuel?.AddFuel(normalizedAmount);

        public void AssignRuntimeReferences(VehicleDefinition vehicleDefinition, VehicleInputController targetInput,
            SuspensionController[] points)
        {
            definition = vehicleDefinition;
            input = targetInput;
            suspensionPoints = points;
        }
    }
}
