#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // UI/Adventure tests may legitimately write selections and collect rewards.
    // They require the separately named QA player, never the user's save profile.
    public sealed class BlacklinePresentationProbe : MonoBehaviour
    {
        string output;
        int errors;
        readonly List<string> checks=new List<string>();
        readonly bool[] stages=new bool[4];
        MiniTurboStage captureStage;
        bool stageCaptureBusy;
        RacingVehiclePhysics captureVehicle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            string[] args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--blackline-evidence")<0)return;
            if(Application.companyName!="HillAdventurer.BlacklineQA")
            {Debug.LogError("Evidence tests require the isolated Blackline QA save profile.");Application.Quit(1);return;}
            var probe=new GameObject("Explicit Blackline presentation validation").AddComponent<BlacklinePresentationProbe>();
            int i=Array.IndexOf(args,"--blackline-output");probe.Begin(i>=0?args[i+1]:"Logs/Blackline/evidence");
        }
        void Begin(string directory)
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            output=Path.GetFullPath(directory);Directory.CreateDirectory(output);
            Application.logMessageReceived+=Logged;
            RacingQualityScope.OverrideForValidation(RacingQuality.High);
            // This is the QA profile, guarded above. The real user build is untouched.
            RacingSaveManager.SelectedVehicleId=MetaProgressionConfig.MetaVehicleId;
            RacingSaveManager.SelectedWheelId=MetaProgressionConfig.MetaWheelId;
            StartCoroutine(Run());
        }
        void Logged(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
            errors++;File.AppendAllText(Path.Combine(output,"runtime-errors.txt"),message+"\n"+stack+"\n");
        }
        void Check(bool valid,string label){checks.Add((valid?"PASS ":"FAIL ")+label);if(!valid)errors++;}
        static Button ButtonNamed(string name)
        {foreach(var b in FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name==name&&b.gameObject.activeInHierarchy)return b;return null;}
        void Click(string name)
        {var b=ButtonNamed(name);Check(b!=null&&b.interactable,"Visible actionable button: "+name);if(b!=null&&b.interactable)b.onClick.Invoke();}
        void Capture(string name)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--blackline-onscreen")>=0)
                ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
            else RacingEvidenceCapture.Capture(Path.Combine(output,name+".png"));
        }
        IEnumerator Loaded(string scene)
        {
            float deadline=Time.realtimeSinceStartup+30;
            while((SceneManager.GetActiveScene().name!=scene||RacingLoadingScreen.IsLoading)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.75f);
            Check(SceneManager.GetActiveScene().name==scene,"Scene navigation: "+scene);
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            Check(FindFirstObjectByType<RacingMainMenuUI>()!=null,"Actual new main menu constructed");
            foreach(string name in new[]{"ADVENTURE","RACING","GARAGE","SHOP"})Check(ButtonNamed(name)!=null,"Main menu mode: "+name);
            Capture("01-main-menu");yield return null;
            foreach(string section in new[]{"GARAGE","SHOP"})
            {
                Click(section);yield return Loaded("Garage");
                Check(GameSession.RequestedGarageSection==(section=="GARAGE"?GarageSection.Vehicle:GarageSection.Store)&&!GameSession.HasPendingAdventureTrack,
                    "Main menu "+section+" opens its intended section without a stale Adventure run");
                Capture("main-"+section.ToLowerInvariant());yield return null;
                var garage=FindFirstObjectByType<AnimeHillRacing.UI.GarageUI>();
                Check(garage!=null,"Adventure Garage controller available");
                if(garage!=null)garage.Back();yield return Loaded("PreStart");
                Check(FindFirstObjectByType<RacingMainMenuUI>()!=null,"Garage back returns to the true main menu");
            }
            Click("RACING");yield return new WaitForSecondsRealtime(.25f);
            Check(RacingLoadingScreen.IsLoading,"Real async loading screen with progress car");Capture("02-loading-car-bar");
            yield return Loaded("RacingSelect");
            Capture("racing-hub");yield return null;
            var hub=FindFirstObjectByType<RacingModeSelectUI>();
            foreach(int rating in new[]{1000,10000,25000,50000,75000,90000,100000,125000})
            {
                hub.ShowMmrInspector(rating,4207);yield return null;yield return null;
                var rows=GameObject.Find("Lobby rows");Check(rows!=null&&rows.GetComponentsInChildren<Button>().Length==12,"MMR inspector twelve rows: "+rating);
            }
            hub.ShowMmrInspector(75000,4207);yield return new WaitForSecondsRealtime(.2f);Capture("03a-mmr-75000-lobby");yield return null;
            Click("DRIVE TEST");yield return Loaded("RacingGarage");
            Capture("04-formula-blackline-closeup");yield return null;
            Click("CONFIRM BUILD");yield return new WaitForSecondsRealtime(.35f);Capture("ranked-briefing");yield return null;
            Click("START RACE");
            while(SceneManager.GetActiveScene().name!="Racing3D")yield return null;
            RaceManager manager=null;
            while(manager==null||manager.State!=RaceState.Countdown)
            {manager=FindFirstObjectByType<RaceManager>();yield return null;}
            Time.timeScale=0;
            Check(!manager.PersistResults,"Test race preserves ranked rewards");
            var camera=Camera.main;var follow=camera.GetComponent<RacingCamera3D>();if(follow!=null)follow.enabled=false;
            var hud=GameObject.Find("Racing HUD");if(hud!=null)hud.SetActive(false);
            int exact=0;Vector3 gridCenter=Vector3.zero;
            foreach(var identity in manager.LobbyRacers)if(!identity.isHuman&&identity.buildCategory==MetaBuildCategory.Meta)exact++;
            foreach(var vehicle in manager.SpawnedVehicles)gridCenter+=vehicle.transform.position;
            gridCenter/=manager.SpawnedVehicles.Count;
            Check(exact>=10,"Physical 75k lobby has at least ten exact META bots");
            camera.fieldOfView=58;camera.transform.position=gridCenter+new Vector3(10,28,-23);camera.transform.LookAt(gridCenter);
            var caption=RacingDesign.Canvas("Evidence caption",90);
            var plate=RacingDesign.CompetitivePanel(caption,"Elite grid",.04f,.87f,.57f,.085f,RacingDesign.Cyan);
            RacingDesign.Label(plate.transform,"75,000 MMR / "+exact+" OF 11 AI: EXACT BLACKLINE",RacingType.Section,.035f,.12f,.93f,.76f,RacingDesign.Ink);
            yield return null;Capture("03b-physical-elite-grid");yield return null;Destroy(caption.GetComponentInParent<Canvas>().gameObject);yield return null;
            var graph=manager.ProgressTracker.TrackGraph;
            var city=graph.GetNode(34);camera.transform.position=city.Position+city.Right*20+Vector3.up*12-city.Forward*14;camera.transform.LookAt(city.Position+city.Forward*35);camera.fieldOfView=65;
            yield return null;Capture("07-city-depth-background");yield return null;
            TrackShortcut drop=null;foreach(var route in graph.Shortcuts)if(route.shortcutId=="return_drop")drop=route;
            Check(drop!=null,"Hairpin Drop exists in the playable world");
            if(drop!=null)
            {
                Vector3 center=drop.pathPoints[12];camera.transform.position=center+new Vector3(-22,25,-32);camera.transform.LookAt(center);camera.fieldOfView=62;
                yield return null;Capture("06-hairpin-drop-shortcut");yield return null;
            }
            if(hud!=null)hud.SetActive(true);if(follow!=null)follow.enabled=true;camera.fieldOfView=60;
            var player=manager.PlayerVehicle;player.GetComponent<PlayerRacingInput>().SetInputEnabled(false);
            var ai=player.gameObject.AddComponent<AiVehicleController>();ai.Initialize(AiSkillModel.CreateProfile(75000,manager.LobbyRacers[0].personalitySeed),graph,manager.ProgressTracker,"EXPLICIT EVIDENCE DRIVER");
            foreach(var racer in manager.SpawnedVehicles)
            {
                var observed=racer;
                observed.OnDriftStage+=stage=>DriftStage(observed,stage);
            }
            Time.timeScale=1;
            float deadline=Time.realtimeSinceStartup+270;
            while(manager.State!=RaceState.Results&&Time.realtimeSinceStartup<deadline)
            {
                if(captureStage!=MiniTurboStage.None&&!stageCaptureBusy)
                {
                    stageCaptureBusy=true;var stage=captureStage;var observed=captureVehicle;captureStage=MiniTurboStage.None;
                    if(follow!=null)follow.SetTarget(observed.transform,observed);
                    camera.transform.position=observed.transform.position-observed.transform.forward*8+Vector3.up*4;
                    camera.transform.LookAt(observed.transform.position+observed.transform.forward*4);
                    if(hud!=null)hud.SetActive(false);
                    yield return new WaitForSecondsRealtime(.13f);
                    if(observed.CurrentMiniTurboStage==stage)
                    {Capture("05-drift-"+stage);stages[(int)stage]=true;}
                    yield return null;
                    if(hud!=null)hud.SetActive(true);if(follow!=null)follow.SetTarget(player.transform,player);
                    stageCaptureBusy=false;
                }
                yield return null;
            }
            Check(manager.State==RaceState.Results,"Full test race reaches Results");
            Check(stages[1]&&stages[2]&&stages[3],"Green, cyan and purple captured from real driving charge");
            if(manager.State==RaceState.Results){yield return new WaitForSecondsRealtime(1.7f);Capture("racing-results");yield return null;}
            RacingTestSession.Clear();RacingLoadingScreen.Load("MainMenu","ADVENTURE REGRESSION");yield return Loaded("MainMenu");
            Click("ADVENTURE");yield return Loaded("PreStart");
            Check(GameSession.IsAdventureSelectionRequested&&FindFirstObjectByType<AnimeHillRacing.UI.PreStartMenuUI>()!=null,"Adventure selection still opens from the new main menu");Capture("adventure-track-selection");yield return null;
            GameSession.SelectedVehicleId="car-01";GameSession.SelectedMapId="countryside";GameSession.SelectedGameMode=GameModeType.Adventure2_5D;
            RacingLoadingScreen.Load("Adventure2_5D","ADVENTURE / COUNTRYSIDE");yield return Loaded("Adventure2_5D");
            var adventure=new GameObject("Explicit Adventure regression").AddComponent<AdventurePhysics2DPlayModeProbe>();adventure.Configure("car-01","countryside",false);
            deadline=Time.realtimeSinceStartup+150;
            while(!adventure.Completed&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(adventure.Completed&&adventure.Passed,"Real Adventure Box2D driving, braking, reverse, terrain, suspension and landing regression");
            File.WriteAllText(Path.Combine(output,"adventure-regression.txt"),adventure.ResultLine+"\n"+adventure.FailureSummary);Capture("adventure-runtime");yield return null;
            File.WriteAllLines(Path.Combine(output,"presentation-checks.txt"),checks);
            File.WriteAllText(Path.Combine(output,"complete.txt"),"errors="+errors+"\nprofile="+Application.companyName+"/"+Application.productName);
            Application.logMessageReceived-=Logged;Application.Quit(errors==0?0:1);
        }
        void DriftStage(RacingVehiclePhysics vehicle,MiniTurboStage stage)
        {if(!stageCaptureBusy&&stage!=MiniTurboStage.None&&!stages[(int)stage]){captureStage=stage;captureVehicle=vehicle;}}
    }
}
#endif
