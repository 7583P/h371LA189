using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class ItemInventory:MonoBehaviour
    {
        RacingVehiclePhysics vehicle;
        RacingItemPool pool;
        RaceItemType heldItem=RaceItemType.None,pendingItem;
        bool rolling,shieldActive;
        float rollStarted,shieldUntil,lastUse;
        readonly Collider[] hits=new Collider[64];
        readonly RacingVehiclePhysics[] affected=new RacingVehiclePhysics[12];
        public RaceItemType HeldItem=>heldItem;
        public bool HasItem=>heldItem!=RaceItemType.None&&!rolling;
        public bool IsRolling=>rolling;
        public float RouletteProgress=>rolling?Mathf.Clamp01((Time.time-rollStarted)/.9f):1f;
        public bool HasShield=>shieldActive&&Time.time<shieldUntil;
        public float ShieldRemaining=>HasShield?shieldUntil-Time.time:0;
        public DeterministicRng ItemRng{get;private set;}
        public int ItemsUsed{get;private set;}
        public event Action<RaceItemType> OnItemReceived;
        public event Action<RaceItemType> OnItemUsed;
        void Awake(){vehicle=GetComponent<RacingVehiclePhysics>();}
        public void SetSeed(int seed){ItemRng=new DeterministicRng(seed);pool=RacingItemPool.ForScene();}
        void Update()
        {
            if(shieldActive&&Time.time>=shieldUntil)shieldActive=false;
            if(rolling&&Time.time-rollStarted>=.9f){rolling=false;heldItem=pendingItem;OnItemReceived?.Invoke(heldItem);if(GetComponent<PlayerRacingInput>()!=null)RacingFeedback.Play(RacingCue.ItemReady);}
            if(vehicle.RaceEnabled&&vehicle.LastCommands.UseItem)UseItem();
        }
        public void AcquireItem(RaceItemType type,bool rollRoulette=true)
        {
            if(HasItem||rolling||type==RaceItemType.None)return;
            if(rollRoulette){rolling=true;pendingItem=type;rollStarted=Time.time;}
            else{heldItem=type;OnItemReceived?.Invoke(type);}
        }
        public bool UseItem()
        {
            if(!HasItem||vehicle==null||!vehicle.RaceEnabled||Time.time-lastUse<.18f)return false;
            if(pool==null)pool=RacingItemPool.ForScene();
            var used=heldItem;heldItem=RaceItemType.None;lastUse=Time.time;ItemsUsed++;
            switch(used)
            {
                case RaceItemType.TurboPulse:vehicle.ApplySpeedBoost(1.28f,2.1f);break;
                case RaceItemType.HyperCharge:vehicle.ApplySpeedBoost(1.45f,2.8f);shieldActive=true;shieldUntil=Time.time+2.8f;break;
                case RaceItemType.MagneticShield:shieldActive=true;shieldUntil=Time.time+5.5f;break;
                case RaceItemType.ElectroSpike:
                    Vector3 position=transform.position-transform.forward*2.1f;
                    if(Physics.Raycast(position+Vector3.up*1.5f,Vector3.down,out var ground,4,~0,QueryTriggerInteraction.Ignore))position=ground.point+Vector3.up*.15f;
                    pool.Drop(vehicle,position,transform.rotation);break;
                case RaceItemType.KineticMissile:
                    var missile=pool.Fire(vehicle,transform.position+transform.forward*2.2f+Vector3.up*.35f,Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward,Vector3.up),Vector3.up));
                    if(missile!=null)missile.SetTarget(FindTarget());break;
                case RaceItemType.ShockwavePulse:Shockwave();break;
            }
            if(vehicle.GetComponent<PlayerRacingInput>()!=null)RacingFeedback.Play(RacingCue.ItemUse);
            RacingVfxPool.Burst(transform.position,RacingDesign.Cyan,10);OnItemUsed?.Invoke(used);return true;
        }
        Transform FindTarget()
        {
            var tracker=FindFirstObjectByType<TrackProgressTracker>();float nearest=42f;Transform result=null;
            if(tracker==null)return null;
            foreach(var racer in tracker.Leaderboard)
            {
                if(racer.Vehicle==vehicle||racer.Vehicle==null||racer.Finished)continue;
                Vector3 delta=racer.Vehicle.transform.position-transform.position;float distance=delta.magnitude;
                if(distance>=nearest||Vector3.Dot(delta.normalized,transform.forward)<.72f)continue;
                if(Physics.Raycast(transform.position+transform.forward*2.1f+Vector3.up*.35f,delta.normalized,out var hit,distance,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<RacingVehiclePhysics>()!=racer.Vehicle)continue;
                nearest=distance;result=racer.Vehicle.transform;
            }
            return result;
        }
        void Shockwave()
        {
            int count=Physics.OverlapSphereNonAlloc(transform.position,8,hits,~0,QueryTriggerInteraction.Ignore),visited=0;
            for(int i=0;i<count;i++)
            {
                var rival=hits[i].GetComponentInParent<RacingVehiclePhysics>();if(rival==null||rival==vehicle||!rival.RaceEnabled)continue;
                bool duplicate=false;for(int j=0;j<visited;j++)if(affected[j]==rival){duplicate=true;break;}
                if(duplicate)continue;if(visited<affected.Length)affected[visited++]=rival;
                var defense=rival.GetComponent<ItemInventory>();if(defense!=null&&defense.AbsorbHit())continue;
                Vector3 direction=Vector3.ProjectOnPlane(rival.transform.position-transform.position,Vector3.up).normalized;
                rival.Body.AddForce(direction*3.5f,ForceMode.VelocityChange);rival.TriggerSpinout(.38f);
            }
            Array.Clear(affected,0,affected.Length);
        }
        public bool AbsorbHit()
        {
            if(!HasShield)return false;shieldActive=false;RacingVfxPool.Burst(transform.position,RacingDesign.Cyan,10);return true;
        }
    }
}
