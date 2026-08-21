using UnityEngine;

namespace AnimeHillRacing.Experiments.Box2DJeep
{
    [DisallowMultipleComponent]
    public sealed class Box2DJeepRig : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D chassis;
        [SerializeField] private Rigidbody2D rearWheel;
        [SerializeField] private Rigidbody2D frontWheel;
        [SerializeField] private WheelJoint2D rearJoint;
        [SerializeField] private WheelJoint2D frontJoint;
        [SerializeField] private AbsoluteEdgeTerrain2D terrain;
        [SerializeField] private float maximumMotorSpeed = 1800f;
        [SerializeField] private float driveTorque = 85f;
        [SerializeField] private float brakeTorque = 160f;
        [SerializeField] private float airControlTorque = 7f;
        [SerializeField] private bool showInstructions = true;

        public Rigidbody2D Chassis => chassis;
        public Rigidbody2D RearWheel => rearWheel;
        public Rigidbody2D FrontWheel => frontWheel;
        public WheelJoint2D RearJoint => rearJoint;
        public WheelJoint2D FrontJoint => frontJoint;
        public AbsoluteEdgeTerrain2D Terrain => terrain;
        public CircleCollider2D RearCollider => rearWheel != null ? rearWheel.GetComponent<CircleCollider2D>() : null;
        public CircleCollider2D FrontCollider => frontWheel != null ? frontWheel.GetComponent<CircleCollider2D>() : null;

        public void Configure(
            Rigidbody2D chassisBody,
            Rigidbody2D rearWheelBody,
            Rigidbody2D frontWheelBody,
            WheelJoint2D rearWheelJoint,
            WheelJoint2D frontWheelJoint,
            AbsoluteEdgeTerrain2D absoluteTerrain)
        {
            chassis = chassisBody;
            rearWheel = rearWheelBody;
            frontWheel = frontWheelBody;
            rearJoint = rearWheelJoint;
            frontJoint = frontWheelJoint;
            terrain = absoluteTerrain;
        }

        private void FixedUpdate()
        {
            float drive = Input.GetAxisRaw("Horizontal");
            float brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;
            StepDrive(drive, brake);
        }

        public void StepDrive(float drive, float brake)
        {
            drive = Mathf.Clamp(drive, -1f, 1f);
            brake = Mathf.Clamp01(brake);

            float requestedSpeed = drive * maximumMotorSpeed;
            float requestedTorque = driveTorque * Mathf.Abs(drive);
            if (brake > 0.001f)
            {
                requestedSpeed = 0f;
                requestedTorque = brakeTorque * brake;
            }

            bool useMotor = requestedTorque > 0.001f;
            ApplyMotor(rearJoint, requestedSpeed, requestedTorque, useMotor);
            ApplyMotor(frontJoint, requestedSpeed, requestedTorque, useMotor);

            if (Mathf.Abs(drive) > 0.01f && !AnyWheelGrounded() && chassis != null)
            {
                chassis.AddTorque(drive * airControlTorque, ForceMode2D.Force);
            }
        }

        public bool AnyWheelGrounded()
        {
            EdgeCollider2D edge = terrain != null ? terrain.TerrainCollider : null;
            if (edge == null)
            {
                return false;
            }

            return IsNearContact(RearCollider, edge) || IsNearContact(FrontCollider, edge);
        }

        private static bool IsNearContact(Collider2D wheel, Collider2D edge)
        {
            if (wheel == null || edge == null)
            {
                return false;
            }

            ColliderDistance2D distance = Physics2D.Distance(wheel, edge);
            return distance.isValid && distance.distance <= Physics2D.defaultContactOffset * 2f;
        }

        private static void ApplyMotor(WheelJoint2D joint, float speed, float torque, bool enabled)
        {
            if (joint == null)
            {
                return;
            }

            JointMotor2D motor = joint.motor;
            motor.motorSpeed = speed;
            motor.maxMotorTorque = Mathf.Max(0f, torque);
            joint.motor = motor;
            joint.useMotor = enabled;
        }

        private void OnGUI()
        {
            if (!showInstructions)
            {
                return;
            }

            GUI.Label(new Rect(16f, 16f, 520f, 24f), "BOX2D JEEP POC - Left/Right: drive or reverse, Space: brake");
        }
    }
}
