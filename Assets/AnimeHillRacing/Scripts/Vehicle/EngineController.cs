using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class EngineController : MonoBehaviour
    {
        private VehicleDefinition definition;
        private ResolvedVehicleStats stats;
        private FuelSystem fuelSystem;
        private float throttle;

        public float CurrentTorqueNm { get; private set; }
        public float CurrentRpm { get; private set; }
        public float AppliedThrottle => throttle;
        public bool Enabled { get; private set; } = true;

        public void Initialize(VehicleDefinition vehicleDefinition, ResolvedVehicleStats resolvedStats,
            FuelSystem targetFuelSystem)
        {
            definition = vehicleDefinition;
            stats = resolvedStats;
            fuelSystem = targetFuelSystem;
            Enabled = true;
        }

        public float Tick(float deltaTime, float requestedThrottle, float rpm, float transmissionTorqueScale,
            float normalizedVehicleSpeed = 0f)
        {
            bool hasFuel = fuelSystem == null || fuelSystem.FuelNormalized > 0f;
            float target = Enabled && hasFuel ? Mathf.Clamp01(requestedThrottle) : 0f;
            throttle = target;
            float normalizedRpm = Mathf.InverseLerp(definition.engine.idleRpm, definition.engine.redlineRpm, rpm);
            CurrentRpm = rpm;
            float curveTorque = definition.engine.torqueCurve.Evaluate(normalizedRpm);
            float speedTorque = definition.engine.speedTorqueCurve != null
                ? Mathf.Max(0f, definition.engine.speedTorqueCurve.Evaluate(Mathf.Max(0f, normalizedVehicleSpeed)))
                : 1f - Mathf.SmoothStep(.55f, 1.04f, Mathf.Max(0f, normalizedVehicleSpeed));
            float lowSpeedBonus = Mathf.Lerp(stats.lowSpeedTorqueMultiplier, 1f, normalizedRpm);
            float recoveredShiftScale = Mathf.Lerp(transmissionTorqueScale,
                Mathf.Sqrt(Mathf.Clamp01(transmissionTorqueScale)), stats.shiftRecovery);
            float limiterScale = rpm >= definition.engine.limiterRpm ? 0f : 1f;
            CurrentTorqueNm = stats.motorTorqueNm * curveTorque * speedTorque * lowSpeedBonus * throttle *
                              recoveredShiftScale * limiterScale;
            return CurrentTorqueNm;
        }

        public void SetEnabled(bool value)
        {
            Enabled = value;
            if (!value)
                throttle = 0f;
        }
    }
}
