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
        private Mesh surfaceColliderMesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private EdgeCollider2D edgeCollider2D;
        private LineRenderer edgeRenderer;
        private PickupPool pickupPool;
        private float[] surfaceHeights;
        private Vector2[] edgePoints2D;
        private float chunkLength;
        private float surfaceWidth;
        private MaterialPropertyBlock groundProperties;

        private static readonly int HorizontalSampleId = Shader.PropertyToID("_HorizontalSample");
        private static PhysicsMaterial2D s_defaultGroundMaterial2D;

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
            if (edgeCollider2D == null)
            {
                Transform child2D = transform.Find("SurfaceCollider2D");
                if (child2D == null)
                {
                    GameObject go = new GameObject("SurfaceCollider2D");
                    go.transform.SetParent(transform, false);
                    child2D = go.transform;
                }
                edgeCollider2D = child2D.GetComponent<EdgeCollider2D>();
                if (edgeCollider2D == null) edgeCollider2D = child2D.gameObject.AddComponent<EdgeCollider2D>();
            }
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
            if (surfaceColliderMesh == null)
            {
                surfaceColliderMesh = new Mesh { name = "Ground Surface Only Collider" };
                surfaceColliderMesh.MarkDynamic();
            }
            if (s_defaultGroundMaterial2D == null)
            {
                s_defaultGroundMaterial2D = new PhysicsMaterial2D("DefaultTerrain2D")
                {
                    friction = 0.85f,
                    bounciness = 0f
                };
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
            TerrainGenerator.BuildSurfaceColliderMesh(surfaceColliderMesh, profile,
                index * targetChunkLength, targetChunkLength, surfaceHeights, width);
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = surfaceColliderMesh;
                meshCollider.sharedMaterial = profile.groundPhysicsMaterial;
            }

            // Generate Physics2D EdgeCollider2D representing the exact driving surface
            if (surfaceHeights != null && surfaceHeights.Length > 1 && edgeCollider2D != null)
            {
                float dx = targetChunkLength / Mathf.Max(1, surfaceHeights.Length - 1);
                int totalPoints = surfaceHeights.Length + 2;
                if (edgePoints2D == null || edgePoints2D.Length != totalPoints)
                    edgePoints2D = new Vector2[totalPoints];

                float worldStartX = index * targetChunkLength;
                float prevY = profile != null ? TerrainGenerator.SampleHeight(profile, worldStartX - dx) : surfaceHeights[0];
                float nextY = profile != null ? TerrainGenerator.SampleHeight(profile, worldStartX + targetChunkLength + dx) : surfaceHeights[surfaceHeights.Length - 1];

                edgePoints2D[0] = new Vector2(-dx, prevY);
                for (int i = 0; i < surfaceHeights.Length; i++)
                {
                    edgePoints2D[i + 1] = new Vector2(i * dx, surfaceHeights[i]);
                }
                edgePoints2D[totalPoints - 1] = new Vector2(targetChunkLength + dx, nextY);

                edgeCollider2D.points = edgePoints2D;
                edgeCollider2D.edgeRadius = 0.02f;
                edgeCollider2D.sharedMaterial = profile != null && profile.groundPhysicsMaterial2D != null
                    ? profile.groundPhysicsMaterial2D : s_defaultGroundMaterial2D;
                edgeCollider2D.enabled = true;
            }

            if ((profile.supportsLiquidExposure || profile.useSeparateTerrainMaterials) &&
                profile.terrainEdgeMaterial != null)
            {
                meshRenderer.sharedMaterials = new[] { profile.terrainMaterial, profile.terrainEdgeMaterial };
                groundProperties ??= new MaterialPropertyBlock();
                groundProperties.Clear();
                // City Ground.png contains dark panel separators in the source art. Sampling a clean vertical
                // cross-section preserves its authored shading while producing one continuous ground ribbon.
                groundProperties.SetFloat(HorizontalSampleId, profile.useCityHighwayTerrain ? .44f : -1f);
                meshRenderer.SetPropertyBlock(groundProperties, 1);
            }
            else
            {
                meshRenderer.sharedMaterial = profile.terrainMaterial;
                meshRenderer.SetPropertyBlock(null);
            }
            edgeRenderer.sharedMaterial = profile.terrainEdgeMaterial != null
                ? profile.terrainEdgeMaterial : profile.terrainMaterial;
            edgeRenderer.startWidth = profile.terrainEdgeWidth;
            edgeRenderer.endWidth = profile.terrainEdgeWidth;
            if (profile.supportsLiquidExposure || profile.useSeparateTerrainMaterials)
            {
                // Textured maps use the surface submesh as their edge. Neon also contains real gaps, so a
                // single LineRenderer would bridge them. In both cases the extra line is intentionally omitted.
                edgeRenderer.positionCount = 0;
                name = $"Terrain Chunk {index}";
                return;
            }
            edgeRenderer.positionCount = surfaceHeights.Length;
            float lineDx = targetChunkLength / Mathf.Max(1, surfaceHeights.Length - 1);
            for (int i = 0; i < surfaceHeights.Length; i++)
                edgeRenderer.SetPosition(i, new Vector3(i * lineDx, surfaceHeights[i], -width * 0.5f - 0.03f));
            name = $"Terrain Chunk {index}";
        }

        public float GetSurfaceHeight(int sampleIndex) => surfaceHeights != null && surfaceHeights.Length > 0
            ? surfaceHeights[Mathf.Clamp(sampleIndex, 0, surfaceHeights.Length - 1)] : 0f;

        public void SetChunkColliderEnabled(bool value)
        {
            EnsureComponents();
            if (meshCollider != null) meshCollider.enabled = value;
            if (edgeCollider2D != null) edgeCollider2D.enabled = value;
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
            float sampleDx = chunkLength / (surfaceHeights.Length - 1);
            slopeDegrees = Mathf.Atan2(surfaceHeights[lower + 1] - surfaceHeights[lower], sampleDx) * Mathf.Rad2Deg;
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
            if (surfaceColliderMesh != null)
                Destroy(surfaceColliderMesh);
        }
    }
}
