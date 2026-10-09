#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    // Scripted QA only; screenshots and JSON never run during FPS measurements.
    public sealed class RacingSkyHopProbe : MonoBehaviour
    {
        [Serializable] sealed class Checks
        {
            public int passed,failed;
            public List<string> successes=new List<string>(),failures=new List<string>();
            public void Check(bool condition,string text)
            {
                if(condition){passed++;successes.Add(text);}
                else{failed++;failures.Add(text);Debug.LogError("SKYHOP_FAILED "+text);}
            }
        }
        [Serializable] sealed class HopProof
        {
            public string classification="NATIVE_MINIHOP_PHYSICS_AND_REAL_TRACK_PRESENTATION_NOT_FPS_OR_ELITE";
            public Checks checks=new Checks();public int errors;
            public float humanHeight,botHeight,humanAirSeconds,botAirSeconds,redirectDegrees,hopRedirectDegrees;
            public int grassNodeCount,grassShortcuts;public string grassTrackId;
        }
        sealed class Driver : IVehicleCommandProvider { public VehicleCommands value;public VehicleCommands GetCommands()=>value; }
        readonly HopProof proof=new HopProof();string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--skyhop-test")<0)return;
            if(Application.companyName!="HillAdventurer.BlacklineQA"){Debug.LogError("SkyHop QA requires isolated company");Application.Quit(1);return;}
            var obj=new GameObject("Explicit SkyHop validation").AddComponent<RacingSkyHopProbe>();DontDestroyOnLoad(obj);
            obj.output=RacingFrameForensics.Argument("--closure-output");Directory.CreateDirectory(obj.output);Application.logMessageReceived+=obj.Logged;
            obj.StartCoroutine(obj.Guarded());
        }
        void Logged(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)proof.errors++;}
        void Check(bool value,string text)=>proof.checks.Check(value,text);
        void Capture(string name){RacingEvidenceCapture.Capture(Path.Combine(output,name+".png"));Check(File.Exists(Path.Combine(output,name+".png")),"Native render "+name);}
        IEnumerator Guarded()
        {
            var work=Run();
            while(true)
            {
                bool more=false;Exception failure=null;try{more=work.MoveNext();}catch(Exception e){failure=e;}
                if(failure!=null){Check(false,failure.ToString());break;}if(!more)break;yield return work.Current;
            }
            Time.timeScale=1;File.WriteAllText(Path.Combine(output,"skyhop-native.json"),JsonUtility.ToJson(proof,true));Application.logMessageReceived-=Logged;
            Application.Quit(proof.checks.failed==0&&proof.errors==0?0:1);
        }
        IEnumerator Run()
        {
            RacingTrackSelection.Select("night_city");RacingTestSession.Set(9700,4207);SceneManager.LoadScene("Racing3D");
            RaceManager manager=null;float deadline=Time.realtimeSinceStartup+40;
            while(Time.realtimeSinceStartup<deadline){manager=FindFirstObjectByType<RaceManager>();if(manager!=null&&manager.PlayerVehicle!=null&&manager.SpawnedVehicles.Count==12)break;yield return null;}
            Check(manager!=null&&manager.SpawnedVehicles.Count==12,"Night City sky starts the ordinary twelve-racer scene");if(manager==null||manager.PlayerVehicle==null)yield break;
            manager.PersistResults=false;var camera=Camera.main;var follow=camera.GetComponent<RacingCamera3D>();var player=manager.PlayerVehicle;
            var graph=manager.ProgressTracker.TrackGraph;
            Time.timeScale=0;follow.enabled=false;camera.farClipPlane=1700;camera.fieldOfView=64;
            camera.transform.position=player.transform.position-player.transform.forward*9+Vector3.up*4;
            camera.transform.LookAt(player.transform.position+player.transform.forward*17);
            yield return null;Capture("01-night-sky-racing");
            var hud=GameObject.Find("Racing HUD");if(hud!=null)hud.SetActive(false);
            camera.transform.position=new Vector3(610,390,450);camera.transform.LookAt(new Vector3(90,-30,-55));camera.fieldOfView=58;
            yield return null;Capture("02-night-sky-overview");
            var corner=graph.GetNode(52);camera.transform.position=corner.Position-corner.Forward*34+corner.Right*7+Vector3.up*4;
            camera.transform.LookAt(corner.Position+corner.Forward*18);camera.fieldOfView=66;
            yield return null;Capture("03-night-neon-guidance");
            Time.timeScale=1;if(hud!=null)hud.SetActive(true);follow.enabled=true;
            follow.SetTarget(player.transform,player);follow.SnapToTarget();
            var nightDriver=player.gameObject.AddComponent<AiVehicleController>();nightDriver.Initialize(AiSkillModel.CreateProfile(9700,4207),graph,manager.ProgressTracker,"EXPLICIT SKY SCREENSHOT DRIVER");
            deadline=Time.realtimeSinceStartup+35;while(manager.RaceElapsed<3&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(manager.RaceElapsed>=3&&player.SpeedKmh>30,"Sky track reaches real moving gameplay with the ordinary camera and HUD");
            Capture("07-night-sky-live-gameplay");
            // Actual shared Rigidbody cars on an explicitly isolated fixture.
            manager.StopAllCoroutines();manager.enabled=false;manager.ProgressTracker.enabled=false;
            var metrics=FindFirstObjectByType<RacingRaceMetrics>();if(metrics!=null){metrics.StopMeasurement();metrics.enabled=false;}
            foreach(var car in manager.SpawnedVehicles)
            {
                car.SetRaceEnabled(false);car.enabled=false;
                var ai=car.GetComponent<AiVehicleController>();if(ai!=null)ai.enabled=false;
                var recovery=car.GetComponent<VehicleRecovery>();if(recovery!=null)recovery.enabled=false;
                var recorder=car.GetComponent<ML.RacingTelemetryRecorder>();if(recorder!=null){recorder.MarkScripted();recorder.enabled=false;}
                var ml=car.GetComponent<ML.RacingMlInferenceAdapter>();if(ml!=null)ml.executionMode=ML.MlExecutionMode.Off;
            }
            var floor=new GameObject("QA hop floor / not a game route");floor.transform.SetParent(transform,false);floor.transform.position=new Vector3(0,249.5f,0);floor.AddComponent<BoxCollider>().size=new Vector3(50,1,1500);
            var bot=manager.SpawnedVehicles[1];var humanDriver=new Driver();var botDriver=new Driver();player.GetComponent<PlayerRacingInput>().SetInputEnabled(false);
            player.ResetVelocities();bot.ResetVelocities();player.Body.position=new Vector3(-6,250.65f,-300);bot.Body.position=new Vector3(6,250.65f,-300);
            player.Body.rotation=bot.Body.rotation=Quaternion.identity;player.SetCommandProvider(humanDriver);bot.SetCommandProvider(botDriver);
            player.SetRaceEnabled(true);bot.SetRaceEnabled(true);player.enabled=bot.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSeconds(1.4f);
            Check(player.IsGrounded&&bot.IsGrounded,"Both shared motors settle on the same QA collision surface");
            string humanStats=JsonUtility.ToJson(player.Stats),botStats=JsonUtility.ToJson(bot.Stats);float mass=player.Body.mass;
            float humanY=player.Body.position.y,botY=bot.Body.position.y;
            humanDriver.value=new VehicleCommands{MiniHop=true};botDriver.value=humanDriver.value;
            yield return new WaitForFixedUpdate();humanDriver.value=botDriver.value=VehicleCommands.Idle;
            Check(player.IsMiniHopping&&bot.IsMiniHopping,"One rising edge starts the same technical hop for both motors");
            float until=Time.fixedTime+1;
            while(Time.fixedTime<until)
            {
                yield return new WaitForFixedUpdate();
                proof.humanHeight=Mathf.Max(proof.humanHeight,player.Body.position.y-humanY);proof.botHeight=Mathf.Max(proof.botHeight,bot.Body.position.y-botY);
                if(!player.IsGrounded)proof.humanAirSeconds+=Time.fixedDeltaTime;if(!bot.IsGrounded)proof.botAirSeconds+=Time.fixedDeltaTime;
            }
            Check(proof.humanHeight>.08f&&proof.humanHeight<.50f&&proof.humanAirSeconds>.10f&&proof.humanAirSeconds<.40f,"Hop is small, fast and physically lands without a floaty full jump");
            Check(Mathf.Abs(proof.humanHeight-proof.botHeight)<.02f&&Mathf.Abs(proof.humanAirSeconds-proof.botAirSeconds)<.025f,"Human/bot hop uses identical forces and flight envelope");
            Check(player.IsGrounded&&!player.IsMiniHopping&&!player.IsDrifting&&player.MiniHopsPerformed==1,"Tap lands without drift, automatic repeat or stale hop state");
            Check(player.JumpsPerformed==0&&player.TricksPerformed==0&&player.LandingBoosts==0,"Mini hop does not grant full-jump tricks or landing boost rewards");
            player.Body.linearVelocity=Vector3.forward*22;float yaw=player.Body.rotation.eulerAngles.y;
            humanDriver.value=new VehicleCommands{Throttle=1,Steering=.7f,Drift=true,MiniHop=true};
            yield return new WaitForFixedUpdate();humanDriver.value.MiniHop=false;
            until=Time.fixedTime+.8f;while(Time.fixedTime<until)
            {
                yield return new WaitForFixedUpdate();
                if(player.IsMiniHopping)proof.hopRedirectDegrees=Mathf.DeltaAngle(yaw,player.Body.rotation.eulerAngles.y);
            }
            proof.redirectDegrees=Mathf.DeltaAngle(yaw,player.Body.rotation.eulerAngles.y);
            Check(proof.redirectDegrees>2&&proof.redirectDegrees<65,"Steering redirects the technical hop with bounded shared yaw torque");
            Check(proof.hopRedirectDegrees>1&&proof.hopRedirectDegrees<20,"Directional yaw is measurable during the tiny hop itself, before grounded drift");
            Check(player.IsDrifting&&player.DriftsStarted==1&&player.MiniHopsPerformed==2,"Held pre-drift input transitions into drift after landing without repeating hop");
            humanDriver.value=VehicleCommands.Idle;yield return new WaitForFixedUpdate();Check(!player.IsDrifting,"Releasing Drift clears drift normally");
            Check(humanStats==JsonUtility.ToJson(player.Stats)&&botStats==JsonUtility.ToJson(bot.Stats)&&player.Body.mass==mass,"Hop leaves permanent stats, mass and tuning unchanged");
            player.ResetVelocities();Check(!player.IsMiniHopping,"Recovery/reset clears the hop state");
            player.SetRaceEnabled(false);Check(!player.IsMiniHopping,"Race restart clears the hop state");
            Destroy(floor);yield return null;
            // Exercise the actual new track button, then the ordinary scene path.
            SceneManager.LoadScene("RacingSelect");yield return new WaitForSecondsRealtime(.6f);
            Capture("08-racing-track-selection");
            UnityEngine.UI.Button select=null;foreach(var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))if(button.name=="VERDANT RUN")select=button;
            Check(select!=null,"New track is reachable from the actual racing hub");if(select!=null)select.onClick.Invoke();
            Check(RacingTrackSelection.Id==VerdantCircuit.Id,"Track button chooses Verdant without touching ranked saves");
            RacingTestSession.Set(9700,4207);SceneManager.LoadScene("Racing3D");manager=null;deadline=Time.realtimeSinceStartup+40;
            while(Time.realtimeSinceStartup<deadline){manager=FindFirstObjectByType<RaceManager>();if(manager!=null&&manager.PlayerVehicle!=null&&manager.SpawnedVehicles.Count==12)break;yield return null;}
            Check(manager!=null&&manager.SpawnedVehicles.Count==12,"Verdant uses the ordinary twelve-racer lobby, physics and HUD");if(manager==null)yield break;
            manager.PersistResults=false;graph=manager.ProgressTracker.TrackGraph;var info=FindFirstObjectByType<RacingTrackInfo>();
            proof.grassTrackId=info.TrackId;proof.grassNodeCount=graph.NodeCount;proof.grassShortcuts=graph.Shortcuts.Length;
            Check(info.TrackId==VerdantCircuit.Id&&FindFirstObjectByType<CanyonRushBuilder>()==null,"Additional track loads alone with its separate ID and navigation graph");
            camera=Camera.main;follow=camera.GetComponent<RacingCamera3D>();follow.enabled=false;Time.timeScale=0;camera.farClipPlane=1600;
            camera.farClipPlane=2400;camera.transform.position=new Vector3(1070,950,650);camera.transform.LookAt(new Vector3(220,4,-150));camera.fieldOfView=56;
            hud=GameObject.Find("Racing HUD");if(hud!=null)hud.SetActive(false);
            yield return null;Capture("04-verdant-layout");
            var entry=graph.GetNode(8);camera.transform.position=entry.Position-entry.Forward*32+Vector3.up*7;camera.transform.LookAt(graph.Shortcuts[0].pathPoints[12]);camera.fieldOfView=70;
            yield return null;Capture("05-verdant-grass-cut");
            Time.timeScale=1;if(hud!=null)hud.SetActive(true);follow.enabled=true;
            var driver=manager.PlayerVehicle.gameObject.AddComponent<AiVehicleController>();driver.Initialize(AiSkillModel.CreateProfile(9700,4207),graph,manager.ProgressTracker,"EXPLICIT VERDANT SCREENSHOT DRIVER");
            deadline=Time.realtimeSinceStartup+35;while(manager.RaceElapsed<12&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(manager.RaceElapsed>=12&&manager.PlayerVehicle.SpeedKmh>30,"Twelve-racer additional track reaches actual gameplay with a moving scripted player");
            Capture("06-verdant-live-gameplay");
        }
    }
}
#endif
