using System.Collections.Generic;
using AnimeHillRacing.Pickups;
using UnityEngine;

namespace AnimeHillRacing.Road
{
    public sealed class RoadTrackChunk : MonoBehaviour
    {
        private struct TrackedPickup
        {
            public PickupBase pickup;
            public int spawnVersion;
        }

        private readonly List<TrackedPickup> pickups = new List<TrackedPickup>(16);
        private readonly List<DistanceBonusMarker> bonusMarkers = new List<DistanceBonusMarker>(2);
        private Mesh groundMesh;
        private Mesh roadMesh;
        private MeshCollider groundCollider;
        private MeshRenderer groundRenderer;
        private MeshFilter groundFilter;
        private MeshRenderer roadRenderer;
        private MeshFilter roadFilter;
        private PickupPool pool;

        public int Index { get; private set; }
        public bool IsGenerated { get; private set; }

        private void EnsureObjects()
        {
            if (groundFilter != null)
                return;
            groundFilter = gameObject.AddComponent<MeshFilter>();
            groundRenderer = gameObject.AddComponent<MeshRenderer>();
            groundCollider = gameObject.AddComponent<MeshCollider>();
            groundMesh = new Mesh { name = "Road Shoulder Chunk" };
            groundFilter.sharedMesh = groundMesh;
            GameObject road = new GameObject("Road Surface");
            road.transform.SetParent(transform, false);
            roadFilter = road.AddComponent<MeshFilter>();
            roadRenderer = road.AddComponent<MeshRenderer>();
            roadMesh = new Mesh { name = "Road Surface Chunk" };
            roadFilter.sharedMesh = roadMesh;
        }

        public void Generate(int index, RoadMapProfile profile, PickupPool pickupPool)
        {
            EnsureObjects();
            ReleasePickups();
            ReleaseBonusMarkers();
            Index = index;
            IsGenerated = true;
            pool = pickupPool;
            transform.position = new Vector3(0f, 0f, index * profile.chunkLength);
            BuildStrip(groundMesh, profile, index, profile.roadWidth + 24f, 0f);
            BuildStrip(roadMesh, profile, index, profile.roadWidth, 0.035f);
            groundCollider.sharedMesh = null;
            groundCollider.sharedMesh = groundMesh;
            groundCollider.sharedMaterial = profile.groundPhysicsMaterial;
            groundRenderer.sharedMaterial = profile.shoulderMaterial;
            roadRenderer.sharedMaterial = profile.roadMaterial;
            name = $"Road Chunk {index}";
        }

        private static void BuildStrip(Mesh mesh, RoadMapProfile profile, int index, float width, float yOffset)
        {
            int count = profile.samplesPerChunk + 1;
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(count - 1) * 6];
            float dz = profile.chunkLength / profile.samplesPerChunk;
            float startZ = index * profile.chunkLength;
            for (int i = 0; i < count; i++)
            {
                float localZ = i * dz;
                float worldZ = startZ + localZ;
                float center = RoadTrackGenerator.SampleCenterX(profile, worldZ);
                float height = RoadTrackGenerator.SampleHeight(profile, worldZ) + yOffset;
                vertices[i * 2] = new Vector3(center - width * 0.5f, height, localZ);
                vertices[i * 2 + 1] = new Vector3(center + width * 0.5f, height, localZ);
                uvs[i * 2] = new Vector2(0f, worldZ * 0.08f);
                uvs[i * 2 + 1] = new Vector2(1f, worldZ * 0.08f);
            }
            int triangle = 0;
            for (int i = 0; i < count - 1; i++)
            {
                int vertex = i * 2;
                triangles[triangle++] = vertex;
                triangles[triangle++] = vertex + 2;
                triangles[triangle++] = vertex + 1;
                triangles[triangle++] = vertex + 1;
                triangles[triangle++] = vertex + 2;
                triangles[triangle++] = vertex + 3;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public void Track(PickupBase pickup)
        {
            if (pickup != null)
                pickups.Add(new TrackedPickup { pickup = pickup, spawnVersion = pickup.SpawnVersion });
        }

        public void TrackBonusMarker(DistanceBonusMarker marker)
        {
            if (marker != null) bonusMarkers.Add(marker);
        }

        private void ReleasePickups()
        {
            if (pool != null)
                for (int i = 0; i < pickups.Count; i++)
                    if (pickups[i].pickup != null && pickups[i].pickup.SpawnVersion == pickups[i].spawnVersion &&
                        pickups[i].pickup.gameObject.activeSelf)
                        pool.Release(pickups[i].pickup);
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
            if (groundMesh != null) Destroy(groundMesh);
            if (roadMesh != null) Destroy(roadMesh);
        }
    }
}
