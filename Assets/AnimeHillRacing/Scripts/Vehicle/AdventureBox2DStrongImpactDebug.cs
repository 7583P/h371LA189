using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehiclePhysics2DController))]
    public sealed class AdventureBox2DStrongImpactDebug : MonoBehaviour
    {
        private const float RequestedVx = 100f;
        private const float ImpactVy = -40f;
        private const float DrivenSeconds = 6f;
        private const float ObservationSeconds = 8f;

        private VehiclePhysics2DController vehicle;
        private float elapsed;
        private bool active;
        private bool completed;
        private WheelMetrics rear;
        private WheelMetrics front;

        public bool IsActive => active;

        private void Awake()
        {
            vehicle = GetComponent<VehiclePhysics2DController>();
        }

        public void BeginStrongImpact()
        {
            if (vehicle == null) vehicle = GetComponent<VehiclePhysics2DController>();
            if (vehicle == null || vehicle.Body2D == null) return;

            vehicle.SetDriveEnabled(false);
            vehicle.ResetGlitchState();
            vehicle.SetWholeRigVelocity(new Vector2(0f, ImpactVy));
            vehicle.Body2D.angularVelocity = 0f;
            elapsed = 0f;
            rear = default;
            front = default;
            active = true;
            completed = false;
            Debug.Log($"ADVENTURE_BOX2D_IMPACT_BEGIN|x={vehicle.Body2D.position.x:F0}|vx={RequestedVx:F2}|vy={ImpactVy:F2}", this);
        }

        public void ResumeNormalDrive()
        {
            active = false;
            completed = false;
            vehicle?.SetDriveEnabled(true);
        }

        private void FixedUpdate()
        {
            if (!active || vehicle == null || vehicle.Body2D == null) return;

            Vector2 chassisVelocity = vehicle.Body2D.linearVelocity;
            chassisVelocity.x = elapsed < DrivenSeconds ? RequestedVx : 0f;
            vehicle.Body2D.linearVelocity = chassisVelocity;

            SampleWheel(vehicle.RearSuspension, ref rear);
            SampleWheel(vehicle.FrontSuspension, ref front);
            elapsed += Time.fixedDeltaTime;
            if (elapsed + .0001f < ObservationSeconds) return;

            active = false;
            completed = true;
            Debug.Log(BuildResult(), this);
        }

        private static void SampleWheel(Suspension2DController suspension, ref WheelMetrics metrics)
        {
            if (suspension == null || suspension.WheelBody == null) return;
            Rigidbody2D wheel = suspension.WheelBody;
            MapProfile profile = TerrainManager.Instance != null ? TerrainManager.Instance.ActiveProfile : null;
            float surfaceY = profile != null ? TerrainGenerator.SampleHeight(profile, wheel.position.x) : wheel.position.y;
            bool centerBelow = wheel.position.y < surfaceY;
            bool partiallyBelow = wheel.position.y - suspension.WheelRadius < surfaceY;
            if (centerBelow) metrics.centerBelowSeconds += Time.fixedDeltaTime;
            if (partiallyBelow) metrics.partiallyBelowSeconds += Time.fixedDeltaTime;
            metrics.everCenterBelow |= centerBelow;
            metrics.lastCenterBelow = centerBelow;
            metrics.maxOverlap = Mathf.Max(metrics.maxOverlap,
                Mathf.Max(0f, surfaceY - (wheel.position.y - suspension.WheelRadius)));
            metrics.maxJointSpan = Mathf.Max(metrics.maxJointSpan, PhysicalJointSpan(suspension.SuspensionJoint));
            metrics.lastContact = suspension.HasBox2DContact;
        }

        private static float PhysicalJointSpan(WheelJoint2D joint)
        {
            if (joint == null || joint.attachedRigidbody == null || joint.connectedBody == null) return 0f;
            Vector2 wheelAnchor = joint.attachedRigidbody.transform.TransformPoint(joint.anchor);
            Vector2 chassisAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
            return Vector2.Distance(wheelAnchor, chassisAnchor);
        }

        private string BuildResult()
        {
            float x = vehicle != null && vehicle.Body2D != null ? vehicle.Body2D.position.x : 0f;
            return $"ADVENTURE_BOX2D_IMPACT_RESULT|x={x:F0}|vx={RequestedVx:F2}|vy={ImpactVy:F2}" +
                   $"|rearMaxOverlap={rear.maxOverlap:F6}|frontMaxOverlap={front.maxOverlap:F6}" +
                   $"|rearCenterBelowSeconds={rear.centerBelowSeconds:F3}|frontCenterBelowSeconds={front.centerBelowSeconds:F3}" +
                   $"|rearPartiallyBelowSeconds={rear.partiallyBelowSeconds:F3}|frontPartiallyBelowSeconds={front.partiallyBelowSeconds:F3}" +
                   $"|rearMaxJointSpan={rear.maxJointSpan:F6}|frontMaxJointSpan={front.maxJointSpan:F6}" +
                   $"|rearRecovered={rear.everCenterBelow && !rear.lastCenterBelow}|frontRecovered={front.everCenterBelow && !front.lastCenterBelow}" +
                   $"|rearWheelStuck={rear.lastCenterBelow && rear.centerBelowSeconds > 1f}" +
                   $"|frontWheelStuck={front.lastCenterBelow && front.centerBelowSeconds > 1f}";
        }

        private void OnGUI()
        {
            if (!active && !completed) return;
            Rect panel = new Rect(18f, 18f, 560f, 128f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(30f, 27f, 530f, 22f),
                $"BOX2D GOLDEN  X={vehicle.Body2D.position.x:F0}  t={elapsed:F2}/{ObservationSeconds:F0}  vx=100  vy inicial=-40");
            GUI.Label(new Rect(30f, 51f, 530f, 22f),
                $"REAR  overlap={rear.maxOverlap:F3}  centerBelow={rear.centerBelowSeconds:F2}s  joint={rear.maxJointSpan:F3}");
            GUI.Label(new Rect(30f, 75f, 530f, 22f),
                $"FRONT overlap={front.maxOverlap:F3}  centerBelow={front.centerBelowSeconds:F2}s  joint={front.maxJointSpan:F3}");
            GUI.Label(new Rect(30f, 99f, 530f, 22f),
                active ? "Prueba en curso; drivetrain normal desactivado." : "Prueba terminada. Abre Teleport y pulsa REANUDAR MANEJO.");
        }

        private struct WheelMetrics
        {
            public float maxOverlap;
            public float centerBelowSeconds;
            public float partiallyBelowSeconds;
            public float maxJointSpan;
            public bool everCenterBelow;
            public bool lastCenterBelow;
            public bool lastContact;
        }
    }
}
