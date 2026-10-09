#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using AnimeHillRacing.Racing3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingLancer300Contracts
    {
        [Serializable] sealed class ProjectionProof { public int samples;public double referenceMs,indexedMs;public long indexedAllocatedBytes;public float maxAlongError,maxLateralError; }
        [Serializable] sealed class SaveProof { public string classification="ISOLATED_10K_LEAGUE_OLD_TRUE_TRANSITION_PATH_VS_IDEMPOTENT_TRUE_TRANSITIONS";public double oldSaveMs,new100TransitionsMs;public long oldAllocatedBytes,newAllocatedBytes,oldSnapshots,oldSaveRequests,newSnapshots,newSaveRequests; }
        public static void Run()
        {
            string output=RacingFrameForensics.Argument("--closure-output");Directory.CreateDirectory(output);
            var report=new ClosureContractReport();var previous=SceneManager.GetActiveScene();
            if(Application.isBatchMode&&string.IsNullOrEmpty(previous.path))previous=EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                report.Check(RaceItemUtility.LancerSpeedKmh==300&&RaceItemUtility.LancerDurationSeconds==6,"Lancer shared target 300 km/h, six seconds");
                Controls(report);
                SaveTransitions(report,output);
                var graph=new GameObject("Projection contract").AddComponent<TrackSplineGraph>();
                var nodes=new TrackNode[512];
                for(int i=0;i<nodes.Length;i++){float a=i*Mathf.PI*2/nodes.Length;nodes[i]=new TrackNode{Position=new Vector3(Mathf.Sin(a)*600,Mathf.Sin(3*a)*45,Mathf.Cos(a)*450),TrackWidth=24};}
                graph.InitializeFromNodes(nodes);Projections(graph,report,output);
                graph.InitializeFromNodes(new[]{new TrackNode{Position=new Vector3(80,0,-500)},new TrackNode{Position=new Vector3(80,0,500)},new TrackNode{Position=new Vector3(180,0,500)},new TrackNode{Position=new Vector3(180,0,-500)}},new[]{new TrackShortcut{entryNodeIndex=0,exitNodeIndex=1,width=8,requiresBoost=false,pathPoints=new[]{new Vector3(0,0,-400),new Vector3(0,0,400)}}});
                report.Check(graph.OfficialShortcutAt(new Vector3(0,.5f,0))!=null,"Official spatial corridor detects a car independently of human/AI intent");
                report.Check(graph.OfficialShortcutAt(new Vector3(5,.5f,0))==null,"Outside authored width has no penalty");
                report.Check(graph.OfficialShortcutAt(new Vector3(0,20,0))==null,"Road above/below an unrelated shortcut is excluded");
                report.Check(graph.OfficialShortcutAt(new Vector3(80,.5f,0))==null,"Main road remains unpenalized");
                report.Check(graph.OfficialShortcutAt(new Vector3(0,.5f,401))==null&&graph.OfficialShortcutAt(new Vector3(0,.5f,-401))==null,"Entry and exit end planes restore normal speed");
                var car=new GameObject("Shared human and bot motor").AddComponent<RacingVehiclePhysics>();car.BindRaceTrack(graph);car.Body.position=new Vector3(0,.5f,0);car.StepPhysics();
                string stats=JsonUtility.ToJson(car.Stats);
                report.Check(car.ShortcutSpeedMultiplier==.5f&&car.EffectiveLancerTargetKmh==150,"Official shortcut uses 0.50 including Lancer 300 -> 150");
                car.Body.position=new Vector3(80,.5f,0);car.StepPhysics();report.Check(car.ShortcutSpeedMultiplier==1&&car.EffectiveLancerTargetKmh==300,"Spatial exit restores 300 target without rewriting stats");
                car.Body.position=new Vector3(0,.5f,0);car.StepPhysics();car.ResetVelocities();report.Check(car.ShortcutSpeedMultiplier==1,"Recovery/reset clears transient shortcut state");
                car.StepPhysics();car.SetRaceEnabled(false);report.Check(car.ShortcutSpeedMultiplier==1,"Stopping/restarting a race clears shortcut state");
                report.Check(JsonUtility.ToJson(car.Stats)==stats,"Shortcut never changes engine, acceleration, speed, grip, mass or save tuning");
                // Real authored geometry: report conservative equal-speed travel
                // estimates separately from actual matched driving measurements.
                var city=new GameObject("Night City contract").AddComponent<CanyonRushBuilder>();city.Build();
                Projections(city.Graph,report,output,"night-city-");
                var routeReport=new System.Text.StringBuilder("route,main_length_m,shortcut_length_m,shortcut_penalty,main_at_150_s,shortcut_at_75_s\n");
                foreach(var route in city.Graph.Shortcuts){float length=0;for(int i=1;i<route.pathPoints.Length;i++)length+=Vector3.Distance(route.pathPoints[i-1],route.pathPoints[i]);float main=Mathf.Repeat(city.Graph.GetNode(route.exitNodeIndex).CumulativeDistance-city.Graph.GetNode(route.entryNodeIndex).CumulativeDistance,city.Graph.TotalLength);routeReport.AppendLine(FormattableString.Invariant($"{route.shortcutId},{main},{length},0.50,{main/(150f/3.6f)},{length/(75f/3.6f)}"));}
                File.WriteAllText(Path.Combine(output,"shortcut-route-estimates.csv"),routeReport.ToString());
            } finally {if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            File.WriteAllText(Path.Combine(output,"lancer300-contracts.json"),JsonUtility.ToJson(report,true));
            if(report.failed!=0)throw new InvalidOperationException("Lancer300 contracts failed: "+report.failed);
            UnityEngine.Debug.Log("LANCER300_CONTRACTS passed="+report.passed);
            if(RacingFrameForensics.Argument("--closure-mode")=="build")RacingClosureValidation.Run();
        }
        static void UpdateInput()
        {
            // The public parameterless API selects Editor updates outside play.
            // Exercise the installed package's actual player Dynamic update.
            typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
        }
        static void SaveTransitions(ClosureContractReport report,string output)
        {
            var state=RacingProgressionService.State;
            RacingProgressionService.SetRaceActive(true);RacingProgressionService.MarkChanged();
            var proof=new SaveProof();long oldCopies=RacingProgressionService.SaveSnapshotCopies,oldRequests=RacingProgressionService.SaveRequestsDuringGameplay;
            long allocated=GC.GetAllocatedBytesForCurrentThread(),before=Stopwatch.GetTimestamp();
            // Reproduce the previous SetRaceActive(true) body on the actual
            // isolated store; no mock timing or real commercial transactions.
            RacingProgressionService.Save(true);
            proof.oldSaveMs=(Stopwatch.GetTimestamp()-before)*1000d/Stopwatch.Frequency;proof.oldAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            proof.oldSnapshots=RacingProgressionService.SaveSnapshotCopies-oldCopies;proof.oldSaveRequests=RacingProgressionService.SaveRequestsDuringGameplay-oldRequests;
            report.Check(proof.oldSnapshots==1&&proof.oldSaveRequests==1,"Old forced-save transition reproduces exactly one real 10k snapshot and synchronous commit");
            RacingProgressionService.MarkChanged();long copies=RacingProgressionService.SaveSnapshotCopies,requests=RacingProgressionService.SaveRequestsDuringGameplay;
            allocated=GC.GetAllocatedBytesForCurrentThread();before=Stopwatch.GetTimestamp();
            for(int i=0;i<100;i++)RacingProgressionService.SetRaceActive(true);
            proof.new100TransitionsMs=(Stopwatch.GetTimestamp()-before)*1000d/Stopwatch.Frequency;proof.newAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            proof.newSnapshots=RacingProgressionService.SaveSnapshotCopies-copies;proof.newSaveRequests=RacingProgressionService.SaveRequestsDuringGameplay-requests;
            report.Check(proof.newSnapshots==0&&proof.newSaveRequests==0&&proof.newAllocatedBytes==0,"100 active-to-active transitions copy no league snapshot, request no save and allocate zero bytes");
            RacingProgressionService.SetRaceActive(false);RacingProgressionService.Save(true);
            report.Check(!RacingProgressionService.IsRaceActive&&!RacingProgressionService.HasPendingSave,"Dirty progression still commits after exiting gameplay");
            report.Check(state.bots.Length==10000,"Save transition contract uses the real 10000-bot store");
            File.WriteAllText(Path.Combine(output,"league-save-transition-ab.json"),JsonUtility.ToJson(proof,true));
        }
        static void Controls(ClosureContractReport report)
        {
            // The installed Input System skips player updates outside play
            // unless this documented test flag is enabled. Clone settings so
            // neither the project asset nor subsequent editor input is changed.
            var originalSettings=InputSystem.settings;
            var fixtureSettings=UnityEngine.Object.Instantiate(originalSettings);
            fixtureSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);
            InputSystem.settings=fixtureSettings;
            var car=new GameObject("Input contract").AddComponent<RacingVehiclePhysics>();var input=car.gameObject.AddComponent<PlayerRacingInput>();
            var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
            try {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));InputSystem.QueueStateEvent(mouse,new MouseState());UpdateInput();
                report.Check(input.SampleCommands().Drift&&!input.LookBack,"Shift drifts without moving the rear camera");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));UpdateInput();var command=input.SampleCommands();
                report.Check(input.LookBack&&command.Throttle==1&&!command.UseItem,"RMB rear view + W preserves full throttle and does not auto-fire");
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));UpdateInput();command=input.SampleCommands();
                report.Check(command.UseItem&&command.ItemBackward&&command.Throttle==1,"RMB + LMB fires backward while W still accelerates");
                UpdateInput();report.Check(!input.SampleCommands().UseItem,"Holding the mouse button cannot repeat activation");
                InputSystem.QueueStateEvent(mouse,new MouseState());UpdateInput();input.SampleCommands();report.Check(!input.LookBack,"Releasing RMB restores the forward camera state");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));UpdateInput();command=input.SampleCommands();
                report.Check(command.UseItem&&!command.ItemBackward,"LMB alone fires forward");
                InputSystem.QueueStateEvent(mouse,new MouseState());UpdateInput();input.SampleCommands();
                input.SetTouchLookBack(true);input.SetTouchDrift(true);input.TriggerTouchItem();command=input.SampleCommands();
                report.Check(command.UseItem&&command.ItemBackward&&command.Drift&&input.LookBack,"Touch rear hold + use fires backward; drift remains independent");
                report.Check(!input.SampleCommands().UseItem,"One touch press is consumed once");
                input.SetTouchLookBack(false);input.TriggerTouchItem();command=input.SampleCommands();report.Check(command.UseItem&&!command.ItemBackward,"Touch use fires forward after rear release");
                input.SetInputEnabled(false);report.Check(!input.LookBack&&!input.SampleCommands().UseItem&&!input.SampleCommands().Drift,"Pause/focus loss clears all held commands");
            } finally {InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings=originalSettings;UnityEngine.Object.DestroyImmediate(fixtureSettings);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();}
        }
        static void Projections(TrackSplineGraph graph,ClosureContractReport report,string output,string prefix="")
        {
            var proof=new ProjectionProof{samples=20000};var rng=new System.Random(4207);var points=new Vector3[proof.samples];
            for(int i=0;i<points.Length;i++)points[i]=graph.GetNode(rng.Next(graph.NodeCount)).Position+new Vector3((float)rng.NextDouble()*200-100,(float)rng.NextDouble()*60-30,(float)rng.NextDouble()*200-100);
            bool equal=true;
            foreach(var p in points){graph.ProjectPosition(p,out int a,out float x,out float y);graph.ProjectPositionReference(p,out int b,out float xx,out float yy);equal&=a==b;proof.maxAlongError=Mathf.Max(proof.maxAlongError,Mathf.Abs(x-xx));proof.maxLateralError=Mathf.Max(proof.maxLateralError,Mathf.Abs(y-yy));}
            report.Check(equal&&proof.maxAlongError==0&&proof.maxLateralError==0,prefix+"20000 indexed projections exactly equal exhaustive segment/along/lateral results");
            long started=Stopwatch.GetTimestamp();foreach(var p in points)graph.ProjectPositionReference(p,out _,out _,out _);proof.referenceMs=(Stopwatch.GetTimestamp()-started)*1000d/Stopwatch.Frequency;
            long allocated=GC.GetAllocatedBytesForCurrentThread();started=Stopwatch.GetTimestamp();foreach(var p in points)graph.ProjectPosition(p,out _,out _,out _);proof.indexedMs=(Stopwatch.GetTimestamp()-started)*1000d/Stopwatch.Frequency;proof.indexedAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            report.Check(proof.indexedAllocatedBytes==0,prefix+"Indexed projection is allocation-free during gameplay");
            File.WriteAllText(Path.Combine(output,prefix+"projection-benchmark.json"),JsonUtility.ToJson(proof,true));
        }
    }
}
#endif
