using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingItemPool : MonoBehaviour
    {
        private readonly List<ProjectileItemEntity> missiles = new List<ProjectileItemEntity>(24);
        private readonly List<TrapItemEntity> traps = new List<TrapItemEntity>(32);
        public int ActiveCount { get; private set; }
        public int DroppedRequests { get; private set; }
        public static RacingItemPool ForScene()
        {
            var pool = FindFirstObjectByType<RacingItemPool>();
            return pool != null ? pool : new GameObject("Racing item pool").AddComponent<RacingItemPool>();
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
            root.SetActive(false); return entity;
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
        }
        public void Release(ItemEntityBase entity)
        {
            if(!entity.gameObject.activeSelf) return; entity.gameObject.SetActive(false); ActiveCount=Mathf.Max(0,ActiveCount-1);
        }
    }
}
