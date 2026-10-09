using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-900)]
    public sealed class RacingProgressionHost : MonoBehaviour
    {
        static RacingProgressionHost instance;
        float nextClockRefresh;
        static readonly ProfilerMarker PumpMarker=new ProfilerMarker("Racing.League.Pump");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create() {
            if (instance != null) return;
            var root = new GameObject("Local Racing seasons and league"); DontDestroyOnLoad(root);
            instance = root.AddComponent<RacingProgressionHost>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            root.AddComponent<RacingLeagueDiagnostics>();
#endif
            RacingProgressionService.EnsureInitialized();
            SceneManager.activeSceneChanged += instance.SceneChanged;
            instance.SceneChanged(default,SceneManager.GetActiveScene());
        }
        void SceneChanged(Scene previous,Scene current) {
            string name=current.name;
            bool menu=name=="MainMenu" || name=="PreStart" || name=="Garage" || name=="RacingMainMenu" || name=="RacingModeSelect" || name=="RacingSelect" || name=="RacingGarage" || name=="Loading";
            RacingProgressionService.SetExternalGameplay(!menu);
        }
        void Update() {
            if(RacingProgressionService.IsRaceActive) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                long before=GC.GetAllocatedBytesForCurrentThread();
#endif
                PumpMarker.Begin();RacingProgressionService.Pump();PumpMarker.End();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RacingProgressionService.ObserveRacePump(GC.GetAllocatedBytesForCurrentThread()-before);
#endif
                return;
            }
            if (Time.unscaledTime >= nextClockRefresh) { nextClockRefresh = Time.unscaledTime + 30; RacingProgressionService.Resume(DateTime.UtcNow.Ticks); }
            PumpMarker.Begin();RacingProgressionService.Pump();PumpMarker.End();
        }
        void OnApplicationPause(bool paused) { if (paused) RacingProgressionService.Save(true); else RacingProgressionService.Resume(DateTime.UtcNow.Ticks); }
        void OnApplicationFocus(bool focused) { if (focused) RacingProgressionService.Resume(DateTime.UtcNow.Ticks); }
        void OnApplicationQuit() { RacingProgressionService.Save(true); }
        void OnDestroy() { if (instance == this) { SceneManager.activeSceneChanged -= SceneChanged; instance = null; RacingProgressionService.Save(true); } }
    }
}
