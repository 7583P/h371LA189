using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class AiVehicleController : MonoBehaviour, IVehicleCommandProvider
    {
        [SerializeField] private TrackSplineGraph trackGraph;
        [SerializeField] private TrackProgressTracker progressTracker;
        private RacingVehiclePhysics vehicle;
        private ItemInventory inventory;
        private AiRacerProfile profile;
        private AiDecisionLogger logger;
        private AiItemAgent itemAgent;
        private VehicleCommands commands;
        private Vector3 target;
        private float nextDecision, targetSpeed, steering, lane, targetLane, launchTime = -1;
        private float angle, trackDistance;
        private int currentNode;
        private bool drift, shortcut;
        private float blockedTime, reverseUntil, nextReverse;
        public AiRacerProfile Profile => profile;
        public AiDecisionLogger Logger => logger;
        public Vector3 CurrentTarget => target;
        public float DecisionHz => 10f;
        public bool ExecutingShortcut => shortcut;
        private void Awake()
        {
            vehicle = GetComponent<RacingVehiclePhysics>();
            inventory = GetComponent<ItemInventory>();
            logger = new AiDecisionLogger();
        }
        public void Initialize(AiRacerProfile skill,TrackSplineGraph graph,TrackProgressTracker progress,string name="AI")
        {
            profile = skill; trackGraph = graph; progressTracker = progress;
            logger.BotName = name;
            itemAgent = new AiItemAgent(skill,inventory,vehicle,graph,logger);
            lane = ((skill.personalitySeed%997)/996f-.5f)*.22f;
            nextDecision = Time.time + (Mathf.Abs(skill.personalitySeed%11))*.009f;
            vehicle.SetCommandProvider(this);
        }
        private void FixedUpdate()
        {
            if(profile == null || trackGraph == null || !vehicle.RaceEnabled) { commands = VehicleCommands.Idle; launchTime = -1; return; }
            if(launchTime < 0) launchTime = Time.time + Mathf.Lerp(.62f,.08f,profile.reactionSkill) + (Mathf.Abs(profile.personalitySeed%17))*.005f;
            if(Time.time < launchTime) { commands = VehicleCommands.Idle; return; }
            if(Time.time >= nextDecision) { nextDecision = Time.time+.1f; Decide(); }
            lane = Mathf.MoveTowards(lane,targetLane,Time.fixedDeltaTime*.5f);
            Vector3 local = transform.InverseTransformPoint(target);
            float distance = Mathf.Max(4f,new Vector2(local.x,local.z).magnitude);
            float sine = local.x/distance;
            float desiredYaw = 2f*Mathf.Max(9f,vehicle.SpeedMps)*sine/distance;
            float desiredSteer = Mathf.Clamp(desiredYaw/(vehicle.Stats.HandlingRadPerSec*.5f),-1,1);
            if(local.z < 0) desiredSteer = Mathf.Sign(local.x);
            // Drift adds locked turn authority; counter-steer preserves the requested racing line.
            if(vehicle.IsDrifting) desiredSteer = Mathf.Clamp((desiredSteer/1.2f-Mathf.Sign(steering)*.56f)/.55f,-1,1);
            steering = Mathf.MoveTowards(steering,desiredSteer,Time.fixedDeltaTime*Mathf.Lerp(4f,10f,profile.racingLineSkill));
            float overspeed = vehicle.SpeedKmh-targetSpeed;
            commands = new VehicleCommands {
                Steering=steering, Throttle=overspeed > 4f ? 0f : Mathf.Lerp(.76f,1f,profile.throttleSkill),
                Brake=overspeed > 5f ? Mathf.Clamp01(overspeed/22f)*Mathf.Lerp(.6f,1f,profile.brakingSkill) : 0f,
                Drift=drift, Pitch=!vehicle.IsGrounded ? Mathf.Clamp(-transform.forward.y*1.2f,-.5f,.5f) : 0f
            };
            if(vehicle.IsGrounded && !vehicle.IsSpunOut && commands.Throttle>.5f && vehicle.SpeedMps<1.5f)
                blockedTime+=Time.fixedDeltaTime;
            else blockedTime=0;
            if(blockedTime>.85f && Time.time>nextReverse)
            {reverseUntil=Time.time+1.3f;nextReverse=Time.time+5;blockedTime=0;}
            if(Time.time<reverseUntil)
                commands=new VehicleCommands{Brake=1,Steering=-Mathf.Sign(Mathf.Abs(desiredSteer)>.1f?desiredSteer:1)};
        }
        private void Decide()
        {
            trackGraph.ProjectPosition(transform.position,out currentNode,out trackDistance,out float lateral);
            float lookAhead = Mathf.Clamp(7f+vehicle.SpeedMps*.48f,9f,28f);
            TrackNode current = trackGraph.SampleDistance(trackDistance);
            TrackNode future = trackGraph.SampleDistance(trackDistance+lookAhead);
            float turn = Vector3.SignedAngle(current.Forward,future.Forward,Vector3.up);
            float racingOffset = Mathf.Lerp(current.SafeLineOffset,future.OptimalLineOffset,profile.racingLineSkill);
            float natural = Mathf.Sin(Time.time*.33f+profile.personalitySeed*.01f)*(1f-profile.consistency)*.2f;
            EvaluatePack(current.TrackWidth,out float passing,out bool rear,out bool front);
            targetLane = Mathf.Clamp(passing+natural+((profile.personalitySeed%23)/22f-.5f)*.16f,-.65f,.65f);
            float laneMeters = Mathf.Clamp((racingOffset+lane)*current.TrackWidth*.4f,-current.TrackWidth*.35f,current.TrackWidth*.35f);
            target = future.Position+future.Right*laneMeters;
            angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward,Vector3.up),Vector3.ProjectOnPlane(target-transform.position,Vector3.up));
            float allowedSpeed = Mathf.Min(current.TargetSpeedKmh > 0 ? current.TargetSpeedKmh : vehicle.Stats.SpeedKmh,future.TargetSpeedKmh > 0 ? future.TargetSpeedKmh : vehicle.Stats.SpeedKmh);
            float turnRadians = Mathf.Max(.02f,Mathf.Abs(turn)*Mathf.Deg2Rad);
            float curveSpeed = lookAhead/turnRadians * vehicle.Stats.HandlingRadPerSec*.5f*.78f*3.6f;
            targetSpeed = Mathf.Min(allowedSpeed,curveSpeed,vehicle.Stats.SpeedKmh)*Mathf.Lerp(.70f,.995f,profile.racingLineSkill);
            if(Mathf.Abs(lateral)>current.TrackWidth*.30f)
            {
                targetLane=0;
                target=future.Position;
                targetSpeed=Mathf.Min(targetSpeed,65f);
            }
            if(angle > 55f) targetSpeed = Mathf.Min(targetSpeed,48f);
            drift = profile.driftSkill > .48f && Mathf.Abs(turn) > 12f && Mathf.Abs(turn) < 52f && vehicle.SpeedKmh > 55f && Mathf.Abs(lateral) < current.TrackWidth*.3f;
            shortcut = false;
            var nearby = trackGraph.FindShortcutNear(currentNode);
            if(nearby != null && nearby.IsValid() && profile.shortcutKnowledge >= nearby.minSkillRequired &&
                profile.riskTolerance >= nearby.riskLevel && (!nearby.requiresBoost || vehicle.IsBoosting))
            {
                // A route can be entered only ahead of its authored gate, never by steering backwards.
                if(currentNode >= nearby.entryNodeIndex && currentNode < nearby.exitNodeIndex)
                {
                    for(int i=1;i<nearby.pathPoints.Length;i++)
                    {
                        Vector3 localPoint = transform.InverseTransformPoint(nearby.pathPoints[i]);
                        if(localPoint.z > 3f) { target=nearby.pathPoints[i]; shortcut=true; break; }
                    }
                }
            }
            if(itemAgent != null && inventory.HasItem && itemAgent.ShouldUseItem(currentNode,angle,nearby != null,rear,front,vehicle.SpeedKmh)) inventory.UseItem();
            logger.UpdateSteeringDecision(shortcut?"CanyonShortcut":drift?"ChargeDrift":passing != 0?"Overtake":"RacingLine","TrackAndTraffic",target,steering,commands.Throttle,drift);
        }
        private void EvaluatePack(float width,out float passing,out bool rear,out bool front)
        {
            passing = 0; rear = false; front = false;
            if(progressTracker == null) return;
            float leftCost=0,rightCost=0;
            bool blocked = false;
            foreach(var racer in progressTracker.Leaderboard)
            {
                if(racer.Vehicle == vehicle || racer.Vehicle == null || racer.Finished) continue;
                Vector3 local = transform.InverseTransformPoint(racer.Vehicle.transform.position);
                if(Mathf.Abs(local.x) > width*.7f) continue;
                if(local.z < -2f && local.z > -14f && Mathf.Abs(local.x) < 2.4f) rear = true;
                if(local.z > 3f && local.z < 35f && Mathf.Abs(local.x) < 2.8f) front = true;
                if(local.z < -4f || local.z > 22f) continue;
                float cost = 1f/Mathf.Max(3f,Mathf.Abs(local.z));
                if(local.x < -.4f) leftCost += cost; else if(local.x > .4f) rightCost += cost;
                if(local.z > 2f && local.z < 18f && Mathf.Abs(local.x) < 2.3f && vehicle.SpeedMps-racer.Vehicle.SpeedMps > -.5f) blocked = true;
            }
            if(blocked) passing = (leftCost < rightCost || (Mathf.Approximately(leftCost,rightCost) && (profile.personalitySeed&1)==0) ? -1 : 1)*Mathf.Lerp(.24f,.46f,profile.overtakingSkill);
        }
        public VehicleCommands GetCommands() => commands;
    }
}
