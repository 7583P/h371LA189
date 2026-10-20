Shader "HillAdventurer/PrismaticRoad"
{
    Properties { _MainTex ("Structural road", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; UNITY_FOG_COORDS(1) };
            sampler2D _MainTex;
            v2f vert(appdata v) { v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.position);return o; }
            fixed3 spectrum(float hue) { float3 p=abs(frac(hue+float3(0,.666667,.333333))*6-3);return lerp(float3(.055,.055,.055),float3(.92,.92,.92),saturate(p-1)); }
            fixed4 frag(v2f i):SV_Target
            {
                float across=frac(i.uv.x*8),along=frac(i.uv.y*1.5);
                float panel=smoothstep(.025,.10,across)*(1-smoothstep(.90,.975,across));
                panel*=smoothstep(.025,.08,along)*(1-smoothstep(.92,.975,along));
                fixed3 prism=spectrum(frac(i.uv.x*.88+i.uv.y*.012));
                float ribbon=1-smoothstep(.12,.22,abs(across-.5));
                float pulse=1-smoothstep(.015,.065,abs(frac(i.uv.y*.14-_Time.y*.17)-.5));
                float edge=smoothstep(.94,.99,abs(i.uv.x-.5)*2);
                fixed3 structure=tex2D(_MainTex,i.uv*float2(1,.25)).rgb*.025;
                fixed4 color=fixed4(float3(.016,.022,.040)+structure+prism*(panel*(.17+.19*ribbon+.10*pulse)+edge*.48),1);
                UNITY_APPLY_FOG(i.fogCoord,color);return color;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
