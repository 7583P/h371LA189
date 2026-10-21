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
            gameObject.AddComponent<RacingEnvironmentScope>().Capture();
            var info=gameObject.AddComponent<RacingTrackInfo>();info.Theme="Starry neon sky-road / open edges / hidden movement routes";
            TrackNode[] nodes=NightCityCircuit.CreateNodes();
            TrackShortcut[] routes=NightCityCircuit.CreateShortcuts(nodes);
            int[] checkpoints=NightCityCircuit.Checkpoints;
            var graphRoot=new GameObject("Night City Grand Circuit graph");graphRoot.transform.SetParent(transform,false);
            Graph=graphRoot.AddComponent<TrackSplineGraph>();Graph.InitializeFromNodes(nodes,routes,checkpoints);
            var gridRoot=new GameObject("12 slot starting grid");gridRoot.transform.SetParent(transform,false);
            gridRoot.transform.SetPositionAndRotation(nodes[0].Position,Quaternion.LookRotation(nodes[0].Forward,Vector3.up));
            Grid=gridRoot.AddComponent<StartingGrid>();Grid.Initialize(Graph);

            Material road=RacingCityArt.Asphalt();
            Material shoulder=RacingArt.Material(new Color(.028f,.040f,.064f),.25f);
            Material white=RacingArt.IdentityMaterial("Night City / titanium edge",RacingVehicleFactory.MetaWhite,.45f);
            Material cyan=RacingArt.Material(RacingDesign.Cyan,.5f,true);
            Material gold=RacingArt.Material(new Color(.84f,.68f,.37f),.45f,true);
            Material indigo=RacingArt.Material(new Color(.38f,.38f,.68f),.50f,true);
            Material graphite=RacingArt.Material(new Color(.040f,.055f,.088f),.65f);

            // Relative offsets use a 20 m reference. The road and navigation now have identical widths.
            // The main route remains continuous: only the optional Skyline Link has an intentional gap.
            var mainRoad=RacingTrackGeometry.Ribbon(transform,"Grand Circuit asphalt",nodes,-10,10,road,RacingSurfaceKind.Road,true,true);
            // Only the main-road renderer changes; undisclosed shortcut decks
            // retain their materials, collision, topology and navigation.
            // Review proposal is explicit Editor/Development opt-in. Production
            // keeps the existing presentation until the owner approves the design.
            var prismShader=Resources.Load<Shader>(RacingRainbowScenery.Requested?"RacingRainbowRoad":"RacingPrismaticRoad");
            if(prismShader!=null){
                var luminousRoad=new Material(prismShader){name="Original prismatic energy road",mainTexture=road.mainTexture};
                GetComponent<RacingEnvironmentScope>().Own(luminousRoad);mainRoad.GetComponent<MeshRenderer>().sharedMaterial=luminousRoad;
            }
            RacingTrackGeometry.Ribbon(transform,"Left titanium curb",nodes,-10.35f,-10,white,RacingSurfaceKind.Road,false,true);
            RacingTrackGeometry.Ribbon(transform,"Right titanium curb",nodes,10,10.35f,white,RacingSurfaceKind.Road,false,true);
            RacingTrackGeometry.Ribbon(transform,"Left recovery shoulder",nodes,-12,-10.35f,shoulder,RacingSurfaceKind.Dirt,true,true,clearRoutes:routes);
            RacingTrackGeometry.Ribbon(transform,"Right recovery shoulder",nodes,10.35f,12,shoulder,RacingSurfaceKind.Dirt,true,true,clearRoutes:routes);

            var scenery=new GameObject("Batched premium city scenery");scenery.transform.SetParent(transform,false);
            if(RacingRainbowScenery.Requested)RacingRainbowScenery.Build(transform,nodes,routes);
            else RacingSkyTrack.Build(transform,nodes,routes);
            BuildEdgeWarnings(nodes,scenery.transform,cyan,routes);
            BuildGate(nodes[0],scenery.transform,graphite,white,gold);
            BuildShortcuts(routes,scenery.transform,road,cyan,indigo,white);
            BuildRaceGantries(nodes,scenery.transform,graphite,white,gold);
            BuildApexKerbs(nodes,scenery.transform,white,indigo,graphite);

            Graph.MainJumpNodeIndex=172;
            var launch=nodes[Graph.MainJumpNodeIndex];
            var kicker=new GameObject("Grand Avenue hop / continuous landing road");kicker.transform.SetParent(transform,false);
            kicker.transform.SetPositionAndRotation(launch.Position,Quaternion.LookRotation(launch.Forward,Vector3.up));
            var launchTrigger=kicker.AddComponent<BoxCollider>();launchTrigger.isTrigger=true;launchTrigger.center=Vector3.up;
            launchTrigger.size=new Vector3(launch.TrackWidth,2.5f,2);
            kicker.AddComponent<RacingJumpPad>().VerticalImpulse=3.8f;
            RacingArt.Box(kicker.transform,"White launch marker",new Vector3(0,.06f,0),new Vector3(launch.TrackWidth,.05f,.8f),white,.01f);

            for(int i=0;i<checkpoints.Length;i++)
            {
                var n=nodes[checkpoints[i]];var cp=new GameObject(i==0?"Finish gate":"Checkpoint "+i);cp.transform.SetParent(transform,false);
                cp.transform.SetPositionAndRotation(n.Position+Vector3.up*2,Quaternion.LookRotation(n.Forward,Vector3.up));
                var trigger=cp.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(n.TrackWidth+10,9,8);
                cp.AddComponent<CheckpointTrigger3D>().Configure(i,i==0);
            }
            foreach(int station in new[]{16,56,100,152})for(int lane=-1;lane<=1;lane++)
                ItemBox.Create(transform,nodes[station].Position+nodes[station].Right*lane*5+Vector3.up*1.1f);
            for(int row=0;row<6;row++)
            {
                var n=Graph.SampleDistance(Graph.TotalLength-8-row*7);
                for(int side=-1;side<=1;side+=2)
                {
                    var stripe=RacingArt.Box(scenery.transform,"Grid mark",n.Position+n.Right*side*1.6f+Vector3.up*.03f,new Vector3(1.5f,.025f,.14f),white,.01f);
                    stripe.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
            }
            BuildSectionGuidance(nodes,scenery.transform,graphite);
            BuildNeonPolish(nodes,routes,scenery.transform,white);
            RacingArt.Combine(scenery.transform,110);
            ApplyLighting();
            Debug.Log($"NIGHT_CITY_GRAND_CIRCUIT_BUILT nodes={nodes.Length} length={Graph.TotalLength:F1} checkpoints={checkpoints.Length} shortcuts={routes.Length} width=25-30m");
        }

        static float SectorWeight(int node,int start,int end)
            => node<=start||node>=end?0:Mathf.Sin(Mathf.PI*(node-start)/(end-start));

        static void BuildCitySkyscrapers(TrackNode[] nodes,TrackShortcut[] routes,Transform parent,DeterministicRng rng,Material dark,Material cyan,Material magenta,Material yellow)
        {
            Material warm=RacingArt.Material(new Color(.82f,.53f,.20f),.2f,true);
            Material teal=RacingArt.Material(new Color(.08f,.65f,.49f),.25f,true);
            Material violet=RacingArt.Material(new Color(.48f,.20f,.75f),.3f,true);
            Material[] neonWindows=new[]{cyan,warm,magenta,cyan};

            // Tier 1: Trackside commercial buildings (48 towers)
            for(int i=0;i<36;i++)
            {
                TrackNode n=nodes[(i*7)%nodes.Length];int side=(i&1)==0?-1:1;
                float dist=rng.Range(48,82);
                Vector3 basePos=n.Position+n.Right*side*dist-Vector3.up*rng.Range(2,6);
                float width=rng.Range(16,32);
                float depth=rng.Range(16,30);
                float height=rng.Range(42,95);
                if (!ClearSceneryPosition(basePos, Mathf.Sqrt(width*width+depth*depth)*.5f, nodes, routes)) continue;

                var towerRoot=new GameObject("Skyscraper_"+i);
                towerRoot.transform.SetParent(parent,false);
                towerRoot.transform.position=basePos;
                towerRoot.transform.rotation=Quaternion.Euler(0,rng.Range(0,360),0);

                var block=RacingArt.Box(towerRoot.transform,"Tower core",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),dark,.2f);
                block.AddComponent<BoxCollider>().size=new Vector3(width,height,depth);

                Material winMat=neonWindows[i%neonWindows.Length];
                if(i%3==0)RacingArt.Box(towerRoot.transform,"Stepped crown",new Vector3(0,height+4,0),new Vector3(width*.62f,8,depth*.62f),dark,.5f);
                RacingCityArt.Facade(towerRoot.transform,width,height,depth,90173+i*127,winMat,warm);
                if(i%4==0)for(int facadeSide=-1;facadeSide<=1;facadeSide+=2)
                    RacingArt.Box(towerRoot.transform,"Facade neon spine",new Vector3(facadeSide*width*.48f,height*.5f,depth*.506f),new Vector3(.14f,height*.88f,.06f),winMat,.02f);
                RacingArt.Box(towerRoot.transform,"Entrance podium",new Vector3(0,2,0),new Vector3(width*1.04f,4,depth*1.04f),dark,.6f);

                RacingArt.Box(towerRoot.transform,"Roof Spire",new Vector3(0,height+6,0),new Vector3(1.2f,12,1.2f),winMat,.1f);
                RacingArt.Box(towerRoot.transform,"Crown Beacon",new Vector3(0,height+12,0),new Vector3(2.5f,2.5f,2.5f),winMat,.1f);
            }

            // Tier 2: Mid-distance corporate towers (32 towers)
            for(int i=0;i<24;i++)
            {
                TrackNode n=nodes[(i*11)%nodes.Length];int side=(i&1)==0?1:-1;
                float dist=rng.Range(85,170);
                Vector3 basePos=n.Position+n.Right*side*dist-Vector3.up*rng.Range(4,12);
                float width=rng.Range(28,48);
                float depth=rng.Range(28,46);
                float height=rng.Range(90,165);
                if (!ClearSceneryPosition(basePos, Mathf.Sqrt(width*width+depth*depth)*.5f, nodes, routes)) continue;

                var midRoot=new GameObject("MidTower_"+i);
                midRoot.transform.SetParent(parent,false);
                midRoot.transform.position=basePos;
                midRoot.transform.rotation=Quaternion.Euler(0,rng.Range(0,360),0);

                RacingArt.Box(midRoot.transform,"Mid Core",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),dark,.3f);
                Material winMat=neonWindows[(i+1)%neonWindows.Length];
                RacingCityArt.Facade(midRoot.transform,width,height,depth,104729+i*113,winMat,warm);
                RacingArt.Box(midRoot.transform,"Stepped tower penthouse",new Vector3(0,height+5,0),new Vector3(width*.64f,10,depth*.70f),dark,.8f);
                RacingArt.Box(midRoot.transform,"Roof Pylon",new Vector3(0,height+10,0),new Vector3(1.8f,20,1.8f),cyan,.1f);
            }

            // Tier 3: Distant mega-monolith silhouettes (14 grand spires)
            for(int i=0;i<14;i++)
            {
                float angle=i*(Mathf.PI*2f/14f);
                float dist=rng.Range(280,420);
                Vector3 basePos=new Vector3(Mathf.Cos(angle)*dist+90,-10,Mathf.Sin(angle)*dist);
                float width=rng.Range(55,95);
                float depth=rng.Range(50,85);
                float height=rng.Range(180,275);
                if (!ClearSceneryPosition(basePos, Mathf.Sqrt(width*width+depth*depth)*.5f, nodes, routes)) continue;

                var megaRoot=new GameObject("MegaMonolith_"+i);
                megaRoot.transform.SetParent(parent,false);
                megaRoot.transform.position=basePos;
                megaRoot.transform.rotation=Quaternion.Euler(0,rng.Range(0,360),0);

                RacingArt.Box(megaRoot.transform,"Monolith Core",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),dark,.4f);
                Material glow=(i%2==0)?cyan:magenta;
                RacingCityArt.Facade(megaRoot.transform,width,height,depth,209759+i*89,glow,warm);
                RacingArt.Box(megaRoot.transform,"Skyline Spine",new Vector3(0,height*.5f,depth*.52f),new Vector3(2.5f,height*.85f,.3f),glow,.1f);
                RacingArt.Box(megaRoot.transform,"Crown Beacon",new Vector3(0,height+8,0),new Vector3(4f,16f,4f),glow,.1f);
            }
        }

        void BuildEdgeWarnings(TrackNode[] nodes,Transform parent,Material cyan,TrackShortcut[] shortcuts)
        {
            for(int i=0;i<nodes.Length;i+=12)
            {
                var n=nodes[i];
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 edge=n.Position+n.Right*side*(n.TrackWidth*.5f+.7f);
                    if(IntersectsShortcut(edge,4,shortcuts,2.4f))continue;
                    // Sparse visual warnings only. Open edges have no invisible
                    // continuous collision wall and reveal no shortcut entrance.
                    var marker=RacingArt.Box(parent,"Open edge warning",edge+Vector3.up*.65f,new Vector3(.16f,.65f,.28f),cyan,.03f);
                    marker.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
            }
        }

        void BuildGate(TrackNode node,Transform parent,Material metal,Material cyan,Material yellow)
        {
            var gate=new GameObject("Night City start arch");gate.transform.SetParent(parent,false);gate.transform.position=node.Position;gate.transform.rotation=Quaternion.LookRotation(node.Forward,Vector3.up);
            for(int s=-1;s<=1;s+=2){RacingArt.Box(gate.transform,"Pylon",new Vector3(s*17.5f,7,0),new Vector3(1.6f,14,1.6f),metal,.3f);var span=RacingArt.Box(gate.transform,"Neon Truss",new Vector3(s*8.5f,13.8f,0),new Vector3(19,.95f,1.6f),cyan,.2f);span.transform.localRotation=Quaternion.Euler(0,0,-s*10);}
            Sign(gate.transform,"HILL ADVENTURER // GRAND CIRCUIT",new Vector3(0,10.6f,-.65f),.75f,yellow.color);
        }

        void BuildBridge(TrackNode[] nodes,Transform parent,Material metal,Material cyan)
        {
            for(int i=5*Steps;i<7*Steps;i+=4)
            {
                var n=nodes[i];
                for(int side=-1;side<=1;side+=2)
                {
                    var pylon=RacingArt.Box(parent,"Skyway pylon",n.Position+n.Right*side*17.5f+Vector3.up*4.5f,new Vector3(.8f,11,.8f),cyan,.1f);
                    pylon.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
                var cross=RacingArt.Box(parent,"Skyway arch",n.Position+Vector3.up*10.2f,new Vector3(36,.75f,1.3f),metal,.1f);
                cross.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
            }
        }

        void BuildTunnel(TrackNode[] nodes,Transform parent,Material metal,Material magenta,Material cyan)
        {
            for(int i=13*Steps;i<14*Steps+4;i+=3)
            {
                var n=nodes[i];var hoop=new GameObject("Cyber tunnel arch");hoop.transform.SetParent(parent,false);hoop.transform.position=n.Position;hoop.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                for(int s=-1;s<=1;s+=2)
                {
                    if(IntersectsShortcut(n.Position+n.Right*s*17.5f,4,Graph.Shortcuts))continue;
                    var wall=RacingArt.Box(hoop.transform,"Tunnel flank",new Vector3(s*17.5f,4.5f,0),new Vector3(4.2f,9,6),metal,.4f);
                    wall.AddComponent<BoxCollider>().size=new Vector3(4.2f,9,6);
                    RacingArt.Box(hoop.transform,"Neon rib",new Vector3(s*15.2f,4f,0),new Vector3(.18f,8f,1.4f),s==1?cyan:magenta,.05f);
                }
                var ceiling=RacingArt.Box(hoop.transform,"Tunnel roof",new Vector3(0,9.8f,0),new Vector3(37,2.4f,6),metal,.4f);
                ceiling.AddComponent<BoxCollider>().size=new Vector3(37,2.4f,6);
            }
        }

        static bool ClearSceneryPosition(Vector3 position, float radius, TrackNode[] nodes, TrackShortcut[] routes)
        {
            foreach (var n in nodes)
            {
                Vector3 delta = position - n.Position; delta.y = 0;
                if (delta.sqrMagnitude < (radius+n.TrackWidth*.5f+4)*(radius+n.TrackWidth*.5f+4)) return false;
            }
            foreach (var route in routes) for (int i=1;i<route.pathPoints.Length;i++)
            {
                Vector3 a=route.pathPoints[i-1], delta=route.pathPoints[i]-a; delta.y=0;
                Vector3 offset=position-a; offset.y=0;
                float t=delta.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(offset,delta)/delta.sqrMagnitude):0;
                if ((offset-delta*t).sqrMagnitude < (radius+10)*(radius+10)) return false;
            }
            return true;
        }
        static bool IntersectsShortcut(Vector3 position,float radius,TrackShortcut[] routes,float maxHeightDifference=100)
        {
            foreach(var route in routes)for(int i=1;i<route.pathPoints.Length;i++)
            {
                Vector3 a=route.pathPoints[i-1],delta=route.pathPoints[i]-a;
                float heightChange=delta.y;delta.y=0;
                Vector3 offset=position-a;offset.y=0;
                float t=delta.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(offset,delta)/delta.sqrMagnitude):0;
                if(Mathf.Abs(position.y-a.y-heightChange*t)>maxHeightDifference)continue;
                if((offset-delta*t).sqrMagnitude<(radius+6)*(radius+6))return true;
            }
            return false;
        }
        void BuildShortcuts(TrackShortcut[] routes,Transform parent,Material road,Material cyan,Material magenta,Material yellow)
        {
            foreach (var route in routes)
            {
                var routeNodes=new TrackNode[route.pathPoints.Length];
                for(int i=0;i<routeNodes.Length;i++)
                {
                    Vector3 tangent=route.pathPoints[Mathf.Min(i+1,routeNodes.Length-1)]-route.pathPoints[Mathf.Max(0,i-1)];
                    routeNodes[i]=new TrackNode{Position=route.pathPoints[i],Forward=tangent.normalized,Right=Vector3.Cross(Vector3.up,tangent).normalized,Up=Vector3.up,TrackWidth=route.WidthAt(i)};
                }
                void Deck(int first,int last,RacingSurfaceKind surface)
                {
                    var section=new TrackNode[last-first+1];Array.Copy(routeNodes,first,section,0,section.Length);
                    RacingTrackGeometry.Ribbon(parent,route.displayName+" deck "+first,section,-10,10,road,surface,true,true,-1,-1,0,false);
                }
                if(!route.requiresJump)Deck(0,routeNodes.Length-1,RacingSurfaceKind.Road);
                else
                {
                    Deck(0,4,RacingSurfaceKind.Road);
                    if(route.requiresJump){Deck(4,route.gapStartNodeIndex,RacingSurfaceKind.Road);Deck(route.landingNodeIndex,routeNodes.Length-1,RacingSurfaceKind.Road);}
                    else Deck(4,routeNodes.Length-1,RacingSurfaceKind.Road);
                }
                // Keep physical route decks and requirements, but no signs,
                // arrows, glowing edges or explicit route names for these cuts.
                if (route.requiresJump)
                {
                    if(route.requiresBoost)
                    {
                        route.BoostSupply=ItemBox.CreateBoostSupply(transform,route.pathPoints[4]+Vector3.up*1.1f,route.width*.70f);
                        route.BoostSupply.transform.rotation=Quaternion.LookRotation(route.pathPoints[5]-route.pathPoints[4],Vector3.up);
                    }
                    var pad=new GameObject(route.displayName+" launch strip");pad.transform.SetParent(parent,false);
                    pad.transform.position=route.pathPoints[route.launchNodeIndex];
                    pad.transform.rotation=Quaternion.LookRotation(route.pathPoints[route.launchNodeIndex+1]-route.pathPoints[route.launchNodeIndex],Vector3.up);
                    var trigger=pad.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up;trigger.size=new Vector3(route.width,3,2);
                    var jump=pad.AddComponent<RacingJumpPad>();
                    jump.RequiresBoost=route.requiresBoost;
                    if(route.shortcutId=="crest_cut")jump.VerticalImpulse=6.0f;
                }
            }
        }

        void BuildStreetLights(TrackNode[] nodes,Transform parent,Material metal,Material cyan)
        {
            for(int i=0;i<nodes.Length;i+=4)
            {
                var n=nodes[i];
                Vector3 p=n.Position+n.Right*(n.TrackWidth*.5f+2.5f);
                var post=RacingArt.Box(parent,"Streetlight post",p+Vector3.up*3f,new Vector3(.2f,6f,.2f),metal,.05f);
                var arm=RacingArt.Box(parent,"Streetlight arm",p+Vector3.up*5.8f-n.Right*.8f,new Vector3(1.6f,.15f,.15f),metal,.04f);
                RacingArt.Box(parent,"Neon light fixture",p+Vector3.up*5.6f-n.Right*1.4f,new Vector3(.4f,.15f,.6f),cyan,.05f);
            }
        }

        void BuildDigitalCascade(Vector3 point,Transform parent,Material cyan)
        {
            var cascade=RacingArt.Box(parent,"Holographic cascade",point+Vector3.up*12,new Vector3(6,28,.25f),cyan,.1f);
            cascade.AddComponent<RacingEnvironmentMotion>().Water=true;
        }

        void BuildRaceGantries(TrackNode[] nodes,Transform parent,Material metal,Material cyan,Material yellow)
        {
            foreach(int st in new[]{4*Steps, 12*Steps})
            {
                var n=nodes[st];
                var gantry=new GameObject("Holo Race Gantry "+st);
                gantry.transform.SetParent(parent,false);
                gantry.transform.position=n.Position;
                gantry.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                for(int s=-1;s<=1;s+=2)
                {
                    RacingArt.Box(gantry.transform,"Pylon",new Vector3(s*(n.TrackWidth*.5f+2.5f),5.5f,0),new Vector3(1.1f,11,1.1f),metal,.2f);
                }
                RacingArt.Box(gantry.transform,"Crossbeam",new Vector3(0,10.5f,0),new Vector3(n.TrackWidth+6,.7f,1.2f),cyan,.1f);
                Sign(gantry.transform,"SPEED SECTOR // FLOW",new Vector3(0,9.2f,-.55f),.55f,yellow.color);
            }
        }

        void BuildApexKerbs(TrackNode[] nodes,Transform parent,Material cyan,Material yellow,Material metal)
        {
            for(int i=0;i<nodes.Length;i++)
            {
                var n=nodes[i];
                if(n.IsDriftZone)
                {
                    float side=Mathf.Sign(n.OptimalLineOffset);
                    if(side==0)side=1;
                    Vector3 curbPos=n.Position+n.Right*side*(n.TrackWidth*.5f-.4f)+Vector3.up*.035f;
                    Material mat=(i%2==0)?cyan:yellow;
                    var curb=RacingArt.Box(parent,"Apex curb",curbPos,new Vector3(.9f,.08f,3.2f),mat,.02f);
                    curb.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
            }
        }

        void BuildStarrySky(Transform parent)
        {
            var skyRoot=new GameObject("Starry Night Dome");
            skyRoot.transform.SetParent(parent,false);
            skyRoot.transform.position=new Vector3(90,40,0);
            var starMat=RacingArt.Material(Color.white,.95f,true);
            var cyanStarMat=RacingArt.Material(new Color(.65f,.92f,1f),.95f,true);
            var goldStarMat=RacingArt.Material(new Color(1f,.88f,.55f),.95f,true);
            var rng=new DeterministicRng(71829);
            float domeRadius=650f;
            for(int i=0;i<320;i++)
            {
                float u=rng.NextFloat();float v=rng.NextFloat();
                float theta=u*Mathf.PI*2f;
                float phi=Mathf.Acos(1f-v*.72f);
                float x=domeRadius*Mathf.Sin(phi)*Mathf.Cos(theta);
                float y=domeRadius*Mathf.Cos(phi)+15f;
                float z=domeRadius*Mathf.Sin(phi)*Mathf.Sin(theta);
                float size=rng.Range(2.2f,4.5f);
                Material mat=(i%7==0)?goldStarMat:(i%3==0)?cyanStarMat:starMat;
                var star=RacingArt.Box(skyRoot.transform,"Star_"+i,new Vector3(x,y,z),new Vector3(size,size,size),mat,.05f);
                star.transform.LookAt(skyRoot.transform.position);
            }
        }

        void BuildTrafficStreaks(Transform parent,Material cyan,Material magenta,Material yellow,Material structure)
        {
            var trafficRoot=new GameObject("Animated Highway Traffic");
            trafficRoot.transform.SetParent(parent,false);
            Vector3[] origins={
                new Vector3(-180,48,120),
                new Vector3(320,62,-80),
                new Vector3(140,55,380),
                new Vector3(-90,70,-320)
            };
            Vector3[] vectors={
                new Vector3(450,0,-40),
                new Vector3(-480,0,80),
                new Vector3(-350,0,-420),
                new Vector3(380,0,360)
            };
            for(int corridor=0;corridor<4;corridor++)
            {
                var corridorObj=new GameObject("Skyway Corridor "+corridor);
                corridorObj.transform.SetParent(trafficRoot.transform,false);
                corridorObj.transform.position=origins[corridor];
                var girder=RacingArt.Box(corridorObj.transform,"Transit viaduct",vectors[corridor]*.5f,new Vector3(8,1.2f,vectors[corridor].magnitude),structure,.1f);
                girder.transform.rotation=Quaternion.LookRotation(vectors[corridor].normalized,Vector3.up);

                int trafficCount=QualitySettings.GetQualityLevel()==0?2:4;
                for(int car=0;car<trafficCount;car++)
                {
                    var flowObj=new GameObject("Traffic vehicle "+car);
                    flowObj.transform.SetParent(corridorObj.transform,false);
                    var flow=flowObj.AddComponent<RacingTrafficFlow>();
                    flow.Direction=vectors[corridor].normalized;
                    flow.Length=vectors[corridor].magnitude;
                    flow.Speed=38f+corridor*9f;
                    flow.Phase=car/(float)trafficCount;
                    Vector3 carPos=Vector3.zero;
                    Material headlightMat=(car%2==0)?cyan:yellow;
                    RacingArt.Box(flowObj.transform,"Headlight Streak",carPos+Vector3.up*1.2f+girder.transform.right*1.8f,new Vector3(.6f,.4f,4.5f),headlightMat,.05f).transform.rotation=girder.transform.rotation;
                    RacingArt.Box(flowObj.transform,"Taillight Streak",carPos+Vector3.up*1.2f-girder.transform.right*1.8f,new Vector3(.6f,.4f,4.5f),magenta,.05f).transform.rotation=girder.transform.rotation;
                    RacingArt.Box(flowObj.transform,"Transit silhouette",carPos+Vector3.up*.6f,new Vector3(2.2f,1,6),RacingArt.Material(new Color(.045f,.07f,.11f),.2f),.2f).transform.rotation=girder.transform.rotation;
                    RacingArt.Combine(flowObj.transform);
                }
            }
        }

        static void Sign(Transform parent,string text,Vector3 point,float scale,Color color)
        {
            var sign=new GameObject(text);sign.transform.SetParent(parent,false);sign.transform.localPosition=point;sign.transform.localScale=Vector3.one*scale;var label=sign.AddComponent<TextMesh>();label.text=text;label.fontSize=52;label.characterSize=.11f;label.anchor=TextAnchor.MiddleCenter;label.color=color;label.font=RacingDesign.Font;sign.GetComponent<MeshRenderer>().sharedMaterial=RacingDesign.Font.material;
        }
        static void BuildSectionGuidance(TrackNode[] nodes,Transform parent,Material metal)
        {
            Color[] colors={RacingVehicleFactory.MetaWhite,RacingDesign.Cyan,new Color(.72f,.67f,1),RacingVehicleFactory.MetaWhite};
            string[] names=NightCityCircuit.Sectors;
            for(int sector=0;sector<names.Length;sector++)
            {
                var n=nodes[sector*nodes.Length/names.Length+5];
                var board=new GameObject(names[sector]);board.transform.SetParent(parent,false);board.transform.position=n.Position+n.Right*(n.TrackWidth*.5f+3)+Vector3.up*4;
                board.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                RacingArt.Box(board.transform,"Sign backing",Vector3.zero,new Vector3(12,2.6f,.18f),metal,.2f);
                Sign(board.transform,names[sector],new Vector3(0,0,-.12f),.70f,colors[sector]);
                var accent=RacingArt.Box(board.transform,"Living sign edge",new Vector3(0,-1.1f,-.16f),new Vector3(11,.08f,.08f),RacingArt.Material(colors[sector],.3f,true),.01f);
                accent.AddComponent<RacingBillboardPulse>();
            }
            // Prism lane guides: an original 12-step hue gradient around the lap
            // (teal -> electric blue -> violet -> magenta -> amber). Bucketed into
            // shared materials so the combined scenery keeps its batching.
            var prism=new Material[PrismSteps];
            for(int k=0;k<PrismSteps;k++)prism[k]=RacingArt.IdentityMaterial("Night City / prism lane "+k,Prism(k/(float)PrismSteps),.75f);
            for(int i=0;i<nodes.Length;i+=2)
            {
                var n=nodes[i];if(i>=56&&i<=61)continue;
                var lane=prism[Mathf.Min(PrismSteps-1,i*PrismSteps/nodes.Length)];
                for(int side=-1;side<=1;side+=2)
                {
                    var mark=RacingArt.Box(parent,"Lane guide",n.Position+n.Right*side*3.7f+Vector3.up*.03f,new Vector3(.12f,.025f,2.5f),lane,.01f);
                    mark.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
            }
        }

        // Static emissive geometry built once and batched with the scenery: it reads
        // as neon without bloom, adds no lights, Updates, transparency or colliders.
        public static readonly Color[] SectorNeon={new Color(.22f,.48f,1f),new Color(.64f,.34f,1f),new Color(.16f,.88f,1f)};
        static void BuildNeonPolish(TrackNode[] nodes,TrackShortcut[] routes,Transform parent,Material white)
        {
            int per=nodes.Length/SectorNeon.Length;
            for(int s=0;s<SectorNeon.Length;s++)
            {
                var neon=RacingArt.IdentityMaterial("Night City / sector neon "+s,SectorNeon[s],.95f);
                int first=s*per,last=s==SectorNeon.Length-1?nodes.Length:(s+1)*per;
                var slice=new TrackNode[last-first+1];
                for(int i=0;i<slice.Length;i++)slice[i]=nodes[(first+i)%nodes.Length];
                // Inner lane strip in the sector colour, inside the cyan edge core.
                RacingTrackGeometry.Ribbon(parent,"Sector neon lane strip",slice,-9.66f,-9.54f,neon,RacingSurfaceKind.Road,false,true,closed:false,clearRoutes:routes).transform.localPosition=Vector3.up*.06f;
                RacingTrackGeometry.Ribbon(parent,"Sector neon lane strip",slice,9.54f,9.66f,neon,RacingSurfaceKind.Road,false,true,closed:false,clearRoutes:routes).transform.localPosition=Vector3.up*.06f;
                // Faint electric under-deck line: visible only from the side/below.
                var under=RacingArt.IdentityMaterial("Night City / under-deck glow "+s,SectorNeon[s]*.55f,.8f);
                for(int side=-1;side<=1;side+=2)
                    RacingTrackGeometry.Ribbon(parent,"Under-deck glow",slice,side<0?-10.42f:10.30f,side<0?-10.30f:10.42f,under,RacingSurfaceKind.Road,false,true,closed:false,clearRoutes:routes).transform.localPosition=Vector3.down*1.7f;
                if(s==0)continue;
                // Sector marker: a thin transverse light bar plus two posts. Not a finish line.
                var n=nodes[first];
                for(int k=0;k<2;k++)
                {
                    var bar=RacingArt.Box(parent,"Sector marker bar",n.Position+n.Forward*(k*1.4f)+Vector3.up*.045f,new Vector3(n.TrackWidth*.86f,.02f,.28f),neon,.01f);
                    bar.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
            }
            // Glowing racing-line accents through drift zones (the same optimal offset the AI targets).
            var line=RacingArt.IdentityMaterial("Night City / racing line accent",new Color(.86f,.95f,1f),.55f);
            var red=RacingArt.IdentityMaterial("Night City / red corner kerb",new Color(1f,.07f,.12f),1f);
            for(int i=0;i<nodes.Length;i++)
            {
                var n=nodes[i];
                if(n.IsDriftZone&&i%2==0)
                {
                    Vector3 p=n.Position+n.Right*(n.OptimalLineOffset*n.TrackWidth*.4f)+Vector3.up*.035f;
                    if(!IntersectsShortcut(p,2,routes,3f))
                    {
                        var dash=RacingArt.Box(parent,"Racing line accent",p,new Vector3(.22f,.02f,2.2f),line,.01f);
                        dash.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                    }
                }
                // Luminous red/white kerbs on the outside of sharp corners.
                float corner=Vector3.SignedAngle(nodes[(i+nodes.Length-3)%nodes.Length].Forward,nodes[(i+3)%nodes.Length].Forward,Vector3.up);
                if(Mathf.Abs(corner)<25)continue;
                Vector3 kerb=n.Position-n.Right*Mathf.Sign(corner)*(n.TrackWidth*.5f-.45f)+Vector3.up*.04f;
                if(IntersectsShortcut(kerb,2,routes,3f))continue;
                var block=RacingArt.Box(parent,"Outside corner kerb",kerb,new Vector3(.85f,.07f,2.6f),i%2==0?red:white,.02f);
                block.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
            }
        }

        const int PrismSteps=12;
        // Smooth closed loop through an original neon palette; never pure white.
        static Color Prism(float t)
        {
            Color[] keys={new Color(.10f,.85f,.80f),new Color(.16f,.48f,1f),new Color(.56f,.32f,1f),new Color(.95f,.25f,.75f),new Color(1f,.62f,.22f),new Color(.10f,.85f,.80f)};
            float f=Mathf.Repeat(t,1f)*(keys.Length-1);int k=Mathf.Min(keys.Length-2,Mathf.FloorToInt(f));
            return Color.Lerp(keys[k],keys[k+1],f-k);
        }

        public static void ApplyLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.20f,.28f,.43f);
            RenderSettings.ambientEquatorColor=new Color(.14f,.20f,.32f);
            RenderSettings.ambientGroundColor=new Color(.07f,.09f,.15f);
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.00045f;
            RenderSettings.fogColor=new Color(.012f,.028f,.065f);
            var moon=FindFirstObjectByType<Light>();
            if(moon==null)moon=new GameObject("Night City Moonlight").AddComponent<Light>();
            moon.type=LightType.Directional;
            moon.intensity=.95f;
            moon.color=new Color(.72f,.84f,1f);
            moon.transform.rotation=Quaternion.LookRotation(new Vector3(.45f,-.65f,.25f),Vector3.up);
            moon.shadows=LightShadows.Hard;
            RenderSettings.sun=moon;
            var scope=FindFirstObjectByType<RacingEnvironmentScope>();
            RenderSettings.skybox=RacingRainbowScenery.Requested?RacingRainbowScenery.CreateSky(scope):RacingNightSky.Create(scope);
        }
    }



}
