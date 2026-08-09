using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.TimeTrial
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class TimeTrialCircuitGenerator : MonoBehaviour
    {
        [SerializeField] private CircuitDefinition definition;
        [SerializeField] private TimeTrialManager manager;

        private Mesh mesh;
        private bool hasShortcut;
        private bool hasTech;
        private readonly Dictionary<int, Transform> checkpoints = new Dictionary<int, Transform>();
        public CheckpointGraph Graph { get; private set; }
        public int Seed => definition != null ? definition.seed : 12345;

        private void Start() => Generate();

        public void Generate()
        {
            if (definition == null) return;
            ClearRuntimeChildren();
            int count = Mathf.Max(64, definition.samples);
            float baseRadius = definition.targetLength / (Mathf.PI * 2f);
            Vector3[] centers = new Vector3[count];
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[count * 2];
            int[] triangles = new int[count * 6];
            float phase = definition.seed * 0.0017f;
            hasShortcut = definition.seed == 12345 || Hash01(definition.seed, 71) < definition.shortcutChance;
            hasTech = definition.seed == 12345 || Hash01(definition.seed, 97) < definition.techChance;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float angle = t * Mathf.PI * 2f;
                int section = Mathf.FloorToInt(t * 12f);
                float sectionT = t * 12f - section;
                float envelope = Mathf.Sin(sectionT * Mathf.PI);
                CircuitFeatureType feature = FeatureAtSection(section);
                float featureRadius = 0f;
                float featureHeight = 0f;
                switch (feature)
                {
                    case CircuitFeatureType.Hairpin: featureRadius = -9f * envelope; break;
                    case CircuitFeatureType.SCurve: featureRadius = Mathf.Sin(sectionT * Mathf.PI * 2f) * 7f; break;
                    case CircuitFeatureType.FastCurve: featureRadius = 5f * envelope; break;
                    case CircuitFeatureType.MediumCurve: featureRadius = -5f * envelope; break;
                    case CircuitFeatureType.ElevationRise: featureHeight = 4f * envelope; break;
                    case CircuitFeatureType.ElevationDrop: featureHeight = -4f * envelope; break;
                    case CircuitFeatureType.TechnicalSection: featureRadius = Mathf.Sin(sectionT * Mathf.PI * 3f) * 4f; break;
                }
                float radius = baseRadius + Mathf.Sin(angle * 3f + phase) * 11f +
                               Mathf.Sin(angle * 7f + phase * 2.3f) * 3f + featureRadius;
                float height = Mathf.Sin(angle * 2f + phase) * 3.2f + Mathf.Sin(angle * 5f) * 1.1f + featureHeight;
                centers[i] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            }
            for (int i = 0; i < count; i++)
            {
                Vector3 forward = (centers[(i + 1) % count] - centers[(i - 1 + count) % count]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                vertices[i * 2] = centers[i] - right * definition.roadWidth * 0.5f;
                vertices[i * 2 + 1] = centers[i] + right * definition.roadWidth * 0.5f;
                uvs[i * 2] = new Vector2(0f, i * 0.25f);
                uvs[i * 2 + 1] = new Vector2(1f, i * 0.25f);
                int n = (i + 1) % count;
                int ti = i * 6;
                triangles[ti] = i * 2; triangles[ti + 1] = n * 2 + 1; triangles[ti + 2] = i * 2 + 1;
                triangles[ti + 3] = i * 2; triangles[ti + 4] = n * 2; triangles[ti + 5] = n * 2 + 1;
            }
            if (mesh == null) mesh = new Mesh { name = "Procedural Circuit Seed " + definition.seed };
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sharedMaterial = definition.roadMaterial;
            GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            MeshCollider collider = GetComponent<MeshCollider>(); collider.sharedMesh = null; collider.sharedMesh = mesh;

            BuildGraphAndCheckpoints(centers);
            if (hasShortcut) BuildShortcut(centers[count * 2 / 8], centers[count * 4 / 8]);
        }

        private void BuildGraphAndCheckpoints(Vector3[] centers)
        {
            Graph = new CheckpointGraph();
            Graph.Add(0, 1); Graph.Add(1, 2);
            if (hasShortcut) { Graph.Add(2, 3, 103); Graph.Add(103, 4); }
            else Graph.Add(2, 3);
            Graph.Add(3, 4); Graph.Add(4, 5); Graph.Add(5, 6); Graph.Add(6, 7); Graph.Add(7, 0);
            int count = centers.Length;
            for (int id = 0; id < 8; id++)
                CreateCheckpoint(id, centers, id * count / 8, definition.roadWidth);
            if (hasShortcut)
            {
                Vector3 shortcutPoint = Vector3.Lerp(centers[count * 2 / 8], centers[count * 4 / 8], 0.5f);
                CreateCheckpointObject(103, shortcutPoint + Vector3.up * 1.2f,
                    centers[count * 4 / 8] - centers[count * 2 / 8], definition.roadWidth * 0.72f, new Color(1f, 0.2f, 0.8f, 0.24f));
            }
        }

        private void CreateCheckpoint(int id, Vector3[] centers, int index, float width)
        {
            Vector3 forward = centers[(index + 1) % centers.Length] - centers[(index - 1 + centers.Length) % centers.Length];
            CreateCheckpointObject(id, centers[index] + Vector3.up * 1.2f, forward, width, id == 0
                ? new Color(0.1f, 1f, 0.25f, 0.28f) : new Color(0.1f, 0.65f, 1f, 0.18f));
        }

        private void CreateCheckpointObject(int id, Vector3 position, Vector3 forward, float width, Color color)
        {
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = id == 0 ? "Start Finish" : id == 103 ? "Shortcut Tech Checkpoint" : $"Checkpoint {id}";
            gate.transform.SetParent(transform, true); gate.transform.position = position;
            gate.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up).normalized, Vector3.up);
            gate.transform.localScale = new Vector3(width, 2.4f, 0.8f);
            BoxCollider box = gate.GetComponent<BoxCollider>(); box.isTrigger = true;
            gate.GetComponent<MeshRenderer>().enabled = false;
            gate.AddComponent<CheckpointTrigger>().Assign(id, manager);
            checkpoints[id] = gate.transform;
        }

        private void BuildShortcut(Vector3 from, Vector3 to)
        {
            GameObject shortcut = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shortcut.name = "Intentional Shortcut / Tech Lane";
            shortcut.transform.SetParent(transform, true);
            Vector3 delta = to - from;
            shortcut.transform.position = Vector3.Lerp(from, to, 0.5f) - Vector3.up * 0.25f;
            shortcut.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(delta, Vector3.up), Vector3.up);
            shortcut.transform.localScale = new Vector3(definition.roadWidth * 0.58f, 0.35f, delta.magnitude);
            shortcut.GetComponent<MeshRenderer>().sharedMaterial = definition.shortcutMaterial;
            if (hasTech)
            {
                GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "Reproducible Barrier Hop Ramp"; ramp.transform.SetParent(transform, true);
                ramp.transform.position = Vector3.Lerp(from, to, 0.20f) + Vector3.up * 0.25f;
                ramp.transform.rotation = shortcut.transform.rotation * Quaternion.Euler(-9f, 0f, 0f);
                ramp.transform.localScale = new Vector3(definition.roadWidth * 0.42f, 0.35f, 9f);
                ramp.GetComponent<MeshRenderer>().sharedMaterial = definition.shortcutMaterial;
            }
        }

        public CircuitFeatureType FeatureAtSection(int section)
        {
            int value = Mathf.Abs((definition != null ? definition.seed : 12345) * 31 + section * 17) % 10;
            return (CircuitFeatureType)value;
        }

        private static float Hash01(int seed, int salt)
        {
            uint value = (uint)(seed * 374761393) ^ (uint)(salt * 668265263);
            value = (value ^ (value >> 13)) * 1274126177u;
            return (value & 0x00FFFFFFu) / 16777215f;
        }

        public bool TryGetCheckpoint(int id, out Transform checkpoint) => checkpoints.TryGetValue(id, out checkpoint);

        public Vector3 StartPosition
        {
            get
            {
                if (checkpoints.TryGetValue(0, out Transform start)) return start.position + start.forward * 4f + Vector3.up;
                float radius = definition != null ? definition.targetLength / (Mathf.PI * 2f) : 140f;
                return new Vector3(radius, 2f, -4f);
            }
        }

        private void ClearRuntimeChildren()
        {
            checkpoints.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        }

        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
        public void Assign(CircuitDefinition circuit, TimeTrialManager targetManager) { definition = circuit; manager = targetManager; }
    }
}
