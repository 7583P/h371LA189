using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-80)]
    public sealed class VerdantCircuitBuilder : MonoBehaviour
    {
        public TrackSplineGraph Graph {get;private set;}
        bool built;
        void Awake()=>Build();
        public void Build()
        {
            if(built)return;built=true;
            var scope=gameObject.AddComponent<RacingEnvironmentScope>();scope.Capture();
            var info=gameObject.AddComponent<RacingTrackInfo>();info.TrackId=VerdantCircuit.Id;info.DisplayName=VerdantCircuit.Name;
            info.Theme="Emerald meadows / coral kerbs / technical grass cuts";
            var nodes=VerdantCircuit.CreateNodes();var routes=VerdantCircuit.CreateShortcuts(nodes);
            var graphRoot=new GameObject("Verdant technical navigation");graphRoot.transform.SetParent(transform,false);
            Graph=graphRoot.AddComponent<TrackSplineGraph>();Graph.InitializeFromNodes(nodes,routes,VerdantCircuit.Checkpoints);
            var gridRoot=new GameObject("Verdant / 12 slot starting grid");gridRoot.transform.SetParent(transform,false);
            gridRoot.transform.SetPositionAndRotation(nodes[0].Position,Quaternion.LookRotation(nodes[0].Forward));gridRoot.AddComponent<StartingGrid>().Initialize(Graph);

            var asphalt=RacingArt.Material(new Color(.10f,.15f,.18f),.25f);
            var grass=RacingArt.Material(new Color(.15f,.39f,.17f),.08f);
            var white=RacingArt.Material(new Color(.94f,.94f,.79f),.2f);
            var coral=RacingArt.Material(new Color(.95f,.22f,.16f),.25f);
            var field=BuildField(transform,scope);
            var fieldCollider=field.AddComponent<BoxCollider>();fieldCollider.center=Vector3.down;fieldCollider.size=new Vector3(1800,2,1600);field.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Grass;
            RacingTrackGeometry.Ribbon(transform,"Verdant winding road",nodes,-10,10,asphalt,RacingSurfaceKind.Road,true,true);
            for(int side=-1;side<=1;side+=2)
                RacingTrackGeometry.Ribbon(transform,"Verdant white edge",nodes,side<0?-10.12f:9.98f,side<0?-9.98f:10.12f,white,RacingSurfaceKind.Road,false,true).transform.localPosition=Vector3.up*.035f;
            var art=new GameObject("Verdant / batched track furniture");art.transform.SetParent(transform,false);
            for(int i=0;i<nodes.Length;i+=2)
            {
                var n=nodes[i];if(!n.IsDriftZone)continue;
                int side=n.OptimalLineOffset<0?-1:1;
                var kerb=RacingArt.Box(art.transform,"Technical apex kerb",n.Position+n.Right*side*(n.TrackWidth*.5f-.45f)+Vector3.up*.035f,new Vector3(.8f,.06f,3),i%4==0?coral:white,.02f);
                kerb.transform.rotation=Quaternion.LookRotation(n.Forward);
            }
            foreach(var route in routes)
            {
                var path=new TrackNode[route.pathPoints.Length];
                for(int i=0;i<path.Length;i++)
                {
                    Vector3 forward=(route.pathPoints[Mathf.Min(i+1,path.Length-1)]-route.pathPoints[Mathf.Max(0,i-1)]).normalized;
                    path[i]=new TrackNode{Position=route.pathPoints[i],Forward=forward,Right=Vector3.Cross(Vector3.up,forward).normalized,Up=Vector3.up,TrackWidth=route.WidthAt(i)};
                }
                var surface=RacingTrackGeometry.Ribbon(transform,route.displayName+" / official grass surface",path,-10,10,grass,RacingSurfaceKind.Grass,true,true,closed:false);
                // Physics/50% corridors remain shared by all racers. The plain
                // meadow supplies their visuals without revealing a painted path.
                surface.GetComponent<MeshRenderer>().enabled=false;
            }
            RacingSkyTrack.BuildTurnSigns(art.transform,nodes,new Color(.95f,.24f,.14f));
            for(int c=0;c<VerdantCircuit.Checkpoints.Length;c++)
            {
                var n=nodes[VerdantCircuit.Checkpoints[c]];var cp=new GameObject(c==0?"Verdant finish":"Verdant checkpoint "+c);cp.transform.SetParent(transform,false);
                cp.transform.SetPositionAndRotation(n.Position+Vector3.up*2,Quaternion.LookRotation(n.Forward));
                var trigger=cp.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(n.TrackWidth+8,9,8);cp.AddComponent<CheckpointTrigger3D>().Configure(c,c==0);
            }
            foreach(int station in new[]{16,64,112,176})for(int lane=-1;lane<=1;lane++)
                ItemBox.Create(transform,nodes[station].Position+nodes[station].Right*lane*5+Vector3.up*1.1f);
            for(int side=-1;side<=1;side+=2)
            {
                RacingArt.Box(art.transform,"Verdant start pylon",nodes[0].Position+nodes[0].Right*side*16+Vector3.up*4,new Vector3(.8f,8,.8f),coral,.1f);
            }
            RacingArt.Box(art.transform,"Verdant finish header",nodes[0].Position+Vector3.up*8,new Vector3(33,.5f,.9f),white,.08f);
            BuildMeadowArt(art.transform,nodes,routes);
            RacingArt.Combine(art.transform,100);
            BuildHills(transform,scope);
            RenderSettings.skybox=null;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.47f,.68f,.72f);RenderSettings.fogDensity=.00065f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.7f,.83f,.88f);
            RenderSettings.ambientEquatorColor=new Color(.45f,.54f,.48f);RenderSettings.ambientGroundColor=new Color(.25f,.33f,.24f);
            var camera=Camera.main;if(camera!=null){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.48f,.72f,.82f);}
            var sun=FindFirstObjectByType<Light>();if(sun!=null){sun.color=new Color(1,.95f,.80f);sun.intensity=1.15f;sun.shadows=LightShadows.Hard;sun.transform.rotation=Quaternion.Euler(45,-32,0);}
        }

        static GameObject BuildField(Transform parent,RacingEnvironmentScope scope)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGB24,true){name="Verdant / mown grass / 64x64",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var pixels=new Color32[4096];var rng=new DeterministicRng(9713);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                int noise=(int)rng.Range(-5,6),stripe=(y/16)%2==0?0:8;
                pixels[y*64+x]=new Color32((byte)(34+noise+stripe),(byte)(83+noise+stripe),(byte)(35+noise+stripe),255);
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);scope.Own(texture);
            var material=new Material(Shader.Find("Standard")){name="Verdant / shared opaque grass",color=Color.white,mainTexture=texture};material.SetFloat("_Glossiness",.05f);scope.Own(material);
            var mesh=new Mesh{name="Verdant field / two triangles",vertices=new[]{new Vector3(-900,0,-800),new Vector3(-900,0,800),new Vector3(900,0,800),new Vector3(900,0,-800)},uv=new[]{new Vector2(0,0),new Vector2(0,18),new Vector2(20,18),new Vector2(20,0)},triangles=new[]{0,1,2,0,2,3}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();var obj=RacingArt.Part(parent,"Meadow / shared grass surface",mesh,material,new Vector3(220,3.8f,-160),Quaternion.identity);obj.AddComponent<RacingGeneratedMesh>().Mesh=mesh;return obj;
        }

        static void BuildHills(Transform parent,RacingEnvironmentScope scope)
        {
            var material=new Material(Resources.Load<Shader>("RacingSkylineCards")){name="Verdant / vertex colored distant hills",mainTexture=Texture2D.whiteTexture};scope.Own(material);
            var vertices=new System.Collections.Generic.List<Vector3>();var colors=new System.Collections.Generic.List<Color>();var triangles=new System.Collections.Generic.List<int>();var uv=new System.Collections.Generic.List<Vector2>();
            for(int layer=0;layer<2;layer++)for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48,radius=1000+layer*180;
                Vector3 x=new Vector3(220+Mathf.Cos(a)*radius,2,-160+Mathf.Sin(a)*radius),y=new Vector3(220+Mathf.Cos(b)*radius,2,-160+Mathf.Sin(b)*radius);
                float h1=45+25*Mathf.Sin(a*5+layer),h2=45+25*Mathf.Sin(b*5+layer);int v=vertices.Count;
                vertices.Add(x);vertices.Add(y);vertices.Add(y+Vector3.up*h2);vertices.Add(x+Vector3.up*h1);
                Color low=layer==0?new Color(.18f,.35f,.27f):new Color(.36f,.51f,.47f),high=layer==0?new Color(.31f,.48f,.33f):new Color(.44f,.61f,.58f);
                colors.Add(low);colors.Add(low);colors.Add(high);colors.Add(high);for(int u=0;u<4;u++)uv.Add(Vector2.zero);
                triangles.Add(v);triangles.Add(v+1);triangles.Add(v+2);triangles.Add(v);triangles.Add(v+2);triangles.Add(v+3);
            }
            var mesh=new Mesh{name="Verdant distant hills / 192 triangles"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var obj=RacingArt.Part(parent,"Verdant / two flat hill layers",mesh,material,Vector3.zero,Quaternion.identity);obj.AddComponent<RacingGeneratedMesh>().Mesh=mesh;obj.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;obj.GetComponent<MeshRenderer>().receiveShadows=false;
        }

        static void BuildMeadowArt(Transform parent,TrackNode[] nodes,TrackShortcut[] routes)
        {
            // Small stylized trees, no colliders or Updates. Shared meshes/materials
            // are combined once. Keep them outside the road and all route cuts.
            var trunk=RacingArt.Material(new Color(.29f,.20f,.12f),.1f);
            var leaf=RacingArt.Material(new Color(.08f,.27f,.16f),.1f);
            for(int i=0;i<nodes.Length;i+=8)
            {
                var n=nodes[i];int side=(i/8)%2==0?-1:1;Vector3 pos=n.Position+n.Right*side*32;
                // No tree is on a supported shortcut corridor.
                bool clear=true;
                foreach(var route in routes)foreach(var p in route.pathPoints)if((p-pos).sqrMagnitude<24*24)clear=false;
                if(!clear)continue;
                RacingArt.Box(parent,"Meadow tree trunk",pos+Vector3.up*2,new Vector3(.7f,4,.7f),trunk,.1f);
                RacingArt.Box(parent,"Meadow geometric crown",pos+Vector3.up*5,new Vector3(5,5,5),leaf,1.1f);
            }
        }
    }
}
