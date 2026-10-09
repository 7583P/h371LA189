Shader "HillAdventurer/SkyTrackSky"
{
    Properties { _SunsetSky ("Sunset / baked 2D clouds", Cube) = "" {} }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            samplerCUBE _SunsetSky;
            struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
            v2f vert(float4 vertex:POSITION) { v2f o;o.pos=UnityObjectToClipPos(vertex);o.direction=vertex.xyz;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                return fixed4(texCUBE(_SunsetSky,i.direction).rgb,1);
            }
            ENDCG
        }
    }
}
