#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Racing3D.ML;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingReleaseContracts
    {
        [Serializable] sealed class Case { public float[] features,outputs; }
        [Serializable] sealed class Fixture { public Case[] cases; public TelemetrySample sample; public int mmr; public float[] normalized; public string weights_sha256; }
        [Serializable] sealed class Parity { public int cases;public float maxOutputError,maxFeatureError;public string modelVersion,representationNote;public int[] representedMmr; }
        [Serializable] sealed class TrackManifest { public TrackNode[] nodes;public TrackShortcut[] shortcuts; }
        [Serializable] sealed class ExpectedTech { public string tech_id;public int flights,occurrences,successes; }
        [Serializable] sealed class TechFixture { public RaceTelemetrySession session;public ExpectedTech[] expected; }
        // Targeted continuation check; deliberately does not rerun the complete
        // product, league, Adventure or parity suite when only scheduling changes.
        public static void RunShadowScheduling()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--closure-output");
            if(index<0||index+1>=args.Length)throw new ArgumentException("Missing --closure-output");
            string output=args[index+1];Directory.CreateDirectory(output);
            var report=new ClosureContractReport();
            var schedules=new RacingInferenceSchedule[11];var counts=new int[11];
            int peak=0,total=0;
            for(int step=0;step<1000;step++)
            {
                int ticks=0;float now=100f+step*.02f;
                for(int car=0;car<11;car++)if(schedules[car].TryTick(now,.1f,car,out _,out _)){ticks++;counts[car]++;}
                peak=Math.Max(peak,ticks);total+=ticks;
            }
            report.Check(peak<=3,"After countdown, eleven cars are spread over five 50-Hz physics steps (peak="+peak+")");
            report.Check(Array.TrueForAll(counts,x=>x>=199&&x<=201),"Each car retains its intended 10-Hz inference cadence over twenty seconds (total="+total+")");
            var schedule=new RacingInferenceSchedule();
            schedule.TryTick(0,.1,0,out _,out _);
            report.Check(schedule.TryTick(.28,.1,0,out float horizon,out bool discontinuity)&&!discontinuity&&Math.Abs(horizon-.28)<.00001,
                "A late frame runs once and retains the actual prediction horizon");
            report.Check(!schedule.TryTick(.28,.1,0,out _,out _)&&schedule.TryTick(.3,.1,0,out _,out _),"Missed deadlines are skipped without a catch-up inference burst");
            report.Check(!schedule.TryTick(-1,.1,5,out _,out discontinuity)&&discontinuity,"Backward clock clears temporal state and restores the phase");
            report.Check(!schedule.TryTick(1,.1,5,out _,out discontinuity)&&discontinuity,"Pause over 300 ms clears temporal state before the delayed phase is due");
            schedule.Reset();report.Check(!schedule.TryTick(3,.1,5,out _,out _),"Resume schedules from the active step rather than expired countdown time");
            report.Check(!schedule.TryTick(double.NaN,.1,0,out _,out _)&&!schedule.TryTick(0,0,0,out _,out _),"Invalid clock and period never execute inference");
            schedule.Reset();schedule.TryTick(0,.1,0,out _,out _);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int step=1;step<50000;step++)schedule.TryTick(step*.02,.1,0,out _,out _);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            report.Check(allocated==0,"Actual scheduler allocates zero managed bytes in 49,999 physics steps (bytes="+allocated+")");
            report.Check(RacingRaceMetrics.IsUsableGpuTiming(2.4,10000)&&RacingRaceMetrics.IsUsableGpuTiming(195,10000),"Valid GPU durations, including a real long frame, remain measurable");
            report.Check(!RacingRaceMetrics.IsUsableGpuTiming(85576480,10000),"Impossible GPU timestamps cannot contaminate the average");
            report.Check(!RacingRaceMetrics.IsUsableGpuTiming(double.NaN,10000)&&!RacingRaceMetrics.IsUsableGpuTiming(double.PositiveInfinity,10000),"Non-finite GPU measurements are rejected");
            report.Check(!RacingRaceMetrics.IsUsableGpuTiming(0,10000)&&!RacingRaceMetrics.IsUsableGpuTiming(-1,10000),"Missing or negative GPU timings never count as valid frames");
            byte[] original=Resources.Load<TextAsset>("RacingMlWeights").bytes;
            VerifyShadowAlignment(report,original);
            File.WriteAllText(Path.Combine(output,"shadow-scheduling-contracts.json"),JsonUtility.ToJson(report,true));
            if(report.failed!=0)throw new InvalidOperationException("Shadow scheduling contracts failed: "+report.failed);
            Debug.Log("SHADOW_SCHEDULING_CONTRACTS passed="+report.passed+" failed="+report.failed);
        }
        public static void RunCandidateParity()
        {
            string[] args=Environment.GetCommandLineArgs();
            string Value(string key) { int index=Array.IndexOf(args,key);if(index<0||index+1>=args.Length)throw new ArgumentException("Missing "+key);return args[index+1]; }
            string path=Path.GetFullPath(Value("--racing-shadow-weights"));
            string output=Value("--closure-output");Directory.CreateDirectory(output);
            byte[] original=Resources.Load<TextAsset>("RacingMlWeights").bytes;
            try
            {
                byte[] candidate=File.ReadAllBytes(path);
                int fixtureFlag=Array.IndexOf(args,"--racing-parity-fixture");
                string fixturePath=fixtureFlag>=0?Value("--racing-parity-fixture"):Path.Combine(Path.GetDirectoryName(path),"native-parity.json");
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(fixturePath));
                if(!NativeNeuralEvaluator.LoadWeights(candidate)||NativeNeuralEvaluator.InputDimension!=33||NativeNeuralEvaluator.OutputDimension!=8)throw new InvalidDataException("Candidate layout rejected.");
                using(var sha=System.Security.Cryptography.SHA256.Create())
                    if(BitConverter.ToString(sha.ComputeHash(candidate)).Replace("-","").ToLowerInvariant()!=fixture.weights_sha256)throw new InvalidDataException("Candidate fixture hash mismatch.");
                var parity=new Parity{cases=fixture.cases.Length,modelVersion=NativeNeuralEvaluator.ModelVersion};
                if(parity.cases<64)throw new InvalidDataException("Insufficient numerical parity cases.");
                var ratings=new HashSet<int>();foreach(var c in fixture.cases)ratings.Add(Mathf.RoundToInt(c.features[0]*10000));
                int[] expectedRatings={100,1000,2500,5000,7500,9000,9700,10000};
                if(!ratings.SetEquals(expectedRatings))throw new InvalidDataException("Native parity must cover all eight MMR conditions.");
                parity.representedMmr=expectedRatings;
                parity.representationNote="Numerical PyTorch/native parity at current MMR / 10000; this does not establish human skill, held-out driving quality or model promotion.";
                var prediction=new float[8];
                foreach(var c in fixture.cases)
                {
                    NativeNeuralEvaluator.Predict(c.features,prediction);
                    for(int i=0;i<8;i++)parity.maxOutputError=Mathf.Max(parity.maxOutputError,Mathf.Abs(prediction[i]-c.outputs[i]));
                }
                File.WriteAllText(Path.Combine(output,"candidate-native-parity.json"),JsonUtility.ToJson(parity,true));
                if(parity.maxOutputError>=.00001f)throw new InvalidDataException("Candidate PyTorch/native parity exceeds 1e-5.");
                Debug.Log("RELEASE_CANDIDATE_PARITY cases="+parity.cases+" error="+parity.maxOutputError);
            }
            finally { if(!NativeNeuralEvaluator.LoadWeights(original))throw new InvalidDataException("Legacy weights restoration failed."); }
        }
        public static void Run(ClosureContractReport report,string output)
        {
            byte[] original=Resources.Load<TextAsset>("RacingMlWeights").bytes;
            report.Check(!NativeNeuralEvaluator.LoadWeights(null),"Missing weights rejected");
            report.Check(!NativeNeuralEvaluator.LoadWeights(new byte[24]),"Truncated weights rejected");
            var corrupt=(byte[])original.Clone();corrupt[0]=0;
            report.Check(!NativeNeuralEvaluator.LoadWeights(corrupt),"Wrong model magic rejected");
            corrupt=(byte[])original.Clone();Array.Copy(BitConverter.GetBytes(float.NaN),0,corrupt,24,4);
            report.Check(!NativeNeuralEvaluator.LoadWeights(corrupt),"Non-finite weights rejected");
            report.Check(NativeNeuralEvaluator.LoadWeights(original),"Installed legacy model restored after negative fixtures");
            var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText("tmp/release-candidate-20261005/native-parity.json"));
            var parity=new Parity{cases=fixture.cases.Length,modelVersion=NativeNeuralEvaluator.ModelVersion};
            using(var sha=System.Security.Cryptography.SHA256.Create())
                report.Check(BitConverter.ToString(sha.ComputeHash(original)).Replace("-","").ToLowerInvariant()==fixture.weights_sha256,"Parity fixture corresponds to installed weights hash");
            var prediction=new float[5];
            foreach(var c in fixture.cases){NativeNeuralEvaluator.Predict(c.features,prediction);for(int i=0;i<5;i++)parity.maxOutputError=Mathf.Max(parity.maxOutputError,Mathf.Abs(prediction[i]-c.outputs[i]));}
            report.Check(parity.maxOutputError<.00001f,"64 PyTorch/native inference cases agree within 1e-5");
            var currentFeatureFixture=JsonUtility.FromJson<Fixture>(File.ReadAllText("tmp/mmr10k/feature-parity.json"));
            var encoded=new float[RacingTelemetryFeatures.Count];RacingTelemetryFeatures.Encode(currentFeatureFixture.sample,currentFeatureFixture.mmr,encoded);
            for(int i=0;i<encoded.Length;i++)parity.maxFeatureError=Mathf.Max(parity.maxFeatureError,Mathf.Abs(encoded[i]-currentFeatureFixture.normalized[i]));
            report.Check(parity.maxFeatureError<.000001f,"All 33 Python/Unity feature normalizations agree within 1e-6");
            File.WriteAllText(Path.Combine(output,"native-parity.json"),JsonUtility.ToJson(parity,true));
            var techFixture=JsonUtility.FromJson<TechFixture>(File.ReadAllText("tmp/release-candidate-20261005/tech-parity.json"));
            var chains=MovementTechDetector.ExtractChains(techFixture.session);
            bool techParity=chains.Count==techFixture.expected.Length;
            foreach(var expected in techFixture.expected)
            {var chain=chains.Find(c=>c.techId==expected.tech_id);techParity&=chain!=null&&chain.flights==expected.flights&&chain.occurrences==expected.occurrences&&chain.successes==expected.successes;}
            report.Check(techParity,"Python and Unity agree on stable IDs, repeated-flight chains and success counts");
            report.Check(chains.TrueForAll(c=>!c.hasMatchedBaseline&&c.averageTimeSaved==0),"Runtime tech extraction cannot invent measured time savings");
            var samples=new List<TelemetrySample>();
            for(int i=0;i<6;i++)samples.Add(new TelemetrySample{timestamp=i*.1f,worldPosition=new Vector3(0,1,i),speedMps=10,speedKmh=36,velocity=Vector3.forward*10,throttle=1});
            var events=new List<TelemetryEvent>();
            report.Check(DeterministicLegalityFilter.ValidateTrajectory(samples,events).IsLegal,"Ordered physical trajectory accepted");
            var first=samples[0];first.speedMps=float.NaN;samples[0]=first;
            report.Check(!DeterministicLegalityFilter.ValidateTrajectory(samples,events).IsLegal,"Non-finite first sample rejected");
            first.speedMps=10;samples[0]=first;var second=samples[1];second.timestamp=0;samples[1]=second;
            report.Check(!DeterministicLegalityFilter.ValidateTrajectory(samples,events).IsLegal,"Duplicate sample clock rejected");
            second.timestamp=.1f;samples[1]=second;
            var root=new GameObject("Release safety contracts");
            try
            {
                var input=root.AddComponent<PlayerRacingInput>();root.AddComponent<RacingVehiclePhysics>();
                input.SetTouchSteer(-.6f);input.SetTouchPitch(.4f);
                var commands=input.SampleCommands();
                report.Check(Mathf.Abs(commands.AirRoll+.6f)<.001f&&Mathf.Abs(commands.Pitch-.4f)<.001f,"Touch joystick controls airborne roll and pitch");
                input.SetInputEnabled(false);report.Check(input.GetCommands().Throttle==0&&input.GetCommands().Pitch==0,"Disabled input clears held touch controls");
                var adapter=root.AddComponent<RacingMlInferenceAdapter>();
                adapter.executionMode=MlExecutionMode.Authority;report.Check(adapter.EffectiveMode==MlExecutionMode.Shadow,"Authority cannot bypass unpassed promotion gates");
                adapter.executionMode=MlExecutionMode.Assist;report.Check(adapter.EffectiveMode==MlExecutionMode.Shadow,"Assist remains Shadow until safety gates pass");
                adapter.executionMode=MlExecutionMode.Off;report.Check(adapter.EffectiveMode==MlExecutionMode.Off,"Off mode honored");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            VerifyShadowAlignment(report, original);
            VerifyAdventureCustomizationPreservation(report);
            var world=new GameObject("Release track manifest");
            try
            {
                var builder=world.AddComponent<CanyonRushBuilder>();builder.Build();var graph=builder.Graph;
                var manifest=new TrackManifest{nodes=new TrackNode[graph.NodeCount],shortcuts=graph.Shortcuts};
                for(int i=0;i<graph.NodeCount;i++)manifest.nodes[i]=graph.GetNode(i);
                File.WriteAllText(Path.Combine(output,"track-graph.json"),JsonUtility.ToJson(manifest,true));
                report.Check(graph.Shortcuts.Length==2,"Exactly two authored primary shortcuts");
                bool legal=true;foreach(var route in graph.Shortcuts)foreach(int gate in graph.CheckpointNodeIndices)legal&=!(gate>route.entryNodeIndex&&gate<route.exitNodeIndex);
                report.Check(legal,"Both shortcut portals preserve all required checkpoints");
                report.Check(NightCityCircuit.Sectors.Length==3,"Three named Night City sectors");
                bool scripts=true;
                foreach(var component in world.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var script=UnityEditor.MonoScript.FromMonoBehaviour(component);
                    scripts&=script!=null&&script.GetClass()==component.GetType();
                }
                report.Check(scripts,"Generated track components have resolvable Unity MonoScript assets");
                VerifyCommandSafety(report,graph);
                VerifyTrafficMeters(report,graph);
            }
            finally { UnityEngine.Object.DestroyImmediate(world); }
        }

        static void VerifyAdventureCustomizationPreservation(ClosureContractReport report)
        {
            var definition = ScriptableObject.CreateInstance<AnimeHillRacing.Vehicle.VehicleDefinition>();
            try
            {
                var skin = new AnimeHillRacing.Vehicle.VehicleDefinition.CustomizationOption
                {
                    optionId = "glitch-phase-release-contract", unlockedByDefault = false,
                    requiredBestDistance = 12000, gemPrice = 750, overrideGlitchTransform = true,
                    glitchManualSize = new Vector2(4.3f, 2.1f), glitchManualOffset = new Vector2(.2f, -.1f)
                };
                var wheel = new AnimeHillRacing.Vehicle.VehicleDefinition.CustomizationOption
                {
                    optionId = "glitch-wheel-release-contract", unlockedByDefault = false,
                    requiredBestDistance = 24000, overrideGlitchWheelTransform = true,
                    glitchRearWheelScale = .83f, glitchFrontWheelScale = 1.12f,
                    glitchRearWheelOffset = new Vector2(-.1f, .15f)
                };
                string skinBefore = JsonUtility.ToJson(skin), wheelBefore = JsonUtility.ToJson(wheel);
                definition.skins = new[]
                {
                    new AnimeHillRacing.Vehicle.VehicleDefinition.CustomizationOption
                    { optionId = "buggy-skin-red", gemPrice = 1 }, skin
                };
                definition.wheels = new[] { wheel };
                var configure = typeof(AnimeHillRacing.Editor.BuggyMountainsInstaller).GetMethod(
                    "ConfigureBuggy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                configure.Invoke(null, new object[] { definition, null, null, null, null });
                report.Check(definition.skins.Length == 4 && definition.skins[0].optionId == "buggy-skin-default" &&
                    definition.skins[1].optionId == "buggy-skin-red" && definition.skins[1].gemPrice == 500 &&
                    definition.skins[2].optionId == "buggy-skin-black-gold" && definition.skins[2].gemPrice == 500,
                    "Actual Buggy installer refreshes canonical skins without duplicating existing IDs");
                report.Check(ReferenceEquals(definition.skins[3], skin) && JsonUtility.ToJson(skin) == skinBefore,
                    "Actual Buggy installer preserves additional skin progression and Glitch transforms");
                report.Check(definition.wheels.Length == 2 && definition.wheels[0].optionId == "buggy-wheel-default" &&
                    ReferenceEquals(definition.wheels[1], wheel) && JsonUtility.ToJson(wheel) == wheelBefore,
                    "Actual Buggy installer preserves additional wheel progression and Glitch transforms");
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        static void VerifyShadowAlignment(ClosureContractReport report, byte[] original)
        {
            // A constant-output model isolates the temporal comparator from model accuracy.
            // This fixture is never a telemetry recording or a trained candidate.
            var fixture=new byte[120632];
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("ML02"),fixture,4);
            int[] dims={33,128,128,64,8};
            for(int i=0;i<dims.Length;i++)Array.Copy(BitConverter.GetBytes(dims[i]),0,fixture,4+i*4,4);
            var root=new GameObject("Release causal Shadow contract");
            try
            {
                report.Check(NativeNeuralEvaluator.LoadWeights(fixture),"Constant-output ML02 comparator fixture loads");
                var car=root.AddComponent<RacingVehiclePhysics>();
                car.Initialize(MetaProgressionConfig.MetaVehicleId,MetaProgressionConfig.MetaWheelId,null);
                root.AddComponent<AiVehicleController>();
                var adapter=root.AddComponent<RacingMlInferenceAdapter>();
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(RacingMlInferenceAdapter).GetField("staggerOffset",flags).SetValue(adapter,0);
                car.SetRaceEnabled(true);
                var property=typeof(RacingVehiclePhysics).GetProperty("LastCommands");
                property.SetValue(car,new VehicleCommands{Steering=.4f,Throttle=1});
                adapter.StepInference(.12f,1);
                report.Check(adapter.ShadowInferenceTicks==1&&adapter.ShadowComparisonTicks==0,"First causal proposal cannot compare against its own state");
                var next=new VehicleCommands{Steering=-1,Throttle=1,Jump=true,Drift=true};
                property.SetValue(car,next);string stats=JsonUtility.ToJson(car.Stats);
                adapter.StepInference(.24f,2);
                report.Check(adapter.ShadowInferenceTicks==2&&adapter.ShadowComparisonTicks==1&&Mathf.Abs(adapter.SteeringMae-1)<.0001f,"Next tick compares the previous proposal with the next command");
                report.Check(adapter.JumpAgreements==0&&adapter.DriftAgreements==0,"Causal Shadow compares commanded jump and drift intent");
                report.Check(car.LastCommands.Steering==next.Steering&&car.LastCommands.Jump&&stats==JsonUtility.ToJson(car.Stats),"Shadow cannot overwrite commands or physical stats");
                adapter.executionMode=MlExecutionMode.Off;adapter.StepInference(.36f,3);
                adapter.executionMode=MlExecutionMode.Shadow;adapter.StepInference(.48f,4);
                report.Check(adapter.ShadowInferenceTicks==3&&adapter.ShadowComparisonTicks==1,"Off clears pending proposals without inventing comparisons on resume");
                adapter.StepInference(1f,5);
                report.Check(adapter.ShadowInferenceTicks==4&&adapter.ShadowComparisonTicks==1,"Clock discontinuity clears a pending proposal before inference resumes");
                car.SetRaceEnabled(false);adapter.StepInference(1.02f,6);
                car.SetRaceEnabled(true);adapter.StepInference(5f,7);
                report.Check(adapter.ShadowInferenceTicks==5&&adapter.ShadowComparisonTicks==1,"Countdown or inactive racing clears comparisons and resets scheduling");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if(!NativeNeuralEvaluator.LoadWeights(original))throw new InvalidDataException("Installed model restoration failed.");
            }
        }

        static void VerifyTrafficMeters(ClosureContractReport report,TrackSplineGraph graph)
        {
            var root=new GameObject("Traffic lane occupation regression");
            try
            {
                var tracker=root.AddComponent<TrackProgressTracker>();tracker.Initialize(graph,3);
                RacingVehiclePhysics Racer(string id,Vector3 position,float speed,int grid)
                {
                    var item=new GameObject(id);item.transform.SetParent(root.transform,false);item.transform.position=position;
                    var car=item.AddComponent<RacingVehiclePhysics>();car.Initialize(MetaProgressionConfig.MetaVehicleId,MetaProgressionConfig.MetaWheelId,null);
                    car.Body.linearVelocity=Vector3.forward*speed;
                    tracker.RegisterRacer(new RacerIdentity(id,id,car.VehicleId,car.WheelId,10000,false,332,grid),car,null);
                    return car;
                }
                var subject=Racer("subject",Vector3.right*3,20,1);
                Racer("slower leader",Vector3.right*3+Vector3.forward*8,10,2);
                Racer("occupied left lane",Vector3.right*(3-4.8f)+Vector3.forward*8,10,3);
                var driver=subject.gameObject.AddComponent<AiVehicleController>();driver.Initialize(AiSkillModel.CreateProfile(10000,332),graph,tracker);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(AiVehicleController).GetField("lateralOffset",flags).SetValue(driver,3f);
                var road=new TrackNode {Position=Vector3.zero,Forward=Vector3.forward,Right=Vector3.right,TrackWidth=30};
                typeof(AiVehicleController).GetMethod("EvaluatePack",flags).Invoke(driver,new object[]{road,3f,0f});
                float selected=(float)typeof(AiVehicleController).GetField("targetLane",flags).GetValue(driver);
                report.Check(driver.PassingDecisions==1 && selected>0,"A 3-meter racing line recognizes a rival in the left lane and chooses the physically free right lane");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void VerifyCommandSafety(ClosureContractReport report,TrackSplineGraph graph)
        {
            var root=new GameObject("Release physical command safety");
            try
            {
                var car=root.AddComponent<RacingVehiclePhysics>();
                car.Initialize(MetaProgressionConfig.MetaVehicleId,MetaProgressionConfig.MetaWheelId,null);
                var driver=root.AddComponent<AiVehicleController>();
                driver.Initialize(AiSkillModel.CreateProfile(10000,331),graph,null,"Command contract");
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                void Set(object target,string name,object value)=>target.GetType().GetField(name,flags).SetValue(target,value);
                void Control()=>typeof(AiVehicleController).GetMethod("ExecuteFastControl",flags).Invoke(driver,null);
                void Plan()=>typeof(AiVehicleController).GetMethod("UpdateActionPlan",flags).Invoke(driver,null);
                string originalStats=JsonUtility.ToJson(car.Stats);
                root.transform.rotation=Quaternion.LookRotation(graph.SampleDistance(17f).Forward,Vector3.up);
                Set(driver,"target",root.transform.forward*35);Set(driver,"targetSpeed",110f);Set(driver,"angle",0f);
                car.Body.linearVelocity=root.transform.forward*50;Set(car,"isGrounded",true);Control();
                report.Check(driver.GetCommands().Throttle==0&&driver.GetCommands().Brake>.9f,"Overspeed on a straight cannot restore full throttle");
                Set(car,"isGrounded",false);Set(car,"airborneTime",.3f);Set(driver,"simTimeOverride",(float?).3f);
                Set(driver,"targetSpeed",115f);car.Body.linearVelocity=root.transform.forward*(100f/3.6f);Plan();Control();
                report.Check(!driver.GetCommands().AerialBoost,"Natural road crest cannot invent an aerial launch plan");
                Set(car,"lastJumpTime",0f);Control();
                report.Check(driver.GetCommands().AerialBoost,"Recent intentional launch can request a bounded aerial pulse");
                Set(car,"aerialBoostTimer",.5f);Control();
                report.Check(!driver.GetCommands().AerialBoost,"Aerial pulse stops at its planned duration");
                Set(car,"aerialBoostTimer",0f);Set(car,"lastJumpTime",-.8f);Control();
                report.Check(!driver.GetCommands().AerialBoost,"Landing bounce cannot reuse a previous flight's launch");
                Set(car,"lastJumpTime",0f);car.Body.linearVelocity=root.transform.forward*70;Control();
                report.Check(!driver.GetCommands().AerialBoost,"Aerial pulse cannot exceed its commanded speed target");
                report.Check(originalStats==JsonUtility.ToJson(car.Stats),"Planning and control leave all physical vehicle statistics unchanged");
                Set(driver,"activeRoute",graph.Shortcuts[0]);Set(driver,"routeSegment",0);Plan();
                report.Check(driver.PlannedActionDepth==5&&driver.PlanSummary.Contains("Launch")&&driver.PlanSummary.Contains("Rejoin"),"Elite route plan contains five bounded action phases");
                Set(driver,"routeSegment",graph.Shortcuts[0].landingNodeIndex);Plan();
                report.Check(driver.PlanSummary=="LandingBoost > Rejoin","Completed launch phases leave only landing and rejoin actions");
                report.Check(!car.UsesHumanInput,"AI command provider cannot claim human input provenance");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
#endif
