using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class CrashDetector : MonoBehaviour
    {
        [SerializeField] private float minimumWorldHeight = -100f;
        [SerializeField, Min(20f)] private float maximumDistanceBelowTerrain = 80f;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MapProfile terrainProfile;

        private VehicleRuntimeBase vehicle;

        private void Awake()
        {
            vehicle = GetComponent<VehicleRuntimeBase>();
        }

        private void FixedUpdate()
        {
            // Liquid has its own cumulative failure rule. While the chassis is inside a pool,
            // the generic world-height fallback does not bypass that rule.
            if (gameManager != null && gameManager.IsInsideLiquid)
                return;

            // A vehicle has only fallen out of the world if it drops deep below all possible terrain into the abyss.
            // Never false-kill on steep slopes, crests, or valleys.
            float groundSample = 0f;
            MapProfile activeProfile = terrainProfile != null ? terrainProfile
                : (gameManager != null && gameManager.MapProfile != null ? gameManager.MapProfile : null)
                ?? (TerrainManager.Instance != null ? TerrainManager.Instance.ActiveProfile : null);

            float safetyFloor = -150f;
            if (activeProfile != null)
            {
                TerrainManager terrain = TerrainManager.Instance;
                groundSample = terrain != null && terrain.TrySampleAuthoritativeSurface(transform.position.x,
                    out float authoritativeY, out _)
                    ? authoritativeY
                    : TerrainGenerator.SampleHeight(activeProfile, transform.position.x);
                safetyFloor = Mathf.Min(-100f, groundSample - maximumDistanceBelowTerrain);
            }
            else
            {
                safetyFloor = minimumWorldHeight;
            }

            if (transform.position.y < safetyFloor)
            {
                Debug.Log($"[CrashDetector] Vehicle fell into abyss! Y={transform.position.y:0.0} < SafetyFloor={safetyFloor:0.0} (GroundSample={groundSample:0.0})", this);
                gameManager?.RequestGameOver(RunEndReason.Crashed);
                enabled = false;
            }
        }

        public void AssignGameManager(GameManager manager, MapProfile profile = null)
        {
            gameManager = manager;
            terrainProfile = profile;
        }

        public bool NotifyDriverHeadGroundImpact()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return false;
            gameManager.RequestGameOver(RunEndReason.DriverDown);
            return true;
        }
    }
}
