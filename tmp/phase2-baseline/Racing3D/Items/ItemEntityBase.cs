using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public abstract class ItemEntityBase : MonoBehaviour
    {
        [SerializeField] protected float lifetime = 8f;
        protected RacingVehiclePhysics owner;

        public virtual void Initialize(RacingVehiclePhysics source)
        {
            owner = source;
            Destroy(gameObject, lifetime);
        }

        protected virtual void ApplyHit(RacingVehiclePhysics target)
        {
            if (target != null)
            {
                target.TriggerSpinout(1.2f);
            }
            Destroy(gameObject);
        }
    }

    public sealed class ProjectileItemEntity : ItemEntityBase
    {
        [SerializeField] private float speedMps = 55f;
        [SerializeField] private float homingSteer = 2.5f;
        private Transform targetRacer;

        public void SetTarget(Transform target)
        {
            targetRacer = target;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (targetRacer != null)
            {
                Vector3 toTarget = (targetRacer.position + Vector3.up * 0.5f - transform.position).normalized;
                transform.forward = Vector3.RotateTowards(transform.forward, toTarget, homingSteer * dt, 0f);
            }

            transform.position += transform.forward * (speedMps * dt);
        }

        private void OnTriggerEnter(Collider other)
        {
            RacingVehiclePhysics hitVehicle = other.GetComponentInParent<RacingVehiclePhysics>();
            if (hitVehicle != null && hitVehicle != owner)
            {
                ApplyHit(hitVehicle);
            }
            else if (!other.isTrigger && other.gameObject.layer != gameObject.layer)
            {
                // Hit track wall / terrain
                Destroy(gameObject);
            }
        }
    }

    public sealed class TrapItemEntity : ItemEntityBase
    {
        [SerializeField] private float armDelay = 0.4f;
        private float spawnTime;

        private void Awake()
        {
            spawnTime = Time.time;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - spawnTime < armDelay) return;

            RacingVehiclePhysics hitVehicle = other.GetComponentInParent<RacingVehiclePhysics>();
            if (hitVehicle != null)
            {
                ApplyHit(hitVehicle);
            }
        }
    }
}
