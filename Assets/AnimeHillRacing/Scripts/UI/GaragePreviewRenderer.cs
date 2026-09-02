using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    [ExecuteAlways]
    public sealed class GaragePreviewRenderer : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private RawImage targetImage;
        [SerializeField] private Transform previewRoot;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer rearWheel;
        [SerializeField] private SpriteRenderer frontWheel;
        [SerializeField] private ArticulatedDriver2D driver;
        [SerializeField] private Vector2 renderSize = new Vector2(1024f, 512f);

        private RenderTexture texture;
        private VehicleDefinition appliedVehicle;
        private Sprite appliedSkin;
        private Sprite appliedWheel;
        private DriverDefinition appliedDriver;
        private string appliedAccessoryId;
        private bool appliedAccessoryFlipX;
        private Vector2 lastTargetSize;
        private bool animateElectricSkin;
        private bool animateHillAdventurerSkin;
        private bool animateSportBikeNeonFlow;
        private bool animateGlitchCrystalTide;
        private SpriteRenderer[] underbodyAttachments = new SpriteRenderer[0];

        public SpriteRenderer BodyRenderer => body;
        public SpriteRenderer RearWheelRenderer => rearWheel;
        public SpriteRenderer FrontWheelRenderer => frontWheel;
        public ArticulatedDriver2D Driver => driver;
        public Camera PreviewCamera => previewCamera;
        public Transform PreviewRoot => previewRoot;
        public RawImage TargetImage => targetImage;
        public float HorizontalCenterError
        {
            get
            {
                return previewCamera != null && TryGetVisibleBounds(out Bounds bounds)
                    ? Mathf.Abs(bounds.center.x - previewCamera.transform.position.x)
                    : 0f;
            }
        }

        private int warmUpFrames = 4;

        private void Awake()
        {
            EnsureTexture();
            EnsureDefaultMaterials();
        }

        private void OnEnable()
        {
            Invalidate();
            warmUpFrames = 4;
            EnsureTexture();
            EnsureDefaultMaterials();
            RenderNow();
        }

        private void EnsureDefaultMaterials()
        {
            Material defaultMat = HillAdventurerSkinEffect.GetDefaultSpriteMaterial();
            if (body != null && body.sharedMaterial == null) body.sharedMaterial = defaultMat;
            if (rearWheel != null && rearWheel.sharedMaterial == null) rearWheel.sharedMaterial = defaultMat;
            if (frontWheel != null && frontWheel.sharedMaterial == null) frontWheel.sharedMaterial = defaultMat;
            if (previewRoot != null)
            {
                SpriteRenderer[] srs = previewRoot.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++)
                {
                    if (srs[i] != null && srs[i].sharedMaterial == null)
                        srs[i].sharedMaterial = defaultMat;
                }
            }
        }

        public void Invalidate()
        {
            appliedVehicle = null;
            appliedSkin = null;
            appliedWheel = null;
            appliedDriver = null;
            appliedAccessoryId = null;
            appliedAccessoryFlipX = false;
            animateSportBikeNeonFlow = false;
            animateGlitchCrystalTide = false;
            lastTargetSize = Vector2.zero;
            warmUpFrames = 4;
        }

        public void RefreshNow()
        {
            warmUpFrames = Mathf.Max(warmUpFrames, 4);
            lastTargetSize = Vector2.zero;
            RenderNow();
        }

        private void LateUpdate()
        {
            if (targetImage == null) return;
            if (animateElectricSkin || animateHillAdventurerSkin || animateSportBikeNeonFlow ||
                animateGlitchCrystalTide ||
                (driver != null && driver.AnimatedAccessoryActive))
            {
                RenderNow();
                return;
            }
            if (warmUpFrames > 0)
            {
                warmUpFrames--;
                RenderNow();
                return;
            }
            Vector2 targetSize = targetImage.rectTransform.rect.size;
            if ((targetSize - lastTargetSize).sqrMagnitude < 1f) return;
            lastTargetSize = targetSize;
            RenderNow();
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private void ReleaseTexture()
        {
            if (previewCamera != null && previewCamera.targetTexture == texture)
                previewCamera.targetTexture = null;
            if (texture == null) return;
            texture.Release();
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
            texture = null;
        }

        private Vector2Int GetRenderTextureSize()
        {
            int maximumWidth = Mathf.Max(256, Mathf.RoundToInt(renderSize.x));
            int maximumHeight = Mathf.Max(128, Mathf.RoundToInt(renderSize.y));
            Rect rect = targetImage.rectTransform.rect;
            float aspect = rect.height > 1f && rect.width > 1f
                ? rect.width / rect.height
                : maximumWidth / (float)maximumHeight;
            float maximumAspect = maximumWidth / (float)maximumHeight;
            int width;
            int height;
            if (aspect >= maximumAspect)
            {
                width = maximumWidth;
                height = Mathf.Max(64, Mathf.RoundToInt(width / aspect));
            }
            else
            {
                height = maximumHeight;
                width = Mathf.Max(64, Mathf.RoundToInt(height * aspect));
            }
            return new Vector2Int(width, height);
        }

        private void EnsureTexture()
        {
            if (previewCamera == null || targetImage == null) return;
            Vector2Int targetSize = GetRenderTextureSize();
            if (texture != null && texture.IsCreated() && texture.width == targetSize.x &&
                texture.height == targetSize.y)
            {
                previewCamera.targetTexture = texture;
                previewCamera.enabled = false;
                targetImage.texture = texture;
                return;
            }

            ReleaseTexture();
            texture = new RenderTexture(targetSize.x, targetSize.y, 16, RenderTextureFormat.ARGB32)
            {
                name = "Garage Preview (Reusable)", antiAliasing = 2,
                hideFlags = HideFlags.DontSave
            };
            texture.Create();
            if (previewCamera != null) previewCamera.targetTexture = texture;
            if (previewCamera != null) previewCamera.enabled = false;
            if (targetImage != null) targetImage.texture = texture;
        }

        public void Apply(VehicleDefinition vehicle, VehicleDefinition.CustomizationOption skin,
            VehicleDefinition.CustomizationOption wheelOption, DriverDefinition driverDefinition)
        {
            if (vehicle == null) return;
            Sprite skinSprite = skin != null ? skin.sprite : null;
            Sprite wheelSprite = wheelOption != null ? wheelOption.sprite : null;
            string accessoryId = driverDefinition != null
                ? AnimeHillRacing.Core.SaveManager.GetSelectedCharacterAccessoryId(driverDefinition.DefaultAccessoryId)
                : string.Empty;
            DriverDefinition.CharacterAccessoryOption accessory = driverDefinition?.FindAccessory(accessoryId);
            bool accessoryFlipX = accessory != null && (accessory.supportsFlipVariant
                ? AnimeHillRacing.Core.SaveManager.GetCharacterAccessoryFlipX(
                    accessory.accessoryId, accessory.flipX)
                : accessory.flipX);
            if (vehicle == appliedVehicle && skinSprite == appliedSkin && wheelSprite == appliedWheel &&
                driverDefinition == appliedDriver && accessoryId == appliedAccessoryId &&
                accessoryFlipX == appliedAccessoryFlipX)
                return;
            appliedVehicle = vehicle;
            appliedSkin = skinSprite;
            appliedWheel = wheelSprite;
            appliedDriver = driverDefinition;
            appliedAccessoryId = accessoryId;
            appliedAccessoryFlipX = accessoryFlipX;
            if (body != null)
            {
                body.sprite = skinSprite;
                body.flipX = vehicle.flipBodyX;
                body.sortingOrder = VehicleVisual2D.BodySortingOrder;
                body.transform.localPosition = vehicle.BodyCustomizationOffset(skin);
                body.transform.localScale = vehicle.BodyCustomizationScale(skin);
            }
            animateElectricSkin = FormulaElectricSkinEffect.Apply(body, vehicle, skin);
            HillAdventurerSkinEffect.Apply(body, vehicle, skin);
            SportBikeNeonFlowEffect.ApplySkin(body, vehicle, skin);
            GlitchCrystalTideEffect.ApplySkin(body, skin);
            animateHillAdventurerSkin = HillAdventurerSkinEffect.IsHillAdventurer(vehicle, skin);
            int wheelSortingOrder = Mathf.Max(VehicleVisual2D.MinimumWheelSortingOrder,
                FormulaElectricSkinEffect.ResolveWheelSortingOrder(vehicle, skin));
            if (driver != null)
            {
                driver.Apply(driverDefinition, vehicle.driverPose,
                    accessory, null, false);
                driver.EnforceRenderingRelativeToVehicle(body);
            }
            ApplyWheel(rearWheel, wheelOption, vehicle.rearWheelVisualPosition, vehicle.RearWheelDiameter,
                wheelSortingOrder, false);
            ApplyWheel(frontWheel, wheelOption, vehicle.frontWheelVisualPosition, vehicle.FrontWheelDiameter,
                wheelSortingOrder, true);
            animateSportBikeNeonFlow = SportBikeNeonFlowEffect.IsAnimated(vehicle, skin, wheelOption);
            animateGlitchCrystalTide = GlitchCrystalTideEffect.IsAnimated(skin, wheelOption);
            ApplyUnderbodyAttachments(vehicle);
            CenterCompositeHorizontally();
            warmUpFrames = 3;
            RenderNow();
        }

        private void CenterCompositeHorizontally()
        {
            if (previewRoot == null || previewCamera == null) return;

            // Reset before measuring so switching between asymmetrical vehicles cannot accumulate
            // offsets. Only the reusable preview hierarchy moves; gameplay prefabs remain untouched.
            Vector3 rootPosition = previewRoot.position;
            rootPosition.x = previewCamera.transform.position.x;
            previewRoot.position = rootPosition;
            if (!TryGetVisibleBounds(out Bounds bounds)) return;
            rootPosition = previewRoot.position;
            rootPosition.x += previewCamera.transform.position.x - bounds.center.x;
            previewRoot.position = rootPosition;
        }

        private bool TryGetVisibleBounds(out Bounds bounds)
        {
            bounds = default;
            if (previewRoot == null) return false;
            SpriteRenderer[] renderers = previewRoot.GetComponentsInChildren<SpriteRenderer>(true);
            bool found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || renderer.sprite == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        public void Assign(Camera camera, RawImage image, Transform root, SpriteRenderer bodyRenderer,
            SpriteRenderer rearWheelRenderer, SpriteRenderer frontWheelRenderer, ArticulatedDriver2D articulated)
        {
            previewCamera = camera; targetImage = image; previewRoot = root; body = bodyRenderer;
            rearWheel = rearWheelRenderer; frontWheel = frontWheelRenderer; driver = articulated;
            if (body != null && body.sharedMaterial == null)
                body.sharedMaterial = HillAdventurerSkinEffect.GetDefaultSpriteMaterial();
            warmUpFrames = 3;
            EnsureTexture();
        }

        public void SetTarget(RawImage image)
        {
            if (targetImage == image)
            {
                EnsureTexture();
                RenderNow();
                return;
            }

            if (targetImage != null && targetImage.texture == texture)
                targetImage.texture = null;
            targetImage = image;
            lastTargetSize = Vector2.zero;
            warmUpFrames = 3;
            if (targetImage == null)
            {
                ReleaseTexture();
                return;
            }
            EnsureTexture();
            RenderNow();
        }

        private void RenderNow()
        {
            EnsureTexture();
            if (Application.isBatchMode || previewCamera == null || texture == null) return;
            previewCamera.enabled = false;
            previewCamera.Render();
            if (targetImage != null && targetImage.texture != texture)
                targetImage.texture = texture;
        }

        private void ApplyWheel(SpriteRenderer renderer, VehicleDefinition.CustomizationOption option,
            Vector2 position, float diameter, int sortingOrder, bool front)
        {
            if (renderer == null) return;
            if (renderer.sharedMaterial == null)
                renderer.sharedMaterial = HillAdventurerSkinEffect.GetDefaultSpriteMaterial();
            renderer.sprite = option != null ? option.sprite : null;
            Vector2 cosmeticOffset = appliedVehicle.WheelCustomizationOffset(option, front);
            renderer.transform.localPosition = new Vector3(position.x + cosmeticOffset.x,
                position.y + cosmeticOffset.y, -.02f);
            if (body != null) renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localScale = Vector3.one *
                                            appliedVehicle.WheelCustomizationScale(option, diameter, front);
            SportBikeNeonFlowEffect.ApplyWheel(renderer, appliedVehicle, option);
            GlitchCrystalTideEffect.ApplyWheel(renderer, option);
        }

        private void ApplyUnderbodyAttachments(VehicleDefinition vehicle)
        {
            Vector2[] positions = vehicle.underbodyAttachmentPositions;
            int required = vehicle.underbodyAttachmentSprite != null && positions != null &&
                           vehicle.underbodyAttachmentVisualWidth > 0f
                ? positions.Length : 0;
            EnsureUnderbodyAttachmentPool(required);
            for (int i = 0; i < underbodyAttachments.Length; i++)
            {
                SpriteRenderer renderer = underbodyAttachments[i];
                if (renderer == null) continue;
                bool active = i < required;
                renderer.gameObject.SetActive(active);
                if (!active) continue;
                renderer.sprite = vehicle.underbodyAttachmentSprite;
                renderer.sortingOrder = 12;
                renderer.flipX = false;
                renderer.transform.localPosition = new Vector3(positions[i].x, positions[i].y, -.03f);
                float scale = VehicleDefinition.SpriteWidthScale(vehicle.underbodyAttachmentSprite,
                    vehicle.underbodyAttachmentVisualWidth);
                renderer.transform.localScale = Vector3.one * scale;
                if (body != null)
                {
                    renderer.sharedMaterial = body.sharedMaterial;
                    var bodyProperties = new MaterialPropertyBlock();
                    body.GetPropertyBlock(bodyProperties);
                    renderer.SetPropertyBlock(bodyProperties);
                }
            }
        }

        private void EnsureUnderbodyAttachmentPool(int required)
        {
            if (underbodyAttachments.Length >= required) return;
            var expanded = new SpriteRenderer[required];
            for (int i = 0; i < underbodyAttachments.Length; i++) expanded[i] = underbodyAttachments[i];
            Transform parent = previewRoot != null ? previewRoot : transform;
            for (int i = underbodyAttachments.Length; i < required; i++)
            {
                GameObject attachment = new GameObject($"Preview Underbody Attachment {i + 1}");
                attachment.hideFlags = HideFlags.DontSave;
                attachment.layer = parent.gameObject.layer;
                attachment.transform.SetParent(parent, false);
                expanded[i] = attachment.AddComponent<SpriteRenderer>();
            }
            underbodyAttachments = expanded;
        }
    }
}
