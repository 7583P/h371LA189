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
    /// chassis via WheelJoint2D. Handles suspension parameters, progressive bump stops, ground contact,
    /// and visual sprite synchronization.
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
        private bool isGrounded;
        private float lastAppliedMotorTorque;
        private DriveMethod2D driveMethod = DriveMethod2D.RigidbodyTorque;
        private ContactFilter2D groundFilter;
        private readonly Collider2D[] groundHitBuffer = new Collider2D[4];

        public Rigidbody2D WheelBody => wheelBody;
        public CircleCollider2D WheelCollider => wheelCollider;
        public WheelJoint2D SuspensionJoint => suspensionJoint;
        public bool IsGrounded => isGrounded;
        public float WheelRadius => wheelRadius;
        public float LastAppliedMotorTorque => lastAppliedMotorTorque;
        public float WheelAngularVelocity => wheelBody != null ? wheelBody.angularVelocity * Mathf.Deg2Rad : 0f;
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
            driveMethod = method;

            groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundMask,
                useTriggers = false
            };

            EnsureWheelComponents(definition, gripRating);
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
            wheelBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            wheelCollider.radius = wheelRadius;
            wheelCollider.offset = Vector2.zero;
            wheelCollider.isTrigger = false;

            // PhysicsMaterial2D for arcade tire grip
            runtimeWheelMaterial = new PhysicsMaterial2D($"{name}_Tire2D")
            {
                friction = Mathf.Clamp(gripRating / 150f, 0.6f, 2.4f),
                bounciness = 0f
            };
            wheelCollider.sharedMaterial = runtimeWheelMaterial;
        }

        private void ConfigureJoint(float anchorLocalX, float axleY)
        {
            if (suspensionJoint == null) suspensionJoint = GetComponent<WheelJoint2D>();
            if (suspensionJoint == null) suspensionJoint = gameObject.AddComponent<WheelJoint2D>();

            suspensionJoint.connectedBody = chassisBody;
            suspensionJoint.autoConfigureConnectedAnchor = false;
            suspensionJoint.anchor = Vector2.zero;
            suspensionJoint.connectedAnchor = new Vector2(anchorLocalX, axleY);

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

        public void Simulate()
        {
            if (wheelBody == null || chassisBody == null) return;

            // 1. Ground detection via universal GroundContactFilter
            isGrounded = false;
            if (wheelCollider != null)
            {
                ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
                int count = wheelCollider.Overlap(filter, groundHitBuffer);
                Transform root = chassisBody != null ? chassisBody.transform.root : transform.root;
                for (int i = 0; i < count; i++)
                {
                    if (GroundContactFilter.IsGround2D(groundHitBuffer[i], root))
                    {
                        isGrounded = true;
                        break;
                    }
                }
            }

            // 2. Progressive Bump Stop
            // When compression exceeds bumpStopStartRatio (e.g. 72%), apply smooth progressive push
            // to absorb hard impacts without unnatural position teleportation
            if (suspensionJoint != null)
            {
                float compression01 = SuspensionCompression01;
                if (compression01 > bumpStopStartRatio)
                {
                    float bumpProgress = Mathf.InverseLerp(bumpStopStartRatio, 1f, compression01);
                    Vector2 suspensionDir = chassisBody.transform.up;
                    float bumpForce = chassisBody.mass * Physics2D.gravity.magnitude *
                                     bumpStopSpringMultiplier * bumpProgress * bumpProgress;

                    wheelBody.AddForce(-suspensionDir * bumpForce, ForceMode2D.Force);
                    chassisBody.AddForceAtPosition(suspensionDir * bumpForce, wheelBody.position, ForceMode2D.Force);
                }
            }

            // 3. Visual synchronization
            SyncVisual();
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
                // When wheel is airborne, prevent runaway over-spin to ensure silky smooth touchdown
                float appliedTorque = requestedTorque;
                if (!isGrounded && chassisBody != null)
                {
                    float forwardSpeed = Vector2.Dot(chassisBody.linearVelocity, chassisBody.transform.right);
                    float expectedSpinDeg = -(forwardSpeed / Mathf.Max(0.1f, wheelRadius)) * Mathf.Rad2Deg;
                    float currentSpinDeg = wheelBody.angularVelocity;
                    if (Mathf.Abs(currentSpinDeg) > Mathf.Abs(expectedSpinDeg) * 1.25f + 120f)
                    {
                        appliedTorque *= 0.10f;
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
            wheelVisual.position = new Vector3(wheelBody.position.x, wheelBody.position.y, wheelVisual.position.z);
            wheelVisual.rotation = Quaternion.Euler(0f, 0f, wheelBody.rotation);
        }

        private void LateUpdate()
        {
            SyncVisual();
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
