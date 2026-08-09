using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public enum TransmissionMode
    {
        Auto,
        Manual
    }

    public enum ShiftQuality
    {
        None,
        Auto,
        Perfect,
        Early,
        Late
    }

    public sealed class TransmissionController : MonoBehaviour
    {
        private VehicleDefinition definition;
        private float shiftRemaining;
        private float shiftTorqueScale = 1f;
        private float statusRemaining;
        private int targetGear = 1;

        public TransmissionMode Mode { get; private set; } = TransmissionMode.Auto;
        public int CurrentGear { get; private set; } = 1;
        public int GearCount => definition != null ? definition.transmission.forwardGearRatios.Length : 7;
        public float Rpm { get; private set; } = 1100f;
        public bool IsShifting => shiftRemaining > 0f;
        public float TorqueScale => IsShifting ? shiftTorqueScale : 1f;
        public ShiftQuality LastShiftQuality { get; private set; }
        public string ShiftStatus => statusRemaining > 0f && LastShiftQuality != ShiftQuality.None
            ? LastShiftQuality.ToString().ToUpperInvariant()
            : string.Empty;

        public void Initialize(VehicleDefinition vehicleDefinition, TransmissionMode mode)
        {
            definition = vehicleDefinition;
            Mode = mode;
            CurrentGear = 1;
            targetGear = 1;
            shiftRemaining = 0f;
        }

        public void SetMode(TransmissionMode mode) => Mode = mode;

        public void Tick(float deltaTime, float drivenSpeedMps, float throttle, bool grounded, bool manualShiftRequest)
        {
            if (definition == null)
                return;

            if (shiftRemaining > 0f)
            {
                shiftRemaining -= deltaTime;
                if (shiftRemaining <= 0f)
                    CurrentGear = targetGear;
            }

            statusRemaining = Mathf.Max(0f, statusRemaining - deltaTime);
            float ratio = definition.transmission.forwardGearRatios[Mathf.Clamp(CurrentGear - 1, 0, GearCount - 1)];
            float wheelRpm = Mathf.Abs(drivenSpeedMps) / (2f * Mathf.PI * definition.wheelRadius) * 60f;
            float coupledRpm = wheelRpm * ratio * definition.transmission.finalDrive;
            float freeRev = Mathf.Lerp(definition.engine.idleRpm, definition.engine.redlineRpm, throttle * 0.38f);
            Rpm = Mathf.Clamp(grounded ? Mathf.Max(coupledRpm, freeRev) : Mathf.Lerp(Rpm, freeRev, deltaTime * 2f),
                definition.engine.idleRpm, definition.engine.limiterRpm);

            if (IsShifting)
                return;

            if (Mode == TransmissionMode.Auto)
                TickAuto(throttle, drivenSpeedMps);
            else if (manualShiftRequest)
                RequestManualShift(1);
        }

        public bool RequestShift(int direction)
        {
            if (Mode != TransmissionMode.Manual || IsShifting || direction == 0)
                return false;
            int oldGear = CurrentGear;
            RequestManualShift(direction > 0 ? 1 : -1);
            return targetGear != oldGear;
        }

        private void TickAuto(float throttle, float speedMps)
        {
            VehicleDefinition.TransmissionSettings settings = definition.transmission;
            float loadBias = Mathf.Lerp(-250f, 250f, throttle);
            if (CurrentGear < GearCount && Rpm >= settings.autoUpshiftRpm + loadBias && speedMps > 1f)
                BeginShift(CurrentGear + 1, settings.autoShiftDuration, settings.autoTorqueDuringShift, ShiftQuality.Auto);
            else if (CurrentGear > 1 && Rpm < settings.autoDownshiftRpm && throttle > 0.12f)
                BeginShift(CurrentGear - 1, settings.autoShiftDuration, settings.autoTorqueDuringShift, ShiftQuality.Auto);
        }

        private void RequestManualShift(int direction)
        {
            int nextGear = Mathf.Clamp(CurrentGear + direction, 1, GearCount);
            if (nextGear == CurrentGear)
                return;

            VehicleDefinition.TransmissionSettings settings = definition.transmission;
            ShiftQuality quality;
            float duration;
            float torqueScale;
            if (direction > 0 && Rpm >= settings.perfectRpmWindow.x && Rpm <= settings.perfectRpmWindow.y)
            {
                quality = ShiftQuality.Perfect;
                duration = settings.perfectShiftDuration;
                torqueScale = settings.perfectTorqueDuringShift;
            }
            else
            {
                quality = direction > 0 && Rpm > settings.perfectRpmWindow.y ? ShiftQuality.Late : ShiftQuality.Early;
                duration = settings.badShiftDuration;
                torqueScale = settings.badTorqueDuringShift;
            }

            BeginShift(nextGear, duration, torqueScale, quality);
        }

        private void BeginShift(int nextGear, float duration, float torqueScale, ShiftQuality quality)
        {
            targetGear = nextGear;
            shiftRemaining = duration;
            shiftTorqueScale = torqueScale;
            LastShiftQuality = quality;
            statusRemaining = 1.2f;
        }

        public float CurrentRatio
        {
            get
            {
                if (definition == null)
                    return 0f;
                return definition.transmission.forwardGearRatios[Mathf.Clamp(CurrentGear - 1, 0, GearCount - 1)];
            }
        }
    }
}
