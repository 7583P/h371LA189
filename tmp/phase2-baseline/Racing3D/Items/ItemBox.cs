using System.Collections;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemBox : MonoBehaviour
    {
        [SerializeField] private float respawnTime = 4.0f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private Renderer boxRenderer;
        [SerializeField] private Collider boxCollider;

        private bool isAvailable = true;

        private void Awake()
        {
            if (boxCollider == null) boxCollider = GetComponent<Collider>();
            boxCollider.isTrigger = true;
            if (boxRenderer == null) boxRenderer = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            if (isAvailable)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAvailable) return;

            ItemInventory inventory = other.GetComponentInParent<ItemInventory>();
            if (inventory != null && !inventory.HasItem && !inventory.IsRolling)
            {
                // Find race position if tracker is available
                TrackProgressTracker tracker = FindFirstObjectByType<TrackProgressTracker>();
                RacingVehiclePhysics vehicle = other.GetComponentInParent<RacingVehiclePhysics>();
                int pos = 6;
                if (tracker != null && vehicle != null)
                {
                    var prog = tracker.FindRacer(vehicle);
                    if (prog != null) pos = prog.RacePosition;
                }

                RaceItemType rolledItem = ItemDistributionTable.RollItem(pos);
                inventory.AcquireItem(rolledItem, true);

                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            isAvailable = false;
            if (boxRenderer != null) boxRenderer.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;

            yield return new WaitForSeconds(respawnTime);

            isAvailable = true;
            if (boxRenderer != null) boxRenderer.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;
        }
    }
}
