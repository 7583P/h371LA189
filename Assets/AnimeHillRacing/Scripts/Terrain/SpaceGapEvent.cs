using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>A pooled Space event: independent launch ramp, warning, animated black hole and hazard.</summary>
    public sealed class SpaceGapEvent : MonoBehaviour
    {
        private MeshFilter rampFilter;
        private MeshRenderer rampRenderer;
        private MeshCollider rampCollider;
        private EdgeCollider2D rampCollider2D;
        private SpriteRenderer warningRenderer;
        private SpriteRenderer outerRenderer;
        private SpriteRenderer innerRenderer;
        private SphereCollider hazard3D;
        private CircleCollider2D hazard2D;
        private Mesh rampMesh;
        private GameManager gameManager;
        private Vector3 innerBaseScale;
        private float pulsePhase;
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
            Transform ramp = GetOrCreate("Thin Light-Brown Launch Ramp");
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

            if (hazard3D == null) hazard3D = GetComponent<SphereCollider>();
            if (hazard3D == null) hazard3D = gameObject.AddComponent<SphereCollider>();
            hazard3D.isTrigger = true;
            Transform trigger2D = GetOrCreate("Black Hole Hazard Trigger 2D");
            if (hazard2D == null) hazard2D = trigger2D.GetComponent<CircleCollider2D>();
            if (hazard2D == null) hazard2D = trigger2D.gameObject.AddComponent<CircleCollider2D>();
            hazard2D.isTrigger = true;
            SpaceGapEvent2DForwarder forwarder = trigger2D.GetComponent<SpaceGapEvent2DForwarder>();
            if (forwarder == null) forwarder = trigger2D.gameObject.AddComponent<SpaceGapEvent2DForwarder>();
            forwarder.AssignParent(this);
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
            pulsePhase = layout.phase * 1.713f;
            transform.position = new Vector3(layout.rampStart, layout.baselineHeight, 0f);

            BuildRamp(settings, trackWidth);
            rampFilter.transform.localPosition = new Vector3(0f, .025f, -.06f);
            rampRenderer.sharedMaterial = settings.rampMaterial != null
                ? settings.rampMaterial : profile != null ? profile.terrainEdgeMaterial : null;
            // Physics follows the same integrated terrain ramp used by the track. The brown mesh remains
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

            float blackHoleX = (layout.gapStart + layout.gapEnd) * .5f - layout.rampStart;
            float blackHoleWidth = Mathf.Max(1f, layout.gapLength);
            float blackHoleHeight = CalculateBlackHoleVisualHeight(blackHoleWidth);
            float blackHoleY = CalculateBlackHoleCenterY(settings.verticalOffset, blackHoleWidth);
            ConfigureBlackHoleRenderer(outerRenderer, settings.blackHoleSprite,
                blackHoleWidth, blackHoleHeight,
                blackHoleX, blackHoleY, -trackWidth * .5f - .18f, 5);
            ConfigureBlackHoleRenderer(innerRenderer, settings.blackHoleSprite,
                blackHoleWidth * .68f, blackHoleHeight * .68f,
                blackHoleX, blackHoleY, -trackWidth * .5f - .19f, 6);
            outerRenderer.color = Color.white;
            innerRenderer.color = new Color(1f, .62f, .16f, .62f);
            innerBaseScale = innerRenderer.transform.localScale;

            float requestedRadius = Mathf.Max(.35f,
                layout.blackHoleDiameter * settings.hazardRadiusFactor);
            float maximumRadiusBelowRamp = Mathf.Max(.35f, -blackHoleY - .25f);
            float radius = Mathf.Min(requestedRadius, maximumRadiusBelowRamp);
            hazard3D.center = new Vector3(blackHoleX, blackHoleY, 0f);
            hazard3D.radius = radius;
            hazard2D.offset = new Vector2(blackHoleX, blackHoleY);
            hazard2D.radius = radius;

            gameObject.name = $"Space Void Phase {layout.phase} - Pooled";
            gameObject.SetActive(true);
        }

        public static float CalculateBlackHoleVisualHeight(float width) =>
            Mathf.Clamp(Mathf.Max(1f, width) * .62f, 9f, 18f);

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

        private void BuildRamp(SpaceChallengeSettings settings, float trackWidth)
        {
            float length = Mathf.Max(4f, settings.rampLength);
            float flatEnd = length * Mathf.Clamp(settings.rampFlatFraction, .05f, .5f);
            float rise = Mathf.Max(.2f, settings.rampRise);
            float halfWidth = Mathf.Max(1f, trackWidth * .5f);
            float thickness = .62f;
            Vector3[] vertices =
            {
                new Vector3(0f, 0f, -halfWidth), new Vector3(0f, 0f, halfWidth),
                new Vector3(flatEnd, 0f, -halfWidth), new Vector3(flatEnd, 0f, halfWidth),
                new Vector3(length, rise, -halfWidth), new Vector3(length, rise, halfWidth),
                new Vector3(0f, -thickness, -halfWidth), new Vector3(flatEnd, -thickness, -halfWidth),
                new Vector3(length, rise - thickness, -halfWidth)
            };
            int[] triangles =
            {
                0, 1, 2, 1, 3, 2, 2, 3, 4, 3, 5, 4,
                0, 2, 6, 2, 7, 6, 2, 4, 7, 4, 8, 7
            };
            rampMesh.Clear();
            rampMesh.vertices = vertices;
            rampMesh.triangles = triangles;
            rampMesh.RecalculateNormals();
            rampMesh.RecalculateBounds();
            rampCollider.sharedMesh = null;
            rampCollider.sharedMesh = rampMesh;
            rampCollider2D.points = new[] { Vector2.zero, new Vector2(flatEnd, 0f), new Vector2(length, rise) };
        }

        private void Update()
        {
            if (outerRenderer == null || innerRenderer == null) return;
            // The hole and its horizontal silhouette remain perfectly fixed beneath the ramp. Only the
            // orange overlay flickers and breathes, like fire moving around an immobile event horizon.
            outerRenderer.transform.localRotation = Quaternion.identity;
            innerRenderer.transform.localRotation = Quaternion.identity;
            float slowFlame = Mathf.Sin(Time.time * 2.35f + pulsePhase);
            float fastFlame = Mathf.Sin(Time.time * 6.7f + pulsePhase * 1.91f);
            float flame = Mathf.Clamp01(.5f + slowFlame * .34f + fastFlame * .16f);
            innerRenderer.transform.localScale = new Vector3(
                innerBaseScale.x * Mathf.Lerp(.985f, 1.018f, flame),
                innerBaseScale.y * Mathf.Lerp(.97f, 1.035f, 1f - flame),
                innerBaseScale.z);
            innerRenderer.color = new Color(1f, Mathf.Lerp(.48f, .73f, flame),
                Mathf.Lerp(.08f, .22f, flame), Mathf.Lerp(.48f, .72f, flame));
        }

        private void OnTriggerEnter(Collider other)
        {
            VehicleRuntimeBase candidate = other != null ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            if (candidate != null && candidate.Body != null && other.attachedRigidbody == candidate.Body) TriggerCrash();
        }

        internal void Trigger2D(Collider2D other)
        {
            VehicleRuntimeBase candidate = other != null ? other.GetComponentInParent<VehicleRuntimeBase>() : null;
            if (candidate != null && candidate.Body2D != null && other.attachedRigidbody == candidate.Body2D)
                TriggerCrash();
        }

        private void TriggerCrash()
        {
            if (triggered) return;
            triggered = true;
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.RequestGameOver(RunEndReason.Crashed);
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
