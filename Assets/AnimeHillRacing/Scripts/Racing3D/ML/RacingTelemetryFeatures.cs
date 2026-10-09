using UnityEngine;

namespace AnimeHillRacing.Racing3D.ML
{
    public static class RacingTelemetryFeatures
    {
        public const int Count=33;
        public static TelemetrySample Capture(RacingVehiclePhysics car, TrackProgressTracker tracker, float timestamp)
        {
            var cmd=car.LastCommands;var angles=car.transform.eulerAngles;
            var velocity=car.Body!=null?car.Body.linearVelocity:Vector3.zero;
            var sample=new TelemetrySample {
                timestamp=timestamp,worldPosition=car.transform.position,velocity=velocity,speedMps=car.SpeedMps,speedKmh=car.SpeedKmh,
                steering=cmd.Steering,throttle=cmd.Throttle,brake=cmd.Brake,isDrifting=car.IsDrifting,isJumping=cmd.Jump,
                jumpInput=cmd.Jump,driftInput=cmd.Drift,aerialBoostInput=cmd.AerialBoost,pitchInput=cmd.Pitch,
                isAirborne=!car.IsGrounded,pitch=Mathf.DeltaAngle(0,angles.x),roll=Mathf.DeltaAngle(0,angles.z),yaw=angles.y,
                airRollInput=cmd.AirRoll,isAerialBoosting=car.IsAerialBoosting,
                localVelocity=car.transform.InverseTransformDirection(velocity),angularVelocity=car.Body!=null?car.transform.InverseTransformDirection(car.Body.angularVelocity):Vector3.zero,
                driftCharge=car.DriftCharge*RacingDriftFeedback.PurpleCharge,boostRemaining=car.BoostRemainingSeconds,
                nextJumpDistance=200,gapAhead=100,gapBehind=100,racePosition=12,
                activeItemType=(int)(car.GetComponent<ItemInventory>()?.HeldItem??RaceItemType.None)
            };
            var progress=tracker!=null?tracker.FindRacer(car):null;
            var graph=tracker!=null?tracker.TrackGraph:null;
            if(progress!=null){sample.trackProgress=graph!=null?Mathf.Repeat(progress.DistanceAlongTrack/Mathf.Max(1,graph.TotalLength),1):0;sample.trackDistance=progress.TotalDistance;sample.racePosition=progress.RacePosition;sample.lap=progress.CurrentLap;sample.checkpointProgress=(progress.LastCheckpointIndex+1f)/Mathf.Max(1,tracker.TotalCheckpointsCount);}
            if(graph!=null&&graph.NodeCount>0)
            {
                graph.ProjectPosition(car.transform.position,out _,out float distance,out sample.lateralOffset);
                var road=graph.SampleDistance(distance);
                sample.headingError=Vector3.SignedAngle(car.transform.forward,road.Forward,Vector3.up);
                sample.curvature15=Curvature(graph,road,distance,15);sample.curvature35=Curvature(graph,road,distance,35);sample.curvature70=Curvature(graph,road,distance,70);
                sample.slope=road.Forward.y;sample.bankAngle=Mathf.Asin(Mathf.Clamp(road.Right.y,-1,1))*Mathf.Rad2Deg;
                if(graph.MainJumpNodeIndex>=0)sample.nextJumpDistance=Mathf.Repeat(graph.GetNode(graph.MainJumpNodeIndex).CumulativeDistance-distance,graph.TotalLength);
                if(tracker.Leaderboard.Count>0&&progress!=null)sample.distanceToLeadRacer=Mathf.Max(0,tracker.Leaderboard[0].TotalDistance-progress.TotalDistance);
                // IReadOnlyList enumeration boxes its struct enumerator. Index the
                // cached leaderboard so recorder/ML feature ticks allocate no iterator.
                var rivals=tracker.Leaderboard;
                for(int i=0;i<rivals.Count;i++)
                {
                    var rival=rivals[i];
                    if(rival.Vehicle==car||rival.Vehicle==null||rival.Finished)continue;
                    Vector3 delta=rival.Vehicle.transform.position-car.transform.position;
                    if(delta.sqrMagnitude<625&&Mathf.Abs(delta.y)<3)sample.nearbyRacerCount++;
                    float ahead=Vector3.Dot(delta,road.Forward);
                    if(Mathf.Abs(Vector3.Dot(delta,road.Right))>road.TrackWidth*.6f||Mathf.Abs(delta.y)>3)continue;
                    if(ahead>0&&ahead<sample.gapAhead){sample.gapAhead=ahead;sample.relativeSpeedAhead=Vector3.Dot(velocity-rival.Vehicle.Body.linearVelocity,road.Forward);}
                    if(ahead<0)sample.gapBehind=Mathf.Min(sample.gapBehind,-ahead);
                }
            }
            var ai=car.GetComponent<AiVehicleController>();if(ai!=null)sample.activeShortcutId=ai.ActiveRouteIndex+1;
            return sample;
        }
        static float Curvature(TrackSplineGraph graph,TrackNode road,float distance,float horizon)
            =>Vector3.SignedAngle(road.Forward,graph.SampleDistance(distance+horizon).Forward,Vector3.up)*Mathf.Deg2Rad/horizon;
        public static void Encode(TelemetrySample s,int mmr,float[] f)
        {
            f[0]=AiSkillModel.Normalize(mmr);f[1]=Mathf.Clamp(s.speedMps/50,0,2);f[2]=s.velocity.x/50;f[3]=s.velocity.y/50;f[4]=s.velocity.z/50;
            f[5]=s.pitch/90;f[6]=s.roll/90;f[7]=s.yaw/180;f[8]=s.isAirborne?1:0;f[9]=s.lateralOffset/15;f[10]=Mathf.Clamp(s.distanceToLeadRacer/50,0,2);
            if(f.Length<Count)return;
            f[11]=Mathf.Clamp(s.localVelocity.x/50,-2,2);f[12]=Mathf.Clamp(s.localVelocity.y/50,-2,2);f[13]=Mathf.Clamp(s.localVelocity.z/50,-2,2);
            f[14]=Mathf.Clamp(s.angularVelocity.x/8,-2,2);f[15]=Mathf.Clamp(s.angularVelocity.y/8,-2,2);f[16]=Mathf.Clamp(s.angularVelocity.z/8,-2,2);
            f[17]=Mathf.Clamp(s.headingError/90,-2,2);f[18]=Mathf.Clamp(s.curvature15/.04f,-2,2);f[19]=Mathf.Clamp(s.curvature35/.04f,-2,2);f[20]=Mathf.Clamp(s.curvature70/.04f,-2,2);
            f[21]=Mathf.Clamp(s.slope,-1,1);f[22]=Mathf.Clamp(s.bankAngle/45,-2,2);f[23]=Mathf.Clamp(s.driftCharge/2.7f,0,2);f[24]=Mathf.Clamp(s.boostRemaining/5,0,2);
            f[25]=Mathf.Clamp01(s.nextJumpDistance/200);f[26]=Mathf.Clamp01(s.checkpointProgress);f[27]=Mathf.Clamp01((s.racePosition-1)/11f);
            f[28]=Mathf.Clamp(s.gapAhead/50,0,2);f[29]=Mathf.Clamp(s.gapBehind/50,0,2);f[30]=Mathf.Clamp(s.relativeSpeedAhead/30,-2,2);f[31]=Mathf.Clamp01(RaceItemUtility.LegacyFeatureFamily(s.activeItemType)/6f);f[32]=Mathf.Clamp01(s.nearbyRacerCount/11f);
        }
    }
}
