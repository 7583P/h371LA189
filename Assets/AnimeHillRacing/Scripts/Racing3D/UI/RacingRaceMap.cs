using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingRaceMap:MaskableGraphic
    {
        TrackProgressTracker tracker;Vector2 min,max;
        RacingRaceMapMarkers markers;
        public void Bind(TrackProgressTracker progress)
        {
            tracker=progress;min=new Vector2(float.MaxValue,float.MaxValue);max=new Vector2(float.MinValue,float.MinValue);
            for(int i=0;i<tracker.TrackGraph.NodeCount;i++){var p=tracker.TrackGraph.GetNode(i).Position;min=Vector2.Min(min,new Vector2(p.x,p.z));max=Vector2.Max(max,new Vector2(p.x,p.z));}
            raycastTarget=false;
            if(markers==null)
            {
                var overlay=new GameObject("Live racer markers",typeof(RectTransform));overlay.transform.SetParent(transform,false);
                var rect=overlay.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                markers=overlay.AddComponent<RacingRaceMapMarkers>();markers.raycastTarget=false;
            }
            markers.Bind(tracker,this);SetVerticesDirty();
        }
        // The authored road stays static. Only the separate marker mesh updates
        // at 30 Hz. The map deliberately reveals only the official main road.
        internal Vector2 Project(Vector3 p){Rect r=rectTransform.rect;float scale=Mathf.Min(r.width/Mathf.Max(1,max.x-min.x),r.height/Mathf.Max(1,max.y-min.y))*.86f;return r.center+(new Vector2(p.x,p.z)-(min+max)*.5f)*scale;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(tracker==null||tracker.TrackGraph==null||tracker.TrackGraph.NodeCount<2)return;
            int nodeCount=tracker.TrackGraph.NodeCount;
            // 1. Under-track road contour (depth background)
            Color trackRoadColor = new Color(.08f,.16f,.24f,.9f);
            for(int i=0;i<nodeCount;i++)
            {
                RacingEmblem.Line(vh,Project(tracker.TrackGraph.GetNode(i).Position),Project(tracker.TrackGraph.GetNode(i+1).Position),5.0f,trackRoadColor);
            }
            // 2. High-visibility track spline core
            Color trackCoreColor = new Color(.30f,.76f,.94f,.95f);
            for(int i=0;i<nodeCount;i++)
            {
                RacingEmblem.Line(vh,Project(tracker.TrackGraph.GetNode(i).Position),Project(tracker.TrackGraph.GetNode(i+1).Position),2.4f,trackCoreColor);
            }
            // Hidden, discovered and grass paths never contribute map geometry.
            // 3. Start / Finish line crossbar
            var startNode = tracker.TrackGraph.GetNode(0);
            Vector2 finishP = Project(startNode.Position);
            Vector2 finishRight = new Vector2(startNode.Right.x, startNode.Right.z).normalized * 5f;
            RacingEmblem.Line(vh, finishP - finishRight, finishP + finishRight, 3.2f, Color.white);

        }
    }
}
