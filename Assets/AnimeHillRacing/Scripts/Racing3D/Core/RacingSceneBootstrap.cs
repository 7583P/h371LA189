using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-100)]
    public sealed class RacingSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool autoConfigureIfEmpty = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBootstrap()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        private static void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => EnsureSceneComponents(scene.name);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnInitialSceneLoaded()
        {
            EnsureSceneComponents(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private static void EnsureSceneComponents(string sceneName)
        {
            if (sceneName == "Racing3D" || sceneName == "TestCircuit3D")
            {
                if (FindFirstObjectByType<RacingSceneBootstrap>() == null && FindFirstObjectByType<RaceManager>() == null)
                {
                    GameObject bootObj = new GameObject("RacingSceneBootstrap");
                    bootObj.AddComponent<RacingSceneBootstrap>();
                }
            }
            else if (sceneName == "RacingGarage")
            {
                if (FindFirstObjectByType<RacingGarageUI>() == null)
                {
                    GameObject garageObj = new GameObject("RacingGarageRoot");
                    garageObj.AddComponent<RacingGarageUI>();
                }
            }
            else if (sceneName == "RacingSelect")
            {
                if (FindFirstObjectByType<RacingModeSelectUI>() == null)
                {
                    GameObject selectObj = new GameObject("RacingSelectRoot");
                    selectObj.AddComponent<RacingModeSelectUI>();
                }
            }
            else if ((sceneName == "MainMenu" || sceneName == "PreStart") && !AnimeHillRacing.Core.GameSession.IsAdventureSelectionRequested)
            {
                if (FindFirstObjectByType<RacingMainMenuUI>() == null) new GameObject("Hill Adventurer presentation").AddComponent<RacingMainMenuUI>();
            }
        }


        private void Awake()
        {
            if (!autoConfigureIfEmpty) return;

            // 1. Check or create Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            var racingCam = mainCam.GetComponent<RacingCamera3D>();
            if (racingCam == null) racingCam = mainCam.gameObject.AddComponent<RacingCamera3D>();

            // 2. Check or create Directional Light
            Light sun = FindFirstObjectByType<Light>();
            if (sun == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.2f;
                sun.color = new Color(1f, 0.96f, 0.9f);
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // 3. Check or create Circuit Builder
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TestCircuit3D")
            {
                if (FindFirstObjectByType<TestCircuit3DBuilder>() == null) new GameObject("TestCircuit3D").AddComponent<TestCircuit3DBuilder>();
            }
            else if (FindFirstObjectByType<CanyonRushBuilder>() == null) new GameObject("Canyon Rush").AddComponent<CanyonRushBuilder>();
            if (FindFirstObjectByType<RacingQualityScope>() == null) gameObject.AddComponent<RacingQualityScope>();
            RacingFeedback.Ensure();RacingVfxPool.Ensure();RacingItemPool.ForScene();

            // 4. Check or create TrackProgressTracker
            var tracker = FindFirstObjectByType<TrackProgressTracker>();
            if (tracker == null)
            {
                GameObject trackerObj = new GameObject("TrackProgressTracker");
                tracker = trackerObj.AddComponent<TrackProgressTracker>();
            }

            // 5. Check or create RaceManager
            var manager = FindFirstObjectByType<RaceManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("RaceManager");
                manager = managerObj.AddComponent<RaceManager>();
            }

            // 6. Check or create UI overlays
            var hud = FindFirstObjectByType<RacingHUD>();
            if (hud == null)
            {
                GameObject hudObj = new GameObject("RacingHUD");
                hudObj.AddComponent<RacingHUD>();
            }

            var resultsUI = FindFirstObjectByType<RacingResultsUI>();
            if (resultsUI == null)
            {
                GameObject resObj = new GameObject("RacingResultsUI");
                resObj.AddComponent<RacingResultsUI>();
            }

            var debugOverlay = FindFirstObjectByType<RacingDebugOverlay>();
            if (debugOverlay == null)
            {
                GameObject debugObj = new GameObject("RacingDebugOverlay");
                debugObj.AddComponent<RacingDebugOverlay>();
            }
            if (FindFirstObjectByType<RacingRaceMetrics>() == null) gameObject.AddComponent<RacingRaceMetrics>();

            Debug.Log("[RacingSceneBootstrap] Scene components verified and bootstrapped successfully.");
        }
    }
}
