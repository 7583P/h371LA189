#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class NightCityRedesignContracts
    {
        public static int World(RaceManager manager, string output)
        {
            int failures=0; var lines=new List<string>();
            void Check(bool passed,string text) { lines.Add((passed?"PASS ":"FAIL ")+text); if(!passed)failures++; }
            var graph=manager.ProgressTracker.TrackGraph;
            Check(graph.Shortcuts.Length==2,"Exactly two authored route choices");
            Check(graph.TotalLength>2500&&graph.TotalLength<3100,"Grand Circuit is a complete, readable 2.5-3.1 km loop");
            bool widths=true,grades=true,road=true,curvature=true;float maxGrade=0,minRadius=float.MaxValue;
            var hits=new RaycastHit[24];
            for(int i=0;i<graph.NodeCount;i++)
            {
                var n=graph.GetNode(i);var next=graph.GetNode(i+1);var prev=graph.GetNode(i-1);
                widths&=n.TrackWidth>=24.9f&&n.TrackWidth<=30.1f;
                float run=Vector3.ProjectOnPlane(next.Position-n.Position,Vector3.up).magnitude;
                float grade=Mathf.Abs(next.Position.y-n.Position.y)/Mathf.Max(.01f,run);maxGrade=Mathf.Max(maxGrade,grade);grades&=grade<.15f;
                Vector3 a=Vector3.ProjectOnPlane(n.Position-prev.Position,Vector3.up),b=Vector3.ProjectOnPlane(next.Position-n.Position,Vector3.up);
                float area=Vector3.Cross(a,b).magnitude;
                float radius=area>.001f?a.magnitude*b.magnitude*(a+b).magnitude/(2*area):10000;
                minRadius=Mathf.Min(minRadius,radius);curvature&=radius>n.TrackWidth*.5f+3;
                bool supported=false;int count=Physics.RaycastNonAlloc(n.Position+Vector3.up*.8f,Vector3.down,hits,1.4f,~0,QueryTriggerInteraction.Ignore);
                for(int h=0;h<count;h++) supported|=hits[h].collider.GetComponent<RacingSurface>()?.Kind==RacingSurfaceKind.Road&&hits[h].normal.y>.65f;
                road&=supported;
            }
            Check(widths,"Main road has 25-30 m of declared race space");
            Check(grades,"Vertical transitions remain below 15% grade (max="+maxGrade.ToString("F3")+")");
            Check(curvature,"Road corners exceed half-width plus clearance (min radius="+minRadius.ToString("F1")+"m)");
            Check(road,"Every main-route sample has physical road support; no mandatory void gaps");
            var asphalt=GameObject.Find("Grand Circuit asphalt");var mesh=asphalt?.GetComponent<MeshFilter>()?.sharedMesh;
            bool sameWidth=mesh!=null;
            if(mesh!=null)
            {
                var vertices=mesh.vertices;
                for(int i=0;i<graph.NodeCount;i++)sameWidth&=Mathf.Abs(Vector3.Distance(vertices[i*2],vertices[i*2+1])-graph.GetNode(i).TrackWidth)<.01f;
            }
            Check(sameWidth,"Rendered asphalt, physical mesh and AI/minimap widths agree");
            foreach(var route in graph.Shortcuts)
            {
                float length=0;for(int i=1;i<route.pathPoints.Length;i++)length+=Vector3.Distance(route.pathPoints[i-1],route.pathPoints[i]);
                float main=graph.GetNode(route.exitNodeIndex).CumulativeDistance-graph.GetNode(route.entryNodeIndex).CumulativeDistance;
                Check(main-length>35,route.displayName+" saves more than 35 physical meters ("+(main-length).ToString("F1")+"m)");
                if(route.requiresJump)
                {
                    float steepest=0;
                    for(int i=1;i<route.pathPoints.Length;i++)
                    {
                        var delta=route.pathPoints[i]-route.pathPoints[i-1];
                        steepest=Mathf.Max(steepest,Mathf.Abs(delta.y)/Mathf.Max(.01f,Vector3.ProjectOnPlane(delta,Vector3.up).magnitude));
                    }
                    Check(steepest<.15f,route.displayName+" supported ramps remain below 15% grade (max="+steepest.ToString("F3")+")");
                    Check(Mathf.Abs(route.pathPoints[route.launchNodeIndex].y-route.pathPoints[route.landingNodeIndex].y)<.05f,"Jump launch and landing have matching height");
                }
            }
            Check(JsonUtility.ToJson(RacingStatsCalculator.Calculate("formula-01","wheel-plasma"))==JsonUtility.ToJson(RacingStatsCalculator.Calculate("formula-01","wheel-whity")),"New Whity identity preserves all nine legacy META performance values");
            Check(RacingStatsCalculator.NormalizeWheelId("wheel-plasma")=="wheel-plasma","Existing saved Plasma selections remain valid");
            int elite=0;bool appearance=true;
            foreach(var identity in manager.LobbyRacers) if(!identity.isHuman&&identity.buildCategory==MetaBuildCategory.Meta)
            {elite++;appearance&=identity.paintColor==RacingVehicleFactory.MetaNavy&&identity.wheelId=="wheel-whity";}
            Check(elite>=10&&appearance,"Elite lobby keeps at least ten dark-blue DAPEX / Whity bots");
            File.WriteAllLines(Path.Combine(output,"night-city-contracts.txt"),lines);return failures;
        }
    }
}
#endif
