#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingAndroidTextureOptimization
    {
        [Serializable] sealed class AssetRow { public string path, type; public long packedBytes; }
        [Serializable] sealed class BuildSize { public AssetRow[] assets; }
        [Serializable] sealed class Change { public string path, format; public int maxSize; public long originalPackedBytes; }
        [Serializable] sealed class Changes { public List<Change> changes = new List<Change>(); }
        [Serializable] sealed class Geometry { public Vector3 meshBounds; public Vector2 rectWorld, normalizedPivot; }
        [Serializable] sealed class GeometryComparison { public string path, sprite; public Geometry before, after; public bool retained; }
        [Serializable] sealed class GeometryReport { public List<GeometryComparison> comparisons = new List<GeometryComparison>(); }
        static Geometry Describe(Sprite sprite) => new Geometry { meshBounds = sprite.bounds.size,
            rectWorld = sprite.rect.size / sprite.pixelsPerUnit,
            normalizedPivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height) };
        static string Output => Path.GetFullPath(RacingFrameForensics.Argument("--texture-optimization-output", RacingFrameForensics.Argument("--closure-output", "Logs/RCPass/20261012/texture-optimization")));
        public static void ValidateAndBuildSizeBatch()
        {
            ValidateBatch();
            RacingRcEvidenceBuilder.BuildAndroidSizeBatch();
        }
        public static void ApplyBatch()
        {
            string reportPath = RacingFrameForensics.Argument("--size-baseline-report", "Logs/RCPass/20261012/android-size-baseline/build-size.json");
            var baseline = JsonUtility.FromJson<BuildSize>(File.ReadAllText(reportPath));
            Directory.CreateDirectory(Output);
            var evidence = new Changes();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var row in baseline.assets)
                {
                    if (row.type != "UnityEngine.Texture2D" || row.packedBytes < 128 * 1024 || !row.path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                    // Keep text glyphs, application icons and lookup/technical textures exact.
                    if (row.path.Contains("Glyph") || row.path.Contains("HillAdventurerIcon") || row.path.Contains("Lookup") || row.path.Contains("Lightmap")) continue;
                    var texture = AssetImporter.GetAtPath(row.path) as TextureImporter;
                    if (texture == null || texture.textureShape != TextureImporterShape.Texture2D || texture.textureType == TextureImporterType.NormalMap) continue;
                    var android = texture.GetPlatformTextureSettings("Android");
                    string backup = Path.Combine(Output, "before", row.path + ".meta");
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    if (File.Exists(backup)) throw new InvalidOperationException("Refusing to overwrite texture baseline: " + row.path);
                    File.Copy(row.path + ".meta", backup);
                    int max = android.overridden ? android.maxTextureSize : texture.maxTextureSize;
                    bool environment = row.path.Contains("/Backgrounds/") || row.path.Contains("/Terrain/") || row.path.Contains("/LastHorizon/Background/") || row.path.EndsWith("NeonDepthCity.png", StringComparison.Ordinal);
                    bool vehicle = row.path.Contains("/Vehicles/") || row.path.Contains("/Characters/");
                    android.overridden = true;
                    android.maxTextureSize = max;
                    android.format = environment ? TextureImporterFormat.ASTC_6x6 : vehicle ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_4x4;
                    android.compressionQuality = 80;
                    android.textureCompression = TextureImporterCompression.CompressedHQ;
                    texture.SetPlatformTextureSettings(android);
                    AssetDatabase.WriteImportSettingsIfDirty(row.path);
                    evidence.changes.Add(new Change { path = row.path, format = android.format.ToString(), maxSize = max, originalPackedBytes = row.packedBytes });
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            File.WriteAllText(Path.Combine(Output, "changes.json"), JsonUtility.ToJson(evidence, true));
            AssetDatabase.Refresh();
            Debug.Log("ANDROID_ASTC_MEASURED_BATCH count=" + evidence.changes.Count + "; no source pixels, dimensions, alpha, mips, sprites or references changed");
        }
        public static void ApplyRecipeBatch()
        {
            string recipe = RacingFrameForensics.Argument("--texture-recipe");
            if (string.IsNullOrEmpty(recipe)) throw new ArgumentException("Measured texture recipe required");
            var changes = JsonUtility.FromJson<Changes>(File.ReadAllText(recipe));
            Directory.CreateDirectory(Output);
            var applied = new Changes(); var geometry = new GeometryReport();
            foreach (var change in changes.changes)
            {
                var importer = AssetImporter.GetAtPath(change.path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Not a texture: " + change.path);
                string backup = Path.Combine(Output, "before", change.path + ".meta");
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (File.Exists(backup)) throw new InvalidOperationException("Refusing to overwrite recipe baseline: " + change.path);
                File.Copy(change.path + ".meta", backup);
                var android = importer.GetPlatformTextureSettings("Android");
                var previous = importer.GetPlatformTextureSettings("Android");
                var bounds = AssetDatabase.LoadAllAssetsAtPath(change.path).OfType<Sprite>()
                    .ToDictionary(sprite => sprite.name, Describe);
                android.overridden = true;
                android.maxTextureSize = change.maxSize;
                android.format = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), change.format);
                android.compressionQuality = 80;
                android.textureCompression = android.format == TextureImporterFormat.RGBA32 || android.format == TextureImporterFormat.RGB24
                    ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
                bool safe = true;
                bool ui = change.path.Contains("/Buttons/") || change.path.Contains("/Interface/") ||
                    change.path.Contains("/Art/Game/") || change.path.Contains("/Resources/UI/") ||
                    change.path.Contains("/Art/TrackPreviews/") || change.path.Contains("/LastHorizon/Preview/");
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(change.path).OfType<Sprite>())
                {
                    var after = Describe(sprite);
                    if (!bounds.TryGetValue(sprite.name, out var before)) { safe = false; continue; }
                    // UI uses fixed Canvas rects. A maximum 0.25% aspect rounding
                    // is below one displayed pixel for the measured small cards.
                    // World sprites must retain exact mesh bounds; no relaxation.
                    bool equal = ui
                        ? Mathf.Abs(after.rectWorld.x / before.rectWorld.x - 1) <= .0025f &&
                          Mathf.Abs(after.rectWorld.y / before.rectWorld.y - 1) <= .0025f &&
                          (after.normalizedPivot - before.normalizedPivot).magnitude <= .001f
                        : (after.meshBounds - before.meshBounds).magnitude <= .001f;
                    safe &= equal;
                    geometry.comparisons.Add(new GeometryComparison { path = change.path, sprite = sprite.name, before = before, after = after, retained = equal });
                }
                if (safe) applied.changes.Add(change);
                else
                {
                    importer.SetPlatformTextureSettings(previous);
                    importer.SaveAndReimport();
                    Debug.Log("Measured texture recipe restored unsafe geometry: " + change.path);
                }
            }
            File.WriteAllText(Path.Combine(Output, "changes.json"), JsonUtility.ToJson(applied, true));
            File.WriteAllText(Path.Combine(Output, "sprite-geometry.json"), JsonUtility.ToJson(geometry, true));
        }
        // GPU readback compares each full-resolution Android imported texture with
        // its unchanged source. The project must be opened with buildTarget Android.
        public static void ValidateBatch()
        {
            var evidence = JsonUtility.FromJson<Changes>(File.ReadAllText(Path.Combine(Output, "changes.json")));
            Directory.CreateDirectory(Path.Combine(Output, "previews"));
            using (var csv = new StreamWriter(Path.Combine(Output, "quality.csv")))
            {
                csv.WriteLine("asset,width,height,format,rgb_psnr_db,alpha_mae,alpha_max_error,original_width,original_height");
                int saved = 0;
                foreach (var change in evidence.changes)
                {
                    var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(change.path);
                    if (imported == null) continue;
                    var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(source, File.ReadAllBytes(change.path))) { UnityEngine.Object.DestroyImmediate(source); continue; }
                    int originalWidth = source.width, originalHeight = source.height;
                    var originalPixels = Read(source, imported.width, imported.height);
                    var optimizedPixels = Read(imported, imported.width, imported.height);
                    var a = originalPixels.GetPixels32(); var b = optimizedPixels.GetPixels32();
                    double square = 0, alpha = 0; int maxAlpha = 0, count = 0;
                    for (int i = 0; i < a.Length; i++)
                    {
                        int da = Math.Abs(a[i].a - b[i].a); alpha += da; maxAlpha = Math.Max(maxAlpha, da);
                        if (a[i].a < 8 && b[i].a < 8) continue;
                        square += (a[i].r - b[i].r) * (a[i].r - b[i].r) + (a[i].g - b[i].g) * (a[i].g - b[i].g) + (a[i].b - b[i].b) * (a[i].b - b[i].b);
                        count += 3;
                    }
                    double psnr = square > 0 ? 10 * Math.Log10(255.0 * 255 * count / square) : 99;
                    csv.WriteLine(FormattableString.Invariant($"\"{change.path}\",{imported.width},{imported.height},{imported.format},{psnr},{alpha / (255 * a.Length)},{maxAlpha / 255.0},{originalWidth},{originalHeight}"));
                    // Full images for representative UI/vehicles/environments and every low-PSNR outlier.
                    if (saved < 12 || psnr < 32 || maxAlpha > 64)
                    {
                        string name = Path.GetFileNameWithoutExtension(change.path) + "-" + saved++;
                        File.WriteAllBytes(Path.Combine(Output, "previews", name + "-source.png"), originalPixels.EncodeToPNG());
                        File.WriteAllBytes(Path.Combine(Output, "previews", name + "-android.png"), optimizedPixels.EncodeToPNG());
                    }
                    UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(originalPixels); UnityEngine.Object.DestroyImmediate(optimizedPixels);
                }
            }
        }
        static Texture2D Read(Texture texture, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var old = RenderTexture.active;
            Graphics.Blit(texture, target); RenderTexture.active = target;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply();
            RenderTexture.active = old; RenderTexture.ReleaseTemporary(target);
            return result;
        }
    }
}
#endif
