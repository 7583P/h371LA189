using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingRaceMapMarkers : MaskableGraphic
    {
        static readonly ProfilerMarker Marker=new ProfilerMarker("Racing.UI.Minimap.Markers");
        TrackProgressTracker tracker;RacingRaceMap map;float nextRefresh;
        public void Bind(TrackProgressTracker progress,RacingRaceMap projection)
        {tracker=progress;map=projection;SetVerticesDirty();}
        void Update()
        {if(tracker!=null&&Time.unscaledTime>nextRefresh){nextRefresh=Time.unscaledTime+.033f;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            using(Marker.Auto())
            {
                vh.Clear();if(tracker==null||map==null)return;
                var leaderboard=tracker.Leaderboard;
                for(int i=0;i<leaderboard.Count;i++)
                {
                    var racer=leaderboard[i];if(racer.Vehicle==null)continue;
                    Vector2 position=map.Project(racer.Vehicle.transform.position);
                    if(racer.Identity.isHuman)
                    {
                        Diamond(vh,position,6,RacingDesign.Gold);Diamond(vh,position,2.2f,Color.white);
                        Vector3 forward=racer.Vehicle.transform.forward;
                        RacingEmblem.Line(vh,position,position+new Vector2(forward.x,forward.z).normalized*9.5f,2,Color.white);
                    }
                    else Diamond(vh,position,3.2f,racer.RacePosition<=3?new Color(.92f,.92f,.95f,.95f):RacingDesign.Cyan);
                }
            }
        }
        static void Diamond(VertexHelper vh,Vector2 center,float size,Color color)
        {
            int index=vh.currentVertCount;
            vh.AddVert(center+new Vector2(0,size),color,Vector2.zero);vh.AddVert(center+new Vector2(size,0),color,Vector2.zero);
            vh.AddVert(center-new Vector2(0,size),color,Vector2.zero);vh.AddVert(center-new Vector2(size,0),color,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
    }
}
