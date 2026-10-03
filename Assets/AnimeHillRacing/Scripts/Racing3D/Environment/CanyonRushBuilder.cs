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
                new Vector3(0,6,0),new Vector3(0,6,110),new Vector3(130,24,205),new Vector3(85,11,290),
                new Vector3(175,9,320),new Vector3(260,11,285),new Vector3(310,13,210),new Vector3(315,19,135),
                new Vector3(285,11,55),new Vector3(235,8,-15),new Vector3(175,14,-78),new Vector3(260,9,-155),
                new Vector3(210,7,-244),new Vector3(105,12,-278),new Vector3(38,15,-221),new Vector3(-48,20,-258),
                new Vector3(-125,18,-175),new Vector3(-85,13,-96),new Vector3(-78,18,0),new Vector3(0,6,-48)
            };
            var positions=RacingTrackGeometry.SmoothLoop(control,Steps);var nodes=new TrackNode[positions.Length];
            for(int i=0;i<nodes.Length;i++)
            {
                Vector3 forward=(positions[(i+1)%positions.Length]-positions[(i+positions.Length-1)%positions.Length]).normalized;
                Vector3 ahead=(positions[(i+3)%positions.Length]-positions[i]).normalized;
                Vector3 behind=(positions[i]-positions[(i+positions.Length-3)%positions.Length]).normalized;
                float corner=Vector3.SignedAngle(behind,ahead,Vector3.up);float target=155-Mathf.Clamp(Mathf.Abs(corner)*1.25f,0,80);
                float technical=SectorWeight(i,78,109)+SectorWeight(i,115,134)+SectorWeight(i,140,156);
                float width=20+Mathf.Sin(i*.075f)*1.5f-Mathf.Clamp01(technical)*5;
                target-=Mathf.Clamp01(technical)*12;
                nodes[i]=new TrackNode{Position=positions[i],Forward=forward,Right=Vector3.Cross(Vector3.up,forward).normalized,Up=Vector3.up,TrackWidth=width,SafeLineOffset=0,OptimalLineOffset=Mathf.Sign(corner)*Mathf.Clamp01(Mathf.Abs(corner)/40)*.28f,TargetSpeedKmh=target,IsDriftZone=Mathf.Abs(corner)>15};
            }
            var groundCut=new TrackShortcut{shortcutId="ridge_cut",displayName="RIDGE CUT",entryNodeIndex=10*Steps,exitNodeIndex=14*Steps-2,requiresBoost=true,minSkillRequired=.42f,riskLevel=.30f};
            // Jump-based shortcut: Neon Skyway Bypass where an intentional jump launches over the outer bend
            var skywayJump=new TrackShortcut{shortcutId="skyway_jump",displayName="SKYWAY LINK",entryNodeIndex=6*Steps+2,exitNodeIndex=9*Steps-2,requiresBoost=true,requiresJump=true,minSkillRequired=.62f,riskLevel=.44f};
            // Boost-gated shortcut 1: Digital Waterfall Cut (Boost-gated void chasm leap)
            var waterfallCut=new TrackShortcut{shortcutId="waterfall_gap",displayName="WATERFALL LINK",entryNodeIndex=3*Steps+1,exitNodeIndex=6*Steps-2,requiresBoost=true,minSkillRequired=.52f,riskLevel=.36f};
            // Boost-gated shortcut 2: Apex Overpass Ramp (Boost-gated elevated sky bridge)
            // Leave after checkpoint 4 and rejoin before checkpoint 5: never bypass a required gate.
            var overpassLaunch=new TrackShortcut{shortcutId="overpass_launch",displayName="APEX VIADUCT",entryNodeIndex=14*Steps+1,exitNodeIndex=17*Steps-2,requiresBoost=true,requiresJump=true,launchNodeIndex=10,gapStartNodeIndex=11,landingNodeIndex=13,minSkillRequired=.70f,riskLevel=.50f};
            // Both precision links stay inside their checkpoint sector. They cut
            // the authored bend, never the checkpoint sequence or lap counter.
            var crestCut=new TrackShortcut{shortcutId="crest_cut",displayName="CREST PHASE",entryNodeIndex=1*Steps+3,exitNodeIndex=3*Steps-2,requiresBoost=true,requiresJump=true,width=8,launchSpeedKmh=110,launchNodeIndex=10,gapStartNodeIndex=11,landingNodeIndex=13,minSkillRequired=.84f,riskLevel=.78f};
            var returnDrop=new TrackShortcut{shortcutId="return_drop",displayName="HAIRPIN DROP",entryNodeIndex=17*Steps+1,exitNodeIndex=19*Steps-1,requiresBoost=false,requiresJump=true,width=7,launchSpeedKmh=80,launchNodeIndex=10,gapStartNodeIndex=11,landingNodeIndex=13,minSkillRequired=.88f,riskLevel=.82f};
            var routes=new[]{groundCut,skywayJump,waterfallCut,overpassLaunch,crestCut,returnDrop};
            foreach(var route in routes)
                route.pathPoints=RacingTrackGeometry.ShortcutPath(nodes[route.entryNodeIndex],nodes[route.exitNodeIndex],
                    route==crestCut||route==returnDrop||route==overpassLaunch);
            for(int i=1;i<skywayJump.pathPoints.Length-1;i++)
                skywayJump.pathPoints[i].y+=7*Mathf.Pow(Mathf.Sin(Mathf.PI*i/(skywayJump.pathPoints.Length-1)),2);
            RacingTrackGeometry.MatchRoadPortals(crestCut,nodes);
            RacingTrackGeometry.MatchRoadPortals(returnDrop,nodes);
            RacingTrackGeometry.MatchRoadPortals(overpassLaunch,nodes);
            var precisionPortals=new[]{crestCut,returnDrop,overpassLaunch};

            int[] checkpoints={0,3*Steps,6*Steps,9*Steps,14*Steps,17*Steps};
            var graphRoot=new GameObject("Night City spline graph");graphRoot.transform.SetParent(transform,false);Graph=graphRoot.AddComponent<TrackSplineGraph>();Graph.InitializeFromNodes(nodes,routes,checkpoints);
            var gridRoot=new GameObject("12 slot starting grid");gridRoot.transform.SetParent(transform,false);gridRoot.transform.position=nodes[0].Position;gridRoot.transform.rotation=Quaternion.LookRotation(nodes[0].Forward,Vector3.up);Grid=gridRoot.AddComponent<StartingGrid>();Grid.Initialize(Graph);

            // Night City Materials with high readable contrast
            Material road=RacingCityArt.Asphalt();
            Material shoulder=RacingArt.Material(new Color(.018f,.026f,.044f),.2f);
            Material cyanGlow=RacingArt.Material(RacingDesign.Cyan,.85f,true);
            Material yellowStripe=RacingArt.Material(new Color(1,.85f,.08f),.4f,true);
            Material magentaGlow=RacingArt.Material(new Color(.95f,.15f,.75f),.85f,true);
            Material metalDark=RacingArt.Material(new Color(.05f,.07f,.11f),.7f);

            // Jump pads / launch kicker
            int gapStart=7*Steps+2,gapEnd=7*Steps+3;
            Graph.MainJumpNodeIndex=gapStart;
            var launchNode=nodes[gapStart];
            var kicker=new GameObject("Skyway leap kicker");kicker.transform.SetParent(transform,false);
            kicker.transform.position=launchNode.Position-launchNode.Forward*2+Vector3.up*.6f;
            kicker.transform.rotation=Quaternion.LookRotation(launchNode.Forward,Vector3.up);
            var launchTrigger=kicker.AddComponent<BoxCollider>();launchTrigger.isTrigger=true;launchTrigger.size=new Vector3(launchNode.TrackWidth+5,2,3);
            kicker.AddComponent<RacingJumpPad>();
            RacingArt.Box(kicker.transform,"Neon leap strip",new Vector3(0,-.57f,0),new Vector3(launchNode.TrackWidth,.06f,1.2f),cyanGlow,.1f);

            // Volumetric track ribbons (with 2.2m base to prevent tunneling)
            RacingTrackGeometry.Ribbon(transform,"City asphalt",nodes,-10,10,road,RacingSurfaceKind.Road,true,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Left neon curb",nodes,-10.8f,-10,cyanGlow,RacingSurfaceKind.Road,false,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Right neon curb",nodes,10,10.8f,cyanGlow,RacingSurfaceKind.Road,false,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Left shoulder",nodes,-15,-10.8f,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,clearRoutes:precisionPortals);
            RacingTrackGeometry.Ribbon(transform,"Right shoulder",nodes,10.8f,15,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,clearRoutes:precisionPortals);
            RacingTrackGeometry.Ribbon(transform,"City left slope",nodes,-15,-45,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,14,clearRoutes:precisionPortals);
            RacingTrackGeometry.Ribbon(transform,"City right slope",nodes,15,45,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,14,clearRoutes:precisionPortals);

            var scenery=new GameObject("Batched night city scenery");scenery.transform.SetParent(transform,false);
            var rng=new DeterministicRng(90173);

            // Procedural Neon Skyscrapers surrounding the circuit
            BuildCitySkyscrapers(nodes,Graph.Shortcuts,scenery.transform,rng,metalDark,cyanGlow,magentaGlow,yellowStripe);

            // City ground basin
            var ground=RacingArt.Box(scenery.transform,"City basin",new Vector3(90,-16,0),new Vector3(1400,1,1400),shoulder,.5f);
            ground.AddComponent<BoxCollider>().size=new Vector3(1400,1,1400);ground.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Dirt;

            BuildBarriers(nodes,scenery.transform,metalDark,magentaGlow,cyanGlow,routes);
            BuildGate(nodes[0],scenery.transform,metalDark,cyanGlow,yellowStripe);
            BuildBridge(nodes,scenery.transform,metalDark,cyanGlow);
            BuildTunnel(nodes,scenery.transform,metalDark,magentaGlow,cyanGlow);
            BuildShortcuts(routes,scenery.transform,road,cyanGlow,magentaGlow,yellowStripe);
            BuildStreetLights(nodes,scenery.transform,metalDark,cyanGlow);
            BuildDigitalCascade(nodes[3].Position+nodes[3].Right*-58,scenery.transform,cyanGlow);
            BuildRaceGantries(nodes,scenery.transform,metalDark,cyanGlow,yellowStripe);
            BuildApexKerbs(nodes,scenery.transform,cyanGlow,yellowStripe,metalDark);
            // Stars and the sky gradient use one inexpensive sky shader.
            BuildTrafficStreaks(scenery.transform,cyanGlow,magentaGlow,yellowStripe);

            for(int i=0;i<checkpoints.Length;i++)
            {
                var n=nodes[checkpoints[i]];var cp=new GameObject(i==0?"Finish gate":"Checkpoint "+i);cp.transform.SetParent(transform,false);cp.transform.position=n.Position+Vector3.up*2;cp.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                var trigger=cp.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(n.TrackWidth+10,9,8);cp.AddComponent<CheckpointTrigger3D>().Configure(i,i==0);
            }
            foreach(int station in new[]{1*Steps,5*Steps,10*Steps-4,15*Steps})for(int lane=-1;lane<=1;lane++)ItemBox.Create(transform,nodes[station].Position+nodes[station].Right*lane*4.5f+Vector3.up*1.1f);
            for(int row=0;row<6;row++)
            {
                var n=Graph.SampleDistance(Graph.TotalLength-8-row*7);
                for(int side=-1;side<=1;side+=2){var stripe=RacingArt.Box(scenery.transform,"Grid mark",n.Position+n.Right*side*1.6f+Vector3.up*.025f,new Vector3(1.5f,.03f,.14f),yellowStripe,.02f);stripe.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);}
            }

            BuildSectionGuidance(nodes,scenery.transform,metalDark);
            RacingArt.Combine(scenery.transform,160);
            RacingCityBackdrop.Build(transform);
            ApplyLighting();
            Debug.Log($"NIGHT_CITY_BUILT nodes={nodes.Length} length={Graph.TotalLength:F1} checkpoints={checkpoints.Length} shortcuts={routes.Length}");
        }

        static float SectorWeight(int node,int start,int end)
            => node<=start||node>=end?0:Mathf.Sin(Mathf.PI*(node-start)/(end-start));

        static void BuildCitySkyscrapers(TrackNode[] nodes,TrackShortcut[] routes,Transform parent,DeterministicRng rng,Material dark,Material cyan,Material magenta,Material yellow)
        {
            Material warm=RacingArt.Material(new Color(.82f,.53f,.20f),.2f,true);
            Material teal=RacingArt.Material(new Color(.08f,.65f,.49f),.25f,true);
            Material violet=RacingArt.Material(new Color(.48f,.20f,.75f),.3f,true);
            Material[] neonWindows=new[]{cyan,warm,teal,violet,magenta,yellow};

            // Tier 1: Trackside commercial buildings (48 towers)
            for(int i=0;i<48;i++)
            {
                TrackNode n=nodes[(i*7)%nodes.Length];int side=(i&1)==0?-1:1;
                float dist=rng.Range(36,68);
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
            for(int i=0;i<32;i++)
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

        void BuildBarriers(TrackNode[] nodes,Transform parent,Material metal,Material magenta,Material cyan,TrackShortcut[] shortcuts)
        {
            for(int i=0;i<nodes.Length;i++)
            {
                var n=nodes[i];var next=nodes[(i+1)%nodes.Length];float length=Vector3.Distance(n.Position,next.Position);
                for(int side=-1;side<=1;side+=2)
                {
                    bool isOpening = false;
                    if(shortcuts != null)
                    {
                        foreach(var sc in shortcuts)
                        {
                            if(sc.pathPoints == null || sc.pathPoints.Length < 2) continue;
                            TrackNode en = nodes[sc.entryNodeIndex % nodes.Length];
                            int entrySide = Vector3.Dot(sc.pathPoints[1] - en.Position, en.Right) >= 0 ? 1 : -1;
                            if(side == entrySide && Mathf.Abs(i - sc.entryNodeIndex) <= 1) { isOpening = true; break; }

                            TrackNode ex = nodes[sc.exitNodeIndex % nodes.Length];
                            int exitSide = Vector3.Dot(sc.pathPoints[sc.pathPoints.Length - 2] - ex.Position, ex.Right) >= 0 ? 1 : -1;
                            if(side == exitSide && Mathf.Abs(i - sc.exitNodeIndex) <= 1) { isOpening = true; break; }
                        }
                    }
                    if(isOpening) continue;

                    Vector3 a=n.Position+n.Right*side*(n.TrackWidth*.5f+.7f),b=next.Position+next.Right*side*(next.TrackWidth*.5f+.7f);
                    if(IntersectsShortcut((a+b)*.5f,3,shortcuts,2.4f))continue;
                    var rail=RacingArt.Box(parent,"Neon barrier",(a+b)*.5f+Vector3.up*.62f,new Vector3(.25f,1.05f,length+.15f),metal,.05f);rail.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);rail.AddComponent<BoxCollider>().size=new Vector3(.25f,1.05f,length+.15f);
                    // Glowing magenta rail topper
                    RacingArt.Box(parent,"Neon rail top",(a+b)*.5f+Vector3.up*1.15f,new Vector3(.12f,.10f,length+.15f),magenta,.02f).transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                    if(i%3==0)RacingArt.Box(parent,"Neon beacon",a+Vector3.up*.8f,new Vector3(.3f,.10f,.9f),cyan,.04f);
                }
            }
        }

        void BuildGate(TrackNode node,Transform parent,Material metal,Material cyan,Material yellow)
        {
            var gate=new GameObject("Night City start arch");gate.transform.SetParent(parent,false);gate.transform.position=node.Position;gate.transform.rotation=Quaternion.LookRotation(node.Forward,Vector3.up);
            for(int s=-1;s<=1;s+=2){RacingArt.Box(gate.transform,"Pylon",new Vector3(s*13,6,0),new Vector3(1.4f,12,1.4f),metal,.3f);var span=RacingArt.Box(gate.transform,"Neon Truss",new Vector3(s*6.2f,11.8f,0),new Vector3(14,.85f,1.4f),cyan,.2f);span.transform.localRotation=Quaternion.Euler(0,0,-s*10);}
            Sign(gate.transform,"HILL ADVENTURER // NIGHT CITY",new Vector3(0,9.2f,-.65f),.62f,yellow.color);
        }

        void BuildBridge(TrackNode[] nodes,Transform parent,Material metal,Material cyan)
        {
            for(int i=6*Steps;i<8*Steps;i+=3)
            {
                var n=nodes[i];
                for(int side=-1;side<=1;side+=2)
                {
                    var pylon=RacingArt.Box(parent,"Skyway pylon",n.Position+n.Right*side*13+Vector3.up*4,new Vector3(.6f,10,.6f),cyan,.1f);
                    pylon.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
                var cross=RacingArt.Box(parent,"Skyway arch",n.Position+Vector3.up*8.8f,new Vector3(26,.65f,1.1f),metal,.1f);
                cross.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
            }
        }

        void BuildTunnel(TrackNode[] nodes,Transform parent,Material metal,Material magenta,Material cyan)
        {
            for(int i=3*Steps;i<4*Steps+4;i+=2)
            {
                var n=nodes[i];var hoop=new GameObject("Cyber tunnel arch");hoop.transform.SetParent(parent,false);hoop.transform.position=n.Position;hoop.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                for(int s=-1;s<=1;s+=2)
                {
                    if(IntersectsShortcut(n.Position+n.Right*s*13,4,Graph.Shortcuts))continue;
                    var wall=RacingArt.Box(hoop.transform,"Tunnel flank",new Vector3(s*13,4,0),new Vector3(3.5f,8,6),metal,.4f);
                    wall.AddComponent<BoxCollider>().size=new Vector3(3.5f,8,6);
                    RacingArt.Box(hoop.transform,"Neon rib",new Vector3(s*11.2f,3.5f,0),new Vector3(.15f,7f,1.2f),s==1?cyan:magenta,.05f);
                }
                var ceiling=RacingArt.Box(hoop.transform,"Tunnel roof",new Vector3(0,8.5f,0),new Vector3(28,2.1f,6),metal,.4f);
                ceiling.AddComponent<BoxCollider>().size=new Vector3(28,2.1f,6);
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
                Material guidance=route.shortcutId=="return_drop"||route.shortcutId=="overpass_launch"?magenta:route.shortcutId=="ridge_cut"?yellow:route.shortcutId=="crest_cut"?RacingArt.Material(RacingDriftFeedback.Green,.4f,true):cyan;
                var routeNodes=new TrackNode[route.pathPoints.Length];
                for(int i=0;i<routeNodes.Length;i++)
                {
                    Vector3 tangent=route.pathPoints[Mathf.Min(i+1,routeNodes.Length-1)]-route.pathPoints[Mathf.Max(0,i-1)];
                    routeNodes[i]=new TrackNode{Position=route.pathPoints[i],Forward=tangent.normalized,Right=Vector3.Cross(Vector3.up,tangent).normalized,Up=Vector3.up,TrackWidth=route.width};
                }
                void Deck(int first,int last,RacingSurfaceKind surface)
                {
                    var section=new TrackNode[last-first+1];Array.Copy(routeNodes,first,section,0,section.Length);
                    RacingTrackGeometry.Ribbon(parent,route.displayName+" deck "+first,section,-route.width*.5f,route.width*.5f,road,surface,true,false,-1,-1,0,false);
                }
                if(route.shortcutId=="ridge_cut")Deck(0,routeNodes.Length-1,RacingSurfaceKind.Dirt);
                else
                {
                    Deck(0,4,route.shortcutId=="crest_cut"||route.shortcutId=="return_drop"||route.shortcutId=="overpass_launch"?RacingSurfaceKind.Road:RacingSurfaceKind.Dirt);
                    if(route.requiresJump){Deck(4,route.gapStartNodeIndex,RacingSurfaceKind.Road);Deck(route.landingNodeIndex,routeNodes.Length-1,RacingSurfaceKind.Road);}
                    else Deck(4,routeNodes.Length-1,RacingSurfaceKind.Road);
                }
                for(int i=0;i<route.pathPoints.Length-1;i++)
                {
                    Vector3 a=route.pathPoints[i],b=route.pathPoints[i+1];
                    Quaternion rotation=Quaternion.LookRotation(b-a,Vector3.up);
                    float length=Vector3.Distance(a,b);
                    // Continuous ribbon collision prevents wheel impulses at overlapping box seams.
                    for(int side=-1;side<=1;side+=2)
                    {
                        var edge=RacingArt.Box(parent,"Route edge guide",(a+b)*.5f+rotation*Vector3.right*side*(route.width*.5f-.18f)+Vector3.up*.045f,new Vector3(.16f,.06f,length),guidance,.02f);
                        edge.transform.rotation=rotation;
                    }
                    for(float d=10;d<length;d+=20)
                    {
                        var arrow=RacingArt.Box(parent,"Boost route arrow",Vector3.Lerp(a,b,d/length)+Vector3.up*.045f,new Vector3(.65f,.04f,2),yellow,.02f);
                        arrow.transform.rotation=rotation;
                    }
                }
                Vector3 direction=(route.pathPoints[1]-route.pathPoints[0]).normalized;
                var sign=new GameObject(route.displayName+" direction sign");sign.transform.SetParent(parent,false);
                sign.transform.position=route.pathPoints[0]+Vector3.up*3;
                sign.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
                string requirement=route.requiresBoost?(route.requiresJump?"BOOST + JUMP":"BOOST"):"PRECISE DROP";
                Sign(sign.transform,route.displayName+" // "+requirement,Vector3.zero,.48f,guidance.color);
                for(int s=-1;s<=1;s+=2)RacingArt.Box(sign.transform,"Precision entry beacon",new Vector3(s*route.width*.42f,-1.8f,0),new Vector3(.18f,2,.18f),guidance,.04f);
                if (route.requiresJump)
                {
                    if(route.requiresBoost)
                    {
                        route.BoostSupply=ItemBox.CreateBoostSupply(transform,route.pathPoints[4]+Vector3.up*1.1f,route.width*.70f);
                        Sign(route.BoostSupply.transform,"BOOST CELL",new Vector3(0,1.5f,0),.28f,guidance.color);
                    }
                    var pad=new GameObject(route.displayName+" launch strip");pad.transform.SetParent(parent,false);
                    pad.transform.position=route.pathPoints[route.launchNodeIndex];
                    pad.transform.rotation=Quaternion.LookRotation(route.pathPoints[route.launchNodeIndex+1]-route.pathPoints[route.launchNodeIndex],Vector3.up);
                    var trigger=pad.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up;trigger.size=new Vector3(route.width,3,2);
                    var jump=pad.AddComponent<RacingJumpPad>();
                    jump.RequiresBoost=route.requiresBoost;
                    if(route.shortcutId=="return_drop")jump.VerticalImpulse=2.7f;
                    if(route.shortcutId=="crest_cut")jump.VerticalImpulse=5.5f;
                    RacingArt.Box(pad.transform,"Visible kicker",new Vector3(0,.08f,0),new Vector3(route.width*.8f,.12f,1),guidance,.03f);
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

        void BuildTrafficStreaks(Transform parent,Material cyan,Material magenta,Material yellow)
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
                var girder=RacingArt.Box(corridorObj.transform,"Girder",vectors[corridor]*.5f,new Vector3(8,1.2f,vectors[corridor].magnitude),cyan,.1f);
                girder.transform.rotation=Quaternion.LookRotation(vectors[corridor].normalized,Vector3.up);

                var flowObj=new GameObject("Traffic Flow");
                flowObj.transform.SetParent(corridorObj.transform,false);
                var flow=flowObj.AddComponent<RacingTrafficFlow>();
                flow.Direction=vectors[corridor].normalized;
                flow.Length=vectors[corridor].magnitude;
                flow.Speed=38f+corridor*9f;

                for(int car=0;car<7;car++)
                {
                    float offsetDist=car*(vectors[corridor].magnitude/7f);
                    Vector3 carPos=vectors[corridor].normalized*offsetDist;
                    Material headlightMat=(car%2==0)?cyan:yellow;
                    RacingArt.Box(flowObj.transform,"Headlight Streak",carPos+Vector3.up*1.2f+girder.transform.right*1.8f,new Vector3(.6f,.4f,4.5f),headlightMat,.05f).transform.rotation=girder.transform.rotation;
                    RacingArt.Box(flowObj.transform,"Taillight Streak",carPos+Vector3.up*1.2f-girder.transform.right*1.8f,new Vector3(.6f,.4f,4.5f),magenta,.05f).transform.rotation=girder.transform.rotation;
                    RacingArt.Box(flowObj.transform,"Transit silhouette",carPos+Vector3.up*.6f,new Vector3(2.2f,1,6),RacingArt.Material(new Color(.045f,.07f,.11f),.2f),.2f).transform.rotation=girder.transform.rotation;
                }
                RacingArt.Combine(flowObj.transform);
            }
        }

        static void Sign(Transform parent,string text,Vector3 point,float scale,Color color)
        {
            var sign=new GameObject(text);sign.transform.SetParent(parent,false);sign.transform.localPosition=point;sign.transform.localScale=Vector3.one*scale;var label=sign.AddComponent<TextMesh>();label.text=text;label.fontSize=52;label.characterSize=.11f;label.anchor=TextAnchor.MiddleCenter;label.color=color;label.font=RacingDesign.Font;sign.GetComponent<MeshRenderer>().sharedMaterial=RacingDesign.Font.material;
        }
        static void BuildSectionGuidance(TrackNode[] nodes,Transform parent,Material metal)
        {
            Color[] colors={new Color(.10f,.85f,.72f),new Color(1,.43f,.12f),new Color(.68f,.32f,1),new Color(.45f,.86f,.22f)};
            string[] names={"01 // TRANSIT QUARTER","02 // SKYLINE BRIDGE","03 // APEX DISTRICT","04 // GARDEN LOOP"};
            for(int sector=0;sector<4;sector++)
            {
                var n=nodes[sector*nodes.Length/4+5];
                var board=new GameObject(names[sector]);board.transform.SetParent(parent,false);board.transform.position=n.Position+n.Right*(n.TrackWidth*.5f+3)+Vector3.up*4;
                board.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                RacingArt.Box(board.transform,"Sign backing",Vector3.zero,new Vector3(12,2.6f,.18f),metal,.2f);
                Sign(board.transform,names[sector],new Vector3(0,0,-.12f),.70f,colors[sector]);
                var accent=RacingArt.Box(board.transform,"Living sign edge",new Vector3(0,-1.1f,-.16f),new Vector3(11,.08f,.08f),RacingArt.Material(colors[sector],.3f,true),.01f);
                accent.AddComponent<RacingBillboardPulse>();
            }
            var lane=RacingArt.Material(new Color(.69f,.77f,.82f),.12f);
            for(int i=0;i<nodes.Length;i+=2)
            {
                var n=nodes[i];if(i>=56&&i<=61)continue;
                for(int side=-1;side<=1;side+=2)
                {
                    var mark=RacingArt.Box(parent,"Lane guide",n.Position+n.Right*side*3.7f+Vector3.up*.03f,new Vector3(.12f,.025f,2.5f),lane,.01f);
                    mark.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                }
            }
        }

        public static void ApplyLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.16f,.19f,.28f);
            RenderSettings.ambientEquatorColor=new Color(.16f,.18f,.23f);
            RenderSettings.ambientGroundColor=new Color(.045f,.05f,.08f);
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.00145f;
            RenderSettings.fogColor=new Color(.075f,.08f,.14f);
            var moon=FindFirstObjectByType<Light>();
            if(moon==null)moon=new GameObject("Night City Moon").AddComponent<Light>();
            moon.type=LightType.Directional;
            moon.intensity=.85f;
            moon.color=new Color(.93f,.96f,1f);
            moon.transform.rotation=Quaternion.Euler(52,-40,0);
            moon.shadows=LightShadows.Soft;
            RenderSettings.sun=moon;
            var shader=Resources.Load<Shader>("RacingNightSky");
            if(shader==null)shader=Shader.Find("Skybox/Procedural");
            if(shader!=null)
            {
                var sky=new Material(shader){name="Night City sky"};
                if(sky.HasProperty("_SkyTint"))sky.SetColor("_SkyTint",new Color(.06f,.04f,.14f));
                if(sky.HasProperty("_GroundColor"))sky.SetColor("_GroundColor",new Color(.02f,.01f,.04f));
                if(sky.HasProperty("_Exposure"))sky.SetFloat("_Exposure",.58f);
                if(sky.HasProperty("_AtmosphereThickness"))sky.SetFloat("_AtmosphereThickness",.65f);
                RenderSettings.skybox=sky;
            }
        }
    }
    public sealed class RacingTrafficFlow:MonoBehaviour
    {
        public Vector3 Direction=Vector3.forward;
        public float Speed=28f;
        public float Length=120f;
        Vector3 startLocal;
        void Awake(){startLocal=transform.localPosition;gameObject.AddComponent<RacingAnimatedPart>();}
        void Update()
        {
            float offset=Mathf.Repeat(Time.time*Speed,Length);
            transform.localPosition=startLocal+Direction*offset;
        }
    }
    public sealed class RacingBillboardPulse:MonoBehaviour
    {
        MeshRenderer display;MaterialPropertyBlock block;Color color;float next;
        void Awake(){gameObject.AddComponent<RacingAnimatedPart>();display=GetComponent<MeshRenderer>();color=display.sharedMaterial.color;block=new MaterialPropertyBlock();display.shadowCastingMode=ShadowCastingMode.Off;}
        void Update(){if(Time.time<next)return;next=Time.time+.2f;block.SetColor("_EmissionColor",color*(.4f+.2f*Mathf.Sin(Time.time*.8f+transform.position.x)));display.SetPropertyBlock(block);}
    }
    public sealed class RacingEnvironmentMotion:MonoBehaviour
    {
        public bool Water;Quaternion rotation;Vector3 scale;
        void Awake(){rotation=transform.localRotation;scale=transform.localScale;gameObject.AddComponent<RacingAnimatedPart>();}
        void Update(){if(Water)transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(Time.time*2.7f)*.015f),scale.y,scale.z);else transform.localRotation=rotation*Quaternion.Euler(Mathf.Sin(Time.time*2.1f+transform.position.x)*3,0,Mathf.Sin(Time.time*3.3f+transform.position.z)*4);}
    }
}
