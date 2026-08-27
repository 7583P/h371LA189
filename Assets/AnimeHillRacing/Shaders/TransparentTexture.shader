Shader "AnimeHillRacing/Transparent Texture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HorizontalSample ("Horizontal Sample (-1 = tiled)", Range(-1,1)) = -1
        _SeamlessHorizontalTiling ("Seamless Mirrored Horizontal Tiling", Float) = 0
        _HorizontalInset ("Horizontal Transparent Edge Inset", Range(0,0.1)) = 0
        _HorizontalUvRange ("Horizontal UV Range", Vector) = (0,1,0,0)
        [Toggle] _DepthWrite ("Depth Write", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite [_DepthWrite]

        Pass
        {
            Name "TransparentUnlit"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _HorizontalSample;
            float _SeamlessHorizontalTiling;
            float _HorizontalInset;
            float4 _HorizontalUvRange;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                if (_HorizontalSample >= 0.0)
                    output.uv.x = _HorizontalSample;
                else if (_SeamlessHorizontalTiling > 0.5)
                {
                    // The authored terrain PNGs have a narrow transparent gutter on both horizontal
                    // edges. Mirrored tiling joins equal edges and the inset keeps that gutter out of
                    // the sampled range, eliminating vertical cuts without changing terrain geometry.
                    float tile = floor(output.uv.x);
                    float localU = frac(output.uv.x);
                    if (fmod(abs(tile), 2.0) >= 1.0)
                        localU = 1.0 - localU;
                    float rangeMin = clamp(min(_HorizontalUvRange.x, _HorizontalUvRange.y), 0.0, 1.0);
                    float rangeMax = clamp(max(_HorizontalUvRange.x, _HorizontalUvRange.y), rangeMin, 1.0);
                    float range = max(0.0001, rangeMax - rangeMin);
                    float inset = clamp(_HorizontalInset, 0.0, 0.49) * range;
                    output.uv.x = lerp(rangeMin + inset, rangeMax - inset, localU);
                }
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv) * _Color;
                clip(color.a - .01);
                return color;
            }
            ENDCG
        }
    }
}
