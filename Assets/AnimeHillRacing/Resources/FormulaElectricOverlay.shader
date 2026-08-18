Shader "AnimeHillRacing/FormulaElectricOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Electricity", 2D) = "white" {}
        [PerRendererData] _MaskTex ("Body Mask", 2D) = "white" {}
        [PerRendererData] _Intensity ("Intensity", Range(0, 1)) = 0.8
        [PerRendererData] _ExternalAmount ("External Amount", Range(0, 1)) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;
            float _Intensity;
            float _ExternalAmount;
            fixed4 _RendererColor;
            float4 _Flip;

            v2f vert(appdata_t input)
            {
                v2f output;
                float2 flip = _Flip.xy;
                if (abs(flip.x) < 0.001) flip.x = 1.0;
                if (abs(flip.y) < 0.001) flip.y = 1.0;
                input.vertex.xy *= flip;

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;

                fixed4 rendColor = _RendererColor;
                if (rendColor.a < 0.001 && dot(rendColor.rgb, rendColor.rgb) < 0.001) rendColor = fixed4(1,1,1,1);
                output.color = input.color * rendColor;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif
                return output;
            }

            fixed4 SampleElectricity(float2 uv)
            {
                fixed4 sample = tex2D(_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                sample.a = lerp(sample.a, alpha.r, _EnableExternalAlpha);
                #endif
                return sample;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 electricity = SampleElectricity(input.texcoord);
                fixed maskAlpha = tex2D(_MaskTex, input.texcoord).a;
                float travel = frac(input.texcoord.x * 1.65 - _Time.y * .42);
                float band = 1.0 - abs(travel * 2.0 - 1.0);
                band = smoothstep(.12, .88, band);
                float current = sin(input.texcoord.x * 38.0 + input.texcoord.y * 17.0 - _Time.y * 8.0);
                float pulse = .30 + .70 * max(band, current * .5 + .5);
                float bodyMask = lerp(maskAlpha, 1.0, saturate(_ExternalAmount));
                fixed alpha = electricity.a * bodyMask * pulse * _Intensity * input.color.a;
                fixed3 electricColor = electricity.rgb * input.color.rgb * (1.05 + pulse * .35);
                return fixed4(electricColor, alpha);
            }
            ENDCG
        }
    }
}
