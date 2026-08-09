using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    public abstract class PickupBase : MonoBehaviour
    {
        private PickupPool pool;
        private Collider triggerCollider;
        private Renderer[] renderers;
        private bool collected;
        private bool feedbackActive;
        private bool scaleCached;
        private float feedbackElapsed;
        private Vector3 feedbackStartPosition;
        private Vector3 baseLocalScale;
        [SerializeField, Range(.06f, .25f)] private float feedbackDuration = .14f;
        [SerializeField, Range(1f, 1.35f)] private float feedbackPopScale = 1.18f;
        [SerializeField, Range(0f, .8f)] private float feedbackRise = .28f;
        public abstract PickupKind Kind { get; }
        public float ProgressPosition { get; private set; }
        public long SpawnId { get; private set; }
        public int SpawnVersion { get; private set; }
        public bool IsAvailable => gameObject.activeInHierarchy && !collected;

        public void Spawn(PickupPool owner, Vector3 position, float progressPosition, long spawnId = 0L)
        {
            pool = owner;
            if (triggerCollider == null) triggerCollider = GetComponent<Collider>();
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            if (!scaleCached) { baseLocalScale = transform.localScale; scaleCached = true; }
            collected = false;
            feedbackActive = false;
            feedbackElapsed = 0f;
            ProgressPosition = progressPosition;
            SpawnId = spawnId;
            SpawnVersion++;
            if (triggerCollider != null) triggerCollider.enabled = true;
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = true;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = baseLocalScale;
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other) => TryCollect(other);

        private void OnTriggerStay(Collider other) => TryCollect(other);

        private void TryCollect(Collider other)
        {
            VehicleRuntimeBase vehicle = other.GetComponentInParent<VehicleRuntimeBase>();
            if (vehicle == null || collected)
                return;
            collected = true;
            if (triggerCollider != null) triggerCollider.enabled = false;
            pool?.NotifyCollected(this);
            Collect(vehicle);
            feedbackStartPosition = transform.position;
            feedbackElapsed = 0f;
            feedbackActive = true;
        }

        private void Update()
        {
            if (!feedbackActive) return;
            feedbackElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(feedbackElapsed / Mathf.Max(.01f, feedbackDuration));
            float pop = 1f + (feedbackPopScale - 1f) * Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseLocalScale * pop;
            transform.position = feedbackStartPosition + Vector3.up * (feedbackRise * t);
            if (t < 1f) return;
            feedbackActive = false;
            pool?.Release(this);
        }

        protected abstract void Collect(VehicleRuntimeBase vehicle);
    }

    public enum PickupKind
    {
        Coin,
        Fuel
    }
}
