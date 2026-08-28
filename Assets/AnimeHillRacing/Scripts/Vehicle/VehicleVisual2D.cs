using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
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
        private const float SurfaceMaskWidth = 48f;
        private const float SurfaceMaskHeight = 96f;

        private static Sprite sharedSurfaceMaskSprite;

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

        private SpriteMask surfaceClipMask;
        private SpriteRenderer[] surfaceMaskedRenderers;
        private readonly Dictionary<SpriteRenderer, SpriteMaskInteraction> originalMaskInteractions =
            new Dictionary<SpriteRenderer, SpriteMaskInteraction>();

        public Transform FrontWheelSprite => frontWheelSprite;
        public Transform RearWheelSprite => rearWheelSprite;
        public Transform DriverAnchor => driverAnchor;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer FrontWheelRenderer => frontWheelRenderer;
        public SpriteRenderer RearWheelRenderer => rearWheelRenderer;

        private void OnEnable() => ApplySavedCustomization();

        private void OnDisable()
        {
            DisableSurfaceClipMask();
        }

        private void LateUpdate()
        {
            TerrainManager terrain = TerrainManager.Instance;
            MapProfile profile = terrain != null ? terrain.ActiveProfile : null;
            bool usesContactLineOcclusion = profile != null && profile.useVehicleContactLineOcclusion;
            if (!usesContactLineOcclusion)
            {
                DisableSurfaceClipMask();
                EnforceVehicleForegroundDepth();
                return;
            }

            Transform reference = vehicle != null ? vehicle.transform : transform;
            if (!terrain.TrySampleAuthoritativeSurface(reference.position.x,
                    out float surfaceY, out float slopeDegrees))
            {
                DisableSurfaceClipMask();
                EnforceVehicleForegroundDepth();
                return;
            }

            EnforceVehicleForegroundDepth();
            EnableSurfaceClipMask(reference.position.x, surfaceY, slopeDegrees);
        }

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
            EnforceVehicleForegroundDepth();
            if (articulatedDriver != null)
            {
                if (fallbackDriver != null) ApplyDriverPhysicsBody();
                else articulatedDriver.EnforceRenderingRelativeToVehicle(bodyRenderer);
            }
            CacheSurfaceMaskedRenderers();
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
            EnforceVehicleForegroundDepth();
            if (articulatedDriver != null)
            {
                if (fallbackDriver != null) ApplyDriverPhysicsBody();
                else articulatedDriver.EnforceRenderingRelativeToVehicle(bodyRenderer);
            }
            CacheSurfaceMaskedRenderers();
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
            CacheSurfaceMaskedRenderers();
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
            CacheSurfaceMaskedRenderers();
            ApplySavedCustomization();
        }

        private void ApplyDriverPhysicsBody()
        {
            Rigidbody body = vehicle != null ? vehicle.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>();
            Rigidbody2D body2D = vehicle != null ? vehicle.GetComponent<Rigidbody2D>() : GetComponentInParent<Rigidbody2D>();
            articulatedDriver.Apply(fallbackDriver, definition.driverPose, body, body2D, true);
            EnforceVehicleForegroundDepth();
            articulatedDriver.EnforceRenderingRelativeToVehicle(bodyRenderer);
        }

        private void EnforceVehicleForegroundDepth()
        {
            SetWorldDepth(bodyRenderer != null ? bodyRenderer.transform : bodySprite);
            SetWorldDepth(frontWheelRenderer != null ? frontWheelRenderer.transform : frontWheelSprite);
            SetWorldDepth(rearWheelRenderer != null ? rearWheelRenderer.transform : rearWheelSprite);
            SetWorldDepth(articulatedDriver != null ? articulatedDriver.transform : driverAnchor);
        }

        private void CacheSurfaceMaskedRenderers()
        {
            surfaceMaskedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < surfaceMaskedRenderers.Length; i++)
            {
                SpriteRenderer renderer = surfaceMaskedRenderers[i];
                if (renderer != null && !originalMaskInteractions.ContainsKey(renderer))
                    originalMaskInteractions.Add(renderer, renderer.maskInteraction);
            }
        }

        private void EnableSurfaceClipMask(float worldX, float surfaceY, float slopeDegrees)
        {
            EnsureSurfaceClipMask();
            if (surfaceClipMask == null) return;
            if (surfaceMaskedRenderers == null) CacheSurfaceMaskedRenderers();
            for (int i = 0; i < surfaceMaskedRenderers.Length; i++)
            {
                SpriteRenderer renderer = surfaceMaskedRenderers[i];
                if (renderer != null) renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }

            int sortingLayerId = bodyRenderer != null ? bodyRenderer.sortingLayerID : 0;
            surfaceClipMask.backSortingLayerID = sortingLayerId;
            surfaceClipMask.frontSortingLayerID = sortingLayerId;
            surfaceClipMask.backSortingOrder = short.MinValue;
            surfaceClipMask.frontSortingOrder = short.MaxValue;
            Transform maskTransform = surfaceClipMask.transform;
            maskTransform.position = new Vector3(worldX, surfaceY, VehicleForegroundZ - .1f);
            maskTransform.rotation = Quaternion.Euler(0f, 0f, slopeDegrees);
            maskTransform.localScale = new Vector3(SurfaceMaskWidth, SurfaceMaskHeight, 1f);
            surfaceClipMask.enabled = true;
        }

        private void EnsureSurfaceClipMask()
        {
            if (surfaceClipMask != null) return;
            if (sharedSurfaceMaskSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Adventure Surface Clip Mask Texture",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);
                sharedSurfaceMaskSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                    new Vector2(.5f, 0f), 1f, 0, SpriteMeshType.FullRect);
                sharedSurfaceMaskSprite.name = "Adventure Surface Clip Mask Sprite";
                sharedSurfaceMaskSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            GameObject maskObject = new GameObject("Progressive Terrain Surface Mask");
            maskObject.transform.SetParent(transform, true);
            surfaceClipMask = maskObject.AddComponent<SpriteMask>();
            surfaceClipMask.sprite = sharedSurfaceMaskSprite;
            surfaceClipMask.alphaCutoff = .01f;
            surfaceClipMask.isCustomRangeActive = true;
        }

        private void DisableSurfaceClipMask()
        {
            if (surfaceClipMask != null) surfaceClipMask.enabled = false;
            foreach (KeyValuePair<SpriteRenderer, SpriteMaskInteraction> pair in originalMaskInteractions)
                if (pair.Key != null) pair.Key.maskInteraction = pair.Value;
        }

        private static void SetWorldDepth(Transform target) => SetWorldDepth(target, VehicleForegroundZ);

        private static void SetWorldDepth(Transform target, float depth)
        {
            if (target == null) return;
            Vector3 position = target.position;
            position.z = depth;
            target.position = position;
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
