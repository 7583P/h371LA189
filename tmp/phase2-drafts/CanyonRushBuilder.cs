using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    [DefaultExecutionOrder(-80)]
    public sealed class CanyonRushBuilder:MonoBehaviour
    {
        public Material ReusedRockMaterial,ReusedGroundMaterial;
        public TrackSplineGraph Graph{get;private set;}
        public StartingGrid Grid{get;private set;}
        public const int Steps=8;
        bool built;
        void Awake(){Build();}
        public void Build()
        {
            if(built)return;built=true;
            gameObject.AddComponent<RacingTrackInfo>();
            Vector3[] control={
                new Vector3(0,6,0),new Vector3(0,6,105),new Vector3(12,7,200),new Vector3(58,8,270),
                new Vector3(145,9,305),new Vector3(240,11,277),new Vector3(292,13,220),new Vector3(304,19,150),
                new Vector3(286,11,68),new Vector3(239,8,4),new Vector3(225,7,-83),new Vector3(262,6,-157),
                new Vector3(228,6,-240),new Vector3(137,7,-265),new Vector3(53,9,-222),new Vector3(-32,11,-240),
                new Vector3(-103,10,-169),new Vector3(-83,8,-83),new Vector3(-24,6,-55),new Vector3(0,6,-40)
            };
            var positions=RacingTrackGeometry.SmoothLoop(control,Steps);var nodes=new TrackNode[positions.Length];
            for(int i=0;i<nodes.Length;i++)
            {
                Vector3 forward=(positions[(i+1)%positions.Length]-positions[(i+positions.Length-1)%positions.Length]).normalized;
                Vector3 ahead=(positions[(i+3)%positions.Length]-positions[i]).normalized;
                Vector3 behind=(positions[i]-positions[(i+positions.Length-3)%positions.Length]).normalized;
                float corner=Vector3.SignedAngle(behind,ahead,Vector3.up);float target=150-Mathf.Clamp(Mathf.Abs(corner)*1.3f,0,78);
                float width=20+Mathf.Sin(i*.075f)*2;
                nodes[i]=new TrackNode{Position=positions[i],Forward=forward,Right=Vector3.Cross(Vector3.up,forward).normalized,Up=Vector3.up,TrackWidth=width,SafeLineOffset=0,OptimalLineOffset=Mathf.Sign(corner)*Mathf.Clamp01(Mathf.Abs(corner)/40)*.28f,TargetSpeedKmh=target,IsDriftZone=Mathf.Abs(corner)>15};
            }
            var shortcut=new TrackShortcut{shortcutId="ridge_cut",displayName="RIDGE CUT",entryNodeIndex=11*Steps,exitNodeIndex=13*Steps,requiresBoost=true,minSkillRequired=.5f,riskLevel=.32f,pathPoints=new[]{control[11],new Vector3(207,6,-207),control[13]}};
            var graphRoot=new GameObject("Canyon spline graph");graphRoot.transform.SetParent(transform,false);Graph=graphRoot.AddComponent<TrackSplineGraph>();Graph.InitializeFromNodes(nodes,new[]{shortcut});
            var gridRoot=new GameObject("12 slot starting grid");gridRoot.transform.SetParent(transform,false);gridRoot.transform.position=nodes[0].Position;gridRoot.transform.rotation=Quaternion.LookRotation(nodes[0].Forward,Vector3.up);Grid=gridRoot.AddComponent<StartingGrid>();Grid.Initialize(Graph);
            Material road=RacingArt.Material(new Color(.23f,.30f,.33f),.32f),sand=ReusedGroundMaterial!=null?ReusedGroundMaterial:RacingArt.Material(new Color(.69f,.46f,.28f),.1f),edge=RacingArt.Material(new Color(.97f,.79f,.48f),.2f),metal=RacingArt.Material(new Color(.055f,.15f,.2f),.6f),cyan=RacingArt.Material(RacingDesign.Cyan,.4f,true);
            // Bridge crest offers a short physical leap. The navigation spline remains continuous.
            int gapStart=7*Steps+2,gapEnd=7*Steps+3;
            RacingTrackGeometry.Ribbon(transform,"Adventure asphalt",nodes,-10,10,road,RacingSurfaceKind.Road,true,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Left shoulder",nodes,-14,-10,sand,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Right shoulder",nodes,10,14,sand,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Left mineral edge",nodes,-10,-9.75f,edge,RacingSurfaceKind.Road,false,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Right mineral edge",nodes,9.75f,10,edge,RacingSurfaceKind.Road,false,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Canyon left slope",nodes,-14,-42,sand,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,14);
            RacingTrackGeometry.Ribbon(transform,"Canyon right slope",nodes,14,42,sand,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,14);
            var scenery=new GameObject("Batched canyon scenery");scenery.transform.SetParent(transform,false);
            Material rock=ReusedRockMaterial!=null?ReusedRockMaterial:RacingArt.Material(new Color(.76f,.37f,.23f),.1f);
            var rng=new DeterministicRng(90173);
            for(int i=0;i<64;i++)
            {
                TrackNode n=nodes[(i*7)%nodes.Length];int side=(i&1)==0?-1:1;
                Vector3 point=n.Position+n.Right*side*rng.Range(40,100)-Vector3.up*rng.Range(10,17);
                var stone=RacingArt.Part(scenery.transform,"Layered sandstone mesa",Rock(rng.Range(9,26),rng.Range(16,52),rng.NextUInt()),i%3==0?rock:RacingArt.Material(i%3==1?new Color(.85f,.49f,.30f):new Color(.61f,.29f,.21f),.1f),point,Quaternion.Euler(0,rng.Range(0,180),0));stone.AddComponent<RacingGeneratedMesh>().Mesh=stone.GetComponent<MeshFilter>().sharedMesh;
            }
            for(int i=0;i<18;i++)
            {
                Vector3 point=new Vector3(rng.Range(-300,450),-15,rng.Range(-450,450));
                var mountain=RacingArt.Part(scenery.transform,"Distant desert ridge",Rock(rng.Range(40,90),rng.Range(70,130),rng.NextUInt()),RacingArt.Material(new Color(.55f,.39f,.33f),.08f),point,Quaternion.identity);mountain.AddComponent<RacingGeneratedMesh>().Mesh=mountain.GetComponent<MeshFilter>().sharedMesh;
            }
            var ground=RacingArt.Box(scenery.transform,"Canyon basin",new Vector3(90,-16,0),new Vector3(1200,1,1200),sand,.5f);ground.AddComponent<BoxCollider>().size=new Vector3(1200,1,1200);ground.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Dirt;
            BuildBarriers(nodes,scenery.transform,metal,edge,cyan);
            BuildGate(nodes[0],scenery.transform,metal,cyan);
            BuildBridge(nodes,scenery.transform,metal,edge);
            BuildTunnel(nodes,scenery.transform,rock,cyan);
            BuildShortcut(shortcut,scenery.transform,sand,cyan);
            BuildVegetation(nodes,scenery.transform,rng);
            BuildWaterfall(nodes[3].Position+nodes[3].Right*-58,scenery.transform);
            int[] checkpoints={0,3*Steps,6*Steps,9*Steps,14*Steps,17*Steps};
            for(int i=0;i<checkpoints.Length;i++)
            {
                var n=nodes[checkpoints[i]];var cp=new GameObject(i==0?"Canyon finish gate":"Canyon checkpoint "+i);cp.transform.SetParent(transform,false);cp.transform.position=n.Position+Vector3.up*2;cp.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                var trigger=cp.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(n.TrackWidth+10,9,8);cp.AddComponent<CheckpointTrigger3D>().Configure(i,i==0);
            }
            foreach(int station in new[]{1*Steps,5*Steps,10*Steps,15*Steps})for(int lane=-1;lane<=1;lane++)ItemBox.Create(transform,nodes[station].Position+nodes[station].Right*lane*4.5f+Vector3.up*1.1f);
            for(int row=0;row<6;row++)
            {
                var n=Graph.SampleDistance(Graph.TotalLength-8-row*7);
                for(int side=-1;side<=1;side+=2){var stripe=RacingArt.Box(scenery.transform,"Grid mark",n.Position+n.Right*side*1.6f+Vector3.up*.025f,new Vector3(1.5f,.03f,.14f),edge,.02f);stripe.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);}
            }
            RacingArt.Combine(scenery.transform);
            ApplyLighting();
            Debug.Log($"CANYON_RUSH_BUILT nodes={nodes.Length} length={Graph.TotalLength:F1} checkpoints={checkpoints.Length} item_pods=12 shortcut=RIDGE_CUT");
        }
        static Mesh Rock(float radius,float height,uint seed)
        {
            int sides=9,layers=5;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int layer=0;layer<layers-1;layer++)for(int side=0;side<sides;side++)
            {
                int next=(side+1)%sides;Vector3 a=Point(layer,side),b=Point(layer+1,side),c=Point(layer+1,next),d=Point(layer,next);
                int index=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);triangles.Add(index);triangles.Add(index+1);triangles.Add(index+2);triangles.Add(index);triangles.Add(index+2);triangles.Add(index+3);
            }
            int top=vertices.Count;vertices.Add(new Vector3(0,height,0));for(int i=0;i<sides;i++)vertices.Add(Point(layers-1,i));for(int i=0;i<sides;i++){triangles.Add(top);triangles.Add(top+(i+1)%sides+1);triangles.Add(top+i+1);}
            var mesh=new Mesh{name="Original stratified sandstone"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            Vector3 Point(int layer,int side)
            {
                float t=layer/(float)(layers-1),angle=side*Mathf.PI*2/sides;
                float irregular=.88f+Mathf.Sin(side*12.31f+seed%137)*.11f;
                float taper=1-t*.42f+(layer==2?.09f:0);
                return new Vector3(Mathf.Cos(angle)*radius*taper*irregular+Mathf.Sin(t*5+seed%7)*radius*.06f,t*height,Mathf.Sin(angle)*radius*.75f*taper*irregular);
            }
        }
        void BuildBarriers(TrackNode[] nodes,Transform parent,Material metal,Material edge,Material cyan)
        {
            for(int i=0;i<nodes.Length;i++)
            {
                var n=nodes[i];var next=nodes[(i+1)%nodes.Length];float length=Vector3.Distance(n.Position,next.Position);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 a=n.Position+n.Right*side*(n.TrackWidth*.5f+.7f),b=next.Position+next.Right*side*(next.TrackWidth*.5f+.7f);
                    var rail=RacingArt.Box(parent,"Canyon guard rail",(a+b)*.5f+Vector3.up*.62f,new Vector3(.25f,1.05f,length+.15f),metal,.05f);rail.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);rail.AddComponent<BoxCollider>().size=new Vector3(.25f,1.05f,length+.15f);
                    if(i%3==0)RacingArt.Box(parent,"Road beacon",a+Vector3.up*.8f,new Vector3(.3f,.10f,.9f),cyan,.04f);
                }
                if(i%8==0)
                {
                    var flag=RacingArt.Box(parent,"Expedition flag",n.Position+n.Right*(n.TrackWidth*.5f+3)+Vector3.up*3,new Vector3(.04f,1.3f,1.9f),edge,.02f);flag.AddComponent<RacingEnvironmentMotion>();
                    RacingArt.Box(parent,"Flag mast",n.Position+n.Right*(n.TrackWidth*.5f+3)+Vector3.up*1.6f,new Vector3(.08f,3.5f,.08f),metal,.03f);
                }
            }
        }
        void BuildGate(TrackNode node,Transform parent,Material metal,Material cyan)
        {
            var gate=new GameObject("Split summit start arch");gate.transform.SetParent(parent,false);gate.transform.position=node.Position;gate.transform.rotation=Quaternion.LookRotation(node.Forward,Vector3.up);
            for(int s=-1;s<=1;s+=2){RacingArt.Box(gate.transform,"Tower",new Vector3(s*12,5,0),new Vector3(1.2f,10,1.2f),metal,.3f);var span=RacingArt.Box(gate.transform,"Mountain span",new Vector3(s*5.7f,10.7f,0),new Vector3(13,.75f,1.2f),cyan,.2f);span.transform.localRotation=Quaternion.Euler(0,0,-s*14);}
            Sign(gate.transform,"HILL ADVENTURER",new Vector3(0,8.2f,-.65f),.65f,RacingDesign.Ink);
        }
        void BuildBridge(TrackNode[] nodes,Transform parent,Material metal,Material edge)
        {
            for(int i=6*Steps;i<8*Steps;i+=3)
            {
                var n=nodes[i];for(int side=-1;side<=1;side+=2){RacingArt.Box(parent,"Suspension bridge pier",n.Position+n.Right*side*12-Vector3.up*6,new Vector3(1.1f,15,1.1f),metal,.2f);var pillar=RacingArt.Box(parent,"Bridge pylon",n.Position+n.Right*side*12+Vector3.up*4,new Vector3(.5f,8,.55f),edge,.1f);pillar.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);}
                var cross=RacingArt.Box(parent,"Bridge girder",n.Position-Vector3.up*.65f,new Vector3(25,.7f,1.1f),metal,.1f);cross.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
            }
        }
        void BuildTunnel(TrackNode[] nodes,Transform parent,Material rock,Material cyan)
        {
            for(int i=3*Steps;i<4*Steps+4;i+=2)
            {
                var n=nodes[i];var hoop=new GameObject("Sunstone tunnel portal");hoop.transform.SetParent(parent,false);hoop.transform.position=n.Position;hoop.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                for(int s=-1;s<=1;s+=2){var wall=RacingArt.Box(hoop.transform,"Tunnel flank",new Vector3(s*13,4,0),new Vector3(4,8,6),rock,.6f);wall.AddComponent<BoxCollider>().size=new Vector3(4,8,6);RacingArt.Box(hoop.transform,"Tunnel guidance",new Vector3(s*10.8f,2,0),new Vector3(.04f,.12f,4),cyan,.02f);}
                var ceiling=RacingArt.Box(hoop.transform,"Tunnel roof",new Vector3(0,8.5f,0),new Vector3(28,2.1f,6),rock,.5f);ceiling.AddComponent<BoxCollider>().size=new Vector3(28,2.1f,6);
            }
        }
        void BuildShortcut(TrackShortcut shortcut,Transform parent,Material sand,Material cyan)
        {
            for(int i=0;i<shortcut.pathPoints.Length-1;i++)
            {
                Vector3 a=shortcut.pathPoints[i],b=shortcut.pathPoints[i+1];var slab=RacingArt.Box(parent,"Ridge cut dirt shelf",(a+b)*.5f-Vector3.up*.15f,new Vector3(8,.3f,Vector3.Distance(a,b)+1),sand,.15f);slab.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);slab.AddComponent<BoxCollider>().size=new Vector3(8,.3f,Vector3.Distance(a,b)+1);slab.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Dirt;
            }
            Sign(parent,"RIDGE CUT",shortcut.pathPoints[0]+Vector3.up*3,.7f,RacingDesign.Cyan);
            RacingArt.Box(parent,"Shortcut signal",shortcut.pathPoints[0]+Vector3.up*1.9f,new Vector3(6,.14f,.14f),cyan);
        }
        void BuildVegetation(TrackNode[] nodes,Transform parent,DeterministicRng rng)
        {
            for(int i=0;i<52;i++)
            {
                var n=nodes[(i*11)%nodes.Length];Vector3 p=n.Position+n.Right*(i%2==0?-1:1)*(n.TrackWidth*.5f+6);
                var green=RacingArt.Material(i%2==0?new Color(.25f,.39f,.23f):new Color(.43f,.49f,.25f),.1f);
                RacingArt.Box(parent,"Canyon yucca",p+Vector3.up*.45f,new Vector3(.2f,.9f,.2f),green,.08f);
                for(int branch=0;branch<3;branch++){var leaf=RacingArt.Box(parent,"Yucca blade",p+Vector3.up*.7f,new Vector3(.10f,.10f,1.3f),green,.04f);leaf.transform.rotation=Quaternion.Euler(-27,branch*120+rng.Range(0,20),0);}
            }
        }
        void BuildWaterfall(Vector3 point,Transform parent)
        {
            var water=RacingArt.Box(parent,"Mineral waterfall",point+Vector3.up*8,new Vector3(5,22,.25f),RacingArt.Material(new Color(.23f,.70f,.78f),.7f,true),.1f);water.AddComponent<RacingEnvironmentMotion>().Water=true;
            RacingArt.Box(parent,"Turquoise canyon pool",point-Vector3.up*3,new Vector3(34,.12f,15),RacingArt.Material(new Color(.13f,.44f,.51f),.8f),.5f);
        }
        static void Sign(Transform parent,string text,Vector3 point,float scale,Color color)
        {
            var sign=new GameObject(text);sign.transform.SetParent(parent,false);sign.transform.localPosition=point;sign.transform.localScale=Vector3.one*scale;var label=sign.AddComponent<TextMesh>();label.text=text;label.fontSize=52;label.characterSize=.11f;label.anchor=TextAnchor.MiddleCenter;label.color=color;label.font=RacingDesign.Font;sign.GetComponent<MeshRenderer>().sharedMaterial=RacingDesign.Font.material;
        }
        public static void ApplyLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.50f,.70f,.77f);RenderSettings.ambientEquatorColor=new Color(.51f,.43f,.34f);RenderSettings.ambientGroundColor=new Color(.25f,.20f,.17f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.00165f;RenderSettings.fogColor=new Color(.80f,.70f,.55f);
            var sun=FindFirstObjectByType<Light>();if(sun==null)sun=new GameObject("Canyon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.color=new Color(1,.85f,.69f);sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
            var shader=Shader.Find("Skybox/Procedural");if(shader!=null){var sky=new Material(shader){name="Canyon atmospheric sky"};sky.SetColor("_SkyTint",new Color(.38f,.62f,.74f));sky.SetColor("_GroundColor",new Color(.65f,.42f,.28f));sky.SetFloat("_Exposure",.95f);sky.SetFloat("_AtmosphereThickness",.8f);RenderSettings.skybox=sky;}
        }
    }
    public sealed class RacingEnvironmentMotion:MonoBehaviour
    {
        public bool Water;Quaternion rotation;Vector3 scale;
        void Awake(){rotation=transform.localRotation;scale=transform.localScale;gameObject.AddComponent<RacingAnimatedPart>();}
        void Update(){if(Water)transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(Time.time*2.7f)*.015f),scale.y,scale.z);else transform.localRotation=rotation*Quaternion.Euler(Mathf.Sin(Time.time*2.1f+transform.position.x)*3,0,Mathf.Sin(Time.time*3.3f+transform.position.z)*4);}
    }
}
