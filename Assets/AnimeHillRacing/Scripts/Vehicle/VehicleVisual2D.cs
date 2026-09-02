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
        private const int SurfaceMaskSampleCount = 129;
        private const int SurfaceMaskIntersectionIterations = 4;

        private static Texture2D sharedSurfaceMaskTexture;

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
        private Sprite surfaceClipMaskSprite;
        private Vector2[] surfaceClipVertices;
        private ushort[] surfaceClipTriangles;
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

        private void OnDestroy()
        {
            if (surfaceClipMaskSprite != null) Destroy(surfaceClipMaskSprite);
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
            EnableSurfaceClipMask(terrain, reference.position.x, surfaceY, slopeDegrees);
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
            GlitchCrystalTideEffect.ApplySkin(bodyRenderer, skin);
            int wheelSortingOrder = Mathf.Max(MinimumWheelSortingOrder,
                FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin));
            ApplyWheel(frontWheelRenderer, wheel, definition.FrontWheelDiameter, wheelSortingOrder, true);
            ApplyWheel(rearWheelRenderer, wheel, definition.RearWheelDiameter, wheelSortingOrder, false);
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
            GlitchCrystalTideEffect.ApplySkin(bodyRenderer, skin);
            int wheelSortingOrder = Mathf.Max(MinimumWheelSortingOrder,
                FormulaElectricSkinEffect.ResolveWheelSortingOrder(definition, skin));
            ApplyWheel(frontWheelRenderer, wheel, definition.FrontWheelDiameter, wheelSortingOrder, true);
            ApplyWheel(rearWheelRenderer, wheel, definition.RearWheelDiameter, wheelSortingOrder, false);
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

        private void EnableSurfaceClipMask(TerrainManager terrain, float worldX, float surfaceY,
            float slopeDegrees)
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
            UpdateSurfaceClipGeometry(terrain, worldX, surfaceY, slopeDegrees);
            surfaceClipMask.enabled = true;
        }

        private void EnsureSurfaceClipMask()
        {
            if (surfaceClipMask != null) return;
            if (sharedSurfaceMaskTexture == null)
            {
                sharedSurfaceMaskTexture = new Texture2D(1, 2, TextureFormat.RGBA32, false)
                {
                    name = "Adventure Surface Clip Mask Texture",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                sharedSurfaceMaskTexture.SetPixel(0, 0, Color.white);
                sharedSurfaceMaskTexture.SetPixel(0, 1, Color.white);
                sharedSurfaceMaskTexture.Apply(false, true);
            }

            surfaceClipMaskSprite = Sprite.Create(sharedSurfaceMaskTexture, new Rect(0f, 0f, 1f, 2f),
                new Vector2(.5f, .5f), 1f, 0, SpriteMeshType.FullRect);
            surfaceClipMaskSprite.name = "Adventure Dynamic Surface Clip Mask Sprite";
            surfaceClipMaskSprite.hideFlags = HideFlags.HideAndDontSave;
            InitializeSurfaceClipGeometry();

            GameObject maskObject = new GameObject("Progressive Terrain Surface Mask");
            maskObject.transform.SetParent(transform, true);
            surfaceClipMask = maskObject.AddComponent<SpriteMask>();
            surfaceClipMask.sprite = surfaceClipMaskSprite;
            surfaceClipMask.alphaCutoff = .01f;
            surfaceClipMask.isCustomRangeActive = true;
        }

        private void InitializeSurfaceClipGeometry()
        {
            surfaceClipVertices = new Vector2[SurfaceMaskSampleCount * 2];
            surfaceClipTriangles = new ushort[(SurfaceMaskSampleCount - 1) * 6];
            for (int i = 0; i < SurfaceMaskSampleCount; i++)
            {
                // OverrideGeometry uses sprite-rect pixel coordinates. With this 1x2 sprite and centred
                // pivot, rect X 0..1 becomes local X -0.5..0.5 and rect Y 0..2 becomes local Y -1..1.
                float rectX = i / (SurfaceMaskSampleCount - 1f);
                int vertex = i * 2;
                surfaceClipVertices[vertex] = new Vector2(rectX, 1f);
                surfaceClipVertices[vertex + 1] = new Vector2(rectX, 2f);
                if (i >= SurfaceMaskSampleCount - 1) continue;
                int triangle = i * 6;
                surfaceClipTriangles[triangle] = (ushort)vertex;
                surfaceClipTriangles[triangle + 1] = (ushort)(vertex + 1);
                surfaceClipTriangles[triangle + 2] = (ushort)(vertex + 3);
                surfaceClipTriangles[triangle + 3] = (ushort)vertex;
                surfaceClipTriangles[triangle + 4] = (ushort)(vertex + 3);
                surfaceClipTriangles[triangle + 5] = (ushort)(vertex + 2);
            }
            surfaceClipMaskSprite.OverrideGeometry(surfaceClipVertices, surfaceClipTriangles);
        }

        private void UpdateSurfaceClipGeometry(TerrainManager terrain, float worldX, float surfaceY,
            float slopeDegrees)
        {
            if (terrain == null || surfaceClipMaskSprite == null || surfaceClipVertices == null) return;

            float radians = slopeDegrees * Mathf.Deg2Rad;
            float tangentX = Mathf.Cos(radians);
            float tangentY = Mathf.Sin(radians);
            float normalX = -tangentY;
            float normalY = tangentX;
            float halfWidth = SurfaceMaskWidth * .5f;
            for (int i = 0; i < SurfaceMaskSampleCount; i++)
            {
                float normalizedX = i / (SurfaceMaskSampleCount - 1f);
                float tangentOffset = Mathf.Lerp(-halfWidth, halfWidth, normalizedX);
                float baseX = worldX + tangentX * tangentOffset;
                float baseY = surfaceY + tangentY * tangentOffset;
                float normalOffset = 0f;
                bool hasSurface = true;
                for (int iteration = 0; iteration < SurfaceMaskIntersectionIterations; iteration++)
                {
                    float sampleX = baseX + normalX * normalOffset;
                    if (!terrain.TrySampleAuthoritativeSurface(sampleX, out float sampledY,
                            out float sampledSlopeDegrees))
                    {
                        hasSurface = false;
                        break;
                    }

                    float error = baseY + normalY * normalOffset - sampledY;
                    if (Mathf.Abs(error) <= .001f) break;
                    float sampledSlope = Mathf.Tan(sampledSlopeDegrees * Mathf.Deg2Rad);
                    float derivative = normalY - sampledSlope * normalX;
                    if (Mathf.Abs(derivative) <= .01f) break;
                    normalOffset = Mathf.Clamp(normalOffset - error / derivative,
                        -SurfaceMaskHeight, SurfaceMaskHeight);
                }

                int vertex = i * 2;
                float localContactY = hasSurface
                    ? Mathf.Clamp(normalOffset / SurfaceMaskHeight, -1f, 1f) : -1f;
                surfaceClipVertices[vertex] = new Vector2(normalizedX, localContactY + 1f);
                surfaceClipVertices[vertex + 1] = new Vector2(normalizedX, 2f);
            }

            surfaceClipMaskSprite.OverrideGeometry(surfaceClipVertices, surfaceClipTriangles);
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
            float diameter, int sortingOrder, bool front)
        {
            if (renderer == null) return;
            renderer.sprite = option != null ? option.sprite : null;
            if (bodyRenderer != null) renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localScale = Vector3.one *
                                            definition.WheelCustomizationScale(option, diameter, front);
            Vector2 authoredPosition = front
                ? definition.frontWheelVisualPosition : definition.rearWheelVisualPosition;
            Vector2 cosmeticOffset = definition.WheelCustomizationOffset(option, front);
            Vector3 localPosition = renderer.transform.localPosition;
            localPosition.x = authoredPosition.x + cosmeticOffset.x;
            localPosition.y = authoredPosition.y + cosmeticOffset.y;
            renderer.transform.localPosition = localPosition;
            SportBikeNeonFlowEffect.ApplyWheel(renderer, definition, option);
            GlitchCrystalTideEffect.ApplyWheel(renderer, option);
        }
    }
}
