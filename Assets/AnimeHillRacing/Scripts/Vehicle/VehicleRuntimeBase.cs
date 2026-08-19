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
        public virtual Rigidbody Body => null;
        public virtual Rigidbody2D Body2D => null;
        public abstract void SetDriveEnabled(bool value);
        public abstract void AddFuel(float normalizedAmount);

        public virtual void RefillFuelFull() => Fuel?.RefillFull();

        public virtual void TeleportToPosition(Vector3 targetPosition)
        {
            if (Body != null)
            {
                Body.position = targetPosition;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            if (Body2D != null)
            {
                Body2D.position = new Vector2(targetPosition.x, targetPosition.y);
                Body2D.linearVelocity = Vector2.zero;
                Body2D.angularVelocity = 0f;
            }
            transform.position = targetPosition;
            transform.rotation = Quaternion.identity;
        }

        public virtual void SetTransmissionMode(TransmissionMode mode)
        {
            Transmission?.SetMode(mode);
        }
    }
}
