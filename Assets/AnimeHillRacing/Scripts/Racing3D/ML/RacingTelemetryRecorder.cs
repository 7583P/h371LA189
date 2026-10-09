using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D.ML
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class RacingTelemetryRecorder : MonoBehaviour
    {
        static readonly ProfilerMarker RecordMarker=new ProfilerMarker("Racing.Telemetry.Record");
        [Range(5f,20f)] public float samplingFrequency=10;
        public bool isHumanPlayer=true;
        public int playerMmr=7500;
        RacingVehiclePhysics physics;
        TrackProgressTracker tracker;
        VehicleRecovery recovery;
        ItemInventory inventory;
        AiVehicleController driver;
        int previousLap,previousEntries,previousCompletions;
        bool previousDrift;
        readonly List<float> sectors=new List<float>(9);
        float lastSector=-1;
        IRacingTelemetrySink sink;
        RaceTelemetrySession session;
        float nextSample,sessionStart,lastLandingBoost=float.NegativeInfinity;
        bool recording,jumpSinceSample;
        public bool IsRecording=>recording;
        public RaceTelemetrySession Session=>session;
        public void SetSink(IRacingTelemetrySink telemetrySink)=>sink=telemetrySink;
        public void MarkScripted()
        {isHumanPlayer=false;if(session!=null){session.metadata.isHumanPlayer=false;session.metadata.source="SCRIPTED_UNITY_PHYSICS";}}
        void Awake(){physics=GetComponent<RacingVehiclePhysics>();}
        void OnEnable(){Subscribe();}
        void Subscribe()
        {
            if(physics==null)physics=GetComponent<RacingVehiclePhysics>();
            Unsubscribe();
            physics.OnJump+=Jump;physics.OnLanding+=Landing;physics.OnLandingBoost+=LandingBoost;
            physics.OnAerialBoost+=AerialBoost;physics.OnMiniTurboReleased+=MiniTurbo;physics.OnPhysicalCollision+=Collision;
            physics.OnItemHit+=ItemHit;
            recovery=GetComponent<VehicleRecovery>();if(recovery!=null)recovery.OnRecoveryRequested+=Recovery;
            inventory=GetComponent<ItemInventory>();if(inventory!=null)inventory.OnItemUsed+=ItemUsed;
        }
        void Unsubscribe()
        {
            if(physics!=null){physics.OnJump-=Jump;physics.OnLanding-=Landing;physics.OnLandingBoost-=LandingBoost;physics.OnAerialBoost-=AerialBoost;physics.OnMiniTurboReleased-=MiniTurbo;physics.OnPhysicalCollision-=Collision;}
            if(physics!=null)physics.OnItemHit-=ItemHit;
            if(recovery!=null)recovery.OnRecoveryRequested-=Recovery;
            if(inventory!=null)inventory.OnItemUsed-=ItemUsed;
            if(tracker!=null)tracker.OnCheckpointReached-=Checkpoint;
        }
        void OnDisable(){Unsubscribe();}
        public void StartSession(string trackId,int seed,int mmr,bool human)
        {
            Subscribe();tracker=FindFirstObjectByType<TrackProgressTracker>();
            if(tracker!=null)tracker.OnCheckpointReached+=Checkpoint;
            sectors.Clear();lastSector=-1;
            playerMmr=mmr;isHumanPlayer=human;sessionStart=Time.time;nextSample=Time.time;lastLandingBoost=float.NegativeInfinity;
            jumpSinceSample=false;
            driver=GetComponent<AiVehicleController>();
            previousEntries=driver!=null?driver.ShortcutEntries:0;previousCompletions=driver!=null?driver.ShortcutCompletions:0;
            previousLap=1;previousDrift=false;
            session=new RaceTelemetrySession {
                metadata=new RaceMetadata {sessionId=Guid.NewGuid().ToString("N"),gameVersion=Application.version,
                    trackId=trackId,trackSeed=seed,playerMmr=mmr,normalizedSkill=AiSkillModel.Normalize(mmr),
                    vehicleName=physics.VehicleId,wheelSetup=physics.WheelId,racerCount=12,isHumanPlayer=human,
                    source=human?"HUMAN_INPUT":"SCRIPTED_UNITY_PHYSICS"},
                samples=new List<TelemetrySample>(8000),events=new List<TelemetryEvent>(8192)
            };
            recording=true;
        }
        void FixedUpdate(){StepRecord(Time.time);}
        public void StepRecord(float currentTime)
        {
            if(!recording||physics==null||!physics.RaceEnabled||currentTime<nextSample||session.samples.Count>=8000)return;
            using(RecordMarker.Auto())
            {
            nextSample=currentTime+1/Mathf.Clamp(samplingFrequency,5,20);
            var sample=RacingTelemetryFeatures.Capture(physics,tracker,currentTime-sessionStart);
            sample.jumpInput|=jumpSinceSample;sample.isJumping=sample.jumpInput;jumpSinceSample=false;
            sample.hasLandingBoost=currentTime-lastLandingBoost<1.85f&&physics.BoostRemainingSeconds>.1f;
            if(sample.isDrifting&&!previousDrift)Event(TelemetryEventType.DriftStart,"DriftStart");
            previousDrift=sample.isDrifting;
            if(sample.lap>previousLap)Event(TelemetryEventType.LapCompleted,"LapCompleted",0,sample.lap-1);
            previousLap=sample.lap;
            if(driver!=null)
            {
                if(driver.ShortcutEntries>previousEntries)Event(TelemetryEventType.ShortcutEnter,"ShortcutEnter");
                if(driver.ShortcutCompletions>previousCompletions)Event(TelemetryEventType.ShortcutExit,"ShortcutExit");
                previousEntries=driver.ShortcutEntries;previousCompletions=driver.ShortcutCompletions;
            }
            session.samples.Add(sample);
            }
        }
        public void EndSession(int position,float time,float bestLap,float[] lapTimes)
        {
            if(!recording||session==null)return;recording=false;
            session.metadata.finalPosition=position;session.metadata.totalRaceTime=time;session.metadata.bestLapTime=bestLap;session.metadata.lapTimes=lapTimes;
            session.metadata.sectorTimes=sectors.ToArray();
            // An automated driver attached after spawn can never masquerade as human input.
            if(GetComponent<AiVehicleController>()!=null || !physics.UsesHumanInput)MarkScripted();
            var legality=DeterministicLegalityFilter.ValidateTrajectory(session.samples,session.events);
            session.metadata.isLegallyValidated=legality.IsLegal;session.metadata.legalityNote=legality.RejectionReason;
            session.ComputeQualityScore();
            if(sink==null)sink=new LocalTelemetrySink();sink.RecordSession(session);
        }
        void Event(TelemetryEventType type,string tag,float value=0,int param=0)
        {if(recording&&session.events.Count<8192)session.events.Add(new TelemetryEvent{timestamp=Time.time-sessionStart,eventType=type,position=transform.position,tag=tag,param1=param,param2=value});}
        void Jump(){bool commanded=physics.LastCommands.Jump;jumpSinceSample|=commanded;Event(TelemetryEventType.JumpStart,commanded?"CommandJump":"AutomaticLaunch");}
        void Landing(float impact){Event(TelemetryEventType.Landing,"Landing",impact);}
        void LandingBoost(){lastLandingBoost=Time.time;Event(TelemetryEventType.LandingBoost,"LandingBoost");}
        void AerialBoost(){Event(TelemetryEventType.AerialRoute,"AerialBoost");}
        void MiniTurbo(MiniTurboStage stage){Event(TelemetryEventType.DriftRelease,"MiniTurbo",0,(int)stage);}
        void Recovery(string reason){Event(TelemetryEventType.Recovery,"Recovery");}
        void Collision(bool wall,float severity){Event(TelemetryEventType.Collision,"Collision",severity,wall?1:0);}
        void ItemUsed(RaceItemType item){Event(TelemetryEventType.ItemUsed,"ItemUsed",0,(int)item);}
        void ItemHit(){Event(TelemetryEventType.ItemHit,"ItemHit");}
        void Checkpoint(RacerProgress racer,int index,float raceTime)
        {
            if(!recording||racer.Vehicle!=physics)return;
            if(index!=0&&index!=2&&index!=4)return;
            if(lastSector>=0&&raceTime>lastSector)sectors.Add(raceTime-lastSector);
            lastSector=raceTime;
        }
    }
}
