using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(Rigidbody), typeof(EngineController), typeof(TransmissionController))]
    [RequireComponent(typeof(FuelSystem))]
    public sealed class VehicleController : VehicleRuntimeBase
    {
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private VehicleUpgradeState upgrades = new VehicleUpgradeState();
        [SerializeField] private VehicleInputController input;
        [SerializeField] private SuspensionController[] suspensionPoints;
        [SerializeField, Min(1f)] private float maxReverseDistanceFromProgress = 250f;
        [SerializeField, Min(1f)] private float reverseBoundaryDeceleration = 35f;
        [SerializeField, Range(2f, 5f)] private float reverseEngageSpeedThresholdKmh = 4f;
        [SerializeField, Range(10f, 60f)] private float maxReverseSpeedKmh = 40f;
        [SerializeField, Range(0.02f, 0.2f)] private float airborneGraceSeconds = 0.08f;
        [SerializeField, Min(0f)] private float groundedAngularDamping = .45f;
        [SerializeField, Min(1f)] private float collisionAngularVelocityLimit = 8f;

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

        public override float SpeedMps => body != null ? Vector3.Dot(body.linearVelocity, transform.right) : 0f;
        public override float ProgressPosition => furthestDistanceReached;
        public float FurthestDistanceReached => furthestDistanceReached;
        public float ReverseBoundaryX => furthestDistanceReached - maxReverseDistanceFromProgress;
        public DriveDirection Direction => driveDirection;
        public override float FuelNormalized => fuel != null ? fuel.FuelNormalized : 0f;
        public override float MotorTorque => engine != null ? engine.CurrentTorqueNm : 0f;
        public override float AppliedThrottle => engine != null ? engine.AppliedThrottle : 0f;
        public override float EngineRpm => engine != null ? engine.CurrentRpm : 0f;
        public override float WheelSlip => wheelSlip;
        public override bool AnyGrounded => anyGrounded;
        public override TransmissionController Transmission => transmission;
        public override VehicleDefinition Definition => definition;
        public override ResolvedVehicleStats Stats => stats;
        public override FuelSystem Fuel => fuel;
        public override Rigidbody Body => body;

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
                inertia.z *= Mathf.Clamp(definition.collisionPitchInertiaMultiplier, 1f, 6f);
                body.inertiaTensor = inertia;
                pitchInertiaConfigured = true;
            }
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxDepenetrationVelocity = definition.maximumDepenetrationVelocity;
            body.angularDamping = groundedAngularDamping;
            body.maxAngularVelocity = collisionAngularVelocityLimit;
            fuel.Initialize(definition, stats);
            engine.Initialize(definition, stats, fuel);
            transmission.Initialize(definition, TransmissionMode.Auto);
            furthestDistanceReached = body.position.x;

            if (suspensionPoints == null || suspensionPoints.Length == 0)
                suspensionPoints = GetComponentsInChildren<SuspensionController>();
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
            if (rawGrounded) lastGroundedTime = Time.fixedTime;
            anyGrounded = rawGrounded || Time.fixedTime - lastGroundedTime <= airborneGraceSeconds;

            float gas = driveEnabled ? inputState.Gas : 0f;
            float brake = driveEnabled ? inputState.BrakeRaw : 0f;
            UpdateDriveDirection(gas, brake);
            bool hasFuel = fuel != null && fuel.FuelNormalized > 0f;
            float engineThrottle = hasFuel && driveDirection == DriveDirection.Forward ? gas : 0f;
            transmission.Tick(Time.fixedDeltaTime, Mathf.Abs(SpeedMps), engineThrottle, AnyGrounded, false);
            float normalizedSpeed = Mathf.Max(0f, SpeedMps) / Mathf.Max(1f, stats.topSpeedMps);
            float engineTorque = engine.Tick(Time.fixedDeltaTime, engineThrottle, transmission.Rpm,
                transmission.TorqueScale, normalizedSpeed);

            ApplyDrive(engineTorque, gas, brake, hasFuel);
            ApplyDrag();
            ApplyAirControl(inputState, rawGrounded);
            ApplyAirborneDownforce(rawGrounded);
        }

        private void ApplyDrive(float engineTorque, float gas, float brake, bool hasFuel)
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
                    requestedWheelTorque = engineTorque * transmission.CurrentRatio *
                                           definition.transmission.finalDrive *
                                           definition.engine.drivetrainEfficiency;
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
                suspensionPoints[i].ApplyMotorTorque(perWheelTorque, stats.grip);
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
            float rollingForce = AnyGrounded ? definition.mass * Physics.gravity.magnitude * definition.grip.rollingResistance : 0f;
            body.AddForce(-velocity.normalized * (dragForce + rollingForce), ForceMode.Force);
        }

        private void ApplyAirControl(VehicleInputState inputState, bool hasWheelContact)
        {
            body.angularDamping = hasWheelContact ? groundedAngularDamping : definition.airAngularDamping;
            body.maxAngularVelocity = hasWheelContact ? collisionAngularVelocityLimit : definition.maxAirAngularVelocity;
            if (hasWheelContact || !driveEnabled) return;
            float control = inputState.Gas - inputState.BrakeRaw;
            if (Mathf.Abs(control) < .001f) return;
            float authority = definition.airControlTorque > .001f
                ? Mathf.Clamp(stats.airControlTorque / definition.airControlTorque, .5f, 1.2f) : 1f;
            float current = body.angularVelocity.z;
            float target = control * definition.maxAirAngularVelocity * authority;
            float acceleration = Mathf.Clamp((target - current) / Mathf.Max(.001f, Time.fixedDeltaTime),
                -definition.airAngularAcceleration * authority, definition.airAngularAcceleration * authority);
            body.AddTorque(Vector3.forward * acceleration, ForceMode.Acceleration);
        }

        private void ApplyAirborneDownforce(bool hasWheelContact)
        {
            if (hasWheelContact || stats.airborneDownforceAcceleration <= 0f) return;
            body.AddForce(Vector3.down * body.mass * stats.airborneDownforceAcceleration, ForceMode.Force);
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
