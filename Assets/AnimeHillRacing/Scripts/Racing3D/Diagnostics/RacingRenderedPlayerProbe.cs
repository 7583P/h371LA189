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
            public int width, height, antiAliasing, renderedFrames, canvases;
            public bool targetCreated;
        }

        Camera renderCamera;
        RaceManager manager;
        RenderTexture target, previousTarget;
        bool previousEnabled, uiBound, resultsBound;
        int renderedFrames, canvasesBound;
        public bool IsBound => renderCamera != null && target != null && target.IsCreated();

        public void Bind(Camera camera, RaceManager race)
        {
            Release();
            if (camera == null) throw new InvalidOperationException("Rendered profiling requires the actual race camera.");
            renderCamera = camera;
            manager = race;
            previousEnabled = camera.enabled;
            previousTarget = camera.targetTexture;
            target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
            {
                name = "Explicit player performance render",
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing)
            };
            target.Create();
            camera.targetTexture = target;
            camera.enabled = false;
            renderedFrames = canvasesBound = 0;
            uiBound = resultsBound = false;
        }

        void BindUi()
        {
            canvasesBound = 0;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
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
            using (RenderMarker.Auto()) renderCamera.Render();
            renderedFrames++;
        }

        public void SaveFrame(string path)
        {
            if (!IsBound) return;
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
                width = target.width, height = target.height, antiAliasing = target.antiAliasing,
                renderedFrames = renderedFrames, canvases = canvasesBound, targetCreated = target.IsCreated()
            }, true));
        }

        void Release()
        {
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
