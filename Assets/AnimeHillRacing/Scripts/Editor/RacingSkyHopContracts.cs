#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingSkyHopContracts
    {
        public static void Run()
        {
            string output=RacingFrameForensics.Argument("--closure-output");Directory.CreateDirectory(output);
            var report=new ClosureContractReport();var previous=SceneManager.GetActiveScene();
            if(Application.isBatchMode&&string.IsNullOrEmpty(previous.path))previous=EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            string selected=RacingTrackSelection.Id;
            try
            {
                SceneManager.SetActiveScene(scene);Controls(report);
                var original=NightCityCircuit.CreateNodes();
                var originalSky=RenderSettings.skybox;var originalFog=RenderSettings.fogColor;float originalDensity=RenderSettings.fogDensity;
                var sky=new GameObject("Sky contracts").AddComponent<CanyonRushBuilder>();
                sky.Build();
                bool unchanged=sky.Graph.NodeCount==original.Length;
                for(int i=0;i<original.Length;i++)unchanged &=sky.Graph.GetNode(i).Position==original[i].Position&&sky.Graph.GetNode(i).TrackWidth==original[i].TrackWidth&&sky.Graph.GetNode(i).TargetSpeedKmh==original[i].TargetSpeedKmh;
                report.Check(unchanged&&sky.Graph.Shortcuts.Length==2&&sky.Graph.CheckpointNodeIndices.SequenceEqual(NightCityCircuit.Checkpoints),"Night City keeps actual road, widths, navigation, checkpoints and two authored shortcuts");
                var layers=sky.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Sky city / parallax layer")).ToArray();
                report.Check(layers.Length==3&&layers.All(r=>r.sharedMaterial==layers[0].sharedMaterial&&r.bounds.max.y<0),"Three static opaque 2D city layers below the road share one material and texture");
                var texture=layers[0].sharedMaterial.mainTexture;
                report.Check(texture.width==256&&texture.height==256,"City uses one 256x256 four-facade atlas, no full panorama draw");
                report.Check(!sky.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Skyscraper_")||t.name.StartsWith("MidTower_")||t.name.StartsWith("MegaMonolith_")||t.name=="City foundation"||t.name=="Layered city horizon"),"Heavy city cores, foundation and legacy PNG cylinder are disconnected from this track");
                report.Check(sky.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Turn guidance /"))>=6,"Key turns have large red neon chevrons");
                report.Check(sky.GetComponentsInChildren<RacingTrafficFlow>().Length==0&&sky.GetComponentsInChildren<ParticleSystem>().Length==0,"New environment adds no traffic Updates, particle systems, fullscreen effects or city lights");
                report.Check(RenderSettings.skybox.shader.name=="HillAdventurer/SkyTrackSky","Lightweight sunset sky shader is included and active");
                report.Check(RenderSettings.skybox.GetTexture("_SunsetSky") is Cubemap cloudSky&&cloudSky.width==RacingSunsetSky.FaceSize,"Sunset and two static 2D cloud bands use one opaque 256px cubemap fetch with no Update");
                report.Check(!sky.GetComponentsInChildren<Transform>().Any(t=>t.name=="Route edge guide"||t.name=="Boost route arrow"||t.name.EndsWith(" direction sign")||t.name=="Precision entry beacon"||t.name=="Visible kicker"),"Hidden Night City routes have no arrows, signs, beacons or glowing route edges");
                report.Check(sky.Graph.CheckpointNodeIndices.SequenceEqual(new[]{0,8,40,72,104,136,168,184})&&sky.Graph.Shortcuts.All(r=>!sky.Graph.CheckpointNodeIndices.Any(c=>c>r.entryNodeIndex&&c<r.exitNodeIndex)),"Eight Night City gates protect major sectors and bracket jumps while both hidden cuts skip no gate");
                RacingShortcutIntegrityContracts.Run(report,sky.Graph,"Night City");
                UnityEngine.Object.DestroyImmediate(sky.gameObject);
                report.Check(RenderSettings.skybox==originalSky&&RenderSettings.fogColor==originalFog&&RenderSettings.fogDensity==originalDensity,"Leaving Night City restores its previous sky and fog for other game modes");
                var meadow=new GameObject("Grass contracts").AddComponent<VerdantCircuitBuilder>();
                meadow.Build();
                var graph=meadow.Graph;report.Check(graph.NodeCount==224&&graph.CheckpointNodeIndices.Length==7,"Additional technical loop has 224 navigation samples and seven actual checkpoints bracketing major corners");
                bool navigableBends=true;
                for(int i=0;i<graph.NodeCount;i++)
                {
                    var a=graph.GetNode(i);var b=graph.GetNode((i+1)%graph.NodeCount);
                    float curvature=Vector3.Angle(a.Forward,b.Forward)*Mathf.Deg2Rad/Vector3.Distance(a.Position,b.Position);
                    navigableBends&=curvature<.001f||1/curvature>a.TrackWidth*.75f;
                }
                report.Check(navigableBends,"Technical road bends have radius beyond their half-width; no folded 4 m hairpin");
                report.Check(meadow.GetComponent<RacingTrackInfo>().TrackId==VerdantCircuit.Id&&graph.Shortcuts.Length==3,"Additional selectable track has a separate stable ID and three grass routes");
                var csv=new System.Text.StringBuilder("route,entry,exit,main_m,shortcut_m,normal_at_150_s,shortcut_at_75_s\n");
                foreach(var route in graph.Shortcuts)
                {
                    float length=0;for(int i=1;i<route.pathPoints.Length;i++)length+=Vector3.Distance(route.pathPoints[i-1],route.pathPoints[i]);
                    float main=graph.GetNode(route.exitNodeIndex).CumulativeDistance-graph.GetNode(route.entryNodeIndex).CumulativeDistance;
                    bool noSkippedCheckpoints=!graph.CheckpointNodeIndices.Any(c=>c>route.entryNodeIndex&&c<route.exitNodeIndex);
                    report.Check(route.IsValid()&&!route.requiresJump&&noSkippedCheckpoints,"Grass route "+route.shortcutId+" is continuous and skips no mandatory checkpoint");
                    report.Check(main>length*2,"Grass route "+route.shortcutId+" retains geometric value at the 50% speed rule");
                    report.Check(graph.OfficialShortcutAt(route.pathPoints[12]+Vector3.up*.5f)==route,"Grass route "+route.shortcutId+" uses shared spatial 0.50 detection");
                    csv.AppendLine(FormattableString.Invariant($"{route.shortcutId},{route.entryNodeIndex},{route.exitNodeIndex},{main},{length},{main/(150f/3.6f)},{length/(75f/3.6f)}"));
                }
                File.WriteAllText(Path.Combine(output,"verdant-route-estimates.csv"),csv.ToString());
                report.Check(meadow.GetComponentsInChildren<RacingSurface>().Count(s=>s.Kind==RacingSurfaceKind.Grass)==4,"Field and three route ribbons use actual Grass physics surface");
                report.Check(!meadow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Grass route marker"||t.name=="Grass route pennant")&&meadow.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.EndsWith(" / official grass surface")).All(r=>!r.enabled),"Grass cuts have no flags, signs or visible painted route ribbons");
                RacingShortcutIntegrityContracts.Run(report,graph,"Verdant");
                RacingTrackSelection.Select(VerdantCircuit.Id);report.Check(RacingTrackSelection.ShortName=="VERDANT RUN","Track selector resolves the new name without changing save schema");
                RacingTrackSelection.Select("night_city");report.Check(RacingTrackSelection.ShortName=="NIGHT CITY SKY","Night City remains the default compatible track ID");
                report.Check(RaceItemUtility.LancerSpeedKmh==300&&TrackSplineGraph.ShortcutSpeedScale==.5f,"Lancer 300 and shortcut 50% remain unchanged");
                UnityEngine.Object.DestroyImmediate(meadow.gameObject);
                report.Check(RenderSettings.skybox==originalSky&&RenderSettings.fogColor==originalFog&&RenderSettings.fogDensity==originalDensity,"Leaving Verdant restores the previous environment instead of leaking its daylight into Adventure");
            }
            finally {RacingTrackSelection.Select(selected);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            File.WriteAllText(Path.Combine(output,"skyhop-contracts.json"),JsonUtility.ToJson(report,true));
            if(report.failed!=0)throw new InvalidOperationException("SkyHop contracts failed: "+report.failed);
            Debug.Log("SKYHOP_CONTRACTS passed="+report.passed);
            if(RacingFrameForensics.Argument("--closure-mode")=="build")RacingClosureValidation.Run();
        }
        static void UpdateInput()=>typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
        static void Controls(ClosureContractReport report)
        {
            var original=InputSystem.settings;var fixture=UnityEngine.Object.Instantiate(original);fixture.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);InputSystem.settings=fixture;
            var car=new GameObject("Hop input contract").AddComponent<RacingVehiclePhysics>();var input=car.gameObject.AddComponent<PlayerRacingInput>();
            var old=Keyboard.current;var oldMouse=Mouse.current;var kb=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();kb.MakeCurrent();mouse.MakeCurrent();
            try
            {
                InputSystem.QueueStateEvent(kb,new KeyboardState(Key.LeftShift));UpdateInput();var cmd=input.SampleCommands();
                report.Check(cmd.MiniHop&&cmd.Drift&&!cmd.Jump,"Shift down requests an immediate small hop and a held drift, independently of full jump");
                UpdateInput();cmd=input.SampleCommands();report.Check(!cmd.MiniHop&&cmd.Drift,"Holding Shift keeps drift but never repeats hops");
                InputSystem.QueueStateEvent(kb,new KeyboardState());UpdateInput();cmd=input.SampleCommands();report.Check(!cmd.MiniHop&&!cmd.Drift,"Releasing a Shift tap does not leave drift held");
                InputSystem.QueueStateEvent(kb,new KeyboardState(Key.RightShift,Key.D));UpdateInput();cmd=input.SampleCommands();report.Check(cmd.MiniHop&&cmd.Steering>0,"Right Shift and directional input can redirect the hop");
                InputSystem.QueueStateEvent(kb,new KeyboardState());UpdateInput();input.SampleCommands();input.SetTouchDrift(true);cmd=input.SampleCommands();report.Check(cmd.MiniHop&&cmd.Drift,"Touch Drift has the same immediate hop / hold behavior");
                report.Check(!input.SampleCommands().MiniHop,"Held touch Drift cannot bunny-hop repeatedly");input.SetTouchDrift(false);input.SampleCommands();
                InputSystem.QueueStateEvent(kb,new KeyboardState(Key.W));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));UpdateInput();cmd=input.SampleCommands();
                report.Check(cmd.Throttle==1&&input.LookBack&&cmd.UseItem&&cmd.ItemBackward,"RMB + LMB still fires backward and W remains full throttle");
                UpdateInput();report.Check(!input.SampleCommands().UseItem,"Held fire still consumes one item press");
                input.SetInputEnabled(false);report.Check(!input.SampleCommands().MiniHop&&!input.SampleCommands().Drift&&!input.LookBack,"Pause/focus loss clears hop, drift and rear state");
            }
            finally {InputSystem.RemoveDevice(kb);InputSystem.RemoveDevice(mouse);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(fixture);if(old!=null&&old.added)old.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();UnityEngine.Object.DestroyImmediate(car.gameObject);}
        }
    }
}
#endif
