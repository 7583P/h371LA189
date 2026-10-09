using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingSkyTrack
    {
        public const int CityLayers=3, BuildingsPerLayer=56;
        public static void Build(Transform parent, TrackNode[] road, TrackShortcut[] shortcuts)
        {
            var scope=parent.GetComponent<RacingEnvironmentScope>();
            var atlas=FacadeAtlas();scope.Own(atlas);
            var material=new Material(Resources.Load<Shader>("RacingSkylineCards")){name="Sky city / one opaque shared facade atlas",mainTexture=atlas};scope.Own(material);
            for(int layer=0;layer<CityLayers;layer++)
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var tris=new List<int>();
                var rng=new DeterministicRng(73019+layer*101);
                float radius=layer==0?380:layer==1?710:1060;
                for(int i=0;i<BuildingsPerLayer;i++)
                {
                    float angle=(i+.35f*layer)*Mathf.PI*2/BuildingsPerLayer;
                    Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    Vector3 right=Vector3.Cross(Vector3.up,radial);
                    float height=rng.Range(112,192),width=rng.Range(28,52)*(1+layer*.25f);
                    Vector3 foot=new Vector3(90,-225,-30)+radial*(radius+rng.Range(-25,25));
                    // Every silhouette stays below the actual road. No city colliders,
                    // billboard Updates, transparency or moving world geometry.
                    height=Mathf.Min(height,-24-foot.y);
                    float tint=1-layer*.23f;
                    Quad(foot,right*width,Vector3.up*height,i%4,new Color(tint,tint,tint));
                    if(i%3==0)Quad(foot+Vector3.up*height,right*width*.52f,Vector3.up*8,i%4,new Color(tint,tint,tint));
                    if(i%7==0)Quad(foot+Vector3.up*(height+8),right*1.4f,Vector3.up*12,i%4,new Color(tint,tint,tint));
                }
                if(layer==0)for(int x=0;x<7;x++)for(int z=0;z<7;z++)
                {
                    Vector3 foot=new Vector3(-310+x*120+rng.Range(-20,20),-225,-390+z*120+rng.Range(-20,20));
                    float height=rng.Range(68,146),width=rng.Range(25,42);Vector3 right=Vector3.right;
                    Quad(foot,right*width,Vector3.up*height,(x+z)%4,new Color(.72f,.79f,.88f));
                    Quad(foot,Vector3.forward*width,Vector3.up*height,(x+z)%4,new Color(.72f,.79f,.88f));
                }
                var mesh=new Mesh{name="Sky city 2D layer "+layer};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();
                var obj=RacingArt.Part(parent,"Sky city / parallax layer "+layer,mesh,material,Vector3.zero,Quaternion.identity);
                obj.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
                var renderer=obj.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                void Quad(Vector3 foot,Vector3 right,Vector3 up,int tile,Color color)
                {
                    int v=vertices.Count;vertices.Add(foot-right*.5f);vertices.Add(foot+right*.5f);vertices.Add(foot+right*.5f+up);vertices.Add(foot-right*.5f+up);
                    float u0=(tile*64+1)/256f,u1=(tile*64+63)/256f;
                    uv.Add(new Vector2(u0,0));uv.Add(new Vector2(u1,0));uv.Add(new Vector2(u1,1));uv.Add(new Vector2(u0,1));
                    for(int c=0;c<4;c++)colors.Add(color);
                    tris.Add(v);tris.Add(v+1);tris.Add(v+2);tris.Add(v);tris.Add(v+2);tris.Add(v+3);
                }
            }
            var guides=new GameObject("Sky track / batched guidance");guides.transform.SetParent(parent,false);
            var core=RacingArt.IdentityMaterial("Sky / cyan edge core",new Color(.25f,.85f,1),.9f);
            var glow=RacingArt.Material(new Color(.035f,.16f,.24f),.4f,true);
            for(int side=-1;side<=1;side+=2)
            {
                float a=side<0?-10.22f:10.08f,b=side<0?-10.08f:10.22f;
                RacingTrackGeometry.Ribbon(guides.transform,"Sky neon edge",road,a,b,core,RacingSurfaceKind.Road,false,true,clearRoutes:shortcuts).transform.localPosition=Vector3.up*.07f;
                a=side<0?-10.32f:9.97f;b=side<0?-9.97f:10.32f;
                RacingTrackGeometry.Ribbon(guides.transform,"Sky edge soft band",road,a,b,glow,RacingSurfaceKind.Road,false,true,clearRoutes:shortcuts).transform.localPosition=Vector3.up*.045f;
                RacingTrackGeometry.Ribbon(guides.transform,"Floating road deck fascia",road,side*10.35f,side*10.37f,
                    RacingArt.Material(new Color(.027f,.045f,.070f),.5f),RacingSurfaceKind.Road,false,true,outerDrop:1.8f,clearRoutes:shortcuts);
            }
            BuildTurnSigns(guides.transform,road,new Color(1,.035f,.075f));
            RacingArt.Combine(guides.transform,100);
            BuildCityFloor(parent,scope);
        }

        static void BuildCityFloor(Transform parent,RacingEnvironmentScope scope)
        {
            // One distant 2D street-map plane anchors the flat building cards.
            // It has no collider and can never become an alternative race path.
            var texture=new Texture2D(128,128,TextureFormat.RGB24,true){name="Sky city / distant street grid",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color32[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                bool road=x%32<3||y%32<3,light=(x%32==1&&y%8==0)||(y%32==1&&x%8==0);
                pixels[y*128+x]=light?new Color32(49,103,130,255):road?new Color32(10,24,39,255):new Color32(3,8,17,255);
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);scope.Own(texture);
            var material=new Material(Resources.Load<Shader>("RacingSkylineCards")){name="Sky city / opaque distant street layer",mainTexture=texture};scope.Own(material);
            var mesh=new Mesh{name="Sky city floor / two triangles",vertices=new[]{new Vector3(-1400,0,-1400),new Vector3(-1400,0,1400),new Vector3(1400,0,1400),new Vector3(1400,0,-1400)},uv=new[]{new Vector2(0,0),new Vector2(0,9),new Vector2(9,9),new Vector2(9,0)},colors=new[]{Color.white,Color.white,Color.white,Color.white},triangles=new[]{0,1,2,0,2,3}};
            mesh.RecalculateBounds();var obj=RacingArt.Part(parent,"Sky city / 2D streets 230m below grid",mesh,material,new Vector3(90,-225,-30),Quaternion.identity);obj.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
            obj.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;obj.GetComponent<MeshRenderer>().receiveShadows=false;
        }

        static Texture2D FacadeAtlas()
        {
            var texture=new Texture2D(256,256,TextureFormat.RGB24,true){name="Sky city / four 2D facade assets / 256x256",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                int local=x%64,tile=x/64,col=local/7,row=y/9;
                int hash=(row*73+col*151+tile*379+193)&1023;
                bool window=local%7>=2&&local%7<=4&&y%9>=2&&y%9<=5&&hash%9>2;
                pixels[y*256+x]=window?(hash%7==0?new Color32(155,136,83,255):new Color32(57,126,170,255)):
                    new Color32((byte)(7+tile*2),(byte)(15+tile*2),(byte)(28+tile*3),255);
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
        }

        public static int BuildTurnSigns(Transform parent,TrackNode[] road,Color color)
        {
            var backing=RacingArt.Material(new Color(.012f,.023f,.042f),.4f);
            var neon=RacingArt.IdentityMaterial("Turn chevrons / "+color,color,1.2f);
            int count=0,last=-16;
            for(int i=8;i<road.Length-8;i++)
            {
                float turn=Vector3.SignedAngle(road[i-5].Forward,road[i+5].Forward,Vector3.up);
                if(Mathf.Abs(turn)<35||i-last<12)continue;
                last=i;count++;
                var n=road[i-3];float direction=Mathf.Sign(turn);
                var sign=new GameObject("Turn guidance / "+i);sign.transform.SetParent(parent,false);
                sign.transform.position=n.Position-n.Right*direction*(n.TrackWidth*.5f+6)+Vector3.up*3.5f;
                sign.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                RacingArt.Box(sign.transform,"Chevron board",Vector3.zero,new Vector3(9.8f,4.4f,.12f),backing,.06f);
                for(int arrow=-1;arrow<=1;arrow++)for(int half=-1;half<=1;half+=2)
                {
                    Vector3 a=new Vector3(arrow*2.7f-direction*.65f,half*1.35f,-.09f),b=new Vector3(arrow*2.7f+direction*.65f,0,-.09f);
                    Vector3 delta=b-a;
                    var bar=RacingArt.Box(sign.transform,"Red neon direction arrow",(a+b)*.5f,new Vector3(delta.magnitude,.43f,.07f),neon,.025f);
                    bar.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                }
            }
            return count;
        }
    }
}
