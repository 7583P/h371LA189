using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    public sealed class NeonLiquidHazard : MonoBehaviour
    {
        private readonly HashSet<Collider> chassisContacts = new HashSet<Collider>();
        private readonly HashSet<Collider2D> chassisContacts2D = new HashSet<Collider2D>();
        private readonly Dictionary<VehicleRuntimeBase, float> entryElapsed =
            new Dictionary<VehicleRuntimeBase, float>();
        private readonly List<VehicleRuntimeBase> vehicleBuffer = new List<VehicleRuntimeBase>(2);

        private BoxCollider volume;
        private BoxCollider2D volume2D;
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
            if (volume == null) volume = GetComponent<BoxCollider>();
            if (volume == null) volume = gameObject.AddComponent<BoxCollider>();
            if (volume != null) volume.isTrigger = true;

            // In Unity, BoxCollider (3D) and BoxCollider2D (2D) cannot reside on the same GameObject directly.
            // Host the 2D trigger volume on a dedicated child GameObject with a lightweight forwarder.
            Transform child2D = transform.Find("Liquid Trigger 2D");
            if (child2D == null)
            {
                child2D = new GameObject("Liquid Trigger 2D").transform;
                child2D.SetParent(transform, false);
            }
            if (volume2D == null) volume2D = child2D.GetComponent<BoxCollider2D>();
            if (volume2D == null) volume2D = child2D.gameObject.AddComponent<BoxCollider2D>();
            if (volume2D != null) volume2D.isTrigger = true;

            NeonLiquidHazard2DForwarder forwarder = child2D.GetComponent<NeonLiquidHazard2DForwarder>();
            if (forwarder == null) forwarder = child2D.gameObject.AddComponent<NeonLiquidHazard2DForwarder>();
            forwarder.AssignParent(this);

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
            if (basinFilter == null) basinFilter = child.GetComponent<MeshFilter>();
            if (basinFilter == null) basinFilter = child.gameObject.AddComponent<MeshFilter>();
            if (basinRenderer == null) basinRenderer = child.GetComponent<MeshRenderer>();
            if (basinRenderer == null) basinRenderer = child.gameObject.AddComponent<MeshRenderer>();
            if (basinCollider == null) basinCollider = child.GetComponent<MeshCollider>();
            if (basinCollider == null) basinCollider = child.gameObject.AddComponent<MeshCollider>();
            if (basinMesh == null)
            {
                basinMesh = new Mesh { name = "Neon Liquid Basin Terrain" };
                basinMesh.MarkDynamic();
                if (basinFilter != null) basinFilter.sharedMesh = basinMesh;
            }
        }

        public void Configure(MapProfile profile, LiquidChallengeLayout layout, float trackWidth,
            GameManager manager)
        {
            EnsureComponents();
            ClearContacts();
            settings = profile != null && profile.liquidChallenges != null
                ? profile.liquidChallenges : new LiquidChallengeSettings();
            gameManager = manager;
            CycleIndex = layout.cycleIndex;
            ChallengeIndex = layout.challengeIndex;
            PoolLength = layout.poolLength;
            surfaceY = layout.liquidSurfaceY;
            contactId = GetInstanceID();
            transform.position = new Vector3(layout.poolStart, surfaceY, 0f);

            float visualDepth = settings != null ? Mathf.Max(1f, settings.visualDepth) : 7f;

            if (volume != null)
            {
                volume.center = new Vector3(layout.poolLength * .5f, -visualDepth * .5f, 0f);
                volume.size = new Vector3(layout.poolLength, visualDepth, Mathf.Max(2f, trackWidth));
            }

            if (volume2D != null)
            {
                volume2D.offset = new Vector2(layout.poolLength * .5f, -visualDepth * .5f);
                volume2D.size = new Vector2(layout.poolLength, visualDepth);
            }

            if (basinRenderer != null) basinRenderer.enabled = false;
            if (basinCollider != null) basinCollider.enabled = false;

            if (liquidRenderer != null)
            {
                liquidRenderer.sprite = settings.liquidSprite;
                liquidRenderer.drawMode = SpriteDrawMode.Tiled;
                liquidRenderer.tileMode = SpriteTileMode.Continuous;
                liquidRenderer.size = new Vector2(layout.poolLength, visualDepth);
                liquidRenderer.sortingOrder = -1;
                // Position behind the front track edge (Z > -trackWidth * 0.5f)
                liquidRenderer.transform.localPosition = new Vector3(layout.poolLength * .5f,
                    -visualDepth * .5f, 0.05f);
                Color liquidColor = liquidRenderer.color;
                liquidColor.a = settings.normalAlpha;
                liquidRenderer.color = liquidColor;
            }

            if (cautionRenderer != null)
            {
                float cautionX = layout.rampStart - settings.cautionDistanceBeforeRamp;
                float cautionSurface = profile != null ? TerrainGenerator.SampleHeight(profile, cautionX) : surfaceY;
                cautionRenderer.sprite = settings.cautionSprite;
                cautionRenderer.sortingOrder = settings.cautionSortingOrder;
                cautionRenderer.transform.localPosition = new Vector3(cautionX - layout.poolStart,
                    cautionSurface - settings.cautionBelowSurface - surfaceY, -trackWidth * .5f - .10f);
                float sourceWidth = cautionRenderer.sprite != null ? cautionRenderer.sprite.bounds.size.x : 1f;
                cautionRenderer.transform.localScale = Vector3.one *
                    (settings.cautionWidth / Mathf.Max(.001f, sourceWidth));
            }

            gameObject.name = $"Neon Liquid C{CycleIndex} #{ChallengeIndex + 1} - Pooled";
            gameObject.SetActive(true);
        }

        private void ConfigureBasin(MapProfile profile, LiquidChallengeLayout layout, float trackWidth)
        {
            float depth = Mathf.Max(1f, settings != null ? settings.visualDepth : 7f);
            float bottomY = -depth;
            float exitY = profile != null ? TerrainGenerator.SampleHeight(profile, layout.poolEnd) - surfaceY : 0f;
            float rampLength = Mathf.Clamp(settings != null ? settings.exitRampHorizontalLength : 12f, 2f,
                Mathf.Max(2f, layout.poolLength - 2f));
            float rampStartX = Mathf.Max(0f, layout.poolLength - rampLength);
            float skirtBottom = bottomY - Mathf.Max(1f, profile != null ? profile.terrainVisualDepth : 25f);
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
            if (basinMesh != null)
            {
                basinMesh.Clear();
                basinMesh.vertices = vertices;
                basinMesh.uv = uvs;
                if (profile != null && profile.terrainEdgeMaterial != null)
                {
                    basinMesh.subMeshCount = 2;
                    basinMesh.SetTriangles(skirtTriangles, 0);
                    basinMesh.SetTriangles(surfaceTriangles, 1);
                    if (basinRenderer != null)
                        basinRenderer.sharedMaterials = new[] { profile.terrainMaterial, profile.terrainEdgeMaterial };
                }
                else
                {
                    var allTriangles = new int[skirtTriangles.Length + surfaceTriangles.Length];
                    skirtTriangles.CopyTo(allTriangles, 0);
                    surfaceTriangles.CopyTo(allTriangles, skirtTriangles.Length);
                    basinMesh.subMeshCount = 1;
                    basinMesh.SetTriangles(allTriangles, 0);
                    if (basinRenderer != null && profile != null)
                        basinRenderer.sharedMaterial = profile.terrainMaterial;
                }
                basinMesh.RecalculateNormals();
                basinMesh.RecalculateBounds();
            }
            if (basinCollider != null)
            {
                basinCollider.sharedMesh = null;
                basinCollider.sharedMesh = basinMesh;
                if (profile != null)
                    basinCollider.sharedMaterial = profile.groundPhysicsMaterial;
            }
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
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.NotifyLiquidContact(contactId, true);
        }

        private static bool TryGetChassisVehicle(Collider other, out VehicleRuntimeBase vehicle)
        {
            vehicle = other != null ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            return vehicle != null && vehicle.Body != null && other.attachedRigidbody == vehicle.Body;
        }

        internal void AddContact2D(Collider2D other)
        {
            if (!TryGetChassisVehicle2D(other, out VehicleRuntimeBase vehicle) || !chassisContacts2D.Add(other)) return;
            if (entryElapsed.ContainsKey(vehicle)) return;
            entryElapsed.Add(vehicle, 0f);
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.NotifyLiquidContact(contactId, true);
        }

        internal void RemoveContact2D(Collider2D other)
        {
            if (!chassisContacts2D.Remove(other)) return;
            if (!TryGetChassisVehicle2D(other, out VehicleRuntimeBase vehicle)) return;
            foreach (Collider2D contact in chassisContacts2D)
                if (TryGetChassisVehicle2D(contact, out VehicleRuntimeBase remaining) && remaining == vehicle) return;
            entryElapsed.Remove(vehicle);
            gameManager?.NotifyLiquidContact(contactId, false);
        }

        private static bool TryGetChassisVehicle2D(Collider2D other, out VehicleRuntimeBase vehicle)
        {
            vehicle = other != null ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            return vehicle != null && vehicle.Body2D != null && other.attachedRigidbody == vehicle.Body2D;
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
                if (vehicle == null) continue;
                float elapsed = entryElapsed[vehicle] + Time.fixedDeltaTime;
                entryElapsed[vehicle] = elapsed;
                float damping = elapsed <= settings.entrySlowdownSeconds
                    ? entryDamping : settings.continuousViscosity;
                float targetY = surfaceY - Mathf.Max(1f, settings.visualDepth) + .75f;

                if (vehicle.Body2D != null)
                {
                    Rigidbody2D body2D = vehicle.Body2D;
                    body2D.AddForce(-body2D.linearVelocity * body2D.mass * damping, ForceMode2D.Force);
                    float buoyancy = Physics2D.gravity.magnitude +
                                     Mathf.Clamp((targetY - body2D.position.y) * 4f - body2D.linearVelocity.y * 3f,
                                         -Mathf.Max(.1f, settings.maximumSinkAcceleration), 8f);
                    body2D.AddForce(Vector2.up * (body2D.mass * buoyancy), ForceMode2D.Force);
                    body2D.AddForce((Vector2)vehicle.transform.right *
                                  (body2D.mass * settings.liquidDriveAcceleration * Mathf.Clamp01(vehicle.AppliedThrottle)),
                        ForceMode2D.Force);
                }
                else if (vehicle.Body != null)
                {
                    Rigidbody body = vehicle.Body;
                    body.AddForce(-body.linearVelocity * body.mass * damping, ForceMode.Force);
                    float buoyancy = Physics.gravity.magnitude +
                                     Mathf.Clamp((targetY - body.position.y) * 4f - body.linearVelocity.y * 3f,
                                         -Mathf.Max(.1f, settings.maximumSinkAcceleration), 8f);
                    body.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
                    body.AddForce(vehicle.transform.right *
                                  (settings.liquidDriveAcceleration * Mathf.Clamp01(vehicle.AppliedThrottle)),
                        ForceMode.Acceleration);
                }
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
            chassisContacts2D.Clear();
            entryElapsed.Clear();
        }
    }

    public sealed class NeonLiquidHazard2DForwarder : MonoBehaviour
    {
        private NeonLiquidHazard parentHazard;

        public void AssignParent(NeonLiquidHazard parent)
        {
            parentHazard = parent;
        }

        private void Awake()
        {
            if (parentHazard == null) parentHazard = GetComponentInParent<NeonLiquidHazard>();
        }

        private void OnTriggerEnter2D(Collider2D other) => parentHazard?.AddContact2D(other);
        private void OnTriggerStay2D(Collider2D other) => parentHazard?.AddContact2D(other);
        private void OnTriggerExit2D(Collider2D other) => parentHazard?.RemoveContact2D(other);
    }
}
