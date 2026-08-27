using System.Collections.Generic;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    public sealed class CityBridgeZone : MonoBehaviour
    {
        // Visible alpha bounds of the supplied Water.png. Scaling the painted water rather than the full
        // transparent canvas keeps its surface and both horizontal ends flush with the event geometry.
        private const float WaterVisibleWidthFraction = .9591f;
        private const float WaterVisibleDepthFraction = .6212f;
        private const float WaterSurfaceFromPivotFraction = .1792f;
        private const float WaterEdgeUnderlap = .35f;

        private CityBridgeDeck entryBridge;
        private CityBridgeDeck exitBridge;
        private CityWaterBed waterBed;
        private CityWaterResistance waterResistance;
        private SpriteRenderer waterRenderer;
        private CityBridgeSettings settings;
        private float animationPhaseOffset;

        public int CycleIndex { get; private set; }
        public int ZoneIndex { get; private set; }
        public bool IsProgressive { get; private set; }
        public bool HasExitBridge => exitBridge != null && exitBridge.gameObject.activeSelf;
        public float WaterLength { get; private set; }

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (entryBridge == null)
                entryBridge = EnsureBridge("Entry Bridge");
            if (exitBridge == null)
                exitBridge = EnsureBridge("Exit Bridge");
            if (waterBed == null)
            {
                Transform child = transform.Find("Solid Water Bed");
                if (child == null)
                {
                    child = new GameObject("Solid Water Bed").transform;
                    child.SetParent(transform, false);
                }
                waterBed = child.GetComponent<CityWaterBed>();
                if (waterBed == null) waterBed = child.gameObject.AddComponent<CityWaterBed>();
            }
            if (waterResistance == null)
            {
                Transform child = transform.Find("City Water Resistance");
                if (child == null)
                {
                    child = new GameObject("City Water Resistance").transform;
                    child.SetParent(transform, false);
                }
                waterResistance = child.GetComponent<CityWaterResistance>();
                if (waterResistance == null)
                    waterResistance = child.gameObject.AddComponent<CityWaterResistance>();
            }
            if (waterRenderer == null)
            {
                Transform child = transform.Find("Water Visual");
                if (child == null)
                {
                    child = new GameObject("Water Visual").transform;
                    child.SetParent(transform, false);
                }
                waterRenderer = child.GetComponent<SpriteRenderer>();
                if (waterRenderer == null) waterRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        private CityBridgeDeck EnsureBridge(string bridgeName)
        {
            Transform child = transform.Find(bridgeName);
            if (child == null)
            {
                child = new GameObject(bridgeName).transform;
                child.SetParent(transform, false);
            }
            CityBridgeDeck deck = child.GetComponent<CityBridgeDeck>();
            if (deck == null) deck = child.gameObject.AddComponent<CityBridgeDeck>();
            return deck;
        }

        public void Configure(MapProfile profile, CityBridgeZoneLayout layout, float trackWidth)
        {
            EnsureComponents();
            settings = profile != null && profile.cityBridges != null
                ? profile.cityBridges : new CityBridgeSettings();
            CycleIndex = layout.cycleIndex;
            ZoneIndex = layout.zoneIndex;
            IsProgressive = layout.isProgressive;
            WaterLength = layout.waterLength;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            entryBridge.Configure(settings, new Vector2(layout.gapStart, layout.baselineHeight),
                layout.bridgeLength, trackWidth, true);
            entryBridge.gameObject.SetActive(true);

            bool needsExit = layout.HasExitBridge;
            exitBridge.gameObject.SetActive(needsExit);
            if (needsExit)
                exitBridge.Configure(settings, new Vector2(layout.gapEnd, layout.baselineHeight),
                    layout.bridgeLength, trackWidth, false);

            if (waterRenderer != null)
            {
                waterRenderer.sprite = settings.waterSprite;
                waterRenderer.drawMode = SpriteDrawMode.Simple;
                waterRenderer.sortingOrder = settings.waterSortingOrder;
                waterRenderer.flipX = false;
                float visualDepth = Mathf.Max(.5f, settings.waterVisualDepth);
                Vector2 sourceSize = settings.waterSprite != null
                    ? settings.waterSprite.bounds.size : Vector2.one;
                float horizontalScale = (layout.waterLength + WaterEdgeUnderlap * 2f) /
                                        Mathf.Max(.001f, sourceSize.x * WaterVisibleWidthFraction);
                float verticalScale = visualDepth /
                                      Mathf.Max(.001f, sourceSize.y * WaterVisibleDepthFraction);
                waterRenderer.transform.localScale = new Vector3(horizontalScale, verticalScale, 1f);
                float surfaceFromPivot = sourceSize.y * WaterSurfaceFromPivotFraction * verticalScale;
                waterRenderer.transform.position = new Vector3(
                    (layout.waterStart + layout.waterEnd) * .5f,
                    layout.baselineHeight + settings.waterSurfaceOffset - surfaceFromPivot,
                    -.10f);
                waterRenderer.transform.rotation = Quaternion.identity;
                Color waterColor = Color.white;
                waterColor.a = Mathf.Clamp01(settings.waterVisualAlpha);
                waterRenderer.color = waterColor;
                waterRenderer.enabled = settings.waterSprite != null;
            }

            float waterSurfaceY = layout.baselineHeight + settings.waterSurfaceOffset;
            float bedSurfaceY = waterSurfaceY - Mathf.Max(.5f, settings.waterVisualDepth);
            waterBed.Configure(profile, settings, layout.gapStart, layout.gapEnd,
                bedSurfaceY, layout.baselineHeight, trackWidth);
            waterResistance.Configure(settings, layout.waterStart, layout.waterEnd,
                waterSurfaceY, bedSurfaceY, trackWidth);

            float period = AnimationPeriod;
            animationPhaseOffset = period > .001f
                ? Mathf.Repeat(layout.cycleIndex * 2.173f + layout.zoneIndex * 1.317f, period) : 0f;
            ApplyAngle(EvaluateAnimationAngle(Time.fixedTime + animationPhaseOffset), true);
            gameObject.name = $"City {(layout.isProgressive ? "Progressive" : "Normal")} Bridge " +
                              $"C{CycleIndex} #{ZoneIndex + 1} - Pooled";
            gameObject.SetActive(true);
        }

        private float AnimationPeriod => settings == null ? 0f :
            settings.closedHoldSeconds + settings.openHoldSeconds + settings.transitionSeconds * 2f;

        private void FixedUpdate()
        {
            if (settings == null) return;
            ApplyAngle(EvaluateAnimationAngle(Time.fixedTime + animationPhaseOffset), false);
        }

        private float EvaluateAnimationAngle(float time)
        {
            float transition = Mathf.Max(.001f, settings.transitionSeconds);
            float closedHold = Mathf.Max(0f, settings.closedHoldSeconds);
            float openHold = Mathf.Max(0f, settings.openHoldSeconds);
            float period = closedHold + transition + openHold + transition;
            float phase = Mathf.Repeat(time, period);
            if (phase < closedHold) return settings.closedAngleDegrees;
            phase -= closedHold;
            if (phase < transition)
            {
                float t = Mathf.Clamp01(phase / transition);
                return Mathf.Lerp(settings.closedAngleDegrees, settings.openAngleDegrees, t);
            }
            phase -= transition;
            if (phase < openHold) return settings.openAngleDegrees;
            phase -= openHold;
            float closingT = Mathf.Clamp01(phase / transition);
            return Mathf.Lerp(settings.openAngleDegrees, settings.closedAngleDegrees, closingT);
        }

        private void ApplyAngle(float angle, bool immediate)
        {
            entryBridge?.SetLiftAngle(angle, immediate);
            if (HasExitBridge) exitBridge.SetLiftAngle(angle, immediate);
        }

        public void Deactivate()
        {
            settings = null;
            gameObject.SetActive(false);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CityBridgeDeck : MonoBehaviour
    {
        private Rigidbody2D body2D;
        private BoxCollider2D bridgeCollider2D;
        private BoxCollider collider3D;
        private SpriteRenderer bridgeRenderer;
        private bool extendsRight;

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (body2D == null) body2D = GetComponent<Rigidbody2D>();
            if (body2D == null) body2D = gameObject.AddComponent<Rigidbody2D>();
            body2D.bodyType = RigidbodyType2D.Kinematic;
            body2D.useFullKinematicContacts = true;
            body2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (bridgeCollider2D == null) bridgeCollider2D = GetComponent<BoxCollider2D>();
            if (bridgeCollider2D == null) bridgeCollider2D = gameObject.AddComponent<BoxCollider2D>();

            Transform collider3DTransform = transform.Find("Bridge Collider 3D");
            if (collider3DTransform == null)
            {
                collider3DTransform = new GameObject("Bridge Collider 3D").transform;
                collider3DTransform.SetParent(transform, false);
            }
            if (collider3D == null) collider3D = collider3DTransform.GetComponent<BoxCollider>();
            if (collider3D == null) collider3D = collider3DTransform.gameObject.AddComponent<BoxCollider>();

            Transform visual = transform.Find("Bridge Visual");
            if (visual == null)
            {
                visual = new GameObject("Bridge Visual").transform;
                visual.SetParent(transform, false);
            }
            if (bridgeRenderer == null) bridgeRenderer = visual.GetComponent<SpriteRenderer>();
            if (bridgeRenderer == null) bridgeRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
        }

        public void Configure(CityBridgeSettings settings, Vector2 pivot, float length, float trackWidth,
            bool pointsRight)
        {
            EnsureComponents();
            extendsRight = pointsRight;
            transform.position = new Vector3(pivot.x, pivot.y, 0f);
            transform.rotation = Quaternion.identity;

            float direction = extendsRight ? 1f : -1f;
            float thickness = Mathf.Max(.08f, settings.bridgeColliderThickness);
            // Terrain gaps are cut per generated sample, so a small hidden overlap absorbs the possible
            // half-sample seam without changing the visible bridge length or its terrain-side pivot.
            float overlap = Mathf.Clamp(settings.bridgeGroundOverlap, 0f, 2f);
            float colliderLength = length + overlap;
            Vector2 center = new Vector2(direction * (length - overlap) * .5f, -thickness * .5f);
            bridgeCollider2D.size = new Vector2(colliderLength, thickness);
            bridgeCollider2D.offset = center;
            bridgeCollider2D.isTrigger = false;

            collider3D.size = new Vector3(colliderLength, thickness, Mathf.Max(2f, trackWidth));
            collider3D.center = new Vector3(center.x, center.y, 0f);
            collider3D.isTrigger = false;

            bridgeRenderer.sprite = settings.bridgeSprite;
            bridgeRenderer.sortingOrder = settings.bridgeSortingOrder;
            bridgeRenderer.flipX = !extendsRight;
            bridgeRenderer.enabled = settings.bridgeSprite != null;
            float sourceWidth = settings.bridgeSprite != null
                ? Mathf.Max(.001f, settings.bridgeSprite.bounds.size.x) : 1f;
            float uniformScale = length / sourceWidth;
            bridgeRenderer.transform.localScale = Vector3.one * uniformScale;
            bridgeRenderer.transform.localPosition = new Vector3(direction * length * .5f,
                settings.bridgeArtSurfaceOffset, -.15f);
            bridgeRenderer.transform.localRotation = Quaternion.Euler(0f, 0f,
                direction * -settings.bridgeArtSlopeDegrees);
            SetLiftAngle(settings.closedAngleDegrees, true);
        }

        public void SetLiftAngle(float angle, bool immediate)
        {
            float signedAngle = extendsRight ? angle : -angle;
            if (body2D == null || immediate)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, signedAngle);
                if (body2D != null) body2D.rotation = signedAngle;
                return;
            }
            body2D.MoveRotation(signedAngle);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CityWaterBed : MonoBehaviour
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private BoxCollider2D bedCollider2D;
        private BoxCollider bedCollider3D;
        private Transform endBankTransform;
        private BoxCollider2D endBankCollider2D;
        private BoxCollider endBankCollider3D;
        private Mesh bedMesh;

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (bedCollider2D == null) bedCollider2D = GetComponent<BoxCollider2D>();
            if (bedCollider2D == null) bedCollider2D = gameObject.AddComponent<BoxCollider2D>();

            Transform collider3DTransform = transform.Find("Water Bed Collider 3D");
            if (collider3DTransform == null)
            {
                collider3DTransform = new GameObject("Water Bed Collider 3D").transform;
                collider3DTransform.SetParent(transform, false);
            }
            if (bedCollider3D == null) bedCollider3D = collider3DTransform.GetComponent<BoxCollider>();
            if (bedCollider3D == null)
                bedCollider3D = collider3DTransform.gameObject.AddComponent<BoxCollider>();

            if (endBankTransform == null) endBankTransform = transform.Find("Solid End Bank");
            if (endBankTransform == null)
            {
                endBankTransform = new GameObject("Solid End Bank").transform;
                endBankTransform.SetParent(transform, false);
            }
            if (endBankCollider2D == null)
                endBankCollider2D = endBankTransform.GetComponent<BoxCollider2D>();
            if (endBankCollider2D == null)
                endBankCollider2D = endBankTransform.gameObject.AddComponent<BoxCollider2D>();
            Transform endBank3DTransform = endBankTransform.Find("End Bank Collider 3D");
            if (endBank3DTransform == null)
            {
                endBank3DTransform = new GameObject("End Bank Collider 3D").transform;
                endBank3DTransform.SetParent(endBankTransform, false);
            }
            if (endBankCollider3D == null)
                endBankCollider3D = endBank3DTransform.GetComponent<BoxCollider>();
            if (endBankCollider3D == null)
                endBankCollider3D = endBank3DTransform.gameObject.AddComponent<BoxCollider>();

            if (bedMesh == null)
            {
                bedMesh = new Mesh { name = "Pooled City Water Bed Mesh" };
                bedMesh.MarkDynamic();
                meshFilter.sharedMesh = bedMesh;
            }
        }

        public void Configure(MapProfile profile, CityBridgeSettings settings, float floorStart,
            float floorEnd, float floorSurfaceY, float terrainSurfaceY, float trackWidth)
        {
            EnsureComponents();
            float width = Mathf.Max(.5f, floorEnd - floorStart);
            float thickness = Mathf.Max(.5f, settings.waterBedThickness);
            if (profile != null) thickness = Mathf.Max(thickness, profile.terrainVisualDepth);
            float centerX = (floorStart + floorEnd) * .5f;
            transform.SetPositionAndRotation(
                new Vector3(centerX, floorSurfaceY - thickness * .5f, -.01f), Quaternion.identity);

            bedCollider2D.size = new Vector2(width, thickness);
            bedCollider2D.offset = Vector2.zero;
            bedCollider2D.isTrigger = false;
            bedCollider2D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial2D : null;

            bedCollider3D.size = new Vector3(width, thickness, Mathf.Max(2f, trackWidth));
            bedCollider3D.center = Vector3.zero;
            bedCollider3D.isTrigger = false;
            bedCollider3D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial : null;

            float bankWidth = Mathf.Min(width, Mathf.Max(1f, settings.waterEndBankWidth));
            float floorBottomY = floorSurfaceY - thickness;
            float bankHeight = Mathf.Max(.5f, terrainSurfaceY - floorBottomY);
            float bankCenterX = floorEnd + bankWidth * .5f;
            float bankCenterY = floorBottomY + bankHeight * .5f;
            endBankTransform.localPosition = new Vector3(bankCenterX - transform.position.x,
                bankCenterY - transform.position.y, 0f);
            endBankTransform.localRotation = Quaternion.identity;
            endBankCollider2D.size = new Vector2(bankWidth, bankHeight);
            endBankCollider2D.offset = Vector2.zero;
            endBankCollider2D.isTrigger = false;
            endBankCollider2D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial2D : null;
            endBankCollider3D.size = new Vector3(bankWidth, bankHeight, Mathf.Max(2f, trackWidth));
            endBankCollider3D.center = Vector3.zero;
            endBankCollider3D.isTrigger = false;
            endBankCollider3D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial : null;

            float halfWidth = width * .5f;
            float halfHeight = thickness * .5f;
            float repeatWidth = profile != null ? Mathf.Max(1f, profile.terrainTextureRepeatWidth) : 40f;
            float u0 = floorStart / repeatWidth;
            float u1 = floorEnd / repeatWidth;
            float bankU0 = floorEnd / repeatWidth;
            float bankU1 = (floorEnd + bankWidth) / repeatWidth;
            float bottomV = profile != null ? profile.terrainWallUvRange.x : 0f;
            float topV = profile != null ? profile.terrainWallUvRange.y : 1f;
            float surfaceBottomV = profile != null ? profile.terrainSurfaceUvRange.x : 0f;
            float surfaceTopV = profile != null ? profile.terrainSurfaceUvRange.y : 1f;
            TerrainGenerator.CalculateGroundVisualExtents(profile,
                out float groundDepth, out float groundRise);
            float floorTop = halfHeight;
            float bankLeft = floorEnd - centerX;
            float bankRight = floorEnd + bankWidth - centerX;
            float bankTop = terrainSurfaceY - transform.position.y;
            float bankBottom = floorBottomY - transform.position.y;
            bedMesh.Clear();
            bedMesh.vertices = new[]
            {
                // Terrain below the water.
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f),
                // Ground artwork whose yellow stripe is the physical floor surface.
                new Vector3(-halfWidth, floorTop + groundRise, -.03f),
                new Vector3(halfWidth, floorTop + groundRise, -.03f),
                new Vector3(-halfWidth, floorTop - groundDepth, -.03f),
                new Vector3(halfWidth, floorTop - groundDepth, -.03f),
                // Solid terrain bank at the end of the water section.
                new Vector3(bankLeft, bankTop, 0f),
                new Vector3(bankRight, bankTop, 0f),
                new Vector3(bankLeft, bankBottom, 0f),
                new Vector3(bankRight, bankBottom, 0f),
                // Matching Ground cap on the end bank.
                new Vector3(bankLeft, bankTop + groundRise, -.03f),
                new Vector3(bankRight, bankTop + groundRise, -.03f),
                new Vector3(bankLeft, bankTop - groundDepth, -.03f),
                new Vector3(bankRight, bankTop - groundDepth, -.03f)
            };
            bedMesh.uv = new[]
            {
                new Vector2(u0, topV), new Vector2(u1, topV),
                new Vector2(u0, bottomV), new Vector2(u1, bottomV),
                new Vector2(u0, surfaceTopV), new Vector2(u1, surfaceTopV),
                new Vector2(u0, surfaceBottomV), new Vector2(u1, surfaceBottomV),
                new Vector2(bankU0, topV), new Vector2(bankU1, topV),
                new Vector2(bankU0, bottomV), new Vector2(bankU1, bottomV),
                new Vector2(bankU0, surfaceTopV), new Vector2(bankU1, surfaceTopV),
                new Vector2(bankU0, surfaceBottomV), new Vector2(bankU1, surfaceBottomV)
            };
            bedMesh.subMeshCount = 2;
            bedMesh.SetTriangles(new[] { 0, 1, 2, 1, 3, 2, 8, 9, 10, 9, 11, 10 }, 0);
            bedMesh.SetTriangles(new[] { 4, 5, 6, 5, 7, 6, 12, 13, 14, 13, 15, 14 }, 1);
            bedMesh.RecalculateNormals();
            bedMesh.RecalculateBounds();

            meshRenderer.sharedMaterials = profile != null
                ? new[] { profile.terrainMaterial, profile.terrainEdgeMaterial }
                : new Material[] { null, null };
            meshRenderer.sortingOrder = settings.waterBedSortingOrder;
            meshRenderer.enabled = profile != null && profile.terrainMaterial != null;
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (bedMesh == null) return;
            if (Application.isPlaying) Destroy(bedMesh);
            else DestroyImmediate(bedMesh);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CityWaterResistance : MonoBehaviour
    {
        private readonly HashSet<Collider> contacts3D = new HashSet<Collider>();
        private readonly HashSet<Collider2D> contacts2D = new HashSet<Collider2D>();
        private readonly HashSet<VehicleRuntimeBase> driverDownVehicles = new HashSet<VehicleRuntimeBase>();
        private readonly List<VehicleRuntimeBase> vehicleBuffer = new List<VehicleRuntimeBase>(2);
        private BoxCollider volume3D;
        private BoxCollider2D volume2D;
        private CityWaterResistance2DForwarder forwarder2D;
        private CityBridgeSettings settings;
        private float floorY;
        private float waterSurfaceY;

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (volume3D == null) volume3D = GetComponent<BoxCollider>();
            if (volume3D == null) volume3D = gameObject.AddComponent<BoxCollider>();
            volume3D.isTrigger = true;

            Transform child2D = transform.Find("Water Resistance Trigger 2D");
            if (child2D == null)
            {
                child2D = new GameObject("Water Resistance Trigger 2D").transform;
                child2D.SetParent(transform, false);
            }
            if (volume2D == null) volume2D = child2D.GetComponent<BoxCollider2D>();
            if (volume2D == null) volume2D = child2D.gameObject.AddComponent<BoxCollider2D>();
            volume2D.isTrigger = true;
            if (forwarder2D == null) forwarder2D = child2D.GetComponent<CityWaterResistance2DForwarder>();
            if (forwarder2D == null)
                forwarder2D = child2D.gameObject.AddComponent<CityWaterResistance2DForwarder>();
            forwarder2D.Assign(this);
        }

        public void Configure(CityBridgeSettings bridgeSettings, float waterStart, float waterEnd,
            float surfaceY, float bottomY, float trackWidth)
        {
            EnsureComponents();
            ClearContacts();
            settings = bridgeSettings;
            floorY = bottomY;
            waterSurfaceY = surfaceY;
            float width = Mathf.Max(.5f, waterEnd - waterStart);
            float depth = Mathf.Max(.5f, surfaceY - bottomY);
            transform.SetPositionAndRotation(new Vector3((waterStart + waterEnd) * .5f,
                surfaceY - depth * .5f, 0f), Quaternion.identity);
            volume3D.center = Vector3.zero;
            volume3D.size = new Vector3(width, depth, Mathf.Max(2f, trackWidth));
            volume2D.offset = Vector2.zero;
            volume2D.size = new Vector2(width, depth);
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other) => contacts3D.Add(other);
        private void OnTriggerStay(Collider other) => contacts3D.Add(other);
        private void OnTriggerExit(Collider other) => contacts3D.Remove(other);

        internal void AddContact2D(Collider2D other) => contacts2D.Add(other);
        internal void RemoveContact2D(Collider2D other) => contacts2D.Remove(other);

        private void FixedUpdate()
        {
            if (settings == null || (contacts3D.Count == 0 && contacts2D.Count == 0)) return;
            vehicleBuffer.Clear();
            foreach (Collider2D contact in contacts2D)
            {
                VehicleRuntimeBase vehicle = ResolveVehicle(contact);
                if (vehicle != null && !vehicleBuffer.Contains(vehicle)) vehicleBuffer.Add(vehicle);
            }
            foreach (Collider contact in contacts3D)
            {
                VehicleRuntimeBase vehicle = ResolveVehicle(contact);
                if (vehicle != null && !vehicleBuffer.Contains(vehicle)) vehicleBuffer.Add(vehicle);
            }

            float linearFactor = Mathf.Pow(Mathf.Clamp(settings.waterSpeedRetainedPerSecond, .05f, 1f),
                Time.fixedDeltaTime);
            float angularFactor = Mathf.Pow(
                Mathf.Clamp(settings.waterAngularSpeedRetainedPerSecond, .05f, 1f), Time.fixedDeltaTime);
            for (int i = 0; i < vehicleBuffer.Count; i++)
            {
                VehicleRuntimeBase vehicle = vehicleBuffer[i];
                if (vehicle.Body2D != null)
                {
                    Rigidbody2D body = vehicle.Body2D;
                    body.linearVelocity *= linearFactor;
                    body.angularVelocity *= angularFactor;
                    float buoyancy = EvaluateWaterBuoyancy(Physics2D.gravity.magnitude,
                        floorY + .75f - body.position.y, body.linearVelocity.y,
                        settings.waterBuoyancySpring, settings.waterVerticalDamping,
                        settings.waterMaximumSinkAcceleration);
                    body.AddForce(Vector2.up * (body.mass * buoyancy), ForceMode2D.Force);
                }
                else if (vehicle.Body != null)
                {
                    Rigidbody body = vehicle.Body;
                    body.linearVelocity *= linearFactor;
                    body.angularVelocity *= angularFactor;
                    float buoyancy = EvaluateWaterBuoyancy(Physics.gravity.magnitude,
                        floorY + .75f - body.position.y, body.linearVelocity.y,
                        settings.waterBuoyancySpring, settings.waterVerticalDamping,
                        settings.waterMaximumSinkAcceleration);
                    body.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
                }

                if (!driverDownVehicles.Contains(vehicle) &&
                    HasFatalWheelSubmersion(vehicle, waterSurfaceY, settings.waterDriverDownClearance))
                    TriggerDriverDown(vehicle);
            }
        }

        public static float EvaluateWaterBuoyancy(float gravityMagnitude, float targetHeightDelta,
            float verticalVelocity, float spring, float damping, float maximumSinkAcceleration)
        {
            float correction = targetHeightDelta * Mathf.Max(.1f, spring) -
                               verticalVelocity * Mathf.Max(.1f, damping);
            return Mathf.Max(0f, gravityMagnitude + Mathf.Clamp(correction,
                -Mathf.Max(.1f, maximumSinkAcceleration), 8f));
        }

        public static bool HasFatalWheelSubmersion(VehicleRuntimeBase vehicle, float surfaceY,
            float requiredDepth)
        {
            float threshold = Mathf.Max(.1f, requiredDepth);
            if (vehicle is VehiclePhysics2DController vehicle2D)
                return WheelSubmersion(vehicle2D.FrontSuspension, surfaceY) >= threshold ||
                       WheelSubmersion(vehicle2D.RearSuspension, surfaceY) >= threshold;
            if (vehicle is VehicleController vehicle3D)
                return WheelSubmersion(vehicle3D.FrontSuspension, surfaceY) >= threshold ||
                       WheelSubmersion(vehicle3D.RearSuspension, surfaceY) >= threshold;
            return false;
        }

        private static float WheelSubmersion(Suspension2DController suspension, float surfaceY)
        {
            return suspension != null && suspension.WheelBody != null
                ? surfaceY - (suspension.WheelBody.position.y - suspension.WheelRadius)
                : float.NegativeInfinity;
        }

        private static float WheelSubmersion(SuspensionController suspension, float surfaceY)
        {
            return suspension != null && suspension.WheelBody != null
                ? surfaceY - (suspension.WheelBody.position.y - suspension.PhysicalWheelRadius)
                : float.NegativeInfinity;
        }

        private void TriggerDriverDown(VehicleRuntimeBase vehicle)
        {
            CrashDetector crashDetector = vehicle != null ? vehicle.GetComponent<CrashDetector>() : null;
            if (crashDetector == null || !crashDetector.NotifyDriverDown()) return;
            driverDownVehicles.Add(vehicle);
            Debug.Log($"[CityWater] Driver Down: a wheel descended " +
                      $"{settings.waterDriverDownClearance:0.0} m below the water surface.", vehicle);
        }

        private static VehicleRuntimeBase ResolveVehicle(Collider2D other)
        {
            VehicleRuntimeBase vehicle = other != null
                ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            return vehicle != null && vehicle.Body2D != null && other.attachedRigidbody == vehicle.Body2D
                ? vehicle : null;
        }

        private static VehicleRuntimeBase ResolveVehicle(Collider other)
        {
            VehicleRuntimeBase vehicle = other != null
                ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            return vehicle != null && vehicle.Body != null && other.attachedRigidbody == vehicle.Body
                ? vehicle : null;
        }

        private void OnDisable() => ClearContacts();

        private void ClearContacts()
        {
            contacts3D.Clear();
            contacts2D.Clear();
            driverDownVehicles.Clear();
            vehicleBuffer.Clear();
        }
    }

    public sealed class CityWaterResistance2DForwarder : MonoBehaviour
    {
        private CityWaterResistance owner;

        public void Assign(CityWaterResistance resistance) => owner = resistance;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<CityWaterResistance>();
        }

        private void OnTriggerEnter2D(Collider2D other) => owner?.AddContact2D(other);
        private void OnTriggerStay2D(Collider2D other) => owner?.AddContact2D(other);
        private void OnTriggerExit2D(Collider2D other) => owner?.RemoveContact2D(other);
    }
}
