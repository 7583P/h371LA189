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
        public static Material MetaYellowMaterial()
        {
            string key="MetaYellowBody_TrueYellow";
            if(materials.TryGetValue(key,out var mat)&&mat!=null)return mat;
            mat=new Material(Shader.Find("Standard")){name="Racing Meta Pure Yellow",color=RacingVehicleFactory.MetaYellow,enableInstancing=true};
            mat.SetFloat("_Glossiness",0.70f);
            mat.SetFloat("_Metallic",0.05f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",RacingVehicleFactory.MetaYellow*0.22f);
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
        public Transform SteeringWheel {get;private set;}
        public Transform DriverHead {get;private set;}
        public Transform LeftArm {get;private set;}
        public Transform RightArm {get;private set;}
        RacingVehiclePhysics vehicle;
        float spin,roll,pitch,landing,trickRoll;Vector3 lastVelocity;float lastForward;

        public static RacingVehicleVisuals Build(GameObject root,string id,string wheelId,Color paint,bool preview=false,bool isHuman=false)
        {
            var visual=root.AddComponent<RacingVehicleVisuals>();visual.vehicle=root.GetComponent<RacingVehiclePhysics>();
            bool formula=id=="formula-01",buggy=id=="buggy-01",bike=id=="sport-bike-01",rally=id=="rally-01",orbit=id=="aerial-satelite-01";
            var visualRoot=new GameObject("Visual");visualRoot.transform.SetParent(root.transform,false);
            var model=new GameObject("Body");model.transform.SetParent(visualRoot.transform,false);visual.BodyVisual=model.transform;

            bool isMetaCar = (id == "car-01" && (paint == Color.clear || isHuman || paint == RacingVehicleFactory.MetaYellow || (paint.r > 0.85f && paint.g > 0.80f && paint.b < 0.25f)));
            Color bodyColor = isMetaCar ? RacingVehicleFactory.MetaYellow : (paint != Color.clear ? paint : (isHuman ? RacingVehicleFactory.MetaYellow : RacingDesign.Cyan));
            var bodyMat = isMetaCar ? RacingArt.MetaYellowMaterial() : RacingArt.Material(bodyColor, .68f);
            var dark=RacingArt.Material(new Color(.045f,.065f,.085f),.42f);
            var accent=RacingArt.Material(RacingDesign.Cyan,.6f,true);
            var ivory=RacingArt.Material(RacingDesign.Ink,.3f);
            var glass=RacingArt.Material(new Color(.06f,.18f,.24f,.85f),.85f);
            var neonPurple=RacingArt.Material(RacingVehicleFactory.MetaPurpleSuit,.55f,true);
            var retroPurple=RacingArt.Material(RacingVehicleFactory.MetaPurpleHelmet,.68f);
            var purpleDriver=RacingArt.Material(new Color(.48f,.18f,.78f),.35f);
            var gloveMat=RacingArt.Material(new Color(.22f,.06f,.42f),.3f);

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
            var stripe=RacingArt.Box(model.transform,"Adventure stripe",new Vector3(0,formula?.202f:.257f,1.12f),new Vector3(formula?.12f:width*.20f,.014f,.78f),accent,.025f);stripe.transform.localRotation=Quaternion.Euler(formula?7:4,0,0);

            // Aerodynamic open/aero cockpit with visible driver
            var cockpitHull = RacingArt.Hull("cockpit_"+width,new Vector4(width*.32f,.18f,.38f,-.95f),new Vector4(width*.36f,.18f,.38f,-.22f),new Vector4(width*.28f,.18f,.32f,.35f));
            RacingArt.Part(model.transform,"Cockpit recess",cockpitHull,dark,Vector3.zero,Quaternion.identity);

            // Sleek low windshield
            var windshield = RacingArt.Box(model.transform,"Aero windshield",new Vector3(0,.54f,.32f),new Vector3(width*.58f,.24f,.04f),glass,.05f);
            windshield.transform.localRotation=Quaternion.Euler(34,0,0);

            // Driver Character: Purple character with neon purple suit & retro purple helmet
            var driverSeat = new GameObject("DriverSeat");
            driverSeat.transform.SetParent(model.transform,false);
            driverSeat.transform.localPosition = new Vector3(0, 0, 0);

            // Suit & Torso
            RacingArt.Part(driverSeat.transform,"Driver Torso",RacingArt.Ellipsoid("meta_pilot_torso",new Vector3(.24f,.28f,.24f)),neonPurple,new Vector3(0,.46f,-.42f),Quaternion.Euler(12,0,0));
            RacingArt.Box(driverSeat.transform,"Suit Neon Chest Stripe",new Vector3(0,.48f,-.30f),new Vector3(.14f,.24f,.03f),accent,.02f);

            // Retro Helmet (visible in gameplay and UI)
            var headRoot = new GameObject("DriverHead");
            headRoot.transform.SetParent(driverSeat.transform,false);
            headRoot.transform.localPosition = new Vector3(0,.83f,-.32f);
            headRoot.AddComponent<RacingAnimatedPart>();
            visual.DriverHead = headRoot.transform;

            RacingArt.Part(headRoot.transform,"Retro Helmet Shell",RacingArt.Ellipsoid("meta_retro_helmet",new Vector3(.25f,.24f,.27f)),retroPurple,Vector3.zero,Quaternion.identity);
            RacingArt.Part(headRoot.transform,"Helmet Crest Band",RacingArt.Ring(.255f,.235f,.045f,24),neonPurple,Vector3.zero,Quaternion.Euler(90,0,0));
            var visor = RacingArt.Box(headRoot.transform,"Polarized Visor",new Vector3(0,.02f,.21f),new Vector3(.34f,.13f,.045f),glass,.03f);
            visor.transform.localRotation = Quaternion.Euler(-6,0,0);

            // Steering Wheel & Column
            var column = RacingArt.Box(driverSeat.transform,"Steering Column",new Vector3(0,.40f,-.16f),new Vector3(.06f,.06f,.28f),dark,.02f);
            column.transform.localRotation = Quaternion.Euler(-24,0,0);

            var wheelRoot = new GameObject("SteeringWheel");
            wheelRoot.transform.SetParent(driverSeat.transform,false);
            wheelRoot.transform.localPosition = new Vector3(0,.52f,-.06f);
            wheelRoot.transform.localRotation = Quaternion.Euler(24,0,0);
            wheelRoot.AddComponent<RacingAnimatedPart>();
            visual.SteeringWheel = wheelRoot.transform;

            RacingArt.Part(wheelRoot.transform,"Wheel Rim",RacingArt.Ring(.18f,.145f,.032f,24),dark,Vector3.zero,Quaternion.identity);
            RacingArt.Part(wheelRoot.transform,"Wheel Center Hub",RacingArt.Ring(.065f,0,.035f,16),accent,Vector3.zero,Quaternion.identity);
            for(int spoke=0;spoke<3;spoke++)
            {
                float angle = spoke * 120f;
                var sp = RacingArt.Box(wheelRoot.transform,"Spoke",new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad)*.09f,Mathf.Cos(angle*Mathf.Deg2Rad)*.09f,0),new Vector3(.03f,.12f,.025f),accent,.01f);
                sp.transform.localRotation = Quaternion.Euler(0,0,-angle);
            }

            // Driver Arms Visibly Holding Wheel
            var leftArmRoot = new GameObject("LeftArm");
            leftArmRoot.transform.SetParent(driverSeat.transform,false);
            leftArmRoot.AddComponent<RacingAnimatedPart>();
            visual.LeftArm = leftArmRoot.transform;
            RacingArt.Box(leftArmRoot.transform,"Left Upper Arm",new Vector3(-.20f,.51f,-.32f),new Vector3(.09f,.09f,.24f),neonPurple,.03f).transform.localRotation=Quaternion.Euler(18,12,0);
            RacingArt.Box(leftArmRoot.transform,"Left Forearm",new Vector3(-.16f,.49f,-.15f),new Vector3(.08f,.08f,.20f),neonPurple,.03f).transform.localRotation=Quaternion.Euler(14,24,0);
            RacingArt.Box(leftArmRoot.transform,"Left Glove",new Vector3(-.14f,.52f,-.055f),new Vector3(.085f,.085f,.09f),gloveMat,.02f);

            var rightArmRoot = new GameObject("RightArm");
            rightArmRoot.transform.SetParent(driverSeat.transform,false);
            rightArmRoot.AddComponent<RacingAnimatedPart>();
            visual.RightArm = rightArmRoot.transform;
            RacingArt.Box(rightArmRoot.transform,"Right Upper Arm",new Vector3(.20f,.51f,-.32f),new Vector3(.09f,.09f,.24f),neonPurple,.03f).transform.localRotation=Quaternion.Euler(18,-12,0);
            RacingArt.Box(rightArmRoot.transform,"Right Forearm",new Vector3(.16f,.49f,-.15f),new Vector3(.08f,.08f,.20f),neonPurple,.03f).transform.localRotation=Quaternion.Euler(14,-24,0);
            RacingArt.Box(rightArmRoot.transform,"Right Glove",new Vector3(.14f,.52f,-.055f),new Vector3(.085f,.085f,.09f),gloveMat,.02f);

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

            // Cyan Wheels: fully cyan and visually striking (no black rubber for meta cyan wheels)
            visual.Wheels=new Transform[4];
            float radius=.38f;float tyreWidth=wheelId=="wheel-offroad"?.32f:wheelId=="wheel-racing"?.26f:.23f;
            bool isMetaCyanWheel = wheelId == "wheel-meta-cyan" || wheelId == "wheel-cyan" || wheelId == "wheel-plasma" || (bodyColor == RacingVehicleFactory.MetaYellow);
            Color rimColor = isMetaCyanWheel ? RacingVehicleFactory.MetaCyan : (wheelId=="wheel-plasma"?RacingDesign.Accent:wheelId=="wheel-carbon"?RacingDesign.Platinum:paint);

            Color cyanTreadColor = new Color(0.04f, 0.88f, 0.98f);
            Color cyanRimColor = RacingVehicleFactory.MetaCyan;
            Color cyanGlowColor = new Color(0.35f, 0.98f, 1.0f);

            var tyre = isMetaCyanWheel
                ? RacingArt.Material(cyanTreadColor, 0.65f, true)
                : RacingArt.Material(new Color(.032f, .046f, .057f), .14f);

            var rim = RacingArt.Material(isMetaCyanWheel ? cyanRimColor : rimColor, 0.82f, isMetaCyanWheel || wheelId == "wheel-plasma");
            var neonRimRing = RacingArt.Material(cyanGlowColor, 0.92f, true);

            for(int i=0;i<4;i++)
            {
                var pivot=new GameObject(i==0?"WheelFL":i==1?"WheelFR":i==2?"WheelRL":"WheelRR");
                pivot.transform.SetParent(visualRoot.transform,false);
                pivot.transform.localPosition=new Vector3((i%2==0?-1:1)*.88f,-.19f,i<2?1.02f:-1.02f);
                pivot.AddComponent<RacingAnimatedPart>();
                visual.Wheels[i]=pivot.transform;

                // 1. Tread (outer ring)
                RacingArt.Part(pivot.transform,"Tread",RacingArt.Ring(radius,.20f,tyreWidth),tyre,Vector3.zero,Quaternion.identity);

                // 2. Rim (inner alloy ring)
                RacingArt.Part(pivot.transform,"Rim",RacingArt.Ring(.22f,.13f,tyreWidth+.025f,16),rim,Vector3.zero,Quaternion.identity);

                if(isMetaCyanWheel)
                {
                    // 3. Full cyan sidewall discs covering both outer and inner faces in luminous celeste
                    RacingArt.Part(pivot.transform,"Cyan Sidewall Disc",RacingArt.Ring(radius,.21f,tyreWidth+.010f,24),neonRimRing,Vector3.zero,Quaternion.identity);
                    RacingArt.Part(pivot.transform,"Cyan Rim Flange",RacingArt.Ring(.25f,.18f,tyreWidth+.035f,20),neonRimRing,Vector3.zero,Quaternion.identity);
                }

                // 4. Center hub
                var hubMat = isMetaCyanWheel ? neonRimRing : rim;
                RacingArt.Part(pivot.transform,"Alloy hub",RacingArt.Ring(.085f,0,tyreWidth+.045f,16),hubMat,Vector3.zero,Quaternion.identity);

                // 5. Spokes
                var spokeMat = isMetaCyanWheel ? neonRimRing : rim;
                for(int spoke=0;spoke<5;spoke++)
                {
                    float a=spoke*Mathf.PI*2/5;
                    var strut=RacingArt.Box(pivot.transform,"Alloy spoke",new Vector3(0,Mathf.Cos(a)*.11f,Mathf.Sin(a)*.11f),new Vector3(tyreWidth+.025f,.18f,.045f),spokeMat,.018f);
                    strut.transform.localRotation=Quaternion.Euler(spoke*72,0,0);
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
            if(LeftArm != null && RightArm != null)
            {
                float armSway = steerInput * 0.04f;
                LeftArm.localPosition = new Vector3(0, 0, armSway);
                RightArm.localPosition = new Vector3(0, 0, -armSway);
            }

            spin=Mathf.Repeat(spin+vehicle.ForwardSpeedMps/.38f*Mathf.Rad2Deg*dt,360);
            for(int i=0;i<Wheels.Length;i++)
            {
                Vector3 p=Wheels[i].localPosition;p.y=-.24f+vehicle.SuspensionCompression(i)*.14f;Wheels[i].localPosition=p;
                Wheels[i].localRotation=Quaternion.Euler(0,i<2?vehicle.LastCommands.Steering*23f:0,0)*Quaternion.Euler(spin,0,0);
            }
        }
        void OnDestroy(){if(vehicle!=null){visualLandCleanup();}}
        void visualLandCleanup(){vehicle.OnLanding-=Land;vehicle.OnImpact-=Impact;}
    }
}
