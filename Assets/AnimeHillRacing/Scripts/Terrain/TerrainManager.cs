using System.Collections.Generic;
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

        public int CurrentChunkIndex => currentChunkIndex;
        public MapProfile ActiveProfile => mapProfile;
        public float CurrentDifficulty => mapProfile != null && vehicle != null
            ? mapProfile.EvaluateDifficulty(Mathf.Max(0f, vehicle.transform.position.x))
            : 0f;
        public TerrainSectionKind CurrentSection => mapProfile != null && vehicle != null
            ? TerrainGenerator.GetSectionKind(mapProfile,
                Mathf.FloorToInt(vehicle.transform.position.x / Mathf.Max(1f, mapProfile.sectionLength)))
            : TerrainSectionKind.Fast;
        public float CurrentFuelSpacing => fuelSequence.CurrentSpacing;
        public float NextFuelSequencePosition => fuelSequence.NextPosition;

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
            currentChunkIndex = Mathf.FloorToInt(vehicle.transform.position.x / chunkLength);
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
        }

        private void Update()
        {
            if (chunks == null)
                return;
            int newIndex = Mathf.FloorToInt(vehicle.transform.position.x / chunkLength);
            if (newIndex <= currentChunkIndex)
                return;
            while (currentChunkIndex < newIndex)
            {
                currentChunkIndex++;
                RecycleOneForward();
            }
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
                        triangles.Add(previous);
                        triangles.Add(previous + 1);
                        triangles.Add(current);
                        triangles.Add(previous + 1);
                        triangles.Add(current + 1);
                        triangles.Add(current);
                    }
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
                    if (x < start || x >= end || !chunk.TrySampleSurface(x, out float y, out _)) continue;
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

        private void OnDestroy()
        {
            if (authoritativeTerrainMesh == null) return;
            if (Application.isPlaying) Destroy(authoritativeTerrainMesh);
            else DestroyImmediate(authoritativeTerrainMesh);
        }
    }
}
