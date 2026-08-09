using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class VehicleVisual2D : MonoBehaviour
    {
        [SerializeField] private VehicleRuntimeBase vehicle;
        [SerializeField] private VehicleDefinition definition;
        [SerializeField] private Transform bodySprite;
        [SerializeField] private Transform frontWheelSprite;
        [SerializeField] private Transform rearWheelSprite;
        [SerializeField] private Transform driverAnchor;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer frontWheelRenderer;
        [SerializeField] private SpriteRenderer rearWheelRenderer;
        [SerializeField] private ArticulatedDriver2D articulatedDriver;
        [SerializeField] private DriverDefinition fallbackDriver;
        [SerializeField, Min(0.05f)] private float wheelRadius = 0.43f;

        public Transform FrontWheelSprite => frontWheelSprite;
        public Transform RearWheelSprite => rearWheelSprite;
        public Transform DriverAnchor => driverAnchor;

        private void OnEnable() => ApplySavedCustomization();

        public void ApplySavedCustomization()
        {
            if (definition == null) return;
            VehicleDefinition.CustomizationOption skin = definition.FindSkin(SaveManager.GetSelectedSkinId(definition));
            VehicleDefinition.CustomizationOption wheel = definition.FindWheel(SaveManager.GetSelectedWheelId(definition));
            if (bodyRenderer != null && skin != null) bodyRenderer.sprite = skin.sprite;
            if (frontWheelRenderer != null && wheel != null) frontWheelRenderer.sprite = wheel.sprite;
            if (rearWheelRenderer != null && wheel != null) rearWheelRenderer.sprite = wheel.sprite;
            if (articulatedDriver != null && fallbackDriver != null)
                articulatedDriver.Apply(fallbackDriver, definition.driverPose,
                    vehicle != null ? vehicle.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>(), true);
        }

        public void ApplyCustomization(DriverDefinition driver)
        {
            if (driver != null) fallbackDriver = driver;
            ApplySavedCustomization();
        }

        private void LateUpdate()
        {
            // Physical 2.5D wheel pose is synchronized by each SuspensionController. Road-mode wheels have
            // their own VehicleVisual3D component, so no speed-derived visual rotation is needed here.
        }

        public void Assign(VehicleRuntimeBase targetVehicle, Transform body, Transform frontWheel,
            Transform rearWheel, Transform targetDriverAnchor, float radius)
        {
            vehicle = targetVehicle;
            bodySprite = body;
            frontWheelSprite = frontWheel;
            rearWheelSprite = rearWheel;
            driverAnchor = targetDriverAnchor;
            wheelRadius = radius;
        }

        public void AssignCustomization(VehicleDefinition vehicleDefinition, SpriteRenderer bodyImage,
            SpriteRenderer frontWheelImage, SpriteRenderer rearWheelImage, ArticulatedDriver2D driver,
            DriverDefinition defaultDriver)
        {
            definition = vehicleDefinition; bodyRenderer = bodyImage;
            frontWheelRenderer = frontWheelImage; rearWheelRenderer = rearWheelImage;
            articulatedDriver = driver; fallbackDriver = defaultDriver;
            ApplySavedCustomization();
        }
    }
}
