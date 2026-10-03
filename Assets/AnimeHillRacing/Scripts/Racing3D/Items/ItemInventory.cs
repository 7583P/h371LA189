using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class ItemInventory:MonoBehaviour
    {
        RacingVehiclePhysics vehicle;
        RacingItemPool pool;
        TrackProgressTracker tracker;
        RaceItemType heldItem=RaceItemType.None,pendingItem;
        bool rolling,shieldActive;
        float rollStarted,shieldUntil,lastUse;
        readonly Collider[] hits=new Collider[64];
        readonly RaycastHit[] sightHits=new RaycastHit[32];
        readonly RacingVehiclePhysics[] affected=new RacingVehiclePhysics[12];
        public RaceItemType HeldItem=>heldItem;
        public bool HasItem=>heldItem!=RaceItemType.None&&!rolling;
        public bool IsRolling=>rolling;
        public float RouletteProgress=>rolling?Mathf.Clamp01((Time.time-rollStarted)/.9f):1f;
        public bool HasShield=>shieldActive&&Time.time<shieldUntil;
        public float ShieldRemaining=>HasShield?shieldUntil-Time.time:0;
        public DeterministicRng ItemRng{get;private set;}
        public int ItemsUsed{get;private set;}
        public int AcquisitionSerial { get; private set; }
        public float LastAcquiredTime { get; private set; }
        public bool HasMissileTarget => FindTarget() != null;
        public event Action<RaceItemType> OnItemReceived;
        public event Action<RaceItemType> OnItemUsed;
        void Awake(){vehicle=GetComponent<RacingVehiclePhysics>();}
        public void SetSeed(int seed){ItemRng=new DeterministicRng(seed);pool=RacingItemPool.ForScene();}
        void Update()
        {
            if(shieldActive&&Time.time>=shieldUntil)shieldActive=false;
            if(rolling&&Time.time-rollStarted>=.9f){rolling=false;heldItem=pendingItem;AcquisitionSerial++;LastAcquiredTime=Time.time;OnItemReceived?.Invoke(heldItem);if(GetComponent<PlayerRacingInput>()!=null)RacingFeedback.Play(RacingCue.ItemReady);}
            if(vehicle.RaceEnabled&&vehicle.LastCommands.UseItem)UseItem();
        }
        public void AcquireItem(RaceItemType type,bool rollRoulette=true)
        {
            if(HasItem||rolling||type==RaceItemType.None)return;
            if(rollRoulette){rolling=true;pendingItem=type;rollStarted=Time.time;}
            else{heldItem=type;AcquisitionSerial++;LastAcquiredTime=Time.time;OnItemReceived?.Invoke(type);}
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
                    Vector3 origin=transform.position+Vector3.up*.35f;
                    float muzzleDistance=2.2f;
                    int obstacles=Physics.SphereCastNonAlloc(origin,.18f,transform.forward,sightHits,muzzleDistance,~0,QueryTriggerInteraction.Ignore);
                    for(int i=0;i<obstacles;i++)if(sightHits[i].collider.GetComponentInParent<RacingVehiclePhysics>()!=vehicle)
                        muzzleDistance=Mathf.Min(muzzleDistance,Mathf.Max(.25f,sightHits[i].distance-.35f));
                    var missile=pool.Fire(vehicle,origin+transform.forward*muzzleDistance,Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward,Vector3.up),Vector3.up));
                    if(missile!=null)missile.SetTarget(FindTarget());break;
                case RaceItemType.ShockwavePulse:Shockwave();break;
            }
            if(vehicle.GetComponent<PlayerRacingInput>()!=null)RacingFeedback.Play(RacingCue.ItemUse);
            RacingVfxPool.Burst(transform.position,RacingDesign.Cyan,10);OnItemUsed?.Invoke(used);return true;
        }
        Transform FindTarget()
        {
            if(tracker==null)tracker=FindFirstObjectByType<TrackProgressTracker>();float nearest=42f;Transform result=null;
            if(tracker==null)return null;
            var leaderboard=tracker.Leaderboard;
            for(int racerIndex=0;racerIndex<leaderboard.Count;racerIndex++)
            {
                var racer=leaderboard[racerIndex];
                if(racer.Vehicle==vehicle||racer.Vehicle==null||racer.Finished)continue;
                Vector3 delta=racer.Vehicle.transform.position-transform.position;float distance=delta.magnitude;
                if(distance>=nearest||Vector3.Dot(delta.normalized,transform.forward)<.72f)continue;
                Vector3 origin=transform.position+Vector3.up*.35f;
                Vector3 aim=racer.Vehicle.transform.position+Vector3.up*.35f-origin;
                int count=Physics.RaycastNonAlloc(origin,aim.normalized,sightHits,aim.magnitude,~0,QueryTriggerInteraction.Ignore);bool blocked=false;
                for(int i=0;i<count;i++)
                {
                    var hitVehicle=sightHits[i].collider.GetComponentInParent<RacingVehiclePhysics>();
                    if(hitVehicle!=vehicle&&hitVehicle!=racer.Vehicle){blocked=true;break;}
                }
                if(blocked)continue;
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
