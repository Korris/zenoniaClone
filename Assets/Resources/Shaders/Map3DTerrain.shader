// Terrain of the 3D map (Map3DTerrain; ChatGPT spec section 1): 4 painted layers blended by the control texture (Map3DHeightField: r dirt road,
// g forest floor, b paving, a water bed) + rock on slopes. Layers tile in world space (grass every 4 m, dirt 3.5 m, rock 3 m); against visible
// repeats: a second grass sample at 0.37x scale rotated ~30 deg (35 %), and a macro value noise at 12 m (+-5 %). Road edges are broken by noise
// (+-0.15 m) and sharpened so the 8-texel control does not read as a blur. Lit with the shared soft ramp (wrap 0.2), receives real shadows, fog.
// _CtrlRect = control texture rect in world units (xMin, yMin, 1/width, 1/height).
Shader "ZenoniaProto/Map3DTerrain"
{
    Properties
    {
        _Control ("Control", 2D) = "black" {}
        _Grass ("Grass", 2D) = "white" {}
        _Dirt ("Dirt", 2D) = "white" {}
        _Rock ("Rock", 2D) = "white" {}
        _Paving ("Paving", 2D) = "white" {}
        _Noise ("Macro noise", 2D) = "gray" {}
        _GrassTint ("Grass tint", Color) = (1,1,1,1)
        _ForestTint ("Forest tint", Color) = (0.7,0.75,0.65,1)
        _DirtTint ("Dirt tint", Color) = (1,1,1,1)
        _RockTint ("Rock tint", Color) = (1,1,1,1)
        _BedColor ("Water bed", Color) = (0.30,0.38,0.33,1)
        _CtrlRect ("Control rect", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest+20" "RenderType"="Opaque" } // 22/11 perf: drawn AFTER trunks, rocks and leaf cards so the depth test
                                                              // skips the 9-sample splat wherever a crown, trunk or rock already covers it

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Map3DCommon.cginc"

            sampler2D _Control, _Grass, _Dirt, _Rock, _Paving, _Noise;
            fixed4 _GrassTint, _ForestTint, _DirtTint, _RockTint, _BedColor;
            float4 _CtrlRect;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 n : TEXCOORD1; SHADOW_COORDS(2) UNITY_FOG_COORDS(3) };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.n = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 w = i.wp.xy;
                float2 cuv = (w - _CtrlRect.xy) * _CtrlRect.zw;
                float4 ctl = tex2D(_Control, cuv);
                float macro = tex2D(_Noise, w / 192.0).r;                 // 12 m
                float fine = tex2D(_Noise, w / 40.0).g;                   // 2.5 m: edge breakup
                // grass: two scales, the second rotated, against tiling
                float2 g1 = w / 64.0;
                float2 g2 = float2(dot(w, float2(0.866, 0.5)), dot(w, float2(-0.5, 0.866))) / 173.0;
                float3 grass = lerp(tex2D(_Grass, g1).rgb, tex2D(_Grass, g2).rgb, 0.35) * _GrassTint.rgb;
                float3 forest = grass * _ForestTint.rgb;
                float3 dirt = tex2D(_Dirt, w / 56.0).rgb * _DirtTint.rgb;
                float3 rock = tex2D(_Rock, w / 48.0).rgb * _RockTint.rgb;
                float3 pave = tex2D(_Paving, w / 40.0).rgb;

                float3 n = normalize(i.n);
                float up = saturate(-n.z);
                float fo = saturate(ctl.g * (0.85 + 0.3 * macro));
                float dr = smoothstep(0.18, 0.82, ctl.r + (fine - 0.5) * 0.34);
                float pv = smoothstep(0.30, 0.70, ctl.b + (fine - 0.5) * 0.25);
                float rk = smoothstep(0.90, 0.78, up + (fine - 0.5) * 0.08);
                float3 c = lerp(grass, forest, fo);
                c = lerp(c, dirt, dr * 0.96);
                c = lerp(c, pave, pv);
                c = lerp(c, rock, rk);
                c *= 1.0 + (macro - 0.5) * 0.10;
                float blot = tex2D(_Noise, w / 128.0 + 0.31).r;              // round 2: 6-10 m blotches, light #869B59 / dark #647B44 on the grass (+-7 %)
                float3 tintB = lerp(float3(0.92, 0.94, 0.88), float3(1.06, 1.04, 1.10), smoothstep(0.3, 0.7, blot));
                c *= lerp(1.0.xxx, tintB, (1.0 - dr) * (1.0 - pv) * (1.0 - rk));
                c = lerp(c, _BedColor.rgb * (0.8 + 0.4 * fine), smoothstep(0.2, 0.75, ctl.a) * 0.85);

                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float atten = SHADOW_ATTENUATION(i) * M3DBakedSun(i.wp, n);
                float3 col = M3DShade(c, n, L, atten, 0.2);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v2f { V2F_SHADOW_CASTER; };
            v2f vert(appdata_base v) { v2f o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) return o; }
            float4 frag(v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
