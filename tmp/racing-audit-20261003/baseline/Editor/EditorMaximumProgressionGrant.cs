#if UNITY_EDITOR
using AnimeHillRacing.Core;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    [InitializeOnLoad]
    public static class EditorMaximumProgressionGrant
    {
        private const string CatalogPath =
            "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private const string SessionGrantKey =
            "AHR.EditorMaximumProgressionGrant.Session.V2";

        static EditorMaximumProgressionGrant()
        {
            EditorApplication.delayCall += GrantOncePerEditorSession;
        }

        [MenuItem("Anime Hill Racing/Development/Grant Everything Maxed And Unlocked")]
        public static void GrantFromMenu()
        {
            if (!TryGrant(out SaveManager.EditorMaximumProgressionResult result)) return;
            SessionState.SetBool(SessionGrantKey, true);
            EditorUtility.DisplayDialog("Anime Hill Racing",
                "Editor progression granted.\n\n" + result, "OK");
        }

        private static void GrantOncePerEditorSession()
        {
            if (SessionState.GetBool(SessionGrantKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += GrantOncePerEditorSession;
                return;
            }

            if (TryGrant(out _)) SessionState.SetBool(SessionGrantKey, true);
        }

        private static bool TryGrant(out SaveManager.EditorMaximumProgressionResult result)
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
            {
                result = default;
                Debug.LogError("EDITOR_MAXIMUM_PROGRESSION_FAILED: GameCatalog was not found at " +
                               CatalogPath + ".");
                return false;
            }

            result = SaveManager.GrantEditorMaximumProgression(catalog);
            Debug.Log("EDITOR_MAXIMUM_PROGRESSION_GRANTED: " + result +
                      ". Race records were preserved.");
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            return true;
        }
    }
}
#endif
