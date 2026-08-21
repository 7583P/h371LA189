using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    public sealed class TerrainManager : MonoBehaviour
    {
        [SerializeField] private MapProfile mapProfile;
        [SerializeField] private VehicleRuntimeBase vehicle;
        [SerializeField] private PickupPool pickupPool;
        [SerializeField] private GameManager gameManager;
        [SerializeField, Min(20f)] private float chunkLength = 80f;
        [SerializeField, Range(8, 96)] private int samples = 48;
        [SerializeField, Min(2f)] private float width = 10f;
        [SerializeField, Range(0, 8)] private int chunksBehind = 2;
        [SerializeField, Range(1, 12)] private int chunksAhead = 7;

        private TerrainChunk[] chunks;
        private int currentChunkIndex = int.MinValue;
        private int minimumLoadedIndex;
        private int maximumLoadedIndex;
        private readonly ProgressiveFuelSequence fuelSequence = new ProgressiveFuelSequence();
        private MeshCollider authoritativeTerrainCollider;
        private Mesh authoritativeTerrainMesh;
        private EdgeCollider2D authoritativeTerrainCollider2D;
        private PhysicsMaterial2D authoritativeTerrainMaterial2D;
        private Vector2[] authoritativeTerrainPoints2D;
        private readonly List<Vector2> authoritativeSurfaceKnots2D = new List<Vector2>(512);
        private readonly List<NeonLiquidHazard> liquidHazardPool = new List<NeonLiquidHazard>(12);
        private readonly Dictionary<long, NeonLiquidHazard> activeLiquidHazards =
            new Dictionary<long, NeonLiquidHazard>();
        private readonly HashSet<long> requiredLiquidHazards = new HashSet<long>();
        private readonly List<long> liquidKeyBuffer = new List<long>(12);

        public static TerrainManager Instance { get; private set; }

        public int CurrentChunkIndex => currentChunkIndex;
        public MapProfile ActiveProfile => mapProfile;
        public float CurrentDifficulty => mapProfile != null && vehicle != null
            ? mapProfile.EvaluateDifficulty(Mathf.Max(0f, VehicleWorldX * GameManager.LogicalDistanceScale))
            : 0f;
        public TerrainSectionKind CurrentSection => mapProfile != null && vehicle != null
            ? TerrainGenerator.GetSectionKind(mapProfile,
                Mathf.FloorToInt(VehicleWorldX / Mathf.Max(1f, mapProfile.sectionLength)))
            : TerrainSectionKind.Fast;
        public float CurrentFuelSpacing => fuelSequence.CurrentSpacing;
        public float NextFuelSequencePosition => fuelSequence.NextPosition;
        public EdgeCollider2D AuthoritativeCollider2D => authoritativeTerrainCollider2D;
        private float VehicleWorldX => vehicle != null && vehicle.Body2D != null
            ? vehicle.Body2D.position.x
            : vehicle != null ? vehicle.transform.position.x : 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (vehicle == null || mapProfile == null)
            {
                enabled = false;
                return;
            }

            pickupPool?.Initialize();
            EnsureAuthoritativeTerrainCollider();
            fuelSequence.Reset(0f, mapProfile.initialFuelGap, mapProfile.fuelGapIncrease);
            int count = chunksBehind + chunksAhead + 1;
            chunks = new TerrainChunk[count];
            currentChunkIndex = Mathf.FloorToInt(VehicleWorldX / chunkLength);
            minimumLoadedIndex = currentChunkIndex - chunksBehind;
            maximumLoadedIndex = currentChunkIndex + chunksAhead;
            for (int i = 0; i < count; i++)
            {
                GameObject chunkObject = new GameObject();
                chunkObject.transform.SetParent(transform, false);
                TerrainChunk chunk = chunkObject.AddComponent<TerrainChunk>();
                chunks[i] = chunk;
                GenerateChunk(chunk, minimumLoadedIndex + i);
            }
            RebuildAuthoritativeTerrainCollider();
            RebuildAuthoritativeTerrainCollider2D();
            PrewarmLiquidHazards(count + 2);
            RefreshLiquidHazards();
        }

        private void Update()
        {
            if (chunks == null)
                return;
            // Rigidbody2D interpolation intentionally lets Transform lag one rendered frame behind the solver.
            // Chunk ownership must follow the authoritative Box2D position, especially after a large-X teleport.
            int newIndex = Mathf.FloorToInt(VehicleWorldX / chunkLength);
            while (currentChunkIndex < newIndex)
            {
                currentChunkIndex++;
                RecycleOneForward();
            }
            while (currentChunkIndex > newIndex)
            {
                currentChunkIndex--;
                RecycleOneBackward();
            }

            // The Box2D comparison intentionally rebuilds one absolute-coordinate EdgeCollider2D every
            // rendered frame. World X is the only precision variable; no floating origin is used.
            if (vehicle != null && vehicle.Body2D != null)
                RebuildAuthoritativeTerrainCollider2D();
        }

        public void TeleportTo(float targetWorldX)
        {
            if (mapProfile == null || chunks == null) return;

            VehicleRuntimeBase activeVehicle = vehicle;
            if (activeVehicle == null)
            {
                GameManager gm = FindFirstObjectByType<GameManager>();
                if (gm != null) activeVehicle = gm.Vehicle;
            }

            int newChunkIndex = Mathf.FloorToInt(targetWorldX / chunkLength);
            currentChunkIndex = newChunkIndex;
            minimumLoadedIndex = currentChunkIndex - chunksBehind;
            maximumLoadedIndex = currentChunkIndex + chunksAhead;

            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i] != null)
                {
                    GenerateChunk(chunks[i], minimumLoadedIndex + i);
                }
            }

            float targetY = TrySampleAuthoritativeSurface(targetWorldX, out float surfaceY, out _)
                ? surfaceY + 2.5f
                : TerrainGenerator.SampleHeight(mapProfile, targetWorldX) + 2.5f;
            if (activeVehicle != null)
            {
                activeVehicle.TeleportToPosition(new Vector3(targetWorldX, targetY, 0f));
                activeVehicle.Fuel?.AddFuel(100f);
            }

            RebuildAuthoritativeTerrainCollider();
            RebuildAuthoritativeTerrainCollider2D();
            RefreshLiquidHazards();
        }

        public bool TrySampleAuthoritativeSurface(float worldX, out float height, out float slopeDegrees)
        {
            height = 0f;
            slopeDegrees = 0f;
            if (chunks == null) return false;

            int targetIndex = Mathf.FloorToInt(worldX / Mathf.Max(1f, chunkLength));
            TerrainChunk chunk = FindChunk(targetIndex);
            if (chunk != null && chunk.TrySampleSurface(worldX, out height, out slopeDegrees))
                return true;

            // At an exact shared boundary either adjacent chunk represents the same visual knot.
            chunk = FindChunk(targetIndex - 1);
            return chunk != null && chunk.TrySampleSurface(worldX, out height, out slopeDegrees);
        }

        private void RecycleOneForward()
        {
            TerrainChunk oldest = FindChunk(minimumLoadedIndex);
            minimumLoadedIndex++;
            maximumLoadedIndex++;
            if (oldest != null)
            {
                GenerateChunk(oldest, maximumLoadedIndex);
                RebuildAuthoritativeTerrainCollider();
                RefreshLiquidHazards();
            }
        }

        private void RecycleOneBackward()
        {
            TerrainChunk newest = FindChunk(maximumLoadedIndex);
            maximumLoadedIndex--;
            minimumLoadedIndex--;
            if (newest == null) return;
            GenerateChunk(newest, minimumLoadedIndex);
            RegenerateLoadedSurfacesAscending();
            RebuildAuthoritativeTerrainCollider();
            RefreshLiquidHazards();
        }

        private void RegenerateLoadedSurfacesAscending()
        {
            TerrainChunk previous = null;
            for (int index = minimumLoadedIndex; index <= maximumLoadedIndex; index++)
            {
                TerrainChunk chunk = FindChunk(index);
                if (chunk == null) continue;
                if (previous != null)
                {
                    chunk.RegenerateSurface(index, mapProfile, chunkLength, samples, width, pickupPool,
                        true, previous.EndHeight, previous.EndSlope);
                    chunk.SetChunkColliderEnabled(false);
                }
                previous = chunk;
            }
        }

        private TerrainChunk FindChunk(int index)
        {
            if (chunks == null)
                return null;
            for (int i = 0; i < chunks.Length; i++)
            {
                TerrainChunk candidate = chunks[i];
                if (candidate != null && candidate.IsGenerated && candidate.Index == index)
                    return candidate;
            }
            return null;
        }

        private void GenerateChunk(TerrainChunk chunk, int index)
        {
            TerrainChunk previous = FindChunk(index - 1);
            bool hasBoundary = previous != null;
            chunk.Generate(index, mapProfile, chunkLength, samples, width, pickupPool, hasBoundary,
                hasBoundary ? previous.EndHeight : 0f, hasBoundary ? previous.EndSlope : 0f);
            // Rendering remains chunked and recyclable, but separate coplanar MeshColliders produce duplicate
            // contacts at their shared edge. Physics is rebuilt below as one welded, authoritative surface.
            chunk.SetChunkColliderEnabled(false);
            SpawnCoins(chunk, index);
            SpawnProgressiveFuel(chunk, index);
        }

        private void EnsureAuthoritativeTerrainCollider()
        {
            if (authoritativeTerrainCollider == null)
                authoritativeTerrainCollider = GetComponent<MeshCollider>();
            if (authoritativeTerrainCollider == null)
                authoritativeTerrainCollider = gameObject.AddComponent<MeshCollider>();
            if (authoritativeTerrainMesh == null)
            {
                authoritativeTerrainMesh = new Mesh { name = "Authoritative Continuous Terrain Collider" };
                authoritativeTerrainMesh.MarkDynamic();
            }
            authoritativeTerrainCollider.sharedMaterial = mapProfile.groundPhysicsMaterial;

            if (authoritativeTerrainCollider2D == null)
            {
                Transform child = transform.Find("Authoritative Terrain 2D");
                if (child == null)
                {
                    GameObject childObject = new GameObject("Authoritative Terrain 2D");
                    childObject.transform.SetParent(transform, false);
                    child = childObject.transform;
                }
                authoritativeTerrainCollider2D = child.GetComponent<EdgeCollider2D>();
                if (authoritativeTerrainCollider2D == null)
                    authoritativeTerrainCollider2D = child.gameObject.AddComponent<EdgeCollider2D>();
            }
            if (authoritativeTerrainMaterial2D == null)
            {
                authoritativeTerrainMaterial2D = new PhysicsMaterial2D("AuthoritativeTerrain2D")
                {
                    friction = .85f,
                    bounciness = 0f
                };
            }
            authoritativeTerrainCollider2D.edgeRadius = 0f;
            authoritativeTerrainCollider2D.sharedMaterial = mapProfile != null && mapProfile.groundPhysicsMaterial2D != null
                ? mapProfile.groundPhysicsMaterial2D : authoritativeTerrainMaterial2D;
        }

        private void RebuildAuthoritativeTerrainCollider2D()
        {
            if (mapProfile == null || vehicle == null || vehicle.Body2D == null || chunks == null) return;
            EnsureAuthoritativeTerrainCollider();

            const int pointsBehind = 30;
            const int pointsAhead = 180;
            const int pointCount = pointsBehind + pointsAhead + 1;

            // The rendered road is the processed surfaceHeights polyline (smoothing, slope/crest limiting and
            // chunk-boundary continuity included). Sampling TerrainGenerator.SampleHeight again produced a
            // different invisible road. Reuse the exact rendered knots so visual and Box2D geometry are one
            // and the same piecewise-linear surface.
            authoritativeSurfaceKnots2D.Clear();
            for (int chunkIndex = minimumLoadedIndex; chunkIndex <= maximumLoadedIndex; chunkIndex++)
            {
                TerrainChunk chunk = FindChunk(chunkIndex);
                if (chunk == null) continue;
                int sampleCount = chunk.SurfaceSampleCount;
                int firstSample = authoritativeSurfaceKnots2D.Count > 0 ? 1 : 0;
                for (int sample = firstSample; sample < sampleCount; sample++)
                    authoritativeSurfaceKnots2D.Add(chunk.GetSurfaceWorldPoint(sample));
            }

            if (authoritativeSurfaceKnots2D.Count < 2)
            {
                authoritativeTerrainCollider2D.enabled = false;
                return;
            }

            float centerX = vehicle.Body2D.position.x;
            int centerKnot = 0;
            while (centerKnot + 1 < authoritativeSurfaceKnots2D.Count &&
                   authoritativeSurfaceKnots2D[centerKnot + 1].x <= centerX)
                centerKnot++;

            int firstKnot = Mathf.Clamp(centerKnot - pointsBehind, 0,
                Mathf.Max(0, authoritativeSurfaceKnots2D.Count - pointCount));
            int selectedCount = Mathf.Min(pointCount, authoritativeSurfaceKnots2D.Count - firstKnot);
            if (authoritativeTerrainPoints2D == null || authoritativeTerrainPoints2D.Length != selectedCount)
                authoritativeTerrainPoints2D = new Vector2[selectedCount];

            Transform colliderTransform = authoritativeTerrainCollider2D.transform;
            for (int i = 0; i < selectedCount; i++)
            {
                Vector2 world = authoritativeSurfaceKnots2D[firstKnot + i];
                Vector3 local = colliderTransform.InverseTransformPoint(new Vector3(world.x, world.y, 0f));
                authoritativeTerrainPoints2D[i] = new Vector2(local.x, local.y);
            }
            authoritativeTerrainCollider2D.points = authoritativeTerrainPoints2D;
            authoritativeTerrainCollider2D.enabled = true;
        }

        private void RebuildAuthoritativeTerrainCollider()
        {
            EnsureAuthoritativeTerrainCollider();
            var ordered = new List<TerrainChunk>(chunks != null ? chunks.Length : 0);
            if (chunks != null)
                for (int i = 0; i < chunks.Length; i++)
                    if (chunks[i] != null && chunks[i].IsGenerated) ordered.Add(chunks[i]);
            ordered.Sort((a, b) => a.Index.CompareTo(b.Index));

            var vertices = new List<Vector3>(ordered.Count * (samples + 1) * 2);
            var triangles = new List<int>(Mathf.Max(0, ordered.Count * samples * 6));
            int pairCount = 0;
            float previousWorldX = 0f;
            for (int chunkIndex = 0; chunkIndex < ordered.Count; chunkIndex++)
            {
                TerrainChunk chunk = ordered[chunkIndex];
                int sampleCount = chunk.SurfaceSampleCount;
                for (int sample = chunkIndex > 0 ? 1 : 0; sample < sampleCount; sample++)
                {
                    float localX = sample * chunk.ChunkLength / Mathf.Max(1, sampleCount - 1);
                    float height = chunk.GetSurfaceHeight(sample);
                    Vector3 leftWorld = chunk.transform.TransformPoint(
                        new Vector3(localX, height, -chunk.SurfaceWidth * .5f));
                    Vector3 rightWorld = chunk.transform.TransformPoint(
                        new Vector3(localX, height, chunk.SurfaceWidth * .5f));
                    vertices.Add(transform.InverseTransformPoint(leftWorld));
                    vertices.Add(transform.InverseTransformPoint(rightWorld));
                    if (pairCount > 0)
                    {
                        int previous = (pairCount - 1) * 2;
                        int current = pairCount * 2;
                        float segmentX = (previousWorldX + leftWorld.x) * .5f;
                        if (!TerrainGenerator.IsLiquidGap(mapProfile, segmentX))
                        {
                            triangles.Add(previous);
                            triangles.Add(previous + 1);
                            triangles.Add(current);
                            triangles.Add(previous + 1);
                            triangles.Add(current + 1);
                            triangles.Add(current);
                        }
                    }
                    previousWorldX = leftWorld.x;
                    pairCount++;
                }
            }

            authoritativeTerrainMesh.Clear();
            authoritativeTerrainMesh.SetVertices(vertices);
            authoritativeTerrainMesh.SetTriangles(triangles, 0, true);
            authoritativeTerrainMesh.RecalculateNormals();
            authoritativeTerrainMesh.RecalculateBounds();
            authoritativeTerrainCollider.sharedMesh = null;
            authoritativeTerrainCollider.sharedMesh = authoritativeTerrainMesh;
        }

        private void SpawnCoins(TerrainChunk chunk, int index)
        {
            CoinRouteDefinition route = mapProfile != null ? mapProfile.coinRoute : null;
            if (pickupPool == null || route == null || index < 0)
                return;
            float start = index * chunkLength;
            float end = start + chunkLength;
            int firstCell = route.FirstCellForDistance(start);
            int lastCell = route.LastCellForDistance(end);
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                for (int pointIndex = 0; pointIndex < group.coinCount; pointIndex++)
                {
                    CoinRoutePoint point = route.GetPoint(group, pointIndex);
                    float x = group.anchorDistance + point.distanceOffset;
                    if (x < start || x >= end || TerrainGenerator.IsLiquidPool(mapProfile, x) ||
                        !chunk.TrySampleSurface(x, out float y, out _)) continue;
                    CoinPickup pickup = pickupPool.SpawnCoin(point.value,
                        new Vector3(x, y + route.surfaceOffset + point.heightOffset, 0f), x,
                        route.GetStableCoinId(group, pointIndex));
                    chunk.TrackPickup(pickup);
                }
            }
        }

        private void SpawnProgressiveFuel(TerrainChunk chunk, int index)
        {
            if (pickupPool == null || index < 0)
                return;
            float start = index * chunkLength;
            float end = start + chunkLength;
            fuelSequence.AdvancePast(start);
            int safety = 0;
            while (fuelSequence.NextPosition < end && safety++ < 128)
            {
                float targetDistance = fuelSequence.NextPosition;
                Vector3 position = FindAccessibleFuelPosition(chunk, start, end, targetDistance);
                PickupBase pickup = pickupPool.Spawn(PickupKind.Fuel, position, position.x);
                chunk.TrackPickup(pickup);
                fuelSequence.Advance(position.x);
            }
        }

        private Vector3 FindAccessibleFuelPosition(TerrainChunk chunk, float chunkStart, float chunkEnd,
            float targetDistance)
        {
            float bestX = Mathf.Clamp(targetDistance, chunkStart + 2f, chunkEnd - 2f);
            float bestY = 0f;
            float bestSlope = float.MaxValue;
            float radius = mapProfile.fuelPlacementSearchRadius;
            float step = mapProfile.fuelPlacementSearchStep;
            int steps = Mathf.CeilToInt(radius / step);
            for (int i = 0; i <= steps; i++)
            {
                for (int side = i == 0 ? 0 : -1; side <= 1; side += 2)
                {
                    float offset = i == 0 ? 0f : i * step * side;
                    float candidateX = Mathf.Clamp(targetDistance + offset, chunkStart + 2f, chunkEnd - 2f);
                    if (TerrainGenerator.IsLiquidPool(mapProfile, candidateX)) continue;
                    if (!chunk.TrySampleSurface(candidateX, out float candidateY, out float slope))
                        continue;
                    float absoluteSlope = Mathf.Abs(slope);
                    if (absoluteSlope < bestSlope)
                    {
                        bestSlope = absoluteSlope;
                        bestX = candidateX;
                        bestY = candidateY;
                    }
                    if (absoluteSlope <= mapProfile.fuelMaximumPlacementSlope)
                        // The new can trigger closely follows its artwork, so place the can on the
                        // terrain instead of relying on the old oversized trigger to reach the car.
                        return new Vector3(candidateX, candidateY + .68f, 0f);
                }
            }
            return new Vector3(bestX, bestY + .68f, 0f);
        }

        public void AssignRuntimeReferences(MapProfile profile, VehicleRuntimeBase targetVehicle, PickupPool pool)
        {
            mapProfile = profile;
            vehicle = targetVehicle;
            pickupPool = pool;
        }

        public void AssignRuntimeReferences(MapProfile profile, VehicleRuntimeBase targetVehicle, PickupPool pool,
            GameManager manager)
        {
            mapProfile = profile;
            vehicle = targetVehicle;
            pickupPool = pool;
            gameManager = manager;
        }

        private void PrewarmLiquidHazards(int count)
        {
            if (mapProfile == null || !mapProfile.supportsLiquidExposure) return;
            while (liquidHazardPool.Count < count)
            {
                GameObject hazardObject = new GameObject("Neon Liquid - Available Pooled Instance");
                hazardObject.transform.SetParent(transform, false);
                NeonLiquidHazard hazard = hazardObject.AddComponent<NeonLiquidHazard>();
                hazardObject.SetActive(false);
                liquidHazardPool.Add(hazard);
            }
        }

        private void RefreshLiquidHazards()
        {
            if (mapProfile == null || !mapProfile.supportsLiquidExposure)
            {
                for (int i = 0; i < liquidHazardPool.Count; i++) liquidHazardPool[i]?.Deactivate();
                activeLiquidHazards.Clear();
                return;
            }
            float loadedStart = minimumLoadedIndex * chunkLength;
            float loadedEnd = (maximumLoadedIndex + 1) * chunkLength;
            int firstCycle = TerrainGenerator.GetCycleIndex(Mathf.Max(0f, loadedStart - 200f));
            int lastCycle = TerrainGenerator.GetCycleIndex(Mathf.Max(0f, loadedEnd + 200f));
            requiredLiquidHazards.Clear();
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                for (int slot = 0; slot < LiquidChallengeSettings.RequiredChallengesPerCycle; slot++)
                {
                    LiquidChallengeLayout layout = TerrainGenerator.GetLiquidChallenge(mapProfile, cycle, slot);
                    if (layout.recoveryEnd < loadedStart || layout.runUpStart > loadedEnd) continue;
                    long key = ((long)cycle << 32) | (uint)slot;
                    requiredLiquidHazards.Add(key);
                    if (activeLiquidHazards.ContainsKey(key)) continue;
                    NeonLiquidHazard hazard = GetAvailableLiquidHazard();
                    hazard.Configure(mapProfile, layout, width, gameManager);
                    activeLiquidHazards.Add(key, hazard);
                }
            }
            liquidKeyBuffer.Clear();
            foreach (KeyValuePair<long, NeonLiquidHazard> pair in activeLiquidHazards)
                if (!requiredLiquidHazards.Contains(pair.Key)) liquidKeyBuffer.Add(pair.Key);
            for (int i = 0; i < liquidKeyBuffer.Count; i++)
            {
                long key = liquidKeyBuffer[i];
                activeLiquidHazards[key].Deactivate();
                activeLiquidHazards.Remove(key);
            }
        }

        private NeonLiquidHazard GetAvailableLiquidHazard()
        {
            for (int i = 0; i < liquidHazardPool.Count; i++)
                if (liquidHazardPool[i] != null && !liquidHazardPool[i].gameObject.activeSelf)
                    return liquidHazardPool[i];
            PrewarmLiquidHazards(liquidHazardPool.Count + 1);
            return liquidHazardPool[liquidHazardPool.Count - 1];
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (authoritativeTerrainMesh != null)
            {
                if (Application.isPlaying) Destroy(authoritativeTerrainMesh);
                else DestroyImmediate(authoritativeTerrainMesh);
            }
            if (authoritativeTerrainMaterial2D != null)
            {
                if (Application.isPlaying) Destroy(authoritativeTerrainMaterial2D);
                else DestroyImmediate(authoritativeTerrainMaterial2D);
            }
        }
    }
}
