from pathlib import Path
p = Path('Assets/AnimeHillRacing/Scripts/Racing3D/Environment/CanyonRushBuilder.cs')
s = p.read_text(encoding='utf-8-sig')
start = s.index('        void BuildShortcuts(')
end = s.index('        void BuildStreetLights(', start)
s = s[:start] + '''        static bool ClearSceneryPosition(Vector3 position, float radius, TrackNode[] nodes, TrackShortcut[] routes)
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
        void BuildShortcuts(TrackShortcut groundCut,TrackShortcut skywayJump,TrackShortcut waterfallCut,TrackShortcut overpassLaunch,Transform parent,Material road,Material cyan,Material magenta,Material yellow)
        {
            foreach (var route in new[]{groundCut,skywayJump,waterfallCut,overpassLaunch})
            {
                Material guidance=route==overpassLaunch?magenta:route==groundCut?yellow:cyan;
                for(int i=0;i<route.pathPoints.Length-1;i++)
                {
                    Vector3 a=route.pathPoints[i],b=route.pathPoints[i+1];
                    Quaternion rotation=Quaternion.LookRotation(b-a,Vector3.up);
                    float length=Vector3.Distance(a,b);
                    // Physical rough surface makes conserving an actual boost useful for every driver.
                    var slab=RacingArt.Box(parent,route.displayName+" rough deck",(a+b)*.5f-Vector3.up*.22f,new Vector3(10,.40f,length+2),road,.08f);
                    slab.transform.rotation=rotation;
                    slab.AddComponent<BoxCollider>().size=new Vector3(10,.40f,length+2);
                    slab.AddComponent<RacingSurface>().Kind=RacingSurfaceKind.Dirt;
                    for(int side=-1;side<=1;side+=2)
                    {
                        var edge=RacingArt.Box(parent,"Route edge guide",(a+b)*.5f+rotation*Vector3.right*side*4.7f+Vector3.up*.045f,new Vector3(.16f,.06f,length),guidance,.02f);
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
                Sign(sign.transform,route.displayName+" // BOOST",Vector3.zero,.58f,guidance.color);
                if (route.requiresJump)
                {
                    var pad=new GameObject(route.displayName+" launch strip");pad.transform.SetParent(parent,false);
                    pad.transform.position=route.pathPoints[0]+direction*18;
                    pad.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
                    var trigger=pad.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up;trigger.size=new Vector3(10,3,3);
                    pad.AddComponent<RacingJumpPad>();
                    RacingArt.Box(pad.transform,"Visible kicker",new Vector3(0,.08f,0),new Vector3(9,.12f,1),guidance,.03f);
                }
            }
        }

''' + s[end:]
p.write_text(s,encoding='utf-8')
