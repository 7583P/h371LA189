using System.Collections.Generic;
using AnimeHillRacing.Pickups;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class TerrainChunk : MonoBehaviour
    {
        private struct TrackedPickup
        {
            public PickupBase pickup;
            public int spawnVersion;
        }

        private readonly List<TrackedPickup> pickups = new List<TrackedPickup>(24);
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private LineRenderer edgeRenderer;
        private PickupPool pickupPool;
        private float[] surfaceHeights;
        private float chunkLength;
        private float surfaceWidth;

        public int Index { get; private set; }
        public bool IsGenerated { get; private set; }
        public float EndHeight { get; private set; }
        public float EndSlope { get; private set; }
        public int SurfaceSampleCount => surfaceHeights != null ? surfaceHeights.Length : 0;
        public float ChunkLength => chunkLength;
        public float SurfaceWidth => surfaceWidth;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null)
                meshCollider = GetComponent<MeshCollider>();
            if (edgeRenderer == null)
            {
                edgeRenderer = GetComponent<LineRenderer>();
                if (edgeRenderer == null) edgeRenderer = gameObject.AddComponent<LineRenderer>();
                edgeRenderer.useWorldSpace = false;
                edgeRenderer.alignment = LineAlignment.View;
                edgeRenderer.textureMode = LineTextureMode.Stretch;
                edgeRenderer.numCapVertices = 2;
                edgeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                edgeRenderer.receiveShadows = false;
            }
            if (mesh == null)
            {
                mesh = new Mesh { name = "Runtime Terrain Chunk" };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }
        }

        public void Generate(int index, MapProfile profile, float targetChunkLength, int samples, float width,
            PickupPool pool, bool hasBoundary, float boundaryHeight, float boundarySlope)
        {
            EnsureComponents();
            ReleasePickups();
            Index = index;
            chunkLength = targetChunkLength;
            surfaceWidth = width;
            pickupPool = pool;
            transform.position = new Vector3(index * targetChunkLength, 0f, 0f);
            TerrainGenerator.BuildMesh(mesh, profile, index * targetChunkLength, targetChunkLength, samples, width,
                hasBoundary, boundaryHeight, boundarySlope, out surfaceHeights, out float endHeight, out float endSlope);
            EndHeight = endHeight;
            EndSlope = endSlope;
            IsGenerated = true;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
            meshCollider.sharedMaterial = profile.groundPhysicsMaterial;
            meshRenderer.sharedMaterial = profile.terrainMaterial;
            edgeRenderer.sharedMaterial = profile.terrainEdgeMaterial != null
                ? profile.terrainEdgeMaterial : profile.terrainMaterial;
            edgeRenderer.startWidth = profile.terrainEdgeWidth;
            edgeRenderer.endWidth = profile.terrainEdgeWidth;
            edgeRenderer.positionCount = surfaceHeights.Length;
            float dx = targetChunkLength / Mathf.Max(1, surfaceHeights.Length - 1);
            for (int i = 0; i < surfaceHeights.Length; i++)
                edgeRenderer.SetPosition(i, new Vector3(i * dx, surfaceHeights[i], -width * 0.5f - 0.03f));
            name = $"Terrain Chunk {index}";
        }

        public float GetSurfaceHeight(int sampleIndex) => surfaceHeights != null && surfaceHeights.Length > 0
            ? surfaceHeights[Mathf.Clamp(sampleIndex, 0, surfaceHeights.Length - 1)] : 0f;

        public void SetChunkColliderEnabled(bool value)
        {
            EnsureComponents();
            meshCollider.enabled = value;
        }

        public bool TrySampleSurface(float worldX, out float height, out float slopeDegrees)
        {
            height = 0f;
            slopeDegrees = 0f;
            if (!IsGenerated || surfaceHeights == null || surfaceHeights.Length < 2)
                return false;
            float localX = worldX - Index * chunkLength;
            if (localX < 0f || localX > chunkLength)
                return false;
            float samplePosition = localX / chunkLength * (surfaceHeights.Length - 1);
            int lower = Mathf.Clamp(Mathf.FloorToInt(samplePosition), 0, surfaceHeights.Length - 2);
            float t = samplePosition - lower;
            height = Mathf.Lerp(surfaceHeights[lower], surfaceHeights[lower + 1], t);
            float dx = chunkLength / (surfaceHeights.Length - 1);
            slopeDegrees = Mathf.Atan2(surfaceHeights[lower + 1] - surfaceHeights[lower], dx) * Mathf.Rad2Deg;
            return true;
        }

        public void TrackPickup(PickupBase pickup)
        {
            if (pickup != null)
                pickups.Add(new TrackedPickup { pickup = pickup, spawnVersion = pickup.SpawnVersion });
        }

        private void ReleasePickups()
        {
            if (pickupPool != null)
            {
                for (int i = 0; i < pickups.Count; i++)
                    if (pickups[i].pickup != null && pickups[i].pickup.SpawnVersion == pickups[i].spawnVersion &&
                        pickups[i].pickup.gameObject.activeSelf)
                        pickupPool.Release(pickups[i].pickup);
            }
            pickups.Clear();
        }

        private void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
