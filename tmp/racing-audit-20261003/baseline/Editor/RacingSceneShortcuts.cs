#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.EditorScripts
{
    public static class RacingSceneShortcuts
    {
        private const string RacingSelectScene = "Assets/AnimeHillRacing/Scenes/RacingSelect.unity";
        private const string Racing3DScene = "Assets/AnimeHillRacing/Scenes/Racing3D.unity";
        private const string RacingGarageScene = "Assets/AnimeHillRacing/Scenes/RacingGarage.unity";
        private const string PreStartScene = "Assets/AnimeHillRacing/Scenes/PreStart.unity";

        [MenuItem("Hill Adventurer/Racing 3D/🏁 Open Racing 3D Circuit", priority = 10)]
        public static void OpenRacing3D()
        {
            OpenScene(Racing3DScene);
        }

        [MenuItem("Hill Adventurer/Racing 3D/🎮 Open Racing Select Menu", priority = 11)]
        public static void OpenRacingSelect()
        {
            OpenScene(RacingSelectScene);
        }

        [MenuItem("Hill Adventurer/Racing 3D/🚗 Open Racing Garage", priority = 12)]
        public static void OpenRacingGarage()
        {
            OpenScene(RacingGarageScene);
        }

        [MenuItem("Hill Adventurer/Racing 3D/🏔️ Back to Adventure PreStart", priority = 30)]
        public static void OpenAdventurePreStart()
        {
            OpenScene(PreStartScene);
        }

        [MenuItem("Hill Adventurer/Racing 3D/▶ Play Racing 3D Now", priority = 0)]
        public static void PlayRacing3DNow()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            if (OpenScene(RacingSelectScene))
            {
                EditorApplication.isPlaying = true;
            }
        }

        private static bool OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log($"[Racing 3D] Scene loaded: {scenePath}");
                return true;
            }
            return false;
        }
    }
}
#endif
