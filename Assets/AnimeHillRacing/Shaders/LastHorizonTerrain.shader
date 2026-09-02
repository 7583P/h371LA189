Shader "AnimeHillRacing/LastHorizonTerrain"
{
    Properties
    {
        _Color ("Pure Black", Color) = (0,0,0,1)
    }

    SubShader
    {
        // Last Horizon uses the same built-in 2.5D TerrainChunk mesh as every other track.
        // Keep this pass deliberately unlit and fog-free so the wall and driveable cap stay
        // mathematically black regardless of scene lights, ambient color or camera distance.
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Lighting Off
        ZWrite On
        Blend One Zero

        Pass
        {
            Name "LastHorizonPureBlack"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f Vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                return output;
            }

            fixed4 Frag(v2f input) : SV_Target
            {
                return fixed4(0.0, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
