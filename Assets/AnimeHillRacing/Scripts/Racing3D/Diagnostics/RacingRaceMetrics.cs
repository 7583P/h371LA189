using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
namespace AnimeHillRacing.Racing3D
{
    [Serializable] public sealed class RacerTelemetry
    {
        public string name,vehicle,wheels;public int mmr,position,collisions,items,recoveries,checkpointVisits;public bool finished;
        public float drivingSeconds,averageSpeedKmh,offroadSeconds,driftSeconds,airborneSeconds,bestLap,finishTime;
        public int shortcutVisits;public List<float> lapTimes=new List<float>();
        public int wallCollisions,racerCollisions,itemHits,miniTurbos,jumps,tricks,landingBoosts,shortcutCompletions,passingDecisions,defensiveDecisions,trafficBrakingDecisions;
        public string buildCategory; public float metaProbability;
        public int driftJumpChains,jumpTrickLandingChains,boostsForShortcuts;
        [NonSerialized] public RacingVehiclePhysics physics;[NonSerialized] public ItemInventory inventory;[NonSerialized] public VehicleRecovery recovery;[NonSerialized] public AiVehicleController ai;[NonSerialized] public float speedIntegral,lastLapTime;[NonSerialized] public bool inShortcut;
    }
    [Serializable] public sealed class RaceTelemetryReport
    {
        public string classification,quality,device,graphics,track;public int seed,frames,poolDroppedRequests;public float meanFrameMs,p95FrameMs,meanMainThreadMs,meanGpuMs,meanGcBytes,maxGcBytes;public bool gpuTimingAvailable;
        public bool equalBuildBenchmark;
        public bool offscreenRendering; public int frameRateLimit;
        public string processor,physicsCounter,aiCounter,drawCounter,batchCounter;
        public int processorCount,width,height,gpuFrames,poolCapacity,poolCreated,poolActiveAtFinish,poolPeakActive,poolActivations,poolReleases,poolReuses;
        public int particleSystems,peakActiveParticleSystems,peakParticles,vfxBursts,vfxEmittedParticles;
        public float timeScale,measuredSeconds,averageFps,minimumFps,p95LowFps,p99FrameMs,meanPhysicsMs,meanAiMs,meanDrawCalls,meanBatches;
        public bool mainThreadAvailable,gcAvailable,physicsAvailable,aiAvailable,drawCallsAvailable,batchesAvailable;
        public string renderCounter,instantiateCounter,destroyCounter,gcCollectCounter;
        public bool renderAvailable,instantiateAvailable,destroyAvailable,gcCollectAvailable,developmentBuild,hudEnabled;
        public float onePercentLowFps,meanRenderMs,maxGcCollectMs,worstGcCollectionFrameMs;
        public int gcCollectionFrames,gcAllocationSpikeFrames,instantiateCalls,destroyCalls,itemEntitiesCreatedDuringRace;
        public int vfxSystemsCreated,impactBursts,boostBursts;
        public RacingPoolCounters missiles,traps;
        public List<RacerTelemetry> racers=new List<RacerTelemetry>();
    }
    public sealed class RacingRaceMetrics:MonoBehaviour
    {
        RaceManager manager;RaceTelemetryReport report=new RaceTelemetryReport();List<float> frameSamples=new List<float>(20000);
        ProfilerRecorder mainThread,gc;double cpuTotal,gpuTotal,gcTotal;float gcMax;int gpuFrames;FrameTiming[] timing=new FrameTiming[1];bool bound;
        ProfilerRecorder physics,ai,drawCalls,batches;double physicsTotal,aiTotal,drawTotal,batchTotal;
        ProfilerRecorder render,instantiate,destroy,gcCollect;double renderTotal;
        int createdAtGo,previousCollections;
        bool prepared;
        RacingItemPool itemPool;RacingVfxPool vfxPool;ParticleSystem[] particleSystems;
        public float MeanFrameMs=>frameSamples.Count>0?(float)frameSamplesTotal/frameSamples.Count:0;
        double frameSamplesTotal;
        public float GcBytes=>gc.Valid?gc.LastValue:0;
        public float CpuMs=>mainThread.Valid?mainThread.LastValue/1000000f:0;
        public RaceTelemetryReport Report=>report;
        public string ExportPath { get; set; }
        void Start()
        {
            manager=FindFirstObjectByType<RaceManager>();
            mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            if(manager!=null){manager.OnStateChanged+=State;manager.OnRaceCompleted+=Complete;}
        }
        void State(RaceState state)
        {
            if(state==RaceState.Racing)
            {
                bound=true;report.timeScale=Time.timeScale;report.frameRateLimit=Application.targetFrameRate;
                createdAtGo=itemPool!=null?itemPool.CreatedCount:0;previousCollections=GC.CollectionCount(0);
                return;
            }
            // Enumerating profiler metadata and exporting counter names allocates.
            // Prepare it during countdown, before the complete GO-to-Results sample.
            if(state!=RaceState.Countdown||prepared)return;prepared=true;
            report.seed=manager.EffectiveSeed;report.track=manager.ProgressTracker.TrackGraph.name;report.quality=RacingQualityScope.Current.ToString();report.device=SystemInfo.deviceModel;report.graphics=SystemInfo.graphicsDeviceName;
            report.equalBuildBenchmark=manager.Rules.EqualBuildBenchmark;
            report.classification=Application.isEditor?(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null?"EDITOR_HEADLESS_PHYSICS":"EDITOR_PLAY_MODE"):"WINDOWS_STANDALONE";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            report.offscreenRendering=FindFirstObjectByType<RacingRenderedPlayerProbe>()?.IsBound==true;
            if(report.offscreenRendering)
            {
                report.classification="WINDOWS_STANDALONE_FULL_OFFSCREEN_RENDER";
                // Uncapped profiling records many more frames than a 60 FPS race.
                // Reserve storage before GO so diagnostic list growth cannot cause
                // artificial allocation spikes in the measured gameplay interval.
                frameSamples.Capacity=Mathf.Max(frameSamples.Capacity,200000);
            }
#endif
            report.processor=SystemInfo.processorType;report.processorCount=SystemInfo.processorCount;report.width=Screen.width;report.height=Screen.height;report.timeScale=Time.timeScale;
            report.mainThreadAvailable=mainThread.Valid;report.gcAvailable=gc.Valid;
            report.developmentBuild=Debug.isDebugBuild;report.hudEnabled=FindFirstObjectByType<RacingHUD>()!=null;
            var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
            physics=Counter(available,out report.physicsCounter,"Physics.Simulate","Physics.Processing");
            ai=Counter(available,out report.aiCounter,"Racing.AI.Decide");
            drawCalls=Counter(available,out report.drawCounter,"Draw Calls Count");batches=Counter(available,out report.batchCounter,"Batches Count");
            render=Counter(available,out report.renderCounter,"Racing.RenderProbe.Camera","Camera.Render","Render Thread");
            instantiate=Counter(available,out report.instantiateCounter,"Object.Instantiate","Instantiate");
            destroy=Counter(available,out report.destroyCounter,"Object.Destroy","Destroy");
            gcCollect=Counter(available,out report.gcCollectCounter,"GC.Collect","GarbageCollectIncremental");
            report.renderAvailable=render.Valid;report.instantiateAvailable=instantiate.Valid;report.destroyAvailable=destroy.Valid;report.gcCollectAvailable=gcCollect.Valid;
            report.physicsAvailable=physics.Valid;report.aiAvailable=ai.Valid;report.drawCallsAvailable=drawCalls.Valid;report.batchesAvailable=batches.Valid;
            itemPool=FindFirstObjectByType<RacingItemPool>();vfxPool=FindFirstObjectByType<RacingVfxPool>();particleSystems=FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);report.particleSystems=particleSystems.Length;
            if(!string.IsNullOrEmpty(ExportPath))
            {
                var names=new List<string>();foreach(var handle in available){var description=ProfilerRecorderHandle.GetDescription(handle);names.Add(description.Category.Name+" / "+description.Name+" / "+description.UnitType);}
                File.WriteAllLines(Path.Combine(Path.GetDirectoryName(ExportPath),"profiler-counters.txt"),names);
            }
            foreach(var racer in manager.ProgressTracker.Leaderboard)
            {
                var metric=new RacerTelemetry{name=racer.Identity.displayName,vehicle=racer.Identity.vehicleId,wheels=racer.Identity.wheelId,mmr=racer.Identity.mmr,physics=racer.Vehicle,inventory=racer.Vehicle.GetComponent<ItemInventory>(),recovery=racer.Recovery};
                metric.buildCategory=report.equalBuildBenchmark?"ControlledEqualBuild":racer.Identity.buildCategory.ToString();metric.metaProbability=racer.Identity.metaProbabilityUsed;
                metric.ai=racer.Vehicle.GetComponent<AiVehicleController>();
                report.racers.Add(metric);
            }
            manager.ProgressTracker.OnRacerLapCompleted+=Lap;
        }
        void Lap(RacerProgress racer,int lap)
        {
            foreach(var metric in report.racers)if(metric.physics==racer.Vehicle){metric.lapTimes.Add(racer.LastLapDuration);metric.lastLapTime=manager.RaceElapsed;break;}
        }
        void FixedUpdate()
        {
            if(!bound||(manager.State!=RaceState.Racing&&manager.State!=RaceState.PlayerFinished&&manager.State!=RaceState.RaceFinishing))return;float dt=Time.fixedDeltaTime;
            foreach(var metric in report.racers)
            {
                var progress=manager.ProgressTracker.FindRacer(metric.physics);if(progress.Finished)continue;
                metric.drivingSeconds+=dt;metric.speedIntegral+=metric.physics.SpeedKmh*dt;if(metric.physics.IsOffroad)metric.offroadSeconds+=dt;if(metric.physics.IsDrifting)metric.driftSeconds+=dt;if(!metric.physics.IsGrounded)metric.airborneSeconds+=dt;
                SnapshotMechanics(metric);
            }
        }
        void Update()
        {
            if(!bound||(manager.State!=RaceState.Racing&&manager.State!=RaceState.PlayerFinished&&manager.State!=RaceState.RaceFinishing))return;
            float ms=Time.unscaledDeltaTime*1000;frameSamples.Add(ms);frameSamplesTotal+=ms;cpuTotal+=CpuMs;gcTotal+=GcBytes;gcMax=Mathf.Max(gcMax,GcBytes);
            if(physics.Valid)physicsTotal+=physics.LastValue/1000000.0;if(ai.Valid)aiTotal+=ai.LastValue/1000000.0;
            if(drawCalls.Valid)drawTotal+=drawCalls.LastValue;if(batches.Valid)batchTotal+=batches.LastValue;
            if(render.Valid)renderTotal+=render.LastValue/1000000.0;
            if(instantiate.Valid&&instantiate.Count>0)report.instantiateCalls+=(int)instantiate.GetSample(instantiate.Count-1).Count;
            if(destroy.Valid&&destroy.Count>0)report.destroyCalls+=(int)destroy.GetSample(destroy.Count-1).Count;
            int collections=GC.CollectionCount(0);
            if(collections>previousCollections){report.gcCollectionFrames++;report.worstGcCollectionFrameMs=Mathf.Max(report.worstGcCollectionFrameMs,ms);}
            previousCollections=collections;
            if(GcBytes>=65536)report.gcAllocationSpikeFrames++;
            if(gcCollect.Valid)report.maxGcCollectMs=Mathf.Max(report.maxGcCollectMs,gcCollect.LastValue/1000000f);
            int activeParticles=0,particles=0;foreach(var system in particleSystems)if(system!=null&&system.gameObject.activeInHierarchy&&system.particleCount>0){activeParticles++;particles+=system.particleCount;}
            report.peakActiveParticleSystems=Mathf.Max(report.peakActiveParticleSystems,activeParticles);report.peakParticles=Mathf.Max(report.peakParticles,particles);
            FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpuTotal+=timing[0].gpuFrameTime;gpuFrames++;}
        }
        static ProfilerRecorder Counter(List<ProfilerRecorderHandle> available,out string selected,params string[] candidates)
        {
            foreach(string candidate in candidates)foreach(var handle in available)
            {
                var description=ProfilerRecorderHandle.GetDescription(handle);if(description.Name!=candidate)continue;
                selected=description.Name;return new ProfilerRecorder(handle,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame);
            }
            selected="Unavailable";return default;
        }
        void Complete(RaceResults results)
        {
            foreach(var metric in report.racers)
            {
                var progress=manager.ProgressTracker.FindRacer(metric.physics);
                if(progress.Finished && progress.LastLapDuration>0)metric.lapTimes.Add(progress.LastLapDuration);
            }
            Export(ExportPath ?? Path.Combine(Application.dataPath,"../Logs/RacingPhase2/last-race-metrics.json"));
        }
        public void Export(string path)
        {
            report.frames=frameSamples.Count;report.meanFrameMs=MeanFrameMs;if(frameSamples.Count>0){var sorted=frameSamples.ToArray();Array.Sort(sorted);int lowFrames=Mathf.Max(1,Mathf.CeilToInt(sorted.Length*.01f));double lowTotal=0;for(int i=sorted.Length-lowFrames;i<sorted.Length;i++)lowTotal+=sorted[i];report.onePercentLowFps=(float)(1000*lowFrames/lowTotal);report.p95FrameMs=sorted[Mathf.Clamp(Mathf.FloorToInt(sorted.Length*.95f),0,sorted.Length-1)];report.p99FrameMs=sorted[Mathf.Clamp(Mathf.FloorToInt(sorted.Length*.99f),0,sorted.Length-1)];report.minimumFps=1000/sorted[sorted.Length-1];report.p95LowFps=1000/report.p95FrameMs;report.averageFps=1000/report.meanFrameMs;report.measuredSeconds=(float)frameSamplesTotal/1000;report.meanMainThreadMs=(float)(cpuTotal/frameSamples.Count);report.meanGcBytes=(float)(gcTotal/frameSamples.Count);report.meanPhysicsMs=(float)(physicsTotal/frameSamples.Count);report.meanAiMs=(float)(aiTotal/frameSamples.Count);report.meanDrawCalls=(float)(drawTotal/frameSamples.Count);report.meanBatches=(float)(batchTotal/frameSamples.Count);}
            report.meanGpuMs=gpuFrames>0?(float)(gpuTotal/gpuFrames):0;report.gpuTimingAvailable=gpuFrames>0;report.maxGcBytes=gcMax;report.poolDroppedRequests=FindFirstObjectByType<RacingItemPool>()?.DroppedRequests??0;
            report.gpuFrames=gpuFrames;
            if(frameSamples.Count>0){report.meanRenderMs=(float)(renderTotal/frameSamples.Count);}
            if(itemPool!=null){report.poolCapacity=itemPool.Capacity;report.poolCreated=itemPool.CreatedCount;report.poolActiveAtFinish=itemPool.ActiveCount;report.poolPeakActive=itemPool.PeakActiveCount;report.poolActivations=itemPool.Activations;report.poolReleases=itemPool.Releases;report.poolReuses=itemPool.Reuses;}
            if(itemPool!=null){report.missiles=itemPool.MissileCounters;report.traps=itemPool.TrapCounters;report.itemEntitiesCreatedDuringRace=itemPool.CreatedCount-createdAtGo;}
            if(vfxPool!=null){report.vfxBursts=vfxPool.BurstCount;report.vfxEmittedParticles=vfxPool.EmittedParticles;report.vfxSystemsCreated=vfxPool.CreatedSystems;report.impactBursts=vfxPool.ImpactBursts;report.boostBursts=vfxPool.BoostBursts;}
            foreach(var metric in report.racers){var progress=manager.ProgressTracker.FindRacer(metric.physics);metric.position=progress.RacePosition;metric.finished=progress.Finished;metric.finishTime=progress.FinishTime;metric.bestLap=progress.GetDisplayBestLap();metric.checkpointVisits=progress.CheckpointVisits;metric.items=metric.inventory.ItemsUsed;metric.recoveries=metric.recovery.RecoveryCount;metric.averageSpeedKmh=metric.drivingSeconds>0?metric.speedIntegral/metric.drivingSeconds:0;}
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        }
        static void SnapshotMechanics(RacerTelemetry metric)
        {
            var car=metric.physics;
            metric.collisions=car.PhysicalCollisions;metric.wallCollisions=car.WallCollisions;metric.racerCollisions=car.RacerCollisions;metric.itemHits=car.ItemHits;
            metric.miniTurbos=car.MiniTurbosReleased;metric.jumps=car.JumpsPerformed;metric.tricks=car.TricksPerformed;metric.landingBoosts=car.LandingBoosts;
            metric.driftJumpChains=car.DriftJumpChains;metric.jumpTrickLandingChains=car.JumpTrickLandingChains;
            var ai=metric.ai;
            if(ai==null)return;
            metric.shortcutVisits=ai.ShortcutEntries;metric.shortcutCompletions=ai.ShortcutCompletions;
            metric.passingDecisions=ai.PassingDecisions;metric.defensiveDecisions=ai.DefensiveDecisions;metric.trafficBrakingDecisions=ai.TrafficBrakingDecisions;
            metric.boostsForShortcuts=ai.BoostsForShortcuts;
        }
        void OnDestroy(){mainThread.Dispose();gc.Dispose();physics.Dispose();ai.Dispose();drawCalls.Dispose();batches.Dispose();render.Dispose();instantiate.Dispose();destroy.Dispose();gcCollect.Dispose();if(manager!=null){manager.OnStateChanged-=State;manager.OnRaceCompleted-=Complete;if(manager.ProgressTracker!=null)manager.ProgressTracker.OnRacerLapCompleted-=Lap;}}
    }
}

