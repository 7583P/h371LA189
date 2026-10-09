using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class DiscoveredRoute
    {
        public string routeId;
        public string displayName;
        public int entryNodeIndex;
        public int exitNodeIndex;
        public float entryDistance;
        public float rejoinDistance;
        public float timeSaved;
        public float successRate;
        public float minSpeedKmh;
        public float idealSpeedKmh;
        public float entryAngle;
        public float lateralCorridor;
        public float jumpTiming;
        public float boostDuration;
        public float pitch;
        public float airRoll;
        public float landingCorridor;
        public float confidence;
        public float failureCost;
        public float expectedValue;
        public bool isAerialRoute;
        public bool requiresAerialBoost;
        public bool validatedSharedPhysics;
        public Vector3[] pathPoints;

        public DiscoveredRoute Clone()
        {
            return new DiscoveredRoute
            {
                routeId = this.routeId,
                displayName = this.displayName,
                entryNodeIndex = this.entryNodeIndex,
                exitNodeIndex = this.exitNodeIndex,
                entryDistance = this.entryDistance,
                rejoinDistance = this.rejoinDistance,
                timeSaved = this.timeSaved,
                successRate = this.successRate,
                minSpeedKmh = this.minSpeedKmh,
                idealSpeedKmh = this.idealSpeedKmh,
                entryAngle = this.entryAngle,
                lateralCorridor = this.lateralCorridor,
                jumpTiming = this.jumpTiming,
                boostDuration = this.boostDuration,
                pitch = this.pitch,
                airRoll = this.airRoll,
                landingCorridor = this.landingCorridor,
                confidence = this.confidence,
                failureCost = this.failureCost,
                expectedValue = this.expectedValue,
                isAerialRoute = this.isAerialRoute,
                requiresAerialBoost = this.requiresAerialBoost,
                validatedSharedPhysics = this.validatedSharedPhysics,
                pathPoints = this.pathPoints != null ? (Vector3[])this.pathPoints.Clone() : null
            };
        }
    }

    [Serializable]
    public struct RouteSessionMemory
    {
        public int attempts;
        public int successes;
        public int failures;
        public float averageTimeSaved;
        public float dynamicConfidence;
        public int lastObservedRacerId;
        public int lastAttemptLap;
    }

    public sealed class ShortcutKnowledgeBase : MonoBehaviour
    {
        private static ShortcutKnowledgeBase instance;
        public static ShortcutKnowledgeBase Instance => instance;

        [SerializeField] private List<DiscoveredRoute> globalExpertRoutes = new List<DiscoveredRoute>();
        private readonly Dictionary<string, RouteSessionMemory> sessionMemory = new Dictionary<string, RouteSessionMemory>(32);

        public IReadOnlyList<DiscoveredRoute> GlobalExpertRoutes => globalExpertRoutes;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeDefaults();
        }

        public static ShortcutKnowledgeBase GetOrCreate()
        {
            if (instance != null) return instance;
            var existing = FindFirstObjectByType<ShortcutKnowledgeBase>();
            if (existing != null) { instance = existing; return instance; }

            var go = new GameObject("Shortcut Knowledge Base");
            instance = go.AddComponent<ShortcutKnowledgeBase>();
            return instance;
        }

        public void InitializeDefaults()
        {
            if (globalExpertRoutes.Count == 0)
            {
                // Load expert catalog including offline discovered routes
                var routes = RouteCatalog.BuildExpertCatalog();
                for (int i = 0; i < routes.Length; i++)
                {
                    globalExpertRoutes.Add(routes[i]);
                }
            }
            ResetSession();
        }

        public void RegisterDiscoveredRoutes(IEnumerable<DiscoveredRoute> routes)
        {
            if (routes == null) return;
            foreach (var r in routes)
            {
                int existingIdx = globalExpertRoutes.FindIndex(x => x.routeId == r.routeId);
                if (existingIdx >= 0)
                {
                    globalExpertRoutes[existingIdx] = r;
                }
                else
                {
                    globalExpertRoutes.Add(r);
                }
            }
            ResetSession();
        }

        public void ResetSession()
        {
            sessionMemory.Clear();
            for (int i = 0; i < globalExpertRoutes.Count; i++)
            {
                var route = globalExpertRoutes[i];
                sessionMemory[route.routeId] = new RouteSessionMemory
                {
                    attempts = 0,
                    successes = 0,
                    failures = 0,
                    averageTimeSaved = route.timeSaved,
                    dynamicConfidence = route.confidence,
                    lastObservedRacerId = -1,
                    lastAttemptLap = -1
                };
            }
        }

        public float GetConfidence(string routeId)
        {
            if (sessionMemory.TryGetValue(routeId, out var mem))
            {
                return mem.dynamicConfidence;
            }
            var route = globalExpertRoutes.Find(r => r.routeId == routeId);
            return route != null ? route.confidence : 0.5f;
        }

        public void RecordAttempt(string routeId, bool success, float timeSaved, int lap)
        {
            if (!sessionMemory.TryGetValue(routeId, out var mem))
            {
                var baseRoute = globalExpertRoutes.Find(r => r.routeId == routeId);
                float initialConf = baseRoute != null ? baseRoute.confidence : 0.5f;
                mem = new RouteSessionMemory { dynamicConfidence = initialConf, averageTimeSaved = baseRoute != null ? baseRoute.timeSaved : timeSaved };
            }

            mem.attempts++;
            mem.lastAttemptLap = lap;
            if (success)
            {
                mem.successes++;
                mem.dynamicConfidence = Mathf.Clamp01(mem.dynamicConfidence + 0.08f);
                mem.averageTimeSaved = Mathf.Lerp(mem.averageTimeSaved, timeSaved, 0.35f);
            }
            else
            {
                mem.failures++;
                mem.dynamicConfidence = Mathf.Clamp01(mem.dynamicConfidence - 0.16f);
            }

            sessionMemory[routeId] = mem;
        }

        // Layer 5: Observational Learning
        // If an elite racer or human opponent executes a valid route,
        // observing bots increase candidate confidence.
        public void ObserveRacerRoute(int racerId, string routeId, float timeSaved, bool legal)
        {
            if (!legal || string.IsNullOrEmpty(routeId)) return;

            if (!sessionMemory.TryGetValue(routeId, out var mem))
            {
                var baseRoute = globalExpertRoutes.Find(r => r.routeId == routeId);
                float initialConf = baseRoute != null ? baseRoute.confidence : 0.6f;
                mem = new RouteSessionMemory { dynamicConfidence = initialConf, averageTimeSaved = baseRoute != null ? baseRoute.timeSaved : timeSaved };
            }

            mem.lastObservedRacerId = racerId;
            // Observing expert human or rival bot success builds confidence
            mem.dynamicConfidence = Mathf.Clamp01(mem.dynamicConfidence + 0.12f);
            mem.averageTimeSaved = Mathf.Lerp(mem.averageTimeSaved, timeSaved, 0.25f);
            sessionMemory[routeId] = mem;
        }

        /// <summary>
        /// Queries known routes near current distance along the track.
        /// Non-allocating query filling the provided buffer.
        /// </summary>
        public void GetCandidateRoutes(float currentTrackDist, float horizon, float totalTrackLength, List<DiscoveredRoute> results)
        {
            results.Clear();
            if (totalTrackLength <= 0) return;

            for (int i = 0; i < globalExpertRoutes.Count; i++)
            {
                var route = globalExpertRoutes[i];
                float ahead = Mathf.Repeat(route.entryDistance - currentTrackDist, totalTrackLength);
                if (ahead <= horizon)
                {
                    results.Add(route);
                }
            }
        }
    }

    public static class RouteCatalog
    {
        public static DiscoveredRoute[] BuildExpertCatalog()
        {
            // Catalog combines authored track shortcuts + offline discovered aerial routes
            return new DiscoveredRoute[]
            {
                // Discovered Route 01: Grand Avenue Hop to Sector 1 Sweeper skip
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_001",
                    displayName = "AVENUE CREST EXTENSION",
                    entryNodeIndex = 172,
                    exitNodeIndex = 188,
                    entryDistance = 1840f,
                    rejoinDistance = 2010f,
                    timeSaved = 2.45f,
                    successRate = 0.94f,
                    minSpeedKmh = 120f,
                    idealSpeedKmh = 142f,
                    entryAngle = 2.0f,
                    lateralCorridor = 4.0f,
                    jumpTiming = 0.05f,
                    boostDuration = 1.8f,
                    pitch = 14.5f,
                    airRoll = -4.0f,
                    landingCorridor = 8.5f,
                    confidence = 0.95f,
                    failureCost = 1.5f,
                    expectedValue = (2.45f * 0.94f) - (1.5f * 0.06f) - 0.1f - 0.2f, // 2.09
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 02: Skyline Crest Aerial Hairpin Bypass
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_002",
                    displayName = "SKYLINE CANYON AIR SKIP",
                    entryNodeIndex = 10,
                    exitNodeIndex = 38,
                    entryDistance = 110f,
                    rejoinDistance = 420f,
                    timeSaved = 3.12f,
                    successRate = 0.91f,
                    minSpeedKmh = 118f,
                    idealSpeedKmh = 138f,
                    entryAngle = -3.5f,
                    lateralCorridor = 3.5f,
                    jumpTiming = 0.08f,
                    boostDuration = 2.2f,
                    pitch = 16.0f,
                    airRoll = -6.5f,
                    landingCorridor = 9.0f,
                    confidence = 0.92f,
                    failureCost = 2.2f,
                    expectedValue = (3.12f * 0.91f) - (2.2f * 0.09f) - 0.15f - 0.25f, // 2.24
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 03: Apex Gallery Drop-In Line
                new DiscoveredRoute
                {
                    routeId = "GroundRoute_003",
                    displayName = "APEX GALLERY CUT",
                    entryNodeIndex = 74,
                    exitNodeIndex = 94,
                    entryDistance = 810f,
                    rejoinDistance = 1025f,
                    timeSaved = 1.85f,
                    successRate = 0.97f,
                    minSpeedKmh = 85f,
                    idealSpeedKmh = 112f,
                    entryAngle = 4.2f,
                    lateralCorridor = 5.0f,
                    jumpTiming = 0.0f,
                    boostDuration = 0.0f,
                    pitch = 0.0f,
                    airRoll = 0.0f,
                    landingCorridor = 10.0f,
                    confidence = 0.96f,
                    failureCost = 1.0f,
                    expectedValue = (1.85f * 0.97f) - (1.0f * 0.03f) - 0.05f - 0.0f, // 1.71
                    isAerialRoute = false,
                    requiresAerialBoost = false
                },
                // Discovered Route 04: Skyline Overpass Aerial Bridge Cut
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_004",
                    displayName = "OVERPASS CREST LAUNCH",
                    entryNodeIndex = 48,
                    exitNodeIndex = 62,
                    entryDistance = 530f,
                    rejoinDistance = 680f,
                    timeSaved = 2.10f,
                    successRate = 0.89f,
                    minSpeedKmh = 125f,
                    idealSpeedKmh = 145f,
                    entryAngle = -1.5f,
                    lateralCorridor = 3.0f,
                    jumpTiming = 0.02f,
                    boostDuration = 1.5f,
                    pitch = 12.0f,
                    airRoll = 3.0f,
                    landingCorridor = 7.5f,
                    confidence = 0.88f,
                    failureCost = 1.8f,
                    expectedValue = (2.10f * 0.89f) - (1.8f * 0.11f) - 0.12f - 0.2f, // 1.35
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 05: Midnight Run Elevation Skip
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_005",
                    displayName = "MIDNIGHT CANYON LEAP",
                    entryNodeIndex = 116,
                    exitNodeIndex = 132,
                    entryDistance = 1270f,
                    rejoinDistance = 1445f,
                    timeSaved = 2.28f,
                    successRate = 0.92f,
                    minSpeedKmh = 130f,
                    idealSpeedKmh = 148f,
                    entryAngle = 2.5f,
                    lateralCorridor = 4.0f,
                    jumpTiming = 0.06f,
                    boostDuration = 1.9f,
                    pitch = 15.0f,
                    airRoll = -5.0f,
                    landingCorridor = 8.0f,
                    confidence = 0.90f,
                    failureCost = 1.6f,
                    expectedValue = (2.28f * 0.92f) - (1.6f * 0.08f) - 0.10f - 0.22f, // 1.62
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 06: Grand Avenue Chicanes Flight Line
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_006",
                    displayName = "AVENUE S-BEND BYPASS",
                    entryNodeIndex = 140,
                    exitNodeIndex = 154,
                    entryDistance = 1530f,
                    rejoinDistance = 1680f,
                    timeSaved = 1.92f,
                    successRate = 0.93f,
                    minSpeedKmh = 122f,
                    idealSpeedKmh = 140f,
                    entryAngle = -2.0f,
                    lateralCorridor = 3.8f,
                    jumpTiming = 0.04f,
                    boostDuration = 1.4f,
                    pitch = 11.5f,
                    airRoll = 4.5f,
                    landingCorridor = 8.0f,
                    confidence = 0.91f,
                    failureCost = 1.4f,
                    expectedValue = (1.92f * 0.93f) - (1.4f * 0.07f) - 0.08f - 0.18f, // 1.43
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 07: Downtown Hairpin Launch Skip
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_007",
                    displayName = "DOWNTOWN HAIRPIN JUMP",
                    entryNodeIndex = 22,
                    exitNodeIndex = 34,
                    entryDistance = 240f,
                    rejoinDistance = 375f,
                    timeSaved = 1.76f,
                    successRate = 0.95f,
                    minSpeedKmh = 115f,
                    idealSpeedKmh = 135f,
                    entryAngle = 1.2f,
                    lateralCorridor = 4.2f,
                    jumpTiming = 0.03f,
                    boostDuration = 1.2f,
                    pitch = 10.0f,
                    airRoll = -2.0f,
                    landingCorridor = 9.0f,
                    confidence = 0.93f,
                    failureCost = 1.2f,
                    expectedValue = (1.76f * 0.95f) - (1.2f * 0.05f) - 0.06f - 0.15f, // 1.40
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 08: Titanium Bridge Air Roll Glide
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_008",
                    displayName = "TITANIUM BRIDGE AIR GLIDE",
                    entryNodeIndex = 98,
                    exitNodeIndex = 112,
                    entryDistance = 1070f,
                    rejoinDistance = 1220f,
                    timeSaved = 2.05f,
                    successRate = 0.90f,
                    minSpeedKmh = 128f,
                    idealSpeedKmh = 144f,
                    entryAngle = -3.0f,
                    lateralCorridor = 3.5f,
                    jumpTiming = 0.05f,
                    boostDuration = 1.6f,
                    pitch = 13.0f,
                    airRoll = -8.0f,
                    landingCorridor = 7.8f,
                    confidence = 0.89f,
                    failureCost = 1.7f,
                    expectedValue = (2.05f * 0.90f) - (1.7f * 0.10f) - 0.10f - 0.20f, // 1.38
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 09: Sector 4 High-Speed Ramp Arc
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_009",
                    displayName = "SECTOR 4 HIGH ARC SKIP",
                    entryNodeIndex = 158,
                    exitNodeIndex = 170,
                    entryDistance = 1720f,
                    rejoinDistance = 1850f,
                    timeSaved = 1.65f,
                    successRate = 0.96f,
                    minSpeedKmh = 120f,
                    idealSpeedKmh = 138f,
                    entryAngle = 0.5f,
                    lateralCorridor = 4.5f,
                    jumpTiming = 0.02f,
                    boostDuration = 1.1f,
                    pitch = 9.5f,
                    airRoll = 1.5f,
                    landingCorridor = 9.2f,
                    confidence = 0.94f,
                    failureCost = 1.1f,
                    expectedValue = (1.65f * 0.96f) - (1.1f * 0.04f) - 0.05f - 0.14f, // 1.35
                    isAerialRoute = true,
                    requiresAerialBoost = true
                },
                // Discovered Route 10: Skyline Apex Drop Rejoin
                new DiscoveredRoute
                {
                    routeId = "AerialRoute_010",
                    displayName = "SKYLINE APEX PLUNGE",
                    entryNodeIndex = 64,
                    exitNodeIndex = 76,
                    entryDistance = 700f,
                    rejoinDistance = 830f,
                    timeSaved = 1.58f,
                    successRate = 0.94f,
                    minSpeedKmh = 110f,
                    idealSpeedKmh = 130f,
                    entryAngle = 2.8f,
                    lateralCorridor = 4.0f,
                    jumpTiming = 0.04f,
                    boostDuration = 1.0f,
                    pitch = 8.0f,
                    airRoll = -3.5f,
                    landingCorridor = 8.5f,
                    confidence = 0.92f,
                    failureCost = 1.2f,
                    expectedValue = (1.58f * 0.94f) - (1.2f * 0.06f) - 0.07f - 0.12f, // 1.22
                    isAerialRoute = true,
                    requiresAerialBoost = true
                }
            };
        }
    }
}
