using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public struct RacingVehicleStats
    {
        [Tooltip("Top speed in km/h on flat asphalt")]
        [Range(80f, 220f)] public float Speed;

        [Tooltip("Forward acceleration in m/s^2")]
        [Range(5f, 25f)] public float Acceleration;

        [Tooltip("Steering agility / yaw rate in rad/sec")]
        [Range(1.2f, 4.5f)] public float Handling;

        [Tooltip("Drift slip angle authority and mini-turbo build agility")]
        [Range(0.5f, 2.0f)] public float Drift;

        [Tooltip("Lateral tire traction coefficient")]
        [Range(0.6f, 2.2f)] public float Grip;

        [Tooltip("Vehicle mass in kg (affects collisions and momentum)")]
        [Range(500f, 1800f)] public float Weight;

        [Tooltip("Mini-turbo boost strength and duration multiplier")]
        [Range(0.5f, 2.5f)] public float MiniTurbo;

        [Tooltip("Speed retention multiplier when driving on off-road terrain [0.3 to 1.0]")]
        [Range(0.3f, 1.0f)] public float Offroad;

        [Tooltip("Resistance to rolling/flipping, downforce, and center-of-mass righting torque")]
        [Range(0.5f, 2.5f)] public float Stability;

        public static RacingVehicleStats Default => new RacingVehicleStats
        {
            Speed = 150f,
            Acceleration = 12f,
            Handling = 2.4f,
            Drift = 1.0f,
            Grip = 1.2f,
            Weight = 950f,
            MiniTurbo = 1.0f,
            Offroad = 0.65f,
            Stability = 1.0f
        };
    }

    [Serializable]
    public struct WheelRacingModifiers
    {
        public float SpeedDelta;
        public float AccelerationDelta;
        public float HandlingDelta;
        public float DriftDelta;
        public float GripDelta;
        public float WeightDelta;
        public float MiniTurboDelta;
        public float OffroadDelta;
        public float StabilityDelta;

        public static WheelRacingModifiers Zero => default;
    }

    [Serializable]
    public struct FinalRacingStats
    {
        public float SpeedKmh;
        public float AccelerationMps2;
        public float HandlingRadPerSec;
        public float DriftMultiplier;
        public float LateralGrip;
        public float WeightKg;
        public float MiniTurboMultiplier;
        public float OffroadRetention;
        public float Stability;

        public override string ToString() =>
            $"Speed: {SpeedKmh:F1} km/h | Accel: {AccelerationMps2:F1} | Grip: {LateralGrip:F2} | Drift: {DriftMultiplier:F2} | Weight: {WeightKg:F0}kg";
    }
}
