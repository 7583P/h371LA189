using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class VehicleCamera : MonoBehaviour
    {
        [SerializeField] private VehicleRuntimeBase target;
        [SerializeField, Min(3f)] private float orthographicSize = 11.5f;
        [SerializeField, Range(0.2f, 0.5f)] private float horizontalScreenPosition = 0.32f;
        [SerializeField, Min(0f)] private float speedLookAheadBonus = 1.4f;
        [SerializeField] private float cameraHeight = 2.6f;
        [SerializeField, Min(0.01f)] private float horizontalSmooth = 0.12f;
        [SerializeField, Min(0.01f)] private float verticalSmooth = 0.42f;
        [SerializeField, Min(1f)] private float reverseTolerance = 6f;
        [SerializeField, Min(0.1f)] private float lookAheadResponse = 2.2f;

        private Camera cameraComponent;
        private float horizontalVelocity;
        private float smoothedTargetY;
        private float verticalVelocity;
        private float smoothedLookAhead;
        private float progressAnchorX;
        private float fixedZ;
        private bool initialized;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = orthographicSize;
            fixedZ = transform.position.z;
            transform.rotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            if (!initialized)
            {
                smoothedTargetY = target.transform.position.y;
                progressAnchorX = target.transform.position.x;
                float initialWidth = orthographicSize * 2f * cameraComponent.aspect;
                smoothedLookAhead = (0.5f - horizontalScreenPosition) * initialWidth;
                initialized = true;
            }

            float speed01 = Mathf.InverseLerp(0f, Mathf.Max(1f, target.Stats.topSpeedMps),
                Mathf.Max(0f, target.SpeedMps));
            if (Mathf.Abs(target.transform.position.x - progressAnchorX) > 50f)
            {
                progressAnchorX = target.transform.position.x;
                smoothedTargetY = target.transform.position.y;
                horizontalVelocity = 0f;
                verticalVelocity = 0f;
            }
            else
            {
                progressAnchorX = Mathf.Max(progressAnchorX, target.transform.position.x);
                if (progressAnchorX - target.transform.position.x > reverseTolerance)
                    progressAnchorX = target.transform.position.x + reverseTolerance;
            }
            smoothedTargetY = Mathf.SmoothDamp(smoothedTargetY, target.transform.position.y, ref verticalVelocity,
                verticalSmooth);
            float visibleWidth = orthographicSize * 2f * cameraComponent.aspect;
            float baseLookAhead = (0.5f - horizontalScreenPosition) * visibleWidth;
            float targetLookAhead = baseLookAhead + speedLookAheadBonus * speed01;
            smoothedLookAhead = Mathf.Lerp(smoothedLookAhead, targetLookAhead,
                1f - Mathf.Exp(-lookAheadResponse * Time.deltaTime));
            float desiredX = progressAnchorX + smoothedLookAhead;
            float x = Mathf.SmoothDamp(transform.position.x, desiredX, ref horizontalVelocity, horizontalSmooth);
            float y = smoothedTargetY + cameraHeight;

            // Replicate Hill Climb Racing float jitter and quantization at extreme distances
            HcrGlitchState glitch = HcrGlitchSystem.Evaluate(target.transform.position.x);
            if (glitch.isActive)
            {
                x = HcrGlitchSystem.Quantize(x, glitch.quantizationStep) + glitch.cameraJitter.x;
                y = HcrGlitchSystem.Quantize(y, glitch.quantizationStep) + glitch.cameraJitter.y;
            }

            transform.position = new Vector3(x, y, fixedZ);
            transform.rotation = Quaternion.identity;
            cameraComponent.orthographicSize = orthographicSize;
        }

        public void AssignTarget(VehicleRuntimeBase vehicle) => target = vehicle;
    }
}
