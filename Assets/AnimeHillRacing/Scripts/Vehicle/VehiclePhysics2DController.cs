using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(Rigidbody2D), typeof(EngineController), typeof(TransmissionController))]
    [RequireComponent(typeof(FuelSystem))]
    public sealed class VehiclePhysics2DController : VehicleRuntimeBase
    {
        [Header("Vehicle Data")]
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private VehicleUpgradeState upgrades = new VehicleUpgradeState();
        [SerializeField] private VehicleInputController input;
        [SerializeField] private Suspension2DController frontSuspension;
        [SerializeField] private Suspension2DController rearSuspension;

        [Header("2D Drivetrain Settings")]
        [SerializeField] private DriveMethod2D driveMethod = DriveMethod2D.RigidbodyTorque;
        [SerializeField, Range(0.05f, 0.35f)] private float torqueRampSeconds = 0.12f;
        [SerializeField, Range(10f, 60f)] private float maxReverseSpeedKmh = 35f;
        [SerializeField, Range(2f, 8f)] private float reverseEngageSpeedThresholdKmh = 4f;

        [Header("2D Suspension & Geometry Defaults")]
        [SerializeField, Range(2.0f, 12.0f)] private float suspensionFrequency = 4.5f;
        [SerializeField, Range(0.2f, 1.5f)] private float suspensionDampingRatio = 0.75f;
        [SerializeField, Range(0.1f, 0.6f)] private float suspensionTravel = 0.35f;

        [Header("Air Control")]
        [SerializeField, Range(0.02f, 0.20f)] private float airborneGraceSeconds = 0.08f;
        [SerializeField] private float maxAirAngularVelocity = 2.5f;
        [SerializeField] private float airAngularAcceleration = 14f;

        private Rigidbody2D body;
        private Collider2D chassisCollider;
        private EngineController engine;
        private TransmissionController transmission;
        private FuelSystem fuel;
        private ResolvedVehicleStats stats;
        private bool driveEnabled = true;
        private float currentDriveTorque;
        private DriveDirection driveDirection = DriveDirection.Forward;
        private float lastGroundedTime = float.NegativeInfinity;
        private bool chassisContactSinceLastStep;
        private float surfaceGripMultiplier = 1f;

        public override float SpeedMps => body != null ? Vector2.Dot(body.linearVelocity, (Vector2)body.transform.right) : 0f;
        public override float ProgressPosition => body != null ? body.position.x : transform.position.x;
        public override float FuelNormalized => fuel != null ? fuel.FuelNormalized : 1f;
        public override float MotorTorque => currentDriveTorque;
        public override float AppliedThrottle => input != null ? input.State.Gas : 0f;
        public override float EngineRpm => engine != null ? engine.CurrentRpm : 0f;
        public override float WheelSlip => 0f;
        public override bool FrontWheelGrounded => frontSuspension != null && frontSuspension.IsGrounded;
        public override bool RearWheelGrounded => rearSuspension != null && rearSuspension.IsGrounded;
        public override bool AnyWheelGrounded => FrontWheelGrounded || RearWheelGrounded;
        public override bool BothWheelsGrounded => FrontWheelGrounded && RearWheelGrounded;
        public override bool ChassisGrounded => chassisContactSinceLastStep;
        public override bool FullyAirborne => !AnyWheelGrounded && !ChassisGrounded;
        public override bool Airborne => FullyAirborne;
        public override bool AnyGrounded => AnyWheelGrounded || (Time.fixedTime - lastGroundedTime <= airborneGraceSeconds);
        public override TransmissionController Transmission => transmission;
        public override VehicleDefinition Definition => definition;
        public override ResolvedVehicleStats Stats => stats;
        public override FuelSystem Fuel => fuel;
        public override Rigidbody2D Body2D => body;

        public Suspension2DController FrontSuspension => frontSuspension;
        public Suspension2DController RearSuspension => rearSuspension;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            chassisCollider = GetComponent<Collider2D>();
            engine = GetComponent<EngineController>();
            transmission = GetComponent<TransmissionController>();
            fuel = GetComponent<FuelSystem>();

            if (definition == null)
            {
                enabled = false;
                return;
            }

            upgrades.CopyFrom(SaveManager.LoadUpgrades(definition.vehicleId, definition));
            upgrades.ClampTo(definition);
            stats = definition.Resolve(upgrades);

            ConfigureChassisBody();
            fuel.Initialize(definition, stats);
            engine.Initialize(definition, stats, fuel);
            transmission.Initialize(definition, TransmissionMode.Auto);

            if (input == null) input = GetComponentInParent<VehicleInputController>() ?? FindFirstObjectByType<VehicleInputController>();
            InitializeSuspensions();
        }

        private void ConfigureChassisBody()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = definition.mass;
            body.linearDamping = 0.05f;
            body.angularDamping = definition.twoWheelAngularDamping;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Center of Mass: slightly lower to match stable HCR-like chassis balance
            body.centerOfMass = new Vector2(definition.centerOfMass.x, definition.centerOfMass.y);

            // Pitch Inertia: natural from collider geometry + mild multiplier
            if (definition.collisionPitchInertiaMultiplier > 1f)
            {
                // In 2D, Rigidbody2D calculates inertia from collider shapes automatically.
            }
        }

        private void InitializeSuspensions()
        {
            if (frontSuspension == null || rearSuspension == null)
            {
                Suspension2DController[] points = GetComponentsInChildren<Suspension2DController>(true);
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].name.ToLower().Contains("front")) frontSuspension = points[i];
                    else if (points[i].name.ToLower().Contains("rear")) rearSuspension = points[i];
                }
            }

            float halfBase = definition.halfWheelbase;
            float axleY = -0.45f;
            float radius = definition.wheelRadius > 0.05f ? definition.wheelRadius : 0.45f;

            // Tune-based suspension scaling: higher tune increases damping and high-impact control
            float tuneProgress = definition.suspensionMaxTunes > 1
                ? (upgrades.suspensionTune - 1f) / (definition.suspensionMaxTunes - 1f) : 0f;
            float tunedFrequency = Mathf.Lerp(suspensionFrequency * 0.85f, suspensionFrequency * 1.15f, tuneProgress);
            float tunedDamping = Mathf.Lerp(suspensionDampingRatio * 0.80f, suspensionDampingRatio * 1.30f, tuneProgress);

            if (frontSuspension != null)
            {
                frontSuspension.Initialize(body, definition, stats, halfBase, axleY, radius,
                    suspensionTravel, tunedFrequency, tunedDamping, stats.grip, driveMethod);
            }
            if (rearSuspension != null)
            {
                rearSuspension.Initialize(body, definition, stats, -halfBase, axleY, radius,
                    suspensionTravel, tunedFrequency, tunedDamping, stats.grip, driveMethod);
            }
        }

        public void AssignRuntimeReferences(VehicleDefinition def, VehicleInputController inputController,
            Suspension2DController front, Suspension2DController rear)
        {
            definition = def;
            input = inputController;
            frontSuspension = front;
            rearSuspension = rear;
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (chassisCollider == null) chassisCollider = GetComponent<Collider2D>();
            if (engine == null) engine = GetComponent<EngineController>();
            if (transmission == null) transmission = GetComponent<TransmissionController>();
            if (fuel == null) fuel = GetComponent<FuelSystem>();

            if (definition != null && body != null)
            {
                upgrades.CopyFrom(SaveManager.LoadUpgrades(definition.vehicleId, definition));
                upgrades.ClampTo(definition);
                stats = definition.Resolve(upgrades);
                ConfigureChassisBody();
                fuel?.Initialize(definition, stats);
                engine?.Initialize(definition, stats, fuel);
                transmission?.Initialize(definition, TransmissionMode.Auto);
                InitializeSuspensions();
            }
        }

        public void SetSurfaceGripMultiplier(float mult)
        {
            surfaceGripMultiplier = Mathf.Clamp(mult, 0.25f, 1.0f);
        }

        private void FixedUpdate()
        {
            StepPhysics(Time.fixedDeltaTime, input != null ? input.State : default);
        }

        private MapProfile terrainProfile;

        public bool FrontWheelBelowSurface => frontSuspension != null && frontSuspension.IsBelowSurface;
        public bool RearWheelBelowSurface => rearSuspension != null && rearSuspension.IsBelowSurface;
        public bool FrontWheelContactUnstable => frontSuspension != null && frontSuspension.IsContactUnstable;
        public bool RearWheelContactUnstable => rearSuspension != null && rearSuspension.IsContactUnstable;

        public void SetTerrainProfile(MapProfile profile) => terrainProfile = profile;

        public void ResetGlitchState()
        {
            if (frontSuspension != null) frontSuspension.ResetTerrainState();
            if (rearSuspension != null) rearSuspension.ResetTerrainState();
        }

        public void StepPhysics(float dt, VehicleInputState inputState)
        {
            if (definition == null || body == null) return;

            // 1. Simulate independent wheels with real Box2D contact solver observation
            if (frontSuspension != null) frontSuspension.Simulate(dt, terrainProfile);
            if (rearSuspension != null) rearSuspension.Simulate(dt, terrainProfile);

            // 2. Update grounded tracking
            bool rawGrounded = AnyWheelGrounded || chassisContactSinceLastStep;
            if (rawGrounded) lastGroundedTime = Time.fixedTime;
            chassisContactSinceLastStep = false;

            // 3. Process Input & Drive
            float gas = driveEnabled ? inputState.Gas : 0f;
            float brake = driveEnabled ? inputState.BrakeRaw : 0f;
            UpdateDriveDirection(gas, brake);

            bool hasFuel = fuel != null ? fuel.FuelNormalized > 0f : true;
            float engineThrottle = hasFuel && driveDirection == DriveDirection.Forward ? gas : 0f;
            if (transmission != null) transmission.Tick(dt, Mathf.Abs(SpeedMps), engineThrottle, AnyGrounded, false);
            float normalizedSpeed = Mathf.Max(0f, SpeedMps) / Mathf.Max(1f, stats.topSpeedMps);
            if (engine != null && transmission != null) engine.Tick(dt, engineThrottle, transmission.Rpm, transmission.TorqueScale, normalizedSpeed);

            ApplyDrive(gas, brake, hasFuel, dt);
            ApplyDrag();
            ApplyAerodynamicDownforce();
            ApplyAirControl(inputState, dt);
        }

        private void ApplyDrive(float gas, float brake, bool hasFuel, float dt)
        {
            float speed = SpeedMps;
            float targetTotalTorque = 0f;
            float targetBrakeTorque = 0f;

            if (!hasFuel)
            {
                targetBrakeTorque = definition.brakeForce * definition.wheelRadius * brake;
            }
            else switch (driveDirection)
            {
                case DriveDirection.BrakingToReverse:
                    targetBrakeTorque = definition.brakeForce * definition.wheelRadius * brake;
                    break;

                case DriveDirection.Reverse:
                    float revPitchAngle = body != null ? -body.rotation : 0f;
                    float revSlopeComp = body != null && AnyWheelGrounded
                        ? body.mass * Physics2D.gravity.magnitude * Mathf.Sin(Mathf.Deg2Rad * Mathf.Clamp(revPitchAngle, -65f, 65f)) : 0f;
                    targetTotalTorque = brake > 0f && speed > -maxReverseSpeedKmh / 3.6f
                        ? -(definition.reverseForce + Mathf.Max(0f, revSlopeComp)) * definition.wheelRadius * brake : 0f;
                    break;

                case DriveDirection.BrakingToForward:
                    targetBrakeTorque = definition.brakeForce * definition.wheelRadius * gas;
                    break;

                default: // Forward
                    float forwardSpeed = Mathf.Max(0f, speed);
                    float targetAcc = stats.targetAccelerationMps2 * Mathf.Max(1f, stats.lowSpeedTorqueMultiplier);

                    // Evaluate progressive high-speed authority (keeps 0-100 exact, soft cruise up to Vmax)
                    float driveAcc = EvaluateDriveAcceleration2D(targetAcc, forwardSpeed, stats.topSpeedMps);

                    // Rally Turbo boost
                    if (definition.archetype == VehicleArchetype.Rally && upgrades.turboTune > 1)
                    {
                        float turboRatio = (upgrades.turboTune - 1f) / Mathf.Max(1f, definition.turboMaxTunes - 1f);
                        driveAcc += turboRatio * 3.5f;
                    }

                    float resistanceCompensation = definition.grip.aerodynamicDrag * forwardSpeed * forwardSpeed +
                                                   body.mass * Physics2D.gravity.magnitude * definition.grip.rollingResistance;

                    // Hill & slope gravitational compensation: preserves climbing ability on steep hills and obstacles
                    float pitchAngle = body != null ? body.rotation : 0f;
                    float slopeCompensation = body != null && AnyWheelGrounded
                        ? body.mass * Physics2D.gravity.magnitude * Mathf.Sin(Mathf.Deg2Rad * Mathf.Clamp(pitchAngle, -65f, 65f)) : 0f;

                    float requiredForce = (body.mass * driveAcc + resistanceCompensation + Mathf.Max(0f, slopeCompensation)) * gas;
                    targetTotalTorque = requiredForce * definition.wheelRadius;
                    break;
            }

            // Smooth torque ramp to eliminate instant wheelie shock on start from standstill
            float baseTorqueCap = Mathf.Max(Mathf.Abs(targetTotalTorque), body.mass * stats.targetAccelerationMps2 * definition.wheelRadius);
            float rampRate = Mathf.Max(100f, baseTorqueCap / Mathf.Max(0.05f, torqueRampSeconds));
            currentDriveTorque = Mathf.MoveTowards(currentDriveTorque, targetTotalTorque, rampRate * dt);

            // Equal 50/50 torque distribution to all active wheels (eliminates the runaway 75% rear wheelie loop)
            float perWheelTorque = currentDriveTorque * 0.5f;
            float perWheelBrake = targetBrakeTorque * 0.5f;

            if (frontSuspension != null)
            {
                frontSuspension.ApplyMotorTorque(perWheelTorque, stats.grip * surfaceGripMultiplier);
                frontSuspension.ApplyBrakeTorque(perWheelBrake);
            }
            if (rearSuspension != null)
            {
                rearSuspension.ApplyMotorTorque(perWheelTorque, stats.grip * surfaceGripMultiplier);
                rearSuspension.ApplyBrakeTorque(perWheelBrake);
            }
        }

        private void UpdateDriveDirection(float gas, float brake)
        {
            if (!driveEnabled) { driveDirection = DriveDirection.Forward; return; }
            float speed = SpeedMps;
            float threshold = reverseEngageSpeedThresholdKmh / 3.6f;

            if (gas > 0.05f)
            {
                // When pressing gas, immediately engage forward unless moving backwards at high speed
                driveDirection = speed < -threshold * 2.5f ? DriveDirection.BrakingToForward : DriveDirection.Forward;
            }
            else if (brake > 0.05f)
            {
                // When pressing brake, decelerate forward motion first, then engage reverse
                driveDirection = speed > threshold ? DriveDirection.BrakingToReverse : DriveDirection.Reverse;
            }
            else
            {
                driveDirection = speed < -0.5f ? DriveDirection.Reverse : DriveDirection.Forward;
            }
        }

        private void ApplyDrag()
        {
            Vector2 velocity = body.linearVelocity;
            float speed = velocity.magnitude;
            if (speed < 0.01f) return;

            float aeroDrag = definition.grip.aerodynamicDrag * speed * speed;
            float rollingDrag = AnyWheelGrounded
                ? body.mass * Physics2D.gravity.magnitude * definition.grip.rollingResistance : 0f;

            body.AddForce(-velocity.normalized * (aeroDrag + rollingDrag), ForceMode2D.Force);
        }

        private void ApplyAerodynamicDownforce()
        {
            if (definition.archetype != VehicleArchetype.Formula || stats.airborneDownforceAcceleration <= 0.01f) return;
            float forwardSpeed = Mathf.Max(0f, SpeedMps);
            float speed01 = Mathf.Clamp01(forwardSpeed / Mathf.Max(1f, definition.downforceReferenceSpeedMps));
            float downforceAccel = stats.airborneDownforceAcceleration * speed01 * speed01;

            if (downforceAccel > 0.01f)
            {
                Vector2 downDirection = -body.transform.up;
                body.AddForce(downDirection * body.mass * downforceAccel, ForceMode2D.Force);
            }
        }

        private void ApplyAirControl(VehicleInputState inputState, float dt)
        {
            // CRITICAL RULE: If ANY wheel touches ground or if chassis is grounded, AIR CONTROL = 0!
            if (FrontWheelGrounded || RearWheelGrounded || ChassisGrounded)
            {
                body.angularDamping = definition.twoWheelAngularDamping;
                return;
            }

            // Require airborne confirmation grace window
            if (Time.fixedTime - lastGroundedTime < airborneGraceSeconds)
            {
                body.angularDamping = definition.airAngularDamping;
                return;
            }

            body.angularDamping = definition.airAngularDamping;
            if (!driveEnabled) return;

            // GAS airborne = nose-up (counter-clockwise, positive Z in 2D facing right), BRAKE airborne = nose-down (negative Z)
            float control = inputState.Gas - inputState.BrakeRaw;
            if (Mathf.Abs(control) < 0.01f) return;

            float targetAngularVelocityDeg = control * maxAirAngularVelocity * Mathf.Rad2Deg;
            float currentAngularVelocityDeg = body.angularVelocity;
            float velocityDifferenceDeg = targetAngularVelocityDeg - currentAngularVelocityDeg;
            float maxStepDeg = airAngularAcceleration * Mathf.Rad2Deg * dt;
            float appliedStepDeg = Mathf.Clamp(velocityDifferenceDeg, -maxStepDeg, maxStepDeg);

            // Correct physical torque: tau = I * alpha
            float appliedTorque = (body.inertia * appliedStepDeg * Mathf.Deg2Rad) / Mathf.Max(0.001f, dt);
            body.AddTorque(appliedTorque, ForceMode2D.Force);
        }

        private static float EvaluateDriveAcceleration2D(float zeroToHundredAcceleration, float speed, float topSpeed)
        {
            float tunedAcc = Mathf.Max(0.5f, zeroToHundredAcceleration);
            float safeTop = Mathf.Max(1f, topSpeed);
            if (speed >= safeTop) return 0f;

            const float hundredKmh = 100f / 3.6f;
            if (speed <= hundredKmh || safeTop <= hundredKmh) return tunedAcc;

            float highSpeed01 = Mathf.InverseLerp(hundredKmh, safeTop, speed);
            float smooth = Mathf.SmoothStep(0f, 1f, highSpeed01);
            return Mathf.Lerp(tunedAcc, tunedAcc * 0.12f, smooth);
        }

        public override void SetDriveEnabled(bool value) => driveEnabled = value;

        public override void AddFuel(float normalizedAmount) => fuel?.AddFuel(normalizedAmount);

        public override void TeleportToPosition(Vector3 targetPosition)
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null) return;

            Vector2 target = new Vector2(targetPosition.x, targetPosition.y);
            Vector2 delta = target - body.position;
            body.position = target;
            body.rotation = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            transform.position = new Vector3(target.x, target.y, targetPosition.z);
            transform.rotation = Quaternion.identity;

            MoveWheelWithChassis(frontSuspension, delta);
            MoveWheelWithChassis(rearSuspension, delta);
            ResetGlitchState();
            Physics2D.SyncTransforms();
        }

        public void SetWholeRigVelocity(Vector2 velocity)
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = velocity;
            SetWheelVelocity(frontSuspension, velocity);
            SetWheelVelocity(rearSuspension, velocity);
        }

        private static void MoveWheelWithChassis(Suspension2DController suspension, Vector2 delta)
        {
            Rigidbody2D wheel = suspension != null ? suspension.WheelBody : null;
            if (wheel == null) return;
            wheel.position += delta;
            wheel.rotation = 0f;
            wheel.linearVelocity = Vector2.zero;
            wheel.angularVelocity = 0f;
        }

        private static void SetWheelVelocity(Suspension2DController suspension, Vector2 velocity)
        {
            Rigidbody2D wheel = suspension != null ? suspension.WheelBody : null;
            if (wheel == null) return;
            wheel.linearVelocity = velocity;
            wheel.angularVelocity = 0f;
        }

        private void OnCollisionEnter2D(Collision2D collision) => chassisContactSinceLastStep = true;
        private void OnCollisionStay2D(Collision2D collision) => chassisContactSinceLastStep = true;
    }
}
