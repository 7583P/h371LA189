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
        public bool UseItem;                     // Item activation button triggered
        public bool Jump;                        // Hop / Jump action triggered
        public bool Trick;                       // In-air trick / stunt action triggered
        [Range(-1f, 1f)] public float Pitch;    // In-air nose up / down pitch control

        public static VehicleCommands Idle => new VehicleCommands
        {
            Steering = 0f,
            Throttle = 0f,
            Brake = 0f,
            Drift = false,
            UseItem = false,
            Jump = false,
            Trick = false,
            Pitch = 0f
        };
    }

    public interface IVehicleCommandProvider
    {
        VehicleCommands GetCommands();
    }
}
