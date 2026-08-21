using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeHillRacing.Experiments.Box2DJeep
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Box2DJeepRig))]
    public sealed class Box2DStrongImpactPlayMode : MonoBehaviour
    {
        private const float WheelRadius = 0.38f;
        private const float RenderStep = 1f / 60f;

        [Header("Confirmed X=524288 scenario")]
        [SerializeField] private float worldX = 524288f;
        [SerializeField] private float requestedVx = 100f;
        [SerializeField] private float impactVy = -40f;
        [SerializeField] private float driveSeconds = 6f;
        [SerializeField] private float observationSeconds = 8f;
        [SerializeField] private bool pauseAtEnd = true;

        private Box2DJeepRig rig;
        private float elapsed;
        private bool initialized;
        private bool completed;
        private bool frontEverBelow;
        private bool rearEverBelow;
        private float frontBelowSeconds;
        private float rearBelowSeconds;
        private float frontMaxOverlap;
        private float rearMaxOverlap;
        private float frontMaxJointSpan;
        private float rearMaxJointSpan;
        private bool previousPaused;

        private float previousFixedDeltaTime;
        private Vector2 previousGravity;
        private int previousVelocityIterations;
        private int previousPositionIterations;
        private float previousContactOffset;
        private float previousMaxTranslationSpeed;
        private bool previousAutoSyncTransforms;
        private SimulationMode2D previousSimulationMode;
        private float renderAccumulator;

        private Material diagnosticMaterial;
        private LineRenderer frontJointLine;
        private LineRenderer rearJointLine;
        private LineRenderer frontWheelLine;
        private LineRenderer rearWheelLine;

        public bool Completed => completed;
        public float RearBelowSeconds => rearBelowSeconds;
        public float FrontBelowSeconds => frontBelowSeconds;
        public bool RearWheelUnder => rearEverBelow;
        public bool FrontWheelUnder => frontEverBelow;

        private void Awake()
        {
            rig = GetComponent<Box2DJeepRig>();
            StoreAndApplyPhysicsSettings();
            CreateDiagnosticLines();
            ResetScenario();
        }

        private void OnDestroy()
        {
            Time.timeScale = previousPaused ? 0f : 1f;
            Time.fixedDeltaTime = previousFixedDeltaTime;
            Physics2D.gravity = previousGravity;
            Physics2D.velocityIterations = previousVelocityIterations;
            Physics2D.positionIterations = previousPositionIterations;
            Physics2D.defaultContactOffset = previousContactOffset;
            Physics2D.maxTranslationSpeed = previousMaxTranslationSpeed;
            Physics2D.autoSyncTransforms = previousAutoSyncTransforms;
            Physics2D.simulationMode = previousSimulationMode;
            if (diagnosticMaterial != null)
            {
                Destroy(diagnosticMaterial);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                ResetScenario();
            }

            if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
            {
                Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
            }

        }

        private void LateUpdate()
        {
            if (!initialized || completed || Time.timeScale <= 0f)
            {
                UpdateDiagnosticLines();
                return;
            }

            renderAccumulator += RenderStep;
            while (renderAccumulator + 0.000001f >= Time.fixedDeltaTime && !completed)
            {
                StepSimulation();
                renderAccumulator -= Time.fixedDeltaTime;
            }

            UpdateDiagnosticLines();
        }

        private void StepSimulation()
        {
            DisableWheelMotors();
            Vector2 velocity = rig.Chassis.linearVelocity;
            velocity.x = elapsed < driveSeconds ? requestedVx : 0f;
            rig.Chassis.linearVelocity = velocity;

            Physics2D.Simulate(Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            SampleMetrics();
            if (elapsed + 0.0001f < observationSeconds)
            {
                return;
            }

            completed = true;
            Debug.Log(BuildResultLog());
            if (pauseAtEnd)
            {
                Time.timeScale = 0f;
            }
        }

        public void ResetScenario()
        {
            if (rig == null)
            {
                rig = GetComponent<Box2DJeepRig>();
            }

            Time.timeScale = 1f;
            rig.enabled = false;
            rig.Chassis.position = new Vector2(worldX, Box2DJeepFactory.ChassisStartY);
            rig.RearWheel.position = new Vector2(worldX - Box2DJeepFactory.HalfWheelbase, Box2DJeepFactory.WheelStartY);
            rig.FrontWheel.position = new Vector2(worldX + Box2DJeepFactory.HalfWheelbase, Box2DJeepFactory.WheelStartY);
            rig.Chassis.rotation = 0f;
            rig.RearWheel.rotation = 0f;
            rig.FrontWheel.rotation = 0f;
            Vector2 impactVelocity = new Vector2(0f, impactVy);
            rig.Chassis.linearVelocity = impactVelocity;
            rig.RearWheel.linearVelocity = impactVelocity;
            rig.FrontWheel.linearVelocity = impactVelocity;
            rig.Chassis.angularVelocity = 0f;
            rig.RearWheel.angularVelocity = 0f;
            rig.FrontWheel.angularVelocity = 0f;
            DisableWheelMotors();
            rig.Terrain.ConfigureRelativeProfile(rig.Chassis.transform, worldX);
            rig.Terrain.RebuildNow();
            Physics2D.SyncTransforms();

            elapsed = 0f;
            completed = false;
            initialized = true;
            frontEverBelow = false;
            rearEverBelow = false;
            frontBelowSeconds = 0f;
            rearBelowSeconds = 0f;
            frontMaxOverlap = 0f;
            rearMaxOverlap = 0f;
            frontMaxJointSpan = 0f;
            rearMaxJointSpan = 0f;
            renderAccumulator = 0f;
            Debug.Log($"BOX2D_STRONG_IMPACT_PLAY_BEGIN|unity={Application.unityVersion}|x={worldX:F0}|vx={requestedVx:F2}|vy={impactVy:F2}");
        }

        private void StoreAndApplyPhysicsSettings()
        {
            previousPaused = Time.timeScale <= 0f;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            previousGravity = Physics2D.gravity;
            previousVelocityIterations = Physics2D.velocityIterations;
            previousPositionIterations = Physics2D.positionIterations;
            previousContactOffset = Physics2D.defaultContactOffset;
            previousMaxTranslationSpeed = Physics2D.maxTranslationSpeed;
            previousAutoSyncTransforms = Physics2D.autoSyncTransforms;
            previousSimulationMode = Physics2D.simulationMode;

            Time.fixedDeltaTime = 0.02f;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            Physics2D.velocityIterations = 8;
            Physics2D.positionIterations = 3;
            Physics2D.defaultContactOffset = 0.01f;
            Physics2D.maxTranslationSpeed = 100f;
            Physics2D.autoSyncTransforms = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        private void DisableWheelMotors()
        {
            rig.RearJoint.useMotor = false;
            rig.FrontJoint.useMotor = false;
        }

        private void SampleMetrics()
        {
            EdgeCollider2D edge = rig.Terrain.TerrainCollider;
            float rearSurface = EdgeHeightAt(edge, rig.RearWheel.position.x);
            float frontSurface = EdgeHeightAt(edge, rig.FrontWheel.position.x);
            bool rearBelow = rig.RearWheel.position.y < rearSurface;
            bool frontBelow = rig.FrontWheel.position.y < frontSurface;
            rearEverBelow |= rearBelow;
            frontEverBelow |= frontBelow;
            if (rearBelow) rearBelowSeconds += Time.fixedDeltaTime;
            if (frontBelow) frontBelowSeconds += Time.fixedDeltaTime;
            rearMaxOverlap = Mathf.Max(rearMaxOverlap, Overlap(rig.RearCollider, edge));
            frontMaxOverlap = Mathf.Max(frontMaxOverlap, Overlap(rig.FrontCollider, edge));
            rearMaxJointSpan = Mathf.Max(rearMaxJointSpan, JointSpan(rig.RearJoint));
            frontMaxJointSpan = Mathf.Max(frontMaxJointSpan, JointSpan(rig.FrontJoint));
        }

        private string BuildResultLog()
        {
            return
                $"BOX2D_STRONG_IMPACT_PLAY_RESULT|x={worldX:F0}|vx={requestedVx:F2}|vy={impactVy:F2}" +
                $"|rearBelowSeconds={rearBelowSeconds:F3}|frontBelowSeconds={frontBelowSeconds:F3}" +
                $"|rearMaxOverlap={rearMaxOverlap:F6}|frontMaxOverlap={frontMaxOverlap:F6}" +
                $"|rearMaxJointSpan={rearMaxJointSpan:F6}|frontMaxJointSpan={frontMaxJointSpan:F6}" +
                $"|rearEverBelow={rearEverBelow}|frontEverBelow={frontEverBelow}";
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 560f, 188f), "");
            GUI.Label(new Rect(24f, 20f, 530f, 24f), "BOX2D STRONG IMPACT - X=524288");
            GUI.Label(new Rect(24f, 44f, 530f, 24f), $"Time {elapsed:F2}/{observationSeconds:F0}s   vx={requestedVx:F0}   initial vy={impactVy:F0}");
            GUI.Label(new Rect(24f, 68f, 530f, 24f), $"Rear: below={rearBelowSeconds:F3}s  overlap={rearMaxOverlap:F4}  joint={rearMaxJointSpan:F3}");
            GUI.Label(new Rect(24f, 92f, 530f, 24f), $"Front: below={frontBelowSeconds:F3}s  overlap={frontMaxOverlap:F4}  joint={frontMaxJointSpan:F3}");
            GUI.Label(new Rect(24f, 116f, 530f, 24f), $"Front wheel-under: {frontEverBelow}   Rear wheel-under: {rearEverBelow}");
            GUI.Label(new Rect(24f, 140f, 530f, 24f), completed ? "FINISHED / PAUSED" : "RUNNING");
            GUI.Label(new Rect(24f, 164f, 530f, 24f), "R: repeat test    P: pause/resume");
        }

        private void CreateDiagnosticLines()
        {
            Shader shader = Shader.Find("Sprites/Default");
            diagnosticMaterial = new Material(shader);
            rearJointLine = CreateLine("Rear Joint Diagnostic", new Color(0.1f, 0.85f, 1f));
            frontJointLine = CreateLine("Front Joint Diagnostic", new Color(1f, 0.85f, 0.1f));
            rearWheelLine = CreateCircleLine("Rear Wheel Diagnostic", new Color(0.1f, 0.85f, 1f));
            frontWheelLine = CreateCircleLine("Front Wheel Diagnostic", new Color(1f, 0.12f, 0.18f));
        }

        private LineRenderer CreateLine(string lineName, Color color)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = diagnosticMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.055f;
            line.endWidth = 0.055f;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 4;
            return line;
        }

        private LineRenderer CreateCircleLine(string lineName, Color color)
        {
            LineRenderer line = CreateLine(lineName, color);
            line.positionCount = 64;
            line.loop = true;
            line.startWidth = 0.07f;
            line.endWidth = 0.07f;
            return line;
        }

        private void UpdateDiagnosticLines()
        {
            if (rig == null) return;
            SetJointLine(rearJointLine, rig.RearJoint);
            SetJointLine(frontJointLine, rig.FrontJoint);
            SetWheelCircle(rearWheelLine, rig.RearWheel.position);
            SetWheelCircle(frontWheelLine, rig.FrontWheel.position);
        }

        private static void SetJointLine(LineRenderer line, WheelJoint2D joint)
        {
            if (line == null || joint == null || joint.connectedBody == null) return;
            line.SetPosition(0, joint.attachedRigidbody.transform.TransformPoint(joint.anchor));
            line.SetPosition(1, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
        }

        private static void SetWheelCircle(LineRenderer line, Vector2 center)
        {
            if (line == null) return;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * WheelRadius);
            }
        }

        private static float JointSpan(WheelJoint2D joint)
        {
            if (joint == null || joint.connectedBody == null) return float.PositiveInfinity;
            Vector2 wheelAnchor = joint.attachedRigidbody.transform.TransformPoint(joint.anchor);
            Vector2 chassisAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
            return Vector2.Distance(wheelAnchor, chassisAnchor);
        }

        private static float Overlap(Collider2D first, Collider2D second)
        {
            ColliderDistance2D distance = Physics2D.Distance(first, second);
            return distance.isValid && distance.isOverlapped ? Mathf.Max(0f, -distance.distance) : 0f;
        }

        private static float EdgeHeightAt(EdgeCollider2D edge, float worldX)
        {
            Vector2[] points = edge.points;
            if (points.Length == 0) return float.NegativeInfinity;
            float localX = edge.transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
            if (localX <= points[0].x) return edge.transform.TransformPoint(points[0]).y;
            if (localX >= points[points.Length - 1].x) return edge.transform.TransformPoint(points[points.Length - 1]).y;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].x < localX) continue;
                float t = Mathf.InverseLerp(points[i - 1].x, points[i].x, localX);
                return edge.transform.TransformPoint(Vector2.Lerp(points[i - 1], points[i], t)).y;
            }
            return edge.transform.TransformPoint(points[points.Length - 1]).y;
        }
    }
}
