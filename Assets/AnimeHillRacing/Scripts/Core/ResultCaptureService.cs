using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Core
{
    public sealed class ResultCaptureService : MonoBehaviour
    {
        [SerializeField] private Camera captureCamera;
        [SerializeField] private Transform target;
        [SerializeField] private RawImage resultImage;
        [SerializeField, Range(256, 1024)] private int resolution = 512;
        [SerializeField, Min(5f)] private float sideViewOrthographicSize = 12.5f;
        [SerializeField] private float sideViewForwardBias = 2f;
        [SerializeField] private float sideViewHeight = 1.8f;
        [SerializeField, Min(5f)] private float sideViewDistance = 20f;
        [SerializeField] private Vector3 localOffset3D = new Vector3(5.5f, 3.5f, -7.5f);

        private const float SideViewMainCameraScale = .78f;
        private const float MaximumSideViewHeight = 1f;
        private static readonly Color CaptureBackground = new Color(.005f, .008f, .02f, 1f);

        private RenderTexture texture;

        public int Resolution => resolution;
        public RenderTexture CapturedTexture => texture;
        public Camera CaptureCamera => captureCamera;
        public RawImage ResultImage => resultImage;
        public float SideViewOrthographicSize => sideViewOrthographicSize;

        private void Awake()
        {
            if (captureCamera != null) captureCamera.enabled = false;
        }

        public IEnumerator Capture(bool sideView)
        {
            Release();
            if (captureCamera == null || target == null) yield break;
            ConfigureWorldOnlyCamera();
            texture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
            {
                name = "Result Capture 1x1",
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            if (sideView)
            {
                captureCamera.orthographic = true;
                Camera worldCamera = Camera.main;
                float framedSize = sideViewOrthographicSize;
                if (worldCamera != null && worldCamera != captureCamera && worldCamera.orthographic)
                    framedSize = Mathf.Min(framedSize, worldCamera.orthographicSize * SideViewMainCameraScale);
                captureCamera.orthographicSize = Mathf.Max(5f, framedSize);
                captureCamera.transform.position = new Vector3(target.position.x + sideViewForwardBias,
                    target.position.y + Mathf.Min(sideViewHeight, MaximumSideViewHeight),
                    target.position.z - sideViewDistance);
                captureCamera.transform.rotation = Quaternion.identity;
            }
            else
            {
                captureCamera.orthographic = false;
                captureCamera.transform.position = target.position + target.TransformDirection(localOffset3D);
                Vector3 focus = target.position + Vector3.up * 0.4f;
                captureCamera.transform.rotation = Quaternion.LookRotation(
                    focus - captureCamera.transform.position, Vector3.up);
            }
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            bool[] canvasStates = new bool[canvases.Length];
            try
            {
                // Camera.Render is synchronous. Disabling only Canvas renderers around this call excludes
                // overlay, camera-space and world-space UI without changing GameObject state or producing a
                // visible frame where the player's HUD is missing.
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvasStates[i] = canvases[i] != null && canvases[i].enabled;
                    if (canvasStates[i]) canvases[i].enabled = false;
                }
                captureCamera.targetTexture = texture;
                captureCamera.Render();
            }
            finally
            {
                captureCamera.targetTexture = null;
                for (int i = 0; i < canvases.Length; i++)
                    if (canvases[i] != null) canvases[i].enabled = canvasStates[i];
            }
            if (resultImage != null) resultImage.texture = texture;
            yield return null;
        }

        private void ConfigureWorldOnlyCamera()
        {
            Camera worldCamera = Camera.main;
            if (worldCamera != null && worldCamera != captureCamera)
                captureCamera.cullingMask = worldCamera.cullingMask;

            // A square side-view capture can expose pixels outside the map's camera-sized background.
            // Clear those pixels to a neutral near-black instead of inheriting the orange gameplay clear.
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = CaptureBackground;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) captureCamera.cullingMask &= ~(1 << uiLayer);
        }

        public void Release()
        {
            if (resultImage != null) resultImage.texture = null;
            if (texture == null) return;
            texture.Release();
            Destroy(texture);
            texture = null;
        }

        private void OnDestroy() => Release();

        public void Assign(Camera camera, Transform captureTarget, RawImage image)
        {
            captureCamera = camera;
            target = captureTarget;
            resultImage = image;
            captureCamera.enabled = false;
        }

        public void SetTarget(Transform captureTarget) => target = captureTarget;
    }
}
