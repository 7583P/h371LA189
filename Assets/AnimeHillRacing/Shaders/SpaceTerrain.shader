Shader "AnimeHillRacing/Space Terrain"
{
    Properties
    {
        _MainTex ("Compatibility Texture", 2D) = "white" {}
        _Color ("Base", Color) = (0.08,0.03,0.15,1)
        _AccentColor ("Lunar Variation", Color) = (0.18,0.08,0.28,1)
        _SecondaryColor ("Second Stratum", Color) = (0.24,0.22,0.31,1)
        _DeepColor ("Deep Stratum", Color) = (0.035,0.055,0.16,1)
        _VeinColor ("Veins and Stones", Color) = (0.46,0.43,0.54,1)
        _PatternScale ("Pattern Scale", Range(0.02,2)) = 0.18
        _Variation ("Variation", Range(0,1)) = 0.22
        _SurfaceMode ("Lunar Surface Mode", Range(0,1)) = 0
        _LayerScale ("Underground Layer Scale", Range(0.03,1)) = 0.18
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
            fixed4 _Color, _AccentColor, _SecondaryColor, _DeepColor, _VeinColor;
            float _PatternScale, _Variation, _SurfaceMode, _LayerScale;
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
            float valueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(cell);
                float b = hash21(cell + float2(1, 0));
                float c = hash21(cell + float2(0, 1));
                float d = hash21(cell + 1.0);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 world = i.world.xy;
                float fine = valueNoise(world * max(.02, _PatternScale));
                float broad = valueNoise(world * max(.012, _PatternScale * .24) + 19.7);

                if (_SurfaceMode > .5)
                {
                    float mottling = saturate(.22 + fine * .56 + broad * .22);
                    fixed4 lunar = lerp(_Color, _AccentColor, mottling * _Variation + mottling * .34);
                    float stoneCell = hash21(floor(world * .72));
                    float stones = smoothstep(.91, .985, stoneCell) * smoothstep(.18, .82, fine);
                    float crackWave = abs(sin(world.x * .92 + valueNoise(world * .13) * 8.0));
                    float cracks = (1.0 - smoothstep(0.0, .055, crackWave)) * smoothstep(.61, .89, broad);
                    lunar.rgb = lerp(lunar.rgb, _VeinColor.rgb, stones * .32);
                    lunar.rgb *= 1.0 - cracks * .28;
                    return lunar;
                }

                float warp = (broad - .5) * 5.2 + sin(world.x * .055) * 1.7;
                float layerPosition = (world.y + warp) * max(.03, _LayerScale);
                float layer = frac(layerPosition);
                fixed4 upper = lerp(_Color, _AccentColor, smoothstep(.08, .48, layer));
                fixed4 lower = lerp(_SecondaryColor, _DeepColor, smoothstep(.52, .94, layer));
                fixed4 strata = layer < .52 ? upper : lower;
                float sediment = sin((world.y + warp * .38) * 1.7) * .5 + .5;
                strata.rgb *= lerp(.78, 1.08, sediment * _Variation + fine * .18);

                float veinField = abs(sin(world.x * .105 + world.y * .31 + broad * 7.0));
                float vein = (1.0 - smoothstep(.0, .052, veinField)) * smoothstep(.48, .76, fine);
                float stone = smoothstep(.93, .985, hash21(floor(world * .31 + 7.0)));
                strata.rgb = lerp(strata.rgb, _VeinColor.rgb, vein * .72 + stone * .24);
                return strata;
            }
            ENDCG
        }
    }
}
