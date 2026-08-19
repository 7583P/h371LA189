using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Road
{
    [RequireComponent(typeof(Camera))]
    public sealed class RoadChaseCamera : MonoBehaviour
    {
        [SerializeField] private VehicleRuntimeBase target;
        [SerializeField] private float height = 7.5f;
        [SerializeField] private float distance = 13f;
        [SerializeField, Range(10f, 55f)] private float pitch = 28f;
        [SerializeField] private float fieldOfView = 58f;
        [SerializeField] private float highSpeedFieldOfView = 63f;
        [SerializeField] private float lookAhead = 6f;
        [SerializeField] private float highSpeedLookAheadBonus = 5f;
        [SerializeField] private float smoothness = 0.22f;
        [SerializeField] private float verticalSmoothness = 0.48f;
        [SerializeField] private float directionResponse = 4f;
        private Vector3 velocity;
        private float verticalVelocity;
        private float filteredTargetY;
        private Vector3 stableForward;
        private bool initialized;
        private Camera cameraComponent;

        private void Awake() => cameraComponent = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 chassisForward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
            if (chassisForward.sqrMagnitude < 0.001f)
                chassisForward = stableForward.sqrMagnitude > 0.001f ? stableForward : Vector3.forward;
            else
                chassisForward.Normalize();
            Vector3 velocityForward = target.Body != null
                ? Vector3.ProjectOnPlane(target.Body.linearVelocity, Vector3.up) : Vector3.zero;
            if (velocityForward.sqrMagnitude > 4f) velocityForward.Normalize();
            else velocityForward = chassisForward;
            Vector3 desiredForward = Vector3.Slerp(chassisForward, velocityForward, 0.28f).normalized;
            if (!initialized)
            {
                stableForward = desiredForward;
                filteredTargetY = target.transform.position.y;
                initialized = true;
            }

            stableForward = Vector3.Slerp(stableForward, desiredForward,
                1f - Mathf.Exp(-directionResponse * Time.deltaTime)).normalized;
            filteredTargetY = Mathf.SmoothDamp(filteredTargetY, target.transform.position.y, ref verticalVelocity,
                verticalSmoothness);
            Vector3 filteredTarget = new Vector3(target.transform.position.x, filteredTargetY,
                target.transform.position.z);
            Vector3 desired = filteredTarget - stableForward * distance + Vector3.up * height;
            Vector3 camPos = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothness);
            HcrGlitchState glitch = HcrGlitchSystem.Evaluate(target != null ? target.LogicalDistance : 0f);
            if (glitch.isActive)
            {
                camPos.x = HcrGlitchSystem.Quantize(camPos.x, glitch.quantizationStep) + glitch.cameraJitter.x;
                camPos.y = HcrGlitchSystem.Quantize(camPos.y, glitch.quantizationStep) + glitch.cameraJitter.y;
                camPos.z = HcrGlitchSystem.Quantize(camPos.z, glitch.quantizationStep);
            }
            transform.position = camPos;
            float speed01 = Mathf.InverseLerp(0f, Mathf.Max(1f, target.Stats.topSpeedMps), Mathf.Abs(target.SpeedMps));
            Vector3 focus = filteredTarget + stableForward * (lookAhead + highSpeedLookAheadBonus * speed01);
            Vector3 horizontalLook = Vector3.ProjectOnPlane(focus - transform.position, Vector3.up);
            if (horizontalLook.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(horizontalLook.normalized, Vector3.up) *
                                     Quaternion.Euler(pitch, 0f, 0f);
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView,
                Mathf.Lerp(fieldOfView, highSpeedFieldOfView, speed01), Time.deltaTime * 3f);
        }

        public void Assign(VehicleRuntimeBase followTarget) => target = followTarget;
    }
}
