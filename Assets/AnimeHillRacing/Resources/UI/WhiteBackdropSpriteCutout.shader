Shader "AnimeHillRacing/White Backdrop Sprite Cutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;

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
                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                fixed maximum = max(color.r, max(color.g, color.b));
                fixed minimum = min(color.r, min(color.g, color.b));
                fixed chroma = maximum - minimum;
                // The generated source has a neutral white backdrop. Remove only bright neutral pixels;
                // saturated gold glow and the painted coin highlights remain visible.
                fixed neutral = 1.0 - smoothstep(0.025, 0.16, chroma);
                fixed whiteBackdrop = smoothstep(0.78, 0.985, minimum) * neutral;
                color.a *= 1.0 - whiteBackdrop;
                return color;
            }
            ENDCG
        }
    }
}
