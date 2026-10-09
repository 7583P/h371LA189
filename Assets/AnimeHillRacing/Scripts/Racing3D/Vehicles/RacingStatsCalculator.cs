using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingStatsCalculator
    {
        private static readonly Dictionary<string, RacingVehicleStats> BaseVehicleStats =
            new Dictionary<string, RacingVehicleStats>(StringComparer.OrdinalIgnoreCase)
            {
                ["car-01"] = new RacingVehicleStats // Classic Jeep
                {
                    Speed = 145f,
                    Acceleration = 11.5f,
                    Handling = 2.2f,
                    Drift = 1.0f,
                    Grip = 1.25f,
                    Weight = 1100f,
                    MiniTurbo = 1.0f,
                    Offroad = 0.72f,
                    Stability = 1.2f,
                    MiniTurboDuration1 = 0.70f,
                    MiniTurboDuration2 = 1.40f,
                    MiniTurboDuration3 = 2.10f
                },
                ["rally-01"] = new RacingVehicleStats // Rally Car
                {
                    Speed = 158f,
                    Acceleration = 14.5f,
                    Handling = 2.6f,
                    Drift = 1.35f,
                    Grip = 1.15f,
                    Weight = 980f,
                    MiniTurbo = 1.2f,
                    Offroad = 0.68f,
                    Stability = 1.05f,
                    MiniTurboDuration1 = 0.85f,
                    MiniTurboDuration2 = 1.65f,
                    MiniTurboDuration3 = 2.45f
                },
                ["formula-01"] = new RacingVehicleStats // Formula 1 - Formula Apex (MAX capability: 1.0s, 2.0s, 3.0s)
                {
                    Speed = 175f,
                    Acceleration = 16.0f,
                    Handling = 3.0f,
                    Drift = 0.85f,
                    Grip = 1.60f,
                    Weight = 750f,
                    MiniTurbo = 1.35f,
                    Offroad = 0.40f,
                    Stability = 1.45f,
                    MiniTurboDuration1 = 1.0f,
                    MiniTurboDuration2 = 2.0f,
                    MiniTurboDuration3 = 3.0f
                },
                ["buggy-01"] = new RacingVehicleStats // Dunes Buggy
                {
                    Speed = 142f,
                    Acceleration = 13.0f,
                    Handling = 2.3f,
                    Drift = 1.15f,
                    Grip = 1.30f,
                    Weight = 880f,
                    MiniTurbo = 1.1f,
                    Offroad = 0.88f,
                    Stability = 1.35f,
                    MiniTurboDuration1 = 0.75f,
                    MiniTurboDuration2 = 1.50f,
                    MiniTurboDuration3 = 2.20f
                },
                ["sport-bike-01"] = new RacingVehicleStats // Sport Bike
                {
                    Speed = 162f,
                    Acceleration = 15.5f,
                    Handling = 3.2f,
                    Drift = 1.20f,
                    Grip = 1.10f,
                    Weight = 620f,
                    MiniTurbo = 1.45f,
                    Offroad = 0.50f,
                    Stability = 0.85f,
                    MiniTurboDuration1 = 0.90f,
                    MiniTurboDuration2 = 1.80f,
                    MiniTurboDuration3 = 2.65f
                },
                ["hill-adventurer"] = new RacingVehicleStats // Hill Adventurer
                {
                    Speed = 148f,
                    Acceleration = 12.0f,
                    Handling = 2.4f,
                    Drift = 1.05f,
                    Grip = 1.20f,
                    Weight = 1050f,
                    MiniTurbo = 1.1f,
                    Offroad = 0.75f,
                    Stability = 1.15f,
                    MiniTurboDuration1 = 0.75f,
                    MiniTurboDuration2 = 1.50f,
                    MiniTurboDuration3 = 2.15f
                },
                ["aerial-satelite-01"] = new RacingVehicleStats // Aerial Satellite
                {
                    Speed = 168f,
                    Acceleration = 15.0f,
                    Handling = 2.8f,
                    Drift = 1.25f,
                    Grip = 1.35f,
                    Weight = 790f,
                    MiniTurbo = 1.5f,
                    Offroad = 0.65f,
                    Stability = 1.25f,
                    MiniTurboDuration1 = 0.95f,
                    MiniTurboDuration2 = 1.85f,
                    MiniTurboDuration3 = 2.75f
                }
            };

        private static readonly Dictionary<string, WheelRacingModifiers> BaseWheelModifiers =
            new Dictionary<string, WheelRacingModifiers>(StringComparer.OrdinalIgnoreCase)
            {
                ["wheel-default"] = WheelRacingModifiers.Zero,
                ["wheel-sport"] = new WheelRacingModifiers
                {
                    SpeedDelta = 4f,
                    AccelerationDelta = 0.5f,
                    HandlingDelta = 0.15f,
                    DriftDelta = 0.05f,
                    GripDelta = 0.10f,
                    WeightDelta = -20f,
                    MiniTurboDelta = 0.1f,
                    OffroadDelta = -0.08f,
                    StabilityDelta = 0.05f
                },
                ["wheel-offroad"] = new WheelRacingModifiers
                {
                    SpeedDelta = -4f,
                    AccelerationDelta = -0.2f,
                    HandlingDelta = -0.1f,
                    DriftDelta = -0.1f,
                    GripDelta = 0.18f,
                    WeightDelta = 35f,
                    MiniTurboDelta = -0.05f,
                    OffroadDelta = 0.22f,
                    StabilityDelta = 0.15f
                },
                ["wheel-racing"] = new WheelRacingModifiers
                {
                    SpeedDelta = 7f,
                    AccelerationDelta = 1.0f,
                    HandlingDelta = 0.25f,
                    DriftDelta = 0.1f,
                    GripDelta = 0.20f,
                    WeightDelta = -40f,
                    MiniTurboDelta = 0.15f,
                    OffroadDelta = -0.15f,
                    StabilityDelta = 0.10f
                },
                ["wheel-carbon"] = new WheelRacingModifiers
                {
                    SpeedDelta = 3f,
                    AccelerationDelta = 1.5f,
                    HandlingDelta = 0.3f,
                    DriftDelta = 0.15f,
                    GripDelta = 0.05f,
                    WeightDelta = -75f,
                    MiniTurboDelta = 0.20f,
                    OffroadDelta = -0.05f,
                    StabilityDelta = -0.05f
                },
                ["wheel-plasma"] = new WheelRacingModifiers
                {
                    SpeedDelta = 8f,
                    AccelerationDelta = 1.8f,
                    HandlingDelta = 0.35f,
                    DriftDelta = 0.25f,
                    GripDelta = 0.15f,
                    WeightDelta = -50f,
                    MiniTurboDelta = 0.30f,
                    OffroadDelta = 0.05f,
                    StabilityDelta = 0.12f
                }
            };

        static RacingStatsCalculator()
        {
            BaseVehicleStats["hill-adventurer-01"] = BaseVehicleStats["hill-adventurer"];
            // Whity is a new identity for the existing META specification, with no performance inflation.
            BaseWheelModifiers["wheel-whity"] = BaseWheelModifiers["wheel-plasma"];
        }
        public static string NormalizeVehicleId(string id) => id == "hill-adventurer" ? "hill-adventurer-01" : !string.IsNullOrEmpty(id) && BaseVehicleStats.ContainsKey(id) ? id : "car-01";
        public static string NormalizeWheelId(string id) => !string.IsNullOrEmpty(id) && BaseWheelModifiers.ContainsKey(id) ? id : "wheel-default";

        public static RacingVehicleStats GetBaseVehicleStats(string vehicleId)
        {
            if (!string.IsNullOrEmpty(vehicleId) && BaseVehicleStats.TryGetValue(vehicleId, out var stats))
                return stats;

            return RacingVehicleStats.Default;
        }

        public static WheelRacingModifiers GetWheelModifiers(string wheelId)
        {
            if (!string.IsNullOrEmpty(wheelId) && BaseWheelModifiers.TryGetValue(wheelId, out var mods))
                return mods;

            return WheelRacingModifiers.Zero;
        }

        /// <summary>
        /// Single authoritative function that resolves FinalRacingStats for both Player and AI.
        /// Guaranteed: identical inputs produce identical physics performance.
        /// </summary>
        public static FinalRacingStats Calculate(string vehicleId, string wheelId)
        {
            RacingVehicleStats baseStats = GetBaseVehicleStats(vehicleId);
            WheelRacingModifiers wheelMods = GetWheelModifiers(wheelId);
            return Calculate(baseStats, wheelMods);
        }

        public static FinalRacingStats Calculate(RacingVehicleStats baseStats, WheelRacingModifiers wheelMods)
        {
            float finalSpeed = Mathf.Clamp(baseStats.Speed + wheelMods.SpeedDelta, 60f, 240f);
            float finalAccel = Mathf.Clamp(baseStats.Acceleration + wheelMods.AccelerationDelta, 4f, 30f);
            float finalHandling = Mathf.Clamp(baseStats.Handling + wheelMods.HandlingDelta, 1.0f, 5.0f);
            float finalDrift = Mathf.Clamp(baseStats.Drift + wheelMods.DriftDelta, 0.4f, 2.5f);
            float finalGrip = Mathf.Clamp(baseStats.Grip + wheelMods.GripDelta, 0.5f, 2.5f);
            float finalWeight = Mathf.Clamp(baseStats.Weight + wheelMods.WeightDelta, 400f, 2200f);
            float finalMiniTurbo = Mathf.Clamp(baseStats.MiniTurbo + wheelMods.MiniTurboDelta, 0.4f, 3.0f);
            float finalOffroad = Mathf.Clamp01(baseStats.Offroad + wheelMods.OffroadDelta);
            float finalStability = Mathf.Clamp(baseStats.Stability + wheelMods.StabilityDelta, 0.4f, 3.0f);
            float finalDuration1 = Mathf.Clamp(baseStats.MiniTurboDuration1 > 0.01f ? baseStats.MiniTurboDuration1 : 0.75f, 0.4f, 1.0f);
            float finalDuration2 = Mathf.Clamp(baseStats.MiniTurboDuration2 > 0.01f ? baseStats.MiniTurboDuration2 : 1.50f, 0.8f, 2.0f);
            float finalDuration3 = Mathf.Clamp(baseStats.MiniTurboDuration3 > 0.01f ? baseStats.MiniTurboDuration3 : 2.25f, 1.2f, 3.0f);

            return new FinalRacingStats
            {
                SpeedKmh = finalSpeed,
                AccelerationMps2 = finalAccel,
                HandlingRadPerSec = finalHandling,
                DriftMultiplier = finalDrift,
                LateralGrip = finalGrip,
                WeightKg = finalWeight,
                MiniTurboMultiplier = finalMiniTurbo,
                OffroadRetention = Mathf.Max(0.2f, finalOffroad),
                Stability = finalStability,
                MiniTurboDuration1 = finalDuration1,
                MiniTurboDuration2 = finalDuration2,
                MiniTurboDuration3 = finalDuration3
            };
        }
    }
}
