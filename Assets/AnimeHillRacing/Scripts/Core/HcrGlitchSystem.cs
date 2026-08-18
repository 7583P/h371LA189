using UnityEngine;

namespace AnimeHillRacing.Core
{
    [System.Serializable]
    public sealed class HcrGlitchSettings
    {
        public bool enabled = true;
        [Tooltip("Distance in meters where the float glitch phases begin.")]
        [Min(0f)] public float startDistance = 32768f;
        [Tooltip("Maximum visual sink depth in meters for grounded wheels.")]
        [Range(0.01f, 0.40f)] public float maxSinkDepth = 0.16f;
        [Tooltip("Additional rolling resistance factor applied as wheels sink into the ground.")]
        [Range(0f, 0.25f)] public float maxExtraRollingResistance = 0.09f;
        [Tooltip("Maximum camera jitter intensity in meters.")]
        [Range(0f, 0.20f)] public float maxCameraJitter = 0.055f;
        [Tooltip("Enable retro texture UV precision quantization at extreme distance.")]
        public bool enableTextureGlitch = true;
    }

    public struct HcrGlitchState
    {
        public bool isActive;
        public float intensity01;
        public float sinkDepth;
        public float extraRollingResistance;
        public float quantizationStep;
        public Vector2 cameraJitter;
    }

    public static class HcrGlitchSystem
    {
        public const float DefaultGlitchDistance = 32768f;

        public static HcrGlitchState Evaluate(float worldX, HcrGlitchSettings settings = null)
        {
            HcrGlitchState state = default;
            bool isEnabled = settings != null ? settings.enabled : true;
            if (!isEnabled) return state;

            float startDist = settings != null ? settings.startDistance : DefaultGlitchDistance;
            if (worldX < startDist) return state;

            float maxSink = settings != null ? settings.maxSinkDepth : 0.16f;
            float maxResistance = settings != null ? settings.maxExtraRollingResistance : 0.09f;
            float maxJitter = settings != null ? settings.maxCameraJitter : 0.055f;

            // Distance past threshold
            float excess = worldX - startDist;

            // Normalized intensity that grows through the glitch phases (32K -> 65K -> 131K)
            float phaseProgress = Mathf.Clamp01(excess / (startDist * 3f));
            state.isActive = true;
            state.intensity01 = phaseProgress;

            // 1. Wheel sinking depth (0m to maxSink)
            state.sinkDepth = Mathf.Lerp(0.025f, maxSink, phaseProgress);

            // 2. Extra rolling resistance
            state.extraRollingResistance = Mathf.Lerp(0.015f, maxResistance, phaseProgress);

            // 3. Float quantization step: emulates IEEE 754 float precision loss at distance (ULP)
            // At 32K: ~0.0039m (~4mm). At 65K: ~0.0078m (~8mm). At 131K: ~0.0156m (~16mm).
            float exponent = Mathf.Floor(Mathf.Log(Mathf.Max(1f, worldX), 2f));
            state.quantizationStep = Mathf.Pow(2f, exponent - 23f);

            // 4. Camera jitter at ~60Hz (pseudo-random per-frame jitter)
            float jitterTime = Time.time * 62f;
            float jitterMag = Mathf.Lerp(0.012f, maxJitter, phaseProgress);
            float jX = (Mathf.PerlinNoise(jitterTime, 17.3f) - 0.5f) * 2f * jitterMag;
            float jY = (Mathf.PerlinNoise(43.7f, jitterTime) - 0.5f) * 2f * jitterMag;
            state.cameraJitter = new Vector2(jX, jY);

            return state;
        }

        public static float Quantize(float value, float step)
        {
            if (step <= 0.00001f) return value;
            return Mathf.Round(value / step) * step;
        }
    }
}
