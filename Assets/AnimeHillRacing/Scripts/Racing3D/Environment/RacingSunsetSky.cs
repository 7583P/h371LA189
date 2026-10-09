using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    /// <summary>
    /// Two static cloud bands and sunset color are baked into six small 2D faces
    /// at scene load. Racing pays one opaque sky texture fetch, no noise shader,
    /// transparent cloud cards, particle systems or per-frame environment work.
    /// </summary>
    public static class RacingSunsetSky
    {
        public const int FaceSize = 256;
        public static readonly Vector3 SunDirection = new Vector3(-.65f,.14f,-.75f).normalized;

        public static Material Create(RacingEnvironmentScope scope)
        {
            var texture = new Cubemap(FaceSize,TextureFormat.RGB24,true)
            {
                name="Sunset / two baked 2D cloud bands / 256px faces",
                filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp
            };
            var pixels = new Color[FaceSize*FaceSize];
            for(int face=0;face<6;face++)
            {
                for(int y=0;y<FaceSize;y++)for(int x=0;x<FaceSize;x++)
                {
                    float u=(x+.5f)*2/FaceSize-1,v=(y+.5f)*2/FaceSize-1;
                    pixels[y*FaceSize+x]=ColorAt(Direction(face,u,v).normalized);
                }
                texture.SetPixels(pixels,(CubemapFace)face);
            }
            texture.Apply(true,true);
            var material=new Material(Resources.Load<Shader>("RacingSkyTrackSky")){name="Night City / sunset and static cloud sky"};
            material.SetTexture("_SunsetSky",texture);scope?.Own(texture);scope?.Own(material);
            return material;
        }

        static Vector3 Direction(int face,float u,float v)
        {
            switch(face)
            {
                case 0:return new Vector3(1,-v,-u);
                case 1:return new Vector3(-1,-v,u);
                case 2:return new Vector3(u,1,v);
                case 3:return new Vector3(u,-1,-v);
                case 4:return new Vector3(u,-v,1);
                default:return new Vector3(-u,-v,-1);
            }
        }

        static Color ColorAt(Vector3 d)
        {
            float height=Mathf.Max(0,d.y);
            float warm=Mathf.Pow(Mathf.Max(0,Vector3.Dot(new Vector3(d.x,0,d.z).normalized,new Vector3(SunDirection.x,0,SunDirection.z).normalized)),3);
            Color horizon=Color.Lerp(new Color(.86f,.39f,.48f),new Color(1f,.55f,.30f),warm);
            Color sky=Color.Lerp(horizon,new Color(.24f,.37f,.62f),Mathf.SmoothStep(0,1,height/.48f));
            sky=Color.Lerp(sky,new Color(.045f,.075f,.20f),Mathf.SmoothStep(0,1,(height-.38f)/.62f));
            if(d.y<0)sky=Color.Lerp(horizon,new Color(.12f,.14f,.23f),Mathf.SmoothStep(0,1,-d.y/.40f));
            float sunDot=Vector3.Dot(d,SunDirection);
            float halo=Mathf.Pow(Mathf.Max(0,sunDot),80)*.3f;
            sky=Color.Lerp(sky,new Color(1f,.76f,.48f),halo);
            sky=Color.Lerp(sky,new Color(1f,.87f,.65f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.9991f,.9997f,sunDot)));
            // Fixed world directions provide continuous seams and natural
            // parallax with camera rotation, without moving geometry or timers.
            for(int layer=0;layer<2;layer++)
            {
                float center=layer==0?.18f:.43f,spread=layer==0?.19f:.20f;
                float band=Mathf.Clamp01(1-Mathf.Abs(d.y-center)/spread);
                float x=d.x*(layer==0?6:9)+d.y*2+layer*37,z=d.z*(layer==0?6:9)+layer*19;
                float noise=Mathf.PerlinNoise(x+11,z+23)*.60f+Mathf.PerlinNoise(x*2+71,z*2+5)*.28f+Mathf.PerlinNoise(x*4+3,z*4+47)*.12f;
                float cloud=Mathf.SmoothStep(0,1,(noise-.43f)*4)*Mathf.SmoothStep(0,1,band)*.78f;
                Color tint=Color.Lerp(new Color(.46f,.43f,.61f),new Color(1f,.72f,.60f),warm*.65f+(1-height)*.25f);
                sky=Color.Lerp(sky,tint,cloud);
            }
            return sky;
        }
    }
}
