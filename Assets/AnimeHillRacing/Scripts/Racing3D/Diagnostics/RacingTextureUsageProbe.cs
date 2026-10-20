#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // Explicit size QA only. Records real screen bounds before any downscaling.
    public sealed class RacingTextureUsageProbe : MonoBehaviour
    {
        [Serializable] sealed class Usage { public string name, scenes; public int width, height; public float maxScreenWidth, maxScreenHeight; }
        [Serializable] sealed class Report { public int screenWidth, screenHeight; public string classification = "OBSERVED_VISIBLE_TEXTURE_SCREEN_BOUNDS_NOT_ALL_CONTENT_COVERAGE"; public List<Usage> textures; }
        readonly Dictionary<int, Usage> textures = new Dictionary<int, Usage>();
        readonly Vector3[] corners = new Vector3[4];
        float next;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--texture-usage") < 0) return;
            var probe = new GameObject("Explicit texture footprint QA").AddComponent<RacingTextureUsageProbe>();
            DontDestroyOnLoad(probe.gameObject);
        }
        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1;
            foreach (var graphic in FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            {
                if (!graphic.isActiveAndEnabled || graphic.mainTexture == null) continue;
                var canvas = graphic.canvas; if (canvas == null) continue;
                graphic.rectTransform.GetWorldCorners(corners);
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (b.x < 0 || a.x > Screen.width || b.y < 0 || a.y > Screen.height) continue;
                Observe(graphic.mainTexture, Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
            }
            var main = Camera.main;
            if (main == null) return;
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.isVisible) continue;
                Vector3 a = main.WorldToScreenPoint(renderer.bounds.min), b = main.WorldToScreenPoint(renderer.bounds.max);
                if (a.z <= 0 || b.z <= 0) continue;
                if (renderer is SpriteRenderer sprite && sprite.sprite != null) Observe(sprite.sprite.texture, Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                else foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || !material.HasProperty("_MainTex")) continue;
                    var texture = material.GetTexture("_MainTex");
                    if (texture != null) Observe(texture, Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                }
            }
        }
        void Observe(Texture texture, float width, float height)
        {
            int key = texture.GetInstanceID();
            if (!textures.TryGetValue(key, out var item)) textures[key] = item = new Usage { name = texture.name, width = texture.width, height = texture.height };
            item.maxScreenWidth = Mathf.Max(item.maxScreenWidth, width); item.maxScreenHeight = Mathf.Max(item.maxScreenHeight, height);
            string scene = SceneManager.GetActiveScene().name;
            if (item.scenes == null || !item.scenes.Contains(scene)) item.scenes += scene + ";";
        }
        void OnApplicationQuit()
        {
            string output = RacingFrameForensics.Argument("--closure-output");
            if (string.IsNullOrEmpty(output)) return;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "texture-screen-usage.json"), JsonUtility.ToJson(new Report { screenWidth = Screen.width,
                screenHeight = Screen.height, textures = new List<Usage>(textures.Values) }, true));
        }
    }
}
#endif
