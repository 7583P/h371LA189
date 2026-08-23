using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    /// <summary>Keeps the first catalog driver as the visual rig model for every future character.</summary>
    public sealed class DriverCanonicalModelEditor : AssetPostprocessor
    {
        private static bool queued;
        private static bool applying;

        [InitializeOnLoadMethod]
        private static void QueueOnEditorLoad() => QueueNormalization();

        [MenuItem("Anime Hill Racing/Vehicles/Normalize Character Models")]
        public static void NormalizeNow()
        {
            queued = false;
            if (applying) return;
            applying = true;
            try
            {
                bool changed = false;
                string[] catalogGuids = AssetDatabase.FindAssets("t:GameCatalog");
                for (int catalogIndex = 0; catalogIndex < catalogGuids.Length; catalogIndex++)
                {
                    GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(
                        AssetDatabase.GUIDToAssetPath(catalogGuids[catalogIndex]));
                    if (catalog == null || catalog.drivers == null || catalog.drivers.Length == 0) continue;
                    DriverDefinition canonical = catalog.drivers[0];
                    if (canonical == null) continue;

                    if (canonical.poseReference != null)
                    {
                        Undo.RecordObject(canonical, "Set Default Character Model");
                        canonical.poseReference = null;
                        EditorUtility.SetDirty(canonical);
                        changed = true;
                    }

                    for (int i = 1; i < catalog.drivers.Length; i++)
                    {
                        DriverDefinition driver = catalog.drivers[i];
                        if (driver == null || driver.poseReference == canonical) continue;
                        Undo.RecordObject(driver, "Assign Default Character Model");
                        driver.poseReference = canonical;
                        EditorUtility.SetDirty(driver);
                        changed = true;
                    }
                }

                if (changed) AssetDatabase.SaveAssets();
            }
            finally
            {
                applying = false;
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (ContainsRelevantAsset(importedAssets) || ContainsRelevantAsset(movedAssets))
                QueueNormalization();
        }

        private static bool ContainsRelevantAsset(string[] paths)
        {
            if (paths == null) return false;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i].Replace('\\', '/');
                if (!path.EndsWith(".asset")) continue;
                if (AssetDatabase.LoadAssetAtPath<GameCatalog>(path) != null ||
                    AssetDatabase.LoadAssetAtPath<DriverDefinition>(path) != null)
                    return true;
            }
            return false;
        }

        private static void QueueNormalization()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += NormalizeNow;
        }
    }
}
