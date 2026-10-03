#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public static class RacingProductValidation
    {
        const string ActiveKey = "AHR.Racing.ProductAudit.Active";
        const string BaselineKey = "AHR.Racing.ProductAudit.Baseline";
        static RacingProductValidation() { EditorApplication.update += Tick; }
        public static void Baseline() { Begin(true); }
        public static void Run() { Begin(false); }
        public static void HealScenes()
        {
            foreach (string name in new[] { "Racing3D", "RacingGarage", "RacingSelect" })
            {
                string path = "Assets/AnimeHillRacing/Scenes/" + name + ".unity";
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                EditorSceneManager.SaveScene(scene);
            }
        }
        static void Begin(bool baseline)
        {
            if (!baseline) HealScenes();
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(BaselineKey, baseline);
            SessionState.SetBool("AHR.Racing.ProductAudit.Started", false);
            EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            AdventurePlayModeStartScene.UseSceneForNextPlayMode("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (EditorApplication.isPlaying && !SessionState.GetBool("AHR.Racing.ProductAudit.Started", false))
            {
                SessionState.SetBool("AHR.Racing.ProductAudit.Started", true);
                var probe = new GameObject("RacingProductAudit").AddComponent<RacingPlayModeProbe>();
                probe.Begin(SessionState.GetBool(BaselineKey, false));
            }
            if (SessionState.GetBool("AHR.Racing.ProductAudit.Done", false))
            {
                SessionState.SetBool(ActiveKey, false);
                SessionState.SetBool("AHR.Racing.ProductAudit.Done", false);
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(SessionState.GetInt("AHR.Racing.ProductAudit.Exit", 1));
            }
        }
    }
}
#endif
