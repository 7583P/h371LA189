using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>
    /// One physical wheel and its suspension. The wheel has its own rigidbody/collider and is constrained to
    /// the chassis by a ConfigurableJoint. Spring and damper forces are equal-and-opposite on the sprung and
    /// unsprung masses; propulsion is torque on this wheel, never a longitudinal chassis force.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuspensionController : MonoBehaviour
    {
        [SerializeField] private Transform wheelVisual;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Rigidbody wheelBody;
        [SerializeField] private SphereCollider wheelCollider;
        [SerializeField] private ConfigurableJoint suspensionJoint;
        [Header("Optional anomaly diagnostics")]
        [SerializeField] private bool logSuspensionAnomalies;
        [SerializeField, Min(.001f)] private float axisDriftToleranceDegrees = .1f;
        [SerializeField, Min(.00001f)] private float anchorDriftTolerance = .0005f;

        private Rigidbody body;
        private PhysicsMaterial runtimeTireMaterial;
        private float anchorLocalX;
        private float restLength;
        private float maximumCompression;
        private float maximumExtension;
        private float wheelRadius;
        private float spring;
        private float damping;
        private float compressionDampingMultiplier;
        private float reboundDampingMultiplier;
        private float bumpStopStart;
        private float progressiveSpringMultiplier;
        private float hardStopSpringMultiplier;
        private float maximumNormalForce;
        private float supportedMass;
        private float maximumReboundAcceleration;
        private float wheelContactTolerance;
        private float landingDampingMultiplier;
        private float landingDampingDuration;
        private float landingDampingUntil = float.NegativeInfinity;
        private float lastLandingImpactSpeed;
        private float lastSuspensionForce;
        private float lastSpringForce;
        private float lastDamperForce;
        private float lastRelativeVelocity;
        private float lastAppliedMotorTorque;
        private float lastContactNormalForce;
        private float currentLength;
        private float tractionBlend;
        private float slipThreshold;
        private float slidingGripMultiplier;
        private float tractionRecoveryPerSecond;
        private bool contactSinceLastStep;
        private bool wasGrounded;
        private bool motionSynchronized;
        private Vector3 contactNormal = Vector3.up;
        private Vector3 contactPoint;
        private Vector3 expectedJointAnchor;
        private Vector3 expectedConnectedAnchor;
        private float nextDiagnosticLogTime;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        public bool IsGrounded { get; private set; }
        public RaycastHit GroundHit { get; private set; }
        public float Compression01 { get; private set; }
        public float CompressionFromRest => restLength - currentLength;
        public float LastNormalForce => Mathf.Max(lastSuspensionForce, lastContactNormalForce);
        public float RestLength => restLength;
        public float MaximumCompression => maximumCompression;
        public float MaximumExtension => maximumExtension;
        public float CurrentLength => currentLength;
        public float Slip01 { get; private set; }
        public float WheelAngularVelocity => wheelBody != null ? -wheelBody.angularVelocity.z : 0f;
        public float WheelRpm => WheelAngularVelocity * 60f / (2f * Mathf.PI);
        public float PhysicalWheelRadius => wheelRadius;
        public bool LandingDamperActive => IsGrounded && Time.fixedTime <= landingDampingUntil;
        public float LastLandingImpactSpeed => lastLandingImpactSpeed;
        public Rigidbody WheelBody => wheelBody;
        public SphereCollider WheelCollider => wheelCollider;
        public ConfigurableJoint SuspensionJoint => suspensionJoint;
        public Vector3 SuspensionAxisWorld => suspensionJoint != null
            ? suspensionJoint.transform.TransformDirection(suspensionJoint.secondaryAxis).normalized
            : body != null ? body.transform.up : Vector3.up;
        public Vector3 ContactNormal => contactNormal;
        public Vector3 ContactPoint => contactPoint;
        public Vector3 SuspensionMountPoint => body != null
            ? body.transform.TransformPoint(new Vector3(anchorLocalX, 0f, 0f)) : transform.position;
        public Vector3 AppliedSuspensionForce => SuspensionAxisWorld * lastSuspensionForce;
        public float LastSpringForce => lastSpringForce;
        public float LastDamperForce => lastDamperForce;
        public float LastSuspensionForce => lastSuspensionForce;
        public float LastRelativeVelocity => lastRelativeVelocity;
        public float LastAppliedMotorTorque => lastAppliedMotorTorque;
        public float CompressionDampingMultiplier => compressionDampingMultiplier;
        public float ReboundDampingMultiplier => reboundDampingMultiplier;
        public float MaximumReboundAcceleration => maximumReboundAcceleration;
        public float JointTranslation => suspensionJoint != null && wheelBody != null
            ? Vector3.Dot(wheelBody.transform.TransformPoint(suspensionJoint.connectedAnchor) -
                          suspensionJoint.transform.TransformPoint(suspensionJoint.anchor), SuspensionAxisWorld)
            : 0f;

        public void Initialize(Rigidbody targetBody, VehicleDefinition definition, ResolvedVehicleStats stats,
            int suspensionPointCount = 2)
        {
            body = targetBody;
            anchorLocalX = body.transform.InverseTransformPoint(transform.position).x;
            restLength = definition.suspension.restLength;
            SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                out maximumCompression, out maximumExtension);
            wheelRadius = definition.wheelRadius;
            float equalSupportedMass = targetBody.mass / Mathf.Max(1, suspensionPointCount);
            supportedMass = SuspensionPhysics.CalculateAxleSupportedMass(targetBody.mass,
                definition.centerOfMass.x, definition.halfWheelbase, anchorLocalX, suspensionPointCount);
            float axleLoadScale = supportedMass / Mathf.Max(.01f, equalSupportedMass);
            spring = stats.spring * axleLoadScale;
            damping = stats.damping * axleLoadScale;
            compressionDampingMultiplier = stats.compressionDampingMultiplier;
            reboundDampingMultiplier = stats.reboundDampingMultiplier;
            bumpStopStart = definition.suspension.bumpStopStart;
            progressiveSpringMultiplier = stats.progressiveSpringMultiplier;
            hardStopSpringMultiplier = definition.suspension.hardStopSpringMultiplier;
            maximumReboundAcceleration = stats.maximumReboundAcceleration;
            wheelContactTolerance = stats.wheelContactTolerance;
            landingDampingMultiplier = stats.landingDampingMultiplier;
            landingDampingDuration = stats.landingDampingDuration;
            maximumNormalForce = supportedMass * definition.suspension.maximumAcceleration;
            slipThreshold = stats.slipThreshold;
            slidingGripMultiplier = stats.slidingGripMultiplier;
            tractionRecoveryPerSecond = stats.tractionRecoveryPerSecond;

            EnsurePhysicalWheel(definition, stats.grip);
            ConfigureHardTravelLimit();
            currentLength = restLength;
            motionSynchronized = false;
            wasGrounded = false;
            landingDampingUntil = float.NegativeInfinity;
            lastLandingImpactSpeed = 0f;
            contactPoint = wheelBody.position - body.transform.up * wheelRadius;
            SyncVisual();
        }

        private void EnsurePhysicalWheel(VehicleDefinition definition, float physicalGrip)
        {
            // Do not use ??= with UnityEngine.Object. A deserialized missing reference can be a CLR-non-null
            // "fake null", which would skip GetComponent and then make AddComponent fail when the component
            // already exists on a regenerated prefab.
            if (wheelBody == null) wheelBody = GetComponent<Rigidbody>();
            if (wheelBody == null) wheelBody = gameObject.AddComponent<Rigidbody>();
            if (wheelCollider == null) wheelCollider = GetComponent<SphereCollider>();
            if (wheelCollider == null) wheelCollider = gameObject.AddComponent<SphereCollider>();
            EnsureChassisRelativeJoint();

            if (wheelBody == null || wheelCollider == null || suspensionJoint == null)
                throw new MissingComponentException($"{name}: independent wheel components could not be initialized.");

            Vector3 localPosition = body.transform.InverseTransformPoint(transform.position);
            localPosition.x = anchorLocalX;
            if (Mathf.Abs(localPosition.y) < .02f)
                localPosition.y = -restLength;
            localPosition.z = 0f;
            transform.position = body.transform.TransformPoint(localPosition);
            transform.rotation = body.rotation;

            wheelBody.mass = definition.wheelMass;
            wheelBody.linearDamping = .02f;
            wheelBody.angularDamping = .08f;
            wheelBody.interpolation = RigidbodyInterpolation.Interpolate;
            wheelBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            wheelBody.maxDepenetrationVelocity = definition.maximumDepenetrationVelocity;
            wheelBody.solverIterations = Mathf.Max(wheelBody.solverIterations, 12);
            wheelBody.solverVelocityIterations = Mathf.Max(wheelBody.solverVelocityIterations, 8);
            wheelBody.maxAngularVelocity = Mathf.Max(40f, statsTopWheelSpeed(definition) * 1.65f);
            wheelBody.constraints = RigidbodyConstraints.FreezePositionZ |
                                    RigidbodyConstraints.FreezeRotationX |
                                    RigidbodyConstraints.FreezeRotationY;

            wheelCollider.radius = wheelRadius;
            wheelCollider.center = Vector3.zero;
            wheelCollider.isTrigger = false;
            PhysicsMaterial sourceMaterial = wheelCollider.sharedMaterial;
            runtimeTireMaterial = new PhysicsMaterial("Runtime Arcade Tire");
            if (sourceMaterial != null)
            {
                runtimeTireMaterial.dynamicFriction = sourceMaterial.dynamicFriction;
                runtimeTireMaterial.staticFriction = sourceMaterial.staticFriction;
                runtimeTireMaterial.bounciness = sourceMaterial.bounciness;
                runtimeTireMaterial.frictionCombine = sourceMaterial.frictionCombine;
                runtimeTireMaterial.bounceCombine = sourceMaterial.bounceCombine;
            }
            runtimeTireMaterial.name = definition.displayName + " Tire (Runtime)";
            runtimeTireMaterial.dynamicFriction = physicalGrip;
            runtimeTireMaterial.staticFriction = physicalGrip * 1.04f;
            runtimeTireMaterial.bounciness = 0f;
            // Multiply preserves the global 0-1000 Grip scale. Maximum imposed the terrain's .72
            // coefficient as a hidden floor, making even a 200-rated tire feel glued down.
            runtimeTireMaterial.frictionCombine = PhysicsMaterialCombine.Multiply;
            runtimeTireMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            runtimeTireMaterial.hideFlags = HideFlags.DontSave;
            wheelCollider.sharedMaterial = runtimeTireMaterial;

            float statsTopWheelSpeed(VehicleDefinition source) =>
                source.topSpeedMps / Mathf.Max(.05f, source.wheelRadius);
        }

        private void EnsureChassisRelativeJoint()
        {
            ConfigurableJoint legacyWheelJoint = suspensionJoint;
            if (legacyWheelJoint == null) legacyWheelJoint = GetComponent<ConfigurableJoint>();

            suspensionJoint = null;
            ConfigurableJoint[] chassisJoints = body.GetComponents<ConfigurableJoint>();
            for (int i = 0; i < chassisJoints.Length; i++)
                if (chassisJoints[i] != null && chassisJoints[i].connectedBody == wheelBody)
                {
                    suspensionJoint = chassisJoints[i];
                    break;
                }

            if (suspensionJoint == null)
                suspensionJoint = body.gameObject.AddComponent<ConfigurableJoint>();

            if (legacyWheelJoint == null || legacyWheelJoint.gameObject == body.gameObject ||
                legacyWheelJoint == suspensionJoint) return;
            legacyWheelJoint.connectedBody = null;
            legacyWheelJoint.xMotion = ConfigurableJointMotion.Free;
            legacyWheelJoint.yMotion = ConfigurableJointMotion.Free;
            legacyWheelJoint.zMotion = ConfigurableJointMotion.Free;
            legacyWheelJoint.angularXMotion = ConfigurableJointMotion.Free;
            legacyWheelJoint.angularYMotion = ConfigurableJointMotion.Free;
            legacyWheelJoint.angularZMotion = ConfigurableJointMotion.Free;
            if (Application.isPlaying) Destroy(legacyWheelJoint);
            else DestroyImmediate(legacyWheelJoint);
        }

        private void ConfigureHardTravelLimit()
        {
            float travel = maximumCompression + maximumExtension;
            float centerLength = restLength + (maximumExtension - maximumCompression) * .5f;
            // ConfigurableJoint axes are expressed in the component transform's local frame. The component
            // therefore belongs to the non-rolling chassis: mounting it on the rolling wheel made its limited
            // Y axis rotate on every revolution and created the wandering suspension axis.
            suspensionJoint.connectedBody = wheelBody;
            suspensionJoint.autoConfigureConnectedAnchor = false;
            suspensionJoint.configuredInWorldSpace = false;
            suspensionJoint.anchor = new Vector3(anchorLocalX, -centerLength, 0f);
            suspensionJoint.connectedAnchor = Vector3.zero;
            suspensionJoint.axis = Vector3.right;
            suspensionJoint.secondaryAxis = Vector3.up;
            suspensionJoint.xMotion = ConfigurableJointMotion.Locked;
            suspensionJoint.yMotion = ConfigurableJointMotion.Limited;
            suspensionJoint.zMotion = ConfigurableJointMotion.Locked;
            suspensionJoint.angularXMotion = ConfigurableJointMotion.Locked;
            suspensionJoint.angularYMotion = ConfigurableJointMotion.Locked;
            suspensionJoint.angularZMotion = ConfigurableJointMotion.Free;
            SoftJointLimit limit = suspensionJoint.linearLimit;
            limit.limit = travel * .5f;
            limit.bounciness = 0f;
            limit.contactDistance = Mathf.Min(.015f, travel * .08f);
            suspensionJoint.linearLimit = limit;
            SoftJointLimitSpring limitSpring = suspensionJoint.linearLimitSpring;
            limitSpring.spring = 0f;
            // The custom force already damps the full travel. A limit damper duplicated that force at the
            // most solver-sensitive point, so this joint remains a geometric travel constraint only.
            limitSpring.damper = 0f;
            suspensionJoint.linearLimitSpring = limitSpring;
            suspensionJoint.enableCollision = false;
            suspensionJoint.enablePreprocessing = true;
            // PhysX joints become noisy when a light wheel is constrained directly against a much heavier
            // chassis. Scale only the connected body's inverse mass for the constraint solve so the effective
            // ratio stays at or below 8:1; the actual Rigidbody masses and all external forces remain intact.
            suspensionJoint.massScale = 1f;
            suspensionJoint.connectedMassScale = Mathf.Clamp(
                8f * wheelBody.mass / Mathf.Max(1f, body.mass), .2f, 1f);
            // Position projection teleports a joint back inside its limit after a large solver error.
            // On hard landings that correction becomes a visible second kick, and at high road speed it
            // presents as wheel jitter. The increased solver iteration budget resolves the physical limit
            // continuously, so projection must stay disabled.
            suspensionJoint.projectionMode = JointProjectionMode.None;
            suspensionJoint.breakForce = Mathf.Infinity;
            suspensionJoint.breakTorque = Mathf.Infinity;
            expectedJointAnchor = suspensionJoint.anchor;
            expectedConnectedAnchor = suspensionJoint.connectedAnchor;
        }

        public void Simulate()
        {
            if (body == null || wheelBody == null) return;

            // A physical wheel is a separate rigidbody. When a vehicle is spawned or respawned with an
            // existing chassis velocity, initialize the unsprung mass to the same rigid motion before the
            // joint and damper solve their first step; otherwise the suspension receives a non-physical
            // impulse caused solely by mismatched initial states.
            if (!motionSynchronized)
            {
                Vector3 pointVelocity = body.GetPointVelocity(wheelBody.worldCenterOfMass);
                wheelBody.linearVelocity = pointVelocity;
                float rollingSpeed = Vector3.Dot(pointVelocity, body.transform.right);
                wheelBody.angularVelocity = body.angularVelocity +
                                            Vector3.back * (rollingSpeed / Mathf.Max(.05f, wheelRadius));
                motionSynchronized = true;
            }

            Vector3 up = body.transform.up;
            Vector3 anchor = body.transform.TransformPoint(new Vector3(anchorLocalX, 0f, 0f));
            float measuredLength = Vector3.Dot(anchor - wheelBody.position, up);
            currentLength = SuspensionPhysics.ClampLength(restLength, maximumCompression,
                maximumExtension, measuredLength);
            float rawCompression = restLength - measuredLength;
            float relativeVelocity = Vector3.Dot(body.GetPointVelocity(anchor) - wheelBody.linearVelocity, up);
            lastRelativeVelocity = relativeVelocity;
            bool probedGround = ProbeGround(out RaycastHit hit);
            // Each wheel owns its own contact probe. The probe is constrained to the physical tire radius
            // plus a small authored skin, so it represents that wheel touching the surface; it never promotes
            // the opposite axle or the complete vehicle to grounded.
            IsGrounded = contactSinceLastStep || probedGround;
            if (IsGrounded && !wasGrounded && relativeVelocity < -.65f)
            {
                lastLandingImpactSpeed = -relativeVelocity;
                landingDampingUntil = Time.fixedTime + landingDampingDuration;
            }
            bool landingSettling = IsGrounded && Time.fixedTime <= landingDampingUntil;
            // Extra compression damping on a hard touchdown transfers the wheel impact into an upward
            // chassis impulse. Let the existing compression tune consume the impact through travel, then
            // use the short settling window only while the spring is extending and returning stored energy.
            float activeCompressionDamping = compressionDampingMultiplier;
            float activeReboundDamping = reboundDampingMultiplier *
                                         (landingSettling && relativeVelocity >= 0f
                                             ? Mathf.Max(1f, landingDampingMultiplier) : 1f);
            float activeDamping = damping;
            if (!IsGrounded)
            {
                // On the road the contact constraint reacts against the damper. In free flight the same
                // coefficient acts only between the light wheel and the much heavier chassis. The sprung-mass
                // damping coefficient can then remove more relative velocity than exists in one physics step,
                // reversing it every frame and producing the visible airborne wheel chatter. Cap only the
                // airborne damper to a passive one-step impulse; the spring, wheel spin and travel stay physical.
                float directionalScale = relativeVelocity < 0f
                    ? activeCompressionDamping : activeReboundDamping;
                float passiveDamping = SuspensionPhysics.CalculatePassiveAirborneDampingLimit(
                    body.mass, wheelBody.mass, Time.fixedDeltaTime);
                activeDamping = Mathf.Min(activeDamping,
                    passiveDamping / Mathf.Max(.5f, directionalScale));
            }
            lastSpringForce = SuspensionPhysics.CalculateSuspensionForce(rawCompression, 0f,
                spring, damping, maximumCompression, maximumExtension, bumpStopStart,
                progressiveSpringMultiplier, hardStopSpringMultiplier, maximumNormalForce);
            float unboundedCombinedForce = SuspensionPhysics.CalculateSuspensionForce(rawCompression,
                relativeVelocity, spring, activeDamping, maximumCompression, maximumExtension, bumpStopStart,
                progressiveSpringMultiplier, hardStopSpringMultiplier, maximumNormalForce,
                activeCompressionDamping, activeReboundDamping);
            lastDamperForce = unboundedCombinedForce - lastSpringForce;
            lastSuspensionForce = unboundedCombinedForce;
            lastSuspensionForce = SuspensionPhysics.LimitSuspensionForce(lastSuspensionForce, relativeVelocity,
                supportedMass, Time.fixedDeltaTime, maximumReboundAcceleration);
            // A grounded tire can unload to zero normal force, but a negative suspension force would pull
            // the unsprung body away from the road and manufacture contact loss. Airborne droop still keeps
            // its signed force so the wheel remains physically attached to the chassis.
            if (IsGrounded) lastSuspensionForce = Mathf.Max(0f, lastSuspensionForce);

            Vector3 suspensionForce = up * lastSuspensionForce;
            body.AddForceAtPosition(suspensionForce, anchor, ForceMode.Force);
            wheelBody.AddForce(-suspensionForce, ForceMode.Force);

            GroundHit = hit;
            if (probedGround)
            {
                contactNormal = hit.normal;
                contactPoint = hit.point;
            }
            contactSinceLastStep = false;
            if (IsGrounded)
                tractionBlend = Mathf.MoveTowards(tractionBlend, 1f,
                    tractionRecoveryPerSecond * Time.fixedDeltaTime);
            else
            {
                tractionBlend = 0f;
                lastContactNormalForce = 0f;
            }
            wasGrounded = IsGrounded;

            Compression01 = SuspensionPhysics.CalculateSag01(restLength - currentLength,
                maximumCompression, maximumExtension);
            UpdateSlip();
            ValidateConfigurationDrift();
        }

        private bool ProbeGround(out RaycastHit nearest)
        {
            const float originLift = .02f;
            Vector3 origin = wheelBody.position + body.transform.up * originLift;
            float distance = wheelRadius + originLift + Mathf.Clamp(wheelContactTolerance, .005f, .04f);
            return SuspensionPhysics.TryRaycastGround(origin, -body.transform.up, distance, groundMask,
                body.transform, groundHits, out nearest);
        }

        private void UpdateSlip()
        {
            if (!IsGrounded)
            {
                Slip01 = 0f;
                return;
            }
            Vector3 tangent = Vector3.Cross(Vector3.forward, contactNormal).normalized;
            if (Vector3.Dot(tangent, body.transform.right) < 0f) tangent = -tangent;
            float groundSpeed = Vector3.Dot(wheelBody.linearVelocity, tangent);
            float rollingSpeed = WheelAngularVelocity * wheelRadius;
            float denominator = Mathf.Max(1f, Mathf.Abs(groundSpeed));
            float slipRatio = Mathf.Abs(rollingSpeed - groundSpeed) / denominator;
            Slip01 = Mathf.InverseLerp(slipThreshold, Mathf.Max(slipThreshold + .01f, slipThreshold * 3f),
                slipRatio);
        }

        public float ApplyMotorTorque(float requestedTorque, float grip)
        {
            lastAppliedMotorTorque = 0f;
            if (wheelBody == null || Mathf.Abs(requestedTorque) < .001f) return Slip01;

            float appliedTorque = requestedTorque;
            if (IsGrounded)
            {
                float normalLoad = Mathf.Max(wheelBody.mass * Physics.gravity.magnitude,
                    LastNormalForce);
                float slipAuthority = Mathf.Lerp(1f, slidingGripMultiplier, Slip01);
                float tractionLimit = normalLoad * Mathf.Max(.1f, grip) * wheelRadius *
                                      Mathf.Lerp(.20f, 1f, tractionBlend) * slipAuthority;
                appliedTorque = SuspensionPhysics.SmoothLimit(requestedTorque, tractionLimit);
            }
            wheelBody.AddTorque(Vector3.back * appliedTorque, ForceMode.Force);
            lastAppliedMotorTorque = appliedTorque;
            return Slip01;
        }

        public void ApplyBrakeTorque(float requestedBrakeTorque)
        {
            if (wheelBody == null || requestedBrakeTorque <= 0f) return;
            float spin = wheelBody.angularVelocity.z;
            if (Mathf.Abs(spin) < .01f) return;
            float required = Mathf.Abs(spin) * Mathf.Max(.001f, wheelBody.inertiaTensor.z) /
                             Mathf.Max(.001f, Time.fixedDeltaTime);
            wheelBody.AddTorque(Vector3.forward * -Mathf.Sign(spin) * Mathf.Min(requestedBrakeTorque, required),
                ForceMode.Force);
        }

        public void AssignWheelVisual(Transform visual) => wheelVisual = visual;

        public void AssignPhysicalReferences(Rigidbody targetWheelBody, SphereCollider targetWheelCollider,
            ConfigurableJoint chassisJoint)
        {
            wheelBody = targetWheelBody;
            wheelCollider = targetWheelCollider;
            suspensionJoint = chassisJoint;
        }

        private void ValidateConfigurationDrift()
        {
            if (!logSuspensionAnomalies || suspensionJoint == null || body == null) return;
            float axisDelta = Vector3.Angle(SuspensionAxisWorld, body.transform.up);
            float jointAnchorDelta = Vector3.Distance(suspensionJoint.anchor, expectedJointAnchor);
            float connectedAnchorDelta = Vector3.Distance(suspensionJoint.connectedAnchor, expectedConnectedAnchor);
            if (axisDelta <= axisDriftToleranceDegrees && jointAnchorDelta <= anchorDriftTolerance &&
                connectedAnchorDelta <= anchorDriftTolerance) return;
            if (Time.unscaledTime < nextDiagnosticLogTime) return;
            nextDiagnosticLogTime = Time.unscaledTime + 1f;
            Debug.LogWarning($"{name} suspension configuration drift | axisDelta={axisDelta:0.000}deg, " +
                             $"anchorDelta={jointAnchorDelta:0.000000}m, " +
                             $"connectedAnchorDelta={connectedAnchorDelta:0.000000}m, " +
                             $"translation={JointTranslation:0.0000}m, wheelV={wheelBody.linearVelocity}, " +
                             $"wheelW={wheelBody.angularVelocity.z:0.000}, chassisV={body.linearVelocity}, " +
                             $"chassisW={body.angularVelocity.z:0.000}, grounded={IsGrounded}, " +
                             $"normal={contactNormal}, motor={lastAppliedMotorTorque:0.0}Nm, " +
                             $"spring={lastSpringForce:0.0}N, damper={lastDamperForce:0.0}N.", this);
        }

        private void LateUpdate() => SyncVisual();

        private void SyncVisual()
        {
            if (wheelVisual == null || wheelBody == null) return;
            // Rigidbody.position/rotation expose the simulation pose, while Transform exposes the
            // interpolated render pose. Mixing simulation-pose wheels with the interpolated chassis
            // creates a one-physics-step visual offset that grows with speed.
            Vector3 position = wheelBody.transform.position;

            // Replicate Hill Climb Racing distance-based soft sinking when grounded
            if (IsGrounded)
            {
                HcrGlitchState glitch = HcrGlitchSystem.Evaluate(position.x);
                if (glitch.isActive && glitch.sinkDepth > 0f)
                {
                    Vector3 sinkDirection = contactNormal.sqrMagnitude > 0.001f ? contactNormal : Vector3.up;
                    position -= sinkDirection * glitch.sinkDepth;
                }
            }

            position.z = wheelVisual.position.z;
            wheelVisual.position = position;
            wheelVisual.rotation = Quaternion.Euler(0f, 0f, wheelBody.transform.rotation.eulerAngles.z);
        }

        private void OnCollisionStay(Collision collision) => CaptureContact(collision);
        private void OnCollisionEnter(Collision collision) => CaptureContact(collision);

        private void CaptureContact(Collision collision)
        {
            if (body == null || collision == null || collision.collider == null ||
                !SuspensionPhysics.LayerInMask(collision.collider.gameObject.layer, groundMask) ||
                collision.collider.transform.IsChildOf(body.transform)) return;

            float bestUp = -.25f;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                float up = Vector3.Dot(contact.normal, body.transform.up);
                if (up <= bestUp) continue;
                bestUp = up;
                contactNormal = contact.normal;
                contactPoint = contact.point;
            }
            if (bestUp <= -.2f) return;
            contactSinceLastStep = true;
            lastContactNormalForce = Mathf.Min(maximumNormalForce,
                collision.impulse.magnitude / Mathf.Max(.001f, Time.fixedDeltaTime));
        }

        private void OnDestroy()
        {
            if (runtimeTireMaterial == null) return;
            if (Application.isPlaying) Destroy(runtimeTireMaterial);
            else DestroyImmediate(runtimeTireMaterial);
        }
    }

    public static class SuspensionPhysics
    {
        public static float CalculateAxleSupportedMass(float totalMass, float centerOfMassX,
            float halfWheelbase, float anchorLocalX, int suspensionPointCount)
        {
            int count = Mathf.Max(1, suspensionPointCount);
            float equalMass = Mathf.Max(.01f, totalMass) / count;
            if (count != 2 || halfWheelbase <= .01f) return equalMass;
            float frontFraction = Mathf.Clamp((centerOfMassX + halfWheelbase) /
                                               (2f * halfWheelbase), .35f, .65f);
            return Mathf.Max(.01f, totalMass) * (anchorLocalX >= 0f
                ? frontFraction : 1f - frontFraction);
        }

        public static bool TryRaycastGround(Vector3 origin, Vector3 direction, float distance, LayerMask mask,
            Transform vehicleRoot, RaycastHit[] hitBuffer, out RaycastHit nearestHit)
        {
            nearestHit = default;
            int count = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, mask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider collider = hitBuffer[i].collider;
                if (collider == null || collider.isTrigger ||
                    (vehicleRoot != null && collider.transform.IsChildOf(vehicleRoot))) continue;
                if (hitBuffer[i].distance >= nearestDistance) continue;
                nearestDistance = hitBuffer[i].distance;
                nearestHit = hitBuffer[i];
                found = true;
            }
            return found;
        }

        public static bool LayerInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        public static void GetTravelLimits(VehicleDefinition.SuspensionSettings settings, float totalTravel,
            out float maximumCompression, out float maximumExtension)
        {
            float safeTravel = Mathf.Max(.05f, totalTravel);
            float compressionRatio = Mathf.Clamp(settings.compressionTravelRatio, .6f, .95f);
            maximumCompression = safeTravel * compressionRatio;
            maximumExtension = safeTravel - maximumCompression;
        }

        public static float ClampLength(float restLength, float maximumCompression, float maximumExtension,
            float measuredLength) => Mathf.Clamp(measuredLength,
            Mathf.Max(.02f, restLength - maximumCompression), restLength + maximumExtension);

        public static float CalculateSag01(float compressionFromRest, float maximumCompression,
            float maximumExtension)
        {
            float totalTravel = Mathf.Max(.001f, maximumCompression + maximumExtension);
            return Mathf.Clamp01((compressionFromRest + maximumExtension) / totalTravel);
        }

        public static float CalculatePassiveAirborneDampingLimit(float chassisMass, float wheelMass,
            float fixedDeltaTime)
        {
            float safeChassisMass = Mathf.Max(.01f, chassisMass);
            float safeWheelMass = Mathf.Max(.01f, wheelMass);
            float reducedMass = safeChassisMass * safeWheelMass /
                                (safeChassisMass + safeWheelMass);
            return reducedMass / Mathf.Max(.001f, fixedDeltaTime);
        }

        public static float CalculateNormalForce(float rawCompression, float pointVelocity, float spring,
            float damping, float maximumCompression, float maximumExtension, float bumpStopStart,
            float progressiveSpringMultiplier, float hardStopSpringMultiplier, float maximumNormalForce) =>
            Mathf.Max(0f, CalculateSuspensionForce(rawCompression, pointVelocity, spring, damping,
                maximumCompression, maximumExtension, bumpStopStart, progressiveSpringMultiplier,
                hardStopSpringMultiplier, maximumNormalForce));

        public static float CalculateSuspensionForce(float rawCompression, float relativeVelocity, float spring,
            float damping, float maximumCompression, float maximumExtension, float bumpStopStart,
            float progressiveSpringMultiplier, float hardStopSpringMultiplier, float maximumForce,
            float compressionDampingMultiplier = 1f, float reboundDampingMultiplier = 1f)
        {
            float displacement = Mathf.Clamp(rawCompression, -maximumExtension, maximumCompression);
            float directionalDamping = damping * (relativeVelocity < 0f
                ? Mathf.Clamp(compressionDampingMultiplier, .5f, 1.8f)
                : Mathf.Clamp(reboundDampingMultiplier, .5f, 1.8f));
            float elasticForce = displacement * spring;

            float compression01 = Mathf.Clamp01(displacement / Mathf.Max(.001f, maximumCompression));
            float start = Mathf.Clamp(bumpStopStart, .5f, .9f);
            if (compression01 > start)
            {
                float progress = Mathf.InverseLerp(start, 1f, compression01);
                float progressiveDisplacement = Mathf.Max(0f, displacement - maximumCompression * start);
                elasticForce += progressiveDisplacement * spring * Mathf.Max(0f, progressiveSpringMultiplier) *
                                progress * progress;
            }

            // A bump stop is a progressive spring inside the final part of the valid travel. Once the
            // limit is reached the joint supplies only the geometric constraint; no second over-travel
            // spring is allowed to store and return energy after the solver has corrected the wheel.
            float hardStopStart = Mathf.Max(start, .9f);
            if (compression01 > hardStopStart)
            {
                float hardStopProgress = Mathf.InverseLerp(hardStopStart, 1f, compression01);
                float hardStopTravel = maximumCompression * (1f - hardStopStart);
                elasticForce += hardStopTravel * spring * Mathf.Max(0f, hardStopSpringMultiplier) *
                                hardStopProgress * hardStopProgress * hardStopProgress;
            }
            // Cap elastic bump-stop authority before applying damping. Clamping only the combined force
            // allowed a saturated spring to consume all rebound damping and return its stored energy as a
            // second jump. The damper must remain effective while the capped spring extends.
            elasticForce = Mathf.Clamp(elasticForce, -maximumForce * .35f, maximumForce);
            float dampingForce = -relativeVelocity * directionalDamping;
            return Mathf.Clamp(elasticForce + dampingForce, -maximumForce * .35f, maximumForce);
        }

        public static float LimitSuspensionForce(float force, float relativeVelocity, float supportedMass,
            float fixedDeltaTime, float maximumReboundAcceleration)
        {
            float safeMass = Mathf.Max(.01f, supportedMass);
            float gravity = Physics.gravity.magnitude;
            float upwardCap = safeMass * gravity * (1f +
                Mathf.Clamp(maximumReboundAcceleration, .5f, 6f));
            if (relativeVelocity < 0f)
            {
                float impactCap = safeMass * (gravity + -relativeVelocity /
                    Mathf.Max(.001f, fixedDeltaTime));
                upwardCap = Mathf.Max(upwardCap, impactCap);
            }
            return Mathf.Clamp(force, -safeMass * gravity * 2f, upwardCap);
        }

        public static float LimitRecoveryForce(float normalForce, float pointVelocity, float supportedMass,
            float fixedDeltaTime, float maximumReboundAcceleration) =>
            Mathf.Max(0f, LimitSuspensionForce(normalForce, pointVelocity, supportedMass,
                fixedDeltaTime, maximumReboundAcceleration));

        public static float SmoothLimit(float value, float absoluteLimit)
        {
            if (absoluteLimit <= .001f) return 0f;
            float magnitude = Mathf.Abs(value);
            float knee = absoluteLimit * .72f;
            if (magnitude <= knee) return value;
            float blend = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(knee, absoluteLimit, magnitude));
            return Mathf.Sign(value) * Mathf.Lerp(magnitude, absoluteLimit, blend);
        }
    }
}
