using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public enum DriveDirection
    {
        Forward,
        BrakingToReverse,
        Reverse,
        BrakingToForward
    }

    public abstract class VehicleRuntimeBase : MonoBehaviour
    {
        public abstract float SpeedMps { get; }
        public float SpeedKmh => SpeedMps * 3.6f;
        public abstract float ProgressPosition { get; }
        public virtual float LogicalDistance => ProgressPosition * Core.GameManager.LogicalDistanceScale;
        public abstract float FuelNormalized { get; }
        public abstract float MotorTorque { get; }
        public abstract float AppliedThrottle { get; }
        public abstract float EngineRpm { get; }
        public abstract float WheelSlip { get; }
        public abstract bool AnyGrounded { get; }
        public virtual bool FrontWheelGrounded => AnyGrounded;
        public virtual bool RearWheelGrounded => AnyGrounded;
        public virtual bool AnyWheelGrounded => AnyGrounded;
        public virtual bool BothWheelsGrounded => AnyGrounded;
        public virtual bool ChassisGrounded => false;
        public virtual bool FullyAirborne => !AnyGrounded;
        public virtual bool Airborne => FullyAirborne;
        public abstract TransmissionController Transmission { get; }
        public abstract VehicleDefinition Definition { get; }
        public abstract ResolvedVehicleStats Stats { get; }
        public abstract FuelSystem Fuel { get; }
        public abstract Rigidbody Body { get; }
        public abstract void SetDriveEnabled(bool value);
        public abstract void AddFuel(float normalizedAmount);

        public virtual void RefillFuelFull() => Fuel?.RefillFull();

        public virtual void SetTransmissionMode(TransmissionMode mode)
        {
            Transmission?.SetMode(mode);
        }
    }
}
