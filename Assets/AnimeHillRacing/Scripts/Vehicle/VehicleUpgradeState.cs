using System;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [Serializable]
    public sealed class VehicleUpgradeState
    {
        [Min(0)] public int engineTune;
        [Min(0)] public int accelerationTune;
        [Min(0)] public int suspensionTune;
        [Min(0)] public int gripTune;
        [Min(0)] public int airControlTune;
        [Min(0)] public int downforceTune;
        [Min(0)] public int fuelTune;
        [Min(0)] public int turboTune;

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
            maximum <= 0 ? 0 : Mathf.Clamp(level, 0, maximum);

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
