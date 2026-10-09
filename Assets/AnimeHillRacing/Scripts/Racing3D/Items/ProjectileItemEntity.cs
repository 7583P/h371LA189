using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public sealed class ProjectileItemEntity : ItemEntityBase
    {
        [SerializeField] private float speedMps = 64f; // Fast, precise Dart projectile
        private Transform target;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        public Transform HomingTarget => target;

        public void SetTarget(Transform racer) { target = racer; }

        public override void Initialize(RacingVehiclePhysics source)
        {
            base.Initialize(source);
            target = null;
        }

        private void FixedUpdate()
        {
            if (!gameObject.activeSelf) return;

            bool golden=ItemType==RaceItemType.GoldenRock;
            if(golden&&target!=null) {
                var racer=target.GetComponent<RacingVehiclePhysics>();
                if(racer==null||!racer.RaceEnabled){Release();return;}
            }
            float distance = (golden?RaceItemUtility.GoldenRockSpeedMps:ItemType == RaceItemType.HomingRocket ? 54f : speedMps) * Time.fixedDeltaTime;
            if (target != null && ItemType != RaceItemType.KineticMissile)
            {
                Vector3 direction = (target.position + Vector3.up * 0.4f - transform.position).normalized;
                // Direct guidance retains the ordinary swept world/vehicle collision test.
                transform.forward = golden?direction:Vector3.RotateTowards(transform.forward, direction, (ItemType == RaceItemType.HomingRocket ? 1.65f : 1.25f) * Time.fixedDeltaTime, 0f);
            }

            int count = Physics.SphereCastNonAlloc(transform.position, 0.28f, transform.forward, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            RacingVehiclePhysics victim = null;
            bool blocked = false;

            for (int i = 0; i < count; i++)
            {
                var racer = hits[i].collider.GetComponentInParent<RacingVehiclePhysics>();
                if (racer == owner || hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                victim = racer;
                blocked = racer == null;
            }

            if (victim != null) { ApplyHit(victim); return; }
            if (blocked) { Release(); return; }

            transform.position += transform.forward * distance;
        }

        private void OnTriggerEnter(Collider other)
        {
            var vehicle = other.GetComponentInParent<RacingVehiclePhysics>();
            if (vehicle != null && vehicle != owner) ApplyHit(vehicle);
        }
    }

}
