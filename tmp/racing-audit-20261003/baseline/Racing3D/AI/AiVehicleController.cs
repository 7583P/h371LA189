using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    using Unity.Profiling;
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class AiVehicleController : MonoBehaviour, IVehicleCommandProvider
    {
        static readonly ProfilerMarker DecisionMarker=new ProfilerMarker("Racing.AI.Decide");
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
        private float lookAheadDistance, trackCurvature, lateralOffset, racingLineOffset;
        private int targetNode;
        private int currentNode;
        private bool drift, shortcut;
        private bool jumpTriggered, trickTriggered;
        private float nextHopTime;
        private float blockedTime, reverseUntil, nextReverse;
        public AiRacerProfile Profile => profile;
        public AiDecisionLogger Logger => logger;
        public Vector3 CurrentTarget => target;
        public float DecisionHz => 10f;
        public bool ExecutingShortcut => shortcut;
        public int CurrentNodeIndex => currentNode;
        public int TargetNodeIndex => targetNode;
        public float LookAheadDistance => lookAheadDistance;
        public float TrackCurvature => trackCurvature;
        public float TargetSpeedKmh => targetSpeed;
        public float LateralOffset => lateralOffset;
        public float RacingLineOffset => racingLineOffset;
        public float UpcomingTurnDegrees => angle;
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
            if(Time.time >= nextDecision) { nextDecision = Time.time+.1f; using(DecisionMarker.Auto()) Decide(); }
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

            // In-Air Stunt / Trick mechanic: triggers 360 roll & earns Landing Boost upon landing
            if(!vehicle.IsGrounded && vehicle.AirborneTime > 0.14f && !vehicle.IsPerformingTrick && profile.racingLineSkill > 0.30f)
            {
                trickTriggered = true;
            }

            // Hop mechanics: hop cooldown tracking
            if(vehicle.IsGrounded && !vehicle.IsDrifting && drift && profile.driftSkill > 0.65f && Time.time > nextHopTime)
            {
                nextHopTime = Time.time + 1.2f;
            }

            float overspeed = vehicle.SpeedKmh - targetSpeed;
            float throttle = vehicle.IsDrifting ? 1f : (overspeed > 8f ? Mathf.Clamp01(1f - (overspeed - 8f) / 18f) : Mathf.Lerp(.85f, 1f, profile.throttleSkill));
            float brake = !vehicle.IsDrifting && overspeed > 14f ? Mathf.Clamp01((overspeed - 14f) / 22f) * Mathf.Lerp(.5f, 1f, profile.brakingSkill) : 0f;

            commands = new VehicleCommands {
                Steering = steering,
                Throttle = throttle,
                Brake = brake,
                Drift = drift,
                Jump = jumpTriggered,
                Trick = trickTriggered,
                Pitch = !vehicle.IsGrounded ? Mathf.Clamp(-transform.forward.y*1.2f,-.5f,.5f) : 0f
            };
            jumpTriggered = false;
            trickTriggered = false;

            if(vehicle.IsGrounded && !vehicle.IsSpunOut && (commands.Throttle > .3f || commands.Brake > .3f) && vehicle.SpeedMps < 1.2f)
            {
                blockedTime += Time.fixedDeltaTime;
                if(blockedTime > 2.5f)
                {
                    vehicle.GetComponent<VehicleRecovery>()?.TriggerRecovery("AI stuck blocked");
                    blockedTime = 0;
                }
            }
            else blockedTime = 0;

            if(blockedTime > .75f && Time.time > nextReverse)
            {
                reverseUntil = Time.time + 1.1f;
                nextReverse = Time.time + 3.5f;
            }
            if(Time.time < reverseUntil)
                commands = new VehicleCommands{Brake = 1, Steering = -Mathf.Sign(Mathf.Abs(desiredSteer) > .1f ? desiredSteer : 1)};
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
            lookAheadDistance = lookAhead;
            trackCurvature = Mathf.Abs(turn)*Mathf.Deg2Rad/lookAhead;
            lateralOffset = lateral;
            racingLineOffset = laneMeters;
            trackGraph.ProjectPosition(future.Position,out targetNode,out _,out _);
            angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward,Vector3.up),Vector3.ProjectOnPlane(target-transform.position,Vector3.up));
            float allowedSpeed = Mathf.Min(current.TargetSpeedKmh > 0 ? current.TargetSpeedKmh : vehicle.Stats.SpeedKmh,future.TargetSpeedKmh > 0 ? future.TargetSpeedKmh : vehicle.Stats.SpeedKmh);
            float turnRadians = Mathf.Max(.02f,Mathf.Abs(turn)*Mathf.Deg2Rad);

            // Apex speed retention: high skill racers carry higher apex speed while respecting track grip
            float corneringSkillMultiplier = Mathf.Lerp(0.88f, 1.18f, profile.racingLineSkill);
            float driftHandlingBonus = vehicle.IsDrifting ? 1.15f : 1.0f;
            float curveSpeed = (lookAhead / turnRadians) * vehicle.Stats.HandlingRadPerSec * 0.5f * corneringSkillMultiplier * driftHandlingBonus * 3.6f;
            targetSpeed = Mathf.Min(allowedSpeed, curveSpeed, vehicle.Stats.SpeedKmh) * Mathf.Lerp(0.82f, 1.0f, profile.racingLineSkill);

            if(Mathf.Abs(lateral) > current.TrackWidth * .30f)
            {
                targetLane = 0;
                target = future.Position;
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Lerp(70f, 85f, profile.recoverySkill));
            }
            if(angle > 55f)
            {
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Lerp(58f, 72f, profile.racingLineSkill));
            }

            // Mechanic Chaining: Drift -> MiniTurbo -> Release on corner exit or shortcut entry
            if(vehicle.IsDrifting)
            {
                if(vehicle.CurrentMiniTurboStage >= MiniTurboStage.MiniTurbo)
                {
                    // If corner is straightening or shortcut is entered, release MiniTurbo to slingshot
                    if(Mathf.Abs(turn) < 16f || shortcut)
                    {
                        drift = false;
                    }
                }
            }
            else
            {
                // Drift on curves between 10 and 68 degrees
                drift = profile.driftSkill > .35f && Mathf.Abs(turn) > 10f && Mathf.Abs(turn) < 68f && vehicle.SpeedKmh > 42f && Mathf.Abs(lateral) < current.TrackWidth * .30f;
            }

            // Boost-Required Shortcut Assessment & Intelligent Mechanic Synergy
            shortcut = false;
            var nearby = trackGraph.FindShortcutNear(currentNode);
            if(nearby != null && nearby.IsValid() && profile.shortcutKnowledge >= nearby.minSkillRequired && profile.riskTolerance >= nearby.riskLevel)
            {
                bool hasBoostResource = vehicle.IsBoosting;

                // Tactical Boost: If shortcut requires boost and bot is not boosting, use boost item right before entry!
                if(!hasBoostResource && nearby.requiresBoost && inventory != null && inventory.HasItem)
                {
                    if(inventory.HeldItem == RaceItemType.TurboPulse || inventory.HeldItem == RaceItemType.HyperCharge)
                    {
                        inventory.UseItem();
                        hasBoostResource = true;
                    }
                }

                // If bot is drifting and has MiniTurbo charged, release it to boost into shortcut!
                if(!hasBoostResource && nearby.requiresBoost && vehicle.IsDrifting && vehicle.CurrentMiniTurboStage != MiniTurboStage.None)
                {
                    drift = false;
                    hasBoostResource = true;
                }

                if(!nearby.requiresBoost || hasBoostResource)
                {
                    if(currentNode >= nearby.entryNodeIndex - 2 && currentNode < nearby.exitNodeIndex)
                    {
                        if(currentNode < nearby.entryNodeIndex)
                        {
                            target = nearby.pathPoints[0];
                            shortcut = true;
                        }
                        else
                        {
                            Vector3 chosenPoint = nearby.pathPoints[nearby.pathPoints.Length - 1];
                            for(int i = 1; i < nearby.pathPoints.Length; i++)
                            {
                                Vector3 localPoint = transform.InverseTransformPoint(nearby.pathPoints[i]);
                                if(localPoint.z > 5f) { chosenPoint = nearby.pathPoints[i]; break; }
                            }
                            target = chosenPoint;
                            shortcut = true;
                        }
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
