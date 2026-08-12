using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnimeHillRacing.Vehicle
{
    public enum VehicleArchetype
    {
        Jeep,
        Formula,
        Rally,
        SportBike
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Vehicle Definition", fileName = "MainVehicle")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        public const int GlobalGripMaximum = 1000;
        public const float UpgradeCurveK = 2.5f;

        [Serializable]
        public sealed class CustomizationOption
        {
            public string optionId;
            public string displayName;
            public Sprite sprite;
            public bool unlockedByDefault = true;
            [Min(0)] public int gemPrice;
            [Min(0f)] public float visibleDiameter;
        }

        [Serializable]
        public struct DriverPartPose
        {
            public Vector2 position;
            public float rotation;
            public Vector2 scale;
            public bool visible;
            public int sortingOrder;

            public static DriverPartPose Create(float x, float y, float rotationDegrees, float scaleX,
                float scaleY, bool isVisible, int order)
            {
                return new DriverPartPose
                {
                    position = new Vector2(x, y), rotation = rotationDegrees,
                    scale = new Vector2(scaleX, scaleY), visible = isVisible, sortingOrder = order
                };
            }
        }

        [Serializable]
        public sealed class DriverPose
        {
            public Vector2 rootPosition;
            public float rootScale = 1f;
            public DriverPartPose torso;
            public DriverPartPose head;
            public DriverPartPose leftUpperArm;
            public DriverPartPose leftForearm;
            public DriverPartPose rightUpperArm;
            public DriverPartPose rightForearm;
            public DriverPartPose leftThigh;
            public DriverPartPose leftLowerLeg;
            public DriverPartPose rightThigh;
            public DriverPartPose rightLowerLeg;
        }

        [Header("Identity")]
        public string vehicleId = "car-01";
        public string displayName = "JEEP";
        public VehicleArchetype archetype = VehicleArchetype.Jeep;
        public GameObject gameplayPrefab2D;
        public GameObject gameplayPrefab3D;
        public Sprite previewSprite;

        [Header("Garage / Customize")]
        public CustomizationOption[] skins;
        public CustomizationOption[] wheels;
        public DriverPose driverPose = new DriverPose();
        [Min(.1f)] public float visualWidth = 3.2f;
        public bool flipBodyX;
        public Vector2 bodyVisualOffset = new Vector2(0f, .1f);
        public Vector2 rearWheelVisualPosition = new Vector2(-1.05f, -.42f);
        public Vector2 frontWheelVisualPosition = new Vector2(1.05f, -.42f);
        [Min(.05f)] public float wheelVisualDiameter = .86f;
        [Min(0f)] public float rearWheelVisualDiameter;
        [Min(0f)] public float frontWheelVisualDiameter;

        [Header("Per-Vehicle Tune Limits")]
        [Min(0)] public int engineMaxTunes = 14;
        [Min(0)] public int accelerationMaxTunes = 15;
        [Min(0)] public int suspensionMaxTunes = 12;
        [Min(0)] public int gripMaxTunes = 12;
        [Min(0)] public int airControlMaxTunes;
        [Min(0)] public int downforceMaxTunes;
        [Min(0)] public int fuelMaxTunes = 15;
        [Min(0)] public int turboMaxTunes;

        [Header("Damage State (optional)")]
        public Sprite damagedBodySprite;
        public Sprite detachableRoofSprite;

        [Serializable]
        public sealed class TuneTargets
        {
            public float[] engineTopSpeedKmh =
                { 135, 138, 141, 144, 147, 150, 153, 156, 160, 164, 168, 172, 175, 178, 180 };
            public float[] accelerationZeroToHundredSeconds =
                { 9.5f, 9.2f, 8.9f, 8.6f, 8.3f, 8.0f, 7.7f, 7.4f, 7.1f, 6.8f, 6.5f, 6.2f, 5.9f, 5.6f, 5.4f, 5.2f };
            public float[] gripRatings =
                { 80f, 120f, 160f, 200f, 240f, 280f, 320f, 355f, 390f, 420f, 450f, 478f, 500f };
            public float[] airControlTorque = { 3200f };
            public float[] airborneDownforceAcceleration = { 0f };
            [FormerlySerializedAs("fuelFullThrottleSeconds")]
            public float[] fuelRunDurationSeconds =
                { 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 };
        }

        [Serializable]
        public sealed class EngineSettings
        {
            [Min(1000f)] public float idleRpm = 1100f;
            [Min(2000f)] public float redlineRpm = 7200f;
            [Min(2000f)] public float limiterRpm = 7600f;
            public AnimationCurve torqueCurve = new AnimationCurve(
                new Keyframe(0f, 0.58f), new Keyframe(0.35f, 0.9f),
                new Keyframe(0.7f, 1f), new Keyframe(1f, 0.72f));
            [Tooltip("Soft arcade torque falloff as vehicle speed approaches its tuned top speed.")]
            public AnimationCurve speedTorqueCurve = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(.55f, 1f),
                new Keyframe(.82f, .68f), new Keyframe(.96f, .22f),
                new Keyframe(1.04f, 0f));
            [Min(1f)] public float minTorqueNm = 180f;
            [Min(1f)] public float maxTorqueNm = 300f;
            [Range(0.1f, 1f)] public float drivetrainEfficiency = 0.78f;
        }

        [Serializable]
        public sealed class TransmissionSettings
        {
            public float[] forwardGearRatios = { 4.80f, 3.55f, 2.80f, 2.35f, 2.05f, 1.82f, 1.68f };
            [Min(0.1f)] public float reverseRatio = 3f;
            [Min(0.1f)] public float finalDrive = 3.35f;
            [Min(0.05f)] public float autoShiftDuration = 0.37f;
            [Range(0f, 1f)] public float autoTorqueDuringShift = 0.12f;
            [Min(1000f)] public float autoUpshiftRpm = 6500f;
            [Min(1000f)] public float autoDownshiftRpm = 2850f;
            [Min(0.05f)] public float perfectShiftDuration = 0.18f;
            [Min(0.05f)] public float badShiftDuration = 0.42f;
            [Range(0f, 1f)] public float perfectTorqueDuringShift = 0.42f;
            [Range(0f, 1f)] public float badTorqueDuringShift = 0.04f;
            public Vector2 perfectRpmWindow = new Vector2(6100f, 6900f);
        }

        [Serializable]
        public sealed class SuspensionSettings
        {
            [Min(0.1f)] public float restLength = 0.55f;
            [Tooltip("Fraction of total suspension travel reserved for compression. The remainder is extension/droop.")]
            [Range(0.6f, 0.95f)] public float compressionTravelRatio = 0.9f;
            [Min(0.05f)] public float minTravel = 0.12f;
            [Min(0.05f)] public float maxTravel = 0.28f;
            [Min(100f)] public float springMin = 26000f;
            [Min(100f)] public float springMax = 38000f;
            [Min(100f)] public float dampingMin = 3600f;
            [Min(100f)] public float dampingMax = 5200f;
            [Tooltip("Sag measured from full droop. The common spring curve solves stiffness from this target.")]
            [Range(.25f, .35f)] public float targetSag01 = .30f;
            [Tooltip("Level 1 / maximum-level stiffness scale around the sag-derived base spring.")]
            public Vector2 stiffnessTuneScale = new Vector2(.94f, 1.06f);
            [Tooltip("Level 1 / maximum-level damping ratio for the sprung mass at one wheel.")]
            public Vector2 dampingRatio = new Vector2(.82f, .96f);
            [Tooltip("Normalized compression at which the progressive bump stop begins to add resistance.")]
            [Range(0.5f, 0.9f)] public float bumpStopStart = 0.7f;
            [Tooltip("Additional progressive spring authority near maximum compression.")]
            [Min(0f)] public float progressiveSpringMultiplier = 2.5f;
            [Tooltip("Additional force per metre after the hard compression limit has been crossed.")]
            [Min(0f)] public float hardStopSpringMultiplier = 8f;
            [Min(1f)] public float maximumAcceleration = 34f;
            [Tooltip("Maximum net upward acceleration allowed while the suspension recovers. This keeps the bump stop from becoming a catapult.")]
            [Range(.5f, 6f)] public float maximumReboundAcceleration = 2.5f;
        }

        [Serializable]
        public sealed class GripSettings
        {
            [Min(0.1f)] public float minGrip = 1.05f;
            [Min(0.1f)] public float maxGrip = 1.75f;
            [Range(.02f, 1f)] public float slipThreshold = .18f;
            [Range(.1f, 1f)] public float slidingGripMultiplier = .72f;
            [Min(.1f)] public float tractionRecoveryPerSecond = 5.5f;
            [Min(0f)] public float rollingResistance = 0.018f;
            [Min(0f)] public float aerodynamicDrag = 0.46f;
        }

        [Serializable]
        public sealed class FuelSettings
        {
            [Min(1f)] public float capacityUnits = 100f;
            [FormerlySerializedAs("minimumFullThrottleDurationSeconds"), Min(1f)]
            public float minimumRunDurationSeconds = 45f;
            [FormerlySerializedAs("maximumFullThrottleDurationSeconds"), Min(1f)]
            public float maximumRunDurationSeconds = 60f;
            [FormerlySerializedAs("referenceFullThrottleDurationSeconds"), Min(1f)]
            public float referenceRunDurationSeconds = 60f;
        }

        [Header("Physical")]
        [Min(50f)] public float mass = 1150f;
        [Tooltip("Unsprung mass of each independent wheel rigidbody.")]
        [Min(1f)] public float wheelMass = 42f;
        [Min(0.05f)] public float wheelRadius = 0.43f;
        [Min(0.1f)] public float halfWheelbase = 1.05f;
        [Min(0.1f)] public float halfTrack = 0.78f;
        [Min(1f)] public float topSpeedMps = 50f;
        [Min(0f)] public float brakeForce = 12500f;
        [Min(0f)] public float reverseForce = 4500f;
        [Min(0f)] public float airControlTorque = 3200f;
        [Header("Aerodynamics (Formula)")]
        [Min(1f)] public float downforceReferenceSpeedMps = 30f;
        [Min(0f)] public float maximumDownforceAcceleration = 14f;
        [Range(1f, 6f)] public float collisionPitchInertiaMultiplier = 1.55f;
        [Tooltip("Caps PhysX penetration correction so a chassis contact cannot catapult the vehicle.")]
        [Min(.5f)] public float maximumDepenetrationVelocity = 3f;
        [Header("Air Rotation Control")]
        [Min(.1f)] public float maxAirAngularVelocity = 2.5f;
        [Min(.1f)] public float airAngularAcceleration = 14f;
        [Min(0f)] public float airAngularDamping = 1.8f;
        [Range(0.5f, 2f)] public float accelerationForceCalibration = 1f;
        public Vector3 centerOfMass = new Vector3(0f, -0.25f, 0f);

        [Header("Systems")]
        public EngineSettings engine = new EngineSettings();
        public TransmissionSettings transmission = new TransmissionSettings();
        public SuspensionSettings suspension = new SuspensionSettings();
        public GripSettings grip = new GripSettings();
        public FuelSettings fuel = new FuelSettings();
        public TuneTargets tuneTargets = new TuneTargets();

        public float RearWheelAnchor => rearWheelVisualPosition.x;
        public float FrontWheelAnchor => frontWheelVisualPosition.x;
        public float RearWheelDiameter => rearWheelVisualDiameter > .001f
            ? rearWheelVisualDiameter : wheelVisualDiameter;
        public float FrontWheelDiameter => frontWheelVisualDiameter > .001f
            ? frontWheelVisualDiameter : wheelVisualDiameter;

        public static float WheelSourceDiameter(CustomizationOption option)
        {
            if (option != null && option.visibleDiameter > .001f) return option.visibleDiameter;
            Sprite sprite = option != null ? option.sprite : null;
            return sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        }

        public ResolvedVehicleStats Resolve(VehicleUpgradeState upgrades)
        {
            upgrades ??= new VehicleUpgradeState();
            upgrades.ClampTo(this);
            float engineLevel = UpgradeProgress(upgrades.engineTune, engineMaxTunes);
            float accelerationLevel = UpgradeProgress(upgrades.accelerationTune, accelerationMaxTunes);
            float turboLevel = UpgradeProgress(upgrades.turboTune, turboMaxTunes);
            float suspensionQuality = UpgradeProgress(upgrades.suspensionTune, suspensionMaxTunes);
            float gripProgress = UpgradeProgress(upgrades.gripTune, gripMaxTunes);
            float airControl = TargetValue(tuneTargets?.airControlTorque,
                UpgradeProgress(upgrades.airControlTune, airControlMaxTunes), airControlTorque);
            float downforceProgress = UpgradeProgress(upgrades.downforceTune, downforceMaxTunes);
            float airborneDownforce = TargetValue(tuneTargets?.airborneDownforceAcceleration,
                downforceProgress, 0f);
            float topSpeed = TargetValue(tuneTargets?.engineTopSpeedKmh, engineLevel, 135f) / 3.6f;
            float accelerationTarget = TargetValue(tuneTargets?.accelerationZeroToHundredSeconds,
                archetype == VehicleArchetype.Rally ? turboLevel : accelerationLevel, 9.5f);
            float suspensionTravel = Mathf.Lerp(suspension.minTravel, suspension.maxTravel, suspensionQuality);
            SuspensionPhysics.GetTravelLimits(suspension, suspensionTravel,
                out _, out float suspensionExtension);
            float targetCompression = Mathf.Max(.015f,
                suspension.targetSag01 * suspensionTravel - suspensionExtension);
            float sprungMassPerWheel = mass * .5f;
            float sagDerivedSpring = sprungMassPerWheel * Physics.gravity.magnitude / targetCompression;
            float springScale = Mathf.Lerp(suspension.stiffnessTuneScale.x,
                suspension.stiffnessTuneScale.y, suspensionQuality);
            float resolvedSpring = sagDerivedSpring * springScale;
            float dampingRatio = Mathf.Lerp(suspension.dampingRatio.x,
                suspension.dampingRatio.y, suspensionQuality);
            float resolvedDamping = 2f * Mathf.Sqrt(resolvedSpring * sprungMassPerWheel) * dampingRatio;
            float gripRating = GripPointValue(tuneTargets?.gripRatings, gripProgress);
            float normalizedGrip = Mathf.Clamp01(gripRating / GlobalGripMaximum);
            float physicalGrip = Mathf.Lerp(grip.minGrip, grip.maxGrip,
                PhysicalGripAuthority(normalizedGrip));
            float fuelDuration = TargetValue(tuneTargets?.fuelRunDurationSeconds,
                UpgradeProgress(upgrades.fuelTune, fuelMaxTunes), 45f);
            float accelerationAuthority = archetype == VehicleArchetype.Rally ? turboLevel : accelerationLevel;
            float torque = Mathf.Lerp(engine.minTorqueNm, engine.maxTorqueNm, accelerationAuthority) *
                           Mathf.Lerp(.92f, 1f, engineLevel);

            return new ResolvedVehicleStats
            {
                motorTorqueNm = torque,
                peakPowerKw = torque * 6200f / 9549f,
                zeroToHundredTargetSeconds = accelerationTarget,
                targetAccelerationMps2 = (100f / 3.6f) / Mathf.Max(0.5f, accelerationTarget) *
                                         accelerationForceCalibration,
                lowSpeedTorqueMultiplier = Mathf.Lerp(0.82f, 1.15f, accelerationAuthority),
                shiftRecovery = Mathf.Lerp(0f, 0.30f, accelerationAuthority),
                suspensionTravel = suspensionTravel,
                spring = resolvedSpring,
                damping = resolvedDamping,
                progressiveSpringMultiplier = suspension.progressiveSpringMultiplier *
                                               Mathf.Lerp(.90f, 1.10f, suspensionQuality),
                grip = physicalGrip,
                gripRating = Mathf.RoundToInt(gripRating),
                normalizedGrip = normalizedGrip,
                slipThreshold = grip.slipThreshold * Mathf.Lerp(1.08f, .86f,
                    normalizedGrip),
                slidingGripMultiplier = grip.slidingGripMultiplier,
                tractionRecoveryPerSecond = grip.tractionRecoveryPerSecond *
                                            Mathf.Lerp(.82f, 1.18f, normalizedGrip),
                totalRunFuelDuration = fuelDuration,
                airControlTorque = archetype == VehicleArchetype.Rally
                    ? airControl * Mathf.Lerp(1f, 1.12f, turboLevel) : airControl,
                airborneDownforceAcceleration = airborneDownforce,
                topSpeedMps = topSpeed
            };
        }

        public int GetMaxTunes(VehicleUpgradeType type)
        {
            switch (type)
            {
                case VehicleUpgradeType.Engine: return engineMaxTunes;
                case VehicleUpgradeType.Acceleration: return accelerationMaxTunes;
                case VehicleUpgradeType.Suspension: return suspensionMaxTunes;
                case VehicleUpgradeType.Grip: return gripMaxTunes;
                case VehicleUpgradeType.AirControl: return airControlMaxTunes;
                case VehicleUpgradeType.Downforce: return downforceMaxTunes;
                case VehicleUpgradeType.Turbo: return turboMaxTunes;
                default: return fuelMaxTunes;
            }
        }

        public VehicleUpgradeType GetTuneTypeForSlot(int slot)
        {
            if (slot == 0) return VehicleUpgradeType.Engine;
            if (slot == 1) return archetype == VehicleArchetype.Rally
                ? VehicleUpgradeType.Turbo : VehicleUpgradeType.Acceleration;
            if (slot == 2) return VehicleUpgradeType.Suspension;
            if (slot == 3) return VehicleUpgradeType.Grip;
            if (slot == 5 && archetype == VehicleArchetype.Formula) return VehicleUpgradeType.Downforce;
            return VehicleUpgradeType.Fuel;
        }

        public int GetTuneSlotCount() => archetype == VehicleArchetype.Formula ? 6 : 5;

        public string GetTuneDisplayName(VehicleUpgradeType type)
        {
            switch (type)
            {
                case VehicleUpgradeType.Engine: return "SPEED";
                case VehicleUpgradeType.Acceleration: return "ACCELERATION";
                case VehicleUpgradeType.Suspension: return "SUSPENSION";
                case VehicleUpgradeType.Grip: return "GRIP";
                case VehicleUpgradeType.AirControl: return "AIR CONTROL";
                case VehicleUpgradeType.Downforce: return "DOWNFORCE";
                case VehicleUpgradeType.Turbo: return "ACCELERATION";
                default: return "FUEL";
            }
        }

        public static float UpgradeProgress(int level, int maximum)
        {
            if (maximum <= 1 || level <= 1) return 0f;
            if (level >= maximum) return 1f;
            float t = Mathf.Clamp01((level - 1f) / (maximum - 1f));
            float denominator = 1f - Mathf.Exp(-UpgradeCurveK);
            return denominator <= Mathf.Epsilon ? t : (1f - Mathf.Exp(-UpgradeCurveK * t)) / denominator;
        }

        private static float TargetValue(float[] values, float progress, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            if (values.Length == 1) return values[0];
            return Mathf.Lerp(values[0], values[values.Length - 1], Mathf.Clamp01(progress));
        }

        private static float GripPointValue(float[] values, float progress)
        {
            float value = TargetValue(values, progress, 100f);
            // Migrates recovered assets that still contain the previous .65-.95 rating scale in memory.
            if (value <= 2f)
                value = Mathf.InverseLerp(.65f, .95f, value) * GlobalGripMaximum;
            return Mathf.Clamp(value, 0f, GlobalGripMaximum);
        }

        private static float PhysicalGripAuthority(float normalizedRating)
        {
            // The 0-1000 rating remains global, while this authored response leaves Grip 200 weak
            // and gives the finite vehicle-specific caps enough authority to reach their top speed.
            float n = Mathf.Clamp01(normalizedRating);
            const float a200 = .11747573f; // .382 coefficient when min/max are .14/2.20
            const float a300 = .6116505f;  // 1.40
            const float a500 = .8543689f;  // 1.90
            const float a750 = .9757282f;  // 2.15
            if (n <= .2f) return Mathf.Lerp(0f, a200, n / .2f);
            if (n <= .3f) return Mathf.Lerp(a200, a300,
                Mathf.SmoothStep(0f, 1f, (n - .2f) / .1f));
            if (n <= .5f) return Mathf.Lerp(a300, a500,
                Mathf.SmoothStep(0f, 1f, (n - .3f) / .2f));
            if (n <= .75f) return Mathf.Lerp(a500, a750,
                Mathf.SmoothStep(0f, 1f, (n - .5f) / .25f));
            return Mathf.Lerp(a750, 1f, Mathf.SmoothStep(0f, 1f, (n - .75f) / .25f));
        }

        private void OnValidate()
        {
            if (transmission.forwardGearRatios == null || transmission.forwardGearRatios.Length != 7)
                transmission.forwardGearRatios = new[] { 4.80f, 3.55f, 2.80f, 2.35f, 2.05f, 1.82f, 1.68f };
            tuneTargets ??= new TuneTargets();
            engineMaxTunes = Mathf.Max(0, engineMaxTunes);
            accelerationMaxTunes = Mathf.Max(0, accelerationMaxTunes);
            suspensionMaxTunes = Mathf.Max(0, suspensionMaxTunes);
            gripMaxTunes = Mathf.Max(0, gripMaxTunes);
            airControlMaxTunes = Mathf.Max(0, airControlMaxTunes);
            downforceMaxTunes = Mathf.Max(0, downforceMaxTunes);
            fuelMaxTunes = Mathf.Max(0, fuelMaxTunes);
            turboMaxTunes = Mathf.Max(0, turboMaxTunes);
            visualWidth = Mathf.Max(.1f, visualWidth);
            wheelVisualDiameter = Mathf.Max(.05f, wheelVisualDiameter);
            rearWheelVisualDiameter = Mathf.Max(0f, rearWheelVisualDiameter);
            frontWheelVisualDiameter = Mathf.Max(0f, frontWheelVisualDiameter);
            wheelMass = Mathf.Max(1f, wheelMass);
            collisionPitchInertiaMultiplier = Mathf.Clamp(collisionPitchInertiaMultiplier, 1f, 6f);
            suspension.compressionTravelRatio = Mathf.Clamp(suspension.compressionTravelRatio, .6f, .95f);
            suspension.targetSag01 = Mathf.Clamp(suspension.targetSag01, .25f, .35f);
            suspension.stiffnessTuneScale.x = Mathf.Clamp(suspension.stiffnessTuneScale.x, .75f, 1.25f);
            suspension.stiffnessTuneScale.y = Mathf.Clamp(suspension.stiffnessTuneScale.y,
                suspension.stiffnessTuneScale.x, 1.35f);
            suspension.dampingRatio.x = Mathf.Clamp(suspension.dampingRatio.x, .55f, 1.25f);
            suspension.dampingRatio.y = Mathf.Clamp(suspension.dampingRatio.y, .55f, 1.35f);
            maximumDepenetrationVelocity = Mathf.Max(.5f, maximumDepenetrationVelocity);
            downforceReferenceSpeedMps = Mathf.Max(1f, downforceReferenceSpeedMps);
            maximumDownforceAcceleration = Mathf.Max(0f, maximumDownforceAcceleration);
            suspension.bumpStopStart = Mathf.Clamp(suspension.bumpStopStart, .5f, .9f);
            suspension.progressiveSpringMultiplier = Mathf.Max(0f, suspension.progressiveSpringMultiplier);
            suspension.hardStopSpringMultiplier = Mathf.Max(0f, suspension.hardStopSpringMultiplier);
            suspension.maximumReboundAcceleration = Mathf.Clamp(suspension.maximumReboundAcceleration, .5f, 6f);
            grip.slipThreshold = Mathf.Clamp(grip.slipThreshold, .02f, 1f);
            grip.slidingGripMultiplier = Mathf.Clamp(grip.slidingGripMultiplier, .1f, 1f);
            grip.tractionRecoveryPerSecond = Mathf.Max(.1f, grip.tractionRecoveryPerSecond);
            if (engine.speedTorqueCurve == null || engine.speedTorqueCurve.length == 0)
                engine.speedTorqueCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.55f, 1f),
                    new Keyframe(.82f, .68f), new Keyframe(.96f, .22f), new Keyframe(1.04f, 0f));
        }

        public CustomizationOption FindSkin(string optionId) => FindOption(skins, optionId);
        public CustomizationOption FindWheel(string optionId) => FindOption(wheels, optionId);

        public string DefaultSkinId => FirstOptionId(skins);
        public string DefaultWheelId => FirstOptionId(wheels);

        private static CustomizationOption FindOption(CustomizationOption[] options, string optionId)
        {
            if (options == null || options.Length == 0) return null;
            for (int i = 0; i < options.Length; i++)
                if (options[i] != null && options[i].optionId == optionId) return options[i];
            return options[0];
        }

        private static string FirstOptionId(CustomizationOption[] options) =>
            options != null && options.Length > 0 && options[0] != null ? options[0].optionId : string.Empty;
    }

    [Serializable]
    public struct ResolvedVehicleStats
    {
        public float motorTorqueNm;
        public float peakPowerKw;
        public float zeroToHundredTargetSeconds;
        public float targetAccelerationMps2;
        public float lowSpeedTorqueMultiplier;
        public float shiftRecovery;
        public float suspensionTravel;
        public float spring;
        public float damping;
        public float progressiveSpringMultiplier;
        public float grip;
        public int gripRating;
        public float normalizedGrip;
        public float slipThreshold;
        public float slidingGripMultiplier;
        public float tractionRecoveryPerSecond;
        public float totalRunFuelDuration;
        public float airControlTorque;
        public float airborneDownforceAcceleration;
        public float topSpeedMps;
    }
}
