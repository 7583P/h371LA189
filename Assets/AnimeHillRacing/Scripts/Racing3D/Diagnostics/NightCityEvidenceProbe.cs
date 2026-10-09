// current-10k QA presets
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    // Explicit QA player only. Captures actual generated meshes, UI and driving, not concept art.
    public sealed class NightCityEvidenceProbe : MonoBehaviour
    {
        string output;int errors;RaceManager manager;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--night-city-evidence");
            if(flag<0&&Array.IndexOf(args,"--night-city-adventure")<0)return;
            if(Application.companyName!="HillAdventurer.BlacklineQA") { Debug.LogError("Night City evidence requires the isolated QA profile.");Application.Quit(1);return; }
            var probe=new GameObject("Explicit Night City evidence").AddComponent<NightCityEvidenceProbe>();
            int folder=Array.IndexOf(args,"--night-city-output");probe.output=Path.GetFullPath(folder>=0?args[folder+1]:"Logs/NightCity/evidence");
            Directory.CreateDirectory(probe.output);DontDestroyOnLoad(probe.gameObject);
            Application.logMessageReceived+=probe.Logged;Application.runInBackground=true;
            RacingQualityScope.OverrideForValidation(RacingQuality.High);
            probe.StartCoroutine(probe.Run());
        }
        void Logged(string message,string stack,LogType type)
        { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");} }
        void Capture(string name) { RacingEvidenceCapture.Capture(Path.Combine(output,name+".png")); }
        IEnumerator Run()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--night-city-adventure")>=0)
            {yield return AdventureRegression();Finish();yield break;}
            RacingSaveManager.SelectedVehicleId=MetaProgressionConfig.MetaVehicleId;
            RacingSaveManager.SelectedWheelId=MetaProgressionConfig.MetaWheelId;
            RacingSaveManager.CurrentMmr=10000; // Isolated Development QA save only.
            SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(1);
            Capture("10-main-menu");yield return null;
            SceneManager.LoadScene("RacingGarage");yield return new WaitForSecondsRealtime(1);
            Capture("05-garage-preview");yield return null;
            RacingTestSession.Set(10000,4207);SceneManager.LoadScene("Racing3D");
            float deadline=Time.realtimeSinceStartup+45;
            while((manager==null||manager.PlayerVehicle==null||manager.State!=RaceState.Countdown)&&Time.realtimeSinceStartup<deadline)
            {manager=FindFirstObjectByType<RaceManager>();yield return null;}
            if(manager==null||manager.PlayerVehicle==null){errors++;Finish();yield break;}
            manager.PersistResults=false;Time.timeScale=0;
            var camera=Camera.main;var follow=camera.GetComponent<RacingCamera3D>();follow.enabled=false;
            var hud=GameObject.Find("Racing HUD");if(hud!=null)hud.SetActive(false);
            var graph=manager.ProgressTracker.TrackGraph;var player=manager.PlayerVehicle;
            camera.farClipPlane=1600;camera.fieldOfView=48;
            camera.transform.position=new Vector3(645,675,315);camera.transform.LookAt(new Vector3(70,10,-90));
            yield return null;Capture("01-grand-circuit-overview");yield return null;
            var pose=player.transform;
            foreach(var vehicle in manager.SpawnedVehicles)if(vehicle!=player)vehicle.gameObject.SetActive(false);
            camera.fieldOfView=40;camera.transform.position=pose.position+pose.right*3.4f+pose.forward*4.5f+Vector3.up*1.7f;
            camera.transform.LookAt(pose.position+Vector3.up*.35f);
            yield return null;Capture("02-dapex-body");yield return null;
            camera.fieldOfView=30;camera.transform.position=pose.position-pose.forward*4.3f+pose.right*.6f+Vector3.up*.65f;
            camera.transform.LookAt(pose.position+Vector3.up*.20f);
            yield return null;Capture("11-dapex-rear-badge");yield return null;
            var wheel=player.GetComponent<RacingVehicleVisuals>().Wheels[1];
            camera.fieldOfView=32;camera.transform.position=wheel.position+pose.right*1.6f+pose.forward*.38f+Vector3.up*.23f;
            camera.transform.LookAt(wheel.position);
            yield return null;Capture("03-whity-wheel-detail");yield return null;
            foreach(var vehicle in manager.SpawnedVehicles)if(vehicle!=player)vehicle.gameObject.SetActive(true);
            Vector3 center=Vector3.zero;foreach(var vehicle in manager.SpawnedVehicles)center+=vehicle.transform.position;center/=12;
            camera.fieldOfView=54;camera.transform.position=center+Vector3.up*13-pose.forward*15+pose.right*12;
            camera.transform.LookAt(center+pose.forward*5);
            yield return null;Capture("06-elite-10k-grid");yield return null;
            // Reuse actual gameplay artwork in the front end; this still is an unmodified game render.
            foreach(var vehicle in manager.SpawnedVehicles)if(vehicle!=player)vehicle.gameObject.SetActive(false);
            camera.fieldOfView=39;camera.transform.position=pose.position+pose.right*3.1f+pose.forward*5.8f+Vector3.up*1.35f;
            camera.transform.LookAt(pose.position+Vector3.up*.30f);
            camera.transform.LookAt(pose.position-camera.transform.right*.9f+Vector3.up*.30f);
            yield return null;Capture("07-native-menu-key-art");yield return null;
            foreach(var vehicle in manager.SpawnedVehicles)if(vehicle!=player)vehicle.gameObject.SetActive(true);
            var skyline=graph.GetNode(38);camera.fieldOfView=62;
            camera.transform.position=skyline.Position+skyline.Right*23+Vector3.up*15-skyline.Forward*28;
            camera.transform.LookAt(skyline.Position+skyline.Forward*35);
            yield return null;Capture("08-skyline-sector");yield return null;
            var route=graph.Shortcuts[1];Vector3 apex=route.pathPoints[12];
            camera.transform.position=apex+new Vector3(-42,65,15);camera.transform.LookAt(apex);camera.fieldOfView=58;
            yield return null;Capture("09-apex-link");yield return null;
            int worldFailures=BlacklineContracts.World(manager,output)+NightCityRedesignContracts.World(manager,output);
            errors+=worldFailures;
            File.WriteAllText(Path.Combine(output,"world-errors.txt"),"failures="+worldFailures);
            if(hud!=null)hud.SetActive(true);follow.enabled=true;follow.SetTarget(pose,player);follow.SnapToTarget();
            player.GetComponent<PlayerRacingInput>().SetInputEnabled(false);
            var ai=player.gameObject.AddComponent<AiVehicleController>();
            ai.Initialize(AiSkillModel.CreateProfile(10000,4207),graph,manager.ProgressTracker,"EXPLICIT SCREENSHOT DRIVER");
            Time.timeScale=1;deadline=Time.realtimeSinceStartup+45;
            while(manager.RaceElapsed<22&&Time.realtimeSinceStartup<deadline)yield return null;
            Capture("04-live-gameplay-hud");yield return null;
            bool airborne=false,drifting=false,boosting=false;
            deadline=Time.realtimeSinceStartup+125;
            while(manager.RaceElapsed<125&&manager.State!=RaceState.Results&&Time.realtimeSinceStartup<deadline)
            {
                if(!airborne&&!player.IsGrounded&&manager.RaceElapsed>25){Capture("12-live-airborne");airborne=true;}
                if(!drifting&&player.IsDrifting&&manager.RaceElapsed>25){Capture("13-live-drift");drifting=true;}
                if(!boosting&&player.IsBoosting&&manager.RaceElapsed>25){Capture("14-live-boost");boosting=true;}
                yield return null;
            }
            Capture("15-live-gameplay-later");yield return null;
            var visuals=player.GetComponent<RacingVehicleVisuals>();
            File.WriteAllText(Path.Combine(output,"capture-proof.txt"),"Actual Unity camera and HUD / 1600x900 render textures\nGPU="+SystemInfo.graphicsDeviceName+"\nDriving speed="+player.SpeedKmh+"\nRace seconds="+manager.RaceElapsed+"\nBrake lamp transitions="+visuals.BrakeLightTransitions+"\nObserved native motion captures: airborne="+airborne+", drift="+drifting+", boost="+boosting+"\nAutomated QA driver; no human playtest claimed.\n");
            RacingTestSession.Clear();Finish();
        }
        IEnumerator AdventureRegression()
        {
            GameSession.SelectedVehicleId="car-01";GameSession.SelectedMapId="countryside";
            SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(1);
            UnityEngine.UI.Button adventure=null;
            foreach(var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if(button.name=="ADVENTURE"&&button.isActiveAndEnabled&&button.interactable){adventure=button;break;}
            if(adventure==null){errors++;File.WriteAllText(Path.Combine(output,"adventure-ui-path.txt"),"FAIL: Adventure button unavailable");yield break;}
            adventure.onClick.Invoke();
            float menuDeadline=Time.realtimeSinceStartup+45;
            while((SceneManager.GetActiveScene().name!="PreStart"||RacingLoadingScreen.IsLoading)&&Time.realtimeSinceStartup<menuDeadline)yield return null;
            var selection=FindFirstObjectByType<PreStartMenuUI>();
            if(selection==null||GameSession.SelectedGameMode!=GameModeType.Adventure2_5D){errors++;File.WriteAllText(Path.Combine(output,"adventure-ui-path.txt"),"FAIL: native 2D track selection unavailable");yield break;}
            yield return null;Capture("adventure-track-selection");yield return null;
            selection.SelectAdventureTrack(0);yield return new WaitForSecondsRealtime(1);
            var garage=FindFirstObjectByType<GarageUI>();
            if(garage==null||SceneManager.GetActiveScene().name!="Garage"){errors++;File.WriteAllText(Path.Combine(output,"adventure-ui-path.txt"),"FAIL: native Adventure garage unavailable");yield break;}
            Capture("adventure-garage");yield return null;garage.StartAdventure();
            menuDeadline=Time.realtimeSinceStartup+45;
            while(SceneManager.GetActiveScene().name!="Adventure2_5D"&&Time.realtimeSinceStartup<menuDeadline)yield return null;
            if(SceneManager.GetActiveScene().name!="Adventure2_5D"){errors++;File.WriteAllText(Path.Combine(output,"adventure-ui-path.txt"),"FAIL: native Adventure launch unavailable");yield break;}
            yield return new WaitForSecondsRealtime(1);
            File.WriteAllText(Path.Combine(output,"adventure-ui-path.txt"),"PASS: scripted native callbacks MainMenu.ADVENTURE > PreStart.SelectAdventureTrack(0) > Garage.StartAdventure > Adventure2_5D. Human pointer/touch usability remains pending.");
            var probe=new GameObject("Explicit unchanged Adventure regression").AddComponent<AdventurePhysics2DPlayModeProbe>();
            probe.Configure("car-01","countryside",false);
            float deadline=Time.realtimeSinceStartup+150;
            while(!probe.Completed&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!probe.Completed||!probe.Passed)errors++;
            File.WriteAllText(Path.Combine(output,"adventure-regression.txt"),probe.ResultLine+"\n"+probe.FailureSummary);
            Capture("adventure-runtime");yield return null;
            // Exercise actual result capture and defeat flow with a graphics device.
            var game=GameManager.Instance;
            if(game==null){errors++;yield break;}
            game.RequestGameOver(RunEndReason.Crashed);deadline=Time.realtimeSinceStartup+30;
            while(game.State!=RunState.Results&&Time.realtimeSinceStartup<deadline)yield return null;
            bool results=game.State==RunState.Results;
            if(!results)errors++;
            if(results){yield return null;Capture("adventure-results");yield return null;}
            File.AppendAllText(Path.Combine(output,"adventure-ui-path.txt"),"\nNormal GameManager defeat/results="+results);
        }
        void Finish()
        {Time.timeScale=1;Application.logMessageReceived-=Logged;File.WriteAllText(Path.Combine(output,"complete.txt"),"runtimeErrors="+errors);Application.Quit(errors==0?0:1);}
    }
}
#endif
