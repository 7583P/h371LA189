#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimeHillRacing.Racing3D;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.EditorTools
{
    // Directed contracts use the production progress authority and actual map
    // meshes. They do not pretend that scripted gate input is a driven race.
    public static class RacingShortcutIntegrityContracts
    {
        public static void Run(ClosureContractReport report,TrackSplineGraph graph,string name)
        {
            var root=new GameObject("Checkpoint and map contracts / "+name);
            var tracker=root.AddComponent<TrackProgressTracker>();tracker.Initialize(graph,1,graph.CheckpointNodeIndices.Length);
            int count=tracker.TotalCheckpointsCount;
            var car=new GameObject("Progress fixture",typeof(RacingVehiclePhysics)).GetComponent<RacingVehiclePhysics>();car.transform.SetParent(root.transform);
            RacerProgress Begin(string id)
            {
                var p=tracker.RegisterRacer(new RacerIdentity(id,id,"car-01","wheel-default",9700,true,1,1),car,null);
                tracker.NotifyCheckpoint(car,0,true,1);return p;
            }
            // Each case has a fresh tracker so FindRacer resolves its one car.
            void Clear()=>tracker.Initialize(graph,1,count);
            void FinishSequence(){for(int gate=1;gate<count;gate++)tracker.NotifyCheckpoint(car,gate,false,10+gate);tracker.NotifyCheckpoint(car,0,true,30);}
            try
            {
                var p=Begin("main");FinishSequence();
                report.Check(p.Finished&&p.CheckpointVisits==count+1,name+" A: official route with every gate in order is valid");
                Clear();p=Begin("small-cut");
                car.transform.position=Vector3.Lerp(graph.GetNode(2).Position,graph.GetNode(5).Position,.5f);
                tracker.UpdateProgress(2);bool noGrant=p.CheckpointVisits==1;FinishSequence();
                report.Check(noGrant&&p.Finished,name+" B: small unregistered cut is legal when the required sequence is crossed");
                Clear();p=Begin("grass");
                foreach(var route in graph.Shortcuts){car.transform.position=route.pathPoints[route.pathPoints.Length/2];tracker.UpdateProgress(2);}
                noGrant=p.CheckpointVisits==1;FinishSequence();
                report.Check(noGrant&&p.Finished,name+" C: grass/alternate path positions grant no gates and preserve ordered-lap eligibility");
                Clear();p=Begin("movement-tech");
                car.transform.position=graph.GetNode(5).Position+Vector3.up*4;tracker.UpdateProgress(2);FinishSequence();
                report.Check(p.Finished,name+" D: airborne hop/glitch-style movement needs no route whitelist; ordered gates remain authoritative");
                Clear();p=Begin("huge-skip");tracker.NotifyCheckpoint(car,1,false,2);
                tracker.NotifyCheckpoint(car,count-1,false,3);tracker.NotifyCheckpoint(car,0,true,4);
                report.Check(!p.Finished&&p.CheckpointsPassedCount==1&&p.CheckpointVisits==2,name+" E: early checkpoint to final sector skip is rejected, with no silently granted gates");
                Clear();p=Begin("projection-skip");
                for(int i=0;i<200;i++){car.transform.position=graph.GetNode(graph.NodeCount-3).Position;tracker.UpdateProgress(2);}
                tracker.NotifyCheckpoint(car,count-1,false,3);tracker.NotifyCheckpoint(car,0,true,4);
                report.Check(!p.Finished&&p.CheckpointVisits==1&&p.CheckpointsPassedCount==0,name+" F: geographically reaching the final sector does not infer checkpoint crossings");
                Clear();p=Begin("early-finish");tracker.NotifyCheckpoint(car,0,true,2);tracker.NotifyCheckpoint(car,0,true,3);
                report.Check(!p.Finished&&p.CurrentLap==1&&p.CheckpointVisits==1,name+" G: finish without required gates never counts a lap");
                Clear();p=Begin("map-player");car.transform.position=graph.GetNode(4).Position;tracker.UpdateProgress(2);
                Minimap(report,root.transform,tracker,car,name);
            }
            finally {Object.DestroyImmediate(root);}
        }

        static List<UIVertex> Mesh(Graphic graphic)
        {
            using(var vertices=new VertexHelper())
            {
                graphic.GetType().GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{typeof(VertexHelper)},null).Invoke(graphic,new object[]{vertices});
                var result=new List<UIVertex>();vertices.GetUIVertexStream(result);return result;
            }
        }
        static void Minimap(ClosureContractReport report,Transform parent,TrackProgressTracker tracker,RacingVehiclePhysics car,string name)
        {
            var graph=tracker.TrackGraph;var nodes=Enumerable.Range(0,graph.NodeCount).Select(graph.GetNode).ToArray();
            var routes=graph.Shortcuts;var gates=graph.CheckpointNodeIndices;
            var map=new GameObject("Actual main-route map",typeof(RectTransform),typeof(CanvasRenderer),typeof(RacingRaceMap)).GetComponent<RacingRaceMap>();map.transform.SetParent(parent,false);
            map.rectTransform.sizeDelta=new Vector2(240,180);map.Bind(tracker);
            var before=Mesh(map);
            try
            {
                graph.InitializeFromNodes(nodes,null,gates);map.Bind(tracker);var after=Mesh(map);
                bool same=before.Count==after.Count&&before.Count==(graph.NodeCount*2+1)*6;
                for(int i=0;same&&i<before.Count;i++)same=before[i].position==after[i].position&&before[i].color.Equals(after[i].color);
                report.Check(same,name+" minimap: actual mesh contains only two main-road contours and finish, identical with/without shortcuts");
                var markers=map.GetComponentInChildren<RacingRaceMapMarkers>();var first=Mesh(markers);
                car.transform.position=graph.GetNode(16).Position;tracker.UpdateProgress(3);var moved=Mesh(markers);
                var project=typeof(RacingRaceMap).GetMethod("Project",BindingFlags.NonPublic|BindingFlags.Instance);
                Vector2 expected=(Vector2)project.Invoke(map,new object[]{car.transform.position});
                Vector3 center=Vector3.zero;for(int i=0;i<6;i++)center+=moved[i].position/6;
                report.Check((new Vector2(center.x,center.y)-expected).sqrMagnitude<.001f&&first[0].position!=moved[0].position,name+" minimap: actual human marker follows world position while the road mesh stays static");
            }
            finally {graph.InitializeFromNodes(nodes,routes,gates);}
        }
    }
}
#endif
