using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingCityBackdrop
    {
        public static void Build(Transform parent)
        {
            var texture=Resources.Load<Texture2D>("RacingArt/NeonDepthCity");
            if(texture==null)return;
            const int segments=48;
            var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;Vector3 p=new Vector3(90+Mathf.Cos(a)*590,-45,Mathf.Sin(a)*590);
                vertices[i*2]=p;vertices[i*2+1]=p+Vector3.up*340;
                uv[i*2]=new Vector2(i*4f/segments,0);uv[i*2+1]=new Vector2(i*4f/segments,1);
                if(i==segments)continue;int v=i*2,t=i*6;
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+2;triangles[t+4]=v+1;triangles[t+5]=v+3;
            }
            var mesh=new Mesh{name="Layered city panorama / 96 triangles",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateBounds();
            var material=new Material(Resources.Load<Shader>("RacingCityPanorama")){name="Opaque distant neon city",mainTexture=texture};
            var root=RacingArt.Part(parent,"Layered city horizon",mesh,material,Vector3.zero,Quaternion.identity);
            root.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
            root.AddComponent<RacingGeneratedMaterial>().Material=material;
            var renderer=root.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }

    public static class RacingCityArt
    {
        public static Material Asphalt()
        {
            var material=RacingArt.Material(new Color(.044f,.059f,.080f),.90f);
            var wet=Resources.Load<Shader>("RacingWetCityRoad");if(wet!=null && wet.isSupported){material.shader=wet;material.SetFloat("_Metallic",.48f);}
            if(material.mainTexture!=null)return material;
            var texture=new Texture2D(64,64,TextureFormat.RGB24,true){name="Native fine asphalt grain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var pixels=new Color32[4096];var rng=new DeterministicRng(34971);
            for(int i=0;i<pixels.Length;i++){byte value=(byte)rng.Range(205,255);pixels[i]=new Color32(value,value,value,255);}
            texture.SetPixels32(pixels);texture.Apply(true,true);material.mainTexture=texture;material.mainTextureScale=new Vector2(2,3);return material;
        }
        // Four-sided, batched facade grids. Flat quads add small illuminated
        // windows and depth cues without thousands of individual objects.
        public static void Facade(Transform parent,float width,float height,float depth,int seed,Material neon,Material warm)
        {
            var vertices=new[]{new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};
            var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
            int floors=Mathf.Clamp(Mathf.FloorToInt(height/5.2f),6,30);
            for(int face=0;face<4;face++)
            {
                Vector3 normal=face==0?Vector3.forward:face==1?Vector3.back:face==2?Vector3.right:Vector3.left;
                Vector3 right=Vector3.Cross(Vector3.up,normal);float span=face<2?width:depth;
                Vector3 origin=normal*((face<2?depth:width)*.5f+.035f);
                Quad(2,origin+Vector3.up*(height*.5f),right*(span*.485f),Vector3.up*(height*.49f));
                int columns=Mathf.Clamp(Mathf.FloorToInt(span/3),5,16);
                for(int row=1;row<floors;row++)for(int col=0;col<columns;col++)
                {
                    int pattern=(seed+row*73+col*151+face*937)&1023;
                    if(pattern%9<3)continue;
                    int group=pattern%7<2?1:0;
                    Vector3 center=origin+normal*.04f+right*((col+.5f)/columns-.5f)*span*.88f+Vector3.up*(row*height/floors);
                    Quad(group,center,right*(span/columns*.25f),Vector3.up*(height/floors*.22f));
                }
            }
            Material[] materials={neon,warm,RacingArt.Material(new Color(.012f,.024f,.042f),.70f)};
            for(int i=0;i<3;i++)
            {
                var mesh=new Mesh{name="Native city facade "+seed+" / "+i};mesh.SetVertices(vertices[i]);mesh.SetTriangles(triangles[i],0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var part=RacingArt.Part(parent,"Layered glass facade",mesh,materials[i],Vector3.zero,Quaternion.identity);part.AddComponent<RacingGeneratedMesh>().Mesh=mesh;
                part.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            void Quad(int group,Vector3 center,Vector3 right,Vector3 up)
            {
                int v=vertices[group].Count;vertices[group].Add(center-right-up);vertices[group].Add(center+right-up);vertices[group].Add(center+right+up);vertices[group].Add(center-right+up);
                triangles[group].Add(v);triangles[group].Add(v+1);triangles[group].Add(v+2);triangles[group].Add(v);triangles[group].Add(v+2);triangles[group].Add(v+3);
            }
        }
    }
}
