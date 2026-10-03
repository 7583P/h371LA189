#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    // Controlled physical ramp fixtures, separate from full-race/performance tests.
    // Never installed in an ordinary player or allowed to write the human profile.
    public sealed class BlacklineShortcutProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--blackline-shortcuts")<0)return;
            if(Application.companyName!="HillAdventurer.BlacklineQA"){Application.Quit(1);return;}
            var root=new GameObject("Explicit physical shortcut fixtures");DontDestroyOnLoad(root);
            root.AddComponent<BlacklineShortcutProbe>().StartCoroutine(root.GetComponent<BlacklineShortcutProbe>().Run());
        }
        static string Arg(string flag,string fallback)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        IEnumerator Run()
        {
            string output=Path.GetFullPath(Arg("--blackline-output","Logs/Blackline/shortcut-fixtures"));Directory.CreateDirectory(output);
            Application.runInBackground=true;RacingQualityScope.OverrideForValidation(RacingQuality.Low);
            RacingTestSession.Set(100000,4207);SceneManager.LoadScene("Racing3D");
            RaceManager manager=null;while(manager==null||manager.PlayerVehicle==null){manager=FindFirstObjectByType<RaceManager>();yield return null;}
            manager.PersistResults=false;manager.StopAllCoroutines();manager.enabled=false;manager.ProgressTracker.enabled=false;
            foreach(var car in manager.SpawnedVehicles)car.gameObject.SetActive(false);
            var metrics=FindFirstObjectByType<RacingRaceMetrics>();if(metrics!=null)metrics.enabled=false;
            var graph=manager.ProgressTracker.TrackGraph;Time.timeScale=float.Parse(Arg("--blackline-timescale","4"),System.Globalization.CultureInfo.InvariantCulture);
            int samples=int.Parse(Arg("--blackline-shortcut-samples","1"));
            int[] ratings=Array.ConvertAll(Arg("--blackline-ratings","75000,100000").Split(','),int.Parse);
            string routes=Arg("--blackline-routes","crest_cut,return_drop,overpass_launch,skyway_jump,ridge_cut,waterfall_gap");
            bool mainline=Array.IndexOf(Environment.GetCommandLineArgs(),"--blackline-fixture-mainline")>=0;
            File.WriteAllText(Path.Combine(output,"geometry.json"),JsonUtility.ToJson(new ShortcutGeometry{routes=graph.Shortcuts},true));
            using(var results=new StreamWriter(Path.Combine(output,"results.csv")))
            {
                results.WriteLine("route,mmr,seed,startSpeedKmh,startOffset,entries,completions,failed,rejoined,recoveries,jumps,wallCollisions,elapsedSeconds,recoveryReason");
                foreach(var route in graph.Shortcuts)foreach(int rating in ratings)for(int sample=0;sample<samples;sample++)
                {
                    if(Array.IndexOf(routes.Split(','),route.shortcutId)<0)continue;
                    int seed=4207+sample*997;
                    var identity=new RacerIdentity("fixture","SHORTCUT FIXTURE","formula-01","wheel-plasma",rating,false,seed,1);
                    var root=RacingVehicleFactory.Create(identity);var car=root.GetComponent<RacingVehiclePhysics>();
                    var start=graph.SampleDistance(graph.GetNode(route.entryNodeIndex).CumulativeDistance-25);
                    float offset=samples>3?(sample%3-1)*.65f:0;
                    root.transform.SetPositionAndRotation(start.Position+start.Right*offset+Vector3.up*.75f,Quaternion.LookRotation(start.Forward,Vector3.up));
                    var inventory=root.AddComponent<ItemInventory>();inventory.SetSeed(seed);
                    if(Arg("--blackline-fixture-boost","item")=="item")inventory.AcquireItem(RaceItemType.TurboPulse,false);
                    var recovery=root.AddComponent<VehicleRecovery>();
                    var ai=root.AddComponent<AiVehicleController>();var profile=AiSkillModel.CreateProfile(rating,seed);
                    if(mainline)profile.shortcutKnowledge=0;
                    ai.Initialize(profile,graph,null,"SHORTCUT FIXTURE");
                    car.Initialize(identity.vehicleId,identity.wheelId,ai);car.SetRaceEnabled(true);
                    Physics.SyncTransforms();car.Body.linearVelocity=start.Forward*(sample%2==0?30:36);
                    float began=Time.time;int routeIndex=Array.IndexOf(graph.Shortcuts,route);
                    var trace=root.AddComponent<BlacklineShortcutRecorder>();
                    trace.Bind(graph,Path.Combine(output,route.shortcutId+"-"+rating+"-"+seed+".csv"),routeIndex);
                    FindFirstObjectByType<RacingCamera3D>()?.SetTarget(root.transform,car);
                    bool rejoined=false;float stable=0,resolvedAt=-1;
                    while(Time.time-began<35)
                    {
                        if(resolvedAt<0&&(ai.RouteCompletions(routeIndex)>0||ai.FailedShortcuts>0||recovery.RecoveryCount>0))resolvedAt=Time.time;
                        if(resolvedAt>=0||mainline)
                        {
                            graph.ProjectPosition(car.Body.position,out _,out float along,out float lateral);
                            var road=graph.SampleDistance(along);
                            float advanced=Mathf.Repeat(along-graph.GetNode(route.exitNodeIndex).CumulativeDistance,graph.TotalLength);
                            bool clean=ai.ActiveRouteIndex!=routeIndex&&car.IsGrounded&&!recovery.IsRecovering&&car.SpeedKmh>35&&
                                Mathf.Abs(lateral)<road.TrackWidth*.4f&&Mathf.Abs(car.Body.position.y-road.Position.y)<2&&
                                Vector3.Angle(Vector3.ProjectOnPlane(car.Body.linearVelocity,Vector3.up),Vector3.ProjectOnPlane(road.Forward,Vector3.up))<30&&
                                advanced>3&&advanced<100;
                            stable=clean?stable+Time.deltaTime:0;
                            if(stable>.5f){rejoined=true;trace.Record("rejoined");break;}
                            if(!mainline&&Time.time-resolvedAt>12)break;
                        }
                        yield return null;
                    }
                    trace.Record("fixture_end");trace.Close();
                    results.WriteLine(FormattableString.Invariant($"{route.shortcutId},{rating},{seed},{(sample%2==0?108:129.6f):F1},{offset:F2},{ai.RouteEntries(routeIndex)},{ai.RouteCompletions(routeIndex)},{ai.FailedShortcuts},{rejoined},{recovery.RecoveryCount},{car.JumpsPerformed},{car.WallCollisions},{Time.time-began:F2},{recovery.LastRecoveryReason}"));results.Flush();
                    Destroy(root);yield return null;
                }
            }
            Time.timeScale=1;RacingTestSession.Clear();File.WriteAllText(Path.Combine(output,"complete.txt"),"Controlled shortcut physics fixtures complete. See actual entries and completions; not a full race.\n");Application.Quit();
        }
        [Serializable] sealed class ShortcutGeometry{public TrackShortcut[] routes;}
    }

    // Opt-in fixed-step physical traces. This recorder is never attached to human
    // gate players or performance runs; writing diagnostics has its own cost.
    [DefaultExecutionOrder(100)]
    public sealed class BlacklineShortcutRecorder:MonoBehaviour
    {
        RacingVehiclePhysics car;AiVehicleController ai;VehicleRecovery recovery;TrackSplineGraph graph;
        StreamWriter writer;float began;int observedRoute=-1,lastActive=-1,lastComplete,lastFailed;
        bool awaitingTakeoff;
        readonly RaycastHit[] hits=new RaycastHit[24];
        public void Bind(TrackSplineGraph track,string path,int fixtureRoute=-1)
        {
            graph=track;car=GetComponent<RacingVehiclePhysics>();ai=GetComponent<AiVehicleController>();recovery=GetComponent<VehicleRecovery>();
            began=Time.time;observedRoute=fixtureRoute;writer=new StreamWriter(path);
            writer.WriteLine("seconds,event,mmr,route,activeRoute,segment,currentNode,targetNode,grounded,speedKmh,targetSpeedKmh,steering,throttle,brake,boost,boostSeconds,drift,slip,x,y,z,vx,vy,vz,fx,fy,fz,desiredX,desiredY,desiredZ,forwardError,velocityError,lateral,airborneSeconds,tx,ty,tz,surface,surfaceY,landingAngle,rejoinNode,entries,completions,failed,recoveries,jumps,tricks,landingBoosts,cameraX,cameraY,cameraZ");
            car.OnJump+=Jump;car.OnLanding+=Landing;if(recovery!=null)recovery.OnRecoveryRequested+=Recover;
        }
        void FixedUpdate()
        {
            if(writer==null)return;
            int active=ai.ActiveRouteIndex;
            if(active>=0)observedRoute=active;
            if(active!=lastActive){Record(active>=0?"committed":"route_left");lastActive=active;}
            if(ai.ShortcutCompletions!=lastComplete){Record("completed");lastComplete=ai.ShortcutCompletions;}
            if(ai.FailedShortcuts!=lastFailed){Record("abandoned:"+ai.LastRouteAbortReason);lastFailed=ai.FailedShortcuts;}
            if(awaitingTakeoff&&!car.IsGrounded){Record("takeoff");awaitingTakeoff=false;}
            Record("step");
        }
        void Jump(){awaitingTakeoff=true;Record("jump");}
        void Landing(float intensity){Record("landing");}
        void Recover(string reason){Record("recovery:"+reason);}
        void OnCollisionEnter(Collision collision)
        {Record("collision:"+collision.collider.name.Replace(',',';'));}
        public void Record(string kind)
        {
            if(writer==null||car==null)return;
            var route=observedRoute>=0?graph.Shortcuts[observedRoute]:null;
            Vector3 p=car.Body.position,v=car.Body.linearVelocity,f=car.Body.rotation*Vector3.forward,t=ai.CurrentTarget;
            Vector3 desired=route!=null?(route.pathPoints[route.launchNodeIndex+1]-route.pathPoints[route.launchNodeIndex]).normalized:Vector3.forward;
            Vector3 horizontal=Vector3.ProjectOnPlane(desired,Vector3.up).normalized;
            float lateral=route!=null?Vector3.Dot(p-route.pathPoints[route.launchNodeIndex],Vector3.Cross(Vector3.up,horizontal)):0;
            string surface="none";float surfaceY=float.NaN;Vector3 normal=Vector3.up;float nearest=float.MaxValue;
            int count=Physics.RaycastNonAlloc(p+Vector3.up*.3f,Vector3.down,hits,3,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(hits[i].collider.attachedRigidbody==null&&hits[i].normal.y>.28f&&hits[i].distance<nearest)
            {nearest=hits[i].distance;surface=hits[i].collider.name.Replace(',',';');surfaceY=hits[i].point.y;normal=hits[i].normal;}
            graph.ProjectPosition(p,out int rejoin,out _,out _);
            Vector3 cp=Camera.main!=null?Camera.main.transform.position:Vector3.zero;var c=car.LastCommands;
            writer.WriteLine(FormattableString.Invariant($"{Time.time-began:F3},{kind},{ai.Profile.mmr},{route?.shortcutId},{ai.ActiveRouteIndex},{ai.RouteSegment},{ai.CurrentNodeIndex},{ai.TargetNodeIndex},{car.IsGrounded},{car.SpeedKmh:F3},{ai.TargetSpeedKmh:F3},{c.Steering:F3},{c.Throttle:F3},{c.Brake:F3},{car.IsBoosting},{car.BoostRemainingSeconds:F3},{car.IsDrifting},{car.SlipAngle:F3},{p.x:F3},{p.y:F3},{p.z:F3},{v.x:F3},{v.y:F3},{v.z:F3},{f.x:F4},{f.y:F4},{f.z:F4},{desired.x:F4},{desired.y:F4},{desired.z:F4},{Vector3.Angle(Vector3.ProjectOnPlane(f,Vector3.up),horizontal):F3},{Vector3.Angle(Vector3.ProjectOnPlane(v,Vector3.up),horizontal):F3},{lateral:F3},{car.AirborneTime:F3},{t.x:F3},{t.y:F3},{t.z:F3},{surface},{surfaceY:F3},{Vector3.Angle(car.Body.rotation*Vector3.up,normal):F3},{rejoin},{ai.ShortcutEntries},{ai.ShortcutCompletions},{ai.FailedShortcuts},{recovery?.RecoveryCount??0},{car.JumpsPerformed},{car.TricksPerformed},{car.LandingBoosts},{cp.x:F3},{cp.y:F3},{cp.z:F3}"));
        }
        public void Close(){if(writer==null)return;writer.Dispose();writer=null;}
        void OnDestroy(){Close();if(car!=null){car.OnJump-=Jump;car.OnLanding-=Landing;}if(recovery!=null)recovery.OnRecoveryRequested-=Recover;}
    }
}
#endif
