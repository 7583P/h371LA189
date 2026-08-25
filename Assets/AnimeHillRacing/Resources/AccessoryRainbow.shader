Shader "AnimeHillRacing/Accessory Rainbow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _RainbowSpeed ("Lightning Speed", Float) = 2.6
        _RainbowFrequency ("Rainbow Frequency", Float) = 1.15
        _NeonGlow ("Neon Glow", Range(0, 2)) = 1.25
        [HideInInspector] _RainbowDirection ("Rainbow Direction", Float) = -1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;
            float _RainbowSpeed;
            float _RainbowFrequency;
            float _NeonGlow;
            float _RainbowDirection;
            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;

            v2f SpriteVert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                input.vertex.xy *= _Flip.xy;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
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

            float LightningPath(float x, float baseY, float seed)
            {
                float density = max(0.5, _RainbowFrequency);
                float wideJolt = sin((x + seed) * 29.0 * density) * 0.022;
                float fineJolt = sin((x * 2.15 + seed) * 41.0 * density) * 0.011;
                float sharpJolt = (abs(frac((x + seed) * 9.0) - 0.5) * 2.0 - 0.5) * 0.026;
                return baseY + wideJolt + fineJolt + sharpJolt;
            }

            float2 LightningStreak(float2 uv, float baseY, float phase, float seed)
            {
                float head = frac(_Time.y * _RainbowSpeed + phase);
                float behindHead = frac(head - uv.x);
                float trail = saturate(1.0 - behindHead / 0.48);
                trail *= trail;
                float distanceToBolt = abs(uv.y - LightningPath(uv.x, baseY, seed));
                float flicker = 0.78 + 0.22 * sin(_Time.y * 24.0 + seed * 31.0);
                float core = 1.0 - smoothstep(0.0, 0.006, distanceToBolt);
                float glow = 1.0 - smoothstep(0.0, 0.032, distanceToBolt);
                return float2(core, glow) * trail * flicker;
            }

            fixed4 SpriteFrag(v2f input) : SV_Target
            {
                fixed4 sprite = SampleSprite(input.texcoord) * input.color;
                // Keep screen-space travel left-to-right even when the cap sprite is mirrored.
                float mirrored = step(0.0, _RainbowDirection);
                float2 lightningUv = float2(lerp(input.texcoord.x, 1.0 - input.texcoord.x, mirrored),
                    input.texcoord.y);

                float2 redBolt = LightningStreak(lightningUv, 0.28, 0.00, 0.11);
                float2 blueBolt = LightningStreak(lightningUv, 0.45, 0.23, 0.37);
                float2 purpleBolt = LightningStreak(lightningUv, 0.62, 0.51, 0.63);
                float2 cyanBolt = LightningStreak(lightningUv, 0.76, 0.76, 0.89);

                fixed3 red = fixed3(1.0, 0.015, 0.055);
                fixed3 blue = fixed3(0.025, 0.18, 1.0);
                fixed3 purple = fixed3(0.68, 0.025, 1.0);
                fixed3 cyan = fixed3(0.0, 0.92, 1.0);
                fixed3 glow = red * redBolt.y + blue * blueBolt.y +
                    purple * purpleBolt.y + cyan * cyanBolt.y;
                fixed3 core = red * redBolt.x + blue * blueBolt.x +
                    purple * purpleBolt.x + cyan * cyanBolt.x;
                fixed3 neon = glow * (0.55 + _NeonGlow * 0.45) + core * (1.45 + _NeonGlow);

                // Premultiplied output: lightning remains strictly inside the black cap silhouette.
                fixed3 baseColor = sprite.rgb * sprite.a;
                return fixed4(baseColor + neon * sprite.a, sprite.a);
            }
            ENDCG
        }
    }
}
