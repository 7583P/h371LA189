using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingTrackGeometry
    {
        public static Vector3[] SmoothLoop(Vector3[] points, int steps)
        {
            Vector3[] samples = new Vector3[points.Length * steps];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = points[(i + points.Length - 1) % points.Length], b = points[i];
                Vector3 c = points[(i + 1) % points.Length], d = points[(i + 2) % points.Length];
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    samples[i * steps + s] = .5f * (2*b + (-a+c)*t + (2*a-5*b+4*c-d)*t*t + (-a+3*b-3*c+d)*t*t*t);
                }
            }
            return samples;
        }
        // Tangent-matched approaches surround a straight launch/landing corridor.
        // Geometry and navigation share these samples, including both exact endpoints.
        public static Vector3[] ShortcutPath(TrackNode entry, TrackNode exit, bool precision = false)
        {
            Vector3 chord = exit.Position - entry.Position;
            Vector3 direction = chord.normalized;
            // Short portals must turn toward the chord without doubling back.
            // The longer general-route handles produced an S at both ends of
            // the precision links, consuming the straight landing/braking room.
            float handle = precision ? Mathf.Clamp(chord.magnitude * .08f, 6, 14) : Mathf.Clamp(chord.magnitude * .18f, 18, 34);
            Vector3 first = entry.Position + chord * .28f, last = entry.Position + chord * .72f;
            var path = new Vector3[25];
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f, u = 1 - t;
                path[i] = u*u*u*entry.Position + 3*u*u*t*(entry.Position + entry.Forward*handle) +
                    3*u*t*t*(first - direction*handle) + t*t*t*first;
                path[16+i] = u*u*u*last + 3*u*u*t*(last + direction*handle) +
                    3*u*t*t*(exit.Position - exit.Forward*handle) + t*t*t*exit.Position;
                path[8+i] = Vector3.Lerp(first,last,t);
            }
            return path;
        }
        public static void MatchRoadPortals(TrackShortcut route,TrackNode[] road)
        {
            var points=route.pathPoints;float launchHeight=points[0].y;
            for(int i=1;i<points.Length-1;i++)
            {
                float best=float.MaxValue,height=points[i].y,width=0;
                for(int s=0;s<road.Length;s++)
                {
                    var next=road[(s+1)%road.Length];Vector3 delta=next.Position-road[s].Position;delta.y=0;
                    Vector3 offset=points[i]-road[s].Position;offset.y=0;
                    float t=delta.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(offset,delta)/delta.sqrMagnitude):0;
                    float distance=(offset-delta*t).sqrMagnitude;
                    if(distance>=best)continue;best=distance;height=Mathf.Lerp(road[s].Position.y,next.Position.y,t);width=road[s].TrackWidth;
                }
                // Match both portals, including the full width of the shortcut.
                // A centerline-only check let the exit ribbon cross underneath
                // the road edge and turn that edge into a head-on collision.
                float portal=width*.5f+route.width*.5f+1;
                if(best<portal*portal)points[i].y=Mathf.Max(points[i].y,height+.06f);
                if(i<=8)launchHeight=Mathf.Max(launchHeight,points[i].y);
            }
            // Hold a supported approach until the straight launch. Descending
            // underneath the main road here would make cars fall before the pad.
            for(int i=5;i<=route.gapStartNodeIndex;i++)points[i].y=Mathf.Max(points[i].y,launchHeight);
            float landingHeight=points[16].y;
            for(int i=route.gapStartNodeIndex+1;i<16;i++)points[i].y=Mathf.Max(points[i].y,Mathf.Lerp(points[route.gapStartNodeIndex].y,landingHeight,(i-route.gapStartNodeIndex)/(float)(16-route.gapStartNodeIndex)));
            // Spread a raised exit portal backward over its supported deck.
            // The old local height correction created a 20-degree crest that
            // launched a landed car again, removing its braking/yaw authority.
            for(int i=points.Length-2;i>0;i--)
            {
                float horizontal=Vector3.ProjectOnPlane(points[i+1]-points[i],Vector3.up).magnitude;
                points[i].y=Mathf.Max(points[i].y,points[i+1].y-horizontal*.10f);
            }
        }
        public static GameObject Ribbon(Transform parent, string name, TrackNode[] nodes, float innerOffset, float outerOffset, Material material, RacingSurfaceKind surface, bool collider = true, bool relativeWidth = false, int gapStart = -1, int gapEnd = -1, float outerDrop = 0, bool closed = true, TrackShortcut[] clearRoutes = null)
        {
            int count = nodes.Length;
            int segments = closed ? count : count-1;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            var triangles = new System.Collections.Generic.List<int>(count * 6);
            var omitted=new bool[segments];
            for (int i = 0; i <= segments; i++)
            {
                var n = nodes[i % count];
                float widthScale = relativeWidth ? n.TrackWidth / 20f : 1f;
                vertices[i * 2] = n.Position + n.Right * innerOffset * widthScale;
                vertices[i * 2 + 1] = n.Position + n.Right * outerOffset * widthScale - Vector3.up * outerDrop;
                uv[i * 2] = new Vector2(0, i * .5f); uv[i * 2 + 1] = new Vector2(1, i * .5f);
                if (i == segments) continue;
                int v = i * 2;
                omitted[i]=i >= gapStart && i < gapEnd;
                if(clearRoutes!=null)
                {
                    // Terrain beside the road must not form a ceiling above a
                    // descending shortcut. Clear shoulder/slope portals only;
                    // the main asphalt and authored shortcut decks stay intact.
                    var nextNode=nodes[(i+1)%count];
                    float nextScale=relativeWidth?nextNode.TrackWidth/20f:1;
                    Vector3 center=(n.Position+n.Right*(innerOffset+outerOffset)*widthScale*.5f+
                        nextNode.Position+nextNode.Right*(innerOffset+outerOffset)*nextScale*.5f)*.5f;
                    float radius=Mathf.Abs(outerOffset-innerOffset)*Mathf.Max(widthScale,nextScale)*.5f+
                        Vector3.ProjectOnPlane(nextNode.Position-n.Position,Vector3.up).magnitude*.5f;
                    foreach(var route in clearRoutes)
                    for(int p=1;p<route.pathPoints.Length&&!omitted[i];p++)
                    {
                        Vector3 a=route.pathPoints[p-1],delta=route.pathPoints[p]-a;delta.y=0;
                        Vector3 offset=center-a;offset.y=0;
                        float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(offset,delta)/delta.sqrMagnitude):0;
                        float clearance=radius+route.width*.5f+1;
                        if((offset-delta*t).sqrMagnitude<clearance*clearance)omitted[i]=true;
                    }
                }
                if (omitted[i]) continue;
                if (outerOffset > innerOffset)
                {
                    triangles.Add(v); triangles.Add(v+2); triangles.Add(v+1);
                    triangles.Add(v+1); triangles.Add(v+2); triangles.Add(v+3);
                }
                else
                {
                    triangles.Add(v); triangles.Add(v+1); triangles.Add(v+2);
                    triangles.Add(v+1); triangles.Add(v+3); triangles.Add(v+2);
                }
            }
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider)
            {
                // Create a volumetric 3D collision mesh with downward extruded base and side skirts
                // to eliminate zero-thickness mesh tunneling and prevent falling through terrain/hills.
                int topLen = vertices.Length;
                Vector3[] colVerts = new Vector3[topLen * 2];
                for (int i = 0; i < topLen; i++)
                {
                    colVerts[i] = vertices[i];
                    colVerts[i + topLen] = vertices[i] - Vector3.up * 2.2f;
                }
                var colTris = new System.Collections.Generic.List<int>(triangles.Count * 2 + count * 12);
                colTris.AddRange(triangles);
                // Bottom triangles (reversed winding)
                for (int t = 0; t < triangles.Count; t += 3)
                {
                    colTris.Add(triangles[t] + topLen);
                    colTris.Add(triangles[t + 2] + topLen);
                    colTris.Add(triangles[t + 1] + topLen);
                }
                // Side skirts (inner and outer boundaries)
                for (int i = 0; i < segments; i++)
                {
                    if (omitted[i]) continue;
                    int i0 = i * 2, i1 = (i + 1) * 2;
                    // Inner skirt
                    colTris.Add(i0); colTris.Add(i0 + topLen); colTris.Add(i1);
                    colTris.Add(i1); colTris.Add(i0 + topLen); colTris.Add(i1 + topLen);
                    // Outer skirt
                    int o0 = i * 2 + 1, o1 = (i + 1) * 2 + 1;
                    colTris.Add(o0); colTris.Add(o1); colTris.Add(o0 + topLen);
                    colTris.Add(o1); colTris.Add(o1 + topLen); colTris.Add(o0 + topLen);
                }
                var colMesh = new Mesh { name = name + "_Collision", vertices = colVerts, triangles = colTris.ToArray() };
                colMesh.RecalculateNormals(); colMesh.RecalculateBounds();
                var meshCollider = root.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = colMesh;
                root.AddComponent<RacingSurface>().Kind = surface;
                root.AddComponent<RacingGeneratedMesh>().Mesh = colMesh;
            }
            root.AddComponent<RacingGeneratedMesh>().Mesh = mesh;
            return root;
        }
    }
    public sealed class RacingGeneratedMesh : MonoBehaviour
    {
        public Mesh Mesh;
        private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }
}
