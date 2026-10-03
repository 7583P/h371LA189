using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class RacingPoolCounters
    {
        public int created, reused, returned, active, peakActive, activated;
    }
    public sealed class RacingItemPool : MonoBehaviour
    {
        private readonly List<ProjectileItemEntity> missiles = new List<ProjectileItemEntity>(24);
        private readonly List<TrapItemEntity> traps = new List<TrapItemEntity>(32);
        public int ActiveCount { get; private set; }
        public int DroppedRequests { get; private set; }
        public int Capacity => missiles.Count+traps.Count;
        public int CreatedCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        public int Activations { get; private set; }
        public int Releases { get; private set; }
        public int Reuses { get; private set; }
        public RacingPoolCounters MissileCounters { get; } = new RacingPoolCounters();
        public RacingPoolCounters TrapCounters { get; } = new RacingPoolCounters();
        public static RacingItemPool ForScene()
        {
            var pool = FindFirstObjectByType<RacingItemPool>();
            return pool != null ? pool : new GameObject("Racing item pool").AddComponent<RacingItemPool>();
        }
        // Public world information only. Cheap scan of the existing fixed-size pool.
        public bool HasIncomingThreat(RacingVehiclePhysics racer)
        {
            foreach (var missile in missiles)
            {
                if (!missile.gameObject.activeSelf || missile.Owner == racer) continue;
                Vector3 delta = racer.transform.position - missile.transform.position;
                if (delta.sqrMagnitude < 28 * 28 && Vector3.Dot(delta.normalized, missile.transform.forward) > .85f) return true;
            }
            return false;
        }
        void Awake()
        {
            for(int i=0;i<24;i++) missiles.Add(Create<ProjectileItemEntity>("Kinetic dart",PrimitiveType.Capsule,new Vector3(.42f,.8f,.42f),new Color(.15f,.88f,1)));
            for(int i=0;i<32;i++) traps.Add(Create<TrapItemEntity>("Electro spike",PrimitiveType.Cylinder,new Vector3(1.2f,.18f,1.2f),new Color(1,.34f,.17f)));
        }
        T Create<T>(string name,PrimitiveType shape,Vector3 scale,Color color) where T:ItemEntityBase
        {
            var root = new GameObject(name); root.transform.SetParent(transform,false);
            var collider = root.AddComponent<SphereCollider>(); collider.radius = typeof(T)==typeof(TrapItemEntity)? .8f : .3f; collider.isTrigger = true;
            var body = root.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
            var visual=GameObject.CreatePrimitive(shape); visual.transform.SetParent(root.transform,false); visual.transform.localScale=scale;
            if(typeof(T)==typeof(ProjectileItemEntity)) visual.transform.localRotation=Quaternion.Euler(90,0,0);
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial=RacingArt.Material(color,.65f,true);
            var entity = root.AddComponent<T>(); entity.Pool=this;
            root.SetActive(false); CreatedCount++;
            (entity is ProjectileItemEntity ? MissileCounters : TrapCounters).created++;
            return entity;
        }
        public ProjectileItemEntity Fire(RacingVehiclePhysics owner,Vector3 position,Quaternion rotation)
        {
            foreach(var missile in missiles) if(!missile.gameObject.activeSelf) { Activate(missile,owner,position,rotation); return missile; }
            DroppedRequests++; return null;
        }
        public void Drop(RacingVehiclePhysics owner,Vector3 position,Quaternion rotation)
        {
            foreach(var trap in traps) if(!trap.gameObject.activeSelf) { Activate(trap,owner,position,rotation); return; }
            DroppedRequests++;
        }
        void Activate(ItemEntityBase entity,RacingVehiclePhysics owner,Vector3 position,Quaternion rotation)
        {
            entity.transform.SetPositionAndRotation(position,rotation); entity.gameObject.SetActive(true); entity.Initialize(owner); ActiveCount++;
            if(entity.PoolUseCount>0)Reuses++;entity.PoolUseCount++;Activations++;PeakActiveCount=Mathf.Max(PeakActiveCount,ActiveCount);
            var counters=entity is ProjectileItemEntity ? MissileCounters : TrapCounters;
            if(entity.PoolUseCount>1)counters.reused++;
            counters.activated++;counters.active++;counters.peakActive=Mathf.Max(counters.peakActive,counters.active);
        }
        public void Release(ItemEntityBase entity)
        {
            if(!entity.gameObject.activeSelf) return; entity.gameObject.SetActive(false); ActiveCount=Mathf.Max(0,ActiveCount-1);Releases++;
            var counters=entity is ProjectileItemEntity ? MissileCounters : TrapCounters;
            counters.returned++;counters.active=Mathf.Max(0,counters.active-1);
        }
    }
}
