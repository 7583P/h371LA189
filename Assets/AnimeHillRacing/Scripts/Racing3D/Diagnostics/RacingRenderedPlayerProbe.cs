#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    /// <summary>
    /// Explicit development-player profiling only. Hidden Windows players can skip
    /// rendering; this executes the complete Built-in camera and HUD every frame.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class RacingRenderedPlayerProbe : MonoBehaviour
    {
        static readonly ProfilerMarker RenderMarker = new ProfilerMarker("Racing.RenderProbe.Camera");
        [Serializable]
        sealed class RenderProof
        {
            public string classification = "FULL_BUILT_IN_CAMERA_AND_HUD_TO_RENDER_TEXTURE";
            public string renderMode;
            public int width, height, antiAliasing, renderedFrames, canvases;
            public bool targetCreated, renderWorkVerified;
        }

        Camera renderCamera;
        RaceManager manager;
        RenderTexture target, previousTarget;
        bool previousEnabled, uiBound, resultsBound;
        int renderedFrames, canvasesBound;
        bool nativeWindow;
        public bool IsBound => renderCamera != null && (nativeWindow || (target != null && target.IsCreated()));
        public bool OffscreenRendering => IsBound && !nativeWindow;

        public void Bind(Camera camera, RaceManager race)
        {
            Release();
            if (camera == null) throw new InvalidOperationException("Rendered profiling requires the actual race camera.");
            renderCamera = camera;
            manager = race;
            previousEnabled = camera.enabled;
            previousTarget = camera.targetTexture;
            nativeWindow = RacingFrameForensics.Requested && RacingFrameForensics.Argument("--forensics-render", "manual") == "native";
            Camera.onPostRender += OnRendered;
            renderedFrames = canvasesBound = 0;
            uiBound = resultsBound = false;
            if (nativeWindow) { camera.enabled = true; return; }
            target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
            {
                name = "Explicit player performance render",
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing)
            };
            target.Create();
            camera.targetTexture = target;
            camera.enabled = RacingFrameForensics.Requested && RacingFrameForensics.Argument("--forensics-render", "manual") == "automatic";
        }

        void OnRendered(Camera camera) { if (camera == renderCamera) renderedFrames++; }

        void BindUi()
        {
            canvasesBound = 0;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
                if (nativeWindow) { canvasesBound++; continue; }
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = renderCamera;
                canvas.planeDistance = .5f;
                canvasesBound++;
            }
            uiBound = true;
        }

        void LateUpdate()
        {
            if (!IsBound) return;
            if (!uiBound || (manager.State == RaceState.Results && !resultsBound))
            {
                BindUi();
                resultsBound = manager.State == RaceState.Results;
            }
            if (!nativeWindow && !renderCamera.enabled) using (RenderMarker.Auto()) renderCamera.Render();
        }

        public void SaveFrame(string path)
        {
            if (!IsBound) return;
            if (nativeWindow) { ScreenCapture.CaptureScreenshot(path); return; }
            BindUi();
            Canvas.ForceUpdateCanvases();
            renderCamera.Render();
            var previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                RenderTexture.active = target;
                texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (texture != null) Destroy(texture);
            }
        }

        public void Export(string path)
        {
            if (!IsBound) return;
            File.WriteAllText(path, JsonUtility.ToJson(new RenderProof
            {
                classification = nativeWindow ? "FULL_NATIVE_WINDOW_CAMERA_AND_HUD" : "OFFSCREEN_RENDER_REQUIRES_POST_RENDER_PROOF",
                renderMode = nativeWindow ? "native" : (renderCamera.enabled ? "automatic" : "manual"),
                width = nativeWindow ? Screen.width : target.width, height = nativeWindow ? Screen.height : target.height,
                antiAliasing = nativeWindow ? QualitySettings.antiAliasing : target.antiAliasing,
                renderedFrames = renderedFrames, canvases = canvasesBound, targetCreated = target != null && target.IsCreated(),
                renderWorkVerified = renderedFrames > 0
            }, true));
        }

        void Release()
        {
            Camera.onPostRender -= OnRendered;
            if (renderCamera != null)
            {
                renderCamera.targetTexture = previousTarget;
                renderCamera.enabled = previousEnabled;
            }
            if (target != null) { target.Release(); Destroy(target); }
            renderCamera = null;
            target = null;
        }

        void OnDestroy() => Release();
    }
}
#endif
