Shader "AnimeHillRacing/SportBikeNeonFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _FlowSpeed ("Flow Speed", Float) = 0.27
        [PerRendererData] _FlowScale ("Flow Scale", Float) = 1.8
        [PerRendererData] _RadialFlow ("Radial Flow", Range(0,1)) = 0
        [PerRendererData] _GlowIntensity ("Glow Intensity", Range(0,2)) = 0.62
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
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
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;
            float _FlowSpeed;
            float _FlowScale;
            float _RadialFlow;
            float _GlowIntensity;
            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;

            v2f vert(appdata_t input)
            {
                v2f output;
                float2 flip = _Flip.xy;
                if (abs(flip.x) < .001) flip.x = 1.0;
                if (abs(flip.y) < .001) flip.y = 1.0;
                input.vertex.xy *= flip;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                fixed4 rendererColor = _RendererColor;
                if (rendererColor.a < .001 && dot(rendererColor.rgb, rendererColor.rgb) < .001)
                    rendererColor = fixed4(1,1,1,1);
                output.color = input.color * _Color * rendererColor;
                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif
                return output;
            }

            fixed4 SampleSprite(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif
                return color;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = SampleSprite(input.texcoord) * input.color;
                float maximum = max(color.r, max(color.g, color.b));
                float minimum = min(color.r, min(color.g, color.b));
                float chroma = maximum - minimum;

                // Bright, saturated authored pixels are the neon paint; dark bodywork and metal stay intact.
                float neonMask = smoothstep(.36, .78, maximum) * smoothstep(.12, .42, chroma);
                float2 centered = input.texcoord - .5;
                float angle = atan2(centered.y, centered.x) / 6.2831853 + .5;
                float radius = length(centered);
                float linearPath = input.texcoord.x + input.texcoord.y * .22;
                float radialPath = angle + radius * .55;
                float path = lerp(linearPath, radialPath, saturate(_RadialFlow));
                float phase = frac(path * _FlowScale - _Time.y * _FlowSpeed);
                fixed3 flowingColor = .55 + .45 * cos(6.2831853 *
                    (phase + fixed3(0.0, .667, .333)));
                float crest = .72 + .28 * smoothstep(.08, .92, 1.0 - abs(phase * 2.0 - 1.0));
                float strength = neonMask * crest;
                fixed3 animated = flowingColor * max(.72, maximum) * 1.22;
                color.rgb = lerp(color.rgb, animated, strength * .72);
                color.rgb += flowingColor * strength * _GlowIntensity * .22;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
