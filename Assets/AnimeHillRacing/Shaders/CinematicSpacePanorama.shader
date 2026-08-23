Shader "AnimeHillRacing/Cinematic Space Panorama"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowStrength ("Nebula Flow Strength", Range(0,0.01)) = 0.0018
        _FlowSpeed ("Nebula Flow Speed", Range(0.01,0.5)) = 0.075
        _PulseAmount ("Nebula Pulse", Range(0,0.2)) = 0.055
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _FlowStrength;
            float _FlowSpeed;
            float _PulseAmount;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                fixed4 baseColor = tex2D(_MainTex, input.uv);
                float maximum = max(baseColor.r, max(baseColor.g, baseColor.b));
                float minimum = min(baseColor.r, min(baseColor.g, baseColor.b));
                float saturation = maximum - minimum;
                float luminance = dot(baseColor.rgb, float3(.2126, .7152, .0722));

                // Saturated mid-tones identify gaseous nebulae and aurora-like regions while the
                // dark silhouettes of planets remain crisp and stable.
                float nebulaMask = smoothstep(.075, .30, saturation) * smoothstep(.025, .58, luminance);
                float edgeSafety = smoothstep(0.0, .045, input.uv.x) * smoothstep(0.0, .045, input.uv.y) *
                                   smoothstep(0.0, .045, 1.0 - input.uv.x) *
                                   smoothstep(0.0, .045, 1.0 - input.uv.y);
                nebulaMask *= edgeSafety;

                float2 flow;
                flow.x = sin(input.uv.y * 18.0 + time * 1.31) + cos(input.uv.x * 9.0 - time * .73);
                flow.y = cos(input.uv.x * 15.0 - time) + sin(input.uv.y * 11.0 + time * .57);
                flow *= _FlowStrength * .5;
                fixed4 flowingColor = tex2D(_MainTex, saturate(input.uv + flow));
                fixed3 animated = lerp(baseColor.rgb, flowingColor.rgb, nebulaMask * .48);

                float wave = sin(time * 1.17 + input.uv.x * 8.0 + input.uv.y * 5.0) * .5 + .5;
                float slowWave = sin(time * .43 - input.uv.x * 4.0 + input.uv.y * 7.0) * .5 + .5;
                animated *= 1.0 + nebulaMask * _PulseAmount * (wave * .66 + slowWave * .34);

                // Very bright, compact pixels receive a restrained scintillation; broad bright areas
                // such as galaxy cores are excluded by their surrounding saturation mask.
                float starMask = smoothstep(.72, .98, luminance) * (1.0 - smoothstep(.10, .30, saturation));
                float starPulse = sin(_Time.y * 2.8 + input.uv.x * 913.0 + input.uv.y * 617.0) * .5 + .5;
                animated += starMask * starPulse * .055;
                return fixed4(animated, baseColor.a) * input.color;
            }
            ENDCG
        }
    }
}
