using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed partial class TrackSplineGraph
    {
        public const float ShortcutSpeedScale = .5f;
        // Geometry, never AI route intent: the same test applies to every car.
        public TrackShortcut OfficialShortcutAt(Vector3 point)
        {
            if(shortcuts==null||nodes==null||nodes.Length<2)return null;
            ProjectPosition(point,out int main,out _,out _);
            Vector3 a=nodes[main].Position,d=nodes[(main+1)%nodes.Length].Position-a;
            float t=d.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(point-a,d)/d.sqrMagnitude):0;
            float closest=(point-a-d*t).sqrMagnitude;TrackShortcut result=null;
            foreach(var route in shortcuts) {
                if(route==null||!route.IsValid())continue;
                for(int i=0;i<route.pathPoints.Length-1;i++) {
                    a=route.pathPoints[i];d=route.pathPoints[i+1]-a;
                    float length=d.sqrMagnitude;if(length<.0001f)continue;
                    float raw=Vector3.Dot(point-a,d)/length;
                    if((i==0&&raw<0)||(i==route.pathPoints.Length-2&&raw>1))continue;
                    t=Mathf.Clamp01(raw);Vector3 delta=point-a-d*t;
                    float width=route.WidthAt(i+t)*.5f;
                    // Permit the authored aerial gap, but not unrelated roads
                    // above/below a link. Closest main road wins shared portals.
                    if(Mathf.Abs(delta.y)>(route.requiresJump?12f:3f)||Vector3.ProjectOnPlane(delta,Vector3.up).sqrMagnitude>width*width)continue;
                    float distance=delta.sqrMagnitude;
                    if(distance+.001f<closest){closest=distance;result=route;}
                }
            }
            return result;
        }
    }
}
