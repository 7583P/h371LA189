using UnityEngine;
using Unity.Profiling;
namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-20), RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class AiVehicleController : MonoBehaviour, IVehicleCommandProvider
    {
        static readonly ProfilerMarker DecisionMarker = new ProfilerMarker("Racing.AI.Decide");
        [SerializeField] TrackSplineGraph trackGraph;
        [SerializeField] TrackProgressTracker progressTracker;
        RacingVehiclePhysics vehicle; VehicleRecovery recovery; ItemInventory inventory; RacingItemPool itemPool;
        RacerProgress raceProgress; AiRacerProfile profile; AiDecisionLogger logger; AiItemAgent itemAgent; DeterministicRng rng;
        VehicleCommands commands; Vector3 target;
        float nextDecision, targetSpeed, steering, lane, targetLane, launchTime = -1;
        float angle, trackDistance, lookAheadDistance, trackCurvature, lateralOffset, racingLineOffset;
        float blockedTime, reverseUntil, nextReverse, laneHoldUntil, mistakeUntil, mistakeOffset, routeEntryDistance, nextChainHop, driftStarted;
        int targetNode, currentNode, routeSegment, recoverySeen, routeConsideredLap = -1, routeAttemptLap=-1,routeAttemptMask;
        TrackShortcut activeRoute, consideredRoute;
        bool drift, shortcut, routeEntered, reserveBoost, routeSelected, routeHopTaken;
        string intent = "RacingLine";
        float routeProgressAt;
        float rejoinDistance=-1,rejoinStableTime;
        int routeIndex=-1;
        int[] routeEntries,routeCompletions;
        readonly float[] laneCosts = new float[3];
        static readonly float[] LaneOptions = { 0, -.40f, .40f };
        public AiRacerProfile Profile => profile;
        public AiDecisionLogger Logger => logger;
        public Vector3 CurrentTarget => target;
        public float DecisionHz => profile == null ? 10 : Mathf.Lerp(10, 16, profile.awareness);
        public bool ExecutingShortcut => shortcut;
        public int CurrentNodeIndex => currentNode;
        public int TargetNodeIndex => targetNode;
        public float LookAheadDistance => lookAheadDistance;
        public float TrackCurvature => trackCurvature;
        public float TargetSpeedKmh => targetSpeed;
        public float LateralOffset => lateralOffset;
        public float RacingLineOffset => racingLineOffset;
        public float UpcomingTurnDegrees => angle;
        public int ShortcutEntries { get; private set; }
        public int ShortcutCompletions { get; private set; }
        public int PassingDecisions { get; private set; }
        public int DefensiveDecisions { get; private set; }
        public int TrafficBrakingDecisions { get; private set; }
        public int BoostsForShortcuts { get; private set; }
        public bool ReservingBoost => reserveBoost;
        public int ActiveRouteIndex=>routeIndex;
        public int RouteSegment=>routeSegment;
        public int FailedShortcuts { get; private set; }
        public string LastRouteAbortReason { get; private set; }
        public int RouteEntries(int index)=>routeEntries!=null&&index>=0&&index<routeEntries.Length?routeEntries[index]:0;
        public int RouteCompletions(int index)=>routeCompletions!=null&&index>=0&&index<routeCompletions.Length?routeCompletions[index]:0;
        void Awake()
        {
            vehicle = GetComponent<RacingVehiclePhysics>(); recovery = GetComponent<VehicleRecovery>();
            inventory = GetComponent<ItemInventory>(); logger = new AiDecisionLogger();
        }
        public void Initialize(AiRacerProfile skill, TrackSplineGraph graph, TrackProgressTracker progress, string name = "AI")
        {
            profile = skill; trackGraph = graph; progressTracker = progress; logger.BotName = name;
            rng = new DeterministicRng(skill.personalitySeed ^ 71829);
            itemAgent = new AiItemAgent(skill, inventory, vehicle, graph, logger); itemPool = RacingItemPool.ForScene();
            lane = Mathf.Abs(skill.personalitySeed % 997) / 996f * .22f - .11f;
            nextDecision = Time.time + Mathf.Abs(skill.personalitySeed % 11) * .009f; vehicle.SetCommandProvider(this);
            routeEntries=new int[graph.Shortcuts.Length];routeCompletions=new int[graph.Shortcuts.Length];
        }
        void FixedUpdate()
        {
            if (profile == null || trackGraph == null || !vehicle.RaceEnabled) { commands = VehicleCommands.Idle; launchTime = -1; return; }
            if (recovery != null && recovery.IsRecovering) { commands = VehicleCommands.Idle; return; }
            if (recovery != null && recovery.RecoveryCount != recoverySeen)
            { recoverySeen = recovery.RecoveryCount; AbortRoute("Shared recovery: "+recovery.LastRecoveryReason); rejoinDistance=-1; shortcut = drift = false; steering = blockedTime = reverseUntil = nextDecision = 0; }
            if (launchTime < 0) launchTime = Time.time + Mathf.Lerp(.65f, .06f, profile.reactionSkill) + Mathf.Abs(profile.personalitySeed % 17) * .003f;
            if (Time.time < launchTime) { commands = VehicleCommands.Idle; return; }
            if (Time.time >= nextDecision) { nextDecision = Time.time + 1f / DecisionHz; using (DecisionMarker.Auto()) Decide(); }
            lane = Mathf.MoveTowards(lane, targetLane, Time.fixedDeltaTime * Mathf.Lerp(.22f, .65f, profile.awareness));
            Vector3 deltaToTarget = Vector3.ProjectOnPlane(target-transform.position,Vector3.up);
            Vector3 heading = vehicle.SpeedMps>5 ? Vector3.ProjectOnPlane(vehicle.Body.linearVelocity,Vector3.up).normalized : Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
            float localX=Vector3.Dot(deltaToTarget,Vector3.Cross(Vector3.up,heading)),localZ=Vector3.Dot(deltaToTarget,heading);
            float distance = Mathf.Max(4, deltaToTarget.magnitude);
            float desiredYaw = 2 * Mathf.Max(7, vehicle.SpeedMps) * localX / (distance * distance);
            float desiredSlip=vehicle.IsDrifting ? -vehicle.DriftDirection*Mathf.Lerp(6,13,profile.driftSkill) : 0;
            desiredYaw += (vehicle.SlipAngle-desiredSlip)*Mathf.Deg2Rad*.65f;
            desiredYaw += (desiredYaw-Vector3.Dot(vehicle.Body.angularVelocity,Vector3.up))*.30f;
            float desiredSteer = Mathf.Clamp(desiredYaw / (vehicle.Stats.HandlingRadPerSec * .5f), -1, 1);
            if (localZ < 0) desiredSteer = Mathf.Sign(localX);
            if (vehicle.IsDrifting)
            {
                float authority = Mathf.Lerp(1, 1.3f, Mathf.Clamp01(vehicle.Stats.DriftMultiplier - .5f));
                desiredSteer = Mathf.Clamp((desiredSteer / authority - vehicle.DriftDirection * .56f) / .55f, -1, 1);
            }
            steering = Mathf.MoveTowards(steering, desiredSteer, Time.fixedDeltaTime * Mathf.Lerp(3.8f, 10, profile.racingLineSkill));
            float overspeed = vehicle.SpeedKmh - targetSpeed;
            // Elite execution uses the same full-throttle command available to a human.
            float throttle = (profile.throttleSkill >= .85f ? 1 : Mathf.Lerp(.65f, 1, profile.throttleSkill)) * Mathf.Clamp01(1 - Mathf.Max(0, overspeed) / 7);
            float brake = Mathf.Clamp01((overspeed - 2) / 8) * Mathf.Lerp(.55f, 1, profile.brakingSkill);
            if (vehicle.IsDrifting && (brake > .75f || Mathf.Abs(vehicle.SlipAngle)>32 || Mathf.Abs(lateralOffset) > trackGraph.GetNode(currentNode).TrackWidth * .40f)) drift = false;
            bool trick = !vehicle.IsGrounded && profile.trickSkill > .22f &&
                vehicle.AirborneTime > Mathf.Lerp(.35f, .16f, profile.trickSkill) &&
                vehicle.TrickCount < (profile.trickSkill > .88f && vehicle.AirborneTime > .67f ? 2 : 1);
            commands = new VehicleCommands { Steering = steering, Throttle = throttle, Brake = brake, Drift = drift, Trick = trick,
                Pitch = !vehicle.IsGrounded ? Mathf.Clamp(-transform.forward.y * 1.4f, -.5f, .5f) : 0 };
            if(activeRoute!=null && routeEntered && !activeRoute.requiresJump && !routeHopTaken &&
                profile.chainingSkill>.68f && routeSegment>=9 && routeSegment<=11 && vehicle.IsGrounded &&
                !vehicle.IsSpunOut && vehicle.SpeedKmh>55 && angle<8 && Mathf.Abs(vehicle.SlipAngle)<6)
            {commands.Jump=true;routeHopTaken=true;intent="ExitTurboToHopTrickLanding";}
            // A released drift can be converted into a hop/trick/landing boost on a
            // clear, straight exit. Cooldown and landing room prevent bunny-hop spam.
            if (activeRoute == null && profile.chainingSkill > .85f && Time.time > nextChainHop &&
                vehicle.IsBoosting && !vehicle.IsDrifting && vehicle.IsGrounded && !vehicle.IsSpunOut &&
                vehicle.SpeedKmh > 72 && angle < 5 && trackCurvature < .004f &&
                Mathf.Abs(lateralOffset) < trackGraph.GetNode(currentNode).TrackWidth * .22f && NextGateDistance() > 24)
            { commands.Jump = true; nextChainHop = Time.time + 5.5f; intent = "DriftBoostHopTrickLanding"; }
            if (vehicle.IsGrounded && !vehicle.IsSpunOut && commands.Throttle > .3f && vehicle.SpeedMps < 1.2f)
            {
                blockedTime += Time.fixedDeltaTime;
                if (blockedTime > .85f && Time.time > nextReverse) { reverseUntil = Time.time + .8f; nextReverse = Time.time + 3; }
                if (blockedTime > Mathf.Lerp(3.4f, 2.6f, profile.recoverySkill)) { recovery?.TriggerRecovery("AI blocked after reverse attempt"); blockedTime = 0; }
            }
            else blockedTime = 0;
            if (Time.time < reverseUntil) commands = new VehicleCommands { Brake = 1, Steering = -Mathf.Sign(Mathf.Abs(desiredSteer) > .1f ? desiredSteer : 1) };
        }
        void Decide()
        {
            trackGraph.ProjectPosition(transform.position, out currentNode, out trackDistance, out float lateral);
            if (raceProgress == null) raceProgress = progressTracker?.FindRacer(vehicle);
            if(raceProgress!=null && routeAttemptLap!=raceProgress.CurrentLap){routeAttemptLap=raceProgress.CurrentLap;routeAttemptMask=0;}
            var current = trackGraph.SampleDistance(trackDistance);
            lookAheadDistance = Mathf.Clamp(5 + vehicle.SpeedMps * Mathf.Lerp(.36f, .38f, profile.racingLineSkill), 9, 25);
            var future = trackGraph.SampleDistance(trackDistance + lookAheadDistance);
            float turn = Vector3.SignedAngle(current.Forward, future.Forward, Vector3.up);
            trackCurvature = Mathf.Abs(turn) * Mathf.Deg2Rad / lookAheadDistance; lateralOffset = lateral;
            targetSpeed = BrakingEnvelope(current);
            float entryTurn = Vector3.SignedAngle(current.Forward, trackGraph.SampleDistance(trackDistance + 7).Forward, Vector3.up);
            float racingOffset = Mathf.Lerp(current.SafeLineOffset, future.OptimalLineOffset, profile.racingLineSkill);
            if (Mathf.Abs(entryTurn) < 4 && Mathf.Abs(turn) > 10 && profile.racingLineSkill > .65f) racingOffset = -Mathf.Sign(turn) * .18f;
            if (Time.time >= mistakeUntil && rng.NextFloat() < Mathf.Lerp(.015f, .00015f, profile.consistency))
            { mistakeUntil = Time.time + Mathf.Lerp(1.6f, .3f, profile.consistency); mistakeOffset = rng.Range(-.20f, .20f) * (1 - profile.consistency); }
            float natural = Mathf.Sin(Time.time * .33f + profile.personalitySeed * .01f) * (1 - profile.consistency) * .16f;
            if (Time.time < mistakeUntil) natural += mistakeOffset;
            intent = "RacingLine";
            var traffic = EvaluatePack(current, racingOffset, turn);
            racingLineOffset = Mathf.Clamp((racingOffset + lane + natural) * current.TrackWidth * .4f, -current.TrackWidth * .34f, current.TrackWidth * .34f);
            target = future.Position + future.Right * racingLineOffset;
            if(NextGateDistance()<28)target=future.Position+future.Right*racingLineOffset*.35f;
            trackGraph.ProjectPosition(future.Position, out targetNode, out _, out _);
            shortcut = false; PlanRoute();
            if (activeRoute != null) FollowRoute();
            else if(rejoinDistance>=0)FollowRejoin();
            else if (Mathf.Abs(lateral) > current.TrackWidth * .34f)
            { targetLane = 0; target = future.Position; targetSpeed = Mathf.Min(targetSpeed, 78); intent = "RecoverLine"; }
            if(activeRoute==null && rejoinDistance<0 && trackGraph.MainJumpNodeIndex>=0)
            {
                var jumpNode=trackGraph.GetNode(trackGraph.MainJumpNodeIndex);
                float toJump=Mathf.Repeat(jumpNode.CumulativeDistance-trackDistance,trackGraph.TotalLength);
                if(toJump<35)
                {
                    Vector3 landing=trackGraph.SampleDistance(jumpNode.CumulativeDistance+Mathf.Clamp(vehicle.SpeedMps*1.05f,24,40)).Position;
                    target=jumpNode.Position+(landing-jumpNode.Position).normalized*10;
                    targetLane=0;targetSpeed=Mathf.Min(targetSpeed,110);drift=false;intent="PrepareJumpAndLanding";
                }
            }
            angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.ProjectOnPlane(target - transform.position, Vector3.up));
            if (angle > 45) targetSpeed = Mathf.Min(targetSpeed, Mathf.Lerp(32, 42, profile.recoverySkill));
            if (vehicle.IsDrifting && activeRoute==null)
            {
                bool exitReady = Mathf.Abs(turn) < 3f || (Mathf.Sign(turn) != vehicle.DriftDirection && Mathf.Abs(turn) > 4);
                bool ultraReady = vehicle.CurrentMiniTurboStage == MiniTurboStage.UltraMiniTurbo && Mathf.Abs(turn) < 10;
                if ((Time.time - driftStarted > .4f && (exitReady || ultraReady)) || shortcut || angle > 46 || Mathf.Abs(lateral) > current.TrackWidth * .38f) drift = false;
            }
            else if(activeRoute==null)
            {
                drift = intent!="PrepareJumpAndLanding" && activeRoute == null && rejoinDistance<0 && profile.driftSkill > .32f &&
                    Mathf.Abs(turn) > Mathf.Lerp(14, 6.5f, profile.driftSkill) && Mathf.Abs(turn) < 58 &&
                    vehicle.SpeedKmh > 43 && vehicle.SpeedKmh < targetSpeed + 7 &&
                    Mathf.Abs(lateral) < current.TrackWidth * .30f && !vehicle.IsSpunOut;
                if (drift) driftStarted = Time.time;
            }
            if (drift) intent = "DriftToExitBoost";
            if (reserveBoost && activeRoute==null && vehicle.IsDrifting && vehicle.CurrentMiniTurboStage != MiniTurboStage.None && routeEntryDistance < 18) drift = false;
            traffic.reserveBoost = reserveBoost;
            traffic.shortcutEntryReady = activeRoute != null && activeRoute.requiresBoost && angle < 22 &&
                ((!activeRoute.requiresJump && !routeEntered && routeEntryDistance < 12) ||
                 (activeRoute.requiresJump && routeEntered && routeSegment>=8 && routeSegment<=activeRoute.launchNodeIndex &&
                  Vector3.Distance(transform.position,activeRoute.pathPoints[activeRoute.launchNodeIndex])<Mathf.Max(9,vehicle.SpeedMps*.35f) &&
                  LaunchAngleError(activeRoute)<7 && vehicle.BoostRemainingSeconds<.6f));
            traffic.turnDegrees = shortcut?angle:Mathf.Max(Mathf.Abs(turn), angle);
            traffic.finalLap = raceProgress != null && raceProgress.CurrentLap == progressTracker.TotalLaps;
            traffic.position = raceProgress != null ? raceProgress.RacePosition : 12;
            if (inventory != null && inventory.HasItem && itemAgent.ShouldUseItem(traffic))
            {bool boost=inventory.HeldItem==RaceItemType.TurboPulse || inventory.HeldItem==RaceItemType.HyperCharge;if(inventory.UseItem()&&boost&&traffic.shortcutEntryReady)BoostsForShortcuts++;}
            logger.UpdateSteeringDecision(intent, reserveBoost ? "PreserveShortcutResource" : "TrackAndPredictedTraffic", target, steering, commands.Throttle, drift);
        }
        float BrakingEnvelope(TrackNode current)
        {
            float efficiency = Mathf.Lerp(.70f, .995f, profile.racingLineSkill), maxSpeed = vehicle.Stats.SpeedKmh * (vehicle.IsBoosting ? vehicle.BoostMultiplier : 1);
            float speed = maxSpeed * efficiency;
            float deceleration = vehicle.Stats.AccelerationMps2 * 2.4f * Mathf.Lerp(.55f, .94f, profile.brakingSkill);
            float horizon = Mathf.Clamp(vehicle.SpeedMps * vehicle.SpeedMps / (2 * deceleration) + vehicle.SpeedMps * .35f, 32, 145);
            var previous = current;
            for (int i = 1; i <= 10; i++)
            {
                float ahead = horizon * i / 10; var sample = trackGraph.SampleDistance(trackDistance + ahead);
                float curvature = Mathf.Abs(Vector3.SignedAngle(previous.Forward, sample.Forward, Vector3.up)) * Mathf.Deg2Rad / (horizon / 10);
                // Handling/2 is the shared physics' full steering yaw authority.
                float corner = curvature > .001f ? vehicle.Stats.HandlingRadPerSec * Mathf.Lerp(.33f, .46f, profile.racingLineSkill) / curvature : maxSpeed / 3.6f;
                float authored = sample.TargetSpeedKmh > 0 ? Mathf.Lerp(sample.TargetSpeedKmh, maxSpeed, .78f * profile.racingLineSkill) / 3.6f : maxSpeed / 3.6f;
                float limit = Mathf.Min(corner, authored) * efficiency;
                speed = Mathf.Min(speed, Mathf.Sqrt(limit * limit + 2 * deceleration * Mathf.Max(0, ahead - vehicle.SpeedMps * .15f)) * 3.6f); previous = sample;
            }
            return speed;
        }
        AiItemContext EvaluatePack(TrackNode road, float racingOffset, float turn)
        {
            var context = new AiItemContext(); if (progressTracker == null) return context;
            float baseLine = racingOffset * road.TrackWidth * .4f;
            for (int i = 0; i < 3; i++) laneCosts[i] = i == 0 ? 0 : .05f;
            bool blocked = false, closingRear = false; float leadSpeed = float.MaxValue, leadGap = float.MaxValue;
            var leaderboard = progressTracker.Leaderboard;
            for (int racerIndex = 0; racerIndex < leaderboard.Count; racerIndex++)
            {
                var racer = leaderboard[racerIndex];
                if (racer.Vehicle == vehicle || racer.Vehicle == null || racer.Finished) continue;
                Vector3 delta = racer.Vehicle.transform.position - transform.position;
                float ahead = Vector3.Dot(delta, road.Forward), side = Vector3.Dot(delta, road.Right);
                if (Mathf.Abs(delta.y) > 3 || Mathf.Abs(side) > road.TrackWidth * .7f || ahead < -18 || ahead > 45) continue;
                float closing = vehicle.ForwardSpeedMps - Vector3.Dot(racer.Vehicle.Body.linearVelocity, road.Forward);
                if (ahead < -2 && ahead > -15 && Mathf.Abs(side) < 2.8f) { context.racerBehind = true; closingRear |= closing < -1.2f; }
                if (ahead > 2 && ahead < 40 && Mathf.Abs(side) < 3) context.targetAhead = true;
                if (delta.sqrMagnitude < 64) context.rivalsInPulseRadius++;
                float prediction = Mathf.Lerp(.15f, .7f, profile.awareness);
                float predictedZ = ahead - closing * prediction, predictedX = lateralOffset + side + Vector3.Dot(racer.Vehicle.Body.linearVelocity, road.Right) * prediction;
                if (predictedZ < -4 || predictedZ > 24) continue;
                for (int i = 0; i < 3; i++)
                {
                    float separation = Mathf.Abs(predictedX - (baseLine + LaneOptions[i] * road.TrackWidth * .4f));
                    if (separation < 2.5f) laneCosts[i] += (2.5f - separation) / Mathf.Max(3, Mathf.Abs(predictedZ));
                }
                if (ahead > 1 && ahead < Mathf.Clamp(vehicle.SpeedMps * .8f, 10, 27) && Mathf.Abs(side) < 2.1f && closing > -.4f)
                { blocked = true; if (ahead < leadGap) { leadGap = ahead; leadSpeed = racer.Vehicle.SpeedKmh; } }
            }
            if (blocked)
            {
                int best = laneCosts[1] < laneCosts[2] || (Mathf.Approximately(laneCosts[1], laneCosts[2]) && (profile.personalitySeed & 1) == 0) ? 1 : 2;
                int held = targetLane < 0 ? 1 : 2;
                if (Time.time < laneHoldUntil && laneCosts[held] <= laneCosts[best] + .16f) best = held;
                float laneCostTolerance = Mathf.Lerp(.015f, .06f, profile.aggression);
                if (profile.overtakingSkill > .25f && laneCosts[best] < laneCosts[0] + laneCostTolerance && Mathf.Abs(turn) < 32)
                {
                    if (Time.time >= laneHoldUntil) { PassingDecisions++; laneHoldUntil = Time.time + Mathf.Lerp(1.8f, 1.1f, profile.overtakingSkill); }
                    targetLane = LaneOptions[best] * Mathf.Lerp(.65f, 1, profile.overtakingSkill) * Mathf.Lerp(.8f, 1.06f, profile.aggression); intent = "CommittedPass";
                }
                if (leadGap < Mathf.Lerp(10, 7, profile.aggression) || laneCosts[best] >= laneCosts[0] + laneCostTolerance)
                { targetSpeed = Mathf.Min(targetSpeed, Mathf.Max(25, leadSpeed - (leadGap < 4 ? 12 : 0))); TrafficBrakingDecisions++; }
            }
            else if (Time.time >= laneHoldUntil)
            {
                targetLane = (Mathf.Abs(profile.personalitySeed % 23) / 22f - .5f) * .15f;
                if (closingRear && profile.defensiveAwareness > .62f && Mathf.Abs(turn) > 8 && Mathf.Abs(turn) < 28 && laneCosts[0] < .18f)
                { targetLane = Mathf.Sign(turn) * .12f; DefensiveDecisions++; intent = "ProtectInside"; }
            }
            context.projectileThreat = profile.awareness > .4f && itemPool != null && itemPool.HasIncomingThreat(vehicle);
            return context;
        }
        void PlanRoute()
        {
            reserveBoost = false;
            if(rejoinDistance>=0)return;
            if (activeRoute != null) { reserveBoost = activeRoute.requiresBoost && (!routeEntered || (activeRoute.requiresJump?routeSegment<=activeRoute.launchNodeIndex:routeSegment<6)); return; }
            var nearby = trackGraph.FindUpcomingShortcut(trackDistance, 150, out float distance); routeEntryDistance = distance;
            if (nearby == null || profile.shortcutKnowledge < nearby.minSkillRequired || profile.riskTolerance < nearby.riskLevel) return;
            int lap = raceProgress != null ? raceProgress.CurrentLap : 1;
            if (consideredRoute != nearby || routeConsideredLap != lap)
            {
                consideredRoute = nearby; routeConsideredLap = lap;
                float probability = Mathf.Lerp(.05f, .995f, profile.shortcutExecution) * Mathf.Lerp(.6f, 1, profile.consistency);
                if (raceProgress != null && lap == progressTracker.TotalLaps && raceProgress.RacePosition > 3) probability = Mathf.Min(.98f, probability + .06f);
                routeSelected = rng.NextFloat() < probability;
            }
            if (!routeSelected) return;
            int index=System.Array.IndexOf(trackGraph.Shortcuts,nearby);
            if(index>=0 && (routeAttemptMask&(1<<index))!=0)return;
            reserveBoost = nearby.requiresBoost;
            float launchSeconds=.4f;
            if(nearby.requiresJump)
            {
                float approach=distance;
                for(int i=0;i<nearby.launchNodeIndex;i++)approach+=Vector3.Distance(nearby.pathPoints[i],nearby.pathPoints[i+1]);
                launchSeconds=approach/Mathf.Max(12,Mathf.Min(vehicle.SpeedMps,nearby.launchSpeedKmh/3.6f))+.35f;
            }
            bool resource = !nearby.requiresBoost || HasBoostItem() || vehicle.BoostRemainingSeconds > launchSeconds ||
                (!nearby.requiresJump&&vehicle.IsDrifting&&vehicle.CurrentMiniTurboStage!=MiniTurboStage.None) ||
                (nearby.BoostSupply!=null&&nearby.BoostSupply.CanCollect(vehicle));
            if(distance<65&&resource)
            {
                targetLane=0;target=trackGraph.SampleDistance(trackDistance+lookAheadDistance).Position;
                float entrySpeed=RouteSetupSpeed(nearby)/3.6f;
                targetSpeed=Mathf.Min(targetSpeed,Mathf.Sqrt(entrySpeed*entrySpeed+2*vehicle.Stats.AccelerationMps2*2.1f*Mathf.Max(0,distance-8))*3.6f);
                intent="PrepareShortcutApproach";
            }
            if (distance < 35 && resource && NextGateDistance()>distance && Mathf.Abs(lateralOffset)<trackGraph.GetNode(currentNode).TrackWidth*.27f && vehicle.IsGrounded && !vehicle.IsSpunOut)
            { activeRoute = nearby; routeIndex=index;routeProgressAt=Time.time;routeSegment = 0; routeEntered = routeHopTaken = false; }
        }
        float NextGateDistance()
        {
            var gates=trackGraph.CheckpointNodeIndices;
            if(raceProgress==null || gates.Length==0)return float.MaxValue;
            int next=(raceProgress.LastCheckpointIndex+1)%gates.Length;
            return Mathf.Repeat(trackGraph.GetNode(gates[next]).CumulativeDistance-trackDistance,trackGraph.TotalLength);
        }
        void FollowRoute()
        {
            shortcut = true; drift = false; intent = "CommittedShortcut"; var points = activeRoute.pathPoints;
            // A hit or a missed landing must not leave a bot orbiting a waypoint
            // indefinitely. Rejoin through steering; shared recovery handles falls.
            if(Time.time-routeProgressAt>(routeEntered?3.5f:5.5f))
            {AbortRoute("No forward route progress");FollowRejoin();return;}
            routeEntryDistance = Vector3.Distance(transform.position, points[0]);
            if (!routeEntered)
            {
                target = points[0];
                float entrySpeed=RouteSetupSpeed(activeRoute)/3.6f;
                targetSpeed=Mathf.Min(targetSpeed,Mathf.Sqrt(entrySpeed*entrySpeed+2*vehicle.Stats.AccelerationMps2*2.1f*Mathf.Max(0,routeEntryDistance-8))*3.6f);
                if (routeEntryDistance > Mathf.Max(7, vehicle.SpeedMps * .25f)) return;
                if (activeRoute.requiresBoost && !vehicle.IsBoosting && vehicle.CurrentMiniTurboStage==MiniTurboStage.None)
                {
                    if (inventory == null || !inventory.HasItem || (inventory.HeldItem != RaceItemType.TurboPulse && inventory.HeldItem != RaceItemType.HyperCharge))
                    if(activeRoute.BoostSupply==null||!activeRoute.BoostSupply.CanCollect(vehicle)){ AbortRoute("Entry boost unavailable");FollowRejoin();return; }
                }
                routeEntered = true; ShortcutEntries++;
                routeProgressAt=Time.time;if(routeIndex>=0)routeEntries[routeIndex]++;
                if(routeIndex>=0)routeAttemptMask|=1<<routeIndex;
            }
            // Route stays committed after boost expiry; targets always advance in order.
            Vector3 a = points[routeSegment], b = points[routeSegment + 1], delta = b - a;
            float fraction = delta.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / delta.sqrMagnitude) : 1;
            while ((fraction >= .995f || Vector3.Distance(transform.position, b) < 2) && routeSegment < points.Length - 2)
            { routeSegment++;routeProgressAt=Time.time; a = points[routeSegment]; b = points[routeSegment + 1]; delta = b - a; fraction = delta.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / delta.sqrMagnitude) : 1; }
            Vector3 right=Vector3.Cross(Vector3.up,delta).normalized;
            Vector3 projected=a+delta*fraction;
            float routeLateral=Vector3.Dot(transform.position-projected,right);
            if(Mathf.Abs(routeLateral)>activeRoute.width*.5f+2.5f || transform.position.y<projected.y-3)
            {AbortRoute("Outside landing corridor");FollowRejoin();return;}
            if(activeRoute.requiresBoost&&activeRoute.requiresJump&&routeSegment==activeRoute.launchNodeIndex-1&&!vehicle.IsBoosting&&!HasBoostItem())
            {AbortRoute("Launch boost expired");FollowRejoin();return;}
            if (routeSegment == points.Length - 2 && Vector3.Distance(transform.position, b)<8 && vehicle.IsGrounded)
            { if(routeIndex>=0)routeCompletions[routeIndex]++;activeRoute = null;routeIndex=-1; shortcut = reserveBoost = false; ShortcutCompletions++; return; }
            lookAheadDistance=Mathf.Clamp(5+vehicle.SpeedMps*.22f,8,16);
            float remaining = lookAheadDistance; target = a + delta * fraction;
            for (int s = routeSegment; s < points.Length - 1; s++)
            { Vector3 next = points[s + 1]; float length = Vector3.Distance(target, next); if (length >= remaining) { target = Vector3.MoveTowards(target, next, remaining); break; } remaining -= length; target = next; }
            Vector3 routeRight=Vector3.Cross(Vector3.up,delta).normalized;
            target+=routeRight*Mathf.Clamp(lane*3,-activeRoute.width*.08f,activeRoute.width*.08f);
            targetSpeed = vehicle.Stats.SpeedKmh * Mathf.Lerp(.68f, .995f, profile.shortcutExecution) * (vehicle.IsBoosting ? vehicle.BoostMultiplier : 1);
            float aheadDistance=Vector3.Distance(transform.position,b), deceleration=vehicle.Stats.AccelerationMps2*2.0f;
            for(int s=routeSegment;s<points.Length-2 && aheadDistance<85;s++)
            {
                Vector3 before=points[s+1]-points[s],after=points[s+2]-points[s+1];
                float curvature=Vector3.Angle(Vector3.ProjectOnPlane(before,Vector3.up),Vector3.ProjectOnPlane(after,Vector3.up))*Mathf.Deg2Rad/Mathf.Max(1,(before.magnitude+after.magnitude)*.5f);
                if(curvature>.001f)
                {
                    float corner=vehicle.Stats.HandlingRadPerSec*Mathf.Lerp(.24f,.40f,profile.shortcutExecution)/curvature;
                    targetSpeed=Mathf.Min(targetSpeed,Mathf.Sqrt(corner*corner+2*deceleration*Mathf.Max(0,aheadDistance-vehicle.SpeedMps*.15f))*3.6f);
                }
                if(activeRoute.shortcutId=="crest_cut"&&s>=activeRoute.landingNodeIndex)
                {
                    // A supported exit crest also needs braking. At racing speed
                    // its falling grade can lift the wheels before the merge,
                    // even after a perfectly aligned first landing.
                    float beforeRun=Vector3.ProjectOnPlane(before,Vector3.up).magnitude;
                    float afterRun=Vector3.ProjectOnPlane(after,Vector3.up).magnitude;
                    float crest=Mathf.Max(0,Mathf.Atan2(before.y,beforeRun)-Mathf.Atan2(after.y,afterRun))/Mathf.Max(1,(beforeRun+afterRun)*.5f);
                    if(crest>.002f)
                    {
                        float supportedSpeed=Mathf.Sqrt(6.5f/crest);
                        targetSpeed=Mathf.Min(targetSpeed,Mathf.Sqrt(supportedSpeed*supportedSpeed+2*deceleration*Mathf.Max(0,aheadDistance-vehicle.SpeedMps*.25f))*3.6f);
                    }
                }
                aheadDistance+=after.magnitude;
            }
            // A straight launch at a controlled speed leaves room for a second trick and a stable landing.
            if(activeRoute.requiresJump && routeSegment>=5 && routeSegment<16)targetSpeed=Mathf.Min(targetSpeed,activeRoute.launchSpeedKmh);
            // Set up the takeoff while wheels still have steering authority.
            // Airborne yaw cannot repair a sideways launch on a narrow deck.
            if(activeRoute.requiresJump && routeSegment<8)
            {
                float setupSpeed=RouteSetupSpeed(activeRoute);
                targetSpeed=Mathf.Min(targetSpeed,setupSpeed);
            }
            if(activeRoute.requiresJump&&routeSegment>=8&&routeSegment<activeRoute.launchNodeIndex&&
                (LaunchAngleError(activeRoute)>5 || Mathf.Abs(vehicle.SlipAngle)>5))
                targetSpeed=Mathf.Min(targetSpeed,activeRoute.width<8?55:78);
            if(progressTracker!=null&&vehicle.IsGrounded)
            {
                Vector3 forward=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;
                foreach(var rival in progressTracker.Leaderboard)
                {
                    if(rival.Vehicle==vehicle||rival.Finished)continue;
                    Vector3 offset=rival.Vehicle.Body.position-vehicle.Body.position;
                    float ahead=Vector3.Dot(offset,forward);
                    if(ahead<1||ahead>Mathf.Clamp(vehicle.SpeedMps*.65f,6,20)||Mathf.Abs(offset.y)>3||Mathf.Abs(Vector3.Dot(offset,right))>2.2f)continue;
                    float lead=Mathf.Max(20,Vector3.Dot(rival.Vehicle.Body.linearVelocity,forward)*3.6f);
                    targetSpeed=Mathf.Min(targetSpeed,Mathf.Max(20,lead+(ahead-7)*3));
                    TrafficBrakingDecisions++;intent="ShortcutTrafficSpacing";
                }
            }
        }
        bool HasBoostItem()=>inventory!=null&&inventory.HasItem&&(inventory.HeldItem==RaceItemType.TurboPulse||inventory.HeldItem==RaceItemType.HyperCharge);
        static float RouteSetupSpeed(TrackShortcut route)=>route.width<8?80:route.width<10?100:route.shortcutId=="overpass_launch"?100:90;
        float LaunchAngleError(TrackShortcut route)=>Vector3.Angle(Vector3.ProjectOnPlane(vehicle.Body.linearVelocity,Vector3.up),
            Vector3.ProjectOnPlane(route.pathPoints[route.launchNodeIndex+1]-route.pathPoints[route.launchNodeIndex],Vector3.up));
        void FollowRejoin()
        {
            var road=trackGraph.SampleDistance(rejoinDistance);
            Vector3 offset=transform.position-road.Position;
            if(offset.magnitude<Mathf.Max(8,lookAheadDistance)&&Mathf.Abs(offset.y)<2.5f&&
                Mathf.Abs(Vector3.Dot(offset,road.Right))<road.TrackWidth*.45f&&Vector3.Dot(offset,road.Forward)>-2)
            {rejoinDistance+=Mathf.Max(8,lookAheadDistance);road=trackGraph.SampleDistance(rejoinDistance);}
            target=road.Position;targetLane=0;drift=shortcut=reserveBoost=false;
            targetSpeed=Mathf.Min(targetSpeed,78);intent="CommittedRoadRejoin";
            var current=trackGraph.SampleDistance(trackDistance);
            bool clean=vehicle.IsGrounded&&Mathf.Abs(lateralOffset)<current.TrackWidth*.30f&&
                Mathf.Abs(transform.position.y-current.Position.y)<2&&Vector3.Dot(transform.forward,current.Forward)>.7f;
            rejoinStableTime=clean?rejoinStableTime+1f/DecisionHz:0;
            if(rejoinStableTime>.5f){rejoinDistance=-1;rejoinStableTime=0;}
        }
        void AbortRoute(string reason="Route cancelled")
        {
            if(activeRoute!=null)
            {
                LastRouteAbortReason=reason;
                if(routeEntered)FailedShortcuts++;
                if(routeIndex>=0)routeAttemptMask|=1<<routeIndex;
                trackGraph.ProjectPosition(transform.position,out _,out float distance,out _);
                rejoinDistance=distance+Mathf.Min(lookAheadDistance,NextGateDistance());rejoinStableTime=0;
            }
            activeRoute=null;routeIndex=-1;shortcut=reserveBoost=routeSelected=false;
        }
        public VehicleCommands GetCommands() => commands;
    }
}
