using UnityEngine;

namespace AnimeHillRacing.Core
{
    [System.Serializable]
    public sealed class HcrGlitchSettings
    {
        public bool enabled = true;
        [Tooltip("Distance in meters where the float glitch phases begin.")]
        [Min(0f)] public float startDistance = 32768f;
        [Tooltip("Maximum camera jitter intensity in meters.")]
        [Range(0f, 0.20f)] public float maxCameraJitter = 0.055f;
        [Tooltip("Enable retro texture UV precision quantization at extreme distance.")]
        public bool enableTextureGlitch = true;
    }

    public struct HcrGlitchState
    {
        public bool isActive;
        public float intensity01;
        public float quantizationStep;
        public Vector2 cameraJitter;
    }

    public static class LongDistancePrecision
    {
        public const float DefaultStartDistance = 32768f;

        public static float CalculateFloatStep(float worldDistance, float startDistance = DefaultStartDistance)
        {
            float absDist = Mathf.Abs(worldDistance);
            if (absDist < startDistance) return 0f;

            // IEEE 754 float32 resolution step (ULP = 2^(exponent - 23))
            float exponent = Mathf.Floor(Mathf.Log(absDist, 2f));
            return Mathf.Pow(2f, exponent - 23f);
        }

        public static int CalculatePrecisionTier(float worldDistance, float startDistance = DefaultStartDistance)
        {
            float absDist = Mathf.Abs(worldDistance);
            if (absDist < startDistance) return 0;
            return Mathf.FloorToInt(Mathf.Log(absDist, 2f));
        }

        public static float Quantize(float value, float step)
        {
            if (step <= 0.000001f) return value;
            return Mathf.Round(value / step) * step;
        }
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

            float maxJitter = settings != null ? settings.maxCameraJitter : 0.055f;

            // Distance past threshold
            float excess = worldX - startDist;

            // Normalized intensity that grows through the glitch phases (32K -> 65K -> 131K)
            float phaseProgress = Mathf.Clamp01(excess / (startDist * 3f));
            state.isActive = true;
            state.intensity01 = phaseProgress;

            // 3. Float quantization step: emulates IEEE 754 float precision loss at distance (ULP)
            // At 32K: ~0.0039m (~4mm). At 65K: ~0.0078m (~8mm). At 131K: ~0.0156m (~16mm).
            state.quantizationStep = LongDistancePrecision.CalculateFloatStep(worldX, startDist);

            // 4. Camera jitter at ~60Hz (pseudo-random per-frame jitter)
            float jitterTime = Time.time * 62f;
            float jitterMag = Mathf.Lerp(0.012f, maxJitter, phaseProgress);
            float jX = (Mathf.PerlinNoise(jitterTime, 17.3f) - 0.5f) * 2f * jitterMag;
            float jY = (Mathf.PerlinNoise(43.7f, jitterTime) - 0.5f) * 2f * jitterMag;
            state.cameraJitter = new Vector2(jX, jY);

            return state;
        }

        public static float Quantize(float value, float step) => LongDistancePrecision.Quantize(value, step);
    }
}
