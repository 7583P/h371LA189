using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrashDetector : MonoBehaviour
    {
        [SerializeField] private float minimumWorldHeight = -8f;
        [SerializeField, Min(2f)] private float maximumDistanceBelowTerrain = 14f;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MapProfile terrainProfile;

        private VehicleRuntimeBase vehicle;
        private Rigidbody body;

        private void Awake()
        {
            vehicle = GetComponent<VehicleRuntimeBase>();
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            // Liquid has its own cumulative failure rule. While the chassis is inside a pool,
            // the generic world-height fallback does not bypass that rule.
            if (gameManager != null && gameManager.IsInsideLiquid)
                return;

            float safetyFloor = terrainProfile != null
                ? TerrainGenerator.SampleHeight(terrainProfile, transform.position.x) - maximumDistanceBelowTerrain
                : minimumWorldHeight;

            if (transform.position.y < safetyFloor)
            {
                gameManager?.RequestGameOver(RunEndReason.Crashed);
                enabled = false;
            }
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
