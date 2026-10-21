Shader "HillAdventurer/OrbitalStructureReview"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; };
            struct v2f { float4 position:SV_POSITION; fixed4 color:COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) { v2f o;o.position=UnityObjectToClipPos(v.vertex);o.color=v.color;UNITY_TRANSFER_FOG(o,o.position);return o; }
            fixed4 frag(v2f i):SV_Target { fixed4 color=i.color;color.a=1;UNITY_APPLY_FOG(i.fogCoord,color);return color; }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
