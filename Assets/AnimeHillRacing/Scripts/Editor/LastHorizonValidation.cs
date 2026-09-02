using System;
using System.Collections.Generic;
using System.Linq;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class LastHorizonValidation
    {
        private const string MapPath = "Assets/AnimeHillRacing/MapProfiles/LastHorizon.asset";
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";

        [MenuItem("Anime Hill Racing/Validate Last Horizon")]
        public static void RunFromMenu() => Validate();

        public static void RunBatch()
        {
            try
            {
                Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Validate()
        {
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(MapPath);
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Require(profile != null, $"Missing map profile at {MapPath}");
            Require(catalog != null, $"Missing catalog at {CatalogPath}");
            Require(profile.mapId == "last-horizon" && profile.displayName == "LAST HORIZON",
                "Map identity is incorrect.");
            Require(profile.coinPrice == 100000 && !profile.unlockedByDefault,
                "Last Horizon must be locked and cost exactly 100000 coins.");
            Require(profile.leaderboardId == "CgkIv97xxuweEAIQCA",
                "Last Horizon Google Play leaderboard ID is missing or incorrect.");
            Require(catalog.maps != null && catalog.maps.Contains(profile),
                "Last Horizon is not registered in GameCatalog.");
            Require(profile.useLastHorizonTerrain && profile.lastHorizon != null,
                "Last Horizon terrain settings are not enabled.");
            ValidateVisuals(profile);
            ValidateCycles(profile, out float averageVariantDifference,
                out float maximumVariantDifference);
            ValidateProcessedTerrain(profile, 120000f, out float maximumProcessedSlope,
                out float maximumProcessedStep);
            ValidateFuel(profile, 160000f, out int normalFuelCount, out int scarcityFuelCount,
                out float maximumFuelGap);
            ValidateSpecialPhases(profile, 120000f, out int roofPhases,
                out int transportPhases, out int voidPhases, out int movingPhases);
            ValidateObstacles(profile, catalog, 120000f, out int cycleOneObstacles,
                out int cycleTwoObstacles);
            ValidateLongDistance(profile);

            Debug.Log($"LAST HORIZON VALIDATION PASSED | variant avg/max delta " +
                      $"{averageVariantDifference:F2}/{maximumVariantDifference:F2} m | " +
                      $"processed slope/step max {maximumProcessedSlope:F2} deg/" +
                      $"{maximumProcessedStep:F3} m | fuel normal/20km-reset " +
                      $"{normalFuelCount}/{scarcityFuelCount}, max gap {maximumFuelGap:F2} m | " +
                      $"phases roof/transport/void/moving " +
                      $"{roofPhases}/{transportPhases}/{voidPhases}/{movingPhases} | " +
                      $"obstacles C1/C2 {cycleOneObstacles}/{cycleTwoObstacles} | " +
                      "long-distance sample 1,000,000 m finite.");
        }

        private static void ValidateVisuals(MapProfile profile)
        {
            Require(profile.terrainMaterial != null && profile.terrainEdgeMaterial != null,
                "Terrain surface/subsurface materials are missing.");
            Require(profile.terrainMaterial == profile.terrainEdgeMaterial,
                "Surface and subsurface must use the same pure-black material.");
            Material material = profile.terrainMaterial;
            Require(material.shader != null &&
                    material.shader.name == "AnimeHillRacing/LastHorizonTerrain",
                "The pure-black Last Horizon shader is not assigned.");
            Require(material.shader.isSupported,
                "The pure-black Last Horizon shader is unsupported by the active render pipeline.");
            Require(material.HasProperty("_BaseColor") == false ||
                    IsPureBlack(material.GetColor("_BaseColor")),
                "Terrain base color is not pure black.");
            Require(material.HasProperty("_Color") == false ||
                    IsPureBlack(material.GetColor("_Color")),
                "Terrain tint is not pure black.");
            Require(profile.selectionPreview != null, "Track selection preview is missing.");
            Require(profile.coinRoute != null, "The existing deterministic coin route is not assigned.");
            EnvironmentDefinition environment = profile.environment;
            Require(environment != null && environment.environmentId == "last-horizon",
                "Last Horizon environment is missing.");
            Require(environment.overrideCameraBackground && environment.mountains != null,
                "Last Horizon background/parallax sprite is missing.");
            Require(environment.cameraBackgroundColor.r < .1f &&
                    environment.cameraBackgroundColor.g < .1f &&
                    environment.cameraBackgroundColor.b < .1f,
                "Camera background must remain dark gray.");
            LastHorizonSettings settings = profile.lastHorizon;
            Require(settings.cycleLength == LastHorizonSettings.RequiredCycleLength &&
                    settings.cycleResetFuelInterval == LastHorizonSettings.RequiredSystemLength,
                "Last Horizon must use 10 km cycles and a 20 km fuel reset interval.");
            Require(settings.seesawSprite != null && settings.liftPlatformSprite != null &&
                    settings.rollerSprite != null, "One or more obstacle sprites are missing.");
            Require(settings.minimumRoofZonesPerCycle == 2 &&
                    settings.maximumRoofZonesPerCycle == 3 &&
                    settings.roofLengthRange.x >= 200f && settings.roofLengthRange.y <= 5000f,
                "Roof phases must occur 2-3 times per 10 km and last 200-5000 m.");
            Require(settings.roofTransportGapLength >= 24f &&
                    settings.roofTransportGapLength <= 64f &&
                    settings.roofTransportPlatformWidth < settings.roofTransportGapLength &&
                    settings.roofCrusherBlockCount >= 2 &&
                    LastHorizonSpecialPhaseEvent.RoofTransportDurationSeconds == 2f,
                "The roof transport/crusher contract is invalid.");
            Require(settings.movingTerrainInterval >= 5000f &&
                    settings.movingTerrainInterval <= 7000f &&
                    settings.voidPlatformInterval >= 7000f &&
                    settings.voidPlatformInterval <= 10000f,
                "Moving/void phase intervals are outside their requested ranges.");
            Require(Mathf.Approximately(settings.extremeBumpAngle, 80f),
                "The small extreme bump must keep its requested 80 degree face.");
        }

        private static void ValidateSpecialPhases(MapProfile profile, float distance,
            out int roofCount, out int transportCount, out int voidCount, out int movingCount)
        {
            var phases = new List<LastHorizonSpecialPhaseLayout>();
            TerrainGenerator.GetLastHorizonSpecialPhases(profile, 0f, distance, phases);
            Require(phases.Count > 0, "Special phase schedule is empty.");
            roofCount = 0;
            transportCount = 0;
            voidCount = 0;
            movingCount = 0;
            float previousEnd = -1f;
            float lastVoidStart = -1f;
            float lastMovingStart = -1f;
            int cycleCount = Mathf.FloorToInt(distance / LastHorizonSettings.RequiredCycleLength);
            var roofsPerCycle = new int[cycleCount];
            for (int i = 0; i < phases.Count; i++)
            {
                LastHorizonSpecialPhaseLayout phase = phases[i];
                Require(phase.IsValid, $"Invalid special phase at schedule index {i}.");
                Require(phase.start >= previousEnd,
                    $"Special phases overlap near {phase.start:F0} m.");
                previousEnd = phase.end;
                Require(TerrainGenerator.TryGetLastHorizonSpecialPhase(profile,
                            (phase.start + phase.end) * .5f, out LastHorizonSpecialPhaseLayout repeated) &&
                        repeated.key == phase.key,
                    $"Special phase {phase.key} is not deterministic/queryable.");

                switch (phase.type)
                {
                    case LastHorizonSpecialPhaseType.RoofTunnel:
                        roofCount++;
                        if (phase.cycleIndex < roofsPerCycle.Length) roofsPerCycle[phase.cycleIndex]++;
                        Require(phase.Length >= 200f - .01f && phase.Length <= 5000f + .01f,
                            $"Roof duration out of range at {phase.start:F0} m.");
                        Require(!phase.ContainsGap((phase.start + phase.end) * .5f),
                            "A roof phase incorrectly removes the ground.");
                        break;
                    case LastHorizonSpecialPhaseType.RoofTransportCrusher:
                        transportCount++;
                        if (phase.cycleIndex < roofsPerCycle.Length) roofsPerCycle[phase.cycleIndex]++;
                        Require(phase.Length >= 200f - .01f && phase.Length <= 5000f + .01f,
                            $"Roof transport duration out of range at {phase.start:F0} m.");
                        Require(TerrainGenerator.IsTerrainGap(profile,
                                (phase.GapStart + phase.GapEnd) * .5f),
                            "Roof transport phase did not carve its platform void.");
                        Require(Mathf.Abs((phase.GapEnd - phase.GapStart) -
                                          profile.lastHorizon.roofTransportGapLength) < .02f,
                            "Roof transport gap does not match its configured length.");
                        break;
                    case LastHorizonSpecialPhaseType.DescendingVoidPlatforms:
                        voidCount++;
                        Require(TerrainGenerator.IsTerrainGap(profile,
                                (phase.GapStart + phase.GapEnd) * .5f),
                            "Descending platform phase did not carve a real void.");
                        if (lastVoidStart >= 0f)
                        {
                            float interval = phase.start - lastVoidStart;
                            Require(interval >= 7000f - .01f && interval <= 10000f + .01f,
                                $"Void phase interval out of range: {interval:F1} m.");
                        }
                        lastVoidStart = phase.start;
                        break;
                    case LastHorizonSpecialPhaseType.MovingExtremeTerrain:
                        movingCount++;
                        Require(TerrainGenerator.IsTerrainGap(profile,
                                (phase.GapStart + phase.GapEnd) * .5f),
                            "Moving extreme terrain did not replace the ordinary ground.");
                        if (lastMovingStart >= 0f)
                        {
                            float interval = phase.start - lastMovingStart;
                            Require(interval >= 5000f - .01f && interval <= 7000f + .01f,
                                $"Moving phase interval out of range: {interval:F1} m.");
                        }
                        lastMovingStart = phase.start;
                        break;
                }
            }
            for (int cycle = 0; cycle < roofsPerCycle.Length; cycle++)
                Require(roofsPerCycle[cycle] >= 2 && roofsPerCycle[cycle] <= 3,
                    $"Cycle {cycle} contains {roofsPerCycle[cycle]} roof phases instead of 2-3.");
            Require(roofCount > 0 && transportCount > 0 && voidCount > 0 && movingCount > 0,
                "The schedule does not contain every special phase family.");
        }

        private static void ValidateCycles(MapProfile profile, out float averageDifference,
            out float maximumDifference)
        {
            Require(TerrainGenerator.GetLastHorizonCycleType(0f) == 0 &&
                    TerrainGenerator.GetLastHorizonCycleType(9999.9f) == 0 &&
                    TerrainGenerator.GetLastHorizonCycleType(10000f) == 1 &&
                    TerrainGenerator.GetLastHorizonCycleType(19999.9f) == 1 &&
                    TerrainGenerator.GetLastHorizonCycleType(20000f) == 0,
                "The required C1 -> C2 -> C1 order is broken.");

            float segmentLength = profile.lastHorizon.segmentLength;
            int segmentsPerCycle = Mathf.RoundToInt(LastHorizonSettings.RequiredCycleLength /
                                                     segmentLength);
            float totalDifference = 0f;
            maximumDifference = 0f;
            int comparisons = 0;
            var familiesPerCycle = new[] { new HashSet<int>(), new HashSet<int>() };
            for (int cycle = 0; cycle < 2; cycle++)
            for (int segment = 0; segment < segmentsPerCycle; segment++)
            {
                float localStart = cycle * LastHorizonSettings.RequiredCycleLength +
                                   segment * segmentLength;
                float centreA = localStart + segmentLength * .5f;
                float centreB = centreA + LastHorizonSettings.RequiredSystemLength;
                int variantA = TerrainGenerator.GetLastHorizonSegmentVariant(profile, centreA);
                int variantB = TerrainGenerator.GetLastHorizonSegmentVariant(profile, centreB);
                familiesPerCycle[cycle].Add(variantA / 4);
                Require(variantA != variantB,
                    $"Segment {segment} of cycle {cycle + 1} repeated its preceding variant.");
                for (int sample = 1; sample <= 5; sample++)
                {
                    float t = sample / 6f;
                    float a = localStart + segmentLength * t;
                    float b = a + LastHorizonSettings.RequiredSystemLength;
                    float heightA = TerrainGenerator.SampleLastHorizonHeight(profile, a);
                    float heightB = TerrainGenerator.SampleLastHorizonHeight(profile, b);
                    Require(heightA == TerrainGenerator.SampleLastHorizonHeight(profile, a) &&
                            heightB == TerrainGenerator.SampleLastHorizonHeight(profile, b),
                        "Terrain is not deterministic for a repeated sample.");
                    float difference = Mathf.Abs(heightA - heightB);
                    totalDifference += difference;
                    maximumDifference = Mathf.Max(maximumDifference, difference);
                    comparisons++;
                }
            }
            averageDifference = totalDifference / Mathf.Max(1, comparisons);
            Require(averageDifference >= 6f && maximumDifference >= 25f,
                $"Repeated cycles are not perceptibly different enough ({averageDifference:F2}/" +
                $"{maximumDifference:F2} m average/max delta).");
            Require(familiesPerCycle[0].Count == 4 && familiesPerCycle[1].Count == 4,
                "Both 10 km cycles must include mountain, mesa, valley and playground chapters.");

            const float seamEpsilon = .001f;
            foreach (float seam in new[] { 10000f, 20000f, 30000f, 40000f })
            {
                float before = TerrainGenerator.SampleLastHorizonHeight(profile, seam - seamEpsilon);
                float after = TerrainGenerator.SampleLastHorizonHeight(profile, seam + seamEpsilon);
                Require(Mathf.Abs(after - before) < .25f,
                    $"Terrain discontinuity detected at {seam:F0} m.");
            }
        }

        private static void ValidateProcessedTerrain(MapProfile profile, float distance,
            out float maximumSlope, out float maximumStep)
        {
            const float chunkLength = 80f;
            const int samples = 128;
            float dx = chunkLength / samples;
            maximumSlope = 0f;
            maximumStep = 0f;
            bool hasBoundary = false;
            float boundaryHeight = 0f;
            float boundarySlope = 0f;
            Mesh mesh = new Mesh { name = "Last Horizon Validation Mesh" };
            try
            {
                int chunks = Mathf.CeilToInt(distance / chunkLength);
                for (int chunk = 0; chunk < chunks; chunk++)
                {
                    TerrainGenerator.BuildMesh(mesh, profile, chunk * chunkLength, chunkLength,
                        samples, 10f, hasBoundary, boundaryHeight, boundarySlope,
                        out float[] heights, out float endHeight, out float endSlope);
                    Require(heights != null && heights.Length == samples + 1,
                        "Terrain mesh returned an invalid surface.");
                    if (hasBoundary)
                        Require(Mathf.Abs(heights[0] - boundaryHeight) < .002f,
                            $"Chunk seam mismatch at {chunk * chunkLength:F0} m.");
                    for (int i = 0; i < heights.Length; i++)
                    {
                        Require(!float.IsNaN(heights[i]) && !float.IsInfinity(heights[i]),
                            $"Non-finite terrain height at chunk {chunk}, sample {i}.");
                        if (i == 0) continue;
                        float step = Mathf.Abs(heights[i] - heights[i - 1]);
                        float slope = Mathf.Abs(Mathf.Atan2(step, dx) * Mathf.Rad2Deg);
                        maximumStep = Mathf.Max(maximumStep, step);
                        maximumSlope = Mathf.Max(maximumSlope, slope);
                    }
                    boundaryHeight = endHeight;
                    boundarySlope = endSlope;
                    hasBoundary = true;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            Require(maximumSlope <= profile.maxSlopeAngle + .05f,
                $"Processed slope exceeded {profile.maxSlopeAngle:F1} degrees.");
            Require(maximumStep <= profile.maxHeightDeltaPerSample + .002f,
                "Processed terrain exceeded the maximum per-sample height delta.");
        }

        private static void ValidateFuel(MapProfile profile, float distance, out int normalCount,
            out int scarcityCount, out float maximumGap)
        {
            var sequence = new LastHorizonFuelSequence();
            var points = new List<LastHorizonFuelPoint>(512);
            sequence.Reset(profile);
            sequence.GetPoints(0f, distance, points);
            Require(points.Count > 100, "Fuel route did not populate the long-distance test.");
            normalCount = 0;
            scarcityCount = 0;
            maximumGap = 0f;
            float previous = 0f;
            var specialGapBuckets = new HashSet<int>();
            var stableIds = new HashSet<long>();
            for (int i = 0; i < points.Count; i++)
            {
                LastHorizonFuelPoint point = points[i];
                float gap = point.Position - previous;
                maximumGap = Mathf.Max(maximumGap, gap);
                Require(stableIds.Add(point.StableId), "Fuel generated a duplicate stable ID.");
                Require(!IsCoinPickupReserved(profile.coinRoute, point.Position),
                    $"Fuel overlaps the coin route at {point.Position:F2} m.");
                if (point.IsScarcityFuel)
                {
                    float expectedBoundary = (scarcityCount + 1) *
                                             LastHorizonSettings.RequiredSystemLength;
                    scarcityCount++;
                    Require(gap >= 500f - .01f && gap <= 1000f + .01f,
                        $"Scarcity gap out of range: {gap:F2} m.");
                    Require(previous < expectedBoundary + .01f &&
                            point.Position >= expectedBoundary - .01f &&
                            point.Position <= expectedBoundary + 1000f + .01f,
                        $"20 km fuel event missed its boundary at {expectedBoundary:F0} m.");
                    specialGapBuckets.Add(Mathf.RoundToInt(gap));
                }
                else
                {
                    normalCount++;
                    Require(gap >= 200f - .01f && gap <= 500f + .01f,
                        $"Normal fuel gap out of range: {gap:F2} m.");
                }
                Require(gap <= 1000f + .01f, $"Absolute fuel gap exceeded 1000 m: {gap:F2}.");
                previous = point.Position;
            }
            int expectedScarcityCount = Mathf.FloorToInt((distance - .01f) /
                                                         LastHorizonSettings.RequiredSystemLength);
            Require(scarcityCount == expectedScarcityCount && specialGapBuckets.Count >= 3,
                "There must be exactly one independently varied fuel gap after each 20 km boundary.");

            var repeated = new List<LastHorizonFuelPoint>(points.Count);
            sequence.GetPoints(0f, distance, repeated);
            Require(repeated.Count == points.Count, "Fuel route changed after a repeated query.");
            for (int i = 0; i < points.Count; i++)
                Require(points[i].Position == repeated[i].Position &&
                        points[i].StableId == repeated[i].StableId,
                    $"Fuel route is unstable at sequence index {i}.");
        }

        private static bool IsCoinPickupReserved(CoinRouteDefinition route, float position)
        {
            if (route == null) return false;
            int firstCell = route.FirstCellForDistance(position - 3f);
            int lastCell = route.LastCellForDistance(position + 3f);
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                for (int pointIndex = 0; pointIndex < group.coinCount; pointIndex++)
                {
                    CoinRoutePoint point = route.GetPoint(group, pointIndex);
                    if (Mathf.Abs(position - group.anchorDistance - point.distanceOffset) < 2.25f)
                        return true;
                }
            }
            return false;
        }

        private static void ValidateObstacles(MapProfile profile, GameCatalog catalog, float distance,
            out int cycleOneCount, out int cycleTwoCount)
        {
            LastHorizonSettings settings = profile.lastHorizon;
            float maximumVehicleLength = 0f;
            if (catalog.vehicles != null)
                foreach (VehicleDefinition vehicle in catalog.vehicles)
                    if (vehicle != null)
                        maximumVehicleLength = Mathf.Max(maximumVehicleLength,
                            vehicle.halfWheelbase * 2f + vehicle.wheelRadius * 2f);
            Require(settings.safeVehicleLength >= maximumVehicleLength,
                $"Safe vehicle length {settings.safeVehicleLength:F2} is below catalog maximum " +
                $"{maximumVehicleLength:F2}.");

            cycleOneCount = 0;
            cycleTwoCount = 0;
            var types = new HashSet<LastHorizonObstacleType>();
            var layouts = new List<LastHorizonObstacleLayout>();
            int cells = Mathf.CeilToInt(distance / settings.obstacleCellLength);
            for (int cell = 0; cell < cells; cell++)
            {
                if (!TerrainGenerator.TryGetLastHorizonObstacle(profile, cell,
                        out LastHorizonObstacleLayout layout)) continue;
                Require(layout.width >= settings.safeVehicleLength,
                    $"Obstacle in cell {cell} is narrower than the largest-vehicle clearance.");
                Require(layout.type != LastHorizonObstacleType.None,
                    $"Invalid obstacle type in cell {cell}.");
                Require(!TerrainGenerator.IsLastHorizonSpecialPhaseReserved(profile, layout.centerX,
                        settings.specialPhaseSeparation),
                    $"Obstacle in cell {cell} overlaps a special phase.");
                Require(TerrainGenerator.TryGetLastHorizonObstacle(profile, cell,
                            out LastHorizonObstacleLayout repeated) &&
                        layout.type == repeated.type && layout.centerX == repeated.centerX &&
                        layout.width == repeated.width,
                    $"Obstacle in cell {cell} is not deterministic.");
                types.Add(layout.type);
                layouts.Add(layout);
                if (layout.cycleType == 0) cycleOneCount++;
                else cycleTwoCount++;
            }
            layouts.Sort((a, b) => a.Start.CompareTo(b.Start));
            for (int i = 1; i < layouts.Count; i++)
                Require(layouts[i].Start > layouts[i - 1].End,
                    $"Obstacles overlap near {layouts[i].centerX:F0} m.");
            Require(cycleOneCount > 0 && cycleTwoCount > cycleOneCount,
                "Cycle 2 must contain more obstacles than Cycle 1.");
            Require(types.Contains(LastHorizonObstacleType.Seesaw) &&
                    types.Contains(LastHorizonObstacleType.LiftPlatform) &&
                    types.Contains(LastHorizonObstacleType.Rollers) &&
                    types.Contains(LastHorizonObstacleType.TiltingPlatform) &&
                    types.Contains(LastHorizonObstacleType.Ramp),
                "The long-distance route did not include every Last Horizon obstacle type.");
        }

        private static void ValidateLongDistance(MapProfile profile)
        {
            foreach (float distance in new[] { 10000f, 20000f, 30000f, 50000f, 100000f, 1000000f })
            {
                float height = TerrainGenerator.SampleLastHorizonHeight(profile, distance + 37.25f);
                Require(!float.IsNaN(height) && !float.IsInfinity(height),
                    $"Long-distance terrain failed at {distance:F0} m.");
            }
        }

        private static bool IsPureBlack(Color color) =>
            color.r == 0f && color.g == 0f && color.b == 0f && color.a == 1f;

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Last Horizon validation: " + message);
        }
    }
}
