using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    // Authored once by the Editor generator, loaded once before GO, shared by
    // held rewards and physical pickups. No mesh/material creation on consumption.
    public static class RacingCoinArt
    {
        static Mesh mesh;
        static Material material;
        public static Mesh SharedMesh => mesh != null ? mesh : mesh = Resources.Load<Mesh>("RacingCoin10");
        public static Material SharedMaterial => material != null ? material : material = Resources.Load<Material>("RacingCoinGold");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){mesh=null;material=null;}
        public static void Build(Transform parent)
        {
            var part=RacingArt.Part(parent,"Gold coin +10",SharedMesh,SharedMaterial,Vector3.zero,Quaternion.identity);
            var renderer=part.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }

        // Exported as a native .asset; this method is never called during a race.
        public static Mesh CreateMesh()
        {
            const int sides=20;
            var vertices=new List<Vector3>(400);var triangles=new List<int>(660);var colors=new List<Color>(400);
            float[] radius={.49f,.55f,.55f,.49f};float[] z={-.085f,-.045f,.045f,.085f};
            Color gold=new Color(1f,.79f,.06f),rim=new Color(1f,.61f,.025f),stamp=new Color(.60f,.28f,.025f);
            for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++) {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Quad(Point(a,radius[ring],z[ring]),Point(b,radius[ring],z[ring]),Point(b,radius[ring+1],z[ring+1]),Point(a,radius[ring+1],z[ring+1]),ring==1?rim:gold);
            }
            for(int side=-1;side<=1;side+=2) {
                for(int i=0;i<sides;i++) {
                    int at=vertices.Count;float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    vertices.Add(new Vector3(0,0,side*.085f));vertices.Add(Point(a,.49f,side*.085f));vertices.Add(Point(b,.49f,side*.085f));
                    colors.Add(gold);colors.Add(gold);colors.Add(gold);
                    triangles.Add(at);triangles.Add(at+(side>0?1:2));triangles.Add(at+(side>0?2:1));
                }
                Stroke(-.34f,0,-.16f,0);Stroke(-.25f,-.09f,-.25f,.09f);
                Stroke(-.07f,-.19f,-.07f,.19f);Stroke(-.14f,.12f,-.07f,.19f);
                Stroke(.12f,-.19f,.31f,-.19f);Stroke(.31f,-.19f,.31f,.19f);
                Stroke(.31f,.19f,.12f,.19f);Stroke(.12f,.19f,.12f,-.19f);
                void Stroke(float ax,float ay,float bx,float by) {
                    Vector2 a=new Vector2(ax,ay),b=new Vector2(bx,by);Vector2 edge=b-a;
                    Vector2 width=new Vector2(-edge.y,edge.x).normalized*.026f;
                    Vector3 p=new Vector3((a.x-width.x)*side,a.y-width.y,side*.087f),q=new Vector3((b.x-width.x)*side,b.y-width.y,side*.087f);
                    Vector3 r=new Vector3((b.x+width.x)*side,b.y+width.y,side*.087f),s=new Vector3((a.x+width.x)*side,a.y+width.y,side*.087f);
                    Quad(p,q,r,s,stamp);
                }
            }
            var result=new Mesh{name="Racing Coin +10 / 20 sides / single draw"};
            result.SetVertices(vertices);result.SetTriangles(triangles,0);result.SetColors(colors);result.RecalculateNormals();result.RecalculateBounds();
            return result;
            Vector3 Point(float angle,float r,float depth)=>new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,depth);
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color) {
                int at=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                for(int i=0;i<4;i++)colors.Add(color);
                triangles.Add(at);triangles.Add(at+1);triangles.Add(at+2);triangles.Add(at);triangles.Add(at+2);triangles.Add(at+3);
            }
        }
    }
}
