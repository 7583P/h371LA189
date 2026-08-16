using System.Collections.Generic;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    [InitializeOnLoad]
    public static class VehicleVisualNormalizationEditor
    {
        private const byte AlphaThreshold = 8;
        private static readonly HashSet<string> ForcedSpritePaths = new HashSet<string>();
        private static bool scheduled;
        private static bool running;

        static VehicleVisualNormalizationEditor() => Schedule(false, null);

        [MenuItem("Anime Hill Racing/Vehicles/Normalize Visible Skin Sizes")]
        public static void NormalizeAllFromMenu()
        {
            NormalizeAll(true);
            Debug.Log("VEHICLE_VISIBLE_SIZE_NORMALIZATION_PASS: alpha-visible bounds cached for all body and wheel skins.");
        }

        private static void Schedule(bool force, IEnumerable<string> paths)
        {
            if (paths != null)
                foreach (string path in paths)
                    if (!string.IsNullOrEmpty(path)) ForcedSpritePaths.Add(path.Replace('\\', '/'));
            if (force) ForcedSpritePaths.Add("*");
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += () =>
            {
                scheduled = false;
                if (!EditorApplication.isPlayingOrWillChangePlaymode) NormalizeAll(false);
            };
        }

        public static void NormalizeAll(bool forceAll)
        {
            if (running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            running = true;
            try
            {
                bool wildcard = forceAll || ForcedSpritePaths.Contains("*");
                string[] definitionGuids = AssetDatabase.FindAssets("t:VehicleDefinition");
                for (int i = 0; i < definitionGuids.Length; i++)
                {
                    string definitionPath = AssetDatabase.GUIDToAssetPath(definitionGuids[i]);
                    VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(definitionPath);
                    if (definition == null) continue;
                    bool changed = NormalizeOptions(definition.skins, wildcard) |
                                   NormalizeOptions(definition.wheels, wildcard);
                    if (changed) EditorUtility.SetDirty(definition);
                }
                if (GUIUtility.hotControl == 0) AssetDatabase.SaveAssets();
                ForcedSpritePaths.Clear();
            }
            finally
            {
                running = false;
            }
        }

        private static bool NormalizeOptions(VehicleDefinition.CustomizationOption[] options, bool forceAll)
        {
            if (options == null) return false;
            bool changed = false;
            for (int i = 0; i < options.Length; i++)
            {
                VehicleDefinition.CustomizationOption option = options[i];
                if (option == null || option.sprite == null) continue;
                string spritePath = AssetDatabase.GetAssetPath(option.sprite).Replace('\\', '/');
                bool forced = forceAll || ForcedSpritePaths.Contains(spritePath);
                if (!forced && option.visibleSize.x > .001f && option.visibleSize.y > .001f) continue;
                Vector2 measured = MeasureAlphaVisibleSize(option.sprite);
                if (measured.x <= .001f || measured.y <= .001f)
                    throw new System.InvalidOperationException($"{option.optionId}: sprite has no visible alpha content.");
                if ((option.visibleSize - measured).sqrMagnitude <= .00000001f) continue;
                option.visibleSize = measured;
                changed = true;
            }
            return changed;
        }

        public static Vector2 MeasureAlphaVisibleSize(Sprite sourceSprite)
        {
            if (sourceSprite == null) return Vector2.zero;
            string path = AssetDatabase.GetAssetPath(sourceSprite);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            bool restoreReadable = importer != null && !importer.isReadable;
            if (restoreReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                sourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            try
            {
                if (sourceSprite == null || sourceSprite.texture == null || !sourceSprite.texture.isReadable)
                    return sourceSprite != null ? sourceSprite.bounds.size : Vector2.zero;
                Rect rect = sourceSprite.textureRect;
                int startX = Mathf.RoundToInt(rect.x);
                int startY = Mathf.RoundToInt(rect.y);
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);
                Color32[] pixels = sourceSprite.texture.GetPixels32();
                int textureWidth = sourceSprite.texture.width;
                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (startY + y) * textureWidth + startX + x;
                    if (pixelIndex < 0 || pixelIndex >= pixels.Length || pixels[pixelIndex].a <= AlphaThreshold)
                        continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX || maxY < minY) return Vector2.zero;
                return new Vector2(maxX - minX + 1, maxY - minY + 1) /
                       Mathf.Max(.001f, sourceSprite.pixelsPerUnit);
            }
            finally
            {
                if (restoreReadable)
                {
                    importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        importer.isReadable = false;
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private sealed class VehicleSpritePostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
            {
                var affected = new List<string>();
                bool definitionChanged = false;
                Collect(importedAssets, affected, ref definitionChanged);
                Collect(movedAssets, affected, ref definitionChanged);
                if (affected.Count > 0 || definitionChanged) Schedule(false, affected);
            }

            private static void Collect(string[] paths, List<string> affected, ref bool definitionChanged)
            {
                if (paths == null) return;
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i].Replace('\\', '/');
                    if (path.StartsWith("Assets/Art/Vehicles/") && path.EndsWith(".png")) affected.Add(path);
                    if (path.StartsWith("Assets/AnimeHillRacing/VehicleData/") && path.EndsWith(".asset"))
                        definitionChanged = true;
                }
            }
        }
    }
}
