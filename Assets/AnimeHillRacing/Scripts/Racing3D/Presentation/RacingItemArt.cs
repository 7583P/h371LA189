using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace AnimeHillRacing.Racing3D
{
    // Original, compact meshes shared by pool variants, held items and known pickups.
    public static class RacingItemArt
    {
        static Mesh rock;
        static Material Metal => RacingArt.Material(new Color(.40f,.49f,.61f),.82f);
        static Material Dark => RacingArt.Material(new Color(.035f,.05f,.09f),.75f);
        static Material Glow(RaceItemType type) => RacingArt.Material(RaceItemUtility.ColorFor(type),.5f,true);
        static Material Glass(RaceItemType type) {
            Color color=RaceItemUtility.ColorFor(type);color.a=.24f;
            var material=RacingArt.Material(color,.86f,true);
            material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHABLEND_ON");material.renderQueue=3000;return material;
        }
        public static void Build(Transform parent,bool projectile) => Build(parent,projectile?RaceItemType.KineticMissile:RaceItemType.ElectroSpike);
        public static void Build(Transform parent,RaceItemType type)
        {
            switch(type) {
                case RaceItemType.Coin10: RacingCoinArt.Build(parent);return;
                case RaceItemType.Lancer: RacingLancerArt.Build(parent);return;
                case RaceItemType.MiniTurbo: Canister(parent,type,Vector3.zero);break;
                case RaceItemType.TurboPulse: case RaceItemType.HyperCharge: Canister(parent,type,Vector3.zero);break;
                case RaceItemType.TripleTurbo:
                    for(int i=0;i<3;i++) {var cell=new GameObject("Turbo charge "+(i+1)).transform;cell.SetParent(parent,false);cell.localPosition=new Vector3((i-1)*.55f,i==1?.2f:0,0);cell.localScale=Vector3.one*.65f;Canister(cell,type,Vector3.zero);}break;
                case RaceItemType.ElectroSpike: Boulder(parent,Vector3.zero);break;
                case RaceItemType.GoldenRock: Boulder(parent,Vector3.zero,type);break;
                case RaceItemType.TwinRock: Boulder(parent,new Vector3(-.34f,0,0));Boulder(parent,new Vector3(.34f,.13f,.15f));break;
                case RaceItemType.KineticMissile: case RaceItemType.TrackingDart: case RaceItemType.HomingRocket: Dart(parent,type);break;
                case RaceItemType.PulseMine: case RaceItemType.ShockwavePulse: Disc(parent,type);break;
                case RaceItemType.MagneticShield:
                    RacingArt.Part(parent,"Transparent neon barrier",RacingArt.Ellipsoid("item_shield",Vector3.one*.68f),Glass(type),Vector3.zero,Quaternion.identity);
                    for(int i=0;i<3;i++)RacingArt.Part(parent,"Hex shield lattice",RacingArt.Ring(.72f,.685f,.025f,6),Glow(type),Vector3.zero,Quaternion.Euler(i*60,0,i*50));break;
                case RaceItemType.AerialRecovery:
                    RacingArt.Part(parent,"Recovery crystal",RacingArt.Hull("recovery_core",new Vector4(.025f,-.02f,.02f,-.5f),new Vector4(.19f,-.15f,.15f,0),new Vector4(.03f,-.02f,.02f,.5f)),Glow(type),Vector3.zero,Quaternion.Euler(90,0,0));
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++) {
                        var wing=RacingArt.Box(parent,"Aerial wing vane",new Vector3(side*(.38f+i*.17f),.12f+i*.15f,0),new Vector3(.55f-i*.10f,.10f,.24f),i==1?Glow(type):Metal,.04f);wing.transform.localRotation=Quaternion.Euler(0,0,side*35);
                    }break;
            }
            RacingArt.Combine(parent);
        }
        static void Canister(Transform parent,RaceItemType type,Vector3 offset) {
            RacingArt.Part(parent,"Transparent boost chamber",RacingArt.Hull("turbo_glass",new Vector4(.26f,-.24f,.24f,-.68f),new Vector4(.30f,-.28f,.28f,-.55f),new Vector4(.30f,-.28f,.28f,.55f),new Vector4(.26f,-.24f,.24f,.68f)),Glass(type),offset,Quaternion.Euler(90,0,0));
            RacingArt.Part(parent,"Emissive energy core",RacingArt.Hull("turbo_core",new Vector4(.09f,-.09f,.09f,-.59f),new Vector4(.14f,-.14f,.14f,0),new Vector4(.09f,-.09f,.09f,.59f)),Glow(type),offset,Quaternion.Euler(90,0,0));
            for(int side=-1;side<=1;side+=2) {
                RacingArt.Box(parent,"Graphite cap",offset+Vector3.up*(side*.72f),new Vector3(.64f,.16f,.64f),Dark,.15f);
                RacingArt.Part(parent,"Metal cap band",RacingArt.Ring(.34f,.28f,.075f,12),Metal,offset+Vector3.up*(side*.61f),Quaternion.Euler(0,0,90));
                RacingArt.Part(parent,"Charge halo",RacingArt.Ring(.32f,.28f,.025f,12),Glow(type),offset+Vector3.up*(side*.54f),Quaternion.Euler(0,0,90));
            }
            for(int side=-1;side<=1;side+=2)RacingArt.Box(parent,"Chamber brace",offset+Vector3.right*(side*.31f),new Vector3(.075f,1.13f,.10f),Metal,.02f);
            for(int i=0;i<3;i++)for(int side=-1;side<=1;side+=2) {
                var chevron=RacingArt.Box(parent,"Energy chevron",offset+new Vector3(side*.065f,.26f-i*.24f,-.306f),new Vector3(.16f,.038f,.022f),Glow(type),.008f);chevron.transform.localRotation=Quaternion.Euler(0,0,side*-40);
            }
        }
        static void Boulder(Transform parent,Vector3 position,RaceItemType type=RaceItemType.ElectroSpike) {
            if(rock==null) {
                float g=(1+Mathf.Sqrt(5))*.5f;var v=new[]{new Vector3(-1,g,0),new Vector3(1,g,0),new Vector3(-1,-g,0),new Vector3(1,-g,0),new Vector3(0,-1,g),new Vector3(0,1,g),new Vector3(0,-1,-g),new Vector3(0,1,-g),new Vector3(g,0,-1),new Vector3(g,0,1),new Vector3(-g,0,-1),new Vector3(-g,0,1)};
                int[] idx={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
                var verts=new Vector3[idx.Length];var tris=new int[idx.Length];
                for(int i=0;i<idx.Length;i++){int j=idx[i];verts[i]=Vector3.Scale(v[j].normalized,new Vector3(.51f,.48f,.50f))*(.92f+(j%4)*.055f);tris[i]=i;}
                rock=new Mesh{name="Helmet scale fractured boulder",vertices=verts,triangles=tris};rock.RecalculateNormals();rock.RecalculateBounds();
            }
            bool golden=type==RaceItemType.GoldenRock;
            var stone=RacingArt.Material(golden?RaceItemUtility.ColorFor(type):new Color(.28f,.24f,.23f),golden?.78f:.22f);
            if(golden)stone.SetFloat("_Metallic",.80f);
            RacingArt.Part(parent,golden?"Golden faceted stone":"Faceted stone",rock,stone,position,Quaternion.identity);
            for(int i=0;i<3;i++) {
                var seam=RacingArt.Box(parent,golden?"Gold fracture":"Amber fracture",position+new Vector3(-.19f+i*.17f,.16f-i*.04f,-.45f),new Vector3(.025f,.38f,.018f),Glow(type),.006f);seam.transform.localRotation=Quaternion.Euler(0,0,25-i*35);
            }
            if(golden)RacingArt.Part(parent,"Golden guidance halo",RacingArt.Ring(.62f,.58f,.03f,12),Glow(type),position,Quaternion.Euler(12,0,90));
        }
        static void Dart(Transform parent,RaceItemType type) {
            bool rocket=type==RaceItemType.HomingRocket;float size=rocket?1.4f:1;
            var shell=RacingArt.Part(parent,"Directional aero shell",RacingArt.Hull("item_dart",new Vector4(.10f,-.10f,.10f,-.75f),new Vector4(.20f,-.17f,.17f,-.48f),new Vector4(.14f,-.13f,.13f,.40f),new Vector4(.01f,-.01f,.01f,.92f)),Metal,Vector3.zero,Quaternion.identity);shell.transform.localScale=Vector3.one*size;
            RacingArt.Box(parent,"Guidance spine",new Vector3(0,.19f,-.03f),new Vector3(.075f,.035f,.88f),Glow(type),.01f);
            for(int i=0;i<3;i++) {var fin=RacingArt.Box(parent,"Swept tail fin",new Vector3(0,.30f,-.52f),new Vector3(.04f,.42f,.46f),Dark,.035f);fin.transform.RotateAround(parent.position,parent.forward,i*120);}
            RacingArt.Part(parent,"Ion thruster",RacingArt.Ring(.16f,.09f,.075f,12),Glow(type),new Vector3(0,0,-.78f),Quaternion.Euler(0,90,0));
            RacingArt.Box(parent,"Directional energy tip",new Vector3(0,0,.99f*size),new Vector3(.12f,.12f,.24f)*size,Glow(type),.025f);
            if(rocket)for(int side=-1;side<=1;side+=2)RacingArt.Box(parent,"Rocket ruby side chamber",new Vector3(side*.20f,0,-.08f)*size,new Vector3(.09f,.10f,.48f)*size,Glow(type),.02f);
            if(type==RaceItemType.TrackingDart)RacingArt.Part(parent,"Tracking collar",RacingArt.Ring(.25f,.22f,.04f,8),Glow(type),new Vector3(0,0,.05f),Quaternion.Euler(0,90,0));
        }
        static void Disc(Transform parent,RaceItemType type) {
            RacingArt.Box(parent,"Induction disc",Vector3.zero,new Vector3(1.16f,.22f,1.16f),Dark,.31f);
            RacingArt.Part(parent,"Pulse halo",RacingArt.Ring(.56f,.46f,.05f,16),Glow(type),new Vector3(0,.14f,0),Quaternion.Euler(0,0,90));
            RacingArt.Part(parent,"Live center",RacingArt.Ellipsoid("pulse_cap",new Vector3(.22f,.15f,.22f)),Glow(type),new Vector3(0,.18f,0),Quaternion.identity);
            for(int i=0;i<6;i++) {float a=i*Mathf.PI/3;RacingArt.Box(parent,"Radial lock",new Vector3(Mathf.Cos(a)*.49f,.15f,Mathf.Sin(a)*.49f),new Vector3(.12f,.09f,.12f),Metal,.025f);}
            if(type==RaceItemType.ShockwavePulse)RacingArt.Box(parent,"Shockwave emitter",new Vector3(0,.33f,0),new Vector3(.12f,.26f,.12f),Glow(type),.025f);
        }
        public static void Pickup(Transform parent) {
            var shell=RacingArt.Material(new Color(.06f,.10f,.17f),.72f);
            RacingArt.Box(parent,"Mystery induction capsule",Vector3.zero,new Vector3(.78f,.74f,.78f),shell,.25f);
            RacingArt.Box(parent,"Unknown energy core",Vector3.zero,new Vector3(.38f,.89f,.38f),Glow(RaceItemType.TurboPulse),.16f);
            RacingArt.Part(parent,"Pickup halo",RacingArt.Ring(.66f,.625f,.05f,16),Glow(RaceItemType.TurboPulse),Vector3.zero,Quaternion.Euler(18,0,90));RacingArt.Combine(parent);
        }
    }
}
