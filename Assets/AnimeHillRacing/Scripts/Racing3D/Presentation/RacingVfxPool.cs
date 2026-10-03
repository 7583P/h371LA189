using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingVfxPool : MonoBehaviour
    {
        static RacingVfxPool instance;
        ParticleSystem particles;
        public int BurstCount { get; private set; }
        public int EmittedParticles { get; private set; }
        public int ImpactBursts { get; private set; }
        public int BoostBursts { get; private set; }
        public int CreatedSystems { get; private set; }
        public int ParticleCount => particles!=null?particles.particleCount:0;
        public static RacingVfxPool Ensure()
        {
            if(instance!=null)return instance;
            return instance=new GameObject("Budgeted racing particles").AddComponent<RacingVfxPool>();
        }
        void Awake()
        {
            instance=this;particles=gameObject.AddComponent<ParticleSystem>();CreatedSystems++;var main=particles.main;
            main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=RacingQualityScope.Current==RacingQuality.Low?96:RacingQualityScope.Current==RacingQuality.Medium?192:320;main.startLifetime=.45f;main.startSize=.08f;main.gravityModifier=.25f;
            var emission=particles.emission;emission.enabled=false;
            var shape=particles.shape;shape.enabled=false;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=new Material(Resources.Load<Shader>("RacingParticle"));particles.Play();
        }
        public static void Burst(Vector3 position,Color color,int count,Vector3 direction=default)
        {
            var pool=Ensure();if(Camera.main!=null&&(Camera.main.transform.position-position).sqrMagnitude>10000)return;
            count=Mathf.Min(count,RacingQualityScope.ParticleBurstBudget);
            pool.BurstCount++;pool.EmittedParticles+=count;
            for(int i=0;i<count;i++)
            {
                float angle=i*2.39996f;Vector3 velocity=new Vector3(Mathf.Cos(angle),.7f+(.17f*(i%4)),Mathf.Sin(angle))*1.8f+direction;
                pool.particles.Emit(new ParticleSystem.EmitParams{position=position,velocity=velocity,startColor=color,startSize=.06f+(i%3)*.035f,startLifetime=.25f+(i%5)*.07f},1);
            }
        }
        public static void ImpactBurst(Vector3 position,Color color,int count)
        {
            Ensure().ImpactBursts++;Burst(position,color,count);
        }
        public static void BoostBurst(Vector3 position,Color color,int count,Vector3 direction)
        {
            Ensure().BoostBursts++;Burst(position,color,count,direction);
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
    public sealed class RacingVehicleVfx : MonoBehaviour
    {
        RacingVehiclePhysics vehicle;ItemInventory inventory;TrailRenderer[] trails;GameObject shield;
        float nextEmission;
        Transform boostPlume;
        MeshRenderer[] plumeRenderers;
        Color boostColor=RacingDriftFeedback.Cyan;
        Color trailColor=RacingDriftFeedback.Green;
        public void Initialize()
        {
            vehicle=GetComponent<RacingVehiclePhysics>();inventory=GetComponent<ItemInventory>();trails=new TrailRenderer[2];
            for(int i=0;i<2;i++)
            {
                var root=new GameObject("Tire energy trail");root.transform.SetParent(transform,false);root.transform.localPosition=new Vector3(i==0?-.86f:.86f,-.5f,-1f);
                var trail=root.AddComponent<TrailRenderer>();trail.time=.55f;trail.minVertexDistance=.25f;trail.startWidth=.09f;trail.endWidth=.015f;trail.sharedMaterial=RacingArt.EnergyTrailMaterial();trail.emitting=false;trails[i]=trail;
            }
            shield=RacingArt.Part(transform,"Magnetic shield field",RacingArt.Ring(1.95f,1.89f,.06f,32),RacingArt.Material(RacingDesign.Cyan,.4f,true),new Vector3(0,.2f,0),Quaternion.Euler(0,0,90));shield.SetActive(false);
            boostPlume=new GameObject("Twin ion turbo plumes").transform;boostPlume.SetParent(transform,false);
            var plume=RacingArt.Hull("ion_turbo_plume",new Vector4(.025f,-.025f,.025f,-1.2f),new Vector4(.11f,-.09f,.09f,-.35f),new Vector4(.065f,-.065f,.065f,0));
            for(int s=-1;s<=1;s+=2)RacingArt.Part(boostPlume,"Ion jet",plume,RacingArt.Material(boostColor,.25f,true),new Vector3(s*.4f,-.025f,-1.72f),Quaternion.identity);
            RacingArt.Combine(boostPlume);plumeRenderers=boostPlume.GetComponentsInChildren<MeshRenderer>();boostPlume.gameObject.SetActive(false);
            vehicle.OnImpact+=Impact;vehicle.OnLanding+=Landing;vehicle.OnBoost+=Boost;
        }
        void LateUpdate()
        {
            if(vehicle==null)return;
            Color color=RacingDriftFeedback.ColorForStage(vehicle.CurrentMiniTurboStage);
            if(vehicle.IsDrifting)trailColor=color;
            foreach(var trail in trails){trail.emitting=vehicle.RaceEnabled&&vehicle.IsGrounded&&vehicle.IsDrifting;trail.startColor=trailColor;trail.endColor=new Color(trailColor.r,trailColor.g,trailColor.b,0);}
            bool active=inventory!=null&&inventory.HasShield;shield.SetActive(active);if(active)shield.transform.localScale=Vector3.one*(1+Mathf.Sin(Time.time*5)*.025f);
            bool turbo=vehicle.RaceEnabled&&vehicle.IsBoosting;boostPlume.gameObject.SetActive(turbo);
            if(turbo)boostPlume.localScale=new Vector3(1,1,1+Mathf.Sin(Time.time*27)*.06f);
            if(Time.time<nextEmission||!vehicle.RaceEnabled)return;nextEmission=Time.time+.1f;
            if(vehicle.IsDrifting&&vehicle.IsGrounded) RacingVfxPool.Burst(transform.position-transform.forward*.9f-transform.up*.4f,color,2,-transform.forward*vehicle.SpeedMps*.10f);
            if(vehicle.IsBoosting)RacingVfxPool.BoostBurst(transform.position-transform.forward*1.6f,boostColor,3,-transform.forward*5);
            else if(vehicle.IsOffroad&&vehicle.IsGrounded)RacingVfxPool.Burst(transform.position-transform.forward*1.4f,new Color(.88f,.64f,.4f,.7f),1,-transform.forward*1.5f);
        }
        void Impact(float value){RacingVfxPool.ImpactBurst(transform.position,RacingDesign.Warning,Mathf.CeilToInt(value*10));}
        void Landing(float value){RacingVfxPool.Burst(transform.position-Vector3.up*.4f,RacingDesign.Ink,8);}
        void Boost()
        {
            boostColor=vehicle.CurrentMiniTurboStage==MiniTurboStage.None?RacingDriftFeedback.Cyan:RacingDriftFeedback.ColorForStage(vehicle.CurrentMiniTurboStage);
            var material=RacingArt.Material(boostColor,.25f,true);
            foreach(var renderer in plumeRenderers)renderer.sharedMaterial=material;
            RacingVfxPool.BoostBurst(transform.position-transform.forward,boostColor,10,-transform.forward*3);
        }
        void OnDestroy(){if(vehicle!=null){vehicle.OnImpact-=Impact;vehicle.OnLanding-=Landing;vehicle.OnBoost-=Boost;}}
    }
}
