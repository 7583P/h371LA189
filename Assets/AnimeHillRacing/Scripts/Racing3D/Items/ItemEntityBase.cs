using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public abstract class ItemEntityBase:MonoBehaviour
    {
        [SerializeField] protected float lifetime=8f;
        protected RacingVehiclePhysics owner;
        protected float expiresAt,spawnTime;
        public RacingItemPool Pool { get; set; }
        public int PoolUseCount { get; internal set; }
        public virtual void Initialize(RacingVehiclePhysics source) { owner=source; spawnTime=Time.time; expiresAt=Time.time+lifetime; }
        protected void Release() { if(Pool!=null) Pool.Release(this); else gameObject.SetActive(false); }
        protected virtual void Update() { if(Time.time>=expiresAt) Release(); }
        protected virtual void ApplyHit(RacingVehiclePhysics target)
        {
            if(target == null || target == owner) return;
            var inventory=target.GetComponent<ItemInventory>();
            if(inventory == null || !inventory.AbsorbHit()) target.TriggerSpinout(1f);
            RacingFeedback.Play(RacingCue.ItemImpact,transform.position);
            RacingVfxPool.ImpactBurst(transform.position,RacingDesign.Cyan,12);
            Release();
        }
    }
    public sealed class ProjectileItemEntity:ItemEntityBase
    {
        [SerializeField] private float speedMps=58f;
        private Transform target;
        private readonly RaycastHit[] hits=new RaycastHit[16];
        public void SetTarget(Transform racer) { target=racer; }
        public override void Initialize(RacingVehiclePhysics source) { base.Initialize(source); target=null; }
        protected override void Update()
        {
            base.Update(); if(!gameObject.activeSelf) return;
            float distance=speedMps*Time.deltaTime;
            if(target!=null)
            {
                Vector3 direction=(target.position+Vector3.up*.4f-transform.position).normalized;
                transform.forward=Vector3.RotateTowards(transform.forward,direction,1.6f*Time.deltaTime,0);
            }
            int count=Physics.SphereCastNonAlloc(transform.position,.28f,transform.forward,hits,distance,~0,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue; RacingVehiclePhysics victim=null; bool blocked=false;
            for(int i=0;i<count;i++)
            {
                var racer=hits[i].collider.GetComponentInParent<RacingVehiclePhysics>();
                if(racer==owner || hits[i].distance>=nearest) continue;
                nearest=hits[i].distance; victim=racer; blocked=racer==null;
            }
            if(victim!=null) { ApplyHit(victim); return; }
            if(blocked) { Release(); return; }
            transform.position+=transform.forward*distance;
        }
        private void OnTriggerEnter(Collider other) { var vehicle=other.GetComponentInParent<RacingVehiclePhysics>(); if(vehicle!=null && vehicle!=owner) ApplyHit(vehicle); }
    }
    public sealed class TrapItemEntity:ItemEntityBase
    {
        private void OnTriggerStay(Collider other)
        {
            if(Time.time-spawnTime<.45f) return;
            var racer=other.GetComponentInParent<RacingVehiclePhysics>();
            if(racer!=null && racer!=owner) ApplyHit(racer);
        }
    }
}
