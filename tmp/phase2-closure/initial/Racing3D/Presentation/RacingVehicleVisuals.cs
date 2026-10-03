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
            if(emission){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.7f);}
            materials[key]=mat;return mat;
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
        public static void Combine(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var filter=renderer.GetComponent<MeshFilter>();var animated=renderer.GetComponentInParent<RacingAnimatedPart>();if(filter==null||(animated!=null&&animated.transform!=root))continue;
                var material=renderer.sharedMaterial;if(!groups.TryGetValue(material,out var list)){list=new List<CombineInstance>();groups[material]=list;}
                list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name="Batched "+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);
                var part=Part(root,mesh.name,mesh,pair.Key,Vector3.zero,Quaternion.identity);part.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
            }
        }
    }
    public sealed class RacingAnimatedPart:MonoBehaviour {}
    public sealed class RacingVehicleVisuals:MonoBehaviour
    {
        public Transform BodyVisual {get;private set;}
        public Transform[] Wheels {get;private set;}
        RacingVehiclePhysics vehicle;
        float spin,roll,pitch,landing;Vector3 lastVelocity;float lastForward;
        public static RacingVehicleVisuals Build(GameObject root,string id,string wheelId,Color paint,bool preview=false)
        {
            var visual=root.AddComponent<RacingVehicleVisuals>();visual.vehicle=root.GetComponent<RacingVehiclePhysics>();
            bool formula=id=="formula-01",buggy=id=="buggy-01",bike=id=="sport-bike-01",rally=id=="rally-01",orbit=id=="aerial-satelite-01";
            var visualRoot=new GameObject("Visual");visualRoot.transform.SetParent(root.transform,false);
            var model=new GameObject("Body");model.transform.SetParent(visualRoot.transform,false);visual.BodyVisual=model.transform;
            var bodyMat=RacingArt.Material(paint,.62f);var dark=RacingArt.Material(new Color(.055f,.077f,.095f),.38f);var accent=RacingArt.Material(RacingDesign.Cyan,.55f,true);var ivory=RacingArt.Material(RacingDesign.Ink,.3f);var glass=RacingArt.Material(new Color(.075f,.23f,.29f),.8f);
            float width=bike?.7f:formula?1.28f:1.65f;
            var hull=formula?RacingArt.Hull("formula",new Vector4(.44f,-.13f,.25f,-1.55f),new Vector4(.58f,-.13f,.37f,-.75f),new Vector4(.52f,-.13f,.35f,-.05f),new Vector4(.28f,-.09f,.22f,.8f),new Vector4(.16f,-.05f,.10f,1.8f)):
                RacingArt.Hull("adventure_"+width,new Vector4(width*.40f,-.14f,.22f,-1.52f),new Vector4(width*.5f,-.14f,.33f,-.7f),new Vector4(width*.48f,-.13f,.29f,.7f),new Vector4(width*.37f,-.08f,.19f,1.62f));
            RacingArt.Part(model.transform,"Sculpted monocoque",hull,bodyMat,Vector3.zero,Quaternion.identity);
            if(formula)
            {
                for(int s=-1;s<=1;s+=2)
                {
                    RacingArt.Part(model.transform,"Swept sidepod",RacingArt.Hull("formula_pod",new Vector4(.19f,-.08f,.16f,-1.12f),new Vector4(.28f,-.09f,.21f,-.20f),new Vector4(.15f,-.08f,.12f,.55f)),bodyMat,new Vector3(s*.65f,0,0),Quaternion.identity);
                    RacingArt.Box(model.transform,"Front aero blade",new Vector3(s*.52f,-.1f,1.68f),new Vector3(.8f,.09f,.36f),ivory,.06f);
                    RacingArt.Box(model.transform,"Aero endplate",new Vector3(s*.94f,.02f,1.67f),new Vector3(.045f,.32f,.5f),bodyMat,.03f);
                }
            }
            RacingArt.Box(model.transform,"Lower splitter",new Vector3(0,-.08f,1.52f),new Vector3(width*1.08f,.08f,.52f),dark);
            RacingArt.Box(model.transform,"Rear diffuser",new Vector3(0,-.08f,-1.45f),new Vector3(width,.16f,.3f),dark);
            var stripe=RacingArt.Box(model.transform,"Adventure stripe",new Vector3(0,formula?.202f:.257f,1.12f),new Vector3(formula?.12f:width*.20f,.014f,.78f),ivory,.025f);stripe.transform.localRotation=Quaternion.Euler(formula?7:4,0,0);
            if(rally||(!formula&&!buggy&&!bike&&!orbit))
            {
                RacingArt.Part(model.transform,"Expedition cabin",RacingArt.Hull("cabin_"+width,new Vector4(width*.38f,.3f,.78f,-.98f),new Vector4(width*.42f,.3f,.85f,-.12f),new Vector4(width*.34f,.3f,.60f,.50f)),bodyMat,Vector3.zero,Quaternion.identity);
                var windscreen=RacingArt.Box(model.transform,"Raked windshield",new Vector3(0,.665f,.34f),new Vector3(width*.66f,.36f,.045f),glass,.08f);windscreen.transform.localRotation=Quaternion.Euler(23,0,0);
                for(int side=-1;side<=1;side+=2) RacingArt.Box(model.transform,"Side window",new Vector3(side*width*.428f,.65f,-.26f),new Vector3(.02f,.29f,1.07f),glass,.04f);
                if(!rally)RacingArt.Box(model.transform,"Expedition roof rack",new Vector3(0,.89f,-.22f),new Vector3(width*.8f,.08f,1.25f),dark);
            }
            else
            {
                RacingArt.Part(model.transform,"Recessed cockpit",RacingArt.Hull("cockpit",new Vector4(.27f,.21f,.39f,-.9f),new Vector4(.31f,.21f,.39f,-.28f),new Vector4(.23f,.21f,.31f,.3f)),dark,Vector3.zero,Quaternion.identity);
                RacingArt.Part(model.transform,"Driver suit",RacingArt.Ellipsoid("pilot_suit",new Vector3(.23f,.26f,.25f)),ivory,new Vector3(0,.48f,-.44f),Quaternion.identity);
                RacingArt.Part(model.transform,"Pilot helmet",RacingArt.Ellipsoid("pilot_helmet",new Vector3(.235f,.22f,.26f)),bodyMat,new Vector3(0,.80f,-.3f),Quaternion.identity);
                RacingArt.Box(model.transform,"Polarized visor",new Vector3(0,.81f,-.055f),new Vector3(.37f,.12f,.045f),glass,.04f);
                if(buggy)
                    for(int s=-1;s<=1;s+=2){RacingArt.Box(model.transform,"Roll cage",new Vector3(s*.62f,.62f,-.75f),new Vector3(.09f,1f,.1f),ivory);RacingArt.Box(model.transform,"Cage crown",new Vector3(0,1.1f,-.75f),new Vector3(1.3f,.08f,.1f),ivory);}
            }
            if(formula||rally||orbit)
            {
                RacingArt.Box(model.transform,"Aero wing",new Vector3(0,.64f,-1.33f),new Vector3(width*1.28f,.11f,.55f),dark);
                for(int s=-1;s<=1;s+=2)RacingArt.Box(model.transform,"Wing strut",new Vector3(s*.46f,.42f,-1.34f),new Vector3(.08f,.45f,.1f),bodyMat);
            }
            for(int s=-1;s<=1;s+=2)
            {
                RacingArt.Box(model.transform,"Front running light",new Vector3(s*width*.34f,.2f,1.646f),new Vector3(width*.22f,.085f,.04f),accent);
                RacingArt.Box(model.transform,"Tail lamp",new Vector3(s*width*.34f,.15f,-1.645f),new Vector3(width*.23f,.06f,.025f),RacingArt.Material(RacingDesign.Secondary,.6f,true));
                RacingArt.Box(model.transform,"Side rail",new Vector3(s*width*.51f,.05f,0),new Vector3(.07f,.16f,1.65f),dark);
                RacingArt.Box(model.transform,"Exhaust",new Vector3(s*.4f,-.025f,-1.62f),new Vector3(.15f,.16f,.3f),dark);
            }
            RacingArt.Combine(model.transform);
            visual.Wheels=new Transform[4];
            float radius=.38f;float tyreWidth=wheelId=="wheel-offroad"?.32f:wheelId=="wheel-racing"?.26f:.23f;
            var tyre=RacingArt.Material(new Color(.032f,.046f,.057f),.14f);var rim=RacingArt.Material(wheelId=="wheel-plasma"?RacingDesign.Accent:wheelId=="wheel-carbon"?RacingDesign.Platinum:paint,.66f,wheelId=="wheel-plasma");
            for(int i=0;i<4;i++)
            {
                var pivot=new GameObject(i==0?"WheelFL":i==1?"WheelFR":i==2?"WheelRL":"WheelRR");pivot.transform.SetParent(visualRoot.transform,false);pivot.transform.localPosition=new Vector3((i%2==0?-1:1)*.88f,-.19f,i<2?1.02f:-1.02f);pivot.AddComponent<RacingAnimatedPart>();
                visual.Wheels[i]=pivot.transform;
                RacingArt.Part(pivot.transform,"Tread",RacingArt.Ring(radius,.20f,tyreWidth),tyre,Vector3.zero,Quaternion.identity);
                RacingArt.Part(pivot.transform,"Rim",RacingArt.Ring(.22f,.13f,tyreWidth+.025f,12),rim,Vector3.zero,Quaternion.identity);
                RacingArt.Part(pivot.transform,"Alloy hub",RacingArt.Ring(.085f,0,tyreWidth+.045f,16),ivory,Vector3.zero,Quaternion.identity);
                for(int spoke=0;spoke<5;spoke++)
                {
                    float a=spoke*Mathf.PI*2/5;var strut=RacingArt.Box(pivot.transform,"Alloy spoke",new Vector3(0,Mathf.Cos(a)*.11f,Mathf.Sin(a)*.11f),new Vector3(tyreWidth+.025f,.18f,.045f),rim,.018f);strut.transform.localRotation=Quaternion.Euler(spoke*72,0,0);
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
            float acceleration=(vehicle.ForwardSpeedMps-lastForward)/Mathf.Max(.001f,dt);lastForward=vehicle.ForwardSpeedMps;
            float targetRoll=vehicle.LastCommands.Steering*Mathf.Clamp(vehicle.SpeedKmh/100,0,1)*-4.5f;
            if(vehicle.IsDrifting)targetRoll*=1.35f;
            roll=Mathf.Lerp(roll,targetRoll,1-Mathf.Exp(-dt*10));pitch=Mathf.Lerp(pitch,Mathf.Clamp(acceleration*.13f,-3.5f,2.8f),1-Mathf.Exp(-dt*9));landing=Mathf.MoveTowards(landing,0,dt*.55f);
            BodyVisual.localRotation=Quaternion.Euler(pitch,0,roll);BodyVisual.localPosition=new Vector3(0,-landing+Mathf.Sin(Time.time*33f)*.0015f,0);
            spin=Mathf.Repeat(spin+vehicle.ForwardSpeedMps/.38f*Mathf.Rad2Deg*dt,360);
            for(int i=0;i<Wheels.Length;i++)
            {
                Vector3 p=Wheels[i].localPosition;p.y=-.24f+vehicle.SuspensionCompression(i)*.14f;Wheels[i].localPosition=p;
                Wheels[i].localRotation=Quaternion.Euler(0,i<2?vehicle.LastCommands.Steering*23f:0,0)*Quaternion.Euler(spin,0,0);
            }
        }
        void OnDestroy(){if(vehicle!=null){vehicle.OnLanding-=Land;vehicle.OnImpact-=Impact;}}
    }
}
