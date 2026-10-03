using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemBox:MonoBehaviour
    {
        [SerializeField] float respawnTime=4f;
        Collider trigger;Renderer[] renderers;Transform visual;Vector3 basePosition;float availableAt,phase;TrackProgressTracker tracker;
        void Awake()
        {
            trigger=GetComponent<Collider>();trigger.isTrigger=true;renderers=GetComponentsInChildren<Renderer>();visual=transform.childCount>0?transform.GetChild(0):transform;basePosition=visual.localPosition;phase=transform.position.x*.1f+transform.position.z*.03f;
        }
        void Start(){tracker=FindFirstObjectByType<TrackProgressTracker>();}
        void Update()
        {
            bool ready=Time.time>=availableAt;trigger.enabled=ready;
            foreach(var renderer in renderers)if(renderer!=null)renderer.enabled=ready;
            if(!ready)return;
            if(visual!=transform){visual.localPosition=basePosition+Vector3.up*Mathf.Sin(Time.time*2.1f+phase)*.15f;visual.localRotation=Quaternion.Euler(0,Time.time*52+phase*30,0);float respawn=Mathf.Clamp01((Time.time-availableAt)/.25f);visual.localScale=Vector3.one*Mathf.SmoothStep(0,1,respawn);}
        }
        void OnTriggerEnter(Collider other)
        {
            if(Time.time<availableAt)return;var inventory=other.GetComponentInParent<ItemInventory>();var vehicle=other.GetComponentInParent<RacingVehiclePhysics>();
            if(inventory==null||vehicle==null||!vehicle.RaceEnabled||inventory.HasItem||inventory.IsRolling)return;
            int position=tracker?.FindRacer(vehicle)?.RacePosition??6;
            inventory.AcquireItem(ItemDistributionTable.RollItem(position,inventory.ItemRng));availableAt=Time.time+respawnTime;
            RacingVfxPool.Burst(transform.position,RacingDesign.Cyan,12);
            if(other.GetComponentInParent<PlayerRacingInput>()!=null)RacingFeedback.Play(RacingCue.Pickup);
        }
        public static GameObject Create(Transform parent,Vector3 position)
        {
            var root=new GameObject("Energy item pod");root.transform.SetParent(parent,false);root.transform.position=position;
            var trigger=root.AddComponent<SphereCollider>();trigger.radius=1.1f;trigger.isTrigger=true;
            var visual=new GameObject("Floating pod");visual.transform.SetParent(root.transform,false);
            var shell=RacingArt.Box(visual.transform,"Cut crystal",Vector3.zero,new Vector3(.95f,.8f,.95f),RacingArt.Material(RacingDesign.Cyan,.7f,true),.33f);shell.transform.localRotation=Quaternion.Euler(0,45,15);
            RacingArt.Part(visual.transform,"Induction ring",RacingArt.Ring(.74f,.65f,.08f,16),RacingArt.Material(RacingDesign.Ink,.6f),Vector3.zero,Quaternion.Euler(0,0,90));
            RacingArt.Combine(visual.transform);root.AddComponent<ItemBox>();return root;
        }
    }
}
