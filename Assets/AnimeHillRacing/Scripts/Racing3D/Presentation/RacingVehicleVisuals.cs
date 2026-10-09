using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingArt
    {
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){materials.Clear();meshes.Clear();}
        public static Material Material(Color color,float smoothness=.25f,bool emission=false)
        {
            string key=((Color32)color).ToString()+smoothness.ToString("F2")+emission;
            if(materials.TryGetValue(key,out var mat)&&mat!=null)return mat;
            mat=new Material(Shader.Find("Standard")){name="Racing palette "+key,color=color,enableInstancing=true};mat.SetFloat("_Glossiness",smoothness);mat.SetFloat("_Metallic",smoothness>.5f?.35f:0);
            if(emission){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.75f);}
            materials[key]=mat;return mat;
        }
        public static Material MetaBodyMaterial()
        {
            string key="DAPEX / midnight blue pearl";
            if(materials.TryGetValue(key,out var mat)&&mat!=null)
            {
                mat.color = RacingVehicleFactory.MetaNavy;
                return mat;
            }
            mat=new Material(Shader.Find("Standard")){name=key,color=RacingVehicleFactory.MetaNavy,enableInstancing=true};
            mat.SetFloat("_Glossiness",.76f);
            mat.SetFloat("_Metallic",.58f);
            materials[key]=mat;return mat;
        }
        public static Material IdentityMaterial(string key, Color color, float glow = .12f)
        {
            if(materials.TryGetValue(key,out var material)&&material!=null)
            {
                material.color = color;
                material.SetColor("_EmissionColor", color * glow);
                return material;
            }
            material=new Material(Shader.Find("Standard")){name=key,color=color,enableInstancing=true};
            material.SetFloat("_Metallic", glow > .5f ? .10f : 0f);
            material.SetFloat("_Glossiness", glow > .5f ? .88f : .55f);
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*glow);
            materials[key]=material;return material;
        }
        public static Material EnergyTrailMaterial()
        {
            const string key="Vertex-colored energy trail";
            if(materials.TryGetValue(key,out var material)&&material!=null)return material;
            material=new Material(Resources.Load<Shader>("RacingEnergyTrail")){name=key};materials[key]=material;return material;
        }
        public static Mesh Bevel(Vector3 size,float cut=.18f)
        {
            string key="bevel"+size+cut;
            if(meshes.TryGetValue(key,out var cached))return cached;
            float x=size.x*.5f,y=size.y*.5f,z=size.z*.5f,c=Mathf.Min(cut,Mathf.Min(x,z)*.5f);
            Vector2[] ring={new Vector2(-x+c,-z),new Vector2(x-c,-z),new Vector2(x,-z+c),new Vector2(x,z-c),new Vector2(x-c,z),new Vector2(-x+c,z),new Vector2(-x,z-c),new Vector2(-x,-z+c)};
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<8;i++)
            {
                int n=(i+1)%8;
                AddQuad(vertices,indices,new Vector3(ring[i].x,-y,ring[i].y),new Vector3(ring[i].x,y,ring[i].y),new Vector3(ring[n].x,y,ring[n].y),new Vector3(ring[n].x,-y,ring[n].y));
            }
            int top=vertices.Count;vertices.Add(new Vector3(0,y,0));for(int i=0;i<8;i++)vertices.Add(new Vector3(ring[i].x,y,ring[i].y));
            int bottom=vertices.Count;vertices.Add(new Vector3(0,-y,0));for(int i=0;i<8;i++)vertices.Add(new Vector3(ring[i].x,-y,ring[i].y));
            for(int i=0;i<8;i++){indices.Add(top);indices.Add(top+(i+1)%8+1);indices.Add(top+i+1);indices.Add(bottom);indices.Add(bottom+i+1);indices.Add(bottom+(i+1)%8+1);}
            var mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
        }
        static void AddQuad(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int v=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);indices.Add(v);indices.Add(v+1);indices.Add(v+2);indices.Add(v);indices.Add(v+2);indices.Add(v+3);
        }
        public static Mesh Ring(float outer,float inner,float depth,int segments=20)
        {
            string key="ring"+outer+inner+depth+segments;if(meshes.TryGetValue(key,out var cached))return cached;
            var verts=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Vector3 frontA=new Vector3(-depth*.5f,Mathf.Cos(a)*outer,Mathf.Sin(a)*outer),frontB=new Vector3(-depth*.5f,Mathf.Cos(b)*outer,Mathf.Sin(b)*outer);
                Vector3 backA=frontA+Vector3.right*depth,backB=frontB+Vector3.right*depth;
                Vector3 ia=new Vector3(-depth*.5f,Mathf.Cos(a)*inner,Mathf.Sin(a)*inner),ib=new Vector3(-depth*.5f,Mathf.Cos(b)*inner,Mathf.Sin(b)*inner);
                AddQuad(verts,tris,frontA,frontB,backB,backA);
                AddQuad(verts,tris,frontA,ia,ib,frontB);
                AddQuad(verts,tris,backA,backB,ib+Vector3.right*depth,ia+Vector3.right*depth);
                AddQuad(verts,tris,ia,ia+Vector3.right*depth,ib+Vector3.right*depth,ib);
            }
            var mesh=new Mesh{name=key};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
        }
        public static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Quaternion rotation)
        {
            var part=new GameObject(name);part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localRotation=rotation;
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;return part;
        }
        public static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,float bevel=.12f)
            => Part(parent,name,Bevel(size,bevel),material,position,Quaternion.identity);
        // Width, lower height, upper height and longitudinal station. Flat faces give the
        // authored Adventure silhouettes clean highlights without primitive colliders.
        public static Mesh Hull(string key,params Vector4[] stations)
        {
            key="hull_"+key;if(meshes.TryGetValue(key,out var cached))return cached;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int s=0;s<stations.Length-1;s++)for(int side=0;side<8;side++)
                AddQuad(vertices,triangles,Point(s,side),Point(s,(side+1)%8),Point(s+1,(side+1)%8),Point(s+1,side));
            for(int end=0;end<2;end++)
            {
                int s=end==0?0:stations.Length-1;int first=vertices.Count;
                vertices.Add(new Vector3(0,(stations[s].y+stations[s].z)*.5f,stations[s].w));
                for(int i=0;i<8;i++)vertices.Add(Point(s,i));
                for(int i=0;i<8;i++){triangles.Add(first);triangles.Add(first+(end==0?(i+1)%8:i)+1);triangles.Add(first+(end==0?i:(i+1)%8)+1);}
            }
            var mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
            Vector3 Point(int s,int i)
            {
                Vector4 p=stations[s];float bevel=Mathf.Min(.08f,(p.z-p.y)*.28f);
                Vector2[] ring={new Vector2(-p.x*.78f,p.y),new Vector2(p.x*.78f,p.y),new Vector2(p.x,p.y+bevel),new Vector2(p.x,p.z-bevel),new Vector2(p.x*.78f,p.z),new Vector2(-p.x*.78f,p.z),new Vector2(-p.x,p.z-bevel),new Vector2(-p.x,p.y+bevel)};
                return new Vector3(ring[i].x,ring[i].y,p.w);
            }
        }
        public static Mesh Ellipsoid(string key,Vector3 radii)
        {
            key="ellipsoid_"+key;if(meshes.TryGetValue(key,out var cached))return cached;
            int slices=16,rings=10;var vertices=new List<Vector3>();var indices=new List<int>();
            for(int y=0;y<=rings;y++)for(int x=0;x<=slices;x++)
            {
                float a=x*Mathf.PI*2/slices,b=y*Mathf.PI/rings;
                vertices.Add(Vector3.Scale(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)),radii));
                if(y==rings||x==slices)continue;
                int v=y*(slices+1)+x;indices.Add(v);indices.Add(v+1);indices.Add(v+slices+1);indices.Add(v+1);indices.Add(v+slices+2);indices.Add(v+slices+1);
            }
            var mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
        }
        // Shared, rounded body panels: sixteen vertices per station, no textures,
        // extra colliders or per-car material instances.
        public static Mesh AeroShell(string key,params Vector4[] stations)
        {
            key="aeroshell_"+key;if(meshes.TryGetValue(key,out var cached))return cached;
            const int sides=16;var vertices=new List<Vector3>();var indices=new List<int>();
            foreach(var p in stations)for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=Mathf.Cos(a),s=Mathf.Sin(a);
                vertices.Add(new Vector3(p.x*Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.65f),
                    (p.y+p.z)*.5f+(p.z-p.y)*.5f*Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.75f),p.w));
            }
            for(int p=0;p<stations.Length-1;p++)for(int i=0;i<sides;i++)
            {
                int a=p*sides+i,b=p*sides+(i+1)%sides,c=b+sides,d=a+sides;
                indices.Add(a);indices.Add(b);indices.Add(c);indices.Add(a);indices.Add(c);indices.Add(d);
            }
            for(int end=0;end<2;end++)
            {
                int p=end==0?0:stations.Length-1,v=vertices.Count;
                vertices.Add(new Vector3(0,(stations[p].y+stations[p].z)*.5f,stations[p].w));
                for(int i=0;i<sides;i++)vertices.Add(vertices[p*sides+i]);
                for(int i=0;i<sides;i++)
                {indices.Add(v);indices.Add(v+1+(end==0?(i+1)%sides:i));indices.Add(v+1+(end==0?i:(i+1)%sides));}
            }
            var mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
        }
        public static Mesh Airfoil(string key,float span,float chord)
        {
            key="airfoil_"+key;if(meshes.TryGetValue(key,out var cached))return cached;
            Vector2[] section={new Vector2(-.5f,0),new Vector2(-.32f,.020f),new Vector2(.05f,.040f),new Vector2(.38f,.030f),new Vector2(.5f,.005f),new Vector2(.28f,-.012f),new Vector2(-.18f,-.012f),new Vector2(-.48f,-.006f)};
            var stations=new List<Vector3>();var indices=new List<int>();const int segments=8;
            for(int x=0;x<=segments;x++)foreach(var p in section)
            {
                float t=x/(float)segments*2-1;
                stations.Add(new Vector3(t*span*.5f,p.y+.022f*t*t,p.x*chord-.035f*Mathf.Abs(t)));
            }
            for(int x=0;x<segments;x++)for(int j=0;j<8;j++)
            {
                int a=x*8+j,b=x*8+(j+1)%8;indices.Add(a);indices.Add(b);indices.Add(b+8);indices.Add(a);indices.Add(b+8);indices.Add(a+8);
            }
            var mesh=new Mesh{name=key};mesh.SetVertices(stations);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
        }
        public static Mesh HelmetVisor(string key,float bottom,float top)
        {
            key="visor_"+key;if(meshes.TryGetValue(key,out var cached))return cached;
            var vertices=new List<Vector3>();var indices=new List<int>();const int segments=12;
            for(int i=0;i<=segments;i++)
            {
                float a=Mathf.Lerp(-68,68,i/(float)segments)*Mathf.Deg2Rad;
                vertices.Add(new Vector3(Mathf.Sin(a)*.231f,bottom,Mathf.Cos(a)*.266f));
                vertices.Add(new Vector3(Mathf.Sin(a)*.231f,top,Mathf.Cos(a)*.266f));
                if(i==segments)continue;int v=i*2;
                indices.Add(v);indices.Add(v+2);indices.Add(v+1);indices.Add(v+1);indices.Add(v+2);indices.Add(v+3);
            }
            var mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
        }
        public static void Combine(Transform root,float cellSize=0)
        {
            var groups=new Dictionary<(Material material,int x,int z),List<CombineInstance>>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var filter=renderer.GetComponent<MeshFilter>();var animated=renderer.GetComponentInParent<RacingAnimatedPart>();if(filter==null||(animated!=null&&animated.transform!=root))continue;
                var material=renderer.sharedMaterial;Vector3 center=renderer.bounds.center;
                var key=(material,cellSize>0?Mathf.FloorToInt(center.x/cellSize):0,cellSize>0?Mathf.FloorToInt(center.z/cellSize):0);
                if(!groups.TryGetValue(key,out var list)){list=new List<CombineInstance>();groups[key]=list;}
                list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name="Batched "+pair.Key.material.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);
                var part=Part(root,mesh.name,mesh,pair.Key.material,Vector3.zero,Quaternion.identity);part.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
                if(pair.Key.material.IsKeywordEnabled("_EMISSION"))
                    part.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }

    public sealed class RacingVehicleVisuals:MonoBehaviour
    {
        public Transform BodyVisual {get;private set;}
        public Transform[] Wheels {get;private set;}
        public Transform SteeringWheel {get;private set;}
        public Transform DriverHead {get;private set;}
        public Transform LeftArm {get;private set;}
        public Transform RightArm {get;private set;}
        RacingVehiclePhysics vehicle;
        float spin,roll,pitch,landing,trickRoll,lastForward;
        readonly Transform[] upperArms=new Transform[2],forearms=new Transform[2],gloves=new Transform[2];
        readonly Transform[] calipers=new Transform[4];
        static readonly int EmissionColorId=Shader.PropertyToID("_EmissionColor");
        MeshRenderer brakeRenderer;
        MaterialPropertyBlock brakeProperties;
        public bool BrakeLightsOn {get;private set;}
        public int BrakeLightTransitions {get;private set;}

        public static RacingVehicleVisuals Build(GameObject root,string id,string wheelId,Color paint,bool preview=false,bool isHuman=false)
        {
            var visual=root.AddComponent<RacingVehicleVisuals>();visual.vehicle=root.GetComponent<RacingVehiclePhysics>();
            bool formula=id=="formula-01",buggy=id=="buggy-01",bike=id=="sport-bike-01",rally=id=="rally-01",orbit=id=="aerial-satelite-01";
            var visualRoot=new GameObject("Visual");visualRoot.transform.SetParent(root.transform,false);
            var model=new GameObject("Body");model.transform.SetParent(visualRoot.transform,false);visual.BodyVisual=model.transform;

            bool isMetaCar = formula;
            Color bodyColor = formula ? RacingVehicleFactory.MetaNavy : (paint != Color.clear ? paint : RacingDesign.Cyan);
            var bodyMat = formula ? RacingArt.MetaBodyMaterial() : RacingArt.Material(bodyColor, .68f);
            var dark=RacingArt.Material(new Color(.045f,.065f,.085f),.56f);
            var accent=RacingArt.Material(RacingDesign.Cyan,.6f,true);
            var ivory=RacingArt.Material(RacingDesign.Ink,.3f);
            var glass=RacingArt.Material(new Color(.025f,.09f,.15f),.92f);
            var suit=RacingArt.Material(new Color(.055f,.09f,.15f),.36f);
            var helmet=RacingArt.IdentityMaterial("Blackline Cobalt Helmet",RacingVehicleFactory.MetaBlueHelmet,.18f);
            helmet.SetFloat("_Glossiness",.82f);
            helmet.SetFloat("_Metallic",.25f);
            var gloveMat=RacingArt.Material(new Color(.12f,.17f,.23f),.42f);
            var red=RacingArt.Material(RacingVehicleFactory.MetaRed,.55f,true);
            var blue=RacingArt.Material(RacingVehicleFactory.MetaBlue,.55f,true);

            float width=bike?.7f:formula?1.70f:1.65f;
            if(formula)
                RacingMetaVisuals.Body(model.transform,bodyMat,dark,glass);
            else
            {
                var hull=RacingArt.Hull("adventure_"+width,new Vector4(width*.40f,-.14f,.22f,-1.52f),new Vector4(width*.5f,-.14f,.33f,-.7f),new Vector4(width*.48f,-.13f,.29f,.7f),new Vector4(width*.37f,-.08f,.19f,1.62f));
                RacingArt.Part(model.transform,"Sculpted monocoque",hull,bodyMat,Vector3.zero,Quaternion.identity);
                RacingArt.Box(model.transform,"Lower splitter",new Vector3(0,-.08f,1.52f),new Vector3(width*1.08f,.08f,.52f),dark);
                RacingArt.Box(model.transform,"Rear diffuser",new Vector3(0,-.08f,-1.45f),new Vector3(width,.16f,.3f),dark);
                var stripe=RacingArt.Box(model.transform,"Nose precision line",new Vector3(0,.257f,1.12f),new Vector3(width*.20f,.014f,.78f),accent,.012f);
                stripe.transform.localRotation=Quaternion.Euler(4,0,0);
                var cockpitHull=RacingArt.Hull("cockpit_"+width,new Vector4(width*.32f,.18f,.38f,-.95f),new Vector4(width*.36f,.18f,.38f,-.22f),new Vector4(width*.28f,.18f,.32f,.35f));
                RacingArt.Part(model.transform,"Cockpit recess",cockpitHull,dark,Vector3.zero,Quaternion.identity);
                var windshield=RacingArt.Box(model.transform,"Aero windshield",new Vector3(0,.47f,.27f),new Vector3(width*.45f,.17f,.04f),glass,.05f);
                windshield.transform.localRotation=Quaternion.Euler(34,0,0);
            }

            // A cobalt helmet and a fitted, segmented racing suit. Arms follow the
            // actual steering-rim hand positions rather than sliding through it.
            var driverSeat = new GameObject("DriverSeat");
            driverSeat.transform.SetParent(model.transform,false);
            driverSeat.transform.localPosition = new Vector3(0, formula ? -.16f : 0, 0);

            // Suit & Torso
            RacingArt.Part(driverSeat.transform,"Fitted Driver Torso",RacingArt.Ellipsoid("blackline_pilot_torso",new Vector3(.225f,.255f,.18f)),suit,new Vector3(0,.47f,-.42f),Quaternion.Euler(12,0,0));
            RacingArt.Box(driverSeat.transform,"Chest armour",new Vector3(0,.54f,-.26f),new Vector3(.27f,.17f,.025f),ivory,.025f);
            RacingArt.Box(driverSeat.transform,"Chest cyan seam",new Vector3(0,.54f,-.243f),new Vector3(.025f,.14f,.01f),accent,.006f);
            for(int s=-1;s<=1;s+=2)
            {
                RacingArt.Part(driverSeat.transform,"Contoured shoulder pad",RacingArt.Ellipsoid("pilot_pad",new Vector3(.058f,.043f,.075f)),ivory,new Vector3(s*.205f,.61f,-.40f),Quaternion.identity);
                RacingArt.Box(driverSeat.transform,"Harness",new Vector3(s*.085f,.47f,-.259f),new Vector3(.035f,.27f,.02f),dark,.009f);
            }

            // Helmet shell, chin aero, visor, vents and crest are distinct geometry.
            var headRoot = new GameObject("DriverHead");
            headRoot.transform.SetParent(driverSeat.transform,false);
            headRoot.transform.localPosition = new Vector3(0,.88f,-.30f);
            headRoot.AddComponent<RacingAnimatedPart>();
            visual.DriverHead = headRoot.transform;

            RacingArt.Part(headRoot.transform,"Cobalt Helmet Shell",RacingArt.Ellipsoid("blackline_helmet",new Vector3(.24f,.255f,.265f)),helmet,Vector3.zero,Quaternion.identity);
            RacingArt.Box(headRoot.transform,"Crown cyan seam",new Vector3(0,.246f,-.012f),new Vector3(.036f,.018f,.24f),accent,.008f);
            RacingArt.Part(headRoot.transform,"Chin aero",RacingArt.AeroShell("helmet_chin",new Vector4(.115f,-.145f,-.075f,.11f),new Vector4(.18f,-.15f,-.075f,.20f),new Vector4(.16f,-.135f,-.085f,.285f)),helmet,Vector3.zero,Quaternion.identity);
            RacingArt.Box(headRoot.transform,"Chin intake",new Vector3(0,-.09f,.295f),new Vector3(.14f,.025f,.012f),dark,.009f);
            for(int s=-1;s<=1;s+=2)
            {
                RacingArt.Box(headRoot.transform,"Helmet vent",new Vector3(s*.215f,.105f,-.025f),new Vector3(.025f,.035f,.11f),dark,.01f);
                RacingArt.Box(headRoot.transform,"Visor hinge",new Vector3(s*.225f,.015f,.11f),new Vector3(.025f,.05f,.065f),ivory,.013f);
            }
            var visorGlass=RacingArt.Material(new Color(.015f,.04f,.08f),.95f);
            visorGlass.SetFloat("_Metallic",.5f);
            RacingArt.Part(headRoot.transform,"Polarized panoramic visor",RacingArt.HelmetVisor("panoramic",-.023f,.093f),visorGlass,Vector3.zero,Quaternion.identity);
            RacingArt.Part(headRoot.transform,"Curved visor highlight",RacingArt.HelmetVisor("visor_crest",.092f,.100f),accent,Vector3.zero,Quaternion.identity);
            RacingArt.Combine(headRoot.transform);

            // Steering Wheel & Column
            var column = RacingArt.Box(driverSeat.transform,"Steering Column",new Vector3(0,.40f,-.16f),new Vector3(.06f,.06f,.28f),dark,.02f);
            column.transform.localRotation = Quaternion.Euler(-24,0,0);

            var wheelRoot = new GameObject("SteeringWheel");
            wheelRoot.transform.SetParent(driverSeat.transform,false);
            wheelRoot.transform.localPosition = new Vector3(0,.52f,-.06f);
            wheelRoot.transform.localRotation = Quaternion.Euler(24,0,0);
            wheelRoot.AddComponent<RacingAnimatedPart>();
            visual.SteeringWheel = wheelRoot.transform;

            RacingArt.Part(wheelRoot.transform,"Wheel Rim",RacingArt.Ring(.18f,.145f,.032f,20),dark,Vector3.zero,Quaternion.Euler(0,90,0));
            RacingArt.Box(wheelRoot.transform,"Wheel telemetry hub",Vector3.zero,new Vector3(.12f,.065f,.025f),suit,.018f);
            RacingArt.Box(wheelRoot.transform,"Telemetry LEDs",new Vector3(0,.035f,.02f),new Vector3(.085f,.012f,.012f),accent,.004f);
            for(int spoke=0;spoke<3;spoke++)
            {
                float angle = spoke * 120f;
                var sp = RacingArt.Box(wheelRoot.transform,"Spoke",new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad)*.09f,Mathf.Cos(angle*Mathf.Deg2Rad)*.09f,0),new Vector3(.03f,.12f,.025f),accent,.01f);
                sp.transform.localRotation = Quaternion.Euler(0,0,-angle);
            }

            RacingArt.Combine(wheelRoot.transform);
            for(int i=0;i<2;i++)
            {
                var armRoot=new GameObject(i==0?"LeftArm":"RightArm");armRoot.transform.SetParent(driverSeat.transform,false);armRoot.AddComponent<RacingAnimatedPart>();
                if(i==0)visual.LeftArm=armRoot.transform;else visual.RightArm=armRoot.transform;
                visual.upperArms[i]=RacingArt.Box(armRoot.transform,"Suit upper arm",Vector3.zero,new Vector3(.085f,.085f,1),suit,.025f).transform;
                visual.forearms[i]=RacingArt.Box(armRoot.transform,"Suit forearm",Vector3.zero,new Vector3(.075f,.075f,1),suit,.023f).transform;
                visual.gloves[i]=RacingArt.Box(armRoot.transform,"Driving glove",Vector3.zero,new Vector3(.075f,.075f,.09f),gloveMat,.022f).transform;
            }
            visual.PoseHands();

            if(!formula && (rally||orbit))
            {
                RacingArt.Box(model.transform,"Aero wing",new Vector3(0,.64f,-1.33f),new Vector3(width*1.28f,.075f,.55f),dark);
                for(int s=-1;s<=1;s+=2)RacingArt.Box(model.transform,"Wing strut",new Vector3(s*.46f,.42f,-1.34f),new Vector3(.08f,.45f,.1f),bodyMat);
            }
            if(!formula)for(int s=-1;s<=1;s+=2)
            {
                RacingArt.Box(model.transform,"Front running light",new Vector3(s*width*.34f,.2f,1.646f),new Vector3(width*.22f,.085f,.04f),accent);
                RacingArt.Box(model.transform,"Tail lamp",new Vector3(s*width*.34f,.15f,-1.645f),new Vector3(width*.23f,.06f,.025f),RacingArt.Material(RacingDesign.Secondary,.6f,true));
                RacingArt.Box(model.transform,"Side rail",new Vector3(s*width*.51f,.05f,0),new Vector3(.07f,.16f,1.65f),dark);
                RacingArt.Box(model.transform,"Exhaust",new Vector3(s*.4f,-.025f,-1.62f),new Vector3(.15f,.16f,.3f),dark);
            }
            RacingArt.Combine(model.transform);
            if(formula)
            {
                // Update only this car's combined rear lamps; the cached material
                // remains shared by every DAPEX and the garage preview.
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.enabled&&renderer.sharedMaterial.name=="DAPEX / rear brake ribbon")
                    {visual.brakeRenderer=renderer;visual.brakeProperties=new MaterialPropertyBlock();break;}
            }

            // Whity and legacy wheels share the same collision envelope and suspension animation.
            visual.Wheels=new Transform[4];
            float radius=.38f;float tyreWidth=wheelId=="wheel-offroad"?.32f:wheelId=="wheel-racing"?.26f:.23f;
            bool whity=wheelId==MetaProgressionConfig.MetaWheelId;
            bool luxury=wheelId=="wheel-luxury-black"||wheelId=="wheel-plasma"||wheelId=="wheel-meta-cyan";
            var tyre=RacingArt.Material(new Color(.018f,.022f,.027f),.16f);
            var rim=RacingArt.Material(luxury?new Color(.04f,.05f,.065f):wheelId=="wheel-carbon"?RacingDesign.Platinum:bodyColor,.80f);
            var lip=RacingArt.Material(new Color(.18f,.22f,.27f),.85f);

            for(int i=0;i<4;i++)
            {
                var pivot=new GameObject(i==0?"WheelFL":i==1?"WheelFR":i==2?"WheelRL":"WheelRR");
                pivot.transform.SetParent(visualRoot.transform,false);
                pivot.transform.localPosition=new Vector3((i%2==0?-1:1)*.88f,-.19f,i<2?1.02f:-1.02f);
                pivot.AddComponent<RacingAnimatedPart>();
                visual.Wheels[i]=pivot.transform;
                if(whity)
                {
                    RacingMetaVisuals.Wheel(pivot.transform,tyreWidth,tyre,dark);
                    RacingArt.Combine(pivot.transform);
                    continue;
                }


                // 1. Tread (outer ring)
                RacingArt.Part(pivot.transform,"Tread",RacingArt.Ring(radius,.20f,tyreWidth),tyre,Vector3.zero,Quaternion.identity);

                // 2. Rim (inner alloy ring)
                RacingArt.Part(pivot.transform,"Rim",RacingArt.Ring(.22f,.13f,tyreWidth+.025f,16),rim,Vector3.zero,Quaternion.identity);

                if(luxury)
                {
                    RacingArt.Part(pivot.transform,"Machined black sidewall",RacingArt.Ring(radius,.225f,tyreWidth+.008f,20),tyre,Vector3.zero,Quaternion.identity);
                    RacingArt.Part(pivot.transform,"Polished rim lip",RacingArt.Ring(.245f,.232f,tyreWidth+.038f,20),lip,Vector3.zero,Quaternion.identity);
                    RacingArt.Part(pivot.transform,"Vented brake rotor",RacingArt.Ring(.178f,.06f,.035f,16),lip,Vector3.zero,Quaternion.identity);
                    var caliper=new GameObject("Fixed performance caliper");caliper.transform.SetParent(visualRoot.transform,false);
                    caliper.transform.localPosition=pivot.transform.localPosition;caliper.AddComponent<RacingAnimatedPart>();visual.calipers[i]=caliper.transform;
                    for(int s=-1;s<=1;s+=2)RacingArt.Box(caliper.transform,"Performance caliper",new Vector3(s*tyreWidth*.42f,.13f,.055f),new Vector3(.028f,.10f,.058f),red,.015f);
                    RacingArt.Combine(caliper.transform);
                }

                // 4. Center hub
                RacingArt.Part(pivot.transform,"Alloy hub",RacingArt.Ring(.060f,0,tyreWidth+.050f,12),rim,Vector3.zero,Quaternion.identity);

                // 5. Spokes
                int spokes=luxury?10:5;
                for(int spoke=0;spoke<spokes;spoke++)
                {
                    float a=spoke*Mathf.PI*2/spokes;
                    var strut=RacingArt.Box(pivot.transform,"Forged swept spoke",new Vector3(0,Mathf.Cos(a)*.13f,Mathf.Sin(a)*.13f),new Vector3(tyreWidth+.025f,.19f,luxury?.027f:.045f),rim,.009f);
                    strut.transform.localRotation=Quaternion.Euler(spoke*360f/spokes+(luxury?11:0),0,0);
                }
                RacingArt.Combine(pivot.transform);
            }
            if(visual.vehicle!=null){visual.vehicle.OnLanding+=visual.Land;visual.vehicle.OnImpact+=visual.Impact;}
            return visual;
        }
        void Land(float intensity){landing=intensity*.12f;}
        void Impact(float intensity){landing=intensity*.05f;}
        void LateUpdate()
        {
            float dt=Time.deltaTime;if(vehicle==null)return;
            bool braking=vehicle.LastCommands.Brake>.07f;
            if(brakeRenderer!=null&&braking!=BrakeLightsOn)
            {
                BrakeLightsOn=braking;BrakeLightTransitions++;
                brakeProperties.SetColor(EmissionColorId,new Color(.98f,.06f,.12f)*(braking?1.65f:.95f));
                brakeRenderer.SetPropertyBlock(brakeProperties);
            }
            float acceleration=(vehicle.ForwardSpeedMps-lastForward)/Mathf.Max(.001f,dt);lastForward=vehicle.ForwardSpeedMps;
            float targetRoll=vehicle.LastCommands.Steering*Mathf.Clamp(vehicle.SpeedKmh/100,0,1)*-4.5f;
            if(vehicle.IsDrifting)targetRoll*=1.35f;
            roll=Mathf.Lerp(roll,targetRoll,1-Mathf.Exp(-dt*10));pitch=Mathf.Lerp(pitch,Mathf.Clamp(acceleration*.13f,-3.5f,2.8f),1-Mathf.Exp(-dt*9));landing=Mathf.MoveTowards(landing,0,dt*.55f);

            // Procedural trick roll in the air
            if(vehicle.IsPerformingTrick)
            {
                trickRoll = Mathf.MoveTowards(trickRoll, 360f * Mathf.Max(1, vehicle.TrickCount), dt * 680f);
            }
            else
            {
                trickRoll = Mathf.MoveTowards(trickRoll, 0f, dt * 720f);
            }

            BodyVisual.localRotation=Quaternion.Euler(pitch,0,roll + trickRoll);
            BodyVisual.localPosition=new Vector3(0,-landing+Mathf.Sin(Time.time*33f)*.0015f,0);

            // Animate steering wheel and driver arms
            float steerInput = vehicle.LastCommands.Steering;
            if(SteeringWheel != null)
            {
                SteeringWheel.localRotation = Quaternion.Euler(24f, 0, -steerInput * 65f);
            }
            if(DriverHead != null)
            {
                DriverHead.localRotation = Quaternion.Euler(0, steerInput * 16f, -roll * 0.8f);
            }
            PoseHands();

            spin=Mathf.Repeat(spin+vehicle.ForwardSpeedMps/.38f*Mathf.Rad2Deg*dt,360);
            for(int i=0;i<Wheels.Length;i++)
            {
                Vector3 p=Wheels[i].localPosition;p.y=-.24f+vehicle.SuspensionCompression(i)*.14f;Wheels[i].localPosition=p;
                Wheels[i].localRotation=Quaternion.Euler(0,i<2?vehicle.LastCommands.Steering*23f:0,0)*Quaternion.Euler(spin,0,0);
                if(calipers[i]!=null){calipers[i].localPosition=p;calipers[i].localRotation=Quaternion.Euler(0,i<2?vehicle.LastCommands.Steering*23f:0,0);}
            }
        }
        void PoseHands()
        {
            if(SteeringWheel==null)return;
            Transform seat=SteeringWheel.parent;
            for(int i=0;i<2;i++)
            {
                float s=i==0?-1:1;
                Vector3 hand=seat.InverseTransformPoint(SteeringWheel.TransformPoint(new Vector3(s*.157f,0,.005f)));
                Vector3 shoulder=new Vector3(s*.205f,.575f,-.39f);
                Vector3 elbow=Vector3.Lerp(shoulder,hand,.5f)+new Vector3(s*.035f,-.065f,-.025f);
                Segment(upperArms[i],shoulder,elbow);Segment(forearms[i],elbow,hand);
                gloves[i].localPosition=hand;gloves[i].localRotation=SteeringWheel.localRotation;
            }
        }
        static void Segment(Transform segment,Vector3 from,Vector3 to)
        {
            segment.localPosition=(from+to)*.5f;
            segment.localRotation=Quaternion.LookRotation(to-from,Vector3.up);
            segment.localScale=new Vector3(1,1,Vector3.Distance(from,to));
        }
        void OnDestroy(){if(vehicle!=null){visualLandCleanup();}}
        void visualLandCleanup(){vehicle.OnLanding-=Land;vehicle.OnImpact-=Impact;}
    }
}
