using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-20), RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class AiVehicleController : MonoBehaviour, IVehicleCommandProvider
    {
        static readonly ProfilerMarker DecisionMarker = new ProfilerMarker("Racing.AI.Decide");
        static readonly ProfilerMarker TacticalMarker = new ProfilerMarker("Racing.AI.Tactical");
        static readonly ProfilerMarker StrategicMarker = new ProfilerMarker("Racing.AI.Strategic");

        private static int globalBotCounter = 0;
        private static readonly List<DiscoveredRoute> candidateDiscoveredRoutes = new List<DiscoveredRoute>(16);

        [SerializeField] TrackSplineGraph trackGraph;
        [SerializeField] TrackProgressTracker progressTracker;

        RacingVehiclePhysics vehicle;
        VehicleRecovery recovery;
        ItemInventory inventory;
        RacingItemPool itemPool;
        RacerProgress raceProgress;
        AiRacerProfile profile;
        AiDecisionLogger logger;
        AiItemAgent itemAgent;
        DeterministicRng rng;

        VehicleCommands commands;
        Vector3 target;

        int botId;
        float profileStartedTime;
        readonly string[] plannedActions = new string[5];
        int plannedActionCount;
        static readonly string[] RoadPlan = {"RacingLine","Brake","Drift","Turbo","Rejoin"};
        static readonly string[] DriftPlan = {"Drift","Turbo","Hop","AerialCorrection","LandingBoost"};
        static readonly string[] AirPlan = {"Hop","AerialCorrection","LandingBoost","Rejoin"};
        static readonly string[] SkyPlan = {"ReserveBoost","Launch","AerialCorrection","LandingBoost","Rejoin"};
        static readonly string[] ApexPlan = {"Shortcut","Turbo","Hop","AerialCorrection","LandingBoost"};
        static readonly string[] SafeRoutePlan = {"Shortcut","Brake","Rejoin"};
        float nextTactical, nextStrategic, launchTime = -1;
        float targetSpeed, steering, lane, targetLane, trafficSpeedLimit = float.MaxValue;
        float angle, trackDistance, lookAheadDistance, trackCurvature, lateralOffset, racingLineOffset;
        float blockedTime, reverseUntil, nextReverse, laneHoldUntil, mistakeUntil, mistakeOffset, routeEntryDistance, nextChainHop, driftStarted;
        int targetNode, currentNode, routeSegment, recoverySeen, routeConsideredLap = -1, routeAttemptLap = -1, routeAttemptMask;
        int driftTurnSign;
        float edgeUrgency, edgeSide;
        float gridLane, raceStartTime = -1;

        TrackShortcut activeRoute, consideredRoute;
        DiscoveredRoute activeDiscoveredRoute;
        float aerialBoostRemaining, plannedPitch, plannedAirRoll, aerialAirStart;

        bool drift, shortcut, routeEntered, reserveBoost, routeSelected, routeHopTaken;
        string intent = "RacingLine";
        float routeProgressAt;
        float rejoinDistance = -1, rejoinStableTime;
        int routeIndex = -1;
        int[] routeEntries, routeCompletions;
        float[] routeValueSeconds;
        readonly float[] laneCosts = new float[3];
        static readonly float[] LaneOptions = { 0, -.40f, .40f };

        // Performance Budget Update Frequencies (Hz)
        public float FastControlHz => 20f;
        public float TacticalHz => profile == null ? 6f : Mathf.Lerp(4f, 8f, profile.awareness);
        public float StrategicHz => 1.5f;

        public AiRacerProfile Profile => profile;
        public AiDecisionLogger Logger => logger;
        public Vector3 CurrentTarget => target;
        public float DecisionHz => TacticalHz;
        public bool ExecutingShortcut => shortcut;
        public int CurrentNodeIndex => currentNode;
        public int TargetNodeIndex => targetNode;
        public float LookAheadDistance => lookAheadDistance;
        public float TrackCurvature => trackCurvature;
        public float TargetSpeedKmh => targetSpeed;
        // Observation only: the final safe command includes the edge/heading caps.
        public float CommandSpeedKmh { get; private set; }
        public float EdgeUrgency => edgeUrgency;
        public bool ProtectingRoadBoundary => false;
        public bool TrafficBraking => trafficSpeedLimit<float.MaxValue && vehicle.SpeedKmh>trafficSpeedLimit+4;
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
        public int ActiveRouteIndex => routeIndex;
        public int RouteSegment => routeSegment;
        public int FailedShortcuts { get; private set; }
        public int EdgeGuardSteps { get; private set; }
        public int DriftEnvelopeReleases { get; private set; }
        public string LastRouteAbortReason { get; private set; }
        public DiscoveredRoute ActiveDiscoveredRoute => activeDiscoveredRoute;
        public string CurrentIntent => intent;
        public Vector3 TargetLanding => activeRoute!=null&&activeRoute.requiresJump?activeRoute.pathPoints[activeRoute.landingNodeIndex]:target;
        public int PlannedActionDepth => plannedActionCount;
        public string PlanSummary => string.Join(" > ", plannedActions, 0, plannedActionCount);

        public int RouteEntries(int index) => routeEntries != null && index >= 0 && index < routeEntries.Length ? routeEntries[index] : 0;
        public int RouteCompletions(int index) => routeCompletions != null && index >= 0 && index < routeCompletions.Length ? routeCompletions[index] : 0;

        void Awake()
        {
            botId = ++globalBotCounter;
            vehicle = GetComponent<RacingVehiclePhysics>();
            recovery = GetComponent<VehicleRecovery>();
            inventory = GetComponent<ItemInventory>();
            logger = new AiDecisionLogger();
        }

        public void Initialize(AiRacerProfile skill, TrackSplineGraph graph, TrackProgressTracker progress, string name = "AI")
        {
            if (logger == null) logger = new AiDecisionLogger();
            if (vehicle == null) vehicle = GetComponent<RacingVehiclePhysics>();
            if (recovery == null) recovery = GetComponent<VehicleRecovery>();
            if (inventory == null) inventory = GetComponent<ItemInventory>();
            // A previous race must not change this seed's update schedule.
            botId = Mathf.Abs(skill.personalitySeed % 24) + 1;

            profile = skill;
            profileStartedTime = CurrentTime;
            trackGraph = graph;
            progressTracker = progress;
            logger.BotName = name;
            rng = new DeterministicRng(skill.personalitySeed ^ 71829);
            itemAgent = new AiItemAgent(skill, inventory, vehicle, graph, logger);
            itemPool = RacingItemPool.ForScene();
            UpdateActionPlan();
            lane = Mathf.Abs(skill.personalitySeed % 997) / 996f * .22f - .11f;

            // Stagger bot updates across frames to prevent CPU frame spikes
            float tacticalStagger = (botId % 8) * (1f / (8f * 6f));
            float strategicStagger = (botId % 6) * (1f / (6f * 1.5f));
            nextTactical = CurrentTime + tacticalStagger;
            nextStrategic = CurrentTime + strategicStagger;

            vehicle.SetCommandProvider(this);
            routeEntries = new int[graph.Shortcuts != null ? graph.Shortcuts.Length : 0];
            routeCompletions = new int[graph.Shortcuts != null ? graph.Shortcuts.Length : 0];
            routeValueSeconds = new float[routeCompletions.Length];
            for (int i = 0; i < routeValueSeconds.Length; i++) routeValueSeconds[i] = EstimateRouteValue(graph.Shortcuts[i]);
        }

        // Geometric time value of a link for this car: main-road length at a
        // typical racing pace versus link length under the shared 50% link speed
        // rule (boost-fuelled links at the shared boost multiplier). Track
        // knowledge, not a physics change; a link that loses time is not "secret tech".
        float EstimateRouteValue(TrackShortcut route)
        {
            if (route == null || route.pathPoints == null || route.pathPoints.Length < 2 || vehicle == null) return 0;
            float linkLength = 0;
            for (int i = 1; i < route.pathPoints.Length; i++) linkLength += Vector3.Distance(route.pathPoints[i - 1], route.pathPoints[i]);
            float mainLength = Mathf.Repeat(trackGraph.GetNode(route.exitNodeIndex).CumulativeDistance - trackGraph.GetNode(route.entryNodeIndex).CumulativeDistance, trackGraph.TotalLength);
            float top = Mathf.Max(10, vehicle.Stats.SpeedKmh / 3.6f);
            float linkSpeed = top * TrackSplineGraph.ShortcutSpeedScale * (route.requiresBoost ? 1.3f : 1f);
            float value = mainLength / (top * .80f) - linkLength / linkSpeed;
            if (!loggedRouteValues.Contains(route.shortcutId))
            {
                loggedRouteValues.Add(route.shortcutId);
                Debug.Log($"[AI] Route {route.shortcutId}: link {linkLength:F0} m, main {mainLength:F0} m, estimated value {value:F2} s");
            }
            return value;
        }
        const float RouteOverheadSeconds = 3f;
        static readonly HashSet<string> loggedRouteValues = new HashSet<string>();

        private float? simTimeOverride;
        public float CurrentTime => simTimeOverride ?? Time.time;
        public void StepAi(float customTime = -1f)
        {
            if (customTime >= 0f) simTimeOverride = customTime;
            try
            {
                FixedUpdate();
            }
            finally
            {
                if (customTime >= 0f) simTimeOverride = null;
            }
        }
        public void LaunchImmediate() => launchTime = 0f;

        void FixedUpdate()
        { using(DecisionMarker.Auto()) StepControl(); }
        void StepControl()
        {
            if (profile == null || trackGraph == null || !vehicle.RaceEnabled)
            {
                commands = VehicleCommands.Idle;
                launchTime = -1;
                return;
            }
            if (recovery != null && recovery.IsRecovering)
            {
                commands = VehicleCommands.Idle;
                return;
            }
            if (recovery != null && recovery.RecoveryCount != recoverySeen)
            {
                recoverySeen = recovery.RecoveryCount;
                AbortRoute("Shared recovery: " + recovery.LastRecoveryReason);
                rejoinDistance = -1;
                shortcut = drift = false;
                steering = blockedTime = reverseUntil = nextTactical = 0;
            }

            if (launchTime < 0)
            {
                launchTime = CurrentTime + Mathf.Lerp(.55f, .03f, profile.reactionSkill) + Mathf.Abs(profile.personalitySeed % 17) * .002f;
                // Remember the grid column; the opening seconds hold it (see below).
                trackGraph.ProjectPosition(transform.position, out _, out _, out float gridLateral);
                gridLane = Mathf.Clamp(gridLateral / Mathf.Max(1, trackGraph.GetNode(0).TrackWidth * .4f), -.85f, .85f);
                raceStartTime = CurrentTime;
            }

            if (launchTime > 0f && CurrentTime < launchTime)
            {
                commands = VehicleCommands.Idle;
                return;
            }

            // Layer 4 & 5: Strategic Planning (1-2 Hz, staggered)
            if (CurrentTime >= nextStrategic)
            {
                nextStrategic = CurrentTime + 1f / StrategicHz;
                using (StrategicMarker.Auto())
                {
                    StrategicDecide();
                }
            }

            // Tactical Decisions (4-8 Hz, staggered)
            if (CurrentTime >= nextTactical)
            {
                nextTactical = CurrentTime + 1f / TacticalHz;
                using (TacticalMarker.Auto())
                {
                    TacticalDecide();
                }
            }

            // Fast Control (15-20 Hz per FixedUpdate)
            ExecuteFastControl();
        }

        private void ExecuteFastControl()
        {
            // A route target only 8-16 m away expires before a 1.5 Hz strategy tick.
            // Follow ordered route segments at the physics cadence; strategy only selects.
            if (activeRoute != null) FollowRoute();
            else if (activeDiscoveredRoute != null) FollowDiscoveredRoute();
            else if (rejoinDistance >= 0) FollowRejoin(true);
            targetSpeed = Mathf.Min(targetSpeed, trafficSpeedLimit);
            lane = Mathf.MoveTowards(lane, targetLane, Time.fixedDeltaTime * Mathf.Lerp(.22f, .65f, profile.awareness));
            Vector3 deltaToTarget = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            Vector3 heading = vehicle.SpeedMps > 5 ? Vector3.ProjectOnPlane(vehicle.Body.linearVelocity, Vector3.up).normalized : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float localX = Vector3.Dot(deltaToTarget, Vector3.Cross(Vector3.up, heading));
            float localZ = Vector3.Dot(deltaToTarget, heading);
            float distance = Mathf.Max(4, deltaToTarget.magnitude);
            if(vehicle.IsDrifting && drift && activeRoute==null && activeDiscoveredRoute==null)
            {
                var road=trackGraph.GetNode(currentNode);
                float offset=Vector3.Dot(transform.position-road.Position,road.Right);
                float sideSpeed=Vector3.Dot(vehicle.Body.linearVelocity,road.Right);
                // Release while there is still room for normal tire grip to catch
                // an outward slide. Waiting until 46% width reaches the barrier.
                if(Mathf.Abs(offset)>road.TrackWidth*.34f && offset*sideSpeed>0 &&
                    Mathf.Abs(offset+sideSpeed*.18f)>road.TrackWidth*.43f)
                    drift=false;
            }
            // Open road edges: predict the outward slide from the actual velocity.
            // Lifting/braking is the only remedy once steering is saturated, so it
            // limits this step's pace command; stats and grip remain untouched.
            float commandSpeed = targetSpeed;
            edgeUrgency = 0;
            if (activeRoute == null && activeDiscoveredRoute == null && rejoinDistance < 0 &&
                vehicle.IsGrounded && !vehicle.IsSpunOut && vehicle.SpeedMps > 6)
            {
                trackGraph.ProjectPosition(transform.position, out _, out float edgeDistance, out float edgeLateral);
                var edgeRoad = trackGraph.SampleDistance(edgeDistance);
                float half = edgeRoad.TrackWidth * .5f;
                float outward = Vector3.Dot(vehicle.Body.linearVelocity, edgeRoad.Right) * Mathf.Sign(edgeLateral);
                float predicted = Mathf.Abs(edgeLateral) + Mathf.Max(0, outward) * 1.0f;
                if (outward > .5f && predicted > half - 3f)
                {
                    float urgency = Mathf.InverseLerp(half - 3f, half + 1.5f, predicted);
                    edgeUrgency = urgency; edgeSide = Mathf.Sign(edgeLateral);
                    // A sliding drift keeps only part of its lateral grip.
                    if (vehicle.IsDrifting && drift && urgency > .25f) { drift = false; DriftEnvelopeReleases++; }
                    commandSpeed = Mathf.Min(commandSpeed, vehicle.SpeedKmh * (1f - .30f * urgency) - 4f * urgency);
                    EdgeGuardSteps++;
                }
            }
            if (vehicle.IsGrounded && !vehicle.IsSpunOut && distance > 6)
            {
                // Reserve actual yaw authority for line correction on every road.
                // A drift multiplier is available only during an actual drift.
                float pursuitCurvature = Mathf.Abs(2 * localX / (distance * distance));
                if (pursuitCurvature > .005f)
                {
                    float authority=vehicle.Stats.HandlingRadPerSec*.5f;
                    if(vehicle.IsDrifting&&drift)authority*=Mathf.Lerp(1,1.3f,Mathf.Clamp01(vehicle.Stats.DriftMultiplier-.5f));
                    float margin=activeRoute!=null?.80f:Mathf.Lerp(.78f,.93f,profile.racingLineSkill);
                    targetSpeed=Mathf.Min(targetSpeed,authority*margin/pursuitCurvature*3.6f);
                }
            }
            commandSpeed = Mathf.Min(commandSpeed, targetSpeed);
            Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float faceAngle = Mathf.Abs(Mathf.Atan2(Vector3.Dot(deltaToTarget, Vector3.Cross(Vector3.up, facing)), Vector3.Dot(deltaToTarget, facing))) * Mathf.Rad2Deg;
            // Facing well away from the road after a spin/contact: turn around at a
            // radius that fits inside the open road instead of a wide fast U-turn.
            if (vehicle.IsGrounded && faceAngle > 75f)
                commandSpeed = Mathf.Min(commandSpeed, Mathf.Lerp(70f, 28f, Mathf.InverseLerp(75f, 100f, faceAngle)));
            // Full throttle commitment: drive at maximum speed when under target speed
            CommandSpeedKmh = commandSpeed;
            float overspeed = vehicle.SpeedKmh - commandSpeed;
            float throttle = 1f;
            if (overspeed > 0)
            {
                float throttleAllowance = Mathf.Lerp(4.0f, 9.0f, profile.throttleSkill);
                throttle = Mathf.Clamp01(1f - overspeed / throttleAllowance);
            }
            if (overspeed <= 0)
                throttle = 1f;

            float brakeThreshold = Mathf.Lerp(4.0f, 7.5f, profile.brakingSkill);
            float brake = overspeed > brakeThreshold
                ? Mathf.Clamp01((overspeed - brakeThreshold) / 6.0f) * Mathf.Lerp(.55f, 1f, profile.brakingSkill)
                : 0f;

            if (vehicle.IsDrifting && (brake > .88f || Mathf.Abs(vehicle.SlipAngle) > 42 || Mathf.Abs(lateralOffset) > trackGraph.GetNode(currentNode).TrackWidth * .46f))
                drift = false;

            // Physics applies UpdateDrift before steering in this same fixed step.
            // Use the steering equation of the requested state, including the
            // first step after releasing; the previous drift equation inverted it.
            float pathYaw = 2 * Mathf.Max(7, vehicle.SpeedMps) * localX / (distance * distance);
            // Shared drift steering spans only DriftDirection*[0.01, 1.11]. When the
            // path needs yaw the other way (reversed corner, wrong-side start), the
            // drift physically cannot turn there: release to ordinary grip steering.
            if (vehicle.IsDrifting && drift && vehicle.IsGrounded && pathYaw * vehicle.DriftDirection < -.10f)
            { drift = false; DriftEnvelopeReleases++; }
            bool steeringInDrift=vehicle.IsDrifting&&drift;
            float desiredYaw = pathYaw;
            float desiredSlip = steeringInDrift ? -vehicle.DriftDirection * Mathf.Lerp(
                profile.style == AiRacingStyle.DriftPrecision ? 8 : 6, 13, profile.driftSkill) : 0;
            desiredYaw += (vehicle.SlipAngle - desiredSlip) * Mathf.Deg2Rad * .65f;
            desiredYaw += (desiredYaw - Vector3.Dot(vehicle.Body.angularVelocity, Vector3.up)) * .30f;
            float desiredSteer = Mathf.Clamp(desiredYaw / (vehicle.Stats.HandlingRadPerSec * .5f), -1, 1);
            if (localZ < 0) desiredSteer = Mathf.Sign(localX);
            // At crawling speed the shared yaw authority scales with speed; the
            // pursuit law then asks for almost no steering and the car creeps out.
            else if (vehicle.SpeedMps < 14f && faceAngle > 25f && vehicle.IsGrounded)
                desiredSteer = Mathf.Sign(Vector3.Dot(deltaToTarget, Vector3.Cross(Vector3.up, facing)));
            // Sliding toward an open edge: a distant pursuit target under-asks for
            // yaw. Steer back toward the centre at least in proportion to urgency.
            if (edgeUrgency > .1f && !steeringInDrift)
                desiredSteer = -edgeSide * Mathf.Max(-edgeSide * desiredSteer, Mathf.Clamp01(edgeUrgency * 1.6f));
            if (steeringInDrift)
            {
                float authority = Mathf.Lerp(1, 1.3f, Mathf.Clamp01(vehicle.Stats.DriftMultiplier - .5f));
                desiredSteer = Mathf.Clamp((desiredSteer / authority - vehicle.DriftDirection * .56f) / .55f, -1, 1);
            }
            steering = Mathf.MoveTowards(steering, desiredSteer, Time.fixedDeltaTime * Mathf.Lerp(3.8f, 10, profile.racingLineSkill));

            bool trick = !vehicle.IsGrounded && profile.trickSkill > .22f &&
                vehicle.AirborneTime > Mathf.Lerp(.35f, .16f, profile.trickSkill) &&
                vehicle.TrickCount < (profile.trickSkill > .88f && vehicle.AirborneTime > .67f ? 2 : 1);

            // Aerial Boost and Air Roll logic
            bool aerialBoost = false;
            float pitch = 0f;
            float airRoll = 0f;

            if (!vehicle.IsGrounded)
            {
                if (activeDiscoveredRoute != null && activeDiscoveredRoute.isAerialRoute)
                {
                    float airElapsed = CurrentTime - aerialAirStart;
                    aerialBoost = airElapsed <= activeDiscoveredRoute.boostDuration && airElapsed >= activeDiscoveredRoute.jumpTiming;
                    pitch = Mathf.Clamp(activeDiscoveredRoute.pitch / 25f, -1f, 1f);
                    airRoll = Mathf.Clamp(activeDiscoveredRoute.airRoll / 30f, -1f, 1f);
                }
                else
                {
                    // General in-air stabilization: level vehicle and avoid nose-dive
                    var landingRoad = trackGraph.SampleDistance(trackDistance + Mathf.Max(12, vehicle.SpeedMps * .6f));
                    Vector3 landingForward=landingRoad.Forward;
                    if(activeRoute!=null&&activeRoute.requiresJump)
                    {int landing=Mathf.Clamp(activeRoute.landingNodeIndex,0,activeRoute.pathPoints.Length-2);landingForward=(activeRoute.pathPoints[landing+1]-activeRoute.pathPoints[landing]).normalized;}
                    pitch = Mathf.Clamp((transform.forward.y - landingForward.y) * 2f - Vector3.Dot(vehicle.Body.angularVelocity, transform.right) * .25f, -.7f, .7f);
                    airRoll = Mathf.Clamp(Vector3.Dot(transform.right, landingRoad.Up) * 2f + Vector3.Dot(vehicle.Body.angularVelocity, transform.forward) * .25f, -.8f, .8f) * profile.trickSkill;

                    // Pulse only after an intentional hop or shared launch pad.
                    // Road crests and a landing bounce are not new jump plans.
                    float sinceLaunch = CurrentTime - vehicle.LastJumpTime;
                    bool launchedThisFlight = sinceLaunch >= 0 && sinceLaunch < .70f &&
                        sinceLaunch <= vehicle.AirborneTime + .16f;
                    bool beforeLanding = activeRoute == null || !activeRoute.requiresJump ||
                        (routeSegment >= activeRoute.launchNodeIndex && routeSegment < activeRoute.landingNodeIndex);
                    float boostTarget = activeRoute != null && activeRoute.requiresJump
                        ? activeRoute.launchSpeedKmh * 1.02f
                        : Mathf.Min(vehicle.Stats.SpeedKmh * 1.10f, targetSpeed + 10f);
                    bool authoredFlight=activeRoute!=null&&activeRoute.requiresJump;
                    // A valid shared kicker can launch below 85 km/h after traffic or
                    // alignment braking. Refusing its planned air correction then
                    // strands the car in the gap. Stop thrust at the route's speed
                    // target, using the same bounded aerial input as the human.
                    float correctionLimit=authoredFlight?.45f:Mathf.Lerp(.07f,
                        profile.style==AiRacingStyle.AerialAggressive?.20f:.17f,profile.trickSkill);
                    if (profile.trickSkill > .70f && launchedThisFlight && beforeLanding &&
                        vehicle.AirborneTime > .16f && vehicle.AirborneTime < .65f &&
                        Plans("AerialCorrection") && vehicle.AerialBoostTimer < correctionLimit &&
                        vehicle.SpeedKmh > (authoredFlight?55f:85f) && vehicle.SpeedKmh < boostTarget && angle < 10f)
                    {
                        if (Vector3.Dot(transform.forward, landingForward) > .94f && transform.forward.y > -.10f)
                        {
                            aerialBoost = true;
                            pitch = Mathf.Clamp(pitch - .10f * profile.trickSkill, -.7f, .7f);
                        }
                    }
                }
            }

            bool itemBackward = commands.ItemBackward;
            commands = new VehicleCommands
            {
                Steering = steering,
                Throttle = throttle,
                Brake = brake,
                // Physics fixes DriftDirection from the steering sign on the start
                // step. Hold the request until steering already points into the
                // planned corner instead of locking a drift the wrong way.
                Drift = drift && (vehicle.IsDrifting || driftTurnSign == 0 || steering * driftTurnSign > .22f),
                Trick = trick,
                AerialBoost = aerialBoost,
                Pitch = pitch,
                AirRoll = airRoll,
                ItemBackward = itemBackward
            };

            // Exit hop chaining
            if (activeRoute != null && routeEntered && !activeRoute.requiresJump && !routeHopTaken &&
                profile.chainingSkill > .68f && Plans("Hop") && routeSegment >= 9 && routeSegment <= 11 && vehicle.IsGrounded &&
                !vehicle.IsSpunOut && vehicle.SpeedKmh > 55 && angle < 8 && Mathf.Abs(vehicle.SlipAngle) < 6 && HasSafeRouteHopLanding())
            {
                commands.Jump = true;
                routeHopTaken = true;
                intent = "ExitTurboToHopTrickLanding";
            }

            if (activeRoute == null && profile.chainingSkill > .85f && Plans("Hop") && CurrentTime > nextChainHop &&
                vehicle.IsBoosting && !vehicle.IsDrifting && vehicle.IsGrounded && !vehicle.IsSpunOut &&
                vehicle.SpeedKmh > 72 && angle < 5 && trackCurvature < .004f &&
                Mathf.Abs(lateralOffset) < trackGraph.GetNode(currentNode).TrackWidth * .22f && NextGateDistance() > 24 && HasSafeHopLanding())
            {
                commands.Jump = true;
                nextChainHop = CurrentTime + Mathf.Lerp(5.5f, 1.55f, profile.chainingSkill);
                intent = "DriftBoostHopTrickLanding";
            }

            if (vehicle.IsGrounded && !vehicle.IsSpunOut && commands.Throttle > .3f && vehicle.SpeedMps < 1.2f)
            {
                blockedTime += Time.fixedDeltaTime;
                if (blockedTime > .85f && CurrentTime > nextReverse) { reverseUntil = CurrentTime + .8f; nextReverse = CurrentTime + 3; }
                if (blockedTime > Mathf.Lerp(3.4f, 2.6f, profile.recoverySkill)) { recovery?.TriggerRecovery("AI blocked after reverse attempt"); blockedTime = 0; }
            }
            else blockedTime = 0;

            if (CurrentTime < reverseUntil)
                commands = new VehicleCommands { Brake = 1, Steering = -Mathf.Sign(Mathf.Abs(desiredSteer) > .1f ? desiredSteer : 1) };
        }

        private void StrategicDecide()
        {
            UpdateDrivingTarget();
            PlanRoute();
            PlanDiscoveredRoute();
            UpdateActionPlan();
        }

        bool HasSafeHopLanding()
        {
            // A hop preserves horizontal velocity: inspect the whole projected flight.
            // This uses the shared hop envelope, never a new force or a different car.
            float startHeight = trackGraph.SampleDistance(trackDistance).Position.y;
            for (int i = 1; i <= 6; i++)
            {
                // Include the full shared hop plus bounded aerial-correction
                // envelope. At ~200 km/h over a falling road a hop stays airborne
                // for 2 s+, so inspect that whole flight and refuse descents.
                float seconds = i * .35f;
                var road = trackGraph.SampleDistance(trackDistance + vehicle.SpeedMps * seconds);
                Vector3 projected = transform.position + vehicle.Body.linearVelocity * seconds;
                float side = Mathf.Abs(Vector3.Dot(projected - road.Position, road.Right));
                if (side > road.TrackWidth * .30f || Vector3.Angle(transform.forward, road.Forward) > 12f || road.Position.y < startHeight - 2.5f)
                    return false;
            }
            return true;
        }

        bool HasSafeRouteHopLanding()
        {
            var points=activeRoute.pathPoints;
            for(int sample=1;sample<=3;sample++)
            {
                Vector3 projected=transform.position+vehicle.Body.linearVelocity*(sample*.20f);
                float closest=float.MaxValue;
                for(int segment=routeSegment;segment<points.Length-1;segment++)
                {
                    Vector3 delta=Vector3.ProjectOnPlane(points[segment+1]-points[segment],Vector3.up);
                    Vector3 offset=Vector3.ProjectOnPlane(projected-points[segment],Vector3.up);
                    float fraction=delta.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(offset,delta)/delta.sqrMagnitude):0;
                    closest=Mathf.Min(closest,(offset-delta*fraction).sqrMagnitude);
                }
                if(closest>activeRoute.width*activeRoute.width*.09f)return false;
            }
            return true;
        }

        void UpdateActionPlan()
        {
            string[] sequence = activeRoute != null
                ? (activeRoute.requiresJump ? SkyPlan : profile.style == AiRacingStyle.SafeConsistency ? SafeRoutePlan : ApexPlan)
                : (!vehicle.IsGrounded || vehicle.IsBoosting) && profile.style != AiRacingStyle.SafeConsistency
                    ? AirPlan : drift ? DriftPlan : RoadPlan;
            int first = activeRoute != null && activeRoute.requiresJump
                ? routeSegment >= activeRoute.landingNodeIndex ? 3 : routeSegment >= activeRoute.launchNodeIndex ? 2 : 0 : 0;
            plannedActionCount = Mathf.Min(profile.PlanningDepth, sequence.Length - first);
            for (int i = 0; i < plannedActionCount; i++) plannedActions[i] = sequence[first + i];
        }

        bool Plans(string action)
        {
            for (int i = 0; i < plannedActionCount; i++) if (plannedActions[i] == action) return true;
            return false;
        }

        private void UpdateDrivingTarget()
        {
            trackGraph.ProjectPosition(transform.position, out currentNode, out trackDistance, out float lateral);
            if (raceProgress == null) raceProgress = progressTracker?.FindRacer(vehicle);
            if (raceProgress != null && routeAttemptLap != raceProgress.CurrentLap)
            {
                routeAttemptLap = raceProgress.CurrentLap;
                routeAttemptMask = 0;
            }

            var current = trackGraph.SampleDistance(trackDistance);
            lookAheadDistance = Mathf.Clamp(16f + vehicle.SpeedMps * Mathf.Lerp(.52f, .85f, profile.racingLineSkill), 24f, 75f);
            var future = trackGraph.SampleDistance(trackDistance + lookAheadDistance);
            float turn = Vector3.SignedAngle(current.Forward, future.Forward, Vector3.up);
            trackCurvature = Mathf.Abs(turn) * Mathf.Deg2Rad / lookAheadDistance;
            lateralOffset = lateral;
            targetSpeed = BrakingEnvelope(current);

            float entryTurn = Vector3.SignedAngle(current.Forward, trackGraph.SampleDistance(trackDistance + 10).Forward, Vector3.up);
            float racingOffset = Mathf.Lerp(current.SafeLineOffset, future.OptimalLineOffset, profile.racingLineSkill);
            if (Mathf.Abs(entryTurn) < 5 && Mathf.Abs(turn) > 10 && profile.racingLineSkill > .60f)
                racingOffset = -Mathf.Sign(turn) * .20f;

            if (CurrentTime >= mistakeUntil && rng.NextFloat() < Mathf.Lerp(.025f, .0001f, profile.consistency))
            {
                mistakeUntil = CurrentTime + Mathf.Lerp(1.6f, .3f, profile.consistency);
                mistakeOffset = rng.Range(-.20f, .20f) * (1 - profile.consistency);
            }
            float natural = Mathf.Sin((CurrentTime-profileStartedTime) * .33f + profile.personalitySeed * .01f) * (1 - profile.consistency) * .14f;
            if (CurrentTime < mistakeUntil) natural += mistakeOffset;
            if (profile.style == AiRacingStyle.DriftPrecision) natural *= 1f - .35f * profile.NormalizedSkill;

            // Opening phase: twelve cars converging on one racing line was the main
            // source of lap-1 contacts. Hold the grid column, then blend before the first corner.
            float opening = raceStartTime >= 0 ? Mathf.InverseLerp(6.5f, 3f, CurrentTime - raceStartTime) : 0;
            float lineBlend = Mathf.Lerp(racingOffset + lane, gridLane, opening);
            racingLineOffset = Mathf.Clamp((lineBlend + natural) * current.TrackWidth * .4f, -current.TrackWidth * .38f, current.TrackWidth * .38f);
            target = future.Position + future.Right * racingLineOffset;
            if (NextGateDistance() < 28) target = future.Position + future.Right * racingLineOffset * .35f;
            float gateDistance=NextGateDistance();
            if(gateDistance<lookAheadDistance&&gateDistance>4&&activeRoute==null&&rejoinDistance<0&&raceProgress!=null)
            {
                int gate=(raceProgress.LastCheckpointIndex+1)%trackGraph.CheckpointNodeIndices.Length;
                target=trackGraph.GetNode(trackGraph.CheckpointNodeIndices[gate]).Position;
            }
            trackGraph.ProjectPosition(future.Position, out targetNode, out _, out _);

            shortcut = false;

            if (activeRoute != null) FollowRoute();
            else if (activeDiscoveredRoute != null) FollowDiscoveredRoute();
            else if (rejoinDistance >= 0) FollowRejoin();
            else if (Mathf.Abs(lateral) > current.TrackWidth * .45f)
            {
                targetLane = 0; target = future.Position; intent = "RecoverLine";
            }
        }

        private void TacticalDecide()
        {
            UpdateDrivingTarget();
            var current = trackGraph.SampleDistance(trackDistance);
            var future = trackGraph.SampleDistance(trackDistance + lookAheadDistance);
            float turn = Vector3.SignedAngle(current.Forward, future.Forward, Vector3.up);

            // RacingLineOffset is already in meters and includes the current lane.
            // Candidate lanes below are offsets from the base racing line.
            var traffic = EvaluatePack(current, racingLineOffset-lane*current.TrackWidth*.4f, turn);
            traffic.straightDistance = lookAheadDistance;

            angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.ProjectOnPlane(target - transform.position, Vector3.up));
            if (angle > 105f) targetSpeed = Mathf.Min(targetSpeed, Mathf.Lerp(55f, 75f, profile.recoverySkill));

            // Drift decision (Mini vs Medium vs Super Turbo release)
            if (vehicle.IsDrifting && activeRoute == null && activeDiscoveredRoute == null)
            {
                float postCornerDistance = 45f;
                var postCorner = trackGraph.SampleDistance(trackDistance + postCornerDistance);
                float postTurn = Mathf.Abs(Vector3.SignedAngle(current.Forward, postCorner.Forward, Vector3.up));
                bool longRunway = postTurn < 12f;

                bool wantSuper = profile.driftSkill > 0.82f && longRunway;
                bool wantMedium = profile.driftSkill > 0.55f;
                bool exitReady = Mathf.Abs(turn) < 4f || (Mathf.Sign(turn) != vehicle.DriftDirection && Mathf.Abs(turn) > 5f);
                bool hasSuper = vehicle.CurrentMiniTurboStage == MiniTurboStage.UltraMiniTurbo;
                bool hasMedium = vehicle.CurrentMiniTurboStage == MiniTurboStage.SuperMiniTurbo;
                bool hasMini = vehicle.CurrentMiniTurboStage == MiniTurboStage.MiniTurbo;

                bool shouldRelease = false;
                if (exitReady && hasMini && !wantSuper && !wantMedium)
                    shouldRelease = true;
                else if (hasSuper)
                {
                    shouldRelease = exitReady || Mathf.Abs(turn) < 14f;
                }
                else if (hasMedium)
                {
                    if (exitReady && (!wantSuper || CurrentTime - driftStarted > 2.8f))
                        shouldRelease = true;
                }
                else if (hasMini)
                {
                    if (exitReady && !wantMedium && !wantSuper)
                        shouldRelease = true;
                }
                else
                {
                    if (exitReady && CurrentTime - driftStarted > 0.7f)
                        shouldRelease = true;
                }

                // A corner that reverses direction ends this drift regardless of
                // the charge being saved for; the stage boost is still released.
                bool reversed = vehicle.DriftDirection != 0 && Mathf.Sign(turn) != vehicle.DriftDirection && Mathf.Abs(turn) > 8f;
                if (shouldRelease || reversed || shortcut || angle > 65f || Mathf.Abs(lateralOffset) > current.TrackWidth * .46f)
                    drift = false;
            }
            else if (activeRoute == null && activeDiscoveredRoute == null)
            {
                drift = intent != "PrepareJumpAndLanding" && rejoinDistance < 0 && profile.driftSkill > .26f && edgeUrgency <= 0f &&
                    Mathf.Abs(turn) > Mathf.Lerp(10f, 4.5f, profile.driftSkill) && Mathf.Abs(turn) < 70f &&
                    vehicle.SpeedKmh > 40f &&
                    Mathf.Abs(lateralOffset) < current.TrackWidth * .42f && !vehicle.IsSpunOut;
                if (drift) { driftStarted = CurrentTime; driftTurnSign = turn > 0 ? 1 : -1; }
            }

            if (drift) intent = "DriftToExitBoost";
            traffic.reserveBoost = reserveBoost;
            traffic.shortcutEntryReady = activeRoute!=null&&activeRoute.requiresBoost&&(!activeRoute.requiresJump||
                routeSegment>=activeRoute.launchNodeIndex-2 && LaunchAngleError(activeRoute)<7 && Mathf.Abs(vehicle.SlipAngle)<7 && trafficSpeedLimit>80) ||
                (activeDiscoveredRoute != null && activeDiscoveredRoute.requiresAerialBoost);
            traffic.turnDegrees = shortcut ? angle : Mathf.Max(Mathf.Abs(turn), angle);
            traffic.finalLap = raceProgress != null && raceProgress.CurrentLap == progressTracker.TotalLaps;
            traffic.position = raceProgress != null ? raceProgress.RacePosition : 12;

            if (inventory != null && inventory.HasItem && itemAgent.ShouldUseItem(traffic))
            {
                commands.ItemBackward = itemAgent.ShouldThrowBackward(traffic);
                bool boost = RaceItemUtility.IsShortcutBoost(inventory.HeldItem);
                if (inventory.UseItem() && boost && traffic.shortcutEntryReady) BoostsForShortcuts++;
            }
            logger.UpdateSteeringDecision(intent, reserveBoost ? "PreserveShortcutResource" : "TrackAndPredictedTraffic", target, steering, commands.Throttle, drift);
            UpdateActionPlan();
        }

        private void PlanDiscoveredRoute()
        {
            if (activeRoute != null || activeDiscoveredRoute != null || rejoinDistance >= 0 || profile.chainingSkill < .85f) return;
            var kb = ShortcutKnowledgeBase.Instance;
            if (kb == null) return;

            kb.GetCandidateRoutes(trackDistance, 90f, trackGraph.TotalLength, candidateDiscoveredRoutes);
            if (candidateDiscoveredRoutes.Count == 0) return;

            int pos = raceProgress != null ? raceProgress.RacePosition : 6;
            bool isLeading = pos == 1;
            bool isTrailing = pos >= 4;
            bool finalLap = raceProgress != null && raceProgress.CurrentLap == progressTracker.TotalLaps;

            for (int i = 0; i < candidateDiscoveredRoutes.Count; i++)
            {
                var candidate = candidateDiscoveredRoutes[i];
                // Ballistic catalog estimates are proposals, not verified shared-physics routes.
                if (!candidate.validatedSharedPhysics || candidate.pathPoints == null || candidate.pathPoints.Length < 2) continue;
                bool bypassesGate = false;
                foreach (int gate in trackGraph.CheckpointNodeIndices)
                    bypassesGate |= gate > candidate.entryNodeIndex && gate < candidate.exitNodeIndex;
                if (bypassesGate) continue;
                float confidence = kb.GetConfidence(candidate.routeId);

                // Decision rule based on race position and risk tolerance
                if (isLeading && (confidence < 0.88f || candidate.expectedValue < 1.0f))
                    continue;

                if (!isLeading && !isTrailing && confidence < 0.70f)
                    continue;

                if (isTrailing && (confidence < 0.50f || candidate.expectedValue <= 0f))
                    continue;

                // Entry speed and alignment check
                if (vehicle.SpeedKmh < candidate.minSpeedKmh * 0.85f)
                    continue;

                float distToEntry = Mathf.Repeat(candidate.entryDistance - trackDistance, trackGraph.TotalLength);
                if (distToEntry < 35f && vehicle.IsGrounded && !vehicle.IsSpunOut)
                {
                    activeDiscoveredRoute = candidate.Clone();
                    shortcut = true;
                    routeEntered = false;
                    routeProgressAt = CurrentTime;
                    aerialAirStart = CurrentTime;
                    intent = "ExecutingDiscoveredAerialSkip";
                    break;
                }
            }
        }

        private void FollowDiscoveredRoute()
        {
            shortcut = true;
            drift = false;
            intent = "DiscoveredAerialTrajectory";

            var exitRoadNode = trackGraph.GetNode(activeDiscoveredRoute.exitNodeIndex);
            target = exitRoadNode.Position + Vector3.up * 0.5f;
            targetSpeed = Mathf.Max(targetSpeed, activeDiscoveredRoute.idealSpeedKmh);

            if (!vehicle.IsGrounded)
            {
                routeEntered = true;
            }
            else if (routeEntered)
            {
                // Touchdown after aerial skip!
                float timeSaved = activeDiscoveredRoute.timeSaved;
                bool safeLanding = Mathf.Abs(lateralOffset) <= exitRoadNode.TrackWidth * 0.48f;

                if (safeLanding)
                {
                    ShortcutKnowledgeBase.Instance?.RecordAttempt(activeDiscoveredRoute.routeId, true, timeSaved, raceProgress != null ? raceProgress.CurrentLap : 1);
                    ShortcutCompletions++;
                    activeDiscoveredRoute = null;
                    shortcut = false;
                }
                else
                {
                    ShortcutKnowledgeBase.Instance?.RecordAttempt(activeDiscoveredRoute.routeId, false, 0, raceProgress != null ? raceProgress.CurrentLap : 1);
                    AbortRoute("Discovered route landing off corridor");
                }
            }
        }

        float BrakingEnvelope(TrackNode current)
        {
            // Commanded pace/anticipation, never a change to the physical speed limit.
            float skill = profile.NormalizedSkill;
            float elite = Mathf.Pow(Mathf.InverseLerp(.90f, 1f, skill), 2f);
            bool lancer = vehicle.IsLancerActive;
            float efficiency = lancer ? 1f : .58f + .34f * Mathf.Pow(skill, .85f) + .08f * elite;
            float maxSpeed = (vehicle.IsLancerActive ? RaceItemUtility.LancerSpeedKmh : vehicle.Stats.SpeedKmh * (vehicle.IsBoosting ? vehicle.BoostMultiplier : 1)) * vehicle.ShortcutSpeedMultiplier;
            float speed = maxSpeed * efficiency;
            if (!lancer && profile.style == AiRacingStyle.SafeConsistency) speed *= 1f - .015f * skill;
            float deceleration = vehicle.Stats.AccelerationMps2 * 2.4f * Mathf.Lerp(.65f, .98f, profile.brakingSkill);
            float horizon = Mathf.Clamp(vehicle.SpeedMps * vehicle.SpeedMps / (2 * deceleration) + vehicle.SpeedMps * .45f, 35, 160);
            var previous = current;
            float driftBonus = vehicle.IsDrifting ? Mathf.Lerp(1, 1.3f, Mathf.Clamp01(vehicle.Stats.DriftMultiplier - .5f)) : 1f;
            for (int i = 1; i <= 10; i++)
            {
                float ahead = horizon * i / 10;
                var sample = trackGraph.SampleDistance(trackDistance + ahead);
                float curvature = Mathf.Abs(Vector3.SignedAngle(previous.Forward, sample.Forward, Vector3.up)) * Mathf.Deg2Rad / (horizon / 10);
                // Shared steering executes Handling * .5, including its bounded drift multiplier.
                // Predicting several times that yaw rate made late braking impossible.
                float cornerAuthority = vehicle.Stats.HandlingRadPerSec * .5f * driftBonus * Mathf.Lerp(.72f, .94f, profile.racingLineSkill);
                float corner = curvature > .0008f ? cornerAuthority / curvature : maxSpeed / 3.6f;
                float authored = sample.TargetSpeedKmh > 0 ? Mathf.Min(maxSpeed, sample.TargetSpeedKmh * Mathf.Lerp(.85f, 1.22f, profile.racingLineSkill)) / 3.6f : maxSpeed / 3.6f;
                // An explicit Lancer item must not fight the ordinary straight
                // pace target. Retain curvature and authored drift-zone braking.
                if (lancer && curvature < .001f && !sample.IsDriftZone) authored = maxSpeed / 3.6f;
                float limit = Mathf.Min(corner, authored) * efficiency;
                speed = Mathf.Min(speed, Mathf.Sqrt(limit * limit + 2 * deceleration * Mathf.Max(0, ahead - vehicle.SpeedMps * .15f)) * 3.6f);
                previous = sample;
            }
            return Mathf.Max(speed, 42f * vehicle.ShortcutSpeedMultiplier);
        }

        AiItemContext EvaluatePack(TrackNode road, float racingOffset, float turn)
        {
            var context = new AiItemContext();
            if (progressTracker == null) return context;
            float packLateral=lateralOffset;
            if(activeRoute!=null && routeEntered) {
                Vector3 start=activeRoute.pathPoints[routeSegment],segment=activeRoute.pathPoints[routeSegment+1]-start;
                road.Forward=segment.normalized;road.Right=Vector3.Cross(Vector3.up,segment).normalized;road.TrackWidth=activeRoute.WidthAt(routeSegment);
                packLateral=Vector3.Dot(transform.position-start,road.Right);racingOffset=0;
            }
            float baseLine = racingOffset;
            for (int i = 0; i < 3; i++) laneCosts[i] = i == 0 ? 0 : .05f;
            bool blocked = false, closingRear = false;
            float leadSpeed = float.MaxValue, leadGap = float.MaxValue, leadClosing = 0;
            trafficSpeedLimit = float.MaxValue;
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
                float predictedZ = ahead - closing * prediction, predictedX = packLateral + side + Vector3.Dot(racer.Vehicle.Body.linearVelocity, road.Right) * prediction;
                if (predictedZ < -4 || predictedZ > 24) continue;
                for (int i = 0; i < 3; i++)
                {
                    float separation = Mathf.Abs(predictedX - (baseLine + LaneOptions[i] * road.TrackWidth * .4f));
                    if (separation < 2.5f) laneCosts[i] += (2.5f - separation) / Mathf.Max(3, Mathf.Abs(predictedZ));
                }
                if (ahead > 0.5f && ahead < Mathf.Clamp(vehicle.SpeedMps * .85f, 12, 35) && Mathf.Abs(side) < 2.2f && closing > -.2f)
                {
                    blocked = true;
                    if (ahead < leadGap) { leadGap = ahead; leadSpeed = racer.Vehicle.SpeedKmh; leadClosing = closing; }
                }
            }
            context.overtakingOpportunity = blocked;
            if (blocked)
            {
                int best = laneCosts[1] < laneCosts[2] || (Mathf.Approximately(laneCosts[1], laneCosts[2]) && (profile.personalitySeed & 1) == 0) ? 1 : 2;
                int held = targetLane < 0 ? 1 : 2;
                if (CurrentTime < laneHoldUntil && laneCosts[held] <= laneCosts[best] + .16f) best = held;
                float laneCostTolerance = Mathf.Lerp(.02f, .08f, profile.aggression) +
                    (profile.style == AiRacingStyle.Opportunistic ? .015f * profile.NormalizedSkill : 0);
                if (profile.overtakingSkill > .15f && laneCosts[best] < laneCosts[0] + laneCostTolerance && Mathf.Abs(turn) < 40)
                {
                    if (CurrentTime >= laneHoldUntil) { PassingDecisions++; laneHoldUntil = CurrentTime + Mathf.Lerp(1.5f, 0.8f, profile.overtakingSkill); }
                    targetLane = LaneOptions[best] * Mathf.Lerp(.75f, 1.05f, profile.overtakingSkill) * Mathf.Lerp(.85f, 1.15f, profile.aggression);
                    intent = "CommittedPass";
                }
                // Brake from predicted closing time; a minimum pace floor previously
                // commanded 120 km/h even when the nearby lead car was almost stopped.
                float safetyGap = 3f + Mathf.Max(0, leadClosing) * Mathf.Lerp(.35f, .70f, profile.awareness);
                if (leadGap < safetyGap && leadSpeed < vehicle.SpeedKmh)
                {
                    trafficSpeedLimit = Mathf.Max(0, leadSpeed + (leadGap - 3f) * 2f);
                    targetSpeed = Mathf.Min(targetSpeed, trafficSpeedLimit);
                    TrafficBrakingDecisions++;
                }
            }
            else if (CurrentTime >= laneHoldUntil)
            {
                targetLane = (Mathf.Abs(profile.personalitySeed % 23) / 22f - .5f) * .15f;
                if (closingRear && profile.defensiveAwareness > .62f && Mathf.Abs(turn) > 8 && Mathf.Abs(turn) < 28 && laneCosts[0] < .18f)
                {
                    targetLane = Mathf.Sign(turn) * .12f;
                    DefensiveDecisions++;
                    intent = "ProtectInside";
                }
            }
            context.projectileThreat = profile.awareness > .4f && itemPool != null && itemPool.HasIncomingThreat(vehicle);
            return context;
        }

        void PlanRoute()
        {
            reserveBoost = false;
            if (rejoinDistance >= 0 || activeDiscoveredRoute != null) return;
            if (activeRoute != null)
            {
                reserveBoost = activeRoute.requiresBoost && (!routeEntered || (activeRoute.requiresJump ? routeSegment <= activeRoute.launchNodeIndex : routeSegment < 6));
                return;
            }
            var nearby = trackGraph.FindUpcomingShortcut(trackDistance, 150, out float distance);
            routeEntryDistance = distance;
            if (nearby == null) return;

            int pos = raceProgress != null ? raceProgress.RacePosition : 6;
            bool isLeading = pos == 1;
            bool isTrailing = pos >= 4;
            float riskBonus = isTrailing ? 0.20f : (isLeading ? -0.12f : 0.04f);
            float styleRisk=profile.style==AiRacingStyle.SafeConsistency?-.08f:profile.style==AiRacingStyle.ShortcutHeavy?.05f:0;
            float effectiveRiskTolerance = Mathf.Clamp01(profile.riskTolerance + riskBonus+styleRisk*profile.NormalizedSkill);
            if (profile.shortcutKnowledge < nearby.minSkillRequired || effectiveRiskTolerance < nearby.riskLevel) return;

            int lap = raceProgress != null ? raceProgress.CurrentLap : 1;
            if (consideredRoute != nearby || routeConsideredLap != lap)
            {
                consideredRoute = nearby;
                routeConsideredLap = lap;
                float stylePreference=profile.style==AiRacingStyle.ShortcutHeavy?.04f:profile.style==AiRacingStyle.SafeConsistency?-.04f:
                    profile.style==AiRacingStyle.Opportunistic&&isTrailing?.03f:0;
                float probability = Mathf.Clamp01(Mathf.Lerp(.15f, .998f, profile.shortcutExecution) * Mathf.Lerp(.7f, 1, profile.consistency)+stylePreference*profile.NormalizedSkill);
                if (isTrailing) probability = Mathf.Min(.99f, probability + .12f);
                if (raceProgress != null && lap == progressTracker.TotalLaps && raceProgress.RacePosition > 2) probability = 1f;
                routeSelected = rng.NextFloat() < probability;
            }
            if (!routeSelected) return;
            int index = Array.IndexOf(trackGraph.Shortcuts, nearby);
            if (index >= 0 && (routeAttemptMask & (1 << index)) != 0) return;
            // Knowledgeable drivers skip links that are slower than the road.
            // Calibrated on clean Night City sectors: braking to the setup speed and
            // the capped rejoin cost ~4-5 s that pure geometry omits (crest +10.1 s
            // estimated / +1.8 s measured; apex +1.3 s estimated / -3.2 s measured).
            if (index >= 0 && profile.shortcutKnowledge > .75f && routeValueSeconds != null && routeValueSeconds[index] < RouteOverheadSeconds) return;
            reserveBoost = nearby.requiresBoost;
            float launchSeconds = .4f;
            if (nearby.requiresJump)
            {
                float approach = distance;
                for (int i = 0; i < nearby.launchNodeIndex; i++) approach += Vector3.Distance(nearby.pathPoints[i], nearby.pathPoints[i + 1]);
                launchSeconds = approach / Mathf.Max(12, Mathf.Min(vehicle.SpeedMps, nearby.launchSpeedKmh / 3.6f)) + .35f;
            }
            bool resource = !nearby.requiresBoost || HasBoostItem() || vehicle.BoostRemainingSeconds > launchSeconds ||
                (!nearby.requiresJump && vehicle.IsDrifting && vehicle.CurrentMiniTurboStage != MiniTurboStage.None) ||
                (nearby.BoostSupply != null && nearby.BoostSupply.CanCollect(vehicle));

            if (distance < 38 && nearby.pathPoints != null && nearby.pathPoints.Length > 0)
            {
                Vector3 toEntrance = nearby.pathPoints[0] - transform.position;
                float entranceAngle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.ProjectOnPlane(toEntrance, Vector3.up));
                if (entranceAngle > 20f && !routeEntered) return;
            }

            if (distance < 65 && resource)
            {
                targetLane = 0;
                target = trackGraph.SampleDistance(trackDistance + lookAheadDistance).Position;
                float entrySpeed = RouteSetupSpeed(nearby) / 3.6f;
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(entrySpeed * entrySpeed + 2 * vehicle.Stats.AccelerationMps2 * 2.1f * Mathf.Max(0, distance - 8)) * 3.6f);
                intent = "PrepareShortcutApproach";
            }
            if (distance < 35 && resource && NextGateDistance() > distance && Mathf.Abs(lateralOffset) < trackGraph.GetNode(currentNode).TrackWidth * .27f && vehicle.IsGrounded && !vehicle.IsSpunOut)
            {
                // A launch link is a single 12 m deck: contacts there cost the
                // launch speed and the landing. Skip it while the deck is crowded.
                if (nearby.requiresJump && RacersOnRouteStart(nearby) >= 1)
                {
                    if (index >= 0) routeAttemptMask |= 1 << index;
                    LastRouteAbortReason = "Launch deck crowded";
                    reserveBoost = false;
                    return;
                }
                activeRoute = nearby;
                routeIndex = index;
                routeProgressAt = CurrentTime;
                routeSegment = 0;
                routeEntered = routeHopTaken = false;
            }
        }

        float NextGateDistance()
        {
            var gates = trackGraph.CheckpointNodeIndices;
            if (raceProgress == null || gates == null || gates.Length == 0) return float.MaxValue;
            int next = (raceProgress.LastCheckpointIndex + 1) % gates.Length;
            return Mathf.Repeat(trackGraph.GetNode(gates[next]).CumulativeDistance - trackDistance, trackGraph.TotalLength);
        }

        void FollowRoute()
        {
            shortcut = true;
            drift = false;
            intent = "CommittedShortcut";
            var points = activeRoute.pathPoints;
            if (CurrentTime - routeProgressAt > (routeEntered ? 3.5f : 5.5f))
            {
                AbortRoute("No forward route progress");
                FollowRejoin();
                return;
            }
            routeEntryDistance = Vector3.Distance(transform.position, points[0]);
            if (!routeEntered)
            {
                target = points[0];
                float entrySpeed = RouteSetupSpeed(activeRoute) / 3.6f;
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(entrySpeed * entrySpeed + 2 * vehicle.Stats.AccelerationMps2 * 2.1f * Mathf.Max(0, routeEntryDistance - 8)) * 3.6f);
                if (routeEntryDistance > Mathf.Max(7, vehicle.SpeedMps * .25f)) return;
                if (activeRoute.requiresBoost && !vehicle.IsBoosting && vehicle.CurrentMiniTurboStage == MiniTurboStage.None)
                {
                    if (inventory == null || !inventory.HasItem || !RaceItemUtility.IsShortcutBoost(inventory.HeldItem))
                        if (activeRoute.BoostSupply == null || !activeRoute.BoostSupply.CanCollect(vehicle)) { AbortRoute("Entry boost unavailable"); FollowRejoin(); return; }
                }
                routeEntered = true;
                ShortcutEntries++;
                routeProgressAt = CurrentTime;
                if (routeIndex >= 0) routeEntries[routeIndex]++;
                if (routeIndex >= 0) routeAttemptMask |= 1 << routeIndex;
            }
            Vector3 a = points[routeSegment], b = points[routeSegment + 1], delta = b - a;
            float fraction = delta.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / delta.sqrMagnitude) : 1;
            while ((fraction >= .995f || Vector3.Distance(transform.position, b) < 2) && routeSegment < points.Length - 2)
            {
                routeSegment++;
                routeProgressAt = CurrentTime;
                a = points[routeSegment];
                b = points[routeSegment + 1];
                delta = b - a;
                fraction = delta.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / delta.sqrMagnitude) : 1;
            }
            Vector3 right = Vector3.Cross(Vector3.up, delta).normalized;
            Vector3 projected = a + delta * fraction;
            float routeLateral = Vector3.Dot(transform.position - projected, right);
            if (Mathf.Abs(routeLateral) > activeRoute.WidthAt(routeSegment + fraction) * .5f + 2.5f || transform.position.y < projected.y - 3)
            {
                AbortRoute("Outside landing corridor");
                FollowRejoin();
                return;
            }
            // On the elevated approach the nearest main road is across a real void.
            // Retain the supported corridor while correcting alignment; steering
            // straight toward that main road was an unsafe abort, not a rejoin.
            var exitRoad = trackGraph.GetNode(activeRoute.exitNodeIndex);
            Vector3 toExit = transform.position - exitRoad.Position;
            bool enteredExitRoad = fraction > .95f && Mathf.Abs(Vector3.Dot(toExit, exitRoad.Right)) < exitRoad.TrackWidth * .43f &&
                Mathf.Abs(toExit.y) < 2f && Vector3.Dot(transform.forward, exitRoad.Forward) > .5f;
            if (routeSegment == points.Length - 2 && vehicle.IsGrounded &&
                enteredExitRoad)
            {
                if (routeIndex >= 0) routeCompletions[routeIndex]++;
                // Completion is observed here; time saving requires a matched legal reference.
                ShortcutKnowledgeBase.Instance?.RecordAttempt(activeRoute.shortcutId, true, 0, raceProgress != null ? raceProgress.CurrentLap : 1);
                activeRoute = null;
                routeIndex = -1;
                shortcut = reserveBoost = false;
                ShortcutCompletions++;
                rejoinDistance = exitRoad.CumulativeDistance + 15f;
                rejoinStableTime = 0;
                FollowRejoin();
                return;
            }
            lookAheadDistance = Mathf.Clamp(4 + vehicle.SpeedMps * .24f, 8, 18);
            float remaining = lookAheadDistance;
            target = a + delta * fraction;
            for (int s = routeSegment; s < points.Length - 1; s++)
            {
                Vector3 next = points[s + 1];
                float length = Vector3.Distance(target, next);
                if (length >= remaining) { target = Vector3.MoveTowards(target, next, remaining); remaining = 0; break; }
                remaining -= length;
                target = next;
            }
            if (remaining > 0 && routeSegment >= points.Length - 4)
            {
                // Continue through the portal. A stationary endpoint behind the car
                // previously requested a U-turn before the exit-heading test could pass.
                target = trackGraph.SampleDistance(exitRoad.CumulativeDistance + remaining).Position;
            }
            Vector3 routeRight = Vector3.Cross(Vector3.up, delta).normalized;
            // Traffic prediction and physical passing use the same route-relative lanes.
            // Main-road lateral coordinates are unrelated while on the elevated link.
            target += routeRight * Mathf.Clamp(lane * activeRoute.width * .4f, -activeRoute.width * .24f, activeRoute.width * .24f);
            targetSpeed = (vehicle.IsLancerActive ? RaceItemUtility.LancerSpeedKmh : vehicle.Stats.SpeedKmh * Mathf.Lerp(.68f, .995f, profile.shortcutExecution) * (vehicle.IsBoosting ? vehicle.BoostMultiplier : 1)) * vehicle.ShortcutSpeedMultiplier;
            float aheadDistance = 0, deceleration = vehicle.Stats.AccelerationMps2 * 2.0f;
            // Retain the current corner's limit through the final segment. Previously
            // the last bend lost its speed limit just as the car needed to finish turning.
            for (int s = Mathf.Max(0, routeSegment - 1); s < points.Length - 2 && aheadDistance < 85; s++)
            {
                if (s == routeSegment) aheadDistance = Vector3.Distance(transform.position, b);
                Vector3 before = points[s + 1] - points[s], after = points[s + 2] - points[s + 1];
                float curvature = Vector3.Angle(Vector3.ProjectOnPlane(before, Vector3.up), Vector3.ProjectOnPlane(after, Vector3.up)) * Mathf.Deg2Rad / Mathf.Max(1, (before.magnitude + after.magnitude) * .5f);
                if (curvature > .001f)
                {
                    float corner = vehicle.Stats.HandlingRadPerSec * .5f * Mathf.Lerp(.65f, .82f, profile.shortcutExecution) / curvature;
                    targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(corner * corner + 2 * deceleration * Mathf.Max(0, aheadDistance - vehicle.SpeedMps * .45f - 5f)) * 3.6f);
                }
                if (s >= routeSegment) aheadDistance += after.magnitude;
            }
            if (activeRoute.requiresJump && routeSegment >= 5)
                targetSpeed = Mathf.Min(targetSpeed, routeSegment >= 20 ? 95f : activeRoute.launchSpeedKmh);
            if (activeRoute.requiresJump && routeSegment < 8)
            {
                float setupSpeed = RouteSetupSpeed(activeRoute);
                targetSpeed = Mathf.Min(targetSpeed, setupSpeed);
            }
            if (activeRoute.requiresJump && routeSegment >= 8 && routeSegment < activeRoute.launchNodeIndex &&
                (LaunchAngleError(activeRoute) > 5 || Mathf.Abs(vehicle.SlipAngle) > 5))
                targetSpeed = Mathf.Min(targetSpeed, activeRoute.width < 8 ? 55 : 78);
        }

        int RacersOnRouteStart(TrackShortcut route)
        {
            if (progressTracker == null) return 0;
            int last = Mathf.Min(route.launchNodeIndex, route.pathPoints.Length - 1), count = 0;
            var board = progressTracker.Leaderboard;
            for (int r = 0; r < board.Count; r++)
            {
                var other = board[r].Vehicle;
                if (other == null || other == vehicle || board[r].Finished) continue;
                Vector3 position = other.transform.position;
                // Only genuine conflicts on the 12 m deck: a rival alongside, close
                // ahead, or slower ahead. A spaced car at pace is not a blocker, so
                // the gate does not reserve the link for whoever leads in clean air.
                Vector3 delta = position - transform.position;
                float ahead = Vector3.Dot(delta, transform.forward), side = Mathf.Abs(Vector3.Dot(delta, transform.right));
                float slower = vehicle.SpeedKmh - other.SpeedKmh;
                bool onDeck = false;
                for (int p = 0; p <= last && !onDeck; p += 2)
                    onDeck = (position - route.pathPoints[p]).sqrMagnitude < route.width * route.width;
                bool conflict = side < 5f && ahead > -6f && (ahead < 12f || (ahead < 25f && slower > 10f));
                if (conflict || (onDeck && ahead < 25f && slower > 10f)) count++;
            }
            return count;
        }

        bool HasBoostItem() => inventory != null && inventory.HasItem && RaceItemUtility.IsShortcutBoost(inventory.HeldItem);
        static float RouteSetupSpeed(TrackShortcut route) => route.requiresJump ? 72 : route.width < 8 ? 80 : route.width < 10 ? 100 : route.shortcutId == "overpass_launch" ? 100 : 90;
        float LaunchAngleError(TrackShortcut route) => Vector3.Angle(Vector3.ProjectOnPlane(vehicle.Body.linearVelocity, Vector3.up),
            Vector3.ProjectOnPlane(route.pathPoints[route.launchNodeIndex + 1] - route.pathPoints[route.launchNodeIndex], Vector3.up));

        void FollowRejoin(bool physicsTick = false)
        {
            if (physicsTick)
                trackGraph.ProjectPosition(transform.position, out currentNode, out trackDistance, out lateralOffset);
            var road = trackGraph.SampleDistance(rejoinDistance);
            Vector3 offset = transform.position - road.Position;
            if (offset.magnitude < Mathf.Max(8, lookAheadDistance) && Mathf.Abs(offset.y) < 2.5f &&
                Mathf.Abs(Vector3.Dot(offset, road.Right)) < road.TrackWidth * .45f && Vector3.Dot(offset, road.Forward) > -2)
            {
                rejoinDistance += Mathf.Max(8, lookAheadDistance);
                road = trackGraph.SampleDistance(rejoinDistance);
            }
            target = road.Position;
            targetLane = 0;
            drift = shortcut = reserveBoost = false;
            targetSpeed = Mathf.Min(targetSpeed, 78);
            intent = "CommittedRoadRejoin";
            var current = trackGraph.SampleDistance(trackDistance);
            bool clean = vehicle.IsGrounded && Mathf.Abs(lateralOffset) < current.TrackWidth * .30f &&
                Mathf.Abs(transform.position.y - current.Position.y) < 2 && Vector3.Dot(transform.forward, current.Forward) > .7f;
            if (physicsTick) rejoinStableTime = clean ? rejoinStableTime + Time.fixedDeltaTime : 0;
            if (rejoinStableTime > .5f) { rejoinDistance = -1; rejoinStableTime = 0; }
        }

        void AbortRoute(string reason = "Route cancelled")
        {
            if (activeRoute != null)
            {
                LastRouteAbortReason = reason;
                if (routeEntered) FailedShortcuts++;
                if (routeIndex >= 0) routeAttemptMask |= 1 << routeIndex;
                ShortcutKnowledgeBase.Instance?.RecordAttempt(activeRoute.shortcutId, false, 0, raceProgress != null ? raceProgress.CurrentLap : 1);
                trackGraph.ProjectPosition(transform.position, out _, out float distance, out _);
                rejoinDistance = distance + Mathf.Min(lookAheadDistance, NextGateDistance());
                rejoinStableTime = 0;
            }
            if (activeDiscoveredRoute != null)
            {
                LastRouteAbortReason = reason;
                if (routeEntered) FailedShortcuts++;
                ShortcutKnowledgeBase.Instance?.RecordAttempt(activeDiscoveredRoute.routeId, false, 0, raceProgress != null ? raceProgress.CurrentLap : 1);
                trackGraph.ProjectPosition(transform.position, out _, out float distance, out _);
                rejoinDistance = distance + Mathf.Min(lookAheadDistance, NextGateDistance());
                rejoinStableTime = 0;
            }
            activeRoute = null;
            activeDiscoveredRoute = null;
            routeIndex = -1;
            shortcut = reserveBoost = routeSelected = false;
        }

        public VehicleCommands GetCommands() => commands;
    }
}
