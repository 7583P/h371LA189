using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingEmblem:MaskableGraphic
    {
        public RaceItemType Item;
        public bool Contours;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;
            if(Contours)
            {
                for(int line=0;line<10;line++)for(int s=0;s<40;s++)
                {
                    float x1=s/40f,x2=(s+1)/40f;
                    float y1=.25f+line*.05f+Mathf.Sin(x1*6+line*.13f)*.09f;
                    float y2=.25f+line*.05f+Mathf.Sin(x2*6+line*.13f)*.09f;
                    Line(vh,new Vector2(r.xMin+x1*r.width,r.yMin+y1*r.height),new Vector2(r.xMin+x2*r.width,r.yMin+y2*r.height),1.2f,color);
                }
                return;
            }
            Vector2 center=r.center;float size=Mathf.Min(r.width,r.height)*.42f;
            if(Item==RaceItemType.Coin10) {
                Ring(vh,center,size*.95f,20,5,color);Ring(vh,center,size*.79f,20,2,color);
                Vector2 plus=center+Vector2.left*size*.45f;
                Line(vh,plus-Vector2.right*size*.15f,plus+Vector2.right*size*.15f,3,color);
                Line(vh,plus-Vector2.up*size*.15f,plus+Vector2.up*size*.15f,3,color);
                Line(vh,center+new Vector2(-size*.08f,-size*.38f),center+new Vector2(-size*.08f,size*.38f),4,color);
                Ring(vh,center+Vector2.right*size*.35f,size*.27f,12,3,color);
            } else if(Item==RaceItemType.Lancer) {
                Vector2 tip=center+new Vector2(.66f,.76f)*size,tail=center-new Vector2(.38f,.42f)*size;
                Line(vh,tail,tip,10,color);
                Ring(vh,center+new Vector2(.2f,.22f)*size,size*.23f,8,3,color);
                Line(vh,tail,tail+new Vector2(-.45f,.12f)*size,5,color);
                Line(vh,tail,tail+new Vector2(.14f,-.43f)*size,5,color);
                for(int i=0;i<3;i++) {
                    Vector2 shift=new Vector2((i-1)*.16f,-(i-1)*.16f)*size;
                    Line(vh,tail+shift,tail+shift-Vector2.one*size*(i==1?.65f:.43f),3,color);
                }
            } else if(Item==RaceItemType.TurboPulse || Item==RaceItemType.TripleTurbo || Item==RaceItemType.HyperCharge || Item==RaceItemType.MiniTurbo) {
                int count=Item==RaceItemType.TripleTurbo?3:1;
                for(int c=0;c<count;c++) {
                    float scale=count==3?.48f:1;Vector2 origin=center+Vector2.right*(c-(count-1)*.5f)*size*.65f;
                    float w=size*.36f*scale,h=size*.88f*scale;
                    Line(vh,origin+new Vector2(-w,-h),origin+new Vector2(-w,h),3,color);
                    Line(vh,origin+new Vector2(w,-h),origin+new Vector2(w,h),3,color);
                    for(int side=-1;side<=1;side+=2)Line(vh,origin+new Vector2(-w,side*h),origin+new Vector2(w,side*h),5,color);
                    for(int i=0;i<2;i++) {Vector2 mid=origin+Vector2.up*(.20f-i*.42f)*h;Line(vh,mid+new Vector2(-w*.6f,-h*.18f),mid+Vector2.up*h*.12f,3,color);Line(vh,mid+new Vector2(w*.6f,-h*.18f),mid+Vector2.up*h*.12f,3,color);}
                }
            } else if(Item==RaceItemType.KineticMissile || Item==RaceItemType.TrackingDart || Item==RaceItemType.HomingRocket) {
                Vector2 tip=center+Vector2.one*size*.72f,tail=center-Vector2.one*size*.6f;
                Line(vh,tail,tip,Item==RaceItemType.HomingRocket?9:6,color);
                Line(vh,center+new Vector2(size*.2f,size*.2f),center+new Vector2(-size*.2f,size*.55f),4,color);
                Line(vh,center+new Vector2(size*.2f,size*.2f),center+new Vector2(size*.55f,-size*.2f),4,color);
                if(Item!=RaceItemType.KineticMissile)Ring(vh,center,size*.66f,16,2,color);
                if(Item==RaceItemType.HomingRocket)Line(vh,tail,tail-Vector2.one*size*.3f,5,color);
            } else if(Item==RaceItemType.ElectroSpike || Item==RaceItemType.TwinRock || Item==RaceItemType.GoldenRock) {
                int count=Item==RaceItemType.TwinRock?2:1;
                for(int c=0;c<count;c++) {
                    Vector2 at=center+Vector2.right*(c-.5f*(count-1))*size*.78f;float radius=size*(count==1?.9f:.58f);
                    for(int i=0;i<7;i++) {
                        float a=i*Mathf.PI*2/7,b=(i+1)*Mathf.PI*2/7;
                        Vector2 x=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*(i%2==0?1:.77f),y=at+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*((i+1)%2==0?1:.77f);
                        Line(vh,x,y,3.5f,color);if(i%2==0)Line(vh,at,x,2,color);
                    }
                }
                if(Item==RaceItemType.GoldenRock) {
                    Ring(vh,center,size*1.08f,12,2,color);
                    Line(vh,center+Vector2.right*size*.85f,center+Vector2.right*size*1.25f,3,color);
                    Line(vh,center+Vector2.right*size*1.25f,center+new Vector2(1.05f,.20f)*size,3,color);
                    Line(vh,center+Vector2.right*size*1.25f,center+new Vector2(1.05f,-.20f)*size,3,color);
                }
            } else if(Item==RaceItemType.MagneticShield) {
                Ring(vh,center,size*.9f,6,4,color);Ring(vh,center,size*.60f,6,2,color);
            } else if(Item==RaceItemType.PulseMine) {
                Ring(vh,center,size*.80f,12,4,color);Ring(vh,center,size*.30f,12,3,color);
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(vh,center+d*size*.65f,center+d*size,4,color);}
            } else if(Item==RaceItemType.AerialRecovery) {
                Line(vh,center-Vector2.up*size*.6f,center+Vector2.up*size*.65f,5,color);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Line(vh,center+new Vector2(side*size*.15f,-size*.2f+i*size*.17f),center+new Vector2(side*size*(.9f-i*.14f),size*(.7f-i*.08f)),3.5f,color);
            } else if(Item==RaceItemType.ShockwavePulse) {
                Ring(vh,center,size*.9f,20,3,color);Ring(vh,center,size*.45f,20,3,color);
            }
        }
        static void Ring(VertexHelper vh,Vector2 center,float radius,int segments,float width,Color color) {
            for(int i=0;i<segments;i++) {float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,color);}
        }
        public static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=vh.currentVertCount;
            vh.AddVert(a-side,color,Vector2.zero);vh.AddVert(a+side,color,Vector2.zero);vh.AddVert(b+side,color,Vector2.zero);vh.AddVert(b-side,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
