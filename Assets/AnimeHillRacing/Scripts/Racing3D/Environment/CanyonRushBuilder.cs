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
                new Vector3(0,6,0),new Vector3(0,6,110),new Vector3(25,7,215),new Vector3(85,8,290),
                new Vector3(175,9,320),new Vector3(260,11,285),new Vector3(310,13,210),new Vector3(315,19,135),
                new Vector3(285,11,55),new Vector3(235,8,-15),new Vector3(195,7,-85),new Vector3(255,6,-165),
                new Vector3(220,6,-250),new Vector3(125,7,-280),new Vector3(45,9,-235),new Vector3(-45,11,-250),
                new Vector3(-120,10,-180),new Vector3(-100,8,-90),new Vector3(-35,6,-60),new Vector3(0,6,-40)
            };
            var positions=RacingTrackGeometry.SmoothLoop(control,Steps);var nodes=new TrackNode[positions.Length];
            for(int i=0;i<nodes.Length;i++)
            {
                Vector3 forward=(positions[(i+1)%positions.Length]-positions[(i+positions.Length-1)%positions.Length]).normalized;
                Vector3 ahead=(positions[(i+3)%positions.Length]-positions[i]).normalized;
                Vector3 behind=(positions[i]-positions[(i+positions.Length-3)%positions.Length]).normalized;
                float corner=Vector3.SignedAngle(behind,ahead,Vector3.up);float target=155-Mathf.Clamp(Mathf.Abs(corner)*1.25f,0,80);
                float width=21+Mathf.Sin(i*.075f)*2.5f;
                nodes[i]=new TrackNode{Position=positions[i],Forward=forward,Right=Vector3.Cross(Vector3.up,forward).normalized,Up=Vector3.up,TrackWidth=width,SafeLineOffset=0,OptimalLineOffset=Mathf.Sign(corner)*Mathf.Clamp01(Mathf.Abs(corner)/40)*.28f,TargetSpeedKmh=target,IsDriftZone=Mathf.Abs(corner)>15};
            }
            var groundCut=new TrackShortcut{shortcutId="ridge_cut",displayName="RIDGE CUT",entryNodeIndex=11*Steps,exitNodeIndex=13*Steps,requiresBoost=true,minSkillRequired=.5f,riskLevel=.32f,pathPoints=new[]{control[11],new Vector3(200,6,-215),control[13]}};
            // Jump-based shortcut: Neon Skyway Bypass where an intentional jump launches over the outer bend
            var skywayJump=new TrackShortcut{shortcutId="skyway_jump",displayName="NEON SKYWAY JUMP",entryNodeIndex=3*Steps+2,exitNodeIndex=5*Steps-2,requiresBoost=true,minSkillRequired=.65f,riskLevel=.45f,pathPoints=new[]{control[3],new Vector3(170,14,295),control[5]}};
            // Boost-gated shortcut 1: Digital Waterfall Cut (Boost-gated void chasm leap)
            var waterfallCut=new TrackShortcut{shortcutId="waterfall_gap",displayName="DIGITAL WATERFALL CUT",entryNodeIndex=1*Steps+2,exitNodeIndex=2*Steps+6,requiresBoost=true,minSkillRequired=.55f,riskLevel=.40f,pathPoints=new[]{control[1]+Vector3.up*.2f,new Vector3(14,8.5f,165),new Vector3(45,9.5f,235),control[3]-Vector3.up*.2f}};
            // Boost-gated shortcut 2: Apex Overpass Ramp (Boost-gated elevated sky bridge)
            var overpassLaunch=new TrackShortcut{shortcutId="overpass_launch",displayName="APEX OVERPASS RAMP",entryNodeIndex=15*Steps+1,exitNodeIndex=18*Steps-1,requiresBoost=true,minSkillRequired=.70f,riskLevel=.50f,pathPoints=new[]{control[15]+Vector3.up*.3f,new Vector3(-68,16.5f,-215),new Vector3(-85,15.2f,-135),control[18]+Vector3.up*.2f}};

            var graphRoot=new GameObject("Night City spline graph");graphRoot.transform.SetParent(transform,false);Graph=graphRoot.AddComponent<TrackSplineGraph>();Graph.InitializeFromNodes(nodes,new[]{groundCut,skywayJump,waterfallCut,overpassLaunch});
            var gridRoot=new GameObject("12 slot starting grid");gridRoot.transform.SetParent(transform,false);gridRoot.transform.position=nodes[0].Position;gridRoot.transform.rotation=Quaternion.LookRotation(nodes[0].Forward,Vector3.up);Grid=gridRoot.AddComponent<StartingGrid>();Grid.Initialize(Graph);

            // Night City Materials with high readable contrast
            Material road=RacingArt.Material(new Color(.11f,.13f,.16f),.45f);
            Material shoulder=RacingArt.Material(new Color(.06f,.07f,.10f),.2f);
            Material cyanGlow=RacingArt.Material(RacingDesign.Cyan,.85f,true);
            Material yellowStripe=RacingArt.Material(RacingDesign.Highlight,.9f,true);
            Material magentaGlow=RacingArt.Material(new Color(.95f,.15f,.75f),.85f,true);
            Material metalDark=RacingArt.Material(new Color(.05f,.07f,.11f),.7f);

            // Jump pads / launch kicker
            int gapStart=7*Steps+2,gapEnd=7*Steps+3;
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
            RacingTrackGeometry.Ribbon(transform,"Left shoulder",nodes,-15,-10.8f,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"Right shoulder",nodes,10.8f,15,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd);
            RacingTrackGeometry.Ribbon(transform,"City left slope",nodes,-15,-45,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,14);
            RacingTrackGeometry.Ribbon(transform,"City right slope",nodes,15,45,shoulder,RacingSurfaceKind.Dirt,true,true,gapStart,gapEnd,14);

            var scenery=new GameObject("Batched night city scenery");scenery.transform.SetParent(transform,false);
            var rng=new DeterministicRng(90173);

            // Procedural Neon Skyscrapers surrounding the circuit
            BuildCitySkyscrapers(nodes,scenery.transform,rng,metalDark,cyanGlow,magentaGlow,yellowStripe);

            // City ground basin
            var ground=RacingArt.Box(scenery.transform,"City basin",new Vector3(90,-16,0),new Vector3(1400,1,1400),shoulder,.5f);
            ground.AddComponent<BoxCollider>().size=new Vector3(1400,1,1400);ground.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Dirt;

            BuildBarriers(nodes,scenery.transform,metalDark,magentaGlow,cyanGlow,new[]{groundCut,skywayJump,waterfallCut,overpassLaunch});
            BuildGate(nodes[0],scenery.transform,metalDark,cyanGlow,yellowStripe);
            BuildBridge(nodes,scenery.transform,metalDark,cyanGlow);
            BuildTunnel(nodes,scenery.transform,metalDark,magentaGlow,cyanGlow);
            BuildShortcuts(groundCut,skywayJump,waterfallCut,overpassLaunch,scenery.transform,road,cyanGlow,magentaGlow,yellowStripe);
            BuildStreetLights(nodes,scenery.transform,metalDark,cyanGlow);
            BuildDigitalCascade(nodes[3].Position+nodes[3].Right*-58,scenery.transform,cyanGlow);
            BuildRaceGantries(nodes,scenery.transform,metalDark,cyanGlow,yellowStripe);
            BuildApexKerbs(nodes,scenery.transform,cyanGlow,yellowStripe,metalDark);
            BuildStarrySky(scenery.transform);
            BuildTrafficStreaks(scenery.transform,cyanGlow,magentaGlow,yellowStripe);

            int[] checkpoints={0,3*Steps,6*Steps,9*Steps,14*Steps,17*Steps};
            for(int i=0;i<checkpoints.Length;i++)
            {
                var n=nodes[checkpoints[i]];var cp=new GameObject(i==0?"Finish gate":"Checkpoint "+i);cp.transform.SetParent(transform,false);cp.transform.position=n.Position+Vector3.up*2;cp.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);
                var trigger=cp.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(n.TrackWidth+10,9,8);cp.AddComponent<CheckpointTrigger3D>().Configure(i,i==0);
            }
            foreach(int station in new[]{1*Steps,5*Steps,10*Steps,15*Steps})for(int lane=-1;lane<=1;lane++)ItemBox.Create(transform,nodes[station].Position+nodes[station].Right*lane*4.5f+Vector3.up*1.1f);
            for(int row=0;row<6;row++)
            {
                var n=Graph.SampleDistance(Graph.TotalLength-8-row*7);
                for(int side=-1;side<=1;side+=2){var stripe=RacingArt.Box(scenery.transform,"Grid mark",n.Position+n.Right*side*1.6f+Vector3.up*.025f,new Vector3(1.5f,.03f,.14f),yellowStripe,.02f);stripe.transform.rotation=Quaternion.LookRotation(n.Forward,Vector3.up);}
            }

            RacingArt.Combine(scenery.transform);
            ApplyLighting();
            Debug.Log($"NIGHT_CITY_BUILT nodes={nodes.Length} length={Graph.TotalLength:F1} checkpoints={checkpoints.Length} shortcuts=4");
        }

        static void BuildCitySkyscrapers(TrackNode[] nodes,Transform parent,DeterministicRng rng,Material dark,Material cyan,Material magenta,Material yellow)
        {
            Material[] neonWindows=new[]{cyan,magenta,yellow};

            // Tier 1: Trackside commercial buildings (48 towers)
            for(int i=0;i<48;i++)
            {
                TrackNode n=nodes[(i*7)%nodes.Length];int side=(i&1)==0?-1:1;
                float dist=rng.Range(36,68);
                Vector3 basePos=n.Position+n.Right*side*dist-Vector3.up*rng.Range(2,6);
                float width=rng.Range(16,32);
                float depth=rng.Range(16,30);
                float height=rng.Range(42,95);

                var towerRoot=new GameObject("Skyscraper_"+i);
                towerRoot.transform.SetParent(parent,false);
                towerRoot.transform.position=basePos;
                towerRoot.transform.rotation=Quaternion.Euler(0,rng.Range(0,360),0);

                var block=RacingArt.Box(towerRoot.transform,"Tower core",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),dark,.2f);
                block.AddComponent<BoxCollider>().size=new Vector3(width,height,depth);

                Material winMat=neonWindows[i%neonWindows.Length];
                int floors=Mathf.Min(8,(int)(height/12));
                for(int f=1;f<floors;f++)
                {
                    float winY=f*11f;
                    RacingArt.Box(towerRoot.transform,"Neon Window Band",new Vector3(0,winY,depth*.51f),new Vector3(width*.75f,1.3f,.15f),winMat,.05f);
                    RacingArt.Box(towerRoot.transform,"Neon Window Band Back",new Vector3(0,winY,-depth*.51f),new Vector3(width*.75f,1.3f,.15f),winMat,.05f);
                }

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

                var midRoot=new GameObject("MidTower_"+i);
                midRoot.transform.SetParent(parent,false);
                midRoot.transform.position=basePos;
                midRoot.transform.rotation=Quaternion.Euler(0,rng.Range(0,360),0);

                RacingArt.Box(midRoot.transform,"Mid Core",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),dark,.3f);
                Material winMat=neonWindows[(i+1)%neonWindows.Length];
                int floors=Mathf.Min(12,(int)(height/14));
                for(int f=1;f<floors;f+=2)
                {
                    float winY=f*12.5f;
                    RacingArt.Box(midRoot.transform,"Mid Window Strip",new Vector3(0,winY,depth*.51f),new Vector3(width*.8f,1.5f,.2f),winMat,.05f);
                }
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

                var megaRoot=new GameObject("MegaMonolith_"+i);
                megaRoot.transform.SetParent(parent,false);
                megaRoot.transform.position=basePos;
                megaRoot.transform.rotation=Quaternion.Euler(0,rng.Range(0,360),0);

                RacingArt.Box(megaRoot.transform,"Monolith Core",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),dark,.4f);
                Material glow=(i%2==0)?cyan:magenta;
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
                    var wall=RacingArt.Box(hoop.transform,"Tunnel flank",new Vector3(s*13,4,0),new Vector3(3.5f,8,6),metal,.4f);
                    wall.AddComponent<BoxCollider>().size=new Vector3(3.5f,8,6);
                    RacingArt.Box(hoop.transform,"Neon rib",new Vector3(s*11.2f,3.5f,0),new Vector3(.15f,7f,1.2f),s==1?cyan:magenta,.05f);
                }
                var ceiling=RacingArt.Box(hoop.transform,"Tunnel roof",new Vector3(0,8.5f,0),new Vector3(28,2.1f,6),metal,.4f);
                ceiling.AddComponent<BoxCollider>().size=new Vector3(28,2.1f,6);
            }
        }

        void BuildShortcuts(TrackShortcut groundCut,TrackShortcut skywayJump,TrackShortcut waterfallCut,TrackShortcut overpassLaunch,Transform parent,Material road,Material cyan,Material magenta,Material yellow)
        {
            // 1. Ground Ridge Cut
            for(int i=0;i<groundCut.pathPoints.Length-1;i++)
            {
                Vector3 a=groundCut.pathPoints[i],b=groundCut.pathPoints[i+1];
                var slab=RacingArt.Box(parent,"Ridge cut shelf",(a+b)*.5f-Vector3.up*.15f,new Vector3(8,.3f,Vector3.Distance(a,b)+1),road,.15f);
                slab.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                slab.AddComponent<BoxCollider>().size=new Vector3(8,.3f,Vector3.Distance(a,b)+1);
                slab.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Road;
            }
            Sign(parent,"RIDGE CUT",groundCut.pathPoints[0]+Vector3.up*3,.7f,RacingDesign.Cyan);

            // 2. Neon Skyway Jump Shortcut (Jump-assisted elevated sky bridge)
            for(int i=0;i<skywayJump.pathPoints.Length-1;i++)
            {
                Vector3 a=skywayJump.pathPoints[i],b=skywayJump.pathPoints[i+1];
                var skybridge=RacingArt.Box(parent,"Skyway jump bridge",(a+b)*.5f+Vector3.up*.5f,new Vector3(7,.45f,Vector3.Distance(a,b)+2),cyan,.2f);
                skybridge.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                skybridge.AddComponent<BoxCollider>().size=new Vector3(7,.45f,Vector3.Distance(a,b)+2);
                skybridge.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Road;
            }
            Sign(parent,"SKYWAY JUMP",skywayJump.pathPoints[0]+Vector3.up*3.5f,.75f,RacingDesign.Highlight);
            var skyKicker=new GameObject("Skyway Jump Pad");skyKicker.transform.SetParent(parent,false);
            skyKicker.transform.position=skywayJump.pathPoints[0];
            skyKicker.transform.rotation=Quaternion.LookRotation(skywayJump.pathPoints[1]-skywayJump.pathPoints[0],Vector3.up);
            var skyTrig=skyKicker.AddComponent<BoxCollider>();skyTrig.isTrigger=true;skyTrig.center=new Vector3(0,1.2f,0);skyTrig.size=new Vector3(8,3,4);
            skyKicker.AddComponent<RacingJumpPad>();
            RacingArt.Box(skyKicker.transform,"Jump kicker marker",new Vector3(0,.05f,0),new Vector3(6,.15f,.15f),magenta);

            // 3. Digital Waterfall Cut (Boost Leap over Void Chasm)
            for(int i=0;i<waterfallCut.pathPoints.Length-1;i++)
            {
                Vector3 a=waterfallCut.pathPoints[i],b=waterfallCut.pathPoints[i+1];
                var ramp=RacingArt.Box(parent,"Waterfall cut road",(a+b)*.5f,new Vector3(7.5f,.35f,Vector3.Distance(a,b)+1.5f),road,.12f);
                ramp.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                ramp.AddComponent<BoxCollider>().size=new Vector3(7.5f,.35f,Vector3.Distance(a,b)+1.5f);
                ramp.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Road;
                for(int s=-1;s<=1;s+=2)
                {
                    var rail=RacingArt.Box(parent,"Waterfall rail",(a+b)*.5f+Vector3.up*.35f+ramp.transform.right*(s*3.75f),new Vector3(.18f,.5f,Vector3.Distance(a,b)+1f),cyan,.05f);
                    rail.transform.rotation=ramp.transform.rotation;
                }
            }
            Sign(parent,"WATERFALL CUT // BOOST GAP",waterfallCut.pathPoints[0]+Vector3.up*3.8f,.72f,RacingDesign.Cyan);
            var wfKicker=new GameObject("Waterfall Kicker Pad");wfKicker.transform.SetParent(parent,false);
            wfKicker.transform.position=waterfallCut.pathPoints[1];
            wfKicker.transform.rotation=Quaternion.LookRotation(waterfallCut.pathPoints[2]-waterfallCut.pathPoints[1],Vector3.up);
            var wfTrig=wfKicker.AddComponent<BoxCollider>();wfTrig.isTrigger=true;wfTrig.center=new Vector3(0,1.2f,0);wfTrig.size=new Vector3(8,3,4);
            wfKicker.AddComponent<RacingJumpPad>();
            RacingArt.Box(wfKicker.transform,"Booster Arrow",new Vector3(0,.05f,0),new Vector3(6.5f,.08f,1.8f),yellow,.05f);

            // 4. Apex Overpass Ramp (High-Elevation Skyway Bypass)
            for(int i=0;i<overpassLaunch.pathPoints.Length-1;i++)
            {
                Vector3 a=overpassLaunch.pathPoints[i],b=overpassLaunch.pathPoints[i+1];
                var overpassSlab=RacingArt.Box(parent,"Overpass bridge",(a+b)*.5f,new Vector3(7.8f,.45f,Vector3.Distance(a,b)+2),road,.15f);
                overpassSlab.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                overpassSlab.AddComponent<BoxCollider>().size=new Vector3(7.8f,.45f,Vector3.Distance(a,b)+2);
                overpassSlab.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Road;
                for(int s=-1;s<=1;s+=2)
                {
                    var pylon=RacingArt.Box(parent,"Overpass Pylon",(a+b)*.5f-Vector3.up*8f+overpassSlab.transform.right*(s*3.9f),new Vector3(.8f,16f,.8f),cyan,.1f);
                    pylon.transform.rotation=overpassSlab.transform.rotation;
                    var rail=RacingArt.Box(parent,"Overpass Rail",(a+b)*.5f+Vector3.up*.45f+overpassSlab.transform.right*(s*3.9f),new Vector3(.2f,.65f,Vector3.Distance(a,b)+1f),magenta,.05f);
                    rail.transform.rotation=overpassSlab.transform.rotation;
                }
            }
            Sign(parent,"OVERPASS RAMP // BOOST REQUIRED",overpassLaunch.pathPoints[0]+Vector3.up*4.2f,.75f,RacingDesign.Highlight);
            var opKicker=new GameObject("Overpass Launch Pad");opKicker.transform.SetParent(parent,false);
            opKicker.transform.position=overpassLaunch.pathPoints[0]+(overpassLaunch.pathPoints[1]-overpassLaunch.pathPoints[0])*.3f;
            opKicker.transform.rotation=Quaternion.LookRotation(overpassLaunch.pathPoints[1]-overpassLaunch.pathPoints[0],Vector3.up);
            var opTrig=opKicker.AddComponent<BoxCollider>();opTrig.isTrigger=true;opTrig.center=new Vector3(0,1.2f,0);opTrig.size=new Vector3(8,3,4);
            opKicker.AddComponent<RacingJumpPad>();
            RacingArt.Box(opKicker.transform,"Overpass Launch Strip",new Vector3(0,.05f,0),new Vector3(7,.08f,2f),yellow,.05f);
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
                }
            }
        }

        static void Sign(Transform parent,string text,Vector3 point,float scale,Color color)
        {
            var sign=new GameObject(text);sign.transform.SetParent(parent,false);sign.transform.localPosition=point;sign.transform.localScale=Vector3.one*scale;var label=sign.AddComponent<TextMesh>();label.text=text;label.fontSize=52;label.characterSize=.11f;label.anchor=TextAnchor.MiddleCenter;label.color=color;label.font=RacingDesign.Font;sign.GetComponent<MeshRenderer>().sharedMaterial=RacingDesign.Font.material;
        }

        public static void ApplyLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.11f,.09f,.22f);
            RenderSettings.ambientEquatorColor=new Color(.13f,.08f,.24f);
            RenderSettings.ambientGroundColor=new Color(.03f,.02f,.05f);
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.0020f;
            RenderSettings.fogColor=new Color(.045f,.035f,.095f);
            var moon=FindFirstObjectByType<Light>();
            if(moon==null)moon=new GameObject("Night City Moon").AddComponent<Light>();
            moon.type=LightType.Directional;
            moon.intensity=1.05f;
            moon.color=new Color(.78f,.88f,1f);
            moon.transform.rotation=Quaternion.Euler(52,-40,0);
            moon.shadows=LightShadows.Soft;
            RenderSettings.sun=moon;
            var shader=Shader.Find("Skybox/Procedural");
            if(shader!=null)
            {
                var sky=new Material(shader){name="Night City sky"};
                sky.SetColor("_SkyTint",new Color(.06f,.04f,.14f));
                sky.SetColor("_GroundColor",new Color(.02f,.01f,.04f));
                sky.SetFloat("_Exposure",.58f);
                sky.SetFloat("_AtmosphereThickness",.65f);
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
        void Awake(){startLocal=transform.localPosition;}
        void Update()
        {
            float offset=Mathf.Repeat(Time.time*Speed,Length);
            transform.localPosition=startLocal+Direction*offset;
        }
    }
    public sealed class RacingEnvironmentMotion:MonoBehaviour
    {
        public bool Water;Quaternion rotation;Vector3 scale;
        void Awake(){rotation=transform.localRotation;scale=transform.localScale;gameObject.AddComponent<RacingAnimatedPart>();}
        void Update(){if(Water)transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(Time.time*2.7f)*.015f),scale.y,scale.z);else transform.localRotation=rotation*Quaternion.Euler(Mathf.Sin(Time.time*2.1f+transform.position.x)*3,0,Mathf.Sin(Time.time*3.3f+transform.position.z)*4);}
    }
}
