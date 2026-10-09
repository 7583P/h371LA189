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

}
