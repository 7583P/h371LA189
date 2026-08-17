Shader "AnimeHillRacing/VehicleBodyBeltline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _ClipLocalY ("Clip Active (< 500)", Float) = 999.0
        _ClipInvert ("Clip Invert (0=keep car, 1=keep canopy)", Float) = 0.0
        _RoofMinU ("Roof Min U (ahead of spoiler)", Float) = 0.16
        _RoofMaxU ("Roof Max U (base of windshield)", Float) = 0.70
        _BeltlineV ("Beltline V base height", Float) = 0.58
        _SeatMinU ("Seat Min U", Float) = 0.33
        _SeatMaxU ("Seat Max U", Float) = 0.40
        _SeatMaxV ("Seat Max V height", Float) = 0.85
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
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _Flip;
            float _ClipLocalY;
            float _ClipInvert;
            float _RoofMinU;
            float _RoofMaxU;
            float _BeltlineV;
            float _SeatMinU;
            float _SeatMaxU;
            float _SeatMaxV;

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.localPos = IN.vertex.xy;

                IN.vertex.xy *= _Flip.xy;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SampleSpriteTexture (float2 uv)
            {
                fixed4 color = tex2D (_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D (_AlphaTex, uv);
                color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                if (_ClipLocalY < 500.0)
                {
                    float u = IN.texcoord.x;
                    float v = IN.texcoord.y;

                    bool isCanopy = false;
                    if (u >= _RoofMinU && u <= _RoofMaxU)
                    {
                        float cutV = _BeltlineV;
                        if (u > 0.56)
                        {
                            cutV = _BeltlineV + (u - 0.56) * 0.15;
                        }

                        // Protect the seat backrest: keep seat on the car
                        bool isSeat = (u >= _SeatMinU && u <= _SeatMaxU && v <= _SeatMaxV);

                        if (!isSeat && v > cutV)
                        {
                            isCanopy = true;
                        }
                    }

                    if (_ClipInvert < 0.5)
                    {
                        // On the car body: discard canopy pixels, keep car (including spoiler & seat backrest)
                        if (isCanopy) discard;
                    }
                    else
                    {
                        // On the flying canopy: keep canopy pixels only, discard the car body/spoiler/seat
                        if (!isCanopy) discard;
                    }
                }

                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
