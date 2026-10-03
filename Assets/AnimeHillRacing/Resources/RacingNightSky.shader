Shader "HillAdventurer/NightCitySky"
{
    Properties { _Top("Zenith",Color)=(.008,.014,.055,1) _Horizon("City haze",Color)=(.13,.085,.19,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; float3 direction:TEXCOORD0; };
            float4 _Top,_Horizon;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction); float h=saturate(d.y);
                float3 color=lerp(_Horizon.rgb,_Top.rgb,smoothstep(0,.70,h));
                color+=float3(.01,.045,.04)*exp(-h*16);
                float2 uv=float2(atan2(d.z,d.x)*.15915494+.5,asin(clamp(d.y,-1,1))*.63661977);
                float2 cell=floor(uv*float2(230,100)),p=frac(uv*float2(230,100));
                float seed=hash(cell),dist=length(p-float2(hash(cell+1),hash(cell+2)));
                float star=(1-smoothstep(.015,.065,dist))*step(.974,seed)*smoothstep(.025,.15,h);
                float twinkle=.82+.18*sin(_Time.y*(.4+seed)+seed*47);
                color+=star*twinkle*lerp(float3(.65,.83,1),float3(1,.90,.70),seed);
                if(d.y<0)color=lerp(float3(.018,.021,.038),_Horizon.rgb,saturate(1+d.y*8));
                return fixed4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
