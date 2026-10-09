using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public struct VehicleCommands
    {
        [Range(-1f, 1f)] public float Steering; // -1 (left) to +1 (right)
        [Range(0f, 1f)]  public float Throttle; // 0 (idle) to 1 (full throttle)
        [Range(0f, 1f)]  public float Brake;    // 0 (none) to 1 (full brake / reverse)
        public bool Drift;                       // Powerslide / drift button held
        public bool MiniHop;                     // Rising edge of the pre-drift button; independent of Jump
        public bool UseItem;                     // Item activation button triggered
        public bool ItemBackward;                // Throw item backward (e.g. drop Rock behind)
        public bool Jump;                        // Hop / Jump action triggered
        public bool Trick;                       // In-air trick / stunt action triggered
        public bool AerialBoost;                 // In-air boost activation
        [Range(-1f, 1f)] public float Pitch;    // In-air nose up / down pitch control
        [Range(-1f, 1f)] public float AirRoll;  // In-air roll control: -1 (Q / left) to +1 (E / right)

        public static VehicleCommands Idle => new VehicleCommands
        {
            Steering = 0f,
            Throttle = 0f,
            Brake = 0f,
            Drift = false,
            UseItem = false,
            ItemBackward = false,
            Jump = false,
            Trick = false,
            AerialBoost = false,
            Pitch = 0f,
            AirRoll = 0f
        };
    }

    public interface IVehicleCommandProvider
    {
        VehicleCommands GetCommands();
    }
}
