#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingPlayModeProbe : MonoBehaviour
    {
        bool baseline;
        string output;
        int errors;
        public void Begin(bool initial)
        {
            baseline = initial;
            output = Path.GetFullPath("Logs/RacingPhase2/" + (baseline ? "baseline" : "validation"));
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"errors.txt"),"");
            File.WriteAllText(Path.Combine(output,"samples.txt"),"");
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            Application.logMessageReceived += Log;
            SceneManager.sceneLoaded += SceneLoaded;
            StartCoroutine(Inspect());
        }
        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (baseline || scene.name != "Racing3D") return;
            var manager=FindFirstObjectByType<RaceManager>();
            if(manager!=null){manager.Configure(RaceRules.CreateRanked(4207));manager.PersistResults=false;}
        }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                errors++;
                File.AppendAllText(Path.Combine(output, "errors.txt"), message + "\n" + stack + "\n");
            }
        }
        IEnumerator Inspect()
        {
            yield return new WaitForSecondsRealtime(2f);
            Capture("01-main-menu");
            if(!baseline)
            {
                FindFirstObjectByType<RacingMainMenuUI>()?.ShowPlayChoices();
                yield return new WaitForSecondsRealtime(.4f);
                Capture("01b-play-selection");
            }
            SceneManager.LoadScene("RacingSelect");
            yield return new WaitForSecondsRealtime(2f);
            Capture("02-racing-hub");
            SceneManager.LoadScene("RacingGarage");
            yield return new WaitForSecondsRealtime(2f);
            Capture("03-garage");
            SceneManager.LoadScene("Racing3D");
            yield return new WaitForSecondsRealtime(1.5f);
            var manager = FindFirstObjectByType<RaceManager>();
            if (!baseline && manager != null)
            {
                manager.PersistResults = false;
                var driver = manager.PlayerVehicle.gameObject.AddComponent<AiVehicleController>();
                driver.Initialize(AiSkillModel.CreateProfile(7000, 4207), manager.ProgressTracker.TrackGraph, manager.ProgressTracker, "SCRIPTED_PLAYER");
                manager.PlayerVehicle.SetCommandProvider(driver);
                Time.timeScale = 6f;
                Time.maximumDeltaTime = 1f;
            }
            Capture("04-grid");
            int movedBeforeGo = 0;
            if (manager != null)
            {
                foreach (var vehicle in manager.SpawnedVehicles)
                    if (vehicle.SpeedKmh > 5f) movedBeforeGo++;
            }
            yield return new WaitForSecondsRealtime(8f);
            Capture("05-race");
            if (baseline && manager != null && manager.PlayerVehicle != null)
            {
                manager.PlayerVehicle.GetComponent<PlayerRacingInput>().SetTouchGas(true);
                manager.PlayerVehicle.GetComponent<PlayerRacingInput>().SetTouchSteer(0.5f);
                manager.PlayerVehicle.GetComponent<PlayerRacingInput>().SetTouchDrift(true);
                manager.PlayerVehicle.GetComponent<ItemInventory>().AcquireItem(RaceItemType.TurboPulse, true);
            }
            yield return new WaitForSecondsRealtime(2f);
            Capture("06-drift-item");
            if (baseline) yield return new WaitForSecondsRealtime(16f);
            else
            {
                float timeout = Time.realtimeSinceStartup + 140f;
                float nextSample = Time.realtimeSinceStartup;
                while (manager != null && manager.State != RaceState.Results && Time.realtimeSinceStartup < timeout)
                {
                    if (Time.realtimeSinceStartup >= nextSample)
                    {
                        nextSample += 15f;
                        string sample = $"TIME {manager.RaceElapsed:F1} STATE {manager.State}\n";
                        foreach (var racer in manager.ProgressTracker.Leaderboard)
                            sample += $"{racer.Identity.displayName} lap={racer.CurrentLap} cp={racer.LastCheckpointIndex} visits={racer.CheckpointVisits} speed={racer.Vehicle.SpeedKmh:F1} pos={racer.Vehicle.transform.position} recovery={racer.Recovery.RecoveryCount} finished={racer.Finished}\n";
                        File.AppendAllText(Path.Combine(output,"samples.txt"), sample);
                    }
                    yield return new WaitForSecondsRealtime(.5f);
                }
                Time.timeScale = 1f;
            }
            int checkpointVisits = 0;
            int finished = 0;
            int qualityViolations = 0;
            string qualityDetails = "";
            if (manager != null)
                foreach (var racer in manager.ProgressTracker.Leaderboard)
                {
                    checkpointVisits += racer.CheckpointVisits;
                    if (racer.Finished) finished++;
                    if(!baseline)
                    {
                        bool stalled=!racer.Finished && manager.RaceElapsed-racer.LastCheckpointTime>60f;
                        bool repeatedRecovery=racer.Recovery.RecoveryCount>2;
                        bool wrongFinish=racer.Finished && racer.CheckpointVisits!=19;
                        manager.ProgressTracker.TrackGraph.ProjectPosition(racer.Vehicle.transform.position,out _,out float along,out _);
                        bool underTrack=racer.Vehicle.transform.position.y<manager.ProgressTracker.TrackGraph.SampleDistance(along).Position.y-5f;
                        if(stalled||repeatedRecovery||wrongFinish||underTrack)
                        {qualityViolations++;qualityDetails+=$"{racer.Identity.displayName}: stalled={stalled} repeated_recovery={repeatedRecovery} invalid_finish={wrongFinish} under_track={underTrack}\n";}
                    }
                }
            File.WriteAllText(Path.Combine(output, "runtime.txt"),
                $"classification=SCRIPTED_REAL_PLAY_MODE\nbaseline={baseline}\nerrors={errors}\n" +
                $"racers={manager?.SpawnedVehicles.Count}\nvehicles_moving_before_GO={movedBeforeGo}\n" +
                $"checkpoint_visits={checkpointVisits}\nfinished={finished}\nrace_elapsed={manager?.RaceElapsed}\nstate={manager?.State}\nquality_violations={qualityViolations}\n"+qualityDetails);
            // Baseline forced transition checks Results lifecycle, never counts as a completed race.
            if (baseline && manager != null) manager.TransitionToResults();
            yield return new WaitForSecondsRealtime(2f);
            Capture(baseline?"07-forced-results-lifecycle":"09-results");
            SceneManager.LoadScene("PreStart");
            yield return new WaitForSecondsRealtime(1f);
            File.AppendAllText(Path.Combine(output, "runtime.txt"), $"return_scene={SceneManager.GetActiveScene().name}\n");
            Application.logMessageReceived -= Log;
            SceneManager.sceneLoaded -= SceneLoaded;
            SessionState.SetInt("AHR.Racing.ProductAudit.Exit", errors == 0 && (baseline || (finished > 0 && qualityViolations==0 && manager.State==RaceState.Results && movedBeforeGo==0)) ? 0 : 1);
            SessionState.SetBool("AHR.Racing.ProductAudit.Done", true);
        }
        void Capture(string name)
        {
            if(baseline){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));return;}
            Camera camera=Camera.main;if(camera==null)return;
            foreach(var preview in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(preview.targetTexture!=null)preview.Render();
            var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);target.Create();var previous=camera.targetTexture;var active=RenderTexture.active;
            var canvases=FindObjectsByType<UnityEngine.Canvas>(FindObjectsSortMode.None);var modes=new RenderMode[canvases.Length];var cameras=new Camera[canvases.Length];var planes=new float[canvases.Length];
            camera.targetTexture=target;
            for(int i=0;i<canvases.Length;i++){modes[i]=canvases[i].renderMode;cameras[i]=canvases[i].worldCamera;planes[i]=canvases[i].planeDistance;canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=.5f;}
            UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=planes[i];}
            camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(texture);
        }
    }
}
#endif
