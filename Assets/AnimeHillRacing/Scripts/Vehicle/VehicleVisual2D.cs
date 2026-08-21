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
                bodyRenderer.sortingOrder = 10;
                bodyRenderer.transform.localPosition = definition.BodyCustomizationOffset(skin);
                bodyRenderer.transform.localScale = definition.BodyCustomizationScale(skin);
            }
            FormulaElectricSkinEffect.Apply(bodyRenderer, definition, skin);
            HillAdventurerSkinEffect.Apply(bodyRenderer, definition, skin);
            int wheelSortingOrder = FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin);
            ApplyWheel(frontWheelRenderer, wheel, definition.FrontWheelDiameter, wheelSortingOrder);
            ApplyWheel(rearWheelRenderer, wheel, definition.RearWheelDiameter, wheelSortingOrder);
            if (articulatedDriver != null && fallbackDriver != null)
                ApplyDriverPhysicsBody();
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
                bodyRenderer.sortingOrder = 10;
                bodyRenderer.transform.localPosition = definition.BodyCustomizationOffset(skin);
                bodyRenderer.transform.localScale = definition.BodyCustomizationScale(skin);
            }
            FormulaElectricSkinEffect.Apply(bodyRenderer, definition, skin);
            HillAdventurerSkinEffect.Apply(bodyRenderer, definition, skin);
            int wheelSortingOrder = FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin);
            ApplyWheel(frontWheelRenderer, wheel, definition.FrontWheelDiameter, wheelSortingOrder);
            ApplyWheel(rearWheelRenderer, wheel, definition.RearWheelDiameter, wheelSortingOrder);
            if (articulatedDriver != null && fallbackDriver != null)
                ApplyDriverPhysicsBody();
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
        }

        private void ApplyWheel(SpriteRenderer renderer, VehicleDefinition.CustomizationOption option,
            float diameter, int sortingOrder)
        {
            if (renderer == null) return;
            renderer.sprite = option != null ? option.sprite : null;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localScale = Vector3.one *
                                            definition.WheelCustomizationScale(option, diameter);
        }
    }
}
