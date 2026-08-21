using System;
using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    /// <summary>
    /// Re-entrant Editor driver for the real Adventure Play Mode Phase 0F audit. SessionState lets the
    /// Jeep -> Hill sequence continue across the domain reloads caused by entering and leaving Play Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class AdventurePhysics2DPlayModeRunner
    {
        private const string MenuPath = "Anime Hill Racing/Box2D/Validate Jeep + Hill Play Mode";
        private const string AdventureScenePath = "Assets/AnimeHillRacing/Scenes/Adventure2_5D.unity";
        private const string RequestPath = "Temp/RunAdventurePhysics2DValidation.request";
        private const string SessionPrefix = "AHR.AdventurePhysics2DPlayMode.";
        private const string ActiveKey = SessionPrefix + "Active";
        private const string PhaseKey = SessionPrefix + "Phase";
        private const string VehicleIndexKey = SessionPrefix + "VehicleIndex";
        private const string OriginalVehicleKey = SessionPrefix + "OriginalVehicle";
        private const string OriginalMapKey = SessionPrefix + "OriginalMap";
        private const string OriginalModeKey = SessionPrefix + "OriginalMode";
        private const string OriginalSceneKey = SessionPrefix + "OriginalScene";
        private const string ResultsKey = SessionPrefix + "Results";
        private const string AllPassedKey = SessionPrefix + "AllPassed";
        private const string PhaseStartedKey = SessionPrefix + "PhaseStartedUtcTicks";

        private const int PhaseLaunching = 1;
        private const int PhaseRunning = 2;
        private const int PhaseStopping = 3;
        private const int PhaseAborting = 4;

        private static readonly string[] VehicleIds =
        {
            "car-01",
            "hill-adventurer-01",
            "formula-01",
            "rally-01",
            "sport-bike-01"
        };

        static AdventurePhysics2DPlayModeRunner()
        {
            RehookUpdate();
            EditorApplication.update -= ConsumeRunRequest;
            EditorApplication.update += ConsumeRunRequest;
        }

        private static void ConsumeRunRequest()
        {
            if (!File.Exists(RequestPath))
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorApplication.update -= ConsumeRunRequest;
            File.Delete(RequestPath);
            StartAudit();
        }

        [MenuItem(MenuPath)]
        public static void RunFromMenu()
        {
            StartAudit();
        }

        /// <summary>Entry point suitable for Unity -executeMethod. Do not add -quit; the runner exits itself.</summary>
        public static void RunFromCommandLine()
        {
            StartAudit();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRunFromMenu()
        {
            return !SessionState.GetBool(ActiveKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling;
        }

        private static void StartAudit()
        {
            if (SessionState.GetBool(ActiveKey, false))
            {
                Debug.LogWarning("ADVENTURE_BOX2D_PLAY_AUDIT is already active.");
                return;
            }
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling)
            {
                FailToStart("Exit Play Mode and wait for compilation before starting the audit.");
                return;
            }
            if (!File.Exists(AdventureScenePath))
            {
                FailToStart("Adventure2_5D scene was not found at " + AdventureScenePath);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                FailToStart("The active scene has unsaved changes; save or discard them before the automated audit.");
                return;
            }

            SessionState.SetString(OriginalVehicleKey, SaveManager.SelectedVehicleId ?? string.Empty);
            SessionState.SetString(OriginalMapKey, SaveManager.SelectedMapId ?? string.Empty);
            SessionState.SetInt(OriginalModeKey, (int)SaveManager.SelectedGameMode);
            SessionState.SetString(OriginalSceneKey, activeScene.IsValid() ? activeScene.path ?? string.Empty : string.Empty);
            SessionState.SetString(ResultsKey, string.Empty);
            SessionState.SetBool(AllPassedKey, true);
            SessionState.SetInt(VehicleIndexKey, 0);
            SessionState.SetBool(ActiveKey, true);
            Application.runInBackground = true;
            Debug.Log($"ADVENTURE_BOX2D_PLAY_BEGIN|classification=REAL_ADVENTURE_PLAY_MODE|vehicles={string.Join(",", VehicleIds)}|simulation=FixedUpdate");
            RehookUpdate();
            BeginCurrentVehicle();
        }

        private static void BeginCurrentVehicle()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            int index = SessionState.GetInt(VehicleIndexKey, 0);
            if (index < 0 || index >= VehicleIds.Length)
            {
                FinishAudit();
                return;
            }

            try
            {
                string vehicleId = VehicleIds[index];
                SaveManager.SelectedVehicleId = vehicleId;
                SaveManager.SelectedMapId = "countryside";
                SaveManager.SelectedGameMode = GameModeType.Adventure2_5D;
                Application.runInBackground = true;
                EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
                SetPhase(PhaseLaunching);
                Debug.Log($"ADVENTURE_BOX2D_PLAY_VEHICLE_BEGIN|index={index}|vehicle={vehicleId}");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                AbortAudit("could not launch vehicle: " + exception.GetType().Name + ":" + exception.Message);
            }
        }

        private static void ContinueAudit()
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                EditorApplication.update -= ContinueAudit;
                return;
            }

            try
            {
                int phase = SessionState.GetInt(PhaseKey, 0);
                switch (phase)
                {
                    case 0:
                        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                            BeginCurrentVehicle();
                        break;
                    case PhaseLaunching:
                        ContinueLaunching();
                        break;
                    case PhaseRunning:
                        ContinueRunning();
                        break;
                    case PhaseStopping:
                        ContinueStopping();
                        break;
                    case PhaseAborting:
                        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                            FinishAudit();
                        break;
                }
            }
            catch (Exception exception)
            {
                AbortAudit("runner exception: " + exception.GetType().Name + ":" + exception.Message);
            }
        }

        private static void ContinueLaunching()
        {
            if (EditorApplication.isPlaying)
            {
                VehiclePhysics2DController vehicle = FindExpectedVehicle();
                if (vehicle == null)
                {
                    if (PhaseAgeSeconds() > 45d)
                        AbortAudit("expected VehiclePhysics2DController did not spawn within 45 seconds.");
                    return;
                }

                AdventurePhysics2DPlayModeProbe probe =
                    UnityEngine.Object.FindFirstObjectByType<AdventurePhysics2DPlayModeProbe>();
                if (probe == null)
                {
                    GameObject probeObject = new GameObject("Adventure Physics2D Play Mode Probe");
                    probe = probeObject.AddComponent<AdventurePhysics2DPlayModeProbe>();
                    probe.Configure(CurrentVehicleId());
                }
                SetPhase(PhaseRunning);
                return;
            }

            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
                if (PhaseAgeSeconds() > 60d)
                    AbortAudit("Unity did not enter Play Mode within 60 seconds.");
            }
        }

        private static void ContinueRunning()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    AbortAudit("Play Mode ended before the runtime probe completed.");
                return;
            }

            AdventurePhysics2DPlayModeProbe probe =
                UnityEngine.Object.FindFirstObjectByType<AdventurePhysics2DPlayModeProbe>();
            if (probe == null)
            {
                if (PhaseAgeSeconds() > 120d)
                    AbortAudit("runtime probe disappeared or did not initialize.");
                return;
            }
            if (!probe.Completed)
            {
                if (PhaseAgeSeconds() > 120d)
                    AbortAudit("runtime probe exceeded the 120 second timeout.");
                return;
            }

            AppendResult(probe.ResultLine);
            if (!probe.Passed)
            {
                SessionState.SetBool(AllPassedKey, false);
                AppendResult("ADVENTURE_BOX2D_PLAY_FAILURE|vehicle=" + CurrentVehicleId() + "|" +
                             probe.FailureSummary);
            }
            SetPhase(PhaseStopping);
            EditorApplication.isPlaying = false;
        }

        private static void ContinueStopping()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            int nextIndex = SessionState.GetInt(VehicleIndexKey, 0) + 1;
            SessionState.SetInt(VehicleIndexKey, nextIndex);
            if (nextIndex >= VehicleIds.Length)
            {
                FinishAudit();
                return;
            }

            BeginCurrentVehicle();
        }

        private static VehiclePhysics2DController FindExpectedVehicle()
        {
            VehiclePhysics2DController[] vehicles =
                UnityEngine.Object.FindObjectsByType<VehiclePhysics2DController>(FindObjectsSortMode.None);
            string expected = CurrentVehicleId();
            for (int i = 0; i < vehicles.Length; i++)
                if (vehicles[i] != null && vehicles[i].Definition != null &&
                    vehicles[i].Definition.vehicleId == expected) return vehicles[i];
            return null;
        }

        private static string CurrentVehicleId()
        {
            int index = Mathf.Clamp(SessionState.GetInt(VehicleIndexKey, 0), 0, VehicleIds.Length - 1);
            return VehicleIds[index];
        }

        private static void SetPhase(int phase)
        {
            SessionState.SetInt(PhaseKey, phase);
            SessionState.SetString(PhaseStartedKey, DateTime.UtcNow.Ticks.ToString());
        }

        private static double PhaseAgeSeconds()
        {
            if (!long.TryParse(SessionState.GetString(PhaseStartedKey, "0"), out long ticks) || ticks <= 0)
                return 0d;
            return Math.Max(0d, (DateTime.UtcNow.Ticks - ticks) / (double)TimeSpan.TicksPerSecond);
        }

        private static void AppendResult(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            string existing = SessionState.GetString(ResultsKey, string.Empty);
            SessionState.SetString(ResultsKey, string.IsNullOrEmpty(existing) ? line : existing + "\n" + line);
        }

        private static void AbortAudit(string reason)
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                FailToStart(reason);
                return;
            }

            string clean = Sanitize(reason);
            Debug.LogError("ADVENTURE_BOX2D_PLAY_ABORT|" + clean);
            AppendResult("ADVENTURE_BOX2D_PLAY_ABORT|" + clean);
            SessionState.SetBool(AllPassedKey, false);
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SetPhase(PhaseAborting);
                EditorApplication.isPlaying = false;
            }
            else
            {
                FinishAudit();
            }
        }

        private static void FinishAudit()
        {
            bool passed = SessionState.GetBool(AllPassedKey, false);
            string results = SessionState.GetString(ResultsKey, string.Empty);
            string originalVehicle = SessionState.GetString(OriginalVehicleKey, "car-01");
            string originalMap = SessionState.GetString(OriginalMapKey, "countryside");
            GameModeType originalMode = (GameModeType)SessionState.GetInt(OriginalModeKey,
                (int)GameModeType.Adventure2_5D);
            string originalScene = SessionState.GetString(OriginalSceneKey, string.Empty);

            try
            {
                SaveManager.SelectedVehicleId = originalVehicle;
                SaveManager.SelectedMapId = originalMap;
                SaveManager.SelectedGameMode = originalMode;
                if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
                    EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            catch (Exception exception)
            {
                passed = false;
                Debug.LogError("ADVENTURE_BOX2D_PLAY_RESTORE_FAILURE|" + exception.Message);
            }

            ClearSession();
            EditorApplication.update -= ContinueAudit;
            if (!string.IsNullOrEmpty(results)) Debug.Log(results);
            Debug.Log($"ADVENTURE_BOX2D_PLAY_END|passed={passed}|count={VehicleIds.Length}|" +
                      "simulation=FixedUpdate|manualSimulate=False");
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void ClearSession()
        {
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(PhaseKey);
            SessionState.EraseInt(VehicleIndexKey);
            SessionState.EraseString(OriginalVehicleKey);
            SessionState.EraseString(OriginalMapKey);
            SessionState.EraseInt(OriginalModeKey);
            SessionState.EraseString(OriginalSceneKey);
            SessionState.EraseString(ResultsKey);
            SessionState.EraseBool(AllPassedKey);
            SessionState.EraseString(PhaseStartedKey);
        }

        private static void RehookUpdate()
        {
            EditorApplication.update -= ContinueAudit;
            if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += ContinueAudit;
        }

        private static void FailToStart(string reason)
        {
            Debug.LogError("ADVENTURE_BOX2D_PLAY_START_FAILURE|" + Sanitize(reason));
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static string Sanitize(string value) => string.IsNullOrEmpty(value)
            ? string.Empty : value.Replace('|', '/').Replace('\n', ' ').Replace('\r', ' ');
    }
}
