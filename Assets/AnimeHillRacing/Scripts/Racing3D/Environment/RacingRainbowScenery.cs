using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    // Original orbital architecture, opt-in review only. This owns no navigation,
    // colliders, lights, Updates, items or vehicle settings.
    public static class RacingRainbowScenery
    {
#if UNITY_EDITOR
        public static bool? EditorPreviewOverride;
#endif
        public static bool Requested
        {
            get
            {
#if UNITY_EDITOR
                if(EditorPreviewOverride.HasValue)return EditorPreviewOverride.Value;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return Array.IndexOf(Environment.GetCommandLineArgs(),"--rainbow-preview")>=0;
#else
                return false;
#endif
            }
        }
        static readonly Color[] Palette={new Color(1,.19f,.25f),new Color(1,.51f,.12f),new Color(.93f,.78f,.21f),new Color(.25f,.84f,.52f),new Color(.12f,.78f,.95f),new Color(.27f,.43f,1),new Color(.65f,.32f,.96f),new Color(.95f,.25f,.68f)};

        public static void Build(Transform parent,TrackNode[] nodes,TrackShortcut[] shortcuts)
        {
            var scope=parent.GetComponent<RacingEnvironmentScope>();
            var shader=Resources.Load<Shader>("RacingRainbowStructure");
            if(shader==null)throw new InvalidOperationException("Rainbow review structure shader is missing");
            var material=new Material(shader){name="Orbital ribbon / shared opaque vertex palette"};scope.Own(material);
            for(int sector=0;sector<3;sector++)
            {
                var mesh=new Geometry();int first=sector*nodes.Length/3,last=(sector+1)*nodes.Length/3;
                for(int i=first;i<last;i++)
                {
                    var n=nodes[i];var next=nodes[(i+1)%nodes.Length];
                    Color hue=Palette[(i/8)%Palette.Length];
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector3 a=n.Position+n.Right*(side*n.TrackWidth*.515f)+Vector3.up*.20f;
                        Vector3 b=next.Position+next.Right*(side*next.TrackWidth*.515f)+Vector3.up*.20f;
                        // Leave both route portals visually open and undisclosed.
                        if(!NearRoute((a+b)*.5f,shortcuts,5))mesh.Beam(a,b,.18f,Color.Lerp(hue,Color.white,.22f));
                    }
                    if(i%8==0)
                    {
                        // Faceted underside ribs express slope and deck depth.
                        Vector3 left=n.Position-n.Right*n.TrackWidth*.51f-Vector3.up*.3f;
                        Vector3 right=n.Position+n.Right*n.TrackWidth*.51f-Vector3.up*.3f;
                        Vector3 keel=n.Position-Vector3.up*4.4f;
                        mesh.Beam(left,keel,.36f,hue*.28f);mesh.Beam(keel,right,.36f,hue*.28f);
                        mesh.Beam(left,right,.24f,new Color(.13f,.21f,.30f));
                    }
                    float turn=Vector3.SignedAngle(nodes[(i+nodes.Length-3)%nodes.Length].Forward,nodes[(i+3)%nodes.Length].Forward,Vector3.up);
                    if(i%3==0&&Mathf.Abs(turn)>18)
                    {
                        float side=-Mathf.Sign(turn);Vector3 p=n.Position+n.Right*(side*(n.TrackWidth*.5f+2.8f))+Vector3.up*2;
                        if(!NearRoute(p,shortcuts,7))
                        {
                            // Arrowhead points into the actual curve, never along an invented route.
                            Vector3 inward=-n.Right*side;
                            mesh.Beam(p-inward*.8f-Vector3.up*.85f,p+inward*.8f,.23f,new Color(.72f,.93f,1));
                            mesh.Beam(p+inward*.8f,p-inward*.8f+Vector3.up*.85f,.23f,new Color(.72f,.93f,1));
                        }
                    }
                }
                foreach(int index in new[]{0,40,72,104,136,168,184})if(index>=first&&index<last)
                {
                    var n=nodes[index];Color hue=Palette[(index/8)%Palette.Length];
                    // Wide asymmetric orbital frames: all members stay above or outside the driving envelope.
                    float half=n.TrackWidth*.5f+3.3f;
                    Vector3[] points={n.Position-n.Right*half+Vector3.up*.5f,n.Position-n.Right*half+Vector3.up*7.5f,
                        n.Position-n.Right*(half-4)+Vector3.up*11,n.Position+n.Right*(half-5)+Vector3.up*11,
                        n.Position+n.Right*half+Vector3.up*6.8f,n.Position+n.Right*half+Vector3.up*.5f};
                    for(int j=1;j<points.Length;j++) {
                        mesh.Beam(points[j-1],points[j],.48f,Color.Lerp(hue,new Color(.09f,.17f,.25f),.76f));
                        mesh.Beam(points[j-1]-n.Forward*.25f,points[j]-n.Forward*.25f,.09f,Color.Lerp(hue,new Color(.6f,.9f,1),.6f));
                    }
                    mesh.Beam(points[2]+n.Forward*.8f,points[3]+n.Forward*.8f,.12f,new Color(.68f,.91f,1));
                }
                mesh.Publish(parent,"Orbital ribbon / sector "+sector,material);
            }
            var celestial=new Geometry();
            Vector3 center=new Vector3(780,410,1120);float radius=128;
            for(int y=0;y<16;y++)for(int x=0;x<48;x++)
            {
                Vector3 Point(int px,int py){float a=px*Mathf.PI*2/48,b=py*Mathf.PI/16;return center+radius*new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));}
                Color band=Color.Lerp(new Color(.10f,.15f,.30f),new Color(.36f,.23f,.49f),.5f+.5f*Mathf.Sin(y*1.7f));
                celestial.Quad(Point(x,y),Point(x+1,y),Point(x+1,y+1),Point(x,y+1),band);
            }
            Vector3 ringRight=new Vector3(1,.18f,0).normalized,ringUp=Vector3.Cross(new Vector3(.12f,.94f,.3f).normalized,ringRight).normalized;
            for(int i=0;i<128;i++)
            {
                Vector3 Ring(int segment,float r){float a=segment*Mathf.PI*2/128;return center+(ringRight*Mathf.Cos(a)+ringUp*Mathf.Sin(a))*r;}
                celestial.Quad(Ring(i,169),Ring(i+1,169),Ring(i+1,180),Ring(i,180),new Color(.28f,.42f,.55f));
                celestial.Quad(Ring(i,190),Ring(i+1,190),Ring(i+1,193),Ring(i,193),new Color(.36f,.25f,.49f));
            }
            celestial.Publish(parent,"Orbital ribbon / distant ringed world",material);
        }

        static bool NearRoute(Vector3 point,TrackShortcut[] routes,float margin)
        {
            foreach(var route in routes)for(int i=1;i<route.pathPoints.Length;i++)
            {
                Vector3 a=route.pathPoints[i-1],delta=route.pathPoints[i]-a;
                float t=delta.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(point-a,delta)/delta.sqrMagnitude);
                if((point-a-delta*t).sqrMagnitude<Mathf.Pow(route.width*.5f+margin,2))return true;
            }
            return false;
        }

        public static Material CreateSky(RacingEnvironmentScope scope)
        {
            const int size=256;var cube=new Cubemap(size,TextureFormat.RGB24,true){name="Orbital ribbon / original baked nebula",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];Vector3 normal=new Vector3(.45f,.63f,-.62f).normalized;
            for(int face=0;face<6;face++)
            {
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    Vector3 d=Direction(face,(x+.5f)*2/size-1,(y+.5f)*2/size-1).normalized;
                    float band=Mathf.Exp(-Mathf.Pow((Vector3.Dot(d,normal)+Mathf.Sin(d.x*9+d.z*5)*.035f)/.19f,2));
                    float cloud=.5f+.28f*Mathf.Sin(d.x*17+d.y*11)+.18f*Mathf.Sin(d.z*31-d.x*13);
                    Color nebula=Color.Lerp(new Color(.035f,.09f,.16f),new Color(.14f,.035f,.16f),.5f+.5f*Mathf.Sin(d.z*4+d.x*6));
                    pixels[y*size+x]=new Color(.008f,.012f,.032f)+nebula*band*(.35f+.65f*cloud);
                }
                var rng=new DeterministicRng(8817+face*193);
                for(int star=0;star<420;star++)
                {
                    int x=rng.Next(1,size-1),y=rng.Next(1,size-1);
                    Color light=Color.Lerp(new Color(.49f,.72f,1),new Color(1,.83f,.64f),rng.NextFloat())*rng.Range(.42f,.90f);
                    pixels[y*size+x]+=light;
                }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply(true,true);var material=new Material(Resources.Load<Shader>("RacingSkyTrackSky")){name="Orbital ribbon / nebula sky"};material.SetTexture("_SunsetSky",cube);
            scope.Own(cube);scope.Own(material);return material;
        }
        static Vector3 Direction(int face,float u,float v)
        {
            switch(face){case 0:return new Vector3(1,-v,-u);case 1:return new Vector3(-1,-v,u);case 2:return new Vector3(u,1,v);case 3:return new Vector3(u,-1,-v);case 4:return new Vector3(u,-v,1);default:return new Vector3(-u,-v,-1);}
        }
        sealed class Geometry
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> indices=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {
                int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});for(int j=0;j<4;j++)colors.Add(color);
                indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            public void Beam(Vector3 a,Vector3 b,float width,Color color)
            {
                Vector3 direction=(b-a).normalized;Vector3 side=Vector3.Cross(direction,Mathf.Abs(direction.y)>.95f?Vector3.forward:Vector3.up).normalized*width*.5f;
                Vector3 up=Vector3.Cross(direction,side).normalized*width*.5f;
                var p=new[]{a-side-up,a+side-up,a+side+up,a-side+up,b-side-up,b+side-up,b+side+up,b-side+up};
                Quad(p[0],p[1],p[2],p[3],color);Quad(p[7],p[6],p[5],p[4],color);
                for(int i=0;i<4;i++){int j=(i+1)%4;Quad(p[i],p[4+i],p[4+j],p[j],color);}
            }
            public void Publish(Transform parent,string name,Material material)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
                var root=new GameObject(name);root.transform.SetParent(parent,false);root.AddComponent<MeshFilter>().sharedMesh=mesh;
                root.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
                var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        }
    }
}
