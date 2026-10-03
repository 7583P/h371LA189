#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    // Opt-in diagnostics. Normal human players never create this component.
    public sealed class BlacklineValidationProbe : MonoBehaviour
    {
        string output;
        int[] ratings, seeds;
        float scale;
        bool equalBuild;
        bool rendered;
        bool onscreen;
        int errors;
        RaceManager manager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--blackline-validation") < 0) return;
            new GameObject("Explicit Blackline validation").AddComponent<BlacklineValidationProbe>().Begin();
        }

        static string Arg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        void Begin()
        {
            DontDestroyOnLoad(gameObject);
            output = Path.GetFullPath(Arg("--blackline-output", "Logs/BlacklineValidation"));
            Directory.CreateDirectory(output);
            errors+=BlacklineContracts.Pure(output);
            ratings = Array.ConvertAll(Arg("--blackline-ratings", "75000,100000").Split(','), int.Parse);
            seeds = Array.ConvertAll(Arg("--blackline-seeds", "4207").Split(','), int.Parse);
            scale = Mathf.Clamp(float.Parse(Arg("--blackline-timescale", "4"), System.Globalization.CultureInfo.InvariantCulture), 1, 6);
            equalBuild = Array.IndexOf(Environment.GetCommandLineArgs(), "--blackline-equal-build") >= 0;
            rendered = Array.IndexOf(Environment.GetCommandLineArgs(), "--blackline-render") >= 0;
            onscreen = Array.IndexOf(Environment.GetCommandLineArgs(), "--blackline-onscreen") >= 0;
            if(onscreen&&(Application.isBatchMode||rendered||scale!=1||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null))
            {Debug.LogError("On-screen validation requires a visible graphics player at timeScale 1.");Application.Quit(1);return;}
            string quality=Arg("--blackline-quality","");
            if(Enum.TryParse(quality,true,out RacingQuality selected))RacingQualityScope.OverrideForValidation(selected);
            Application.runInBackground = true;
            Application.logMessageReceived += Logged;
            SceneManager.sceneLoaded += Loaded;
            StartCoroutine(Run());
        }

        void Logged(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errors++;
            File.AppendAllText(Path.Combine(output, "errors.txt"), message + "\n" + stack + "\n");
        }

        void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Racing3D") return;
            manager = FindFirstObjectByType<RaceManager>();
            var rules = RaceRules.CreateRanked(RacingTestSession.Seed);
            rules.EqualBuildBenchmark = equalBuild;
            rules.PostPlayerFinishTimeoutSeconds = 60;
            manager.Configure(rules);
            manager.PersistResults = false;
        }

        IEnumerator Run()
        {
            foreach (int rating in ratings)
            foreach (int seed in seeds)
            {
                manager = null;
                // Session-only test override: no rating, selection or reward writes.
                RacingTestSession.Set(rating, seed);
                SceneManager.LoadScene("Racing3D");
                while (manager == null || manager.PlayerVehicle == null) yield return null;
                var car = manager.PlayerVehicle;
                car.GetComponent<PlayerRacingInput>().SetInputEnabled(false);
                var driver = car.gameObject.AddComponent<AiVehicleController>();
                driver.Initialize(AiSkillModel.CreateProfile(rating, manager.LobbyRacers[0].personalitySeed), manager.ProgressTracker.TrackGraph, manager.ProgressTracker, "EXPLICIT TEST DRIVER");
                var metrics = FindFirstObjectByType<RacingRaceMetrics>();
                string directory = Path.Combine(output, "mmr-" + rating, "seed-" + seed);
                Directory.CreateDirectory(directory);
                metrics.ExportPath = Path.Combine(directory, "metrics.json");
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--blackline-shortcut-trace")>=0)
                    foreach(var racer in manager.SpawnedVehicles)
                        racer.gameObject.AddComponent<BlacklineShortcutRecorder>().Bind(manager.ProgressTracker.TrackGraph,Path.Combine(directory,"physical-"+racer.name+".csv"));
                errors+=BlacklineContracts.World(manager,directory);
                RacingRenderedPlayerProbe render=null;
                if(rendered){render=manager.gameObject.AddComponent<RacingRenderedPlayerProbe>();render.Bind(Camera.main,manager);}
                while (manager.State != RaceState.Racing) yield return null;
                Time.timeScale = scale;
                metrics.Report.timeScale=scale;
                float deadline = Time.time + 390;
                using(var trace=onscreen?null:new StreamWriter(Path.Combine(directory,"race-trace.csv")))
                {
                    trace?.WriteLine("seconds,name,lap,checkpoint,node,route,segment,speed,x,y,z,targetX,targetY,targetZ,intent");
                    while (manager.State != RaceState.Results && Time.time < deadline)
                    {
                        if(trace!=null)foreach(var racer in manager.ProgressTracker.Leaderboard)
                        {
                            var ai=racer.Vehicle.GetComponent<AiVehicleController>();Vector3 p=racer.Vehicle.transform.position,t=ai.CurrentTarget;
                            trace.WriteLine(FormattableString.Invariant($"{manager.RaceElapsed:F2},{racer.Identity.displayName},{racer.CurrentLap},{racer.LastCheckpointIndex},{ai.CurrentNodeIndex},{ai.ActiveRouteIndex},{ai.RouteSegment},{racer.Vehicle.SpeedKmh:F1},{p.x:F2},{p.y:F2},{p.z:F2},{t.x:F2},{t.y:F2},{t.z:F2},{ai.Logger.CurrentIntent}"));
                        }
                        yield return new WaitForSeconds(1);
                    }
                }
                metrics.Export(metrics.ExportPath);
                if(render!=null)render.Export(Path.Combine(directory,"render-proof.json"));
                if(onscreen)File.WriteAllText(Path.Combine(directory,"onscreen-proof.txt"),
                    "batchMode="+Application.isBatchMode+"\ntimeScale="+Time.timeScale+"\ncameraEnabled="+Camera.main.enabled+
                    "\ncameraTargetTexture="+(Camera.main.targetTexture==null?"none":Camera.main.targetTexture.name)+
                    "\nwidth="+Screen.width+"\nheight="+Screen.height+"\ngraphics="+SystemInfo.graphicsDeviceName+"\n");
                File.WriteAllText(Path.Combine(directory, "execution.txt"), "rating=" + rating + "\nseed=" + seed + "\nstate=" + manager.State + "\ntimeScale="+scale+"\nraceSeconds=" + manager.RaceElapsed + "\nerrors=" + errors + "\n");
                Debug.Log("BLACKLINE_RACE_COMPLETE mmr=" + rating + " seed=" + seed + " state=" + manager.State + " seconds=" + manager.RaceElapsed);
                Time.timeScale = 1;
                yield return null;
            }
            RacingTestSession.Clear();
            SceneManager.sceneLoaded -= Loaded;
            Application.logMessageReceived -= Logged;
            File.WriteAllText(Path.Combine(output, "complete.txt"), "runtimeErrors=" + errors + "\n");
            Application.Quit(errors == 0 ? 0 : 1);
        }
    }
}
#endif
