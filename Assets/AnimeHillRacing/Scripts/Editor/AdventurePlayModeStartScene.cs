using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    /// <summary>
    /// Makes the authored Adventure menu the predictable entry point when Play Mode is started
    /// from any scene. Automated Play Mode verifiers can opt into their own scene for one run.
    /// </summary>
    [InitializeOnLoad]
    public static class AdventurePlayModeStartScene
    {
        private const string AdventureMenuScenePath =
            "Assets/AnimeHillRacing/Scenes/PreStart.unity";

        static AdventurePlayModeStartScene()
        {
            EditorApplication.delayCall += RestoreAdventureMenu;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public static void UseSceneForNextPlayMode(string scenePath)
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (scene == null)
            {
                Debug.LogError("PLAY_MODE_START_SCENE_MISSING|scene=" + scenePath);
                return;
            }

            EditorSceneManager.playModeStartScene = scene;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                RestoreAdventureMenu();
        }

        private static void RestoreAdventureMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            UseSceneForNextPlayMode(AdventureMenuScenePath);
        }
    }
}
