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
        float blockedTime, reverseUntil, nextReverse, laneHoldUntil, mistakeUntil, mistakeOffset, routeEntryDistance;
        int targetNode, currentNode, routeSegment, recoverySeen, routeConsideredLap = -1;
        TrackShortcut activeRoute, consideredRoute;
        bool drift, shortcut, routeEntered, reserveBoost, routeSelected;
        string intent = "RacingLine";
        readonly float[] laneCosts = new float[3];
        static readonly float[] LaneOptions = { 0, -.40f, .40f };
        public AiRacerProfile Profile => profile;
        public AiDecisionLogger Logger => logger;
        public Vector3 CurrentTarget => target;
        public float DecisionHz => 10;
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
        public bool ReservingBoost => reserveBoost;
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
        }
        void FixedUpdate()
        {
            if (profile == null || trackGraph == null || !vehicle.RaceEnabled) { commands = VehicleCommands.Idle; launchTime = -1; return; }
            if (recovery != null && recovery.IsRecovering) { commands = VehicleCommands.Idle; return; }
            if (recovery != null && recovery.RecoveryCount != recoverySeen)
            { recoverySeen = recovery.RecoveryCount; activeRoute = null; shortcut = drift = false; steering = blockedTime = reverseUntil = nextDecision = 0; }
            if (launchTime < 0) launchTime = Time.time + Mathf.Lerp(.65f, .06f, profile.reactionSkill) + Mathf.Abs(profile.personalitySeed % 17) * .003f;
            if (Time.time < launchTime) { commands = VehicleCommands.Idle; return; }
            if (Time.time >= nextDecision) { nextDecision = Time.time + .1f; using (DecisionMarker.Auto()) Decide(); }
            lane = Mathf.MoveTowards(lane, targetLane, Time.fixedDeltaTime * Mathf.Lerp(.22f, .65f, profile.awareness));
            Vector3 local = transform.InverseTransformPoint(target);
            float distance = Mathf.Max(4, new Vector2(local.x, local.z).magnitude);
            float desiredYaw = 2 * Mathf.Max(7, vehicle.SpeedMps) * local.x / (distance * distance);
            float desiredSteer = Mathf.Clamp(desiredYaw / (vehicle.Stats.HandlingRadPerSec * .5f), -1, 1);
            if (local.z < 0) desiredSteer = Mathf.Sign(local.x);
            if (vehicle.IsDrifting)
            {
                float authority = Mathf.Lerp(1, 1.3f, Mathf.Clamp01(vehicle.Stats.DriftMultiplier - .5f));
                desiredSteer = Mathf.Clamp((desiredSteer / authority - vehicle.DriftDirection * .56f) / .55f, -1, 1);
            }
            steering = Mathf.MoveTowards(steering, desiredSteer, Time.fixedDeltaTime * Mathf.Lerp(3.8f, 10, profile.racingLineSkill));
            float overspeed = vehicle.SpeedKmh - targetSpeed;
            float throttle = Mathf.Lerp(.65f, 1, profile.throttleSkill) * Mathf.Clamp01(1 - Mathf.Max(0, overspeed - 3) / 15);
            float brake = overspeed > 8 ? Mathf.Clamp01((overspeed - 8) / 20) * Mathf.Lerp(.55f, 1, profile.brakingSkill) : 0;
            if (vehicle.IsDrifting && (brake > .45f || Mathf.Abs(lateralOffset) > trackGraph.GetNode(currentNode).TrackWidth * .35f)) drift = false;
            bool trick = !vehicle.IsGrounded && profile.trickSkill > .22f &&
                vehicle.AirborneTime > Mathf.Lerp(.35f, .16f, profile.trickSkill) &&
                vehicle.TrickCount < (profile.trickSkill > .88f && vehicle.AirborneTime > .67f ? 2 : 1);
            commands = new VehicleCommands { Steering = steering, Throttle = throttle, Brake = brake, Drift = drift, Trick = trick,
                Pitch = !vehicle.IsGrounded ? Mathf.Clamp(-transform.forward.y * 1.4f, -.5f, .5f) : 0 };
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
            var current = trackGraph.SampleDistance(trackDistance);
            lookAheadDistance = Mathf.Clamp(6 + vehicle.SpeedMps * Mathf.Lerp(.35f, .47f, profile.racingLineSkill), 9, 28);
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
            trackGraph.ProjectPosition(future.Position, out targetNode, out _, out _);
            shortcut = false; PlanRoute();
            if (activeRoute != null) FollowRoute();
            else if (Mathf.Abs(lateral) > current.TrackWidth * .34f)
            { targetLane = 0; target = future.Position; targetSpeed = Mathf.Min(targetSpeed, 78); intent = "RecoverLine"; }
            angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.ProjectOnPlane(target - transform.position, Vector3.up));
            if (angle > 45) targetSpeed = Mathf.Min(targetSpeed, Mathf.Lerp(48, 65, profile.recoverySkill));
            if (vehicle.IsDrifting)
            { if (Mathf.Abs(turn) < 7 || Mathf.Sign(turn) != vehicle.DriftDirection || shortcut || angle > 35 || Mathf.Abs(lateral) > current.TrackWidth * .33f) drift = false; }
            else drift = activeRoute == null && profile.driftSkill > .32f && Mathf.Abs(turn) > 11 && Mathf.Abs(turn) < 45 &&
                vehicle.SpeedKmh > 50 && Mathf.Abs(lateral) < current.TrackWidth * .28f && !vehicle.IsSpunOut;
            if (drift) intent = "DriftToExitBoost";
            if (reserveBoost && vehicle.IsDrifting && vehicle.CurrentMiniTurboStage != MiniTurboStage.None && routeEntryDistance < 18) drift = false;
            traffic.reserveBoost = reserveBoost; traffic.shortcutEntryReady = activeRoute != null && routeEntryDistance < 14 && !routeEntered && angle < 22;
            traffic.turnDegrees = Mathf.Max(Mathf.Abs(turn), angle);
            traffic.finalLap = raceProgress != null && raceProgress.CurrentLap == progressTracker.TotalLaps;
            traffic.position = raceProgress != null ? raceProgress.RacePosition : 12;
            if (inventory != null && inventory.HasItem && itemAgent.ShouldUseItem(traffic)) inventory.UseItem();
            logger.UpdateSteeringDecision(intent, reserveBoost ? "PreserveShortcutResource" : "TrackAndPredictedTraffic", target, steering, commands.Throttle, drift);
        }
        float BrakingEnvelope(TrackNode current)
        {
            float efficiency = Mathf.Lerp(.70f, 1, profile.racingLineSkill), maxSpeed = vehicle.Stats.SpeedKmh * (vehicle.IsBoosting ? vehicle.BoostMultiplier : 1);
            float speed = Mathf.Min(maxSpeed, current.TargetSpeedKmh > 0 ? current.TargetSpeedKmh * efficiency : maxSpeed * efficiency);
            float deceleration = vehicle.Stats.AccelerationMps2 * 2.4f * Mathf.Lerp(.55f, .90f, profile.brakingSkill);
            float horizon = Mathf.Clamp(vehicle.SpeedMps * vehicle.SpeedMps / (2 * deceleration) + vehicle.SpeedMps * .5f, 28, 95);
            var previous = current;
            for (int i = 1; i <= 5; i++)
            {
                float ahead = horizon * i / 5; var sample = trackGraph.SampleDistance(trackDistance + ahead);
                float curvature = Mathf.Abs(Vector3.SignedAngle(previous.Forward, sample.Forward, Vector3.up)) * Mathf.Deg2Rad / (horizon / 5);
                float corner = curvature > .001f ? vehicle.Stats.HandlingRadPerSec * .43f / curvature : maxSpeed / 3.6f;
                float authored = sample.TargetSpeedKmh > 0 ? sample.TargetSpeedKmh / 3.6f : maxSpeed / 3.6f;
                float limit = Mathf.Min(corner, authored) * efficiency;
                speed = Mathf.Min(speed, Mathf.Sqrt(limit * limit + 2 * deceleration * Mathf.Max(0, ahead - vehicle.SpeedMps * .15f)) * 3.6f); previous = sample;
            }
            if (vehicle.IsBoosting && trackCurvature < .006f) speed = Mathf.Min(maxSpeed, speed * vehicle.BoostMultiplier);
            return speed;
        }
        AiItemContext EvaluatePack(TrackNode road, float racingOffset, float turn)
        {
            var context = new AiItemContext(); if (progressTracker == null) return context;
            float baseLine = racingOffset * road.TrackWidth * .4f;
            for (int i = 0; i < 3; i++) laneCosts[i] = i == 0 ? 0 : .05f;
            bool blocked = false, closingRear = false; float leadSpeed = float.MaxValue, leadGap = float.MaxValue;
            foreach (var racer in progressTracker.Leaderboard)
            {
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
                if (profile.overtakingSkill > .25f && laneCosts[best] < laneCosts[0] + .04f && Mathf.Abs(turn) < 32)
                {
                    if (Time.time >= laneHoldUntil) { PassingDecisions++; laneHoldUntil = Time.time + Mathf.Lerp(1.8f, 1.1f, profile.overtakingSkill); }
                    targetLane = LaneOptions[best] * Mathf.Lerp(.65f, 1, profile.overtakingSkill); intent = "CommittedPass";
                }
                if (leadGap < 9 || laneCosts[best] >= laneCosts[0] + .04f)
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
            if (activeRoute != null) { reserveBoost = activeRoute.requiresBoost && !routeEntered; return; }
            var nearby = trackGraph.FindUpcomingShortcut(trackDistance, 150, out float distance); routeEntryDistance = distance;
            if (nearby == null || profile.shortcutKnowledge < nearby.minSkillRequired || profile.riskTolerance < nearby.riskLevel) return;
            int lap = raceProgress != null ? raceProgress.CurrentLap : 1;
            if (consideredRoute != nearby || routeConsideredLap != lap)
            {
                consideredRoute = nearby; routeConsideredLap = lap;
                float probability = Mathf.Lerp(.05f, .96f, profile.shortcutExecution) * Mathf.Lerp(.6f, 1, profile.consistency);
                if (raceProgress != null && lap == progressTracker.TotalLaps && raceProgress.RacePosition > 3) probability = Mathf.Min(.98f, probability + .06f);
                routeSelected = rng.NextFloat() < probability;
            }
            if (!routeSelected) return;
            reserveBoost = nearby.requiresBoost;
            bool resource = !nearby.requiresBoost || vehicle.BoostRemainingSeconds > .4f ||
                (inventory != null && inventory.HasItem && (inventory.HeldItem == RaceItemType.TurboPulse || inventory.HeldItem == RaceItemType.HyperCharge)) ||
                (vehicle.IsDrifting && vehicle.CurrentMiniTurboStage != MiniTurboStage.None);
            if (distance < 30 && resource && vehicle.IsGrounded && !vehicle.IsSpunOut) { activeRoute = nearby; routeSegment = 0; routeEntered = false; }
        }
        void FollowRoute()
        {
            shortcut = true; drift = false; intent = "CommittedShortcut"; var points = activeRoute.pathPoints;
            routeEntryDistance = Vector3.Distance(transform.position, points[0]);
            if (!routeEntered)
            {
                target = points[0];
                if (routeEntryDistance > Mathf.Max(7, vehicle.SpeedMps * .25f)) return;
                if (activeRoute.requiresBoost && !vehicle.IsBoosting)
                {
                    if (vehicle.IsDrifting && vehicle.CurrentMiniTurboStage != MiniTurboStage.None) { drift = false; return; }
                    if (inventory == null || !inventory.HasItem || (inventory.HeldItem != RaceItemType.TurboPulse && inventory.HeldItem != RaceItemType.HyperCharge) || !inventory.UseItem())
                    { activeRoute = null; shortcut = false; return; }
                }
                routeEntered = true; ShortcutEntries++;
            }
            // Route stays committed after boost expiry; targets always advance in order.
            Vector3 a = points[routeSegment], b = points[routeSegment + 1], delta = b - a;
            float fraction = delta.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / delta.sqrMagnitude) : 1;
            while ((fraction > .90f || Vector3.Distance(transform.position, b) < 7) && routeSegment < points.Length - 2)
            { routeSegment++; a = points[routeSegment]; b = points[routeSegment + 1]; delta = b - a; fraction = delta.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(transform.position - a, delta) / delta.sqrMagnitude) : 1; }
            if (routeSegment == points.Length - 2 && (fraction > .97f || Vector3.Distance(transform.position, b) < 6))
            { activeRoute = null; shortcut = reserveBoost = false; ShortcutCompletions++; return; }
            float remaining = lookAheadDistance; target = a + delta * fraction;
            for (int s = routeSegment; s < points.Length - 1; s++)
            { Vector3 next = points[s + 1]; float length = Vector3.Distance(target, next); if (length >= remaining) { target = Vector3.MoveTowards(target, next, remaining); break; } remaining -= length; target = next; }
            targetSpeed = vehicle.Stats.SpeedKmh * Mathf.Lerp(.68f, .94f, profile.shortcutExecution) * (vehicle.IsBoosting ? vehicle.BoostMultiplier : 1);
            if (routeSegment < points.Length - 2 && Vector3.Angle(delta, points[routeSegment + 2] - b) > 30 && Vector3.Distance(transform.position, b) < 30) targetSpeed = Mathf.Min(targetSpeed, 90);
        }
        public VehicleCommands GetCommands() => commands;
    }
}
