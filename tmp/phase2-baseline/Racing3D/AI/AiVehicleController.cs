using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class AiVehicleController : MonoBehaviour, IVehicleCommandProvider
    {
        [Header("Runtime Bindings")]
        [SerializeField] private TrackSplineGraph trackGraph;
        [SerializeField] private TrackProgressTracker progressTracker;

        private RacingVehiclePhysics vehicle;
        private ItemInventory inventory;
        private AiRacerProfile profile;
        private AiDecisionLogger logger;
        private AiItemAgent itemAgent;

        // Current target state
        private Vector3 currentTargetPoint;
        private float currentTargetSpeedKmh = 140f;
        private float currentTurnAngleDegrees;
        private bool isApproachingShortcut;
        private bool executingShortcut;
        private TrackShortcut activeShortcut;
        private int shortcutWaypointIndex;

        // Commands generated
        private VehicleCommands currentCommands;
        private float nextStrategicDecisionTime;
        private const float StrategicDecisionInterval = 0.10f; // 10 Hz strategic update

        // Perlin noise seeds for natural smooth error
        private float errorTimeSeed;
        private float lateralLaneOffset;
        private bool wantDrift;

        public AiRacerProfile Profile => profile;
        public AiDecisionLogger Logger => logger;

        private void Awake()
        {
            vehicle = GetComponent<RacingVehiclePhysics>();
            inventory = GetComponent<ItemInventory>();
            logger = new AiDecisionLogger();
            errorTimeSeed = UnityEngine.Random.Range(10f, 500f);
        }

        public void Initialize(
            AiRacerProfile aiProfile,
            TrackSplineGraph graph,
            TrackProgressTracker tracker,
            string botDisplayName = "AI")
        {
            profile = aiProfile;
            trackGraph = graph;
            progressTracker = tracker;

            if (logger != null) logger.BotName = botDisplayName;
            itemAgent = new AiItemAgent(profile, inventory, vehicle, trackGraph, logger);

            // Set provider on vehicle physics
            if (vehicle != null)
            {
                vehicle.SetCommandProvider(this);
            }
        }

        private void Update()
        {
            if (trackGraph == null || profile == null) return;

            // 1. Lower-frequency strategic decisions (10 Hz)
            if (Time.time >= nextStrategicDecisionTime)
            {
                nextStrategicDecisionTime = Time.time + StrategicDecisionInterval;
                UpdateStrategicDecisions();
            }

            // 2. High-frequency command generation
            GenerateCommands();
        }

        private void UpdateStrategicDecisions()
        {
            Vector3 myPos = transform.position;

            // 1. Project onto track
            trackGraph.ProjectPosition(myPos, out int nearestNodeIndex, out _, out _);
            TrackNode currentNode = trackGraph.GetNode(nearestNodeIndex);

            // 2. Look-ahead distance scales with current speed
            float speedMps = vehicle != null ? vehicle.SpeedMps : 20f;
            float lookAheadMeters = Mathf.Clamp(8f + (speedMps * 0.45f), 10f, 35f);

            // 3. Evaluate Shortcuts
            isApproachingShortcut = false;
            if (!executingShortcut)
            {
                TrackShortcut nearbyShortcut = trackGraph.FindShortcutNear(nearestNodeIndex);
                if (nearbyShortcut != null)
                {
                    isApproachingShortcut = true;
                    // Check if AI knows and wants to take this shortcut
                    bool knowsShortcut = profile.shortcutKnowledge >= nearbyShortcut.minSkillRequired;
                    bool willingRisk = profile.riskTolerance >= (nearbyShortcut.riskLevel * 0.7f);

                    if (knowsShortcut && willingRisk)
                    {
                        bool canTake = !nearbyShortcut.requiresBoost || (inventory != null && inventory.HasItem &&
                            (inventory.HeldItem == RaceItemType.TurboPulse || inventory.HeldItem == RaceItemType.HyperCharge));

                        if (canTake)
                        {
                            executingShortcut = true;
                            activeShortcut = nearbyShortcut;
                            shortcutWaypointIndex = 0;
                            logger?.UpdateSteeringDecision("UseShortcut", $"ShortcutKnown_Risk_{nearbyShortcut.riskLevel:F2}",
                                nearbyShortcut.pathPoints[0], 0f, 1f, false);
                        }
                    }
                }
            }

            // 4. Determine Target Navigation Point
            if (executingShortcut && activeShortcut != null)
            {
                if (shortcutWaypointIndex < activeShortcut.pathPoints.Length)
                {
                    currentTargetPoint = activeShortcut.pathPoints[shortcutWaypointIndex];
                    if (Vector3.Distance(myPos, currentTargetPoint) < 5.0f)
                    {
                        shortcutWaypointIndex++;
                    }
                }
                else
                {
                    // Finished shortcut
                    executingShortcut = false;
                    activeShortcut = null;
                }
            }
            else
            {
                // Natural smooth error offset (never sudden twitch, smooth harmonic wave)
                float noise = Mathf.PerlinNoise(errorTimeSeed, Time.time * 0.8f) - 0.5f;
                float naturalErrorOffset = noise * (1f - profile.consistency) * 0.35f;

                // Overtaking offset
                EvaluateOvertaking(out float overtakeOffset);

                float combinedOffset = Mathf.Clamp(naturalErrorOffset + overtakeOffset + lateralLaneOffset, -0.9f, 0.9f);
                currentTargetPoint = trackGraph.EvaluateTargetPoint(
                    nearestNodeIndex,
                    lookAheadMeters,
                    profile.racingLineSkill,
                    combinedOffset
                );
            }

            // 5. Evaluate Turn Angle and Speed Target
            Vector3 toTarget = currentTargetPoint - myPos;
            toTarget.y = 0f;
            currentTurnAngleDegrees = Vector3.Angle(transform.forward, toTarget);

            // Corner target speed: apexes require slowing down unless drifting
            currentTargetSpeedKmh = currentNode.TargetSpeedKmh > 0 ? currentNode.TargetSpeedKmh : 130f;
            if (currentTurnAngleDegrees > 40f)
            {
                currentTargetSpeedKmh = Mathf.Min(currentTargetSpeedKmh, 85f);
            }
            else if (currentTurnAngleDegrees > 20f)
            {
                currentTargetSpeedKmh = Mathf.Min(currentTargetSpeedKmh, 110f);
            }

            // 6. Drift Intent Evaluation
            wantDrift = false;
            if (currentTurnAngleDegrees > 24f && speedMps > 9f && profile.driftSkill > 0.40f)
            {
                wantDrift = true;
            }

            // 7. Tactical Item Usage
            EvaluateItemDecision(nearestNodeIndex);
        }

        private void EvaluateOvertaking(out float overtakeOffset)
        {
            overtakeOffset = 0f;
            if (profile.overtakingSkill < 0.3f) return;

            // Simple raycast forward to see if another vehicle is right ahead
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.SphereCast(origin, 1.2f, transform.forward, out RaycastHit hit, 14f))
            {
                var rival = hit.collider.GetComponentInParent<RacingVehiclePhysics>();
                if (rival != null && rival != vehicle)
                {
                    // Move laterally opposite to rival's local side
                    Vector3 localHit = transform.InverseTransformPoint(rival.transform.position);
                    overtakeOffset = (localHit.x >= 0 ? -0.45f : 0.45f) * profile.overtakingSkill;
                }
            }
        }

        private void EvaluateItemDecision(int nearestNodeIndex)
        {
            if (itemAgent == null || inventory == null || !inventory.HasItem) return;

            bool threatBehind = false;
            bool targetAheadInSight = false;

            // Check rival in rear proximity (15m)
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, -transform.forward, 15f))
            {
                threatBehind = true;
            }

            // Check rival ahead in clear line-of-sight
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hitFront, 40f))
            {
                var rival = hitFront.collider.GetComponentInParent<RacingVehiclePhysics>();
                if (rival != null && rival != vehicle)
                {
                    targetAheadInSight = true;
                }
            }

            bool shouldUse = itemAgent.ShouldUseItem(
                nearestNodeIndex,
                currentTurnAngleDegrees,
                isApproachingShortcut,
                threatBehind,
                targetAheadInSight,
                vehicle != null ? vehicle.SpeedKmh : 0f
            );

            if (shouldUse)
            {
                inventory.UseItem();
            }
        }

        private void GenerateCommands()
        {
            Vector3 localTarget = transform.InverseTransformPoint(currentTargetPoint);
            localTarget.y = 0f;

            // Steering command: proportional to lateral error
            float rawSteer = Mathf.Clamp(localTarget.x / 4.5f, -1f, 1f);

            // Throttle & Brake commands
            float currentSpeedKmh = vehicle != null ? vehicle.SpeedKmh : 0f;
            float throttle = 1f;
            float brake = 0f;

            if (currentSpeedKmh > currentTargetSpeedKmh + 5f)
            {
                // Need to slow down for upcoming apex
                if (profile.brakingSkill > 0.4f)
                {
                    throttle = 0f;
                    brake = Mathf.Clamp01((currentSpeedKmh - currentTargetSpeedKmh) / 25f);
                }
                else
                {
                    // Low braking skill: simply releases throttle late
                    throttle = 0.3f;
                    brake = 0f;
                }
            }

            // In-air pitch righting
            float pitch = 0f;
            if (vehicle != null && !vehicle.IsGrounded)
            {
                pitch = Mathf.Clamp(-transform.forward.y * 2f, -1f, 1f);
            }

            currentCommands = new VehicleCommands
            {
                Steering = rawSteer,
                Throttle = throttle,
                Brake = brake,
                Drift = wantDrift,
                UseItem = false,
                Pitch = pitch
            };

            logger?.UpdateSteeringDecision(
                wantDrift ? "PowerDriftApex" : (executingShortcut ? "ShortcutDrive" : "FollowRacingLine"),
                $"Angle_{currentTurnAngleDegrees:F1}_TgtSpeed_{currentTargetSpeedKmh:F0}",
                currentTargetPoint,
                rawSteer,
                throttle,
                wantDrift
            );
        }

        public VehicleCommands GetCommands() => currentCommands;
    }
}
