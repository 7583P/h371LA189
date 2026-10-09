Shader "HillAdventurer/Racing Coin"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position:SV_POSITION; fixed4 color:COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                v2f output;output.position=UnityObjectToClipPos(input.vertex);
                float3 normal=UnityObjectToWorldNormal(input.normal);
                // Stable readable gold on the night circuit; one opaque pass,
                // no transparent overdraw, shadow pass, texture or dynamic light.
                float light=.72+.28*saturate(dot(normal,normalize(float3(-.4,.8,-.6))));
                output.color=fixed4(input.color.rgb*light,1);
                UNITY_TRANSFER_FOG(output,output.position);return output;
            }
            fixed4 frag(v2f input):SV_Target
            {
                fixed4 color=input.color;UNITY_APPLY_FOG(input.fogCoord,color);return color;
            }
            ENDCG
        }
    }
}
