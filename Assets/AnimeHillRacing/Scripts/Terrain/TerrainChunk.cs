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
            public float surfaceOffset;
        }

        private readonly List<TrackedPickup> pickups = new List<TrackedPickup>(24);
        private readonly List<DistanceBonusMarker> bonusMarkers = new List<DistanceBonusMarker>(2);
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
            GenerateInternal(index, profile, targetChunkLength, samples, width, pool, hasBoundary,
                boundaryHeight, boundarySlope, false);
        }

        public void RegenerateSurface(int index, MapProfile profile, float targetChunkLength, int samples,
            float width, PickupPool pool, bool hasBoundary, float boundaryHeight, float boundarySlope)
        {
            GenerateInternal(index, profile, targetChunkLength, samples, width, pool, hasBoundary,
                boundaryHeight, boundarySlope, true);
        }

        private void GenerateInternal(int index, MapProfile profile, float targetChunkLength, int samples,
            float width, PickupPool pool, bool hasBoundary, float boundaryHeight, float boundarySlope,
            bool preservePickups)
        {
            EnsureComponents();
            if (!preservePickups)
            {
                ReleasePickups();
                ReleaseBonusMarkers();
            }
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
                int totalPoints = surfaceHeights.Length;
                if (edgePoints2D == null || edgePoints2D.Length != totalPoints)
                    edgePoints2D = new Vector2[totalPoints];

                float worldStartX = index * targetChunkLength;
                for (int i = 0; i < surfaceHeights.Length; i++)
                {
                    float worldX = worldStartX + i * dx;
                    edgePoints2D[i] = new Vector2(worldX - worldStartX, surfaceHeights[i]);
                }

                edgeCollider2D.points = edgePoints2D;
                // Ghost adjacency points provide the tangent used by Box2D at each endpoint without
                // adding overlapping physical segments between chunks. Consecutive chunks share the
                // exact boundary height/slope passed through TerrainManager.
                edgeCollider2D.useAdjacentStartPoint = true;
                edgeCollider2D.adjacentStartPoint = new Vector2(-dx,
                    surfaceHeights[0] - (surfaceHeights[1] - surfaceHeights[0]));
                int last = surfaceHeights.Length - 1;
                edgeCollider2D.useAdjacentEndPoint = true;
                edgeCollider2D.adjacentEndPoint = new Vector2(targetChunkLength + dx,
                    surfaceHeights[last] + (surfaceHeights[last] - surfaceHeights[last - 1]));
                edgeCollider2D.edgeRadius = 0f;
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
                // CityGround.mat restricts sampling to the long road strip and skips only its transparent
                // antialiased end gutter, preserving the authored lane markings without repeat hairlines.
                groundProperties.SetFloat(HorizontalSampleId, -1f);
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
            bool mountainsNeedsPhysicalContactLip = profile.useMountainsTerrain &&
                                                     !profile.fitAuthoredTerrainTexture;
            if (profile.supportsLiquidExposure ||
                profile.useSeparateTerrainMaterials && !mountainsNeedsPhysicalContactLip ||
                profile.useSpaceTerrain || profile.useArenaTerrain)
            {
                // Textured maps use the surface submesh as their edge. Tracks with real gaps must also omit
                // this single continuous line: otherwise it draws a convincing but non-collidable false floor
                // across every void even though the rendered terrain mesh and authoritative colliders are cut.
                edgeRenderer.positionCount = 0;
                name = $"Terrain Chunk {index}";
                if (preservePickups) RepositionTrackedPickups();
                return;
            }
            edgeRenderer.positionCount = surfaceHeights.Length;
            float lineDx = targetChunkLength / Mathf.Max(1, surfaceHeights.Length - 1);
            for (int i = 0; i < surfaceHeights.Length; i++)
                edgeRenderer.SetPosition(i, new Vector3(i * lineDx, surfaceHeights[i], -width * 0.5f - 0.03f));
            name = $"Terrain Chunk {index}";
            if (preservePickups) RepositionTrackedPickups();
        }

        public float GetSurfaceHeight(int sampleIndex) => surfaceHeights != null && surfaceHeights.Length > 0
            ? surfaceHeights[Mathf.Clamp(sampleIndex, 0, surfaceHeights.Length - 1)] : 0f;

        public Vector2 GetSurfaceWorldPoint(int sampleIndex)
        {
            if (surfaceHeights == null || surfaceHeights.Length == 0)
                return transform.position;
            int clamped = Mathf.Clamp(sampleIndex, 0, surfaceHeights.Length - 1);
            float localX = clamped * chunkLength / Mathf.Max(1, surfaceHeights.Length - 1);
            Vector3 world = transform.TransformPoint(new Vector3(localX, surfaceHeights[clamped], 0f));
            return new Vector2(world.x, world.y);
        }

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
            Vector2 first = GetSurfaceWorldPoint(0);
            Vector2 last = GetSurfaceWorldPoint(surfaceHeights.Length - 1);
            if (worldX < first.x || worldX > last.x)
                return false;

            int lower = 0;
            while (lower + 1 < surfaceHeights.Length - 1 &&
                   GetSurfaceWorldPoint(lower + 1).x <= worldX)
                lower++;
            Vector2 a = GetSurfaceWorldPoint(lower);
            Vector2 b = GetSurfaceWorldPoint(lower + 1);
            float dx = b.x - a.x;
            float t = Mathf.Abs(dx) > Mathf.Epsilon ? Mathf.Clamp01((worldX - a.x) / dx) : 0f;
            height = Mathf.Lerp(a.y, b.y, t);
            slopeDegrees = Mathf.Atan2(b.y - a.y, dx) * Mathf.Rad2Deg;
            return true;
        }

        public void TrackPickup(PickupBase pickup)
        {
            if (pickup != null)
            {
                float surfaceOffset = 0f;
                if (TrySampleSurface(pickup.transform.position.x, out float surfaceY, out _))
                    surfaceOffset = pickup.transform.position.y - surfaceY;
                pickups.Add(new TrackedPickup
                {
                    pickup = pickup,
                    spawnVersion = pickup.SpawnVersion,
                    surfaceOffset = surfaceOffset
                });
            }
        }

        public void TrackBonusMarker(DistanceBonusMarker marker)
        {
            if (marker != null) bonusMarkers.Add(marker);
        }

        private void RepositionTrackedPickups()
        {
            for (int i = 0; i < pickups.Count; i++)
            {
                TrackedPickup tracked = pickups[i];
                if (tracked.pickup == null || tracked.pickup.SpawnVersion != tracked.spawnVersion ||
                    !tracked.pickup.gameObject.activeSelf)
                    continue;
                Vector3 position = tracked.pickup.transform.position;
                if (!TrySampleSurface(position.x, out float surfaceY, out _)) continue;
                position.y = surfaceY + tracked.surfaceOffset;
                tracked.pickup.transform.position = position;
            }
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

        private void ReleaseBonusMarkers()
        {
            for (int i = 0; i < bonusMarkers.Count; i++)
                if (bonusMarkers[i] != null)
                    Destroy(bonusMarkers[i].gameObject);
            bonusMarkers.Clear();
        }

        private void OnDestroy()
        {
            ReleaseBonusMarkers();
            if (mesh != null)
                Destroy(mesh);
            if (surfaceColliderMesh != null)
                Destroy(surfaceColliderMesh);
        }
    }
}
