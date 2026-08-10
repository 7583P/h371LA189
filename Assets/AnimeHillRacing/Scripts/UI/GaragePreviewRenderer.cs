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

        public SpriteRenderer BodyRenderer => body;
        public SpriteRenderer RearWheelRenderer => rearWheel;
        public SpriteRenderer FrontWheelRenderer => frontWheel;
        public ArticulatedDriver2D Driver => driver;

        private void Awake() => EnsureTexture();

        private void OnEnable()
        {
            EnsureTexture();
            if (!Application.isPlaying) RenderNow();
        }

        private void EnsureTexture()
        {
            if (previewCamera == null || targetImage == null) return;
            if (texture != null && texture.IsCreated())
            {
                previewCamera.targetTexture = texture;
                previewCamera.enabled = false;
                targetImage.texture = texture;
                return;
            }
            int width = Mathf.Max(256, Mathf.RoundToInt(renderSize.x));
            int height = Mathf.Max(128, Mathf.RoundToInt(renderSize.y));
            texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
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
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (texture == null) return;
            texture.Release();
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
            texture = null;
        }

        public void Apply(VehicleDefinition vehicle, VehicleDefinition.CustomizationOption skin,
            VehicleDefinition.CustomizationOption wheelOption, DriverDefinition driverDefinition)
        {
            if (vehicle == null) return;
            Sprite skinSprite = skin != null ? skin.sprite : null;
            Sprite wheelSprite = wheelOption != null ? wheelOption.sprite : null;
            if (vehicle == appliedVehicle && skinSprite == appliedSkin && wheelSprite == appliedWheel &&
                driverDefinition == appliedDriver)
                return;
            appliedVehicle = vehicle;
            appliedSkin = skinSprite;
            appliedWheel = wheelSprite;
            appliedDriver = driverDefinition;
            if (body != null)
            {
                body.sprite = skinSprite;
                body.transform.localPosition = new Vector3(vehicle.bodyVisualOffset.x, vehicle.bodyVisualOffset.y, 0f);
                FitWidth(body.transform, body.sprite, vehicle.visualWidth);
            }
            if (driver != null) driver.Apply(driverDefinition, vehicle.driverPose, null, false);
            ApplyWheel(rearWheel, wheelOption, vehicle.rearWheelVisualPosition, vehicle.wheelVisualDiameter);
            ApplyWheel(frontWheel, wheelOption, vehicle.frontWheelVisualPosition, vehicle.wheelVisualDiameter);
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
            float source = renderer.sprite != null ? Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y) : 1f;
            renderer.transform.localScale = Vector3.one * (diameter / Mathf.Max(.001f, source));
        }

        private static void FitWidth(Transform target, Sprite sprite, float width)
        {
            float source = sprite != null ? sprite.bounds.size.x : 1f;
            target.localScale = Vector3.one * (width / Mathf.Max(.001f, source));
        }
    }
}
