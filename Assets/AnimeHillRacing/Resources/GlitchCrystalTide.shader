Shader "AnimeHillRacing/GlitchCrystalTide"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _TideSpeed ("Tide Speed", Float) = 0.21
        [PerRendererData] _TideScale ("Tide Scale", Float) = 1.65
        [PerRendererData] _CrystalDensity ("Crystal Density", Float) = 8.5
        [PerRendererData] _GlowIntensity ("Glow Intensity", Range(0,2)) = 0.62
        [PerRendererData] _PhaseOffset ("Phase Offset", Float) = 0
        [PerRendererData] _RadialFlow ("Radial Flow", Range(0,1)) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;
            float _TideSpeed;
            float _TideScale;
            float _CrystalDensity;
            float _GlowIntensity;
            float _PhaseOffset;
            float _RadialFlow;
            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;

            v2f vert(appdata_t input)
            {
                v2f output;
                float2 flip = _Flip.xy;
                if (abs(flip.x) < .001) flip.x = 1.0;
                if (abs(flip.y) < .001) flip.y = 1.0;
                input.vertex.xy *= flip;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                fixed4 rendererColor = _RendererColor;
                if (rendererColor.a < .001 && dot(rendererColor.rgb, rendererColor.rgb) < .001)
                    rendererColor = fixed4(1,1,1,1);
                output.color = input.color * _Color * rendererColor;
                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif
                return output;
            }

            fixed4 SampleSprite(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif
                return color;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float2 Hash22(float2 value)
            {
                float first = Hash21(value + 17.17);
                float second = Hash21(value.yx + float2(61.73, 29.41));
                return float2(first, second);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = SampleSprite(input.texcoord) * input.color;
                float2 centered = input.texcoord - .5;
                float angle = atan2(centered.y, centered.x) / 6.2831853 + .5;
                float radius = length(centered);
                float time = _Time.y * _TideSpeed;

                // Crossed waves travel with different directions and velocities. A radial wave
                // expands through them so the crystalline light never reads as a one-way stripe.
                float2 directionA = normalize(float2(.93, .37));
                float2 directionB = normalize(float2(-.42, .91));
                float2 directionC = normalize(float2(.69, -.72));
                float phaseA = dot(centered, directionA) * _TideScale - time + _PhaseOffset;
                float phaseB = dot(centered, directionB) * (_TideScale * 1.13) +
                    time * 1.07 + _PhaseOffset * .73;
                float phaseC = dot(centered, directionC) * (_TideScale * .86) -
                    time * .79 - _PhaseOffset * .51;
                float radialPhase = (radius * 1.42 + angle * .18) * _TideScale -
                    time * 1.19 + _PhaseOffset;
                float waveA = .5 + .5 * sin(phaseA * 6.2831853);
                float waveB = .5 + .5 * sin(phaseB * 6.2831853);
                float waveC = .5 + .5 * sin(phaseC * 6.2831853);
                float radialWave = .5 + .5 * sin(radialPhase * 6.2831853);
                float crossedWave = waveA * .38 + waveB * .34 + waveC * .28;
                float wave = lerp(crossedWave, crossedWave * .42 + radialWave * .58,
                    saturate(_RadialFlow));
                float crest = smoothstep(.52, .89, wave);
                float undertow = .5 + .5 * sin((phaseA - phaseB + phaseC * .71 + .31) *
                    6.2831853);

                float2 drift = float2(sin(time * 1.31), cos(time * 1.07)) * .026 +
                    float2(cos(time * .73), sin(time * 1.57)) * .014;
                // Jittered Voronoi facets avoid the horizontal/vertical seams produced by the old
                // square UV cells. The derivative-based width keeps their glow stable in the garage
                // thumbnails instead of collapsing into a visible grid when the sprite is reduced.
                float2 crystalUv = (input.texcoord + drift) * _CrystalDensity;
                float2 cell = floor(crystalUv);
                float2 cellLocal = frac(crystalUv);
                float nearestDistance = 8.0;
                float secondDistance = 8.0;
                float facet = 0.5;
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbour = float2(x, y);
                        float2 feature = neighbour + .13 +
                            Hash22(cell + neighbour + _PhaseOffset * 7.31) * .74;
                        float distanceSquared = dot(feature - cellLocal, feature - cellLocal);
                        if (distanceSquared < nearestDistance)
                        {
                            secondDistance = nearestDistance;
                            nearestDistance = distanceSquared;
                            facet = Hash21(cell + neighbour + _PhaseOffset * 13.17);
                        }
                        else if (distanceSquared < secondDistance)
                        {
                            secondDistance = distanceSquared;
                        }
                    }
                }
                float edgeDistance = max(0.0, sqrt(secondDistance) - sqrt(nearestDistance));
                float edgeWidth = max(fwidth(edgeDistance) * 1.35, .012);
                float crystalEdge = 1.0 - smoothstep(edgeWidth, .11 + edgeWidth, edgeDistance);

                // Pale crystal palette: pink, ice blue and white. Green is intentionally absent
                // as a standalone band so it can never dominate the vehicle.
                fixed3 palePink = fixed3(1.00, .66, .86);
                fixed3 iceBlue = fixed3(.48, .82, 1.00);
                fixed3 crystalWhite = fixed3(1.00, .98, 1.00);
                float palettePhase = facet + _PhaseOffset + undertow * .16;
                float pinkToBlue = .5 + .5 * sin(palettePhase * 6.2831853);
                float whiteFacet = pow(.5 + .5 * sin((palettePhase * 1.83 + .27) *
                    6.2831853), 2.25);
                fixed3 crystalColor = lerp(palePink, iceBlue, pinkToBlue);
                crystalColor = lerp(crystalColor, crystalWhite, .26 + whiteFacet * .48);

                float tideChromatic = .5 + .5 * sin((phaseA + phaseB - phaseC * .42) *
                    6.2831853);
                float tideWhite = pow(.5 + .5 * sin((radialPhase * 1.37 + .11) *
                    6.2831853), 3.0);
                fixed3 tideColor = lerp(palePink, iceBlue, tideChromatic);
                tideColor = lerp(tideColor, crystalWhite, .18 + tideWhite * .62);
                float luminance = dot(color.rgb, fixed3(.299, .587, .114));
                float facetLight = lerp(.70, 1.34, facet) * lerp(.84, 1.18, undertow);
                fixed3 crystallized = color.rgb * facetLight;
                crystallized = lerp(crystallized,
                    crystalColor * max(.34, luminance) * 1.48, .68);
                float bodyStrength = saturate(.50 + crest * .38 + crystalEdge * .08);
                color.rgb = lerp(color.rgb, crystallized, bodyStrength);
                color.rgb += tideColor * crest * _GlowIntensity * .52;
                color.rgb += crystalColor * crystalEdge * _GlowIntensity * .085;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
