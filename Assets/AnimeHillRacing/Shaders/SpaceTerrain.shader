Shader "AnimeHillRacing/Space Terrain"
{
    Properties
    {
        _MainTex ("Compatibility Texture", 2D) = "white" {}
        _Color ("Base", Color) = (0.08,0.03,0.15,1)
        _AccentColor ("Lunar Variation", Color) = (0.18,0.08,0.28,1)
        _PatternScale ("Pattern Scale", Range(0.02,2)) = 0.18
        _Variation ("Variation", Range(0,1)) = 0.22
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float3 world : TEXCOORD0; };
            fixed4 _Color, _AccentColor;
            float _PatternScale, _Variation;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 cell = floor(i.world.xy * _PatternScale);
                float n = hash21(cell);
                float waves = sin(i.world.x * .11 + sin(i.world.y * .17)) * .5 + .5;
                float crater = smoothstep(.91, 1.0, n) * .7;
                float blend = saturate((n * .45 + waves * .55 - crater) * _Variation);
                return lerp(_Color, _AccentColor, blend);
            }
            ENDCG
        }
    }
}
