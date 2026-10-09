Shader "HillAdventurer/RacingVehicleBadge"
{
    Properties
    {
        _MainTex ("Font atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Back
        ZWrite On
        ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct v2f { float4 position:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed alpha=tex2D(_MainTex,i.uv).a*i.color.a;
                clip(alpha-.35);
                return fixed4(i.color.rgb,1);
            }
            ENDCG
        }
    }
}
