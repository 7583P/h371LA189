using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingCamera3D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private RacingVehiclePhysics targetPhysics;

        [Header("Follow Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.8f, -6.5f);
        [SerializeField] private float positionSmoothTime = 0.08f;
        [SerializeField] private float rotationSmoothSpeed = 12f;
        [SerializeField] private float lookAheadDistance = 4f;

        [Header("Dynamic FOV")]
        [SerializeField] private Camera cam;
        [SerializeField] private float baseFov = 60f;
        [SerializeField] private float maxFov = 74f;
        [SerializeField] private float fovSmoothSpeed = 4f;

        [Header("Collision")]
        [SerializeField] private LayerMask obstacleLayer = ~0;
        [SerializeField] private float minDistance = 2.0f;

        private Vector3 currentVelocity;
        private bool isReversingView;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        public void SetTarget(Transform newTarget, RacingVehiclePhysics physics = null)
        {
            target = newTarget;
            targetPhysics = physics ?? (newTarget != null ? newTarget.GetComponent<RacingVehiclePhysics>() : null);
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 desiredPos = target.position + target.TransformDirection(offset);
            transform.position = desiredPos;
            transform.LookAt(target.position + target.forward * lookAheadDistance + Vector3.up * 1.2f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float speedRatio = 0f;
            if (targetPhysics != null)
            {
                speedRatio = Mathf.Clamp01(targetPhysics.SpeedKmh / 160f);
                if (targetPhysics.IsBoosting) speedRatio = 1.0f;
            }

            // 1. Dynamic FOV based on speed and boost
            if (cam != null)
            {
                float targetFov = Mathf.Lerp(baseFov, maxFov, speedRatio);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovSmoothSpeed);
            }

            // 2. Position follow with speed distance dilation
            Vector3 dynamicOffset = offset;
            dynamicOffset.z -= (speedRatio * 1.5f); // pull back slightly at top speed
            dynamicOffset.y += (speedRatio * 0.4f);

            Vector3 worldOffset = target.TransformDirection(dynamicOffset);
            Vector3 desiredPosition = target.position + worldOffset;

            // 3. Camera collision avoidance
            Vector3 targetLookAt = target.position + Vector3.up * 1.2f;
            Vector3 fromTarget = desiredPosition - targetLookAt;
            float desiredDistance = fromTarget.magnitude;

            if (Physics.SphereCast(targetLookAt, 0.35f, fromTarget.normalized, out RaycastHit hit, desiredDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = targetLookAt + fromTarget.normalized * Mathf.Max(minDistance, hit.distance - 0.2f);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

            // 4. Look rotation with steering / velocity look-ahead
            Vector3 lookTarget = target.position + (target.forward * lookAheadDistance) + Vector3.up * 1.1f;
            Vector3 lookDir = (lookTarget - transform.position).normalized;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothSpeed);
            }
        }
    }
}
