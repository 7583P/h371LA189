using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    // One small bitmap per cosmetic combination. No live models belong to UI rows.
    public sealed class RacingComboThumbnailCache : MonoBehaviour
    {
        public const int Width = 192, Height = 108, Capacity = 96;
        const int CaptureLayer = 28;
        static readonly ProfilerMarker CaptureMarker = new ProfilerMarker("Racing.Combo.Capture");
        static RacingComboThumbnailCache instance;

        internal sealed class Entry
        {
            internal string key, vehicle, wheel, driver, accessory;
            internal Texture2D texture;
            internal readonly HashSet<RacingComboThumbnail> viewers = new HashSet<RacingComboThumbnail>();
            internal long lastUse;
            internal bool failed;
        }

        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        long useSequence;
        int generated, hits;
        float nextCapture;
        bool menu;
        GameObject stage;
        Camera captureCamera;
        RenderTexture target;

        public static int GeneratedCount => instance == null ? 0 : instance.generated;
        public static int CacheHits => instance == null ? 0 : instance.hits;
        public static int EntryCount => instance == null ? 0 : instance.entries.Count;
        public static long TextureBytes => (long)GeneratedTextureCount * Width * Height * 4;
        public static int GeneratedTextureCount
        {
            get {
                if (instance == null) return 0;
                int count = 0;
                foreach (var entry in instance.entries.Values) if (entry.texture != null) count++;
                return count;
            }
        }
        public static bool CanCapture => instance != null && instance.menu && !RacingProgressionService.IsRaceActive;

        static RacingComboThumbnailCache Get()
        {
            if (instance != null) return instance;
            var root = new GameObject("Racing combo bitmap cache");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<RacingComboThumbnailCache>();
            instance.SceneChanged(default, SceneManager.GetActiveScene());
            SceneManager.activeSceneChanged += instance.SceneChanged;
            return instance;
        }

        internal static Entry Bind(RacingComboThumbnail viewer, string vehicle, string wheel, string driver, string accessory)
        {
            var cache = Get();
            vehicle = string.IsNullOrEmpty(vehicle) ? "hill-adventurer-01" : vehicle;
            wheel = string.IsNullOrEmpty(wheel) ? "wheel-default" : wheel;
            if (wheel == "wheel-meta-cyan") wheel = MetaProgressionConfig.MetaWheelId;
            driver = string.IsNullOrEmpty(driver) ? "cobalt-driver" : driver;
            accessory = string.IsNullOrEmpty(accessory) ? "none" : accessory;
            string key = vehicle + "/" + wheel + "/" + driver + "/" + accessory;
            if (!cache.entries.TryGetValue(key, out var entry))
            {
                // A visible image is pinned. Eviction never leaves another row with a destroyed texture.
                if (cache.entries.Count >= Capacity && !cache.EvictUnused()) return null;
                entry = new Entry { key = key, vehicle = vehicle, wheel = wheel, driver = driver, accessory = accessory };
                cache.entries.Add(key, entry);
            }
            else if (entry.texture != null) cache.hits++;
            entry.lastUse = ++cache.useSequence;
            entry.viewers.Add(viewer);
            return entry;
        }

        internal static void Unbind(Entry entry, RacingComboThumbnail viewer)
        {
            if (entry != null) entry.viewers.Remove(viewer);
        }

        bool EvictUnused()
        {
            // Dispose only in menus; a request during gameplay may use an existing bitmap.
            if (!menu || RacingProgressionService.IsRaceActive) return false;
            Entry oldest = null;
            foreach (var entry in entries.Values)
                if (entry.viewers.Count == 0 && (oldest == null || entry.lastUse < oldest.lastUse)) oldest = entry;
            if (oldest == null) return false;
            entries.Remove(oldest.key);
            if (oldest.texture != null) Destroy(oldest.texture);
            return true;
        }

        void SceneChanged(Scene previous, Scene current)
        {
            string name = current.name;
            menu = name == "MainMenu" || name == "PreStart" || name == "Garage" || name == "RacingMainMenu" ||
                name == "RacingModeSelect" || name == "RacingSelect" || name == "RacingGarage";
        }

        void Update()
        {
            // The race path returns before enumeration, rendering, mesh construction or allocations.
            if (!menu || RacingProgressionService.IsRaceActive || Time.unscaledTime < nextCapture) return;
            Entry requested = null;
            foreach (var entry in entries.Values)
                if (entry.texture == null && !entry.failed && entry.viewers.Count > 0 &&
                    (requested == null || entry.lastUse > requested.lastUse)) requested = entry;
            if (requested == null) return;
            nextCapture = Time.unscaledTime + .05f;
            using (CaptureMarker.Auto())
            {
                try { Capture(requested); }
                catch (Exception error) { requested.failed = true; Debug.LogWarning("Combo preview unavailable: " + error.Message); }
            }
        }

        void EnsureStage()
        {
            if (stage != null) return;
            stage = new GameObject("Single combo capture stage");
            stage.transform.SetParent(transform, false);
            stage.transform.localPosition = new Vector3(10000, -10000, 10000);
            var cameraRoot = new GameObject("Disabled combo capture camera");
            cameraRoot.transform.SetParent(stage.transform, false);
            captureCamera = cameraRoot.AddComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.orthographic = true;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = RacingDesign.Background;
            captureCamera.nearClipPlane = .1f;
            captureCamera.farClipPlane = 30;
            captureCamera.cullingMask = 1 << CaptureLayer;
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            target = new RenderTexture(Width, Height, 16, RenderTextureFormat.ARGB32) { name = "Shared combo capture target", antiAliasing = 1 };
            target.Create();
            captureCamera.targetTexture = target;
            AddLight("Combo key", new Vector3(3, 5, 5), Color.white, 2.4f);
            AddLight("Combo fill", new Vector3(-4, 3, 2), new Color(.68f, .85f, 1), 1.2f);
            foreach (var part in stage.GetComponentsInChildren<Transform>()) part.gameObject.layer = CaptureLayer;
            stage.SetActive(false);
        }

        void AddLight(string name, Vector3 position, Color color, float intensity)
        {
            var root = new GameObject(name);
            root.transform.SetParent(stage.transform, false);
            root.transform.localPosition = position;
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 20;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << CaptureLayer;
        }

        void Capture(Entry entry)
        {
            EnsureStage();
            GameObject subject = null;
            Texture2D bitmap = null;
            var previous = RenderTexture.active;
            try
            {
                stage.SetActive(true);
                subject = new GameObject("Capture " + entry.key);
                subject.transform.SetParent(stage.transform, false);
                bool meta = entry.vehicle == MetaProgressionConfig.MetaVehicleId || entry.vehicle == "formula-01";
                Color paint = meta ? RacingVehicleFactory.MetaNavy : entry.vehicle == "car-01" ? new Color(.95f, .45f, .10f) : RacingDesign.Cyan;
                string wheel = entry.wheel == MetaProgressionConfig.MetaWheelId ? MetaProgressionConfig.MetaWheelVisualId : entry.wheel;
                var visual = RacingVehicleVisuals.Build(subject, entry.vehicle, wheel, paint, preview: true, isHuman: meta);
                RacingLoadoutVisuals.Apply(visual, entry.driver, entry.accessory);
                visual.enabled = false;
                foreach (var part in subject.GetComponentsInChildren<Transform>()) part.gameObject.layer = CaptureLayer;
                var renderers = subject.GetComponentsInChildren<MeshRenderer>();
                var bounds = new Bounds(subject.transform.position, Vector3.zero);
                foreach (var renderer in renderers) if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
                captureCamera.transform.position = bounds.center + new Vector3(6, 3.4f, 8);
                captureCamera.transform.LookAt(bounds.center);
                float halfX = 0, halfY = 0;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    point = captureCamera.transform.InverseTransformPoint(point);
                    halfX = Mathf.Max(halfX, Mathf.Abs(point.x)); halfY = Mathf.Max(halfY, Mathf.Abs(point.y));
                }
                captureCamera.aspect = (float)Width / Height;
                captureCamera.orthographicSize = Mathf.Max(.5f, Mathf.Max(halfY, halfX / captureCamera.aspect) * 1.08f);
                captureCamera.Render();
                RenderTexture.active = target;
                bitmap = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { name = "Combo / " + entry.key, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                bitmap.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                bitmap.Apply(false, true);
                entry.texture = bitmap; bitmap = null; generated++;
                foreach (var viewer in entry.viewers) if (viewer != null) viewer.Accept(entry.texture);
            }
            finally
            {
                RenderTexture.active = previous;
                if (subject != null) { subject.SetActive(false); Destroy(subject); }
                stage.SetActive(false);
                if (bitmap != null) Destroy(bitmap);
            }
        }

        void OnDestroy()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            foreach (var entry in entries.Values) if (entry.texture != null) Destroy(entry.texture);
            if (target != null) { target.Release(); Destroy(target); }
            if (instance == this) instance = null;
        }
    }
}
