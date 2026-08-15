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
        private Vector2 lastTargetSize;

        public SpriteRenderer BodyRenderer => body;
        public SpriteRenderer RearWheelRenderer => rearWheel;
        public SpriteRenderer FrontWheelRenderer => frontWheel;
        public ArticulatedDriver2D Driver => driver;
        public Camera PreviewCamera => previewCamera;

        private void Awake() => EnsureTexture();

        private void OnEnable()
        {
            EnsureTexture();
            if (!Application.isPlaying) RenderNow();
        }

        private void LateUpdate()
        {
            if (targetImage == null) return;
            Vector2 targetSize = targetImage.rectTransform.rect.size;
            if ((targetSize - lastTargetSize).sqrMagnitude < 1f) return;
            lastTargetSize = targetSize;
            RenderNow();
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

        public void Apply(VehicleDefinition vehicle, VehicleDefinition.CustomizationOption skin,
            VehicleDefinition.CustomizationOption wheelOption, DriverDefinition driverDefinition)
        {
            if (vehicle == null) return;
            Sprite skinSprite = skin != null ? skin.sprite : null;
            Sprite wheelSprite = wheelOption != null ? wheelOption.sprite : null;
            string accessoryId = driverDefinition != null
                ? AnimeHillRacing.Core.SaveManager.GetSelectedCharacterAccessoryId(driverDefinition.DefaultAccessoryId)
                : string.Empty;
            if (vehicle == appliedVehicle && skinSprite == appliedSkin && wheelSprite == appliedWheel &&
                driverDefinition == appliedDriver && accessoryId == appliedAccessoryId)
                return;
            appliedVehicle = vehicle;
            appliedSkin = skinSprite;
            appliedWheel = wheelSprite;
            appliedDriver = driverDefinition;
            appliedAccessoryId = accessoryId;
            if (body != null)
            {
                body.sprite = skinSprite;
                body.flipX = vehicle.flipBodyX;
                body.sortingOrder = 10;
                body.transform.localPosition = new Vector3(vehicle.bodyVisualOffset.x, vehicle.bodyVisualOffset.y, 0f);
                FitWidth(body.transform, body.sprite, vehicle.visualWidth);
            }
            if (driver != null) driver.Apply(driverDefinition, vehicle.driverPose,
                driverDefinition?.FindAccessory(accessoryId), null, false);
            ApplyWheel(rearWheel, wheelOption, vehicle.rearWheelVisualPosition, vehicle.RearWheelDiameter);
            ApplyWheel(frontWheel, wheelOption, vehicle.frontWheelVisualPosition, vehicle.FrontWheelDiameter);
            RenderNow();
        }

        public void Assign(Camera camera, RawImage image, Transform root, SpriteRenderer bodyRenderer,
            SpriteRenderer rearWheelRenderer, SpriteRenderer frontWheelRenderer, ArticulatedDriver2D articulated)
        {
            previewCamera = camera; targetImage = image; previewRoot = root; body = bodyRenderer;
            rearWheel = rearWheelRenderer; frontWheel = frontWheelRenderer; driver = articulated;
            EnsureTexture();
        }

        private void RenderNow()
        {
            EnsureTexture();
            if (Application.isBatchMode || previewCamera == null || texture == null) return;
            previewCamera.enabled = false;
            previewCamera.Render();
        }

        private static void ApplyWheel(SpriteRenderer renderer, VehicleDefinition.CustomizationOption option,
            Vector2 position, float diameter)
        {
            if (renderer == null) return;
            renderer.sprite = option != null ? option.sprite : null;
            renderer.transform.localPosition = new Vector3(position.x, position.y, -.02f);
            renderer.sortingOrder = 11;
            renderer.transform.localScale = Vector3.one *
                                            VehicleDefinition.WheelVisualScale(option, diameter);
        }

        private static void FitWidth(Transform target, Sprite sprite, float width)
        {
            target.localScale = Vector3.one * VehicleDefinition.SpriteWidthScale(sprite, width);
        }
    }
}
