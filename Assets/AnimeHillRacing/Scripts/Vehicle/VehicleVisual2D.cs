using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class VehicleVisual2D : MonoBehaviour
    {
        public const int BodySortingOrder = 10;
        public const int MinimumWheelSortingOrder = 30;
        // Adventure 2.5D terrain fronts render at Z=-5. The vehicle must remain closer to the camera
        // so the complete road, including its yellow stripe, is visible without covering the tires.
        public const float VehicleForegroundZ = -6f;

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
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer FrontWheelRenderer => frontWheelRenderer;
        public SpriteRenderer RearWheelRenderer => rearWheelRenderer;

        private void OnEnable() => ApplySavedCustomization();

        public void ApplySavedCustomization()
        {
            if (definition == null) return;
            VehicleDefinition.CustomizationOption skin = definition.FindSkin(SaveManager.GetSelectedSkinId(definition));
            VehicleDefinition.CustomizationOption wheel = definition.FindWheel(SaveManager.GetSelectedWheelId(definition));
            if (bodyRenderer != null && skin != null)
            {
                bodyRenderer.sprite = skin.sprite;
                bodyRenderer.flipX = definition.flipBodyX;
                bodyRenderer.sortingOrder = BodySortingOrder;
                Vector3 bodyOffset = definition.BodyCustomizationOffset(skin);
                bodyOffset.z = VehicleForegroundZ;
                bodyRenderer.transform.localPosition = bodyOffset;
                bodyRenderer.transform.localScale = definition.BodyCustomizationScale(skin);
            }
            FormulaElectricSkinEffect.Apply(bodyRenderer, definition, skin);
            HillAdventurerSkinEffect.Apply(bodyRenderer, definition, skin);
            SportBikeNeonFlowEffect.ApplySkin(bodyRenderer, definition, skin);
            int wheelSortingOrder = Mathf.Max(MinimumWheelSortingOrder,
                FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin));
            ApplyWheel(frontWheelRenderer, wheel, definition.FrontWheelDiameter, wheelSortingOrder);
            ApplyWheel(rearWheelRenderer, wheel, definition.RearWheelDiameter, wheelSortingOrder);
            if (articulatedDriver != null)
            {
                if (fallbackDriver != null) ApplyDriverPhysicsBody();
                else articulatedDriver.EnforceRenderingRelativeToVehicle(bodyRenderer);
            }
        }

        public void ApplyCustomization(DriverDefinition driver)
        {
            if (driver != null) fallbackDriver = driver;
            ApplySavedCustomization();
        }

        public void ApplyCustomization(VehicleDefinition vehicleDefinition,
            VehicleDefinition.CustomizationOption skin, VehicleDefinition.CustomizationOption wheel,
            DriverDefinition driver = null)
        {
            definition = vehicleDefinition;
            if (driver != null) fallbackDriver = driver;
            if (definition == null) return;
            if (bodyRenderer != null && skin != null)
            {
                bodyRenderer.sprite = skin.sprite;
                bodyRenderer.flipX = definition.flipBodyX;
                bodyRenderer.sortingOrder = BodySortingOrder;
                Vector3 bodyOffset = definition.BodyCustomizationOffset(skin);
                bodyOffset.z = VehicleForegroundZ;
                bodyRenderer.transform.localPosition = bodyOffset;
                bodyRenderer.transform.localScale = definition.BodyCustomizationScale(skin);
            }
            FormulaElectricSkinEffect.Apply(bodyRenderer, definition, skin);
            HillAdventurerSkinEffect.Apply(bodyRenderer, definition, skin);
            SportBikeNeonFlowEffect.ApplySkin(bodyRenderer, definition, skin);
            int wheelSortingOrder = Mathf.Max(MinimumWheelSortingOrder,
                FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin));
            ApplyWheel(frontWheelRenderer, wheel, definition.FrontWheelDiameter, wheelSortingOrder);
            ApplyWheel(rearWheelRenderer, wheel, definition.RearWheelDiameter, wheelSortingOrder);
            if (articulatedDriver != null)
            {
                if (fallbackDriver != null) ApplyDriverPhysicsBody();
                else articulatedDriver.EnforceRenderingRelativeToVehicle(bodyRenderer);
            }
        }

        public void Assign(VehicleRuntimeBase vehicleController, Transform bodyTransform,
            Transform frontWheelTransform, Transform rearWheelTransform, Transform driverAnchorTransform,
            float radius)
        {
            vehicle = vehicleController;
            bodySprite = bodyTransform;
            frontWheelSprite = frontWheelTransform;
            rearWheelSprite = rearWheelTransform;
            driverAnchor = driverAnchorTransform;
            wheelRadius = radius;
        }

        public void Assign(VehicleDefinition vehicleDefinition, SpriteRenderer bodyImage,
            SpriteRenderer frontWheelImage, SpriteRenderer rearWheelImage, ArticulatedDriver2D driver,
            DriverDefinition defaultDriver = null)
        {
            definition = vehicleDefinition;
            bodyRenderer = bodyImage;
            frontWheelRenderer = frontWheelImage;
            rearWheelRenderer = rearWheelImage;
            articulatedDriver = driver;
            fallbackDriver = defaultDriver;
            ApplySavedCustomization();
        }

        public void AssignCustomization(VehicleDefinition vehicleDefinition, SpriteRenderer bodyImage,
            SpriteRenderer frontWheelImage, SpriteRenderer rearWheelImage, ArticulatedDriver2D driver,
            DriverDefinition defaultDriver = null)
        {
            definition = vehicleDefinition;
            bodyRenderer = bodyImage;
            frontWheelRenderer = frontWheelImage;
            rearWheelRenderer = rearWheelImage;
            articulatedDriver = driver;
            fallbackDriver = defaultDriver;
            ApplySavedCustomization();
        }

        private void ApplyDriverPhysicsBody()
        {
            Rigidbody body = vehicle != null ? vehicle.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>();
            Rigidbody2D body2D = vehicle != null ? vehicle.GetComponent<Rigidbody2D>() : GetComponentInParent<Rigidbody2D>();
            articulatedDriver.Apply(fallbackDriver, definition.driverPose, body, body2D, true);
            articulatedDriver.EnforceRenderingRelativeToVehicle(bodyRenderer);
        }

        private void ApplyWheel(SpriteRenderer renderer, VehicleDefinition.CustomizationOption option,
            float diameter, int sortingOrder)
        {
            if (renderer == null) return;
            renderer.sprite = option != null ? option.sprite : null;
            if (bodyRenderer != null) renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localScale = Vector3.one *
                                            definition.WheelCustomizationScale(option, diameter);
            SportBikeNeonFlowEffect.ApplyWheel(renderer, definition, option);
        }
    }
}
