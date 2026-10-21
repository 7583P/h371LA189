Shader "HillAdventurer/OrbitalRibbonReview"
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
            fixed3 palette(float lane)
            {
                if(lane<1)return fixed3(1,.16,.24);
                if(lane<2)return fixed3(1,.48,.09);
                if(lane<3)return fixed3(.94,.78,.18);
                if(lane<4)return fixed3(.17,.80,.43);
                if(lane<5)return fixed3(.08,.74,.94);
                if(lane<6)return fixed3(.22,.39,1);
                if(lane<7)return fixed3(.60,.25,.96);
                return fixed3(.94,.18,.62);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float across=saturate(i.uv.x)*8, lane=min(7,floor(across));
                float local=frac(across), seam=smoothstep(.012,.06,local)*(1-smoothstep(.94,.988,local));
                float along=frac(i.uv.y*.5), panel=smoothstep(.012,.045,along)*(1-smoothstep(.955,.988,along));
                float center=1-smoothstep(.09,.19,abs(local-.5));
                float edge=smoothstep(.965,.995,abs(i.uv.x-.5)*2);
                fixed3 road=palette(lane)*(.38+.18*seam*panel+.07*center);
                road+=tex2D(_MainTex,i.uv*float2(1,.25)).rgb*.018;
                road=lerp(road,fixed3(.64,.87,.94),edge*.82);
                fixed4 color=fixed4(road,1);UNITY_APPLY_FOG(i.fogCoord,color);return color;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
