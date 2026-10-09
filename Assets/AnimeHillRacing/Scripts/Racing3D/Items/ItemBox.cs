using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemBox:MonoBehaviour
    {
        [SerializeField] float respawnTime=4f;
        Collider trigger;Renderer[] renderers;Transform visual;Vector3 basePosition;float availableAt,phase;TrackProgressTracker tracker;
        RaceItemType fixedItem;
        readonly RacingVehiclePhysics[] collectors=new RacingVehiclePhysics[12];
        readonly float[] collectedAt=new float[12];
        int CollectorSlot(RacingVehiclePhysics vehicle)
        {
            int free=-1;
            for(int i=0;i<collectors.Length;i++)
            {if(collectors[i]==vehicle)return i;if(collectors[i]==null&&free<0)free=i;}
            return free;
        }
        public bool CanCollect(RacingVehiclePhysics vehicle)
        {
            if(vehicle==null||fixedItem==RaceItemType.None)return false;
            var inventory=vehicle.GetComponent<ItemInventory>();int slot=CollectorSlot(vehicle);
            return inventory!=null&&!inventory.HasItem&&!inventory.IsRolling&&slot>=0&&
                (collectors[slot]!=vehicle||Time.time-collectedAt[slot]>=respawnTime);
        }
        void Awake()
        {
            trigger=GetComponent<Collider>();trigger.isTrigger=true;
            var visible=new System.Collections.Generic.List<Renderer>();foreach(var renderer in GetComponentsInChildren<Renderer>())if(renderer.enabled)visible.Add(renderer);
            renderers=visible.ToArray();visual=transform.childCount>0?transform.GetChild(0):transform;basePosition=visual.localPosition;phase=transform.position.x*.1f+transform.position.z*.03f;
        }
        void Start(){tracker=FindFirstObjectByType<TrackProgressTracker>();}
        void Update()
        {
            bool ready=fixedItem!=RaceItemType.None||Time.time>=availableAt;trigger.enabled=ready;
            foreach(var renderer in renderers)if(renderer!=null)renderer.enabled=ready;
            if(!ready)return;
            if(visual!=transform){visual.localPosition=basePosition+Vector3.up*Mathf.Sin(Time.time*2.1f+phase)*.15f;visual.localRotation=Quaternion.Euler(0,Time.time*52+phase*30,0);float respawn=Mathf.Clamp01((Time.time-availableAt)/.25f);visual.localScale=Vector3.one*Mathf.SmoothStep(0,1,respawn);}
        }
        void OnTriggerEnter(Collider other)
        {
            if(fixedItem==RaceItemType.None&&Time.time<availableAt)return;var inventory=other.GetComponentInParent<ItemInventory>();var vehicle=other.GetComponentInParent<RacingVehiclePhysics>();
            if(inventory==null||vehicle==null||!vehicle.RaceEnabled||inventory.HasItem||inventory.IsRolling)return;
            int position=tracker?.FindRacer(vehicle)?.RacePosition??6;
            if(fixedItem!=RaceItemType.None)
            {
                if(!CanCollect(vehicle))return;int slot=CollectorSlot(vehicle);
                collectors[slot]=vehicle;collectedAt[slot]=Time.time;
                inventory.AcquireItem(fixedItem,false);
            }
            else {
                var racer = tracker != null ? tracker.FindRacer(vehicle) : null;
                float gap = tracker != null && tracker.Leaderboard.Count > 0 && racer != null ?
                    Mathf.Max(0, tracker.Leaderboard[0].TotalDistance - racer.TotalDistance) / Mathf.Max(20, vehicle.SpeedMps) : 0;
                inventory.AcquireItem(ItemDistributionTable.RollItem(new ItemRollContext {
                    position=position, gapLeaderSeconds=gap, lap=racer != null ? racer.CurrentLap : 1,
                    totalLaps=tracker != null ? tracker.TotalLaps : 3, held=inventory.HeldItem,
                    previous=inventory.PreviousItem, previousPrevious=inventory.PreviousPreviousItem
                },inventory.ItemRng));availableAt=Time.time+respawnTime;
            }
            RacingVfxPool.Burst(transform.position,RacingDesign.Cyan,12);
            if(other.GetComponentInParent<PlayerRacingInput>()!=null)RacingFeedback.Play(RacingCue.Pickup);
        }
        // A visible, known-content route pickup. Each racer has the same cooldown
        // and must collect, hold and activate the ordinary inventory item.
        public static ItemBox CreateBoostSupply(Transform parent,Vector3 position,float width)
        {
            var root=Create(parent,position,RaceItemType.TurboPulse);root.name="Visible shortcut boost cell";
            var box=root.GetComponent<ItemBox>();box.fixedItem=RaceItemType.TurboPulse;
            root.GetComponent<SphereCollider>().enabled=false;
            var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(width,2.5f,2);
            box.trigger=trigger;
            return box;
        }
        public static GameObject Create(Transform parent,Vector3 position,RaceItemType knownItem = RaceItemType.None)
        {
            var root=new GameObject("Energy item pod");root.transform.SetParent(parent,false);root.transform.position=position;
            var trigger=root.AddComponent<SphereCollider>();trigger.radius=1.1f;trigger.isTrigger=true;
            var visual=new GameObject("Floating pod");visual.transform.SetParent(root.transform,false);
            if (knownItem == RaceItemType.None) RacingItemArt.Pickup(visual.transform); else RacingItemArt.Build(visual.transform, knownItem);
            var box=root.AddComponent<ItemBox>();box.fixedItem=knownItem;return root;
        }
    }
}
