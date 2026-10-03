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
        private readonly LastHorizonFuelSequence lastHorizonFuelSequence = new LastHorizonFuelSequence();
        private readonly List<LastHorizonFuelPoint> lastHorizonFuelBuffer =
            new List<LastHorizonFuelPoint>(4);
        private readonly RandomGemRouteSequence gemSequence = new RandomGemRouteSequence();
        private readonly List<GemRoutePoint> gemPointBuffer = new List<GemRoutePoint>(2);
        private MeshCollider authoritativeTerrainCollider;
        private Mesh authoritativeTerrainMesh;
        private EdgeCollider2D authoritativeTerrainCollider2D;
        private readonly List<EdgeCollider2D> authoritativeTerrainColliderSegments2D =
            new List<EdgeCollider2D>(4);
        private PhysicsMaterial2D authoritativeTerrainMaterial2D;
        private readonly List<Vector2> authoritativeSurfaceKnots2D = new List<Vector2>(512);
        private bool hasMountainsSafetyPosition;
        private readonly List<NeonLiquidHazard> liquidHazardPool = new List<NeonLiquidHazard>(12);
        private readonly Dictionary<long, NeonLiquidHazard> activeLiquidHazards =
            new Dictionary<long, NeonLiquidHazard>();
        private readonly HashSet<long> requiredLiquidHazards = new HashSet<long>();
        private readonly List<long> liquidKeyBuffer = new List<long>(12);
        private readonly List<CityBridgeZone> cityBridgePool = new List<CityBridgeZone>(6);
        private readonly Dictionary<long, CityBridgeZone> activeCityBridges =
            new Dictionary<long, CityBridgeZone>();
        private readonly HashSet<long> requiredCityBridges = new HashSet<long>();
        private readonly List<long> cityBridgeKeyBuffer = new List<long>(6);
        private readonly List<SpaceGapEvent> spaceEventPool = new List<SpaceGapEvent>(4);
        private readonly Dictionary<int, SpaceGapEvent> activeSpaceEvents = new Dictionary<int, SpaceGapEvent>();
        private readonly HashSet<int> requiredSpaceEvents = new HashSet<int>();
        private readonly List<int> spaceEventKeyBuffer = new List<int>(4);
        private readonly List<ArenaSegmentEvent> arenaEventPool = new List<ArenaSegmentEvent>(8);
        private readonly Dictionary<int, ArenaSegmentEvent> activeArenaEvents =
            new Dictionary<int, ArenaSegmentEvent>();
        private readonly HashSet<int> requiredArenaEvents = new HashSet<int>();
        private readonly List<int> arenaEventKeyBuffer = new List<int>(8);
        private readonly List<LastHorizonObstacleEvent> lastHorizonObstaclePool =
            new List<LastHorizonObstacleEvent>(10);
        private readonly Dictionary<int, LastHorizonObstacleEvent> activeLastHorizonObstacles =
            new Dictionary<int, LastHorizonObstacleEvent>();
        private readonly HashSet<int> requiredLastHorizonObstacles = new HashSet<int>();
        private readonly List<int> lastHorizonObstacleKeyBuffer = new List<int>(8);
        private readonly List<LastHorizonSpecialPhaseEvent> lastHorizonSpecialPhasePool =
            new List<LastHorizonSpecialPhaseEvent>(6);
        private readonly Dictionary<long, LastHorizonSpecialPhaseEvent> activeLastHorizonSpecialPhases =
            new Dictionary<long, LastHorizonSpecialPhaseEvent>();
        private readonly HashSet<long> requiredLastHorizonSpecialPhases = new HashSet<long>();
        private readonly List<long> lastHorizonSpecialPhaseKeyBuffer = new List<long>(6);
        private readonly List<LastHorizonSpecialPhaseLayout> lastHorizonSpecialPhaseLayoutBuffer =
            new List<LastHorizonSpecialPhaseLayout>(8);

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
        public float CurrentFuelSpacing => mapProfile != null && mapProfile.useLastHorizonTerrain
            ? lastHorizonFuelSequence.CurrentSpacing : fuelSequence.CurrentSpacing;
        public float NextFuelSequencePosition => mapProfile != null && mapProfile.useLastHorizonTerrain
            ? lastHorizonFuelSequence.NextPosition : fuelSequence.NextPosition;
        public float CurrentMountainsTargetSlope => mapProfile != null && mapProfile.useMountainsTerrain
            ? TerrainGenerator.GetMountainsTargetSlope(mapProfile, VehicleWorldX) : 0f;
        public EdgeCollider2D AuthoritativeCollider2D => authoritativeTerrainCollider2D;
        private float VehicleWorldX => vehicle != null && vehicle.Body2D != null
            ? vehicle.Body2D.position.x
            : vehicle != null ? vehicle.transform.position.x : 0f;
        private int RuntimeSamples => mapProfile != null && mapProfile.useLastHorizonTerrain
            ? Mathf.Max(samples, 128) : mapProfile != null && mapProfile.useMountainsTerrain
            ? Mathf.Max(samples, 192) : mapProfile != null && mapProfile.useSpaceTerrain
                ? Mathf.Max(samples, 96) : mapProfile != null && mapProfile.useArenaTerrain
                ? Mathf.Max(samples, 80) : samples;

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
            lastHorizonFuelSequence.Reset(mapProfile);
            int gemRouteSeed = unchecked((mapProfile.seed * 397) ^ System.Environment.TickCount ^ GetInstanceID());
            gemSequence.Reset(0f, gemRouteSeed);
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
            if (mapProfile.useMountainsTerrain && vehicle.Body2D != null)
            {
                LiftMountainVehicleClearOfSurface();
                hasMountainsSafetyPosition = true;
            }
            PrewarmLiquidHazards(count + 2);
            RefreshLiquidHazards();
            PrewarmCityBridges(4);
            RefreshCityBridges();
            PrewarmSpaceEvents(3);
            RefreshSpaceEvents();
            PrewarmArenaEvents(6);
            RefreshArenaEvents();
            PrewarmLastHorizonObstacles(10);
            PrewarmLastHorizonSpecialPhases(6);
            RefreshLastHorizonSpecialPhases();
            RefreshLastHorizonObstacles();
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

            // The authoritative collider is rebuilt only when the loaded terrain changes. Rebuilding it every
            // rendered frame invalidates Box2D contacts; leaving the per-chunk colliders enabled creates
            // overlapping edge endpoints that behave like invisible walls.
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

            float targetY;
            if (TerrainGenerator.TrySampleLastHorizonSpecialPickupSurface(mapProfile, targetWorldX,
                    out float specialSurfaceY, out _))
                targetY = specialSurfaceY + 2.5f;
            else
                targetY = TrySampleAuthoritativeSurface(targetWorldX, out float surfaceY, out _)
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
            RefreshCityBridges();
            RefreshSpaceEvents();
            RefreshArenaEvents();
            RefreshLastHorizonSpecialPhases();
            RefreshLastHorizonObstacles();
        }

        private void FixedUpdate()
        {
            if (mapProfile == null || !mapProfile.useMountainsTerrain || vehicle == null ||
                vehicle.Body2D == null || chunks == null)
            {
                hasMountainsSafetyPosition = false;
                return;
            }

            Rigidbody2D body = vehicle.Body2D;
            Vector2 current = body.position;
            if (!hasMountainsSafetyPosition)
            {
                hasMountainsSafetyPosition = true;
                return;
            }

            if (TrySampleAuthoritativeSurface(current.x, out float surfaceY, out _) &&
                current.y < surfaceY - .22f)
            {
                // Mountains never contains intentional gaps. If the chassis centre has crossed beneath
                // the exact rendered surface, Box2D missed a fast/steep contact. Move every articulated
                // Rigidbody2D together so joints remain intact and the vehicle cannot fall through the map.
                float wheelRadius = vehicle.Definition != null ? vehicle.Definition.wheelRadius : .6f;
                float safeY = surfaceY + Mathf.Max(.95f, wheelRadius * 1.55f);
                Vector2 correction = new Vector2(0f, safeY - current.y);
                TranslateMountainVehicle(correction, true);
            }

            // A wheel can start penetrating before the much higher chassis centre crosses the surface.
            // Correcting both tire centres every physics step prevents a wheel from becoming wedged in a
            // steep face and also recovers safely if Box2D misses a fast contact near a chunk boundary.
            LiftMountainVehicleClearOfSurface();
        }

        private void LiftMountainVehicleClearOfSurface()
        {
            if (vehicle == null || vehicle.Body2D == null) return;
            float wheelRadius = vehicle.Definition != null ? vehicle.Definition.wheelRadius : .6f;
            float requiredLift = 0f;
            Vector2 supportNormal = Vector2.up;
            VehiclePhysics2DController physics = vehicle as VehiclePhysics2DController;
            Suspension2DController[] suspensions = physics != null
                ? new[] { physics.RearSuspension, physics.FrontSuspension }
                : vehicle.GetComponentsInChildren<Suspension2DController>(true);
            for (int i = 0; i < suspensions.Length; i++)
            {
                Rigidbody2D wheel = suspensions[i] != null ? suspensions[i].WheelBody : null;
                if (wheel == null || !TrySampleAuthoritativeSurface(wheel.position.x, out float surfaceY,
                        out float slopeDegrees))
                    continue;
                float slopeCosine = Mathf.Max(.30f, Mathf.Cos(Mathf.Abs(slopeDegrees) * Mathf.Deg2Rad));
                float verticalTireClearance = wheelRadius / slopeCosine + .055f;
                float lift = surfaceY + verticalTireClearance - wheel.position.y;
                if (lift > requiredLift)
                {
                    requiredLift = lift;
                    float radians = slopeDegrees * Mathf.Deg2Rad;
                    supportNormal = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
                }
            }
            if (requiredLift <= .001f) return;

            Vector2 correction = Vector2.up * requiredLift;
            TranslateMountainVehicle(correction, false);
            // Position correction supplies the contact missed by Box2D. Leaving
            // the incoming velocity intact made the rig keep falling into this
            // safety plane every step, even after both wheels had landed.
            // Preserve tangent motion and outward/airborne velocity.
            RemoveIncomingMountainVelocity(vehicle.Body2D, supportNormal);
            for (int i = 0; i < suspensions.Length; i++)
                RemoveIncomingMountainVelocity(suspensions[i] != null ? suspensions[i].WheelBody : null, supportNormal);
        }

        private static void RemoveIncomingMountainVelocity(Rigidbody2D part, Vector2 normal)
        {
            if (part == null) return;
            float incoming = Vector2.Dot(part.linearVelocity, normal);
            if (incoming < 0f) part.linearVelocity -= normal * incoming;
        }

        private void TranslateMountainVehicle(Vector2 correction, bool dampVelocity)
        {
            if (vehicle == null) return;
            MoveMountainBody(vehicle.Body2D, correction, dampVelocity);
            VehiclePhysics2DController physics = vehicle as VehiclePhysics2DController;
            if (physics == null) return;
            MoveMountainBody(physics.RearSuspension != null ? physics.RearSuspension.WheelBody : null,
                correction, dampVelocity);
            MoveMountainBody(physics.FrontSuspension != null ? physics.FrontSuspension.WheelBody : null,
                correction, dampVelocity);
        }

        private static void MoveMountainBody(Rigidbody2D part, Vector2 correction, bool dampVelocity)
        {
            if (part == null) return;
            part.position += correction;
            if (!dampVelocity) return;
            Vector2 velocity = part.linearVelocity;
            velocity.y = Mathf.Max(0f, velocity.y * .1f);
            part.linearVelocity = velocity;
            part.angularVelocity *= .35f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (mapProfile == null || !mapProfile.useMountainsTerrain) return;
            float start = vehicle != null ? Mathf.Max(0f, VehicleWorldX - 10f) : 0f;
            Vector3 previous = new Vector3(start, TerrainGenerator.SampleHeight(mapProfile, start), 0f);
            for (float x = start + 2f; x <= start + 120f; x += 2f)
            {
                Vector3 point = new Vector3(x, TerrainGenerator.SampleHeight(mapProfile, x), 0f);
                float slope = Mathf.Abs(Mathf.Atan2(point.y - previous.y, point.x - previous.x) * Mathf.Rad2Deg);
                Gizmos.color = Color.Lerp(new Color(.35f, .85f, .45f), new Color(1f, .22f, .08f),
                    Mathf.InverseLerp(20f, 80f, slope));
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }
#endif

        public bool TrySampleAuthoritativeSurface(float worldX, out float height, out float slopeDegrees)
        {
            height = 0f;
            slopeDegrees = 0f;
            if (chunks == null) return false;
            if (TerrainGenerator.IsTerrainGap(mapProfile, worldX)) return false;

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
                RebuildAuthoritativeTerrainCollider2D();
                RefreshLiquidHazards();
                RefreshCityBridges();
                RefreshSpaceEvents();
                RefreshArenaEvents();
                RefreshLastHorizonSpecialPhases();
                RefreshLastHorizonObstacles();
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
            RebuildAuthoritativeTerrainCollider2D();
            RefreshLiquidHazards();
            RefreshCityBridges();
            RefreshSpaceEvents();
            RefreshArenaEvents();
            RefreshLastHorizonSpecialPhases();
            RefreshLastHorizonObstacles();
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
                    chunk.RegenerateSurface(index, mapProfile, chunkLength, RuntimeSamples, width, pickupPool,
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
            chunk.Generate(index, mapProfile, chunkLength, RuntimeSamples, width, pickupPool, hasBoundary,
                hasBoundary ? previous.EndHeight : 0f, hasBoundary ? previous.EndSlope : 0f);
            // Physics uses the single welded collider assembled from every rendered surface knot. Enabling
            // this chunk collider too would create duplicate, partially overlapping edges at every seam.
            chunk.SetChunkColliderEnabled(false);
            SpawnDistanceBonusMarkers(chunk, index);
            SpawnCoins(chunk, index);
            SpawnProgressiveFuel(chunk, index);
            SpawnGems(chunk, index);
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
            if (!authoritativeTerrainColliderSegments2D.Contains(authoritativeTerrainCollider2D))
                authoritativeTerrainColliderSegments2D.Insert(0, authoritativeTerrainCollider2D);
            if (authoritativeTerrainMaterial2D == null)
            {
                authoritativeTerrainMaterial2D = new PhysicsMaterial2D("AuthoritativeTerrain2D")
                {
                    friction = .85f,
                    bounciness = 0f
                };
            }
            // The collider path itself is the painted driving surface. A non-zero EdgeCollider2D radius
            // inflates that path and makes tires visibly hover above the authored line (Mountains used 14 cm).
            authoritativeTerrainCollider2D.edgeRadius = 0f;
            authoritativeTerrainCollider2D.sharedMaterial = mapProfile != null && mapProfile.groundPhysicsMaterial2D != null
                ? mapProfile.groundPhysicsMaterial2D : authoritativeTerrainMaterial2D;
        }

        private void RebuildAuthoritativeTerrainCollider2D()
        {
            if (mapProfile == null || vehicle == null || vehicle.Body2D == null || chunks == null) return;
            EnsureAuthoritativeTerrainCollider();

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
                SetActiveColliderSegmentCount(0);
                return;
            }

            // Keep collision across the complete loaded terrain window. The old fixed 180-point look-ahead
            // ended around 75 m while recycling happened at 80 m, creating a real five-metre false void at
            // the start of Mountains. Loaded chunks are already pooled, so using all their knots is stable
            // and ensures the collider always extends beyond the next recycle boundary in both directions.
            const int firstKnot = 0;
            int selectedCount = authoritativeSurfaceKnots2D.Count;
            int segmentIndex = 0;
            int runStart = 0;
            for (int edge = 0; edge < selectedCount - 1; edge++)
            {
                Vector2 a = authoritativeSurfaceKnots2D[firstKnot + edge];
                Vector2 b = authoritativeSurfaceKnots2D[firstKnot + edge + 1];
                if (!TerrainGenerator.IsTerrainGap(mapProfile, (a.x + b.x) * .5f)) continue;
                if (edge - runStart + 1 >= 2)
                    WriteColliderSegment(segmentIndex++, firstKnot + runStart, edge - runStart + 1);
                runStart = edge + 1;
            }
            if (selectedCount - runStart >= 2)
                WriteColliderSegment(segmentIndex++, firstKnot + runStart, selectedCount - runStart);
            SetActiveColliderSegmentCount(segmentIndex);
        }

        private void WriteColliderSegment(int segmentIndex, int firstKnot, int pointCount)
        {
            EdgeCollider2D collider = GetColliderSegment(segmentIndex);
            var points = new Vector2[pointCount];
            Transform colliderTransform = collider.transform;
            for (int i = 0; i < pointCount; i++)
            {
                Vector2 world = authoritativeSurfaceKnots2D[firstKnot + i];
                Vector3 local = colliderTransform.InverseTransformPoint(new Vector3(world.x, world.y, 0f));
                points[i] = new Vector2(local.x, local.y);
            }
            collider.points = points;
            collider.enabled = true;
        }

        private EdgeCollider2D GetColliderSegment(int index)
        {
            while (authoritativeTerrainColliderSegments2D.Count <= index)
            {
                GameObject child = new GameObject($"Authoritative Terrain 2D Segment {authoritativeTerrainColliderSegments2D.Count + 1}");
                child.transform.SetParent(transform, false);
                EdgeCollider2D collider = child.AddComponent<EdgeCollider2D>();
                collider.edgeRadius = 0f;
                collider.sharedMaterial = mapProfile != null && mapProfile.groundPhysicsMaterial2D != null
                    ? mapProfile.groundPhysicsMaterial2D : authoritativeTerrainMaterial2D;
                authoritativeTerrainColliderSegments2D.Add(collider);
            }
            return authoritativeTerrainColliderSegments2D[index];
        }

        private void SetActiveColliderSegmentCount(int count)
        {
            for (int i = count; i < authoritativeTerrainColliderSegments2D.Count; i++)
                if (authoritativeTerrainColliderSegments2D[i] != null)
                    authoritativeTerrainColliderSegments2D[i].enabled = false;
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
                        if (!TerrainGenerator.IsTerrainGap(mapProfile, segmentX))
                        {
                            triangles.Add(previous);
                            triangles.Add(previous + 1);
                            triangles.Add(current);
                            triangles.Add(previous + 1);
                            triangles.Add(current + 1);
                            triangles.Add(current);

                            // Reverse winding for solid double-sided terrain collision
                            triangles.Add(current);
                            triangles.Add(previous + 1);
                            triangles.Add(previous);
                            triangles.Add(current);
                            triangles.Add(current + 1);
                            triangles.Add(previous + 1);
                        }
                    }
                    previousWorldX = leftWorld.x;
                    pairCount++;
                }
            }

            bool hasNonDegenerateTriangle = false;
            for (int triangle = 0; triangle + 2 < triangles.Count; triangle += 3)
            {
                Vector3 a = vertices[triangles[triangle]];
                Vector3 b = vertices[triangles[triangle + 1]];
                Vector3 c = vertices[triangles[triangle + 2]];
                if (Vector3.Cross(b - a, c - a).sqrMagnitude <= .00000001f) continue;
                hasNonDegenerateTriangle = true;
                break;
            }

            // A loaded range can temporarily contain only the authored Space void. Unity rejects an
            // empty/degenerate MeshCollider, so leave it disabled until solid terrain is loaded again.
            if (!hasNonDegenerateTriangle)
            {
                authoritativeTerrainCollider.sharedMesh = null;
                authoritativeTerrainCollider.enabled = false;
                authoritativeTerrainMesh.Clear();
                return;
            }

            authoritativeTerrainMesh.Clear();
            authoritativeTerrainMesh.SetVertices(vertices);
            authoritativeTerrainMesh.SetTriangles(triangles, 0, true);
            authoritativeTerrainMesh.RecalculateNormals();
            authoritativeTerrainMesh.RecalculateBounds();
            authoritativeTerrainCollider.sharedMesh = null;
            authoritativeTerrainCollider.enabled = true;
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
                        TerrainGenerator.IsTerrainGap(mapProfile, x) ||
                        TerrainGenerator.IsArenaReservedObstacleZone(mapProfile, x) ||
                        TerrainGenerator.IsLastHorizonObstacleReserved(mapProfile, x) ||
                        !chunk.TrySampleSurface(x, out float y, out _)) continue;
                    CoinPickup pickup = pickupPool.SpawnCoin(point.value,
                        new Vector3(x, y + route.surfaceOffset + point.heightOffset, 0f), x,
                        route.GetStableCoinId(group, pointIndex));
                    chunk.TrackPickup(pickup);
                }
            }
        }

        private void SpawnDistanceBonusMarkers(TerrainChunk chunk, int index)
        {
            if (chunk == null || index < 0) return;
            float start = index * chunkLength;
            float end = start + chunkLength;
            int firstMilestone = Mathf.Max(1,
                Mathf.CeilToInt(Mathf.Max(start, DistanceBonusTracker.IntervalMeters) /
                                DistanceBonusTracker.IntervalMeters));
            for (int milestone = firstMilestone; ; milestone++)
            {
                float distance = milestone * DistanceBonusTracker.IntervalMeters;
                if (distance >= end) break;
                if (distance < start || TerrainGenerator.IsTerrainGap(mapProfile, distance) ||
                    !chunk.TrySampleSurface(distance, out float surfaceY, out _)) continue;

                DistanceBonusMarker marker = DistanceBonusMarker.Create(chunk.transform,
                    new Vector3(distance, surfaceY + 1.7f, -.35f), distance, false);
                chunk.TrackBonusMarker(marker);
            }
        }

        private void SpawnProgressiveFuel(TerrainChunk chunk, int index)
        {
            if (pickupPool == null || index < 0)
                return;
            if (mapProfile != null && mapProfile.useLastHorizonTerrain)
            {
                SpawnLastHorizonFuel(chunk, index);
                return;
            }
            float start = index * chunkLength;
            float end = start + chunkLength;
            fuelSequence.AdvancePast(start);
            int safety = 0;
            while (fuelSequence.NextPosition < end && safety++ < 128)
            {
                float targetDistance = fuelSequence.NextPosition;
                Vector3 position = FindAccessibleFuelPosition(chunk, start, end, targetDistance);
                if (float.IsNaN(position.x))
                {
                    fuelSequence.Advance(targetDistance + 12f);
                    continue;
                }
                PickupBase pickup = pickupPool.Spawn(PickupKind.Fuel, position, position.x);
                chunk.TrackPickup(pickup);
                fuelSequence.Advance(position.x);
            }
        }

        private void SpawnLastHorizonFuel(TerrainChunk chunk, int index)
        {
            float start = index * chunkLength;
            float end = start + chunkLength;
            lastHorizonFuelSequence.GetPoints(start, end, lastHorizonFuelBuffer);
            for (int i = 0; i < lastHorizonFuelBuffer.Count; i++)
            {
                LastHorizonFuelPoint point = lastHorizonFuelBuffer[i];
                float pickupClearance;
                if (!TerrainGenerator.TrySampleLastHorizonSpecialPickupSurface(mapProfile,
                        point.Position, out float surfaceY, out pickupClearance))
                {
                    if (!chunk.TrySampleSurface(point.Position, out surfaceY, out _)) continue;
                    pickupClearance = .68f;
                }
                PickupBase pickup = pickupPool.SpawnFuel(
                    new Vector3(point.Position, surfaceY + pickupClearance, 0f), point.Position,
                    point.StableId);
                chunk.TrackPickup(pickup);
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
                    if (TerrainGenerator.IsLiquidPool(mapProfile, candidateX) ||
                        TerrainGenerator.IsTerrainGap(mapProfile, candidateX) ||
                        TerrainGenerator.IsArenaReservedObstacleZone(mapProfile, candidateX) ||
                        TerrainGenerator.IsLastHorizonObstacleReserved(mapProfile, candidateX)) continue;
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
            return bestSlope < float.MaxValue
                ? new Vector3(bestX, bestY + .68f, 0f)
                : new Vector3(float.NaN, float.NaN, 0f);
        }

        private void SpawnGems(TerrainChunk chunk, int index)
        {
            if (pickupPool == null || chunk == null || index < 0) return;
            float start = index * chunkLength;
            float end = start + chunkLength;
            gemSequence.GetPoints(start, end, gemPointBuffer);
            for (int i = 0; i < gemPointBuffer.Count; i++)
            {
                GemRoutePoint point = gemPointBuffer[i];
                Vector3 position = FindAccessibleGemPosition(chunk, start, end, point.Position);
                if (float.IsNaN(position.x)) continue;
                GemPickup pickup = pickupPool.SpawnGem(position, position.x, point.StableId);
                chunk.TrackPickup(pickup);
            }
        }

        private Vector3 FindAccessibleGemPosition(TerrainChunk chunk, float chunkStart, float chunkEnd,
            float targetDistance)
        {
            const float searchRadius = 18f;
            const float searchStep = 2f;
            const float edgeInset = 1f;
            int steps = Mathf.CeilToInt(searchRadius / searchStep);
            for (int i = 0; i <= steps; i++)
            {
                int sideCount = i == 0 ? 1 : 2;
                for (int sideIndex = 0; sideIndex < sideCount; sideIndex++)
                {
                    float signedOffset = i == 0 ? 0f : i * searchStep * (sideIndex == 0 ? -1f : 1f);
                    float candidateX = Mathf.Clamp(targetDistance + signedOffset,
                        chunkStart + edgeInset, chunkEnd - edgeInset);
                    if (TerrainGenerator.IsLiquidPool(mapProfile, candidateX) ||
                        TerrainGenerator.IsTerrainGap(mapProfile, candidateX) ||
                        TerrainGenerator.IsArenaReservedObstacleZone(mapProfile, candidateX) ||
                        TerrainGenerator.IsLastHorizonObstacleReserved(mapProfile, candidateX) ||
                        pickupPool.IsFuelReserved(candidateX) ||
                        !chunk.TrySampleSurface(candidateX, out float surfaceY, out _))
                        continue;
                    return new Vector3(candidateX, surfaceY + 1.25f, 0f);
                }
            }
            return new Vector3(float.NaN, float.NaN, 0f);
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

        private void PrewarmCityBridges(int count)
        {
            if (mapProfile == null || !mapProfile.useCityBridges) return;
            while (cityBridgePool.Count < count)
            {
                GameObject zoneObject = new GameObject("City Bridge - Available Pooled Instance");
                zoneObject.transform.SetParent(transform, false);
                CityBridgeZone zone = zoneObject.AddComponent<CityBridgeZone>();
                zoneObject.SetActive(false);
                cityBridgePool.Add(zone);
            }
        }

        private void RefreshCityBridges()
        {
            if (mapProfile == null || !mapProfile.useCityBridges || mapProfile.cityBridges == null)
            {
                for (int i = 0; i < cityBridgePool.Count; i++) cityBridgePool[i]?.Deactivate();
                activeCityBridges.Clear();
                return;
            }

            float loadedStart = minimumLoadedIndex * chunkLength;
            float loadedEnd = (maximumLoadedIndex + 1) * chunkLength;
            int firstCycle = Mathf.Max(0, Mathf.FloorToInt(
                Mathf.Max(0f, loadedStart - 200f) / CityBridgeSettings.RequiredCycleLength));
            int lastCycle = Mathf.Max(0, Mathf.FloorToInt(
                Mathf.Max(0f, loadedEnd + 200f) / CityBridgeSettings.RequiredCycleLength));
            requiredCityBridges.Clear();
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                int count = TerrainGenerator.GetCityBridgeZoneCount(mapProfile, cycle);
                for (int slot = 0; slot < count; slot++)
                {
                    CityBridgeZoneLayout layout = TerrainGenerator.GetCityBridgeZone(mapProfile, cycle, slot);
                    if (layout.recoveryEnd < loadedStart || layout.approachStart > loadedEnd) continue;
                    long key = ((long)cycle << 32) | (uint)slot;
                    requiredCityBridges.Add(key);
                    if (activeCityBridges.ContainsKey(key)) continue;
                    CityBridgeZone zone = GetAvailableCityBridge();
                    zone.Configure(mapProfile, layout, width);
                    activeCityBridges.Add(key, zone);
                }
            }

            cityBridgeKeyBuffer.Clear();
            foreach (KeyValuePair<long, CityBridgeZone> pair in activeCityBridges)
                if (!requiredCityBridges.Contains(pair.Key)) cityBridgeKeyBuffer.Add(pair.Key);
            for (int i = 0; i < cityBridgeKeyBuffer.Count; i++)
            {
                long key = cityBridgeKeyBuffer[i];
                activeCityBridges[key].Deactivate();
                activeCityBridges.Remove(key);
            }
        }

        private CityBridgeZone GetAvailableCityBridge()
        {
            for (int i = 0; i < cityBridgePool.Count; i++)
                if (cityBridgePool[i] != null && !cityBridgePool[i].gameObject.activeSelf)
                    return cityBridgePool[i];
            PrewarmCityBridges(cityBridgePool.Count + 1);
            return cityBridgePool[cityBridgePool.Count - 1];
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

        private void PrewarmSpaceEvents(int count)
        {
            if (mapProfile == null || !mapProfile.useSpaceTerrain) return;
            while (spaceEventPool.Count < count)
            {
                GameObject eventObject = new GameObject("Space Event - Available Pooled Instance");
                eventObject.transform.SetParent(transform, false);
                SpaceGapEvent spaceEvent = eventObject.AddComponent<SpaceGapEvent>();
                eventObject.SetActive(false);
                spaceEventPool.Add(spaceEvent);
            }
        }

        private void RefreshSpaceEvents()
        {
            if (mapProfile == null || !mapProfile.useSpaceTerrain || mapProfile.spaceChallenges == null)
            {
                for (int i = 0; i < spaceEventPool.Count; i++) spaceEventPool[i]?.Deactivate();
                activeSpaceEvents.Clear();
                return;
            }

            float loadedStart = minimumLoadedIndex * chunkLength;
            float loadedEnd = (maximumLoadedIndex + 1) * chunkLength;
            float interval = Mathf.Max(250f, mapProfile.spaceChallenges.eventInterval);
            int firstPhase = Mathf.Max(1, Mathf.FloorToInt((loadedStart - 220f) / interval));
            int lastPhase = Mathf.Max(1, Mathf.CeilToInt((loadedEnd + 120f) / interval));
            requiredSpaceEvents.Clear();
            for (int phase = firstPhase; phase <= lastPhase; phase++)
            {
                SpaceChallengeLayout layout = TerrainGenerator.GetSpaceChallenge(mapProfile, phase);
                if (layout.recoveryEnd < loadedStart || layout.warningX > loadedEnd) continue;
                requiredSpaceEvents.Add(phase);
                if (activeSpaceEvents.ContainsKey(phase)) continue;
                SpaceGapEvent spaceEvent = GetAvailableSpaceEvent();
                spaceEvent.Configure(mapProfile, layout, width, gameManager);
                activeSpaceEvents.Add(phase, spaceEvent);
            }

            spaceEventKeyBuffer.Clear();
            foreach (KeyValuePair<int, SpaceGapEvent> pair in activeSpaceEvents)
                if (!requiredSpaceEvents.Contains(pair.Key)) spaceEventKeyBuffer.Add(pair.Key);
            for (int i = 0; i < spaceEventKeyBuffer.Count; i++)
            {
                int phase = spaceEventKeyBuffer[i];
                activeSpaceEvents[phase].Deactivate();
                activeSpaceEvents.Remove(phase);
            }
        }

        private SpaceGapEvent GetAvailableSpaceEvent()
        {
            for (int i = 0; i < spaceEventPool.Count; i++)
                if (spaceEventPool[i] != null && !spaceEventPool[i].gameObject.activeSelf)
                    return spaceEventPool[i];
            PrewarmSpaceEvents(spaceEventPool.Count + 1);
            return spaceEventPool[spaceEventPool.Count - 1];
        }

        private void PrewarmArenaEvents(int count)
        {
            if (mapProfile == null || !mapProfile.useArenaTerrain) return;
            while (arenaEventPool.Count < count)
            {
                GameObject eventObject = new GameObject("Arena Segment - Available Pooled Instance");
                eventObject.transform.SetParent(transform, false);
                ArenaSegmentEvent arenaEvent = eventObject.AddComponent<ArenaSegmentEvent>();
                eventObject.SetActive(false);
                arenaEventPool.Add(arenaEvent);
            }
        }

        private void RefreshArenaEvents()
        {
            if (mapProfile == null || !mapProfile.useArenaTerrain || mapProfile.arena == null)
            {
                for (int i = 0; i < arenaEventPool.Count; i++) arenaEventPool[i]?.Deactivate();
                activeArenaEvents.Clear();
                return;
            }
            float loadedStart = minimumLoadedIndex * chunkLength;
            float loadedEnd = (maximumLoadedIndex + 1) * chunkLength;
            float segmentLength = Mathf.Max(70f, mapProfile.arena.segmentLength);
            int firstSegment = Mathf.Max(0, Mathf.FloorToInt((loadedStart - segmentLength) / segmentLength));
            int lastSegment = Mathf.Max(0, Mathf.CeilToInt((loadedEnd + segmentLength) / segmentLength));
            requiredArenaEvents.Clear();
            for (int segment = firstSegment; segment <= lastSegment; segment++)
            {
                ArenaSegmentLayout layout = TerrainGenerator.GetArenaSegment(mapProfile, segment);
                if (!layout.HasTires && !layout.HasPlatform && !layout.HasLava) continue;
                if (layout.end < loadedStart || layout.start > loadedEnd) continue;
                requiredArenaEvents.Add(segment);
                if (activeArenaEvents.ContainsKey(segment)) continue;
                ArenaSegmentEvent arenaEvent = GetAvailableArenaEvent();
                arenaEvent.Configure(mapProfile, layout, width);
                activeArenaEvents.Add(segment, arenaEvent);
            }

            arenaEventKeyBuffer.Clear();
            foreach (KeyValuePair<int, ArenaSegmentEvent> pair in activeArenaEvents)
                if (!requiredArenaEvents.Contains(pair.Key)) arenaEventKeyBuffer.Add(pair.Key);
            for (int i = 0; i < arenaEventKeyBuffer.Count; i++)
            {
                int key = arenaEventKeyBuffer[i];
                activeArenaEvents[key].Deactivate();
                activeArenaEvents.Remove(key);
            }
        }

        private ArenaSegmentEvent GetAvailableArenaEvent()
        {
            for (int i = 0; i < arenaEventPool.Count; i++)
                if (arenaEventPool[i] != null && !arenaEventPool[i].gameObject.activeSelf)
                    return arenaEventPool[i];
            PrewarmArenaEvents(arenaEventPool.Count + 1);
            return arenaEventPool[arenaEventPool.Count - 1];
        }

        private void PrewarmLastHorizonSpecialPhases(int count)
        {
            if (mapProfile == null || !mapProfile.useLastHorizonTerrain) return;
            while (lastHorizonSpecialPhasePool.Count < count)
            {
                GameObject phaseObject = new GameObject(
                    "Last Horizon Special Phase - Available Pooled Instance");
                phaseObject.transform.SetParent(transform, false);
                LastHorizonSpecialPhaseEvent phase =
                    phaseObject.AddComponent<LastHorizonSpecialPhaseEvent>();
                phaseObject.SetActive(false);
                lastHorizonSpecialPhasePool.Add(phase);
            }
        }

        private void RefreshLastHorizonSpecialPhases()
        {
            if (mapProfile == null || !mapProfile.useLastHorizonTerrain || mapProfile.lastHorizon == null)
            {
                for (int i = 0; i < lastHorizonSpecialPhasePool.Count; i++)
                    lastHorizonSpecialPhasePool[i]?.Deactivate();
                activeLastHorizonSpecialPhases.Clear();
                return;
            }

            float loadedStart = minimumLoadedIndex * chunkLength;
            float loadedEnd = (maximumLoadedIndex + 1) * chunkLength;
            float padding = Mathf.Max(40f, mapProfile.lastHorizon.specialPhaseTransitionLength);
            TerrainGenerator.GetLastHorizonSpecialPhases(mapProfile, loadedStart - padding,
                loadedEnd + padding, lastHorizonSpecialPhaseLayoutBuffer);
            requiredLastHorizonSpecialPhases.Clear();
            for (int i = 0; i < lastHorizonSpecialPhaseLayoutBuffer.Count; i++)
            {
                LastHorizonSpecialPhaseLayout layout = lastHorizonSpecialPhaseLayoutBuffer[i];
                if (layout.end < loadedStart - padding || layout.start > loadedEnd + padding) continue;
                requiredLastHorizonSpecialPhases.Add(layout.key);
                if (activeLastHorizonSpecialPhases.ContainsKey(layout.key)) continue;
                LastHorizonSpecialPhaseEvent phase = GetAvailableLastHorizonSpecialPhase();
                phase.Configure(mapProfile, layout, width, gameManager);
                activeLastHorizonSpecialPhases.Add(layout.key, phase);
            }

            lastHorizonSpecialPhaseKeyBuffer.Clear();
            foreach (KeyValuePair<long, LastHorizonSpecialPhaseEvent> pair in activeLastHorizonSpecialPhases)
                if (!requiredLastHorizonSpecialPhases.Contains(pair.Key))
                    lastHorizonSpecialPhaseKeyBuffer.Add(pair.Key);
            for (int i = 0; i < lastHorizonSpecialPhaseKeyBuffer.Count; i++)
            {
                long key = lastHorizonSpecialPhaseKeyBuffer[i];
                activeLastHorizonSpecialPhases[key].Deactivate();
                activeLastHorizonSpecialPhases.Remove(key);
            }
        }

        private LastHorizonSpecialPhaseEvent GetAvailableLastHorizonSpecialPhase()
        {
            for (int i = 0; i < lastHorizonSpecialPhasePool.Count; i++)
                if (lastHorizonSpecialPhasePool[i] != null &&
                    !lastHorizonSpecialPhasePool[i].gameObject.activeSelf)
                    return lastHorizonSpecialPhasePool[i];
            PrewarmLastHorizonSpecialPhases(lastHorizonSpecialPhasePool.Count + 1);
            return lastHorizonSpecialPhasePool[lastHorizonSpecialPhasePool.Count - 1];
        }

        private void PrewarmLastHorizonObstacles(int count)
        {
            if (mapProfile == null || !mapProfile.useLastHorizonTerrain) return;
            while (lastHorizonObstaclePool.Count < count)
            {
                GameObject obstacleObject = new GameObject(
                    "Last Horizon Obstacle - Available Pooled Instance");
                obstacleObject.transform.SetParent(transform, false);
                LastHorizonObstacleEvent obstacle = obstacleObject.AddComponent<LastHorizonObstacleEvent>();
                obstacleObject.SetActive(false);
                lastHorizonObstaclePool.Add(obstacle);
            }
        }

        private void RefreshLastHorizonObstacles()
        {
            if (mapProfile == null || !mapProfile.useLastHorizonTerrain || mapProfile.lastHorizon == null)
            {
                for (int i = 0; i < lastHorizonObstaclePool.Count; i++)
                    lastHorizonObstaclePool[i]?.Deactivate();
                activeLastHorizonObstacles.Clear();
                return;
            }

            float loadedStart = minimumLoadedIndex * chunkLength;
            float loadedEnd = (maximumLoadedIndex + 1) * chunkLength;
            float cellLength = Mathf.Max(180f, mapProfile.lastHorizon.obstacleCellLength);
            int firstCell = Mathf.Max(0, Mathf.FloorToInt((loadedStart - cellLength) / cellLength));
            int lastCell = Mathf.Max(0, Mathf.CeilToInt((loadedEnd + cellLength) / cellLength));
            requiredLastHorizonObstacles.Clear();
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!TerrainGenerator.TryGetLastHorizonObstacle(mapProfile, cell,
                        out LastHorizonObstacleLayout layout)) continue;
                if (layout.End < loadedStart || layout.Start > loadedEnd) continue;
                requiredLastHorizonObstacles.Add(cell);
                if (activeLastHorizonObstacles.ContainsKey(cell)) continue;
                LastHorizonObstacleEvent obstacle = GetAvailableLastHorizonObstacle();
                obstacle.Configure(mapProfile, layout);
                activeLastHorizonObstacles.Add(cell, obstacle);
            }

            lastHorizonObstacleKeyBuffer.Clear();
            foreach (KeyValuePair<int, LastHorizonObstacleEvent> pair in activeLastHorizonObstacles)
                if (!requiredLastHorizonObstacles.Contains(pair.Key))
                    lastHorizonObstacleKeyBuffer.Add(pair.Key);
            for (int i = 0; i < lastHorizonObstacleKeyBuffer.Count; i++)
            {
                int key = lastHorizonObstacleKeyBuffer[i];
                activeLastHorizonObstacles[key].Deactivate();
                activeLastHorizonObstacles.Remove(key);
            }
        }

        private LastHorizonObstacleEvent GetAvailableLastHorizonObstacle()
        {
            for (int i = 0; i < lastHorizonObstaclePool.Count; i++)
                if (lastHorizonObstaclePool[i] != null &&
                    !lastHorizonObstaclePool[i].gameObject.activeSelf)
                    return lastHorizonObstaclePool[i];
            PrewarmLastHorizonObstacles(lastHorizonObstaclePool.Count + 1);
            return lastHorizonObstaclePool[lastHorizonObstaclePool.Count - 1];
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
