using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class NeonLiquidHazard : MonoBehaviour
    {
        private readonly HashSet<Collider> chassisContacts = new HashSet<Collider>();
        private readonly Dictionary<VehicleRuntimeBase, float> entryElapsed =
            new Dictionary<VehicleRuntimeBase, float>();
        private readonly List<VehicleRuntimeBase> vehicleBuffer = new List<VehicleRuntimeBase>(2);

        private BoxCollider volume;
        private SpriteRenderer liquidRenderer;
        private SpriteRenderer cautionRenderer;
        private MeshFilter basinFilter;
        private MeshRenderer basinRenderer;
        private MeshCollider basinCollider;
        private Mesh basinMesh;
        private LiquidChallengeSettings settings;
        private GameManager gameManager;
        private float surfaceY;
        private int contactId;

        public int CycleIndex { get; private set; }
        public int ChallengeIndex { get; private set; }
        public float PoolLength { get; private set; }
        public bool HasChassisContact => entryElapsed.Count > 0;

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            volume ??= GetComponent<BoxCollider>();
            volume.isTrigger = true;
            if (liquidRenderer == null)
            {
                Transform child = transform.Find("Liquid Visual");
                if (child == null)
                {
                    child = new GameObject("Liquid Visual").transform;
                    child.SetParent(transform, false);
                }
                liquidRenderer = child.GetComponent<SpriteRenderer>();
                if (liquidRenderer == null) liquidRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            if (cautionRenderer == null)
            {
                Transform child = transform.Find("Caution Visual - No Collider");
                if (child == null)
                {
                    child = new GameObject("Caution Visual - No Collider").transform;
                    child.SetParent(transform, false);
                }
                cautionRenderer = child.GetComponent<SpriteRenderer>();
                if (cautionRenderer == null) cautionRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            EnsureBasinComponents();
        }

        private void EnsureBasinComponents()
        {
            Transform child = transform.Find("Liquid Basin Terrain");
            if (child == null)
            {
                child = new GameObject("Liquid Basin Terrain").transform;
                child.SetParent(transform, false);
            }
            basinFilter ??= child.GetComponent<MeshFilter>();
            if (basinFilter == null) basinFilter = child.gameObject.AddComponent<MeshFilter>();
            basinRenderer ??= child.GetComponent<MeshRenderer>();
            if (basinRenderer == null) basinRenderer = child.gameObject.AddComponent<MeshRenderer>();
            basinCollider ??= child.GetComponent<MeshCollider>();
            if (basinCollider == null) basinCollider = child.gameObject.AddComponent<MeshCollider>();
            if (basinMesh == null)
            {
                basinMesh = new Mesh { name = "Neon Liquid Basin Terrain" };
                basinMesh.MarkDynamic();
                basinFilter.sharedMesh = basinMesh;
            }
        }

        public void Configure(MapProfile profile, LiquidChallengeLayout layout, float trackWidth,
            GameManager manager)
        {
            EnsureComponents();
            ClearContacts();
            settings = profile.liquidChallenges;
            gameManager = manager;
            CycleIndex = layout.cycleIndex;
            ChallengeIndex = layout.challengeIndex;
            PoolLength = layout.poolLength;
            surfaceY = layout.liquidSurfaceY;
            contactId = GetInstanceID();
            transform.position = new Vector3(layout.poolStart, surfaceY, 0f);
            volume.center = new Vector3(layout.poolLength * .5f, -settings.visualDepth * .5f, 0f);
            volume.size = new Vector3(layout.poolLength, settings.visualDepth, Mathf.Max(2f, trackWidth));
            ConfigureBasin(profile, layout, trackWidth);

            liquidRenderer.sprite = settings.liquidSprite;
            liquidRenderer.drawMode = SpriteDrawMode.Tiled;
            liquidRenderer.tileMode = SpriteTileMode.Continuous;
            liquidRenderer.size = new Vector2(layout.poolLength, settings.visualDepth);
            liquidRenderer.sortingOrder = settings.liquidSortingOrder;
            liquidRenderer.transform.localPosition = new Vector3(layout.poolLength * .5f,
                -settings.visualDepth * .5f, -trackWidth * .5f - .08f);
            Color liquidColor = liquidRenderer.color;
            liquidColor.a = settings.normalAlpha;
            liquidRenderer.color = liquidColor;

            float cautionX = layout.rampStart - settings.cautionDistanceBeforeRamp;
            float cautionSurface = TerrainGenerator.SampleHeight(profile, cautionX);
            cautionRenderer.sprite = settings.cautionSprite;
            cautionRenderer.sortingOrder = settings.cautionSortingOrder;
            cautionRenderer.transform.localPosition = new Vector3(cautionX - layout.poolStart,
                cautionSurface - settings.cautionBelowSurface - surfaceY, -trackWidth * .5f - .10f);
            float sourceWidth = cautionRenderer.sprite != null ? cautionRenderer.sprite.bounds.size.x : 1f;
            cautionRenderer.transform.localScale = Vector3.one *
                (settings.cautionWidth / Mathf.Max(.001f, sourceWidth));
            gameObject.name = $"Neon Liquid C{CycleIndex} #{ChallengeIndex + 1} - Pooled";
            gameObject.SetActive(true);
        }

        private void ConfigureBasin(MapProfile profile, LiquidChallengeLayout layout, float trackWidth)
        {
            float depth = Mathf.Max(1f, settings.visualDepth);
            float bottomY = -depth;
            float exitY = TerrainGenerator.SampleHeight(profile, layout.poolEnd) - surfaceY;
            float rampLength = Mathf.Clamp(settings.exitRampHorizontalLength, 2f,
                Mathf.Max(2f, layout.poolLength - 2f));
            float rampStartX = Mathf.Max(0f, layout.poolLength - rampLength);
            float skirtBottom = bottomY - Mathf.Max(1f, profile.terrainVisualDepth);
            float halfWidth = Mathf.Max(1f, trackWidth * .5f);
            float[] x = { 0f, rampStartX, layout.poolLength };
            float[] y = { bottomY, bottomY, exitY };
            var vertices = new Vector3[9];
            var uvs = new Vector2[9];
            for (int i = 0; i < x.Length; i++)
            {
                int vertex = i * 3;
                vertices[vertex] = new Vector3(x[i], y[i], -halfWidth);
                vertices[vertex + 1] = new Vector3(x[i], y[i], halfWidth);
                vertices[vertex + 2] = new Vector3(x[i], skirtBottom, -halfWidth);
                uvs[vertex] = new Vector2(x[i] * .1f, 0f);
                uvs[vertex + 1] = new Vector2(x[i] * .1f, 1f);
                uvs[vertex + 2] = new Vector2(x[i] * .1f, Mathf.Abs(y[i] - skirtBottom) * .1f);
            }
            int[] surfaceTriangles =
            {
                0, 1, 3, 1, 4, 3,
                3, 4, 6, 4, 7, 6
            };
            int[] skirtTriangles =
            {
                0, 3, 2, 3, 5, 2,
                3, 6, 5, 6, 8, 5
            };
            basinMesh.Clear();
            basinMesh.vertices = vertices;
            basinMesh.uv = uvs;
            if (profile.terrainEdgeMaterial != null)
            {
                basinMesh.subMeshCount = 2;
                basinMesh.SetTriangles(skirtTriangles, 0);
                basinMesh.SetTriangles(surfaceTriangles, 1);
                basinRenderer.sharedMaterials = new[] { profile.terrainMaterial, profile.terrainEdgeMaterial };
            }
            else
            {
                var allTriangles = new int[skirtTriangles.Length + surfaceTriangles.Length];
                skirtTriangles.CopyTo(allTriangles, 0);
                surfaceTriangles.CopyTo(allTriangles, skirtTriangles.Length);
                basinMesh.subMeshCount = 1;
                basinMesh.SetTriangles(allTriangles, 0);
                basinRenderer.sharedMaterial = profile.terrainMaterial;
            }
            basinMesh.RecalculateNormals();
            basinMesh.RecalculateBounds();
            basinCollider.sharedMesh = null;
            basinCollider.sharedMesh = basinMesh;
            basinCollider.sharedMaterial = profile.groundPhysicsMaterial;
        }

        public void Deactivate()
        {
            ClearContacts();
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other) => AddContact(other);
        private void OnTriggerStay(Collider other) => AddContact(other);

        private void OnTriggerExit(Collider other)
        {
            if (!chassisContacts.Remove(other)) return;
            if (!TryGetChassisVehicle(other, out VehicleRuntimeBase vehicle)) return;
            foreach (Collider contact in chassisContacts)
                if (TryGetChassisVehicle(contact, out VehicleRuntimeBase remaining) && remaining == vehicle) return;
            entryElapsed.Remove(vehicle);
            gameManager?.NotifyLiquidContact(contactId, false);
        }

        private void AddContact(Collider other)
        {
            if (!TryGetChassisVehicle(other, out VehicleRuntimeBase vehicle) || !chassisContacts.Add(other)) return;
            if (entryElapsed.ContainsKey(vehicle)) return;
            entryElapsed.Add(vehicle, 0f);
            gameManager ??= FindFirstObjectByType<GameManager>();
            gameManager?.NotifyLiquidContact(contactId, true);
        }

        private static bool TryGetChassisVehicle(Collider other, out VehicleRuntimeBase vehicle)
        {
            vehicle = other != null ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            return vehicle != null && vehicle.Body != null && other.attachedRigidbody == vehicle.Body;
        }

        private void FixedUpdate()
        {
            if (settings == null || entryElapsed.Count == 0) return;
            vehicleBuffer.Clear();
            foreach (VehicleRuntimeBase vehicle in entryElapsed.Keys) vehicleBuffer.Add(vehicle);
            float entryDamping = -Mathf.Log(Mathf.Max(.001f, settings.entrySpeedRemaining)) /
                                 Mathf.Max(.05f, settings.entrySlowdownSeconds);
            for (int i = 0; i < vehicleBuffer.Count; i++)
            {
                VehicleRuntimeBase vehicle = vehicleBuffer[i];
                if (vehicle == null || vehicle.Body == null) continue;
                float elapsed = entryElapsed[vehicle] + Time.fixedDeltaTime;
                entryElapsed[vehicle] = elapsed;
                Rigidbody body = vehicle.Body;
                float damping = elapsed <= settings.entrySlowdownSeconds
                    ? entryDamping : settings.continuousViscosity;
                body.AddForce(-body.linearVelocity * body.mass * damping, ForceMode.Force);
                // Settle close to the real basin floor instead of hovering just below the surface.
                float targetY = surfaceY - Mathf.Max(1f, settings.visualDepth) + .75f;
                float buoyancy = Physics.gravity.magnitude +
                                 Mathf.Clamp((targetY - body.position.y) * 4f - body.linearVelocity.y * 3f,
                                     -Mathf.Max(.1f, settings.maximumSinkAcceleration), 8f);
                body.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
                body.AddForce(vehicle.transform.right *
                              (settings.liquidDriveAcceleration * Mathf.Clamp01(vehicle.AppliedThrottle)),
                    ForceMode.Acceleration);
            }
        }

        private void Update()
        {
            if (liquidRenderer == null || settings == null) return;
            Color color = liquidRenderer.color;
            float target = HasChassisContact ? settings.submergedAlpha : settings.normalAlpha;
            color.a = Mathf.MoveTowards(color.a, target, settings.alphaLerpSpeed * Time.deltaTime);
            liquidRenderer.color = color;
        }

        private void OnDisable() => ClearContacts();

        private void OnDestroy()
        {
            if (basinMesh == null) return;
            if (Application.isPlaying) Destroy(basinMesh);
            else DestroyImmediate(basinMesh);
        }

        private void ClearContacts()
        {
            if (entryElapsed.Count > 0) gameManager?.NotifyLiquidContact(contactId, false);
            chassisContacts.Clear();
            entryElapsed.Clear();
        }
    }
}
