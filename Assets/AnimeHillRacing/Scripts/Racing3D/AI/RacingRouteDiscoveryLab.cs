using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    /// <summary>
    /// OFFLINE / EDITOR-ONLY Route Discovery Lab.
    /// Simulates physics-accurate aerial and physical trajectories to discover
    /// high-skill shortcuts, skips, and boost lines without runtime gameplay cost.
    /// </summary>
    public static class RacingRouteDiscoveryLab
    {
        public struct SimulationConfig
        {
            public float entrySpeedKmh;
            public float lateralOffset;
            public float yawAngle;
            public float pitchDeg;
            public float airRollDeg;
            public float aerialBoostDuration;
            public float aerialBoostDelay;
            public bool useJumpPad;
        }

        public struct SimulationResult
        {
            public bool valid;
            public string invalidReason;
            public int entryNodeIndex;
            public int exitNodeIndex;
            public float entryDistance;
            public float exitDistance;
            public float airTime;
            public float normalTrackTime;
            public float routeTime;
            public float timeSaved;
            public float landingCorridorWidth;
            public float landingPitchError;
            public float landingSpeedKmh;
            public float successProbability;
            public float failurePenalty;
            public float trafficRisk;
            public float boostCost;
            public float expectedValue;
            public Vector3[] flightPath;
            public SimulationConfig config;
        }

        private const float Gravity = 22f;
        private const float MaxBoostDuration = 3.0f;
        private const float BoostAccelMps2 = 14f * 2.4f; // 33.6 mps2

        public static SimulationResult SimulateCandidate(TrackSplineGraph graph, int startNodeIdx, SimulationConfig cfg)
        {
            var res = new SimulationResult { config = cfg, entryNodeIndex = startNodeIdx };
            if (graph == null || graph.NodeCount < 10)
            {
                res.invalidReason = "InvalidGraph";
                return res;
            }

            var startNode = graph.GetNode(startNodeIdx);
            res.entryDistance = startNode.CumulativeDistance;

            float speedMps = cfg.entrySpeedKmh / 3.6f;
            Vector3 startPos = startNode.Position + startNode.Right * cfg.lateralOffset + Vector3.up * 0.4f;
            Quaternion rot = Quaternion.LookRotation(startNode.Forward, Vector3.up) * Quaternion.Euler(-cfg.pitchDeg, cfg.yawAngle, cfg.airRollDeg);
            Vector3 vel = rot * Vector3.forward * speedMps;

            if (cfg.useJumpPad)
            {
                vel += Vector3.up * 6.5f;
            }
            else
            {
                vel += Vector3.up * 3.8f;
            }

            float dt = 0.02f;
            float maxTime = 4.5f;
            float elapsed = 0f;
            Vector3 pos = startPos;
            var points = new List<Vector3>(128) { pos };

            int groundLayer = ~0; // Query full environment geometry
            bool landed = false;
            int landingNode = -1;

            float boostTimeRemaining = Mathf.Min(cfg.aerialBoostDuration, MaxBoostDuration);
            float boostDelayRemaining = cfg.aerialBoostDelay;

            while (elapsed < maxTime && !landed)
            {
                elapsed += dt;

                // Orientation during flight follows initial launch + air roll / pitch adjustments
                Vector3 forward = rot * Vector3.forward;

                // Aerial boost application
                if (boostDelayRemaining > 0f)
                {
                    boostDelayRemaining -= dt;
                }
                else if (boostTimeRemaining > 0f)
                {
                    boostTimeRemaining -= dt;
                    // Orientation-following thrust
                    vel += forward * (BoostAccelMps2 * dt);
                }

                // Gravity & Drag
                vel.y -= Gravity * dt;
                vel.x *= (1f - 0.015f * dt);
                vel.z *= (1f - 0.015f * dt);

                Vector3 nextPos = pos + vel * dt;
                Vector3 stepDelta = nextPos - pos;
                float stepDist = stepDelta.magnitude;

                Vector3 landingPoint = Vector3.zero;
                bool hitSurface = false;

                if (elapsed > 0.35f)
                {
                    if (stepDist > 0.001f && Physics.SphereCast(pos, 0.35f, stepDelta.normalized, out var hit, stepDist, groundLayer, QueryTriggerInteraction.Ignore))
                    {
                        if (Vector3.Dot(vel.normalized, hit.normal) < -0.1f && hit.normal.y > 0.3f)
                        {
                            landingPoint = hit.point;
                            hitSurface = true;
                        }
                    }
                    else if (vel.y <= 0f)
                    {
                        // Analytical spline road surface intersection
                        graph.ProjectPosition(nextPos, out int checkNodeIdx, out _, out float checkLat);
                        var checkRoad = graph.GetNode(checkNodeIdx);
                        float relY_prev = pos.y - checkRoad.Position.y;
                        float relY_curr = nextPos.y - checkRoad.Position.y;
                        if (relY_prev >= 0.0f && relY_curr <= 0.25f)
                        {
                            if (Mathf.Abs(checkLat) <= checkRoad.TrackWidth * 0.48f)
                            {
                                landingPoint = new Vector3(nextPos.x, checkRoad.Position.y + 0.15f, nextPos.z);
                                hitSurface = true;
                            }
                        }
                    }
                }

                if (hitSurface)
                {
                    graph.ProjectPosition(landingPoint, out int hitNodeIdx, out float hitTrackDist, out float hitLateral);
                    var roadNode = graph.GetNode(hitNodeIdx);

                    float progressSkipped = Mathf.Repeat(hitTrackDist - startNode.CumulativeDistance, graph.TotalLength);

                    if (progressSkipped > 20f && progressSkipped < 550f)
                    {
                        int checkpointsSkipped = CountCheckpointsBetween(graph, startNodeIdx, hitNodeIdx);
                        if (checkpointsSkipped <= 2)
                        {
                            if (Mathf.Abs(hitLateral) <= roadNode.TrackWidth * 0.48f)
                            {
                                landed = true;
                                landingNode = hitNodeIdx;
                                res.exitNodeIndex = hitNodeIdx;
                                res.exitDistance = hitTrackDist;
                                res.landingCorridorWidth = roadNode.TrackWidth * 0.9f;
                                res.landingPitchError = Mathf.Abs(Vector3.Angle(vel.normalized, roadNode.Forward));
                                res.landingSpeedKmh = vel.magnitude * 3.6f;
                                points.Add(landingPoint);
                                break;
                            }
                        }
                        else
                        {
                            res.invalidReason = "TooManyCheckpointsSkipped";
                            break;
                        }
                    }
                    else if (elapsed > 1.0f && progressSkipped <= 20f && vel.y <= -2f)
                    {
                        res.invalidReason = "TooShortOrSameSection";
                        break;
                    }
                }

                // Fail-safe: Under-map check
                if (nextPos.y < -30f)
                {
                    res.invalidReason = "FellUnderMap";
                    break;
                }

                pos = nextPos;
                points.Add(pos);
            }

            if (!landed)
            {
                if (string.IsNullOrEmpty(res.invalidReason)) res.invalidReason = "MissedTrackLanding";
                return res;
            }

            // Route time vs Normal track time
            float skippedDist = Mathf.Repeat(res.exitDistance - res.entryDistance, graph.TotalLength);
            float normalAverageSpeedMps = 38f; // ~137 km/h average on track
            res.airTime = elapsed;
            res.normalTrackTime = skippedDist / normalAverageSpeedMps;
            // Route time = air travel time + 0.3s landing stabilization
            res.routeTime = res.airTime + 0.30f;
            res.timeSaved = res.normalTrackTime - res.routeTime;

            if (res.timeSaved < 0.4f)
            {
                res.invalidReason = "NoMeasurableTimeSaved";
                return res;
            }

            res.flightPath = points.ToArray();
            res.valid = true;
            return res;
        }

        private static int CountCheckpointsBetween(TrackSplineGraph graph, int startIdx, int endIdx)
        {
            int count = 0;
            var gates = graph.CheckpointNodeIndices;
            if (gates == null || gates.Length == 0) return 0;

            float total = graph.TotalLength;
            float startDist = graph.GetNode(startIdx).CumulativeDistance;
            float endDist = graph.GetNode(endIdx).CumulativeDistance;
            float distDelta = Mathf.Repeat(endDist - startDist, total);

            for (int i = 0; i < gates.Length; i++)
            {
                float gateDist = graph.GetNode(gates[i]).CumulativeDistance;
                float toGate = Mathf.Repeat(gateDist - startDist, total);
                if (toGate > 0f && toGate < distDelta)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Runs perturbation analysis around a candidate config to determine success probability,
        /// failure penalty, and expected value.
        /// </summary>
        public static SimulationResult EvaluateRobustness(TrackSplineGraph graph, int startNodeIdx, SimulationConfig baseCfg)
        {
            var baseRes = SimulateCandidate(graph, startNodeIdx, baseCfg);
            if (!baseRes.valid) return baseRes;

            int samples = 12;
            int successes = 0;
            float totalTimeSaved = 0f;

            for (int i = 0; i < samples; i++)
            {
                // Small physical execution variances (approx 1% variance in speed, +-1.5 deg pitch/yaw)
                float speedVar = (i % 3 - 1) * 2.5f;
                float pitchVar = ((i / 3) % 3 - 1) * 1.5f;
                float rollVar = (i % 2 == 0 ? 1 : -1) * 1.0f;

                var perturbed = baseCfg;
                perturbed.entrySpeedKmh += speedVar;
                perturbed.pitchDeg += pitchVar;
                perturbed.airRollDeg += rollVar;

                var test = SimulateCandidate(graph, startNodeIdx, perturbed);
                if (test.valid && (Mathf.Abs(test.exitNodeIndex - baseRes.exitNodeIndex) <= 2 || Mathf.Abs(test.exitDistance - baseRes.exitDistance) <= 15f))
                {
                    successes++;
                    totalTimeSaved += test.timeSaved;
                }
            }

            baseRes.successProbability = (float)successes / samples;
            baseRes.failurePenalty = 1.8f; // Standard off-track / recovery time penalty
            baseRes.trafficRisk = 0.12f;
            baseRes.boostCost = baseCfg.aerialBoostDuration > 0 ? 0.20f : 0.05f;

            float failureProb = 1f - baseRes.successProbability;
            baseRes.expectedValue = (baseRes.timeSaved * baseRes.successProbability)
                                  - (baseRes.failurePenalty * failureProb)
                                  - baseRes.trafficRisk
                                  - baseRes.boostCost;

            return baseRes;
        }

        /// <summary>
        /// Offline Route Discovery Scan.
        /// Evaluates elevated track sections, jumps, and corners to produce candidate routes.
        /// </summary>
        public static List<DiscoveredRoute> DiscoverRoutes(TrackSplineGraph graph, out string report)
        {
            var discovered = new List<DiscoveredRoute>();
            var results = new List<SimulationResult>();

            if (graph == null)
            {
                report = "Error: TrackSplineGraph is null.";
                return discovered;
            }

            // Key interest points along Grand Circuit:
            // Jumps, high crests, sharp hairpin approach ridges across the full track
            int totalNodes = graph.NodeCount;
            var candidateNodesList = new List<int>();
            int nodeStep = Mathf.Max(2, totalNodes / 48);
            for (int i = 2; i < totalNodes - 4; i += nodeStep)
            {
                candidateNodesList.Add(i);
            }
            int[] candidateStartNodes = candidateNodesList.ToArray();

            float[] speeds = new float[] { 115f, 130f, 142f };
            float[] offsets = new float[] { -5f, 0f, 5f };
            float[] pitches = new float[] { 10f, 15f, 20f };
            float[] yaws = new float[] { -10f, -5f, 0f, 5f, 10f };
            float[] rolls = new float[] { -10f, 0f, 10f };
            float[] boosts = new float[] { 0.8f, 1.5f, 2.2f };

            int routeCounter = 1;

            for (int n = 0; n < candidateStartNodes.Length; n++)
            {
                int startNode = candidateStartNodes[n];
                if (startNode >= graph.NodeCount) continue;

                SimulationResult bestForNode = default;
                float bestEv = 0f;

                for (int s = 0; s < speeds.Length; s++)
                {
                    for (int o = 0; o < offsets.Length; o++)
                    {
                        for (int p = 0; p < pitches.Length; p++)
                        {
                            for (int y = 0; y < yaws.Length; y++)
                            {
                                for (int r = 0; r < rolls.Length; r++)
                                {
                                    for (int b = 0; b < boosts.Length; b++)
                                    {
                                        var cfg = new SimulationConfig
                                        {
                                            entrySpeedKmh = speeds[s],
                                            lateralOffset = offsets[o],
                                            yawAngle = yaws[y],
                                            pitchDeg = pitches[p],
                                            airRollDeg = rolls[r],
                                            aerialBoostDuration = boosts[b],
                                            aerialBoostDelay = 0.05f,
                                            useJumpPad = (startNode == graph.MainJumpNodeIndex || startNode == 10 || startNode == 48 || startNode == 172)
                                        };

                                        // Fast Coarse Filter: Only 1 simulation first
                                        var coarse = SimulateCandidate(graph, startNode, cfg);
                                        if (!coarse.valid || coarse.timeSaved < 0.45f) continue;

                                        // Fine Robustness Test: Only executed on valid, time-saving candidates
                                        var sim = EvaluateRobustness(graph, startNode, cfg);
                                        if (sim.valid && sim.expectedValue > bestEv && sim.successProbability >= 0.60f)
                                        {
                                            bestEv = sim.expectedValue;
                                            bestForNode = sim;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (bestForNode.valid && bestForNode.expectedValue > 0f)
                {
                    results.Add(bestForNode);
                    var route = new DiscoveredRoute
                    {
                        routeId = $"AerialRoute_{routeCounter:D3}",
                        displayName = $"DISCOVERED AIR SKIP {routeCounter:D2}",
                        entryNodeIndex = bestForNode.entryNodeIndex,
                        exitNodeIndex = bestForNode.exitNodeIndex,
                        entryDistance = bestForNode.entryDistance,
                        rejoinDistance = bestForNode.exitDistance,
                        timeSaved = bestForNode.timeSaved,
                        successRate = bestForNode.successProbability,
                        minSpeedKmh = bestForNode.config.entrySpeedKmh - 12f,
                        idealSpeedKmh = bestForNode.config.entrySpeedKmh,
                        entryAngle = bestForNode.config.yawAngle,
                        lateralCorridor = bestForNode.landingCorridorWidth * 0.4f,
                        jumpTiming = 0.04f,
                        boostDuration = bestForNode.config.aerialBoostDuration,
                        pitch = bestForNode.config.pitchDeg,
                        airRoll = bestForNode.config.airRollDeg,
                        landingCorridor = bestForNode.landingCorridorWidth,
                        confidence = Mathf.Clamp(bestForNode.successProbability, 0.7f, 0.98f),
                        failureCost = bestForNode.failurePenalty,
                        expectedValue = bestForNode.expectedValue,
                        isAerialRoute = true,
                        requiresAerialBoost = bestForNode.config.aerialBoostDuration > 0f,
                        pathPoints = bestForNode.flightPath
                    };
                    discovered.Add(route);
                    routeCounter++;
                }
            }

            // Sort by Expected Value / Time Saved descending
            discovered.Sort((a, b) => b.expectedValue.CompareTo(a.expectedValue));

            // Generate report
            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine("       ROUTE DISCOVERY LAB: DISCOVERED ROUTES");
            sb.AppendLine("==================================================");
            sb.AppendLine($"Total Valid Routes Found: {discovered.Count}");
            sb.AppendLine();
            sb.AppendLine("TOP 10 FASTEST VALID DISCOVERED ROUTES:");
            sb.AppendLine("---------------------------------------------------------------------------------------------------------");
            sb.AppendLine(string.Format("{0,-16} | {1,-9} | {2,-7} | {3,-10} | {4,-6} | {5,-7} | {6,-8} | {7,-8} | {8,-6}",
                "Route ID", "TimeSaved", "Success", "EntrySpeed", "Pitch", "AirRoll", "BoostDur", "Rejoin", "ExpVal"));
            sb.AppendLine("---------------------------------------------------------------------------------------------------------");

            int topCount = Mathf.Min(10, discovered.Count);
            for (int i = 0; i < topCount; i++)
            {
                var r = discovered[i];
                sb.AppendLine(string.Format("{0,-16} | {1,7:F2}s | {2,6:P1} | {3,8:F0} km/h | {4,5:F1}° | {5,6:F1}° | {6,6:F1}s | Node {7,-3} | {8,6:F2}",
                    r.routeId, r.timeSaved, r.successRate, r.idealSpeedKmh, r.pitch, r.airRoll, r.boostDuration, r.exitNodeIndex, r.expectedValue));
            }
            sb.AppendLine("---------------------------------------------------------------------------------------------------------");

            report = sb.ToString();
            Debug.Log(report);
            return discovered;
        }
    }
}
