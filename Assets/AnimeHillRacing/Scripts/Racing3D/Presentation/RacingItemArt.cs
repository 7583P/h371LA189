using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Original low-poly parts, built once while prewarming the existing pools.
    // No primitives, colliders, lights or allocations are created when an item fires.
    public static class RacingItemArt
    {
        public static void Build(Transform parent, bool projectile)
        {
            var graphite=RacingArt.Material(new Color(.045f,.065f,.10f),.7f);
            var metal=RacingArt.Material(new Color(.47f,.57f,.67f),.75f);
            var cyan=RacingArt.Material(RacingDriftFeedback.Cyan,.45f,true);
            var danger=RacingArt.Material(RacingVehicleFactory.MetaRed,.4f,true);
            if(projectile)
            {
                RacingArt.Part(parent,"Kinetic dart aero shell",RacingArt.Hull("kinetic_dart",new Vector4(.07f,-.07f,.07f,-.52f),new Vector4(.13f,-.13f,.13f,-.30f),new Vector4(.11f,-.11f,.11f,.27f),new Vector4(.02f,-.02f,.02f,.62f)),metal,Vector3.zero,Quaternion.identity);
                RacingArt.Box(parent,"Charge spine",new Vector3(0,.132f,-.01f),new Vector3(.05f,.025f,.65f),cyan,.01f);
                for(int i=0;i<3;i++)
                {
                    var fin=RacingArt.Box(parent,"Swept graphite fin",new Vector3(0,.19f,-.30f),new Vector3(.035f,.26f,.26f),graphite,.02f);
                    fin.transform.RotateAround(parent.position,parent.forward,i*120);
                }
                RacingArt.Part(parent,"Ion nozzle",RacingArt.Ring(.105f,.07f,.035f,12),cyan,new Vector3(0,0,-.55f),Quaternion.Euler(0,90,0));
                RacingArt.Box(parent,"Guidance tip",new Vector3(0,0,.56f),new Vector3(.045f,.045f,.1f),danger,.015f);
            }
            else
            {
                RacingArt.Box(parent,"Hex induction base",Vector3.zero,new Vector3(1.10f,.12f,1.10f),graphite,.30f);
                RacingArt.Part(parent,"Electro warning ring",RacingArt.Ring(.46f,.40f,.026f,12),danger,new Vector3(0,.073f,0),Quaternion.Euler(0,0,90));
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3;
                    var spike=RacingArt.Part(parent,"Conductive spike",RacingArt.Hull("electro_prong",new Vector4(.08f,-.05f,.05f,0),new Vector4(.02f,-.018f,.018f,.28f)),metal,new Vector3(Mathf.Cos(a)*.28f,.10f,Mathf.Sin(a)*.28f),Quaternion.Euler(-58,i*60,0));
                }
                RacingArt.Box(parent,"Live capacitor",new Vector3(0,.12f,0),new Vector3(.18f,.12f,.18f),cyan,.04f);
            }
            RacingArt.Combine(parent);
        }

        public static void Pickup(Transform parent)
        {
            var shell=RacingArt.Material(new Color(.06f,.10f,.17f),.72f);
            var cyan=RacingArt.Material(RacingDriftFeedback.Cyan,.4f,true);
            var white=RacingArt.Material(RacingDesign.Platinum,.62f);
            RacingArt.Box(parent,"Induction capsule",Vector3.zero,new Vector3(.78f,.74f,.78f),shell,.25f);
            RacingArt.Box(parent,"Energy core",Vector3.zero,new Vector3(.38f,.89f,.38f),cyan,.16f);
            for(int s=-1;s<=1;s+=2)
            {
                RacingArt.Box(parent,"Capsule edge",new Vector3(s*.35f,0,0),new Vector3(.065f,.58f,.43f),white,.035f);
                RacingArt.Box(parent,"Charge slit",new Vector3(0,0,s*.40f),new Vector3(.36f,.12f,.025f),cyan,.012f);
            }
            RacingArt.Part(parent,"Induction orbit",RacingArt.Ring(.66f,.625f,.05f,16),white,Vector3.zero,Quaternion.Euler(18,0,90));
            RacingArt.Combine(parent);
        }
    }
}
