using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingShowroom:MonoBehaviour,IDragHandler,IScrollHandler
    {
        public GameObject PreviewVehicle{get;private set;}
        Camera previewCamera;RenderTexture texture;GameObject stage;float yaw=30,zoom=6.2f,idleTime;
        public static RacingShowroom Create(Transform parent,float x,float y,float w,float h,string vehicle,string wheel,Color? paint=null)
        {
            var rt=RacingDesign.Area(parent,"Vehicle studio",x,y,w,h);var image=rt.gameObject.AddComponent<RawImage>();image.raycastTarget=true;
            var preview=rt.gameObject.AddComponent<RacingShowroom>();preview.Initialize(image);preview.Show(vehicle,wheel,paint);return preview;
        }
        void Initialize(RawImage image)
        {
            stage=new GameObject("Racing studio world");stage.transform.position=new Vector3(5000,0,5000);
            var baseMat=RacingArt.Material(new Color(.08f,.14f,.18f),.55f);
            RacingArt.Box(stage.transform,"Display platform",new Vector3(0,-.68f,0),new Vector3(5.8f,.12f,5.8f),baseMat,.6f);
            RacingArt.Part(stage.transform,"Platform light ring",RacingArt.Ring(2.6f,2.565f,.035f,48),RacingArt.Material(RacingDesign.Cyan,.5f,true),new Vector3(0,-.602f,0),Quaternion.Euler(0,0,90));
            var cameraRoot=new GameObject("Studio camera");cameraRoot.transform.SetParent(stage.transform,false);previewCamera=cameraRoot.AddComponent<Camera>();previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=RacingDesign.Background;previewCamera.fieldOfView=36;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=35;previewCamera.depth=-5;
            texture=new RenderTexture(Application.isMobilePlatform?768:1024,768,24,RenderTextureFormat.ARGB32){name="Racing preview"};texture.Create();previewCamera.targetTexture=texture;image.texture=texture;
            var lightRoot=new GameObject("Studio softbox");lightRoot.transform.SetParent(stage.transform,false);lightRoot.transform.localPosition=new Vector3(-3,5,4);var light=lightRoot.AddComponent<Light>();light.type=LightType.Point;light.color=RacingDesign.Cyan;light.intensity=1.8f;light.range=14;
            var keyRoot=new GameObject("Studio keylight");keyRoot.transform.SetParent(stage.transform,false);var keyLight=keyRoot.AddComponent<Light>();keyLight.type=LightType.Directional;keyLight.color=Color.white;keyLight.intensity=1.1f;keyLight.transform.rotation=Quaternion.Euler(45,-30,0);
        }
        public void Show(string id,string wheels,Color? paint=null)
        {
            if(PreviewVehicle!=null)Destroy(PreviewVehicle);
            PreviewVehicle=new GameObject("Studio "+id);PreviewVehicle.transform.SetParent(stage.transform,false);
            Color c = paint ?? ((id == "car-01") ? RacingVehicleFactory.MetaYellow : RacingDesign.Cyan);
            RacingVehicleVisuals.Build(PreviewVehicle,id,wheels,c,preview:false,isHuman:false);
        }
        void LateUpdate()
        {
            if(previewCamera==null||PreviewVehicle==null)return;
            if(Time.unscaledTime-idleTime>2)yaw+=Time.unscaledDeltaTime*6;
            PreviewVehicle.transform.localRotation=Quaternion.Euler(0,yaw,0);
            previewCamera.transform.localPosition=new Vector3(0,zoom*.38f,zoom);previewCamera.transform.LookAt(stage.transform.position+Vector3.up*.15f);
        }
        public void OnDrag(PointerEventData e){yaw+=e.delta.x*.3f;idleTime=Time.unscaledTime;}
        public void OnScroll(PointerEventData e){zoom=Mathf.Clamp(zoom-e.scrollDelta.y*.35f,4.5f,8.5f);idleTime=Time.unscaledTime;}
        void OnDestroy(){if(texture!=null){texture.Release();Destroy(texture);}if(stage!=null)Destroy(stage);}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingEmblem:MaskableGraphic
    {
        public RaceItemType Item;
        public int Tier=-1;
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
            if(Tier>=0)
            {
                for(int i=0;i<6;i++){float a=(i*60+30)*Mathf.Deg2Rad,b=((i+1)*60+30)*Mathf.Deg2Rad;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size,3,color);}
                Line(vh,center+new Vector2(-.7f,-.25f)*size,center+new Vector2(0,.6f)*size,5,color);
                Line(vh,center+new Vector2(0,.6f)*size,center+new Vector2(.7f,-.25f)*size,5,color);
                for(int i=0;i<Mathf.Min(4,Tier/3+1);i++)Line(vh,center+new Vector2(-.4f,-.35f-i*.12f)*size,center+new Vector2(.4f,-.35f-i*.12f)*size,2,color);
            }
            else if(Item==RaceItemType.TurboPulse||Item==RaceItemType.HyperCharge)
            {
                int count=Item==RaceItemType.HyperCharge?3:2;
                for(int i=0;i<count;i++){float x=(i-(count-1)*.5f)*.45f;Line(vh,center+new Vector2(x-.24f,-.6f)*size,center+new Vector2(x+.24f,0)*size,6,color);Line(vh,center+new Vector2(x+.24f,0)*size,center+new Vector2(x-.24f,.6f)*size,6,color);}
            }
            else if(Item==RaceItemType.KineticMissile)
            {
                Line(vh,center+new Vector2(-.6f,-.6f)*size,center+new Vector2(.55f,.6f)*size,7,color);Line(vh,center+new Vector2(.55f,.6f)*size,center+new Vector2(-.05f,.38f)*size,5,color);Line(vh,center+new Vector2(.55f,.6f)*size,center+new Vector2(.35f,-.05f)*size,5,color);
            }
            else
            {
                int edges=Item==RaceItemType.MagneticShield?6:Item==RaceItemType.ElectroSpike?8:24;
                for(int i=0;i<edges;i++){float a=i*Mathf.PI*2/edges,b=(i+1)*Mathf.PI*2/edges;float radius=Item==RaceItemType.ElectroSpike&&(i&1)==0?size:size*.72f;float nextRadius=Item==RaceItemType.ElectroSpike&&((i+1)&1)==0?size:size*.72f;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*nextRadius,4,color);}
                if(Item==RaceItemType.ShockwavePulse)for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*.38f,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size*.38f,3,color);}
            }
        }
        public static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=vh.currentVertCount;
            vh.AddVert(a-side,color,Vector2.zero);vh.AddVert(a+side,color,Vector2.zero);vh.AddVert(b+side,color,Vector2.zero);vh.AddVert(b-side,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
