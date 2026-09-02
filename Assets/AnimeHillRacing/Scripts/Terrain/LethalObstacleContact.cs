using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>Kills the run when a vehicle body/driver touches an authored lethal obstacle.</summary>
    public sealed class LethalObstacleContact : MonoBehaviour
    {
        [SerializeField] private bool ignoreWheelContacts = true;
        private GameManager gameManager;
        private bool requested;

        public void Configure(GameManager manager, bool wheelsAreSafe = true)
        {
            gameManager = manager;
            ignoreWheelContacts = wheelsAreSafe;
            requested = false;
        }

        private void OnEnable() => requested = false;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null) return;
            // Unity exposes both sides of a 2D contact. Checking both keeps this component
            // reliable whether the callback is delivered to a trigger, platform, or rigidbody.
            HandleContact(collision.collider);
            HandleContact(collision.otherCollider);
        }

        private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);

        private void OnCollisionEnter(Collision collision) =>
            HandleContact(collision != null ? collision.collider : null);

        private void OnTriggerEnter(Collider other) => HandleContact(other);

        private void HandleContact(Component other)
        {
            if (requested || other == null) return;
            VehicleRuntimeBase vehicle = other.GetComponentInParent<VehicleRuntimeBase>();
            if (vehicle == null) return;
            if (ignoreWheelContacts && IsWheelContact(other.transform, vehicle.transform)) return;
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
            requested = true;
            gameManager.RequestGameOver(RunEndReason.Crashed);
        }

        private static bool IsWheelContact(Transform contact, Transform vehicleRoot)
        {
            Transform current = contact;
            while (current != null)
            {
                if (current.GetComponent<Suspension2DController>() != null ||
                    current.GetComponent<SuspensionController>() != null) return true;
                string objectName = current.name;
                if (!string.IsNullOrEmpty(objectName) &&
                    (objectName.IndexOf("wheel", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     objectName.IndexOf("suspension", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
                if (current == vehicleRoot) break;
                current = current.parent;
            }
            return false;
        }
    }
}
