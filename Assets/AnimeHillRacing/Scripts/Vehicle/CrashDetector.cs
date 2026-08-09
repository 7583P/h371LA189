using AnimeHillRacing.Core;
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
        [SerializeField] private GameManager gameManager;

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
            if (transform.position.y < minimumWorldHeight)
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

        public void AssignGameManager(GameManager manager) => gameManager = manager;

        public void NotifyDriverHeadGroundImpact()
        {
            gameManager?.RequestGameOver(RunEndReason.DriverDown);
        }
    }
}
