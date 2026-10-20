#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    // Explicit QA-only fixture. Real Rigidbody simulation on an isolated flat
    // platform, with screenshots and export outside any FPS measurement.
    public sealed class RacingLancerProbe : MonoBehaviour
    {
        [Serializable] sealed class Report {
            public string classification = "NATIVE_LANCER_PHYSICS_AND_PRESENTATION_FIXTURE_NOT_A_RACE_FPS_OR_ELITE_BENCHMARK";
            public int passed, failed, errors, samples;
            public float targetKmh, durationSeconds, humanDuration, botDuration, humanCruiseMin, humanCruiseMax, botCruiseMin, botCruiseMax;
            public float liveAiCruiseMin, liveAiCruiseMax, liveAiTargetKmh, aiTargetBeforeActivation, aiTargetAfterExpiry;
            public float ordinaryTopSpeedKmh;
            public float liveAiCornerTargetKmh, liveAiCornerBrake, liveAiCornerSpeedKmh;
            public string trackId;
            public int circuitSamples, roadContactSamples, circuitRecoveries, circuitStartPosition;
            public float circuitDuration, circuitPeakKmh, circuitMaxLateral, circuitMaxHeightFromRoad;
            public float shortcutHumanKmh,shortcutBotKmh,afterShortcutHumanKmh,afterShortcutBotKmh;
            public float ordinaryHumanKmh,ordinaryBotKmh,ordinaryShortcutHumanKmh,ordinaryShortcutBotKmh;
            public int directionalLaunchChecks;
            public int wallSafetyContacts,racerSafetyContacts;
            public float wallApproachKmh,wallMaximumCenterZ,collisionApproachKmh;
            public int goldenLaunches,goldenHits,goldenShieldBlocks;
            public float goldenTargetDistance;
            public long activationBytes, warmedTransformBytes;
            public List<string> successes = new List<string>(), failures = new List<string>();
        }
        struct Sample { public float elapsed, humanKmh, botKmh;public bool humanActive, botActive; }
        struct CircuitSample
        {
            public float elapsed, speedKmh, lateral, height, worldY, targetSpeedKmh, steering, throttle, brake, slip;
            public int node, wallContacts, racerContacts;public bool road, active, spunOut;
        }
        Report report = new Report();readonly List<Sample> samples = new List<Sample>(400);
        readonly List<CircuitSample> circuitSamples = new List<CircuitSample>(400);
        readonly RaycastHit[] roadHits = new RaycastHit[24];
        string output;float humanEnded, botEnded;bool circuit;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args = Environment.GetCommandLineArgs();bool circuit = Array.IndexOf(args, "--lancer-circuit") >= 0;
            if (!circuit && Array.IndexOf(args, "--lancer-test") < 0) return;
            if (Application.companyName != "HillAdventurer.BlacklineQA") { Debug.LogError("Lancer fixture requires the isolated QA company.");Application.Quit(1);return; }
            int at = Array.IndexOf(args, "--closure-output");if (at < 0 || at + 1 >= args.Length) { Application.Quit(1);return; }
            var probe = new GameObject("Explicit Lancer physics fixture").AddComponent<RacingLancerProbe>();
            DontDestroyOnLoad(probe.gameObject);probe.output = args[at + 1];probe.circuit = circuit;Directory.CreateDirectory(probe.output);
            if(circuit)probe.report.classification="NATIVE_LANCER_ON_UNMODIFIED_REAL_CIRCUIT_WITH_12_RACERS_SCRIPTED_QA_NOT_AN_FPS_OR_FULL_RACE_BENCHMARK";
            Application.logMessageReceived += probe.Logged;probe.StartCoroutine(probe.Guarded());
        }
        void Logged(string text, string stack, LogType kind) { if (kind == LogType.Error || kind == LogType.Exception) report.errors++; }
        void Check(bool value, string label) {
            if (value) { report.passed++;report.successes.Add(label); }
            else { report.failed++;report.failures.Add(label);Debug.LogError("LANCER_FAILED " + label); }
        }
        void Capture(string name) { RacingEvidenceCapture.Capture(Path.Combine(output, name + ".png"));Check(File.Exists(Path.Combine(output, name + ".png")), "Rendered snapshot: " + name); }
        IEnumerator Guarded()
        {
            var work = circuit ? RunCircuit() : Run();
            while (true) {
                bool more = false;Exception error = null;try { more = work.MoveNext(); } catch (Exception e) { error = e; }
                if (error != null) { Check(false, error.ToString());break; }if (!more) break;yield return work.Current;
            }
            Time.timeScale = 1f;
            var csv = new StringBuilder("elapsed,human_kmh,bot_kmh,human_active,bot_active\n");
            foreach (var s in samples) csv.AppendFormat(CultureInfo.InvariantCulture, "{0:F4},{1:F4},{2:F4},{3},{4}\n", s.elapsed, s.humanKmh, s.botKmh, s.humanActive ? 1 : 0, s.botActive ? 1 : 0);
            File.WriteAllText(Path.Combine(output, "lancer-speed.csv"), csv.ToString());
            if(circuit) {
                var roadCsv = new StringBuilder("elapsed,speed_kmh,lateral,height_from_real_road,world_y,real_road_under,active,node,target_speed_kmh,steering,throttle,brake,slip,wall_contacts,racer_contacts,spun_out\n");
                foreach(var s in circuitSamples)roadCsv.AppendFormat(CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4},{3:F4},{4:F4},{5},{6},{7},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4},{13},{14},{15}\n",s.elapsed,s.speedKmh,s.lateral,s.height,s.worldY,s.road?1:0,s.active?1:0,s.node,s.targetSpeedKmh,s.steering,s.throttle,s.brake,s.slip,s.wallContacts,s.racerContacts,s.spunOut?1:0);
                File.WriteAllText(Path.Combine(output,"lancer-real-circuit.csv"),roadCsv.ToString());
            }
            File.WriteAllText(Path.Combine(output, "lancer-native.json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= Logged;Application.Quit(report.failed == 0 && report.errors == 0 ? 0 : 1);
        }
        IEnumerator RunCircuit()
        {
            report.targetKmh = RaceItemUtility.LancerSpeedKmh;report.durationSeconds = RaceItemUtility.LancerDurationSeconds;
            RacingTestSession.Set(10000,4207);SceneManager.LoadScene("Racing3D");
            RaceManager manager=null;float deadline=Time.realtimeSinceStartup+40;
            while(Time.realtimeSinceStartup<deadline) {
                manager=FindFirstObjectByType<RaceManager>();
                if(manager!=null&&manager.SpawnedVehicles.Count==12&&manager.PlayerVehicle!=null)break;yield return null;
            }
            Check(manager!=null&&manager.SpawnedVehicles.Count==12,"Real Racing3D scene starts twelve ordinary racers");
            if(manager==null||manager.SpawnedVehicles.Count!=12)yield break;
            // Leave the race manager, real road, checkpoints, collisions,
            // recoveries and all eleven other drivers running normally.
            var metrics=FindFirstObjectByType<RacingRaceMetrics>();if(metrics!=null){metrics.StopMeasurement();metrics.enabled=false;}
            foreach(var car in manager.SpawnedVehicles) {
                var recorder=car.GetComponent<ML.RacingTelemetryRecorder>();if(recorder!=null){recorder.MarkScripted();recorder.enabled=false;}
                var ml=car.GetComponent<ML.RacingMlInferenceAdapter>();if(ml!=null)ml.executionMode=ML.MlExecutionMode.Off;
            }
            var player=manager.PlayerVehicle;var graph=manager.ProgressTracker.TrackGraph;
            report.trackId=FindFirstObjectByType<RacingTrackInfo>()?.TrackId;
            string roadBefore=JsonUtility.ToJson(graph),statsBefore=JsonUtility.ToJson(player.Stats);
            var ai=player.gameObject.AddComponent<AiVehicleController>();ai.Initialize(AiSkillModel.CreateProfile(10000,4207),graph,manager.ProgressTracker,"LANCER REAL CIRCUIT / SCRIPTED QA PLAYER");
            var inventory=player.GetComponent<ItemInventory>();var visuals=player.GetComponent<RacingLancerVisuals>();
            var recovery=player.GetComponent<VehicleRecovery>();int recoveriesBefore=recovery.RecoveryCount;
            float started=-1,ended=-1;
            player.OnLancerChanged+=active=>{if(active&&started<0)started=Time.fixedTime;else if(!active&&started>=0)ended=Time.fixedTime;};
            // Explicitly assign this one test charge; position-biased random
            // distribution is verified separately and is never faked here.
            inventory.AcquireItem(RaceItemType.Lancer,false);
            while(manager.State!=RaceState.Racing&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(manager.State==RaceState.Racing,"Normal lobby, introduction and countdown enter the real race");
            report.circuitStartPosition=manager.ProgressTracker.FindRacer(player).RacePosition;
            Capture("01-lancer-real-circuit-ready");
            deadline=Time.realtimeSinceStartup+40;
            while(started<0&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(started>=0,"The actual AI item agent activates Lancer on the real circuit");if(started<0)yield break;
            bool captured=false;deadline=Time.realtimeSinceStartup+15;
            while(player.IsLancerActive&&Time.realtimeSinceStartup<deadline) {
                yield return new WaitForFixedUpdate();
                graph.ProjectPosition(player.transform.position,out _,out float along,out float lateral);
                var road=graph.SampleDistance(along);float height=player.transform.position.y-road.Position.y;
                bool onRoad=RealRoadUnder(player);
                circuitSamples.Add(new CircuitSample{elapsed=Time.fixedTime-started,speedKmh=player.SpeedKmh,lateral=lateral,height=height,worldY=player.transform.position.y,road=onRoad,active=player.IsLancerActive,
                    node=ai.CurrentNodeIndex,targetSpeedKmh=ai.TargetSpeedKmh,steering=player.LastCommands.Steering,throttle=player.LastCommands.Throttle,brake=player.LastCommands.Brake,
                    slip=player.SlipAngle,wallContacts=player.WallCollisions,racerContacts=player.RacerCollisions,spunOut=player.IsSpunOut});
                if(onRoad)report.roadContactSamples++;
                report.circuitPeakKmh=Mathf.Max(report.circuitPeakKmh,player.SpeedKmh);
                report.circuitMaxLateral=Mathf.Max(report.circuitMaxLateral,Mathf.Abs(lateral));
                report.circuitMaxHeightFromRoad=Mathf.Max(report.circuitMaxHeightFromRoad,Mathf.Abs(height));
                if(!captured&&Time.fixedTime-started>=1f){captured=true;Capture("02-lancer-real-circuit-active");}
            }
            report.circuitSamples=circuitSamples.Count;report.circuitDuration=ended-started;
            report.circuitRecoveries=recovery.RecoveryCount-recoveriesBefore;
            Check(report.circuitSamples>=250&&Mathf.Abs(report.circuitDuration-6f)<.045f,"Lancer lasts six seconds in the live real-track race");
            Check(report.circuitRecoveries==0&&report.circuitMaxLateral<12f&&report.circuitMaxHeightFromRoad<4f&&report.roadContactSamples>=report.circuitSamples*.85f,
                "Lancer stays over the real road with no recovery or detached sky trajectory");
            Check(graph==manager.ProgressTracker.TrackGraph&&JsonUtility.ToJson(graph)==roadBefore&&FindObjectsByType<TrackSplineGraph>(FindObjectsSortMode.None).Length==1&&
                GameObject.Find("QA straight / ordinary collision surface")==null,"Real-circuit demonstration creates no test platform or alternate navigation track");
            Check(!player.IsLancerActive&&!visuals.IsVisible&&JsonUtility.ToJson(player.Stats)==statsBefore,"The real-race car restores its original appearance and base stats");
            yield return new WaitForSeconds(.3f);Capture("03-lancer-real-circuit-restored");
        }
        bool RealRoadUnder(RacingVehiclePhysics car)
        {
            int count=Physics.RaycastNonAlloc(car.transform.position+Vector3.up*.7f,Vector3.down,roadHits,3f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++) {
                var hit=roadHits[i];if(hit.collider.GetComponentInParent<RacingVehiclePhysics>()!=null)continue;
                var surface=hit.collider.GetComponentInParent<RacingSurface>();
                if(surface!=null&&surface.Kind==RacingSurfaceKind.Road)return true;
            }
            return false;
        }
        IEnumerator Run()
        {
            report.targetKmh = RaceItemUtility.LancerSpeedKmh;report.durationSeconds = RaceItemUtility.LancerDurationSeconds;
            RacingTestSession.Set(10000, 4207);SceneManager.LoadScene("Racing3D");
            RaceManager manager = null;float deadline = Time.realtimeSinceStartup + 40;
            while (Time.realtimeSinceStartup < deadline) {
                manager = FindFirstObjectByType<RaceManager>();
                if (manager != null && manager.SpawnedVehicles.Count == 12 && manager.PlayerVehicle != null) break;yield return null;
            }
            Check(manager != null && manager.SpawnedVehicles.Count == 12, "Native scene creates twelve real vehicles");
            if (manager == null || manager.SpawnedVehicles.Count != 12) yield break;
            manager.StopAllCoroutines();manager.enabled = false;manager.ProgressTracker.enabled = false;
            var metrics = FindFirstObjectByType<RacingRaceMetrics>();if (metrics != null) { metrics.StopMeasurement();metrics.enabled = false; }
            foreach (var car in manager.SpawnedVehicles) {
                car.SetRaceEnabled(false);car.enabled = false;
                var recovery = car.GetComponent<VehicleRecovery>();if (recovery != null) recovery.enabled = false;
                var ai = car.GetComponent<AiVehicleController>();if (ai != null) ai.enabled = false;
                var telemetry = car.GetComponent<ML.RacingTelemetryRecorder>();if (telemetry != null) telemetry.enabled = false;
            }
            typeof(RaceManager).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, new object[] { RaceState.Racing });
            // This platform belongs only to this explicit fixture, not the track.
            var platform = RacingArt.Box(transform, "QA straight / ordinary collision surface", new Vector3(0, 249.5f, 0), new Vector3(24, 1, 1200), RacingArt.Material(new Color(.07f, .10f, .15f)));
            platform.AddComponent<BoxCollider>().size = new Vector3(24, 1, 1200);
            for (int side = -1; side <= 1; side += 2) RacingArt.Box(transform, "QA straight edge", new Vector3(side * 10f, 250.01f, 0), new Vector3(.15f, .02f, 1200), RacingArt.Material(RacingDesign.Cyan, .3f, true));
            var player = manager.PlayerVehicle;var bot = manager.SpawnedVehicles[1];
            var input = player.GetComponent<PlayerRacingInput>();input.SetInputEnabled(false);
            player.ResetVelocities();bot.ResetVelocities();
            player.Body.position = new Vector3(-3, 250.65f, -350);player.Body.rotation = Quaternion.identity;
            bot.Body.position = new Vector3(3, 250.65f, -350);bot.Body.rotation = Quaternion.identity;
            player.SetRaceEnabled(true);bot.SetRaceEnabled(true);player.enabled = bot.enabled = true;
            Physics.SyncTransforms();
            var inventory = player.GetComponent<ItemInventory>();var botInventory = bot.GetComponent<ItemInventory>();
            var visuals = player.GetComponent<RacingLancerVisuals>();var botVisuals = bot.GetComponent<RacingLancerVisuals>();
            string playerStats = JsonUtility.ToJson(player.Stats), botStats = JsonUtility.ToJson(bot.Stats);
            float playerMass = player.Body.mass, botMass = bot.Body.mass;
            var original = player.GetComponent<RacingVehicleVisuals>().BodyVisual.parent.GetComponentsInChildren<Renderer>(true);
            var enabledBefore = Array.ConvertAll(original, r => r.enabled);
            var colliders = player.GetComponents<Collider>();var collisionBefore = Array.ConvertAll(colliders, c => c.enabled);
            inventory.AcquireItem(RaceItemType.Lancer, false);botInventory.AcquireItem(RaceItemType.Lancer, false);
            yield return new WaitForSeconds(3.2f);
            Check(player.IsGrounded && bot.IsGrounded, "Both ordinary Rigidbody cars settle on the shared flat surface");
            Capture("01-lancer-held");
            var agent = new AiItemAgent(AiSkillModel.CreateProfile(10000, 42), botInventory, bot, manager.ProgressTracker.TrackGraph, null);
            var safe = new AiItemContext { straightDistance = 60, turnDegrees = 5, position = 12 };
            Check(agent.ShouldUseItem(safe), "AI can release Lancer on a controlled straight");
            safe.turnDegrees = 50;Check(!agent.ShouldUseItem(safe), "AI holds Lancer at a sharp corner");
            safe.turnDegrees = 5;safe.reserveBoost = true;Check(!agent.ShouldUseItem(safe), "AI does not launch Lancer into a reserved shortcut entry");
            player.OnLancerChanged += active => { if (!active) humanEnded = Time.fixedTime; };
            bot.OnLancerChanged += active => { if (!active) botEnded = Time.fixedTime; };
            float start = Time.fixedTime;
            long allocated = GC.GetAllocatedBytesForCurrentThread();bool used = inventory.UseItem();report.activationBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            bool botUsed = botInventory.UseItem();
            Check(used && botUsed && player.UsesHumanInput && !bot.UsesHumanInput && player.IsLancerActive && bot.IsLancerActive,
                "Human and bot consume one Lancer using the same temporary motor");
            Check(!inventory.HasItem && inventory.ItemsUsed == 1 && !inventory.UseItem(), "One charge cannot be activated or counted twice");
            Check(visuals.IsVisible && botVisuals.IsVisible && Array.TrueForAll(original, r => !r.enabled), "Rocket forms replace the complete car, wheels and pilot renderers");
            Check(SameColliders(colliders, collisionBefore), "Transformation leaves every original collider enabled exactly as before");
            player.ApplySpeedBoost(1.5f, 30f);
            Check(!player.IsBoosting && player.LancerRemainingSeconds <= 6f, "Ordinary boosts cannot multiply 300 km/h or extend the Lancer timer");
            report.humanCruiseMin = report.botCruiseMin = float.MaxValue;bool captured = false;
            deadline = Time.realtimeSinceStartup + 25;
            while ((player.IsLancerActive || bot.IsLancerActive) && Time.realtimeSinceStartup < deadline) {
                yield return new WaitForFixedUpdate();float elapsed = Time.fixedTime - start;
                samples.Add(new Sample { elapsed = elapsed, humanKmh = player.SpeedKmh, botKmh = bot.SpeedKmh, humanActive = player.IsLancerActive, botActive = bot.IsLancerActive });
                if (elapsed >= 1.5f && player.IsLancerActive && bot.IsLancerActive) {
                    report.humanCruiseMin = Mathf.Min(report.humanCruiseMin, player.SpeedKmh);report.humanCruiseMax = Mathf.Max(report.humanCruiseMax, player.SpeedKmh);
                    report.botCruiseMin = Mathf.Min(report.botCruiseMin, bot.SpeedKmh);report.botCruiseMax = Mathf.Max(report.botCruiseMax, bot.SpeedKmh);
                    if (!captured) { captured = true;Capture("02-lancer-300kmh-active"); }
                }
            }
            report.samples = samples.Count;report.humanDuration = humanEnded - start;report.botDuration = botEnded - start;
            Check(report.samples >= 250 && Mathf.Abs(report.humanDuration - 6f) < .045f && Mathf.Abs(report.botDuration - 6f) < .045f,
                "Both transformations expire after six simulated seconds within one physics tick");
            Check(report.humanCruiseMin >= 298.5f && report.humanCruiseMax <= 301.5f && report.botCruiseMin >= 298.5f && report.botCruiseMax <= 301.5f,
                "Both real Rigidbody cars cruise at 300 km/h within 1.5 km/h on the level straight");
            Check(!player.IsLancerActive && !visuals.IsVisible && !botVisuals.IsVisible && SameRenderers(original, enabledBefore), "Expiry restores exactly the original visible and batched-disabled renderer states");
            Check(JsonUtility.ToJson(player.Stats) == playerStats && JsonUtility.ToJson(bot.Stats) == botStats && player.Body.mass == playerMass && bot.Body.mass == botMass,
                "Neither human nor bot base stats, engine, grip or mass are rewritten");
            player.ResetVelocities();player.SetRaceEnabled(false);bot.ResetVelocities();bot.SetRaceEnabled(false);
            // A second pass keeps the real AI controller authoritative over its
            // steering/braking commands. The first pass isolated shared physics.
            var aiRoad = new GameObject("QA straight AI graph").AddComponent<TrackSplineGraph>();
            var nodes = new TrackNode[14];
            for (int i = 0; i < 12; i++) nodes[i] = new TrackNode {
                Position = new Vector3(3, 250, -600 + i * 200), Forward = Vector3.forward, Right = Vector3.right,
                Up = Vector3.up, TrackWidth = 20, TargetSpeedKmh = 140
            };
            nodes[12] = new TrackNode { Position = new Vector3(1000, 250, 1600), TrackWidth = 20, TargetSpeedKmh = 70 };
            nodes[13] = new TrackNode { Position = new Vector3(1000, 250, -600), TrackWidth = 20, TargetSpeedKmh = 70 };
            aiRoad.InitializeFromNodes(nodes);
            var aiProgress = aiRoad.gameObject.AddComponent<TrackProgressTracker>();aiProgress.Initialize(aiRoad, 3);aiProgress.enabled = false;
            var aiController = bot.GetComponent<AiVehicleController>();aiController.Initialize(AiSkillModel.CreateProfile(10000, 42), aiRoad, aiProgress, "QA Lancer live AI");
            aiController.LaunchImmediate();bot.Body.position = new Vector3(3, 250.65f, -350);bot.Body.rotation = Quaternion.identity;
            bot.SetRaceEnabled(true);aiController.enabled = true;Physics.SyncTransforms();
            yield return new WaitForSeconds(.9f);report.aiTargetBeforeActivation = aiController.TargetSpeedKmh;
            botInventory.AcquireItem(RaceItemType.Lancer, false);Check(botInventory.UseItem(), "Live AI straight pass consumes the same ordinary Lancer charge");
            start = Time.fixedTime;report.liveAiCruiseMin = float.MaxValue;deadline = Time.realtimeSinceStartup + 25;
            while (bot.IsLancerActive && Time.realtimeSinceStartup < deadline) {
                yield return new WaitForFixedUpdate();
                if (Time.fixedTime - start >= 2f && bot.IsLancerActive) {
                    report.liveAiCruiseMin = Mathf.Min(report.liveAiCruiseMin, bot.SpeedKmh);report.liveAiCruiseMax = Mathf.Max(report.liveAiCruiseMax, bot.SpeedKmh);
                    report.liveAiTargetKmh = aiController.TargetSpeedKmh;
                }
            }
            Check(report.liveAiCruiseMin >= 298.5f && report.liveAiCruiseMax <= 301.5f,
                "Real AI steering/braking recognizes Lancer and maintains 300 km/h on the safe straight");
            yield return new WaitForSeconds(.9f);report.aiTargetAfterExpiry = aiController.TargetSpeedKmh;
            // The normal envelope varies with current speed/braking horizon,
            // so a low-speed pre-launch target need not equal the exit target.
            report.ordinaryTopSpeedKmh = bot.Stats.SpeedKmh;
            Check(!bot.IsLancerActive && report.aiTargetAfterExpiry > 0 && report.aiTargetAfterExpiry <= report.ordinaryTopSpeedKmh + .01f &&
                report.aiTargetAfterExpiry < 199f && JsonUtility.ToJson(bot.Stats) == botStats,
                "Live AI returns to the ordinary straight speed envelope after Lancer expires");
            // A tight curve must still produce a real brake command while the
            // item is active. Its temporary speed target cannot bypass handling.
            aiController.enabled = false;bot.ResetVelocities();
            var curve = new TrackNode[36];
            for (int i = 0; i < curve.Length; i++) {
                float a = i * Mathf.PI * 2 / curve.Length;Vector3 heading = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                curve[i] = new TrackNode { Position = new Vector3(3 + 35 * (1 - Mathf.Cos(a)), 250, -350 + 35 * Mathf.Sin(a)),
                    Forward = heading, Right = Vector3.Cross(Vector3.up, heading), Up = Vector3.up, TrackWidth = 20,
                    TargetSpeedKmh = 80, IsDriftZone = true };
            }
            aiRoad.InitializeFromNodes(curve);aiController.Initialize(AiSkillModel.CreateProfile(10000, 42), aiRoad, aiProgress, "QA Lancer corner control");
            aiController.LaunchImmediate();bot.Body.position = new Vector3(3, 250.65f, -350);bot.Body.rotation = Quaternion.identity;
            bot.Body.linearVelocity = Vector3.forward * (300f / 3.6f);Physics.SyncTransforms();
            botInventory.AcquireItem(RaceItemType.Lancer, false);Check(botInventory.UseItem(), "Corner-control pass activates the same Lancer item");aiController.enabled = true;
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            report.liveAiCornerTargetKmh = aiController.TargetSpeedKmh;report.liveAiCornerBrake = bot.LastCommands.Brake;report.liveAiCornerSpeedKmh = bot.SpeedKmh;
            Check(bot.IsLancerActive && report.liveAiCornerTargetKmh < 180 && report.liveAiCornerBrake > .2f && report.liveAiCornerSpeedKmh < 295,
                "Active Lancer retains live AI curve braking and physically slows for a tight bend");
            bot.ResetVelocities();
            aiController.enabled = false;bot.SetRaceEnabled(false);player.SetRaceEnabled(true);
            yield return ShortcutPass(player,bot);
            yield return ItemDirectionPass(player,inventory);
            yield return GoldenRockPass(manager,player,bot);
            yield return CollisionSafetyPass(player,bot);
            yield return LancerBrakePass(player);
            yield return new WaitForSeconds(.2f);inventory.AcquireItem(RaceItemType.Lancer, false);
            Check(inventory.UseItem(), "A later independent Lancer can activate normally");
            float remaining = player.LancerRemainingSeconds;Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(.25f);
            Check(player.LancerRemainingSeconds == remaining && visuals.IsVisible, "Pause freezes the six-second timer without losing the transformation");Time.timeScale = 1f;
            inventory.AcquireItem(RaceItemType.Lancer, false);
            Check(!inventory.UseItem() && inventory.HeldItem == RaceItemType.Lancer && inventory.ChargesRemaining == 1, "An active Lancer cannot consume a second held Lancer");
            player.ResetVelocities();Check(!player.IsLancerActive && SameRenderers(original, enabledBefore), "Recovery/reset cancels the item and restores the original car");
            // Warmed activation/restoration is measured separately from the HUD,
            // one-shot audio feedback and screenshot/export costs.
            player.TryActivateLancer();player.CancelLancer();
            allocated = GC.GetAllocatedBytesForCurrentThread();bool valid = true;
            for (int i = 0; i < 100; i++) { valid &= player.TryActivateLancer();player.CancelLancer(); }
            report.warmedTransformBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(valid && report.warmedTransformBytes == 0, "100 warmed transformations/restorations allocate zero managed bytes");
            player.TryActivateLancer();player.TriggerSpinout();
            Check(!player.IsLancerActive && !visuals.IsVisible && SameRenderers(original, enabledBefore), "An item stun cancels rocket thrust and restores the car before the shared spinout");
            player.ResetVelocities();player.TryActivateLancer();player.SetRaceEnabled(false);
            Check(!player.IsLancerActive && SameRenderers(original, enabledBefore) && !inventory.UseItem() && inventory.HasItem,
                "Disabling the race cancels Lancer without consuming held inventory");
            player.SetRaceEnabled(true);player.TryActivateLancer();player.enabled = false;
            Check(!player.IsLancerActive && SameRenderers(original, enabledBefore), "Disabling the physics component cannot leave a hidden car or active motor");
            player.enabled = true;player.TryActivateLancer();visuals.enabled = false;
            Check(!player.IsLancerActive && SameRenderers(original, enabledBefore), "Disabling transformation visuals safely cancels the motor");
            visuals.enabled = true;player.TryActivateLancer();player.SetFinished();
            Check(!player.IsLancerActive && !visuals.IsVisible && SameRenderers(original, enabledBefore), "Finishing cancels Lancer and restores the vehicle before the finished ghost state");
            yield return new WaitForSeconds(.2f);Capture("03-lancer-restored");
        }
        static bool SameRenderers(Renderer[] values, bool[] expected) { for (int i = 0; i < values.Length; i++) if (values[i] == null || values[i].enabled != expected[i]) return false;return true; }
        sealed class ProbeCommands : IVehicleCommandProvider
        {
            public bool fire,backward;
            public float throttle,brake;
            public VehicleCommands GetCommands(){var c=new VehicleCommands{UseItem=fire,ItemBackward=backward,Throttle=throttle,Brake=brake};fire=false;return c;}
        }
        IEnumerator LancerBrakePass(RacingVehiclePhysics player)
        {
            player.ResetVelocities();player.SetRaceEnabled(true);
            player.Body.position = new Vector3(-3,250.65f,-350);player.Body.rotation = Quaternion.identity;
            var command = new ProbeCommands();player.SetCommandProvider(command);Physics.SyncTransforms();
            yield return new WaitForSeconds(.3f);
            player.Body.linearVelocity = Vector3.forward * 20f;
            Check(player.TryActivateLancer(), "Brake fixture activates the ordinary shared Lancer motor");
            yield return new WaitForSeconds(.25f);
            Check(player.SpeedMps > 25f, "Lancer preserves its automatic propulsion when throttle is released");
            float before = player.SpeedMps;command.brake = .8f;
            yield return new WaitForSeconds(.2f);
            Check(player.SpeedMps < before - 2f, "Explicit driver braking overrides Lancer propulsion using ordinary shared brakes");
            player.ResetVelocities();command.brake = 0;
        }
        IEnumerator CollisionSafetyPass(RacingVehiclePhysics player,RacingVehiclePhysics bot)
        {
            // Explicit fixture at 300 km/h, with the same ordinary colliders,
            // CCD, masses, motor and collision response as gameplay.
            var command=new ProbeCommands();player.SetCommandProvider(command);
            var botCommand=new ProbeCommands();bot.SetCommandProvider(botCommand);
            var inventory=player.GetComponent<ItemInventory>();
            string stats=JsonUtility.ToJson(player.Stats),botStats=JsonUtility.ToJson(bot.Stats);
            float mass=player.Body.mass,botMass=bot.Body.mass;
            player.ResetVelocities();bot.ResetVelocities();bot.SetRaceEnabled(false);
            player.Body.position=new Vector3(-3,250.65f,-120);player.Body.rotation=Quaternion.identity;
            player.SetRaceEnabled(true);player.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSeconds(.5f);
            var wall=RacingArt.Box(transform,"QA thin wall / standard collision",new Vector3(-3,251.5f,-80),new Vector3(6,3,.35f),RacingArt.Material(Color.red));
            wall.AddComponent<BoxCollider>().size=new Vector3(6,3,.35f);Physics.SyncTransforms();
            inventory.AcquireItem(RaceItemType.Lancer,false);
            Check(inventory.UseItem(),"Wall safety fixture activates the ordinary Lancer charge");
            int wallBefore=player.WallCollisions;
            command.throttle=1;player.Body.linearVelocity=Vector3.forward*(300f/3.6f);
            report.wallApproachKmh=player.SpeedKmh;report.wallMaximumCenterZ=player.Body.position.z;
            for(int i=0;i<50;i++) {yield return new WaitForFixedUpdate();report.wallMaximumCenterZ=Mathf.Max(report.wallMaximumCenterZ,player.Body.position.z);}
            report.wallSafetyContacts=player.WallCollisions-wallBefore;
            Check(report.wallApproachKmh>=298.5f&&report.wallSafetyContacts>0&&report.wallMaximumCenterZ< -79.5f,
                "300 km/h Lancer contacts a 0.35 m static wall without tunneling through its plane");
            Check(Finite(player.Body.position)&&Finite(player.Body.linearVelocity),"Wall impact leaves finite position and velocity");
            player.ResetVelocities();command.throttle=0;
            Destroy(wall);yield return new WaitForFixedUpdate();
            player.Body.position=new Vector3(3,250.65f,-140);player.Body.rotation=Quaternion.identity;
            bot.Body.position=new Vector3(3,250.65f,-100);bot.Body.rotation=Quaternion.identity;
            bot.enabled=true;bot.SetRaceEnabled(true);Physics.SyncTransforms();
            yield return new WaitForSeconds(.5f);
            inventory.AcquireItem(RaceItemType.Lancer,false);
            Check(inventory.UseItem(),"Racer collision fixture activates the same Lancer charge");
            int racerBefore=player.RacerCollisions;
            command.throttle=1;player.Body.linearVelocity=Vector3.forward*(300f/3.6f);
            report.collisionApproachKmh=player.SpeedKmh;
            for(int i=0;i<50;i++)yield return new WaitForFixedUpdate();
            report.racerSafetyContacts=player.RacerCollisions-racerBefore;
            Check(report.collisionApproachKmh>=298.5f&&report.racerSafetyContacts>0&&Finite(player.Body.position)&&Finite(bot.Body.position)&&Finite(player.Body.linearVelocity)&&Finite(bot.Body.linearVelocity),
                "300 km/h Lancer collides with an ordinary dynamic racer using finite shared Rigidbody response");
            player.ResetVelocities();bot.ResetVelocities();command.throttle=0;bot.SetRaceEnabled(false);
            Check(!player.IsLancerActive&&JsonUtility.ToJson(player.Stats)==stats&&JsonUtility.ToJson(bot.Stats)==botStats&&player.Body.mass==mass&&bot.Body.mass==botMass,
                "Both collision fixtures restore Lancer state without mutating either car's permanent stats or mass");
        }
        IEnumerator GoldenRockPass(RaceManager manager,RacingVehiclePhysics player,RacingVehiclePhysics bot)
        {
            var pool=RacingItemPool.ForScene();ClearFixtureProjectiles(pool);
            var ordered=manager.ProgressTracker.Leaderboard;var ranks=new int[ordered.Count];
            for(int i=0;i<ordered.Count;i++){ranks[i]=ordered[i].RacePosition;ordered[i].RacePosition=20+i;}
            var self=manager.ProgressTracker.FindRacer(player);var ahead=manager.ProgressTracker.FindRacer(bot);
            RacingVehiclePhysics decoy=null;foreach(var car in manager.SpawnedVehicles)if(car!=player&&car!=bot){decoy=car;break;}
            Check(decoy!=null&&player!=bot,"Golden fixture uses three distinct racers");
            var lead=manager.ProgressTracker.FindRacer(decoy);
            var inventory=player.GetComponent<ItemInventory>();var shield=bot.GetComponent<ItemInventory>();
            inventory.BindRaceProgress(manager.ProgressTracker);shield.BindRaceProgress(manager.ProgressTracker);
            var command=new ProbeCommands();player.SetCommandProvider(command);
            string stats=JsonUtility.ToJson(player.Stats),botStats=JsonUtility.ToJson(bot.Stats);
            var botConstraints=bot.Body.constraints;
            var decoyConstraints=decoy.Body.constraints;
            var playerConstraints=player.Body.constraints;
            var playerInterpolation=player.Body.interpolation;var botInterpolation=bot.Body.interpolation;var decoyInterpolation=decoy.Body.interpolation;
            player.SetRaceEnabled(true);bot.SetRaceEnabled(true);decoy.SetRaceEnabled(true);player.enabled=true;
            player.ResetVelocities();bot.ResetVelocities();decoy.ResetVelocities();
            // This stationary QA fixture follows a moving/interpolated AI fixture.
            // Teleport both representations explicitly, without a stale interpolation pose.
            player.Body.interpolation=bot.Body.interpolation=decoy.Body.interpolation=RigidbodyInterpolation.None;
            player.Body.constraints=RigidbodyConstraints.FreezeAll;
            FixturePose(player,new Vector3(0,250.65f,-350));FixturePose(bot,new Vector3(0,250.65f,-300));
            FixturePose(decoy,new Vector3(6,250.65f,-330));
            bot.enabled=false;decoy.enabled=false;bot.Body.constraints=decoy.Body.constraints=RigidbodyConstraints.FreezeAll;
            Physics.SyncTransforms();yield return new WaitForSeconds(.25f);
            self.RacePosition=1;ahead.RacePosition=2;lead.RacePosition=3;
            inventory.AcquireItem(RaceItemType.GoldenRock,false);
            Check(!inventory.UseItem()&&inventory.HeldItem==RaceItemType.GoldenRock&&inventory.ChargesRemaining==1,
                "Golden Rock without an opponent ahead preserves its single charge");
            self.RacePosition=3;ahead.RacePosition=2;lead.RacePosition=1;
            yield return new WaitForSeconds(.16f);Capture("04-golden-rock-held");
            int hitsBefore=bot.ItemHits,ownerHits=player.ItemHits,decoyHits=decoy.ItemHits;
            int created=pool.CreatedCount;
            Check(inventory.UseItem()&&!inventory.UseItem(),"Golden Rock consumes one charge and cannot double-fire");
            var missile=ActiveGolden(pool,player);report.goldenLaunches++;
            Check(missile!=null&&missile.HomingTarget==bot.transform,"Golden Rock locks immediately preceding P2, not the physically closer P1 decoy");
            report.goldenTargetDistance=Vector3.Distance(player.transform.position,bot.transform.position);
            Check(Mathf.Abs(report.goldenTargetDistance-50)<.05f&&Vector3.Distance(player.transform.position,player.Body.position)<.01f&&Vector3.Distance(bot.transform.position,bot.Body.position)<.01f,
                "Golden stationary fixture has matching Transform/Rigidbody poses exactly fifty metres apart");
            for(int i=0;i<65;i++)yield return new WaitForFixedUpdate();
            report.goldenHits+=bot.ItemHits-hitsBefore;
            Check(bot.ItemHits==hitsBefore+1&&player.ItemHits==ownerHits&&decoy.ItemHits==decoyHits&&missile!=null&&!missile.gameObject.activeSelf,
                "Guided golden stone hits its opponent exactly once, ignores its owner and returns to the pool");
            bot.ResetVelocities();bot.Body.constraints=RigidbodyConstraints.FreezeAll;
            shield.AcquireItem(RaceItemType.MagneticShield,false);
            Check(shield.UseItem()&&shield.HasShield,"Target activates the existing real shield for the golden-stone fixture");
            hitsBefore=bot.ItemHits;inventory.AcquireItem(RaceItemType.GoldenRock,false);
            Check(inventory.UseItem(),"A second independent Golden Rock uses the warmed projectile pool");
            missile=ActiveGolden(pool,player);report.goldenLaunches++;
            for(int i=0;i<65;i++)yield return new WaitForFixedUpdate();
            bool absorbed=bot.ItemHits==hitsBefore&&!shield.HasShield&&!bot.IsSpunOut&&missile!=null&&!missile.gameObject.activeSelf;
            if(absorbed)report.goldenShieldBlocks++;
            Check(absorbed,"Enemy shield breaks Golden Rock, consumes one protection and prevents the spin/hit");
            // Independent hit case: retain the ordinary post-hit anti-stunlock immunity.
            yield return new WaitForSeconds(1.3f);
            bot.ResetVelocities();player.ResetVelocities();bot.Body.constraints=RigidbodyConstraints.FreezeAll;
            FixturePose(player,new Vector3(0,250.65f,-350));FixturePose(bot,new Vector3(0,250.65f,-380));
            self.RacePosition=1;ahead.RacePosition=2;lead.RacePosition=3;
            command.backward=true;Physics.SyncTransforms();yield return new WaitForFixedUpdate();
            hitsBefore=bot.ItemHits;inventory.AcquireItem(RaceItemType.GoldenRock,false);
            Check(inventory.UseItem(),"Rear-view Golden Rock can use an existing charge against the following rank");
            missile=ActiveGolden(pool,player);report.goldenLaunches++;
            Check(missile!=null&&missile.HomingTarget==bot.transform&&Vector3.Dot(missile.transform.position-player.transform.position,player.transform.forward)<0,
                "Backward golden launch starts behind the vehicle and locks P2 behind P1");
            for(int i=0;i<50;i++)yield return new WaitForFixedUpdate();
            report.goldenHits+=bot.ItemHits-hitsBefore;
            Check(bot.ItemHits==hitsBefore+1,"Backward Golden Rock retains guided impact behavior");
            bot.ResetVelocities();player.ResetVelocities();bot.Body.constraints=RigidbodyConstraints.FreezeAll;
            FixturePose(player,new Vector3(0,250.65f,-350));FixturePose(bot,new Vector3(0,250.65f,-300));
            self.RacePosition=3;ahead.RacePosition=2;lead.RacePosition=1;command.backward=false;
            Physics.SyncTransforms();yield return new WaitForFixedUpdate();
            inventory.AcquireItem(RaceItemType.GoldenRock,false);Check(inventory.UseItem(),"Golden target-loss fixture launches through ordinary inventory");
            missile=ActiveGolden(pool,player);report.goldenLaunches++;bot.SetRaceEnabled(false);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Check(missile!=null&&!missile.gameObject.activeSelf,"Disabling/finishing the locked target safely retires the Golden Rock");
            bot.SetRaceEnabled(true);yield return new WaitForSeconds(.25f);
            var wall=RacingArt.Box(transform,"QA Golden Rock collision wall",new Vector3(0,251.5f,-325),new Vector3(4,3,.35f),RacingArt.Material(Color.gray));
            wall.AddComponent<BoxCollider>().size=new Vector3(4,3,.35f);Physics.SyncTransforms();
            hitsBefore=bot.ItemHits;inventory.AcquireItem(RaceItemType.GoldenRock,false);Check(inventory.UseItem(),"Golden obstruction fixture launches through the warmed pool");
            missile=ActiveGolden(pool,player);report.goldenLaunches++;for(int i=0;i<50;i++)yield return new WaitForFixedUpdate();
            Check(bot.ItemHits==hitsBefore&&missile!=null&&!missile.gameObject.activeSelf,"Golden Rock respects swept world collision and cannot hit through a wall");
            Destroy(wall);ClearFixtureProjectiles(pool);
            Check(pool.CreatedCount==created&&JsonUtility.ToJson(player.Stats)==stats&&JsonUtility.ToJson(bot.Stats)==botStats,
                "Golden Rock allocates no extra projectile entities and never changes either vehicle's stats");
            player.Body.constraints=playerConstraints;bot.Body.constraints=botConstraints;decoy.Body.constraints=decoyConstraints;
            player.Body.interpolation=playerInterpolation;bot.Body.interpolation=botInterpolation;decoy.Body.interpolation=decoyInterpolation;
            bot.enabled=true;bot.SetRaceEnabled(false);decoy.SetRaceEnabled(false);
            player.ResetVelocities();bot.ResetVelocities();
            for(int i=0;i<ordered.Count;i++)ordered[i].RacePosition=ranks[i];
        }
        static void FixturePose(RacingVehiclePhysics car,Vector3 position)
        {
            car.transform.SetPositionAndRotation(position,Quaternion.identity);
            car.Body.position=position;car.Body.rotation=Quaternion.identity;
        }
        static ProjectileItemEntity ActiveGolden(RacingItemPool pool,RacingVehiclePhysics owner)
        {
            foreach(var missile in pool.GetComponentsInChildren<ProjectileItemEntity>(true))
                if(missile.gameObject.activeSelf&&missile.Owner==owner&&missile.ItemType==RaceItemType.GoldenRock)return missile;
            return null;
        }
        static void ClearFixtureProjectiles(RacingItemPool pool)
        {
            foreach(var entity in pool.GetComponentsInChildren<ItemEntityBase>(true))if(entity.gameObject.activeSelf)pool.Release(entity);
        }
        static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
        IEnumerator ShortcutPass(RacingVehiclePhysics player,RacingVehiclePhysics bot)
        {
            var graph=new GameObject("QA official shortcut corridor").AddComponent<TrackSplineGraph>();
            graph.InitializeFromNodes(new[]{new TrackNode{Position=new Vector3(80,250,-500)},new TrackNode{Position=new Vector3(80,250,500)},new TrackNode{Position=new Vector3(180,250,500)},new TrackNode{Position=new Vector3(180,250,-500)}},
                new[]{new TrackShortcut{entryNodeIndex=0,exitNodeIndex=1,width=8,requiresBoost=false,pathPoints=new[]{new Vector3(0,250,-400),new Vector3(0,250,-180)}}});
            player.ResetVelocities();bot.ResetVelocities();player.BindRaceTrack(graph);bot.BindRaceTrack(graph);
            player.SetCommandProvider(new ProbeCommands());bot.SetCommandProvider(new ProbeCommands());
            player.Body.position=new Vector3(-3,250.65f,-350);bot.Body.position=new Vector3(3,250.65f,-350);player.Body.rotation=bot.Body.rotation=Quaternion.identity;
            player.SetRaceEnabled(true);bot.SetRaceEnabled(true);Physics.SyncTransforms();
            player.BindRaceTrack(null);bot.BindRaceTrack(null);
            player.SetCommandProvider(new ProbeCommands{throttle=1});bot.SetCommandProvider(new ProbeCommands{throttle=1});
            player.Body.linearVelocity=Vector3.forward*(player.Stats.SpeedKmh/3.6f);bot.Body.linearVelocity=Vector3.forward*(bot.Stats.SpeedKmh/3.6f);
            yield return new WaitForSeconds(1f);report.ordinaryHumanKmh=player.SpeedKmh;report.ordinaryBotKmh=bot.SpeedKmh;
            player.BindRaceTrack(graph);bot.BindRaceTrack(graph);yield return new WaitForSeconds(1f);
            report.ordinaryShortcutHumanKmh=player.SpeedKmh;report.ordinaryShortcutBotKmh=bot.SpeedKmh;
            Check(Mathf.Abs(player.SpeedKmh/report.ordinaryHumanKmh-.5f)<.02f&&Mathf.Abs(bot.SpeedKmh/report.ordinaryBotKmh-.5f)<.02f,
                "Ordinary human/bot cars physically slow to half speed inside the official shortcut without Lancer");
            player.ResetVelocities();bot.ResetVelocities();player.Body.position=new Vector3(-3,250.65f,-350);bot.Body.position=new Vector3(3,250.65f,-350);Physics.SyncTransforms();
            player.SetCommandProvider(new ProbeCommands());bot.SetCommandProvider(new ProbeCommands());
            player.TryActivateLancer();bot.TryActivateLancer();yield return new WaitForSeconds(2.2f);
            report.shortcutHumanKmh=player.SpeedKmh;report.shortcutBotKmh=bot.SpeedKmh;
            Check(player.ShortcutSpeedMultiplier==.5f&&bot.ShortcutSpeedMultiplier==.5f&&player.EffectiveLancerTargetKmh==150&&bot.EffectiveLancerTargetKmh==150,
                "Real human/bot motors detect the same official corridor and target 150 km/h");
            Check(Mathf.Abs(player.SpeedKmh-150)<1.5f&&Mathf.Abs(bot.SpeedKmh-150)<1.5f,"Measured human and bot Lancer speeds settle at 150 km/h inside the shortcut");
            player.Body.position=new Vector3(-3,250.65f,0);bot.Body.position=new Vector3(3,250.65f,0);Physics.SyncTransforms();yield return new WaitForSeconds(1.2f);
            report.afterShortcutHumanKmh=player.SpeedKmh;report.afterShortcutBotKmh=bot.SpeedKmh;
            Check(player.IsLancerActive&&bot.IsLancerActive&&player.ShortcutSpeedMultiplier==1&&bot.ShortcutSpeedMultiplier==1&&Mathf.Abs(player.SpeedKmh-300)<1.5f&&Mathf.Abs(bot.SpeedKmh-300)<1.5f,
                "Leaving the corridor restores 300 km/h while the original six-second timer continues");
            player.ResetVelocities();bot.ResetVelocities();player.SetRaceEnabled(false);bot.SetRaceEnabled(false);
            Check(player.ShortcutSpeedMultiplier==1&&bot.ShortcutSpeedMultiplier==1,"Native reset/race stop clears shortcut state equally for player and bot");
            player.BindRaceTrack(null);bot.BindRaceTrack(null);player.SetRaceEnabled(true);
        }
        IEnumerator ItemDirectionPass(RacingVehiclePhysics player,ItemInventory inventory)
        {
            var commands=new ProbeCommands();player.SetCommandProvider(commands);
            player.Body.position=new Vector3(0,250.65f,-350);player.Body.rotation=Quaternion.identity;Physics.SyncTransforms();yield return new WaitForSeconds(.25f);
            foreach(bool backward in new[]{false,true})foreach(var type in new[]{RaceItemType.KineticMissile,RaceItemType.TrackingDart,RaceItemType.HomingRocket,RaceItemType.ElectroSpike,RaceItemType.TwinRock}) {
                inventory.AcquireItem(type,false);int before=inventory.ItemsUsed;commands.backward=backward;commands.fire=true;
                yield return new WaitForFixedUpdate();
                var entities=FindObjectsByType<ItemEntityBase>(FindObjectsSortMode.None);int found=0;bool direction=true;
                foreach(var entity in entities)if(entity.Owner==player&&entity.gameObject.activeSelf) {
                    found++;direction&=Vector3.Dot(entity.transform.forward,backward?-player.transform.forward:player.transform.forward)>.9f;
                    direction&=Vector3.Dot(entity.transform.position-player.transform.position,backward?-player.transform.forward:player.transform.forward)>0;
                    RacingItemPool.ForScene().Release(entity);
                }
                Check(inventory.ItemsUsed==before+1&&found==(type==RaceItemType.TwinRock?2:1)&&direction,
                    type+" native "+(backward?"rear":"forward")+" launch: one activation, correct origin/direction");report.directionalLaunchChecks++;
                yield return new WaitForSeconds(.25f);
                Check(inventory.ItemsUsed==before+1,"Consumed fixed-step command cannot repeat on subsequent render frames");
            }
            player.SetCommandProvider(null);
        }
        static bool SameColliders(Collider[] values, bool[] expected) { for (int i = 0; i < values.Length; i++) if (values[i] == null || values[i].enabled != expected[i]) return false;return true; }
    }
}
#endif
