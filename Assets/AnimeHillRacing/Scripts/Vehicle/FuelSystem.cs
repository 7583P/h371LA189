using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class FuelSystem : MonoBehaviour
    {
        public float CurrentFuel { get; private set; } = 100f;
        public float MaxFuel { get; private set; } = 100f;
        public float NormalizedFuel => MaxFuel > 0f ? Mathf.Clamp01(CurrentFuel / MaxFuel) : 0f;
        public float FuelNormalized => NormalizedFuel;
        public float TotalRunDurationSeconds { get; private set; } = 60f;
        public float CurrentConsumptionNormalizedPerSecond { get; private set; }
        public float CurrentConsumptionPerSecond { get; private set; }
        public bool CanConsume { get; private set; } = true;
        public float ConsumptionMultiplier { get; private set; } = 1f;

        public void Initialize(VehicleDefinition definition, ResolvedVehicleStats stats)
        {
            TotalRunDurationSeconds = Mathf.Max(1f, stats.totalRunFuelDuration);
            MaxFuel = Mathf.Max(1f, definition.fuel.capacityUnits);
            CurrentFuel = MaxFuel;
            CurrentConsumptionNormalizedPerSecond = 0f;
            CurrentConsumptionPerSecond = 0f;
            CanConsume = true;
            ConsumptionMultiplier = 1f;
        }

        public void TickRun(float deltaTime, bool runIsActive)
        {
            if (!runIsActive || !CanConsume || Time.timeScale <= 0f || CurrentFuel <= 0f)
            {
                CurrentConsumptionNormalizedPerSecond = 0f;
                CurrentConsumptionPerSecond = 0f;
                return;
            }
            CurrentConsumptionNormalizedPerSecond = ConsumptionMultiplier / TotalRunDurationSeconds;
            CurrentConsumptionPerSecond = CurrentConsumptionNormalizedPerSecond * MaxFuel;
            CurrentFuel = Mathf.Max(0f, CurrentFuel - CurrentConsumptionPerSecond * Mathf.Max(0f, deltaTime));
        }

        public void AddFuel(float normalizedAmount)
        {
            SetNormalized(NormalizedFuel + Mathf.Max(0f, normalizedAmount));
        }

        public void SetNormalized(float normalizedAmount)
        {
            CurrentFuel = Mathf.Clamp01(normalizedAmount) * MaxFuel;
        }

        public void RefillFull()
        {
            SetNormalized(1f);
            CurrentConsumptionNormalizedPerSecond = 0f;
            CurrentConsumptionPerSecond = 0f;
        }

        public void SetConsumptionEnabled(bool enabled)
        {
            CanConsume = enabled;
            if (!enabled)
            {
                CurrentConsumptionNormalizedPerSecond = 0f;
                CurrentConsumptionPerSecond = 0f;
            }
        }

        public void SetConsumptionMultiplier(float multiplier)
        {
            ConsumptionMultiplier = Mathf.Clamp(multiplier, 1f, 4f);
        }
    }
}
