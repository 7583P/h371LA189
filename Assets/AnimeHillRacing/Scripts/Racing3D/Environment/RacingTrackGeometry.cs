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
        public static Vector3[] ShortcutPath(TrackNode entry, TrackNode exit)
        {
            Vector3 chord = exit.Position - entry.Position;
            Vector3 direction = chord.normalized;
            float handle = Mathf.Clamp(chord.magnitude * .18f, 18, 34);
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
        public static GameObject Ribbon(Transform parent, string name, TrackNode[] nodes, float innerOffset, float outerOffset, Material material, RacingSurfaceKind surface, bool collider = true, bool relativeWidth = false, int gapStart = -1, int gapEnd = -1, float outerDrop = 0, bool closed = true)
        {
            int count = nodes.Length;
            int segments = closed ? count : count-1;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            var triangles = new System.Collections.Generic.List<int>(count * 6);
            for (int i = 0; i <= segments; i++)
            {
                var n = nodes[i % count];
                float widthScale = relativeWidth ? n.TrackWidth / 20f : 1f;
                vertices[i * 2] = n.Position + n.Right * innerOffset * widthScale;
                vertices[i * 2 + 1] = n.Position + n.Right * outerOffset * widthScale - Vector3.up * outerDrop;
                uv[i * 2] = new Vector2(0, i * .5f); uv[i * 2 + 1] = new Vector2(1, i * .5f);
                if (i == segments) continue;
                int v = i * 2;
                if (i >= gapStart && i < gapEnd) continue;
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
                    if (i >= gapStart && i < gapEnd) continue;
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
