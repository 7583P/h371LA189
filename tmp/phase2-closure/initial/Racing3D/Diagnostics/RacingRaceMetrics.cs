using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;
namespace AnimeHillRacing.Racing3D
{
    [Serializable] public sealed class RacerTelemetry
    {
        public string name,vehicle,wheels;public int mmr,position,collisions,items,recoveries,checkpointVisits;public bool finished;
        public float drivingSeconds,averageSpeedKmh,offroadSeconds,driftSeconds,airborneSeconds,bestLap,finishTime;
        public int shortcutVisits;public List<float> lapTimes=new List<float>();
        [NonSerialized] public RacingVehiclePhysics physics;[NonSerialized] public ItemInventory inventory;[NonSerialized] public VehicleRecovery recovery;[NonSerialized] public float speedIntegral,lastLapTime;[NonSerialized] public bool inShortcut;
    }
    [Serializable] public sealed class RaceTelemetryReport
    {
        public string classification,quality,device,graphics,track;public int seed,frames,poolDroppedRequests;public float meanFrameMs,p95FrameMs,meanMainThreadMs,meanGpuMs,meanGcBytes,maxGcBytes;public bool gpuTimingAvailable;
        public List<RacerTelemetry> racers=new List<RacerTelemetry>();
    }
    public sealed class RacingRaceMetrics:MonoBehaviour
    {
        RaceManager manager;RaceTelemetryReport report=new RaceTelemetryReport();List<float> frameSamples=new List<float>(20000);
        ProfilerRecorder mainThread,gc;double cpuTotal,gpuTotal,gcTotal;float gcMax;int gpuFrames;FrameTiming[] timing=new FrameTiming[1];bool bound;
        public float MeanFrameMs=>frameSamples.Count>0?(float)frameSamplesTotal/frameSamples.Count:0;
        double frameSamplesTotal;
        public float GcBytes=>gc.Valid?gc.LastValue:0;
        public float CpuMs=>mainThread.Valid?mainThread.LastValue/1000000f:0;
        public RaceTelemetryReport Report=>report;
        void Start()
        {
            manager=FindFirstObjectByType<RaceManager>();
            mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            if(manager!=null){manager.OnStateChanged+=State;manager.OnRaceCompleted+=Complete;}
        }
        void State(RaceState state)
        {
            if(state!=RaceState.Racing||bound)return;bound=true;
            report.seed=manager.EffectiveSeed;report.track=manager.ProgressTracker.TrackGraph.name;report.quality=RacingQualityScope.Current.ToString();report.device=SystemInfo.deviceModel;report.graphics=SystemInfo.graphicsDeviceName;
            report.classification=Application.isEditor?"EDITOR_PLAY_MODE":"WINDOWS_STANDALONE";
            foreach(var racer in manager.ProgressTracker.Leaderboard)
            {
                var metric=new RacerTelemetry{name=racer.Identity.displayName,vehicle=racer.Identity.vehicleId,wheels=racer.Identity.wheelId,mmr=racer.Identity.mmr,physics=racer.Vehicle,inventory=racer.Vehicle.GetComponent<ItemInventory>(),recovery=racer.Recovery};
                racer.Vehicle.OnImpact+=value=>{if(value>.12f)metric.collisions++;};report.racers.Add(metric);
            }
            manager.ProgressTracker.OnRacerLapCompleted+=Lap;
        }
        void Lap(RacerProgress racer,int lap)
        {
            foreach(var metric in report.racers)if(metric.physics==racer.Vehicle){metric.lapTimes.Add(racer.LastLapDuration);metric.lastLapTime=manager.RaceElapsed;break;}
        }
        void FixedUpdate()
        {
            if(!bound||manager.State!=RaceState.Racing)return;float dt=Time.fixedDeltaTime;
            foreach(var metric in report.racers)
            {
                var progress=manager.ProgressTracker.FindRacer(metric.physics);if(progress.Finished)continue;
                metric.drivingSeconds+=dt;metric.speedIntegral+=metric.physics.SpeedKmh*dt;if(metric.physics.IsOffroad)metric.offroadSeconds+=dt;if(metric.physics.IsDrifting)metric.driftSeconds+=dt;if(!metric.physics.IsGrounded)metric.airborneSeconds+=dt;
                bool inShortcut=false;
                if(metric.physics.IsOffroad)
                {
                    manager.ProgressTracker.TrackGraph.ProjectPosition(metric.physics.transform.position,out int node,out _,out _);
                    foreach(var shortcut in manager.ProgressTracker.TrackGraph.Shortcuts)if(node>=shortcut.entryNodeIndex&&node<=shortcut.exitNodeIndex)inShortcut=true;
                }
                if(inShortcut&&!metric.inShortcut)metric.shortcutVisits++;metric.inShortcut=inShortcut;
            }
        }
        void Update()
        {
            if(!bound||manager.State!=RaceState.Racing||manager.RaceElapsed<8)return;
            float ms=Time.unscaledDeltaTime*1000;frameSamples.Add(ms);frameSamplesTotal+=ms;cpuTotal+=CpuMs;gcTotal+=GcBytes;gcMax=Mathf.Max(gcMax,GcBytes);
            FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpuTotal+=timing[0].gpuFrameTime;gpuFrames++;}
        }
        void Complete(RaceResults results)
        {
            foreach(var metric in report.racers)
            {
                var progress=manager.ProgressTracker.FindRacer(metric.physics);
                if(progress.Finished && progress.LastLapDuration>0)metric.lapTimes.Add(progress.LastLapDuration);
            }
            Export(Path.Combine(Application.dataPath,"../Logs/RacingPhase2/last-race-metrics.json"));
        }
        public void Export(string path)
        {
            report.frames=frameSamples.Count;report.meanFrameMs=MeanFrameMs;if(frameSamples.Count>0){var sorted=frameSamples.ToArray();Array.Sort(sorted);report.p95FrameMs=sorted[Mathf.Clamp(Mathf.FloorToInt(sorted.Length*.95f),0,sorted.Length-1)];report.meanMainThreadMs=(float)(cpuTotal/frameSamples.Count);report.meanGcBytes=(float)(gcTotal/frameSamples.Count);}
            report.meanGpuMs=gpuFrames>0?(float)(gpuTotal/gpuFrames):0;report.gpuTimingAvailable=gpuFrames>0;report.maxGcBytes=gcMax;report.poolDroppedRequests=FindFirstObjectByType<RacingItemPool>()?.DroppedRequests??0;
            foreach(var metric in report.racers){var progress=manager.ProgressTracker.FindRacer(metric.physics);metric.position=progress.RacePosition;metric.finished=progress.Finished;metric.finishTime=progress.FinishTime;metric.bestLap=progress.GetDisplayBestLap();metric.checkpointVisits=progress.CheckpointVisits;metric.items=metric.inventory.ItemsUsed;metric.recoveries=metric.recovery.RecoveryCount;metric.averageSpeedKmh=metric.drivingSeconds>0?metric.speedIntegral/metric.drivingSeconds:0;}
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        }
        void OnDestroy(){mainThread.Dispose();gc.Dispose();if(manager!=null){manager.OnStateChanged-=State;manager.OnRaceCompleted-=Complete;if(manager.ProgressTracker!=null)manager.ProgressTracker.OnRacerLapCompleted-=Lap;}}
    }
}
