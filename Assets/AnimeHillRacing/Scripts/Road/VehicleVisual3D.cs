using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Road
{
    public sealed class VehicleVisual3D : MonoBehaviour
    {
        [SerializeField] private RoadVehicleController vehicle;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private bool[] frontWheels;
        [SerializeField, Min(0.05f)] private float wheelRadius = 0.43f;
        [SerializeField] private Vector3 localSpinAxis = Vector3.up;
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private SpriteRenderer[] bodyRenderers;
        [SerializeField] private SpriteRenderer[] wheelRenderers;
        [SerializeField] private ArticulatedDriver2D articulatedDriver;
        [SerializeField] private DriverDefinition fallbackDriver;

        private Quaternion[] baseRotations;
        private Vector3[] baseLocalPositions;
        private float spinAngle;

        private void Awake()
        {
            if (wheels == null) return;
            baseRotations = new Quaternion[wheels.Length];
            baseLocalPositions = new Vector3[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i] != null)
                {
                    baseRotations[i] = wheels[i].localRotation;
                    baseLocalPositions[i] = wheels[i].localPosition;
                }
        }

        private void OnEnable() => ApplySavedCustomization();

        public void ApplySavedCustomization()
        {
            if (definition == null) return;
            VehicleDefinition.CustomizationOption skin = definition.FindSkin(SaveManager.GetSelectedSkinId(definition));
            VehicleDefinition.CustomizationOption wheel = definition.FindWheel(SaveManager.GetSelectedWheelId(definition));
            int wheelSortingOrder = FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin);
            if (bodyRenderers != null && skin != null)
                for (int i = 0; i < bodyRenderers.Length; i++) if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].sprite = skin.sprite;
                    bodyRenderers[i].flipX = definition.flipBodyX ^ (i == 1);
                    bodyRenderers[i].transform.localPosition = definition.BodyCustomizationOffset(skin);
                    bodyRenderers[i].transform.localScale = definition.BodyCustomizationScale(skin);
                    FormulaElectricSkinEffect.Apply(bodyRenderers[i], definition, skin);
                    HillAdventurerSkinEffect.Apply(bodyRenderers[i], definition, skin);
                }
            if (wheelRenderers != null && wheel != null)
                for (int i = 0; i < wheelRenderers.Length; i++) if (wheelRenderers[i] != null)
                {
                    SpriteRenderer renderer = wheelRenderers[i];
                    renderer.sprite = wheel.sprite;
                    renderer.sortingOrder = wheelSortingOrder;
                    bool front = frontWheels != null && i < frontWheels.Length && frontWheels[i];
                    float diameter = front ? definition.FrontWheelDiameter : definition.RearWheelDiameter;
                    renderer.transform.localScale = Vector3.one *
                        definition.WheelCustomizationScale(wheel, diameter);
                }
            if (articulatedDriver != null && fallbackDriver != null)
                articulatedDriver.Apply(fallbackDriver, definition.driverPose, GetComponentInParent<Rigidbody>(), true);
        }

        private void LateUpdate()
        {
            if (vehicle == null || wheels == null) return;
            if (baseRotations == null || baseRotations.Length != wheels.Length ||
                baseLocalPositions == null || baseLocalPositions.Length != wheels.Length) Awake();
            spinAngle += vehicle.ForwardVelocity / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
            Quaternion spin = Quaternion.AngleAxis(spinAngle,
                localSpinAxis.sqrMagnitude > 0.001f ? localSpinAxis.normalized : Vector3.up);
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] == null) continue;
                float steering = frontWheels != null && i < frontWheels.Length && frontWheels[i]
                    ? vehicle.ActualSteeringAngle : 0f;
                Vector3 localPosition = baseLocalPositions[i];
                bool isFront = frontWheels != null && i < frontWheels.Length && frontWheels[i];
                bool isRight = localPosition.x > 0f;
                int physicsWheelIndex = (isFront ? 2 : 0) + (isRight ? 1 : 0);
                localPosition.y = -vehicle.GetWheelSuspensionLength(physicsWheelIndex);
                HcrGlitchState glitch = HcrGlitchSystem.Evaluate(vehicle.ProgressPosition);
                if (glitch.isActive && vehicle.AnyGrounded)
                    localPosition.y -= glitch.sinkDepth;
                wheels[i].localPosition = localPosition;
                wheels[i].localRotation = Quaternion.AngleAxis(steering, Vector3.up) * baseRotations[i] * spin;
            }
        }

        public void Assign(RoadVehicleController targetVehicle, Transform[] wheelVisuals, bool[] steerable,
            float radius)
        {
            vehicle = targetVehicle; wheels = wheelVisuals; frontWheels = steerable; wheelRadius = radius;
            localSpinAxis = Vector3.up;
        }

        public void AssignCustomization(VehicleDefinition vehicleDefinition, SpriteRenderer[] bodies,
            SpriteRenderer[] wheelImages, ArticulatedDriver2D driver = null, DriverDefinition defaultDriver = null)
        {
            definition = vehicleDefinition; bodyRenderers = bodies; wheelRenderers = wheelImages;
            articulatedDriver = driver; fallbackDriver = defaultDriver;
            ApplySavedCustomization();
        }

        public void ApplyCustomization(DriverDefinition driver)
        {
            if (driver != null) fallbackDriver = driver;
            ApplySavedCustomization();
        }

        public void Assign(RoadVehicleController targetVehicle, Transform[] wheelVisuals, bool[] steerable,
            float radius, Vector3 spinAxis)
        {
            vehicle = targetVehicle; wheels = wheelVisuals; frontWheels = steerable; wheelRadius = radius;
            localSpinAxis = spinAxis;
        }
    }
}
