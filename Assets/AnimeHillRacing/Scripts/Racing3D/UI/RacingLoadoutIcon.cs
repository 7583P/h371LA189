using UnityEngine;
using UnityEngine.UI;
namespace AnimeHillRacing.Racing3D
{
    public enum LoadoutIconKind { Vehicle, Wheels, Driver, Accessory }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingLoadoutIcon : MaskableGraphic
    {
        public LoadoutIconKind Kind {get;private set;}
        public string Id {get;private set;}="";
        public static RacingLoadoutIcon Create(Transform parent,LoadoutIconKind kind,float x,float y,float w,float h) {
            var icon=RacingDesign.Area(parent,kind+" loadout icon",x,y,w,h).gameObject.AddComponent<RacingLoadoutIcon>();icon.Kind=kind;icon.raycastTarget=false;return icon;
        }
        public void Configure(string id){id=id??"";if(Id==id)return;Id=id;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();var rect=rectTransform.rect;float scale=Mathf.Min(rect.width,rect.height)*.46f;
            Vector2 P(float x,float y)=>rect.center+new Vector2(x,y)*scale;
            void Line(float x,float y,float xx,float yy,float width,Color c)=>RacingEmblem.Line(vh,P(x,y),P(xx,yy),scale*width,c);
            void Polygon(Color c,params Vector2[] pts){int first=vh.currentVertCount;foreach(var p in pts)vh.AddVert(P(p.x,p.y),c,Vector2.zero);for(int i=1;i<pts.Length-1;i++)vh.AddTriangle(first,first+i,first+i+1);}
            void Ring(float radius,Color c,float width,int count=16){for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;Line(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,width,c);}}
            var ink=RacingDesign.Ink;var dark=RacingDesign.Highlight;
            if(Kind==LoadoutIconKind.Vehicle) {
                bool dapex=Id=="formula-01";Color c=dapex?RacingDesign.Cyan:Id=="rally-01"?RacingDesign.Gold:Id=="sport-bike-01"?RacingDesign.Apex:ink;float width=Id=="sport-bike-01"?.28f:.75f;
                Polygon(c,new Vector2(-width,-.44f),new Vector2(width,-.44f),new Vector2(width*.65f,.3f),new Vector2(.23f,.55f),new Vector2(-.23f,.55f),new Vector2(-width*.65f,.3f));
                Polygon(dark,new Vector2(-.25f,-.12f),new Vector2(.25f,-.12f),new Vector2(.16f,.25f),new Vector2(-.16f,.25f));
                Line(-.92f,-.34f,-.92f,.19f,.21f,ink);Line(.92f,-.34f,.92f,.19f,.21f,ink);
                if(dapex){Line(-.85f,.49f,.85f,.49f,.13f,c);Line(-.8f,-.6f,.8f,-.6f,.12f,c);}
                if(Id=="aerial-satelite-01"){Line(-1,0,-.28f,.38f,.18f,RacingDesign.Apex);Line(1,0,.28f,.38f,.18f,RacingDesign.Apex);}
            } else if(Kind==LoadoutIconKind.Wheels) {
                Color c=Id=="wheel-whity"?Color.white:Id=="wheel-plasma"?RacingDesign.Apex:Id=="wheel-carbon"?RacingDesign.Muted:Id=="wheel-racing"?RacingDesign.Gold:RacingDesign.Cyan;Ring(.83f,c,.19f);Ring(.60f,ink,.05f);
                int spokes=Id=="wheel-offroad"?6:Id=="wheel-whity"?10:5;for(int i=0;i<spokes;i++){float a=i*Mathf.PI*2/spokes;Line(0,0,Mathf.Cos(a)*.58f,Mathf.Sin(a)*.58f,.095f,c);}Ring(.15f,ink,.10f,8);
            } else if(Kind==LoadoutIconKind.Driver) {
                Polygon(RacingLoadoutVisuals.DriverColor(Id),new Vector2(-.74f,-.54f),new Vector2(.65f,-.54f),new Vector2(.82f,-.1f),new Vector2(.65f,.53f),new Vector2(.2f,.85f),new Vector2(-.4f,.7f),new Vector2(-.75f,.2f));
                Polygon(dark,new Vector2(-.05f,.1f),new Vector2(.8f,.1f),new Vector2(.57f,.38f),new Vector2(-.12f,.38f));Line(-.04f,.4f,.6f,.4f,.065f,ink);
            } else if(Id=="crystal-crest") {
                Polygon(RacingDesign.Apex,new Vector2(0,1),new Vector2(-.5f,0),new Vector2(0,-.75f),new Vector2(.5f,0));Line(0,1,0,-.75f,.07f,ink);Line(-.5f,0,.5f,0,.07f,RacingDesign.Cyan);
            } else if(Id=="signal-aerial") {
                Line(0,-.72f,0,.25f,.14f,ink);Ring(.26f,RacingDesign.Cyan,.095f,12);Line(-.65f,.30f,-.42f,.62f,.075f,RacingDesign.Cyan);Line(.65f,.30f,.42f,.62f,.075f,RacingDesign.Cyan);
            } else {Ring(.62f,RacingDesign.Muted,.08f);Line(-.48f,-.48f,.48f,.48f,.075f,RacingDesign.Muted);}
        }
    }
}
