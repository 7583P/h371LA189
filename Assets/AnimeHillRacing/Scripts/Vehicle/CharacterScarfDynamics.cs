using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>
    /// Handles procedural dynamic motion for the character's Scarf.
    /// The neck wrap remains rigidly attached to the character's neck,
    /// while the free tail streams and reacts dynamically to speed, acceleration, airtime, and jumps.
    /// </summary>
    public sealed class CharacterScarfDynamics : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer scarfRenderer;
        [SerializeField] private Transform neckPivot;
        [SerializeField] private Rigidbody vehicleBody;

        private float currentTailAngle;
        private float angleVelocity;
        private Vector3 previousVelocity;
        private float baseRotationZ;
        private Vector3 baseLocalPosition;

        public void Initialize(SpriteRenderer renderer, Rigidbody body, Transform neckPoint)
        {
            scarfRenderer = renderer;
            vehicleBody = body;
            neckPivot = neckPoint;
            baseLocalPosition = transform.localPosition;
            baseRotationZ = transform.localEulerAngles.z;
            currentTailAngle = 0f;
            angleVelocity = 0f;
            previousVelocity = body != null ? body.linearVelocity : Vector3.zero;
        }

        private void LateUpdate()
        {
            if (scarfRenderer == null || !scarfRenderer.enabled) return;

            float dt = Mathf.Max(0.001f, Time.deltaTime);

            // 1. Idle gentle wind flutter
            float idleFlutter = Mathf.Sin(Time.time * 4.2f) * 4.5f + Mathf.Cos(Time.time * 2.1f) * 2.0f;

            // 2. Motion and velocity reaction
            float speedReaction = 0f;
            float verticalReaction = 0f;
            float accelReaction = 0f;

            if (vehicleBody != null)
            {
                Vector3 currentVel = vehicleBody.linearVelocity;
                Vector3 acceleration = (currentVel - previousVelocity) / dt;
                previousVelocity = currentVel;

                // Forward speed: tail streams backward (away from direction of travel)
                float forwardSpeed = Vector3.Dot(currentVel, vehicleBody.transform.right);
                speedReaction = Mathf.Clamp(-forwardSpeed * 1.65f, -32f, 15f);

                // Acceleration reaction (inertial lag)
                float forwardAccel = Vector3.Dot(acceleration, vehicleBody.transform.right);
                accelReaction = Mathf.Clamp(-forwardAccel * 0.45f, -18f, 18f);

                // Vertical reaction (jumps and landings)
                float verticalSpeed = currentVel.y;
                verticalReaction = Mathf.Clamp(-verticalSpeed * 1.8f, -22f, 25f);
            }

            // 3. Compute target angle with strict clamps to prevent flipping or crossing the head
            float targetAngle = Mathf.Clamp(idleFlutter + speedReaction + accelReaction + verticalReaction, -42f, 38f);

            // 4. Smooth damping (zero jitter, responsive fluid spring motion)
            currentTailAngle = Mathf.SmoothDamp(currentTailAngle, targetAngle, ref angleVelocity, 0.075f, 300f, dt);

            // 5. Apply dynamic secondary rotation around the neck anchor
            transform.localRotation = Quaternion.Euler(0f, 0f, baseRotationZ + currentTailAngle);
        }
    }
}
