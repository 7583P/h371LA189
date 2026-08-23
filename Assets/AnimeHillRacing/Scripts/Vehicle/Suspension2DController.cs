using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public enum DriveMethod2D
    {
        RigidbodyTorque,
        WheelJointMotor
    }

    /// <summary>
    /// Manages an independent 2D wheel (Rigidbody2D + CircleCollider2D) connected to the vehicle
    /// chassis via WheelJoint2D. Handles suspension parameters, progressive bump stops, real Box2D ground contact,
    /// physical crossing observation, and visual sprite synchronization.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Suspension2DController : MonoBehaviour
    {
        [SerializeField] private Transform wheelVisual;
        [SerializeField] private Rigidbody2D wheelBody;
        [SerializeField] private CircleCollider2D wheelCollider;
        [SerializeField] private WheelJoint2D suspensionJoint;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Range(0.1f, 1.0f)] private float bumpStopStartRatio = 0.72f;
        [SerializeField, Min(0f)] private float bumpStopSpringMultiplier = 3.5f;

        private Rigidbody2D chassisBody;
        private PhysicsMaterial2D runtimeWheelMaterial;
        private float wheelRadius = 0.45f;
        private float restLength = 0.52f;
        private float totalTravel = 0.35f;
        private float nominalFrequency = 4.5f;
        private float nominalDampingRatio = 0.75f;
        private float highSpeedDampingRatio;
        private bool isGrounded;
        private float lastAppliedMotorTorque;
        private DriveMethod2D driveMethod = DriveMethod2D.RigidbodyTorque;
        private ContactFilter2D groundFilter;
        private readonly Collider2D[] groundHitBuffer = new Collider2D[4];
        private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];

        private WheelTerrainState terrainState = WheelTerrainState.Normal;
        private bool hasBox2DContact;
        private int actualContactCount;
        private Vector2 lastContactPoint;
        private Vector2 lastContactNormal = Vector2.up;
        private float normalVelocity;
        private float normalTravelPerStep;
        private float currentSignedDistance;
        private float previousSignedDistance;
        private int consecutiveContactMisses;
        private int consecutiveContactHits;
        private int contactAlternationCount;
        private bool collisionGroundContactSinceLastStep;
        private Vector2 collisionGroundPoint;
        private Vector2 collisionGroundNormal = Vector2.up;

        public Rigidbody2D WheelBody => wheelBody;
        public CircleCollider2D WheelCollider => wheelCollider;
        public WheelJoint2D SuspensionJoint => suspensionJoint;
        public VehicleRuntimeBase OwnerVehicle => chassisBody != null
            ? chassisBody.GetComponent<VehicleRuntimeBase>()
            : null;
        public bool IsGrounded => isGrounded;
        public float Slip01 { get; private set; }
        public float WheelRadius => wheelRadius;
        public float LastAppliedMotorTorque => lastAppliedMotorTorque;
        public float WheelAngularVelocity => wheelBody != null ? wheelBody.angularVelocity * Mathf.Deg2Rad : 0f;
        public float VisualCenterError => wheelBody != null && wheelVisual != null
            ? Vector2.Distance(wheelBody.position, wheelVisual.position) : 0f;
        public float VisualRotationError => wheelBody != null && wheelVisual != null
            ? Mathf.Abs(Mathf.DeltaAngle(wheelBody.rotation, wheelVisual.eulerAngles.z)) : 0f;

        public WheelTerrainState TerrainState => terrainState;
        public bool IsBelowSurface => terrainState == WheelTerrainState.BelowSurface;
        public bool IsGlitchTrapped => terrainState == WheelTerrainState.BelowSurface;
        public bool IsContactUnstable => terrainState == WheelTerrainState.ContactUnstable;
        public bool HasBox2DContact => hasBox2DContact;
        public int ActualContactCount => actualContactCount;
        public float NormalVelocity => normalVelocity;
        public float NormalTravelPerStep => normalTravelPerStep;
        public float CurrentSignedDistance => currentSignedDistance;
        public float PreviousSignedDistance => previousSignedDistance;
        public Vector2 LastContactPoint => lastContactPoint;
        public Vector2 LastContactNormal => lastContactNormal;
        public float FloatStep => 0f;

        public float SuspensionCompression01
        {
            get
            {
                if (suspensionJoint == null || totalTravel <= 0.001f) return 0f;
                return Mathf.Clamp01(Mathf.Max(0f, suspensionJoint.jointTranslation) / totalTravel);
            }
        }

        private void Awake()
        {
            if (wheelBody == null) wheelBody = GetComponent<Rigidbody2D>();
            if (wheelCollider == null) wheelCollider = GetComponent<CircleCollider2D>();
            if (suspensionJoint == null) suspensionJoint = GetComponent<WheelJoint2D>();
        }

        public void Initialize(Rigidbody2D parentChassis, VehicleDefinition definition, ResolvedVehicleStats stats,
            float anchorLocalX, float axleY, float radius, float travel, float frequency, float dampingRatio,
            float gripRating, DriveMethod2D method)
        {
            chassisBody = parentChassis;
            wheelRadius = Mathf.Max(0.1f, radius);
            totalTravel = Mathf.Max(0.05f, travel);
            restLength = Mathf.Abs(axleY);
            nominalFrequency = Mathf.Max(1.0f, frequency);
            nominalDampingRatio = Mathf.Clamp(dampingRatio, 0.2f, 2.0f);
            highSpeedDampingRatio = nominalDampingRatio;
            driveMethod = method;

            EnsureWheelComponents(definition, gripRating);

            // Spawn the physical centre at the authored tire centre before the first solver step. This matters
            // for Hill Adventurer, whose axle Y differs from Jeep, and prevents an initialization impulse.
            if (chassisBody != null && wheelBody != null)
            {
                Vector3 authoredWorld = chassisBody.transform.TransformPoint(
                    new Vector3(anchorLocalX, axleY, 0f));
                wheelBody.position = new Vector2(authoredWorld.x, authoredWorld.y);
                wheelBody.rotation = chassisBody.rotation;
            }

            // A dynamic Rigidbody2D must not inherit the chassis Transform. Keeping a simulated wheel below
            // a rotating chassis makes its world centre inherit parent rotation between solver updates.
            Transform neutralParent = chassisBody != null ? chassisBody.transform.parent : null;
            if (transform.parent != neutralParent)
                transform.SetParent(neutralParent, true);

            groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundMask,
                useTriggers = false
            };

            ConfigureJoint(anchorLocalX, axleY);
        }

        private void EnsureWheelComponents(VehicleDefinition definition, float gripRating)
        {
            if (wheelBody == null) wheelBody = GetComponent<Rigidbody2D>();
            if (wheelBody == null) wheelBody = gameObject.AddComponent<Rigidbody2D>();

            if (wheelCollider == null) wheelCollider = GetComponent<CircleCollider2D>();
            if (wheelCollider == null) wheelCollider = gameObject.AddComponent<CircleCollider2D>();

            wheelBody.bodyType = RigidbodyType2D.Dynamic;
            wheelBody.mass = Mathf.Max(10f, definition != null ? definition.wheelMass : 35f);
            wheelBody.linearDamping = 0.05f;
            wheelBody.angularDamping = 0.15f;
            wheelBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            // The fastest vehicles can travel farther than the tire radius during one fixed step.
            // Continuous detection keeps the EdgeCollider terrain contact from being recreated as a
            // deep penetration, which otherwise arrives at the joint as a sharp correction.
            wheelBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            wheelCollider.radius = wheelRadius;
            wheelCollider.offset = Vector2.zero;
            wheelCollider.isTrigger = false;

            // PhysicsMaterial2D for arcade tire grip
            runtimeWheelMaterial = new PhysicsMaterial2D($"{name}_Tire2D")
            {
                friction = Mathf.Clamp(gripRating / 150f, 0.6f,
                    definition != null ? definition.grip.maximumPhysicsFriction : 2.4f),
                bounciness = 0f
            };
            wheelCollider.sharedMaterial = runtimeWheelMaterial;
        }

        private void ConfigureJoint(float anchorLocalX, float axleY)
        {
            // The suspension axis belongs to the chassis reference frame. A WheelJoint2D attached to the
            // spinning wheel rotates that axis every time the tire turns and makes the wheel centre orbit.
            if (suspensionJoint != null && suspensionJoint.attachedRigidbody != chassisBody)
            {
                suspensionJoint.enabled = false;
                if (Application.isPlaying) Destroy(suspensionJoint);
                else DestroyImmediate(suspensionJoint);
                suspensionJoint = null;
            }
            if (suspensionJoint == null && chassisBody != null)
            {
                WheelJoint2D[] chassisJoints = chassisBody.GetComponents<WheelJoint2D>();
                for (int i = 0; i < chassisJoints.Length; i++)
                {
                    if (chassisJoints[i] != null && chassisJoints[i].connectedBody == wheelBody)
                    {
                        suspensionJoint = chassisJoints[i];
                        break;
                    }
                }
            }
            if (suspensionJoint == null && chassisBody != null)
                suspensionJoint = chassisBody.gameObject.AddComponent<WheelJoint2D>();
            if (suspensionJoint == null) return;

            suspensionJoint.connectedBody = wheelBody;
            suspensionJoint.autoConfigureConnectedAnchor = false;
            suspensionJoint.anchor = new Vector2(anchorLocalX, axleY);
            suspensionJoint.connectedAnchor = Vector2.zero;

            // Suspension line points vertically up (90 degrees in 2D relative to chassis)
            suspensionJoint.suspension = new JointSuspension2D
            {
                angle = 90f,
                frequency = nominalFrequency,
                dampingRatio = nominalDampingRatio
            };

            suspensionJoint.useMotor = driveMethod == DriveMethod2D.WheelJointMotor;
            suspensionJoint.enableCollision = false;
        }

        public void ResetTerrainState()
        {
            terrainState = WheelTerrainState.Normal;
            currentSignedDistance = 0f;
            previousSignedDistance = 0f;
            consecutiveContactHits = 0;
            consecutiveContactMisses = 0;
            contactAlternationCount = 0;
            hasBox2DContact = false;
            collisionGroundContactSinceLastStep = false;
            // Restore full collider radius on reset (e.g. after teleport)
            if (wheelCollider != null) wheelCollider.radius = wheelRadius;
        }

        public void Simulate(float dt = 0.02f, MapProfile profile = null)
        {
            if (wheelBody == null || chassisBody == null) return;

            UpdateHighSpeedJointDamping();

            Transform root = chassisBody != null ? chassisBody.transform.root : transform.root;
            bool callbackGroundContact = collisionGroundContactSinceLastStep;
            Vector2 callbackGroundPoint = collisionGroundPoint;
            Vector2 callbackGroundNormal = collisionGroundNormal;
            collisionGroundContactSinceLastStep = false;

            // 1. Query real Box2D contact points directly from contact solver
            actualContactCount = wheelCollider != null ? Physics2D.GetContacts(wheelCollider, contactBuffer) : 0;
            hasBox2DContact = false;
            Vector2 sumPoint = Vector2.zero;
            Vector2 sumNormal = Vector2.zero;
            int validGroundContacts = 0;

            for (int i = 0; i < actualContactCount; i++)
            {
                ContactPoint2D cp = contactBuffer[i];
                // Collider2D.GetContacts can report this wheel as cp.collider and the terrain as
                // cp.otherCollider. Test both sides and only accept the non-vehicle ground collider.
                Collider2D groundCollider = GroundContactFilter.IsGround2D(cp.collider, root)
                    ? cp.collider
                    : GroundContactFilter.IsGround2D(cp.otherCollider, root) ? cp.otherCollider : null;
                if (groundCollider != null)
                {
                    hasBox2DContact = true;
                    sumPoint += cp.point;
                    sumNormal += cp.normal;
                    validGroundContacts++;
                }
            }

            // Rebuilding EdgeCollider2D.points in Update can invalidate the queryable manifold before the
            // following FixedUpdate. Collision callbacks are emitted by that real solver step, so retain one
            // step of callback evidence rather than downgrading a physical contact to overlap-only telemetry.
            if (callbackGroundContact)
            {
                hasBox2DContact = true;
                if (validGroundContacts == 0)
                {
                    sumPoint = callbackGroundPoint;
                    sumNormal = callbackGroundNormal;
                    validGroundContacts = 1;
                    actualContactCount = Mathf.Max(actualContactCount, 1);
                }
            }

            if (validGroundContacts > 0)
            {
                lastContactPoint = sumPoint / validGroundContacts;
                lastContactNormal = (sumNormal / validGroundContacts).normalized;
            }

            // 2. Secondary Overlap telemetry
            int overlapCount = wheelCollider != null ? wheelCollider.Overlap(groundFilter, groundHitBuffer) : 0;
            bool overlapGrounded = false;
            for (int i = 0; i < overlapCount; i++)
            {
                if (GroundContactFilter.IsGround2D(groundHitBuffer[i], root))
                {
                    overlapGrounded = true;
                    break;
                }
            }

            // 3. Update Contact History and Alternation Tracking (Observation of contact oscillation)
            if (hasBox2DContact)
            {
                if (consecutiveContactMisses > 0)
                {
                    contactAlternationCount++;
                }
                consecutiveContactHits++;
                consecutiveContactMisses = 0;
            }
            else
            {
                if (consecutiveContactHits > 0)
                {
                    contactAlternationCount++;
                }
                consecutiveContactMisses++;
                consecutiveContactHits = 0;
            }

            // 4. Calculate Surface Geometry & Signed Distance (positive = above ground, negative = below ground)
            Vector2 surfacePoint;
            Vector2 terrainNormal;
            float worldX = wheelBody.position.x;

            if (validGroundContacts > 0 && terrainState != WheelTerrainState.BelowSurface)
            {
                surfacePoint = lastContactPoint;
                terrainNormal = lastContactNormal;
            }
            else
            {
                TerrainManager terrain = TerrainManager.Instance;
                float surfaceY = 0f;
                float slopeDegrees = 0f;
                bool hasAuthoritativeSurface = terrain != null &&
                    terrain.TrySampleAuthoritativeSurface(worldX, out surfaceY, out slopeDegrees);
                if (!hasAuthoritativeSurface)
                {
                    surfaceY = profile != null ? TerrainGenerator.SampleHeight(profile, worldX)
                        : (lastContactPoint.y != 0f ? lastContactPoint.y : wheelBody.position.y);
                    slopeDegrees = 0f;
                }
                surfacePoint = new Vector2(worldX, surfaceY);
                if (hasAuthoritativeSurface)
                {
                    float slopeRadians = slopeDegrees * Mathf.Deg2Rad;
                    terrainNormal = new Vector2(-Mathf.Sin(slopeRadians), Mathf.Cos(slopeRadians));
                }
                else if (profile != null)
                {
                    float yAhead = TerrainGenerator.SampleHeight(profile, worldX + 0.5f);
                    float yBehind = TerrainGenerator.SampleHeight(profile, worldX - 0.5f);
                    Vector2 tangent = new Vector2(1.0f, yAhead - yBehind).normalized;
                    terrainNormal = new Vector2(-tangent.y, tangent.x);
                }
                else
                {
                    terrainNormal = lastContactNormal != Vector2.zero ? lastContactNormal : Vector2.up;
                }
            }

            previousSignedDistance = currentSignedDistance;
            currentSignedDistance = Vector2.Dot(wheelBody.position - surfacePoint, terrainNormal);

            // Grounded state uses this frame's sampled surface rather than the previous step's signed distance.
            // This avoids a visual/drive-state delay when a wheel first meets very steep Mountains terrain.
            isGrounded = (hasBox2DContact || overlapGrounded) &&
                         currentSignedDistance >= -wheelRadius * .25f &&
                         terrainState != WheelTerrainState.BelowSurface;

            // Compute relative normal velocity (positive = pushing into terrain)
            Vector2 relVel = wheelBody.linearVelocity;
            normalVelocity = -Vector2.Dot(relVel, terrainNormal);
            normalTravelPerStep = Mathf.Max(0f, normalVelocity) * dt;

            // 5. OBSERVE BOX2D PHYSICAL STATE (Pure Observation, no forced crossing!)

            if (terrainState == WheelTerrainState.BelowSurface)
            {
                // CRITICAL RULE: NO RECOVERY AFTER PHYSICAL CROSSING!
                // Once Box2D carried the wheel below the surface, it remains BelowSurface for this run.
            }
            else
            {
                // A. Check for Physical Crossing:
                // Box2D physically carried the wheel center across the terrain line to the wrong side
                bool physicallyCrossed = currentSignedDistance < -wheelRadius * 0.85f &&
                                         (previousSignedDistance >= -wheelRadius * 0.85f || previousSignedDistance == 0f) &&
                                         normalVelocity > 4.0f;

                // Also check if wheel center is deep below surface without valid upward contact
                if (currentSignedDistance < -wheelRadius * 1.10f && !hasBox2DContact)
                {
                    physicallyCrossed = true;
                }
                if (physicallyCrossed)
                {
                    terrainState = WheelTerrainState.BelowSurface;
                }
                else
                {
                    // B. Observe Contact Instability (contact -> miss -> contact):
                    if (contactAlternationCount >= 2 && Mathf.Abs(currentSignedDistance) < wheelRadius * 1.5f)
                    {
                        terrainState = WheelTerrainState.ContactUnstable;
                    }
                    else if (consecutiveContactHits >= 4)
                    {
                        // Contact restored stably -> wheel is saved naturally!
                        terrainState = WheelTerrainState.Normal;
                        contactAlternationCount = 0;
                    }
                }
            }

            if (terrainState == WheelTerrainState.BelowSurface)
            {
                EnforceOneWayTerrainTrap(surfacePoint, terrainNormal);
                currentSignedDistance = Vector2.Dot(wheelBody.position - surfacePoint, terrainNormal);
                isGrounded = false;
            }

            // 6. Progressive Bump Stop with Damping.
            // Pure spring bump stops resonate at high speed when aerodynamic downforce is strong.
            // Adding velocity-dependent damping absorbs energy and prevents oscillation.
            if (suspensionJoint != null)
            {
                float compression01 = SuspensionCompression01;
                if (compression01 > bumpStopStartRatio)
                {
                    float bumpProgress = Mathf.InverseLerp(bumpStopStartRatio, 1f, compression01);
                    Vector2 suspensionDir = chassisBody.transform.up;

                    // Spring component (quadratic progressive)
                    float bumpForce = chassisBody.mass * Physics2D.gravity.magnitude *
                                     bumpStopSpringMultiplier * bumpProgress * bumpProgress;

                    // Damping component: proportional to closing velocity between chassis and wheel.
                    Vector2 relativeVel = chassisBody.linearVelocity - wheelBody.linearVelocity;
                    float closingSpeed = Vector2.Dot(relativeVel, -suspensionDir); // positive = compressing
                    float compressionDampingForce = 0f;
                    if (closingSpeed > 0f)
                    {
                        float requestedDamping = chassisBody.mass * closingSpeed *
                                                 bumpStopSpringMultiplier * 1.5f;

                        // A force larger than the reduced-mass impulse can stop and reverse the relative
                        // motion in one Box2D step. That reversal repeats on the next step and is the source
                        // of the violent high-speed wheel chatter. Keep the damper passive: it may consume
                        // at most 85% of the closing impulse in a single simulation step.
                        float reducedMass = chassisBody.mass * wheelBody.mass /
                                            Mathf.Max(.01f, chassisBody.mass + wheelBody.mass);
                        float passiveForceLimit = reducedMass * closingSpeed /
                                                  Mathf.Max(.001f, dt) * .85f;
                        compressionDampingForce = Mathf.Min(requestedDamping, passiveForceLimit);
                    }

                    // The WheelJoint already supplies the normal spring return. The progressive bump stop is
                    // impact protection only; letting it continue pushing during extension duplicates the
                    // spring and launches the chassis into a second bounce.
                    float reboundSpeed = Vector2.Dot(relativeVel, suspensionDir);
                    float totalForce = reboundSpeed > 0f
                        ? 0f
                        : bumpForce + compressionDampingForce;
                    wheelBody.AddForce(-suspensionDir * totalForce, ForceMode2D.Force);
                    chassisBody.AddForceAtPosition(suspensionDir * totalForce, wheelBody.position, ForceMode2D.Force);
                }
            }

            // 6.5 Calculate Physical Slip
            if (!isGrounded || wheelBody == null)
            {
                Slip01 = 0f;
            }
            else
            {
                Vector2 tangent = new Vector2(terrainNormal.y, -terrainNormal.x);
                if (chassisBody != null && Vector2.Dot(tangent, chassisBody.transform.right) < 0f) tangent = -tangent;
                float groundSpeed = Vector2.Dot(wheelBody.linearVelocity, tangent);
                float rollingSpeed = -wheelBody.angularVelocity * Mathf.Deg2Rad * wheelRadius;
                float denominator = Mathf.Max(1f, Mathf.Abs(groundSpeed));
                float slipRatio = Mathf.Abs(rollingSpeed - groundSpeed) / denominator;
                const float slipThresh = 0.18f;
                Slip01 = Mathf.InverseLerp(slipThresh, slipThresh * 3f, slipRatio);
            }

            // 7. Visual synchronization
            SyncVisual();
        }

        private void UpdateHighSpeedJointDamping()
        {
            if (suspensionJoint == null || chassisBody == null) return;

            float forwardSpeed = Mathf.Abs(Vector2.Dot(chassisBody.linearVelocity,
                (Vector2)chassisBody.transform.right));
            float speedBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(22f, 55f, forwardSpeed));
            float targetDamping = Mathf.Lerp(nominalDampingRatio,
                Mathf.Max(nominalDampingRatio, 1.05f), speedBlend);
            if (Mathf.Abs(targetDamping - highSpeedDampingRatio) < .005f) return;

            highSpeedDampingRatio = targetDamping;
            JointSuspension2D settings = suspensionJoint.suspension;
            settings.frequency = nominalFrequency;
            settings.dampingRatio = highSpeedDampingRatio;
            suspensionJoint.suspension = settings;
        }

        private void OnCollisionEnter2D(Collision2D collision) => RecordSolverGroundContact(collision);
        private void OnCollisionStay2D(Collision2D collision) => RecordSolverGroundContact(collision);

        private void RecordSolverGroundContact(Collision2D collision)
        {
            if (collision == null) return;
            Transform root = chassisBody != null ? chassisBody.transform.root : transform.root;
            bool ground = GroundContactFilter.IsGround2D(collision.collider, root) ||
                          GroundContactFilter.IsGround2D(collision.otherCollider, root);
            if (!ground) return;

            collisionGroundContactSinceLastStep = true;
            int count = collision.GetContacts(contactBuffer);
            if (count <= 0) return;
            Vector2 point = Vector2.zero;
            Vector2 normal = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                point += contactBuffer[i].point;
                normal += contactBuffer[i].normal;
            }
            collisionGroundPoint = point / count;
            collisionGroundNormal = normal.sqrMagnitude > .000001f ? normal.normalized : Vector2.up;
        }

        public void ApplyMotorTorque(float requestedTorque, float maxTorqueCapacity)
        {
            lastAppliedMotorTorque = requestedTorque;
            if (wheelBody == null) return;


            if (Mathf.Abs(requestedTorque) < 0.001f)
            {
                if (suspensionJoint != null && suspensionJoint.useMotor)
                    suspensionJoint.useMotor = false;
                return;
            }

            if (driveMethod == DriveMethod2D.RigidbodyTorque)
            {
                // In 2D: positive Z torque rotates counter-clockwise (backward), negative Z rotates clockwise (forward)
                // When wheel is airborne for an extended period, prevent runaway over-spin to ensure silky smooth touchdown
                float appliedTorque = requestedTorque;
                if (!isGrounded && chassisBody != null)
                {
                    float forwardSpeed = Vector2.Dot(chassisBody.linearVelocity, chassisBody.transform.right);
                    float expectedSpinDeg = -(forwardSpeed / Mathf.Max(0.1f, wheelRadius)) * Mathf.Rad2Deg;
                    float currentSpinDeg = wheelBody.angularVelocity;
                    if (Mathf.Abs(currentSpinDeg) > Mathf.Abs(expectedSpinDeg) * 1.5f + 250f)
                    {
                        appliedTorque *= 0.15f;
                    }
                }
                wheelBody.AddTorque(-appliedTorque, ForceMode2D.Force);
            }
            else if (driveMethod == DriveMethod2D.WheelJointMotor && suspensionJoint != null)
            {
                float targetSpeed = -Mathf.Sign(requestedTorque) * 2800f;
                suspensionJoint.motor = new JointMotor2D
                {
                    motorSpeed = targetSpeed,
                    maxMotorTorque = Mathf.Abs(requestedTorque)
                };
                suspensionJoint.useMotor = true;
            }
        }

        private void EnforceOneWayTerrainTrap(Vector2 surfacePoint, Vector2 terrainNormal)
        {
            if (wheelBody == null) return;
            Vector2 normal = terrainNormal.sqrMagnitude > .0001f
                ? terrainNormal.normalized : Vector2.up;
            if (normal.y < 0f) normal = -normal;

            // The centre is never allowed to move back across the depth at which the tire was declared
            // buried. Tangential forces remain untouched, so drivetrain and thrusters keep functioning,
            // but no combination of them can extract the trapped tire through the terrain surface.
            float maximumOutwardDistance = -wheelRadius * .85f;
            float signedDistance = Vector2.Dot(wheelBody.position - surfacePoint, normal);
            if (signedDistance > maximumOutwardDistance)
                wheelBody.position += normal * (maximumOutwardDistance - signedDistance);

            float outwardVelocity = Vector2.Dot(wheelBody.linearVelocity, normal);
            if (outwardVelocity > 0f)
                wheelBody.linearVelocity -= normal * outwardVelocity;
        }

        public void ApplyBrakeTorque(float brakeTorque)
        {
            if (suspensionJoint != null && suspensionJoint.useMotor)
                suspensionJoint.useMotor = false;

            if (wheelBody == null || brakeTorque <= 0f) return;
            float spin = wheelBody.angularVelocity;
            if (Mathf.Abs(spin) < 1f) return;

            float counterTorque = -Mathf.Sign(spin) * Mathf.Min(brakeTorque, Mathf.Abs(spin) * wheelBody.inertia * 50f);
            wheelBody.AddTorque(counterTorque, ForceMode2D.Force);
        }

        public void AssignWheelVisual(Transform visual)
        {
            wheelVisual = visual;
        }

        private void SyncVisual()
        {
            if (wheelVisual == null || wheelBody == null) return;
            Vector2 pos = wheelBody.position;
            wheelVisual.position = new Vector3(pos.x, pos.y, -2.0f);
            wheelVisual.rotation = Quaternion.Euler(0f, 0f, wheelBody.rotation);
        }

        private void LateUpdate()
        {
            if (wheelVisual == null || wheelBody == null) return;

            // Rigidbody2D.position is the latest fixed-step pose. The Transform contains Unity's
            // interpolated render pose, so following it here avoids a visible 50 Hz wheel judder.
            Vector3 renderPosition = wheelBody.transform.position;
            wheelVisual.position = new Vector3(renderPosition.x, renderPosition.y, -2.0f);
            wheelVisual.rotation = wheelBody.transform.rotation;
        }

        private void OnDestroy()
        {
            if (runtimeWheelMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeWheelMaterial);
                else DestroyImmediate(runtimeWheelMaterial);
            }
        }
    }
}
