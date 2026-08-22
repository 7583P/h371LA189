using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    [DisallowMultipleComponent]
    public sealed class PickupTrigger2DRelay : MonoBehaviour
    {
        [SerializeField] private PickupBase owner;

        public void Assign(PickupBase pickupOwner) => owner = pickupOwner;

        private void OnTriggerEnter2D(Collider2D other) => owner?.HandleTrigger2D(other);
        private void OnTriggerStay2D(Collider2D other) => owner?.HandleTrigger2D(other);
    }
}
