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
                    .05f);
                waterRenderer.transform.rotation = Quaternion.identity;
                waterRenderer.enabled = settings.waterSprite != null;
            }

            float waterSurfaceY = layout.baselineHeight + settings.waterSurfaceOffset;
            float bedSurfaceY = waterSurfaceY - Mathf.Max(.5f, settings.waterVisualDepth);
            waterBed.Configure(profile, settings, layout.waterStart, layout.waterEnd,
                bedSurfaceY, trackWidth);

            float period = AnimationPeriod;
            animationPhaseOffset = period > .001f
                ? Mathf.Repeat(layout.cycleIndex * 2.173f + layout.zoneIndex * 1.317f, period) : 0f;
            ApplyAngle(EvaluateAnimationAngle(Time.time + animationPhaseOffset), true);
            gameObject.name = $"City {(layout.isProgressive ? "Progressive" : "Normal")} Bridge " +
                              $"C{CycleIndex} #{ZoneIndex + 1} - Pooled";
            gameObject.SetActive(true);
        }

        private float AnimationPeriod => settings == null ? 0f :
            settings.closedHoldSeconds + settings.openHoldSeconds + settings.transitionSeconds * 2f;

        private void FixedUpdate()
        {
            if (settings == null) return;
            ApplyAngle(EvaluateAnimationAngle(Time.time + animationPhaseOffset), false);
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
                float t = Mathf.SmoothStep(0f, 1f, phase / transition);
                return Mathf.Lerp(settings.closedAngleDegrees, settings.openAngleDegrees, t);
            }
            phase -= transition;
            if (phase < openHold) return settings.openAngleDegrees;
            phase -= openHold;
            float closingT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase / transition));
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

            if (bedMesh == null)
            {
                bedMesh = new Mesh { name = "Pooled City Water Bed Mesh" };
                bedMesh.MarkDynamic();
                meshFilter.sharedMesh = bedMesh;
            }
        }

        public void Configure(MapProfile profile, CityBridgeSettings settings, float waterStart,
            float waterEnd, float surfaceY, float trackWidth)
        {
            EnsureComponents();
            float width = Mathf.Max(.5f, waterEnd - waterStart);
            float thickness = Mathf.Max(.5f, settings.waterBedThickness);
            float centerX = (waterStart + waterEnd) * .5f;
            transform.SetPositionAndRotation(
                new Vector3(centerX, surfaceY - thickness * .5f, .1f), Quaternion.identity);

            bedCollider2D.size = new Vector2(width, thickness);
            bedCollider2D.offset = Vector2.zero;
            bedCollider2D.isTrigger = false;
            bedCollider2D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial2D : null;

            bedCollider3D.size = new Vector3(width, thickness, Mathf.Max(2f, trackWidth));
            bedCollider3D.center = Vector3.zero;
            bedCollider3D.isTrigger = false;
            bedCollider3D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial : null;

            float halfWidth = width * .5f;
            float halfHeight = thickness * .5f;
            float repeatWidth = profile != null ? Mathf.Max(1f, profile.terrainTextureRepeatWidth) : 40f;
            float u0 = waterStart / repeatWidth;
            float u1 = waterEnd / repeatWidth;
            float bottomV = profile != null ? profile.terrainWallUvRange.x : 0f;
            float topV = profile != null ? profile.terrainWallUvRange.y : 1f;
            bedMesh.Clear();
            bedMesh.vertices = new[]
            {
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f)
            };
            bedMesh.uv = new[]
            {
                new Vector2(u0, topV), new Vector2(u1, topV),
                new Vector2(u0, bottomV), new Vector2(u1, bottomV)
            };
            bedMesh.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            bedMesh.RecalculateNormals();
            bedMesh.RecalculateBounds();

            meshRenderer.sharedMaterial = profile != null ? profile.terrainMaterial : null;
            meshRenderer.sortingOrder = settings.waterBedSortingOrder;
            meshRenderer.enabled = meshRenderer.sharedMaterial != null;
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (bedMesh == null) return;
            if (Application.isPlaying) Destroy(bedMesh);
            else DestroyImmediate(bedMesh);
        }
    }
}
