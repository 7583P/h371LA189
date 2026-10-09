Shader "Racing/WetCityRoad" {
    Properties { _Color ("Asphalt tint",Color)=(.05,.07,.10,1) _MainTex ("Fine grain",2D)="white" {} _Glossiness ("Wet smoothness",Range(0,1))=.90 _Metallic ("Reflective road",Range(0,1))=.48 }
    SubShader { Tags {"RenderType"="Opaque"} LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        sampler2D _MainTex;fixed4 _Color;half _Glossiness,_Metallic;
        struct Input {float2 uv_MainTex;float3 worldPos;};
        void surf(Input IN,inout SurfaceOutputStandard o) {
            half grain=tex2D(_MainTex,IN.uv_MainTex).r;
            float2 p=IN.worldPos.xz;
            half wet=saturate(.70+.18*sin(p.x*.13+p.y*.07)+.12*sin(p.y*.31));
            o.Albedo=_Color.rgb*(.70+grain*.30);o.Metallic=_Metallic;o.Smoothness=lerp(.58,_Glossiness,wet);o.Alpha=1;
            // Stylized reflected neon. Long broken streaks follow the road UV;
            // the physical scene remains opaque and has no transparent road layers.
            half stripe=pow(saturate(1-abs(frac(IN.uv_MainTex.x*3+.04*sin(IN.uv_MainTex.y*12))-.5)*9),3);
            half breaks=saturate(.32+.65*sin(IN.uv_MainTex.y*18)+.25*sin(IN.uv_MainTex.y*43));
            half3 neon=lerp(half3(.03,.65,.95),half3(.85,.06,.55),.5+.5*sin(p.x*.019+p.y*.011));
            o.Emission=neon*stripe*breaks*wet*.17;
        }
        ENDCG
    } FallBack "Standard"
}
