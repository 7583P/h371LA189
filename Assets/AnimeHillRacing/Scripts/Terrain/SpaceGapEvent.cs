using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>A pooled Space event: integrated ramp plus either a void or a physical driveable planet.</summary>
    public sealed class SpaceGapEvent : MonoBehaviour
    {
        private MeshFilter rampFilter;
        private MeshRenderer rampRenderer;
        private MeshCollider rampCollider;
        private EdgeCollider2D rampCollider2D;
        private SpriteRenderer warningRenderer;
        private SpriteRenderer outerRenderer;
        private SpriteRenderer innerRenderer;
        private BoxCollider hazard3D;
        private BoxCollider2D hazard2D;
        private SpriteRenderer planetRenderer;
        private SphereCollider planetCollider3D;
        private EdgeCollider2D planetCollider2D;
        private Mesh rampMesh;
        private GameManager gameManager;
        private static PhysicsMaterial2D fallbackPlanetMaterial2D;
        private bool triggered;

        public int Phase { get; private set; }
        public float GapStart { get; private set; }
        public float GapEnd { get; private set; }

        private void Awake() => EnsureComponents();

        private Transform GetOrCreate(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null) return child;
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
            return child;
        }

        private void EnsureComponents()
        {
            Transform ramp = GetOrCreate("Integrated Lunar Launch Ramp");
            // Unity objects can survive a domain reload as "fake null" references. Explicit Unity null
            // checks rebuild every missing component; C# ??/??= would keep the destroyed wrapper instead.
            if (rampFilter == null) rampFilter = ramp.GetComponent<MeshFilter>();
            if (rampFilter == null) rampFilter = ramp.gameObject.AddComponent<MeshFilter>();
            if (rampRenderer == null) rampRenderer = ramp.GetComponent<MeshRenderer>();
            if (rampRenderer == null) rampRenderer = ramp.gameObject.AddComponent<MeshRenderer>();
            if (rampCollider == null) rampCollider = ramp.GetComponent<MeshCollider>();
            if (rampCollider == null) rampCollider = ramp.gameObject.AddComponent<MeshCollider>();
            if (rampMesh == null)
            {
                rampMesh = new Mesh { name = "Reusable Space Launch Ramp" };
                rampMesh.MarkDynamic();
            }
            rampFilter.sharedMesh = rampMesh;

            Transform ramp2D = GetOrCreate("Launch Ramp Collider 2D");
            if (rampCollider2D == null) rampCollider2D = ramp2D.GetComponent<EdgeCollider2D>();
            if (rampCollider2D == null) rampCollider2D = ramp2D.gameObject.AddComponent<EdgeCollider2D>();

            Transform warning = GetOrCreate("Warning Visual - No Collider");
            if (warningRenderer == null) warningRenderer = warning.GetComponent<SpriteRenderer>();
            if (warningRenderer == null) warningRenderer = warning.gameObject.AddComponent<SpriteRenderer>();

            Transform outer = GetOrCreate("Black Hole Static Horizontal Core");
            if (outerRenderer == null) outerRenderer = outer.GetComponent<SpriteRenderer>();
            if (outerRenderer == null) outerRenderer = outer.gameObject.AddComponent<SpriteRenderer>();
            Transform inner = GetOrCreate("Black Hole Animated Orange Fire Ring");
            if (innerRenderer == null) innerRenderer = inner.GetComponent<SpriteRenderer>();
            if (innerRenderer == null) innerRenderer = inner.gameObject.AddComponent<SpriteRenderer>();

            SphereCollider obsoleteSphere = GetComponent<SphereCollider>();
            if (obsoleteSphere != null) obsoleteSphere.enabled = false;
            if (hazard3D == null) hazard3D = GetComponent<BoxCollider>();
            if (hazard3D == null) hazard3D = gameObject.AddComponent<BoxCollider>();
            hazard3D.isTrigger = true;
            Transform trigger2D = GetOrCreate("Black Hole Hazard Trigger 2D");
            CircleCollider2D obsoleteHazardCircle = trigger2D.GetComponent<CircleCollider2D>();
            if (obsoleteHazardCircle != null) obsoleteHazardCircle.enabled = false;
            if (hazard2D == null) hazard2D = trigger2D.GetComponent<BoxCollider2D>();
            if (hazard2D == null) hazard2D = trigger2D.gameObject.AddComponent<BoxCollider2D>();
            hazard2D.isTrigger = true;
            SpaceGapEvent2DForwarder forwarder = trigger2D.GetComponent<SpaceGapEvent2DForwarder>();
            if (forwarder == null) forwarder = trigger2D.gameObject.AddComponent<SpaceGapEvent2DForwarder>();
            forwarder.AssignParent(this);

            Transform planetVisual = GetOrCreate("Planet Visual - No Ring Collider");
            if (planetRenderer == null) planetRenderer = planetVisual.GetComponent<SpriteRenderer>();
            if (planetRenderer == null) planetRenderer = planetVisual.gameObject.AddComponent<SpriteRenderer>();
            Transform planetPhysics = GetOrCreate("Physical Circular Planet Surface");
            if (planetCollider3D == null) planetCollider3D = planetPhysics.GetComponent<SphereCollider>();
            if (planetCollider3D == null) planetCollider3D = planetPhysics.gameObject.AddComponent<SphereCollider>();
            CircleCollider2D obsoleteCircle = planetPhysics.GetComponent<CircleCollider2D>();
            if (obsoleteCircle != null) obsoleteCircle.enabled = false;
            // Collider2D types cannot coexist reliably on the legacy planet object. Keep the
            // driveable 2D arc on its own child so AddComponent cannot return null.
            Transform planetPhysics2D = GetOrCreate("Physical Circular Planet Surface 2D");
            if (planetCollider2D == null) planetCollider2D = planetPhysics2D.GetComponent<EdgeCollider2D>();
            if (planetCollider2D == null)
                planetCollider2D = planetPhysics2D.gameObject.AddComponent<EdgeCollider2D>();
            planetCollider3D.isTrigger = false;
            planetCollider2D.isTrigger = false;
        }

        public void Configure(MapProfile profile, SpaceChallengeLayout layout, float trackWidth,
            GameManager manager)
        {
            EnsureComponents();
            SpaceChallengeSettings settings = profile != null && profile.spaceChallenges != null
                ? profile.spaceChallenges : new SpaceChallengeSettings();
            Phase = layout.phase;
            GapStart = layout.gapStart;
            GapEnd = layout.gapEnd;
            gameManager = manager;
            triggered = false;
            transform.position = new Vector3(layout.rampStart, layout.baselineHeight, 0f);

            BuildRamp(settings, layout, trackWidth);
            rampFilter.transform.localPosition = new Vector3(0f, .025f, -.06f);
            rampRenderer.sharedMaterial = settings.rampMaterial != null
                ? settings.rampMaterial : profile != null ? profile.terrainEdgeMaterial : null;
            // Physics follows the same integrated terrain ramp used by the track. The lunar mesh remains
            // as a visual coating, without overlapping contacts or a seam at the takeoff edge.
            rampCollider.enabled = false;
            rampCollider2D.enabled = false;

            warningRenderer.sprite = settings.warningSprite;
            warningRenderer.sortingOrder = 7;
            warningRenderer.transform.localPosition = new Vector3(layout.warningX - layout.rampStart,
                TerrainGenerator.SampleHeight(profile, layout.warningX) - layout.baselineHeight + 1.65f,
                -trackWidth * .5f - .12f);
            float warningSourceWidth = warningRenderer.sprite != null ? warningRenderer.sprite.bounds.size.x : 1f;
            warningRenderer.transform.localScale = Vector3.one *
                                                   (settings.warningWidth / Mathf.Max(.001f, warningSourceWidth));

            if (layout.IsPlanet)
            {
                ConfigurePlanet(profile, layout, trackWidth);
                outerRenderer.enabled = false;
                innerRenderer.enabled = false;
                hazard3D.enabled = false;
                hazard2D.enabled = false;
                gameObject.name = $"Space Planet {layout.planetType} Phase {layout.phase} - Pooled";
                gameObject.SetActive(true);
                return;
            }

            planetRenderer.enabled = false;
            planetCollider3D.enabled = false;
            planetCollider2D.enabled = false;
            outerRenderer.enabled = true;
            innerRenderer.enabled = false;
            hazard3D.enabled = true;
            hazard2D.enabled = true;
            float blackHoleX = (layout.gapStart + layout.gapEnd) * .5f - layout.rampStart;
            float blackHoleWidth = Mathf.Max(1f, layout.gapLength);
            float blackHoleHeight = CalculateBlackHoleVisualHeight(blackHoleWidth);
            float blackHoleY = CalculateBlackHoleCenterY(settings.verticalOffset, blackHoleWidth);
            ConfigureBlackHoleRenderer(outerRenderer, settings.blackHoleSprite,
                blackHoleWidth, blackHoleHeight,
                blackHoleX, blackHoleY, -trackWidth * .5f - .18f, 5);
            outerRenderer.color = Color.white;

            // Only the lower half is lethal. Measure the trigger from the sprite's actual
            // vertical midpoint so its top edge stays at half of the PNG even if its pivot
            // or imported bounds change.
            // Extend far below the artwork so a fast-falling wheel cannot tunnel through the
            // trigger; its upper edge remains exactly on the requested midpoint.
            float pngMidpointY = CalculateSpriteVerticalMidpoint(outerRenderer, blackHoleY);
            float lethalHeight = Mathf.Max(blackHoleHeight * .5f, 200f);
            float lethalCenterY = pngMidpointY - lethalHeight * .5f;
            hazard3D.center = new Vector3(blackHoleX, lethalCenterY, 0f);
            hazard3D.size = new Vector3(blackHoleWidth, lethalHeight,
                Mathf.Max(2f, trackWidth + 1f));
            hazard2D.offset = new Vector2(blackHoleX, lethalCenterY);
            hazard2D.size = new Vector2(blackHoleWidth, lethalHeight);

            gameObject.name = $"Space Void Phase {layout.phase} - Pooled";
            gameObject.SetActive(true);
        }

        private void ConfigurePlanet(MapProfile profile, SpaceChallengeLayout layout, float trackWidth)
        {
            float localX = layout.planetCenterX - layout.rampStart;
            float localY = layout.planetCenterY - layout.baselineHeight;
            planetRenderer.sprite = SpacePlanetSpriteFactory.GetSprite(layout.planetType);
            planetRenderer.color = Color.white;
            planetRenderer.sortingOrder = 6;
            planetRenderer.enabled = true;
            planetRenderer.transform.localPosition = new Vector3(localX, localY, -trackWidth * .5f - .18f);
            planetRenderer.transform.localRotation = Quaternion.identity;
            planetRenderer.transform.localScale = Vector3.one * layout.planetRadius;

            Transform physics2D = planetCollider2D.transform;
            physics2D.localPosition = new Vector3(localX, localY, 0f);
            physics2D.localRotation = Quaternion.identity;
            physics2D.localScale = Vector3.one;
            Transform physics3D = planetCollider3D.transform;
            physics3D.localPosition = new Vector3(localX, localY, 0f);
            physics3D.localRotation = Quaternion.identity;
            physics3D.localScale = Vector3.one;
            planetCollider3D.radius = layout.planetRadius;
            const int arcSegments = 28;
            var arc = new Vector2[arcSegments + 1];
            float endpointHeight = profile != null && profile.spaceChallenges != null
                ? Mathf.Clamp(profile.spaceChallenges.planetCenterDrop, .6f, .88f) : .82f;
            float rightAngle = Mathf.Asin(endpointHeight);
            float leftAngle = Mathf.PI - rightAngle;
            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = Mathf.Lerp(leftAngle, rightAngle, i / (float)arcSegments);
                arc[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * layout.planetRadius;
            }
            planetCollider2D.points = arc;
            planetCollider2D.edgeRadius = .055f;
            planetCollider3D.sharedMaterial = profile != null ? profile.groundPhysicsMaterial : null;
            if (profile != null && profile.groundPhysicsMaterial2D != null)
            {
                planetCollider2D.sharedMaterial = profile.groundPhysicsMaterial2D;
            }
            else
            {
                if (fallbackPlanetMaterial2D == null)
                {
                    fallbackPlanetMaterial2D = new PhysicsMaterial2D("Runtime Lunar Planet Surface")
                    {
                        friction = .82f,
                        bounciness = .015f,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                planetCollider2D.sharedMaterial = fallbackPlanetMaterial2D;
            }
            planetCollider3D.enabled = true;
            planetCollider2D.enabled = true;
        }

        public static float CalculateBlackHoleVisualHeight(float width) => 9f;

        public static float CalculateBlackHoleCenterY(float authoredCenterY, float width)
        {
            const float clearanceBelowRamp = .2f;
            return Mathf.Min(authoredCenterY,
                -CalculateBlackHoleVisualHeight(width) * .5f - clearanceBelowRamp);
        }

        private static void ConfigureBlackHoleRenderer(SpriteRenderer renderer, Sprite sprite,
            float width, float height, float x, float y, float z, int sortingOrder)
        {
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localPosition = new Vector3(x, y, z);
            renderer.transform.localRotation = Quaternion.identity;
            float sourceWidth = sprite != null ? sprite.bounds.size.x : 1f;
            float sourceHeight = sprite != null ? sprite.bounds.size.y : 1f;
            renderer.transform.localScale = new Vector3(
                width / Mathf.Max(.001f, sourceWidth),
                height / Mathf.Max(.001f, sourceHeight), 1f);
        }

        private static float CalculateSpriteVerticalMidpoint(SpriteRenderer renderer, float fallbackY)
        {
            if (renderer == null || renderer.sprite == null) return fallbackY;
            return renderer.transform.localPosition.y +
                   renderer.sprite.bounds.center.y * renderer.transform.localScale.y;
        }

        private void BuildRamp(SpaceChallengeSettings settings, SpaceChallengeLayout layout, float trackWidth)
        {
            float length = Mathf.Max(4f, layout.rampLength);
            float flatEnd = length * Mathf.Clamp(settings.rampFlatFraction, .05f, .5f);
            float rise = Mathf.Max(.2f, layout.rampRise);
            float halfWidth = Mathf.Max(1f, trackWidth * .5f);
            float thickness = .62f;
            const int segments = 12;
            var vertices = new Vector3[(segments + 1) * 3];
            var triangles = new int[segments * 12];
            var edge = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float x = length * i / segments;
                float t = x <= flatEnd ? 0f : Mathf.InverseLerp(flatEnd, length, x);
                float launchCurve = t * t * (2f - t);
                float y = rise * launchCurve;
                int vertex = i * 3;
                vertices[vertex] = new Vector3(x, y, -halfWidth);
                vertices[vertex + 1] = new Vector3(x, y, halfWidth);
                vertices[vertex + 2] = new Vector3(x, y - thickness, -halfWidth);
                edge[i] = new Vector2(x, y);
                if (i == segments) continue;
                int triangle = i * 12;
                int next = vertex + 3;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = next;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = next + 1;
                triangles[triangle + 5] = next;
                triangles[triangle + 6] = vertex;
                triangles[triangle + 7] = next;
                triangles[triangle + 8] = vertex + 2;
                triangles[triangle + 9] = next;
                triangles[triangle + 10] = next + 2;
                triangles[triangle + 11] = vertex + 2;
            }
            rampMesh.Clear();
            rampMesh.vertices = vertices;
            rampMesh.triangles = triangles;
            rampMesh.RecalculateNormals();
            rampMesh.RecalculateBounds();
            rampCollider.sharedMesh = null;
            rampCollider.sharedMesh = rampMesh;
            rampCollider2D.points = edge;
        }

        private void OnTriggerEnter(Collider other)
        {
            VehicleRuntimeBase candidate = other != null ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            if (candidate != null && candidate.Body != null && other.attachedRigidbody == candidate.Body)
                TriggerDriverDown();
        }

        private void OnTriggerStay(Collider other) => OnTriggerEnter(other);

        internal void Trigger2D(Collider2D other)
        {
            if (other == null) return;
            Suspension2DController wheel = other.GetComponent<Suspension2DController>() ??
                                           other.GetComponentInParent<Suspension2DController>();
            if (wheel == null || wheel.WheelCollider != other ||
                other.attachedRigidbody != wheel.WheelBody || wheel.OwnerVehicle == null) return;
            TriggerDriverDown();
        }

        private void TriggerDriverDown()
        {
            if (triggered) return;
            triggered = true;
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.RequestGameOver(RunEndReason.DriverDown);
        }

        public void Deactivate()
        {
            triggered = false;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (rampMesh == null) return;
            if (Application.isPlaying) Destroy(rampMesh);
            else DestroyImmediate(rampMesh);
        }
    }

    public sealed class SpaceGapEvent2DForwarder : MonoBehaviour
    {
        private SpaceGapEvent parentEvent;
        public void AssignParent(SpaceGapEvent target) => parentEvent = target;
        private void Awake()
        {
            if (parentEvent == null) parentEvent = GetComponentInParent<SpaceGapEvent>();
        }
        private void OnTriggerEnter2D(Collider2D other) => parentEvent?.Trigger2D(other);
        private void OnTriggerStay2D(Collider2D other) => parentEvent?.Trigger2D(other);
    }
}
