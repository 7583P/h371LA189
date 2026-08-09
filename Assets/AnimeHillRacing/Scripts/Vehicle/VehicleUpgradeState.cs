using System;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [Serializable]
    public sealed class VehicleUpgradeState
    {
        [Min(1)] public int engineTune = 1;
        [Min(1)] public int accelerationTune = 1;
        [Min(1)] public int suspensionTune = 1;
        [Min(1)] public int gripTune = 1;
        [Min(0)] public int airControlTune;
        [Min(1)] public int downforceTune = 1;
        [Min(1)] public int fuelTune = 1;
        [Min(1)] public int turboTune = 1;

        public int GetTune(VehicleUpgradeType type)
        {
            switch (type)
            {
                case VehicleUpgradeType.Engine: return engineTune;
                case VehicleUpgradeType.Acceleration: return accelerationTune;
                case VehicleUpgradeType.Suspension: return suspensionTune;
                case VehicleUpgradeType.Grip: return gripTune;
                case VehicleUpgradeType.AirControl: return airControlTune;
                case VehicleUpgradeType.Downforce: return downforceTune;
                case VehicleUpgradeType.Turbo: return turboTune;
                default: return fuelTune;
            }
        }

        public void SetTune(VehicleUpgradeType type, int value)
        {
            switch (type)
            {
                case VehicleUpgradeType.Engine: engineTune = value; break;
                case VehicleUpgradeType.Acceleration: accelerationTune = value; break;
                case VehicleUpgradeType.Suspension: suspensionTune = value; break;
                case VehicleUpgradeType.Grip: gripTune = value; break;
                case VehicleUpgradeType.AirControl: airControlTune = value; break;
                case VehicleUpgradeType.Downforce: downforceTune = value; break;
                case VehicleUpgradeType.Turbo: turboTune = value; break;
                default: fuelTune = value; break;
            }
        }

        public void ClampTo(VehicleDefinition definition)
        {
            if (definition == null) return;
            engineTune = ClampLevel(engineTune, definition.engineMaxTunes);
            accelerationTune = ClampLevel(accelerationTune, definition.accelerationMaxTunes);
            suspensionTune = ClampLevel(suspensionTune, definition.suspensionMaxTunes);
            gripTune = ClampLevel(gripTune, definition.gripMaxTunes);
            airControlTune = ClampLevel(airControlTune, definition.airControlMaxTunes);
            downforceTune = ClampLevel(downforceTune, definition.downforceMaxTunes);
            fuelTune = ClampLevel(fuelTune, definition.fuelMaxTunes);
            turboTune = ClampLevel(turboTune, definition.turboMaxTunes);
        }

        private static int ClampLevel(int level, int maximum) =>
            maximum <= 0 ? 0 : Mathf.Clamp(level, 1, maximum);

        public void CopyFrom(VehicleUpgradeState other)
        {
            if (other == null)
                return;
            engineTune = other.engineTune;
            accelerationTune = other.accelerationTune;
            suspensionTune = other.suspensionTune;
            gripTune = other.gripTune;
            airControlTune = other.airControlTune;
            downforceTune = other.downforceTune;
            fuelTune = other.fuelTune;
            turboTune = other.turboTune;
        }
    }
}
