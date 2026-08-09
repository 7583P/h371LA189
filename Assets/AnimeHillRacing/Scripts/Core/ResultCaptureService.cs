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
                captureCamera.orthographicSize = sideViewOrthographicSize;
                captureCamera.transform.position = new Vector3(target.position.x + sideViewForwardBias,
                    target.position.y + sideViewHeight, target.position.z - sideViewDistance);
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
            captureCamera.targetTexture = texture;
            captureCamera.Render();
            captureCamera.targetTexture = null;
            if (resultImage != null) resultImage.texture = texture;
            yield return null;
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
