using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrashDetector : MonoBehaviour
    {
        [SerializeField, Range(-1f, 1f)] private float invalidUpDot = 0.15f;
        [SerializeField, Min(0.1f)] private float confirmationTime = 1f;
        [SerializeField, Min(0f)] private float maximumVerticalSpeed = 4f;
        [SerializeField] private float minimumWorldHeight = -8f;
        [SerializeField, Min(2f)] private float maximumDistanceBelowTerrain = 14f;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MapProfile terrainProfile;

        private VehicleRuntimeBase vehicle;
        private Rigidbody body;
        private float invalidTimer;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        private void Awake()
        {
            vehicle = GetComponent<VehicleRuntimeBase>();
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            // Liquid has its own cumulative 10-second failure rule. While the chassis is inside a pool,
            // neither the generic world-height fallback nor an inverted pose may bypass that rule.
            if (gameManager != null && gameManager.IsInsideLiquid)
            {
                invalidTimer = 0f;
                return;
            }
            float safetyFloor = terrainProfile != null
                ? TerrainGenerator.SampleHeight(terrainProfile, transform.position.x) - maximumDistanceBelowTerrain
                : minimumWorldHeight;
            if (transform.position.y < safetyFloor)
            {
                gameManager?.RequestGameOver(RunEndReason.Crashed);
                enabled = false;
                return;
            }
            bool inverted = Vector3.Dot(transform.up, Vector3.up) < invalidUpDot;
            bool settledEnough = Mathf.Abs(body.linearVelocity.y) <= maximumVerticalSpeed;
            bool invalid = inverted && (vehicle.AnyGrounded || IsChassisNearGround()) && settledEnough;
            invalidTimer = invalid ? invalidTimer + Time.fixedDeltaTime : 0f;
            if (invalidTimer >= confirmationTime)
            {
                gameManager?.RequestGameOver(RunEndReason.Crashed);
                enabled = false;
            }
        }

        private bool IsChassisNearGround()
        {
            int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * 0.2f, Vector3.down, groundHits, 1.8f,
                ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (groundHits[i].collider != null && !groundHits[i].collider.transform.IsChildOf(transform))
                    return true;
            return false;
        }

        public void AssignGameManager(GameManager manager, MapProfile profile = null)
        {
            gameManager = manager;
            terrainProfile = profile;
        }

        public void NotifyDriverHeadGroundImpact()
        {
            gameManager?.RequestGameOver(RunEndReason.DriverDown);
        }
    }
}
