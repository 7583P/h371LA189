using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class VerdantCircuit
    {
        public const string Id="verdant_run", Name="VERDANT RUN / TECHNICAL LOOP";
        // Bracket the northwest switchback instead of forcing the driving
        // target onto a gate at its apex. All cuts retain the same gate order.
        public static readonly int[] Checkpoints={0,48,96,144,176,200,208};
        public static TrackNode[] CreateNodes()
        {
            Vector3[] control={
                P(0,0),P(0,100),P(90,180),P(195,190),P(250,110),P(150,60),P(180,-25),
                P(300,45),P(395,120),P(460,70),P(450,-45),P(365,-85),P(310,-100),
                P(325,-160),P(420,-225),P(355,-310),P(240,-285),P(180,-185),
                P(75,-240),P(-55,-320),P(-145,-255),P(-120,-145),P(-200,-60),
                P(-190,90),P(-105,125),P(-70,30),P(-75,-65),P(0,-70)
            };
            var positions=RacingTrackGeometry.SmoothLoop(control,8);var nodes=new TrackNode[positions.Length];
            for(int i=0;i<nodes.Length;i++)
            {
                Vector3 forward=(positions[(i+1)%nodes.Length]-positions[(i+nodes.Length-1)%nodes.Length]).normalized;
                Vector3 ahead=(positions[(i+3)%nodes.Length]-positions[i]).normalized;
                Vector3 behind=(positions[i]-positions[(i+nodes.Length-3)%nodes.Length]).normalized;
                float turn=Vector3.SignedAngle(behind,ahead,Vector3.up);
                float authoredSpeed=158-Mathf.Clamp(Mathf.Abs(turn)*1.4f,0,88);
                // The long approach to the northwest switchback otherwise
                // arrives at 160 km/h after the previous bending straight.
                if(i>=176&&i<=201)authoredSpeed=Mathf.Min(authoredSpeed,100);
                nodes[i]=new TrackNode{Position=positions[i],Forward=forward,Right=Vector3.Cross(Vector3.up,forward).normalized,Up=Vector3.up,
                    TrackWidth=26,TargetSpeedKmh=authoredSpeed,
                    OptimalLineOffset=Mathf.Sign(turn)*Mathf.Clamp01(Mathf.Abs(turn)/45)*.22f,IsDriftZone=Mathf.Abs(turn)>10};
            }
            return nodes;
        }
        // Give the technical bends room for the shared vehicle yaw authority.
        // The initial compact layout folded a 26 m road around a 4 m radius.
        static Vector3 P(float x,float z)=>new Vector3(x*1.75f,4,z*1.75f);
        public static TrackShortcut[] CreateShortcuts(TrackNode[] nodes)
        {
            var routes=new[]{
                new TrackShortcut{shortcutId="orchard_cut",displayName="ORCHARD CUT",entryNodeIndex=9,exitNodeIndex=45,width=18,minSkillRequired=.45f,riskLevel=.30f,requiresBoost=false},
                new TrackShortcut{shortcutId="meadow_cut",displayName="MEADOW CUT",entryNodeIndex=49,exitNodeIndex=96,width=18,minSkillRequired=.57f,riskLevel=.43f,requiresBoost=false},
                new TrackShortcut{shortcutId="clover_cut",displayName="CLOVER CUT",entryNodeIndex=99,exitNodeIndex=137,width=18,minSkillRequired=.62f,riskLevel=.48f,requiresBoost=false}
            };
            foreach(var route in routes)
            {
                var entry=nodes[route.entryNodeIndex];var exit=nodes[route.exitNodeIndex];
                float handle=Mathf.Clamp(Vector3.Distance(entry.Position,exit.Position)*.22f,35,65);
                route.pathPoints=RacingTrackGeometry.ShortcutPath(entry,exit,false,.32f,handle);
                for(int i=0;i<route.pathPoints.Length;i++)route.pathPoints[i].y=4.015f;
                route.exitWidth=nodes[route.exitNodeIndex].TrackWidth;
            }
            return routes;
        }
    }

    public static class RacingTrackSelection
    {
        static string selected="night_city";
        public static string Id=>selected;
        public static string ShortName=>selected==VerdantCircuit.Id?"VERDANT RUN":"NIGHT CITY SKY";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            selected="night_city";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var args=System.Environment.GetCommandLineArgs();
            int index=System.Array.IndexOf(args,"--racing-track");
            if(index>=0&&index+1<args.Length)Select(args[index+1]);
#endif
        }
        public static void Select(string id) { if(id=="night_city"||id==VerdantCircuit.Id)selected=id; }
    }
}
