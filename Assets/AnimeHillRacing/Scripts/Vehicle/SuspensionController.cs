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

        private Rigidbody body;
        private PhysicsMaterial runtimeTireMaterial;
        private float anchorLocalX;
        private float restLength;
        private float maximumCompression;
        private float maximumExtension;
        private float wheelRadius;
        private float spring;
        private float damping;
        private float bumpStopStart;
        private float progressiveSpringMultiplier;
        private float hardStopSpringMultiplier;
        private float maximumNormalForce;
        private float supportedMass;
        private float maximumReboundAcceleration;
        private float lastSuspensionForce;
        private float lastContactNormalForce;
        private float currentLength;
        private float tractionBlend;
        private float slipThreshold;
        private float slidingGripMultiplier;
        private float tractionRecoveryPerSecond;
        private bool contactSinceLastStep;
        private Vector3 contactNormal = Vector3.up;
        private Vector3 contactPoint;
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
        public Rigidbody WheelBody => wheelBody;
        public SphereCollider WheelCollider => wheelCollider;
        public ConfigurableJoint SuspensionJoint => suspensionJoint;

        public void Initialize(Rigidbody targetBody, VehicleDefinition definition, ResolvedVehicleStats stats,
            int suspensionPointCount = 2)
        {
            body = targetBody;
            anchorLocalX = body.transform.InverseTransformPoint(transform.position).x;
            restLength = definition.suspension.restLength;
            SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                out maximumCompression, out maximumExtension);
            wheelRadius = definition.wheelRadius;
            spring = stats.spring;
            damping = stats.damping;
            bumpStopStart = definition.suspension.bumpStopStart;
            progressiveSpringMultiplier = stats.progressiveSpringMultiplier;
            hardStopSpringMultiplier = definition.suspension.hardStopSpringMultiplier;
            maximumReboundAcceleration = definition.suspension.maximumReboundAcceleration;
            supportedMass = targetBody.mass / Mathf.Max(1, suspensionPointCount);
            maximumNormalForce = targetBody.mass * definition.suspension.maximumAcceleration /
                                 Mathf.Max(1, suspensionPointCount);
            slipThreshold = stats.slipThreshold;
            slidingGripMultiplier = stats.slidingGripMultiplier;
            tractionRecoveryPerSecond = stats.tractionRecoveryPerSecond;

            EnsurePhysicalWheel(definition, stats.grip);
            ConfigureHardTravelLimit();
            currentLength = restLength;
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
            if (suspensionJoint == null) suspensionJoint = GetComponent<ConfigurableJoint>();
            if (suspensionJoint == null) suspensionJoint = gameObject.AddComponent<ConfigurableJoint>();

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
            runtimeTireMaterial.frictionCombine = PhysicsMaterialCombine.Maximum;
            runtimeTireMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            runtimeTireMaterial.hideFlags = HideFlags.DontSave;
            wheelCollider.sharedMaterial = runtimeTireMaterial;

            float statsTopWheelSpeed(VehicleDefinition source) =>
                source.topSpeedMps / Mathf.Max(.05f, source.wheelRadius);
        }

        private void ConfigureHardTravelLimit()
        {
            float travel = maximumCompression + maximumExtension;
            float centerLength = restLength + (maximumExtension - maximumCompression) * .5f;
            suspensionJoint.connectedBody = body;
            suspensionJoint.autoConfigureConnectedAnchor = false;
            suspensionJoint.anchor = Vector3.zero;
            suspensionJoint.connectedAnchor = new Vector3(anchorLocalX, -centerLength, 0f);
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
            limitSpring.damper = damping;
            suspensionJoint.linearLimitSpring = limitSpring;
            suspensionJoint.enableCollision = false;
            suspensionJoint.enablePreprocessing = true;
            suspensionJoint.projectionMode = JointProjectionMode.PositionAndRotation;
            suspensionJoint.projectionDistance = .015f;
            suspensionJoint.projectionAngle = 2f;
            suspensionJoint.breakForce = Mathf.Infinity;
            suspensionJoint.breakTorque = Mathf.Infinity;
        }

        public void Simulate()
        {
            if (body == null || wheelBody == null) return;

            Vector3 up = body.transform.up;
            Vector3 anchor = body.transform.TransformPoint(new Vector3(anchorLocalX, 0f, 0f));
            float measuredLength = Vector3.Dot(anchor - wheelBody.position, up);
            currentLength = SuspensionPhysics.ClampLength(restLength, maximumCompression,
                maximumExtension, measuredLength);
            float rawCompression = restLength - measuredLength;
            float relativeVelocity = Vector3.Dot(body.GetPointVelocity(anchor) - wheelBody.linearVelocity, up);
            lastSuspensionForce = SuspensionPhysics.CalculateSuspensionForce(rawCompression, relativeVelocity,
                spring, damping, maximumCompression, maximumExtension, bumpStopStart,
                progressiveSpringMultiplier, hardStopSpringMultiplier, maximumNormalForce);
            lastSuspensionForce = SuspensionPhysics.LimitSuspensionForce(lastSuspensionForce, relativeVelocity,
                supportedMass, Time.fixedDeltaTime, maximumReboundAcceleration);

            Vector3 suspensionForce = up * lastSuspensionForce;
            body.AddForceAtPosition(suspensionForce, anchor, ForceMode.Force);
            wheelBody.AddForce(-suspensionForce, ForceMode.Force);

            bool probedGround = ProbeGround(out RaycastHit hit);
            IsGrounded = contactSinceLastStep || probedGround;
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

            Compression01 = SuspensionPhysics.CalculateSag01(restLength - currentLength,
                maximumCompression, maximumExtension);
            UpdateSlip();
        }

        private bool ProbeGround(out RaycastHit nearest)
        {
            Vector3 origin = wheelBody.position + body.transform.up * .035f;
            float distance = wheelRadius + .075f;
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

        private void LateUpdate() => SyncVisual();

        private void SyncVisual()
        {
            if (wheelVisual == null || wheelBody == null) return;
            Vector3 position = wheelBody.position;
            position.z = wheelVisual.position.z;
            wheelVisual.position = position;
            wheelVisual.rotation = Quaternion.Euler(0f, 0f, wheelBody.rotation.eulerAngles.z);
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
            lastContactNormalForce = collision.impulse.magnitude / Mathf.Max(.001f, Time.fixedDeltaTime);
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

        public static float CalculateNormalForce(float rawCompression, float pointVelocity, float spring,
            float damping, float maximumCompression, float maximumExtension, float bumpStopStart,
            float progressiveSpringMultiplier, float hardStopSpringMultiplier, float maximumNormalForce) =>
            Mathf.Max(0f, CalculateSuspensionForce(rawCompression, pointVelocity, spring, damping,
                maximumCompression, maximumExtension, bumpStopStart, progressiveSpringMultiplier,
                hardStopSpringMultiplier, maximumNormalForce));

        public static float CalculateSuspensionForce(float rawCompression, float relativeVelocity, float spring,
            float damping, float maximumCompression, float maximumExtension, float bumpStopStart,
            float progressiveSpringMultiplier, float hardStopSpringMultiplier, float maximumForce)
        {
            float displacement = Mathf.Clamp(rawCompression, -maximumExtension, maximumCompression);
            float force = displacement * spring - relativeVelocity * damping;

            float compression01 = Mathf.Clamp01(displacement / Mathf.Max(.001f, maximumCompression));
            float start = Mathf.Clamp(bumpStopStart, .5f, .9f);
            if (compression01 > start)
            {
                float progress = Mathf.InverseLerp(start, 1f, compression01);
                float progressiveDisplacement = Mathf.Max(0f, displacement - maximumCompression * start);
                force += progressiveDisplacement * spring * Mathf.Max(0f, progressiveSpringMultiplier) *
                         progress * progress;
            }

            float overTravel = Mathf.Max(0f, rawCompression - maximumCompression);
            force += overTravel * spring * Mathf.Max(0f, hardStopSpringMultiplier);
            return Mathf.Clamp(force, -maximumForce * .35f, maximumForce);
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
