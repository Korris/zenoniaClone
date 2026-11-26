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
        _PavingTint ("Paving tint", Color) = (1,1,1,1)
        _PavingContrast ("Paving contrast", Float) = 1
        _PavingScale ("Paving repeat (world units)", Float) = 40
        _BedColor ("Water bed", Color) = (0.30,0.38,0.33,1)
        _CtrlRect ("Control rect", Vector) = (0,0,1,1)
        _Tone ("Tone (darkness)", 2D) = "black" {}
        _Detail ("Ground detail on", Float) = 0
        _MapRect ("Map rect (units)", Vector) = (0,0,1,1)
        _FarHaze ("Far haze colour", Color) = (0.86,0.90,0.79,1)
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

            sampler2D _Control, _Grass, _Dirt, _Rock, _Paving, _Noise, _Tone;
            float _PavingContrast, _Detail, _PavingScale;
            float4 _MapRect; fixed4 _FarHaze;
            fixed4 _GrassTint, _ForestTint, _DirtTint, _RockTint, _PavingTint, _BedColor;
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
                float3 nn = abs(normalize(i.n));                            // World Depth V3.1 cliffs: triplanar rock so steep faces are not stretched
                nn = pow(nn, 4.0); nn /= (nn.x + nn.y + nn.z);
                float3 rock = (tex2D(_Rock, w / 48.0).rgb * nn.z + tex2D(_Rock, i.wp.xz / 48.0).rgb * nn.y + tex2D(_Rock, i.wp.yz / 48.0).rgb * nn.x) * _RockTint.rgb;
                float3 pave = tex2D(_Paving, w / _PavingScale).rgb;
                if (_Detail > 0.5)
                {
                    // RO3 loop round 1 (plaza): a second paving sample turned ~60 deg and shifted, mixed in 4-6 m patches, + per-stone value +-6 %, so the slabs stop repeating
                    float2 pr = float2(dot(w, float2(0.5, 0.866)), dot(w, float2(-0.866, 0.5))) / _PavingScale + 0.37;
                    float pm = smoothstep(0.42, 0.58, tex2D(_Noise, w / 80.0 + 0.71).g);
                    pave = lerp(pave, tex2D(_Paving, pr).rgb, pm);
                    pave *= 0.94 + 0.12 * tex2D(_Noise, w / 11.0 + 0.13).b;
                }
                pave *= _PavingTint.rgb;
                pave = saturate((pave - 0.72) * _PavingContrast + 0.72);

                float3 n = normalize(i.n);
                float up = saturate(-n.z);
                float fo = saturate(ctl.g * (0.85 + 0.3 * macro));
                float edgeN = _Detail > 0.5 ? lerp(tex2D(_Noise, w / 14.0 + 0.53).b, tex2D(_Noise, w / 33.0 + 0.17).r, 0.45) - 0.5 : 0.0;  // round 1: ragged grass bites into the road edge (~0.3-0.5 m)
                float dr = _Detail > 0.5 ? smoothstep(0.26, 0.66, ctl.r + (fine - 0.5) * 0.30 + edgeN * 0.34) : smoothstep(0.18, 0.82, ctl.r + (fine - 0.5) * 0.34);
                float pv = smoothstep(0.30, 0.70, ctl.b + (fine - 0.5) * 0.25);
                float rk = smoothstep(0.90, 0.78, up + (fine - 0.5) * 0.08);
                if (_Detail > 0.5)
                {
                    // road: packed lighter band down the middle, worn darker streaks, a few sunken pebbles (rock swatch), no loose gravel
                    float core = smoothstep(0.86, 0.99, ctl.r);
                    float wear = tex2D(_Noise, float2(w.x / 9.0, w.y / 26.0) + 0.21).r;
                    dirt *= lerp(1.0, 1.11, core) * (0.94 + 0.10 * wear); // round 2: packed band +6 %
                    float peb = smoothstep(0.80, 0.86, tex2D(_Noise, w / 7.0 + 0.47).g) * dr;
                    dirt = lerp(dirt, rock * 0.85, peb * 0.35);
                    // cliffs: lighter / darker rock planes in bands up the face, moss in the lowest metre and on ledges
                    float hm = -i.wp.z / 16.0;
                    float band = tex2D(_Noise, float2(hm * 0.28, (w.x + w.y) / 220.0)).r;
                    rock *= 0.84 + 0.30 * band;
                    float moss = (1.0 - smoothstep(0.3, 1.4, hm)) * 0.6 + smoothstep(0.55, 0.75, up) * 0.35;
                    rock = lerp(rock, grass * _ForestTint.rgb * 0.92, saturate(moss) * smoothstep(0.35, 0.65, tex2D(_Noise, w / 30.0 + 0.9).g + 0.15));
                }
                float3 c = lerp(grass, forest, fo);
                c = lerp(c, dirt, dr * 0.96);
                c = lerp(c, pave, pv);
                c = lerp(c, rock, rk);
                c *= 1.0 + (macro - 0.5) * 0.10;
                float blot = tex2D(_Noise, w / 128.0 + 0.31).r;              // round 2: 6-10 m blotches, light #869B59 / dark #647B44 on the grass (+-7 %; RO3 loop round 1: +-10 %)
                float3 tintB = _Detail > 0.5 ? lerp(float3(0.85, 0.88, 0.82), float3(1.11, 1.09, 1.17), smoothstep(0.3, 0.7, blot)) /* round 2: +-15 % */
                                              : lerp(float3(0.92, 0.94, 0.88), float3(1.06, 1.04, 1.10), smoothstep(0.3, 0.7, blot));
                c *= lerp(1.0.xxx, tintB, (1.0 - dr) * (1.0 - pv) * (1.0 - rk));
                c = lerp(c, _BedColor.rgb * (0.8 + 0.4 * fine), smoothstep(0.2, 0.75, ctl.a) * 0.85);
                c *= 1.0 - tex2D(_Tone, cuv).r;                              // contact AO / cliff foot / nest (Map3DGroundDetail)
                if (_Detail > 0.5)
                {
                    // round 2 background depth: ground past the map edge (far hills / ridge) loses ~22 % contrast and lifts ~8 % toward the haze colour
                    float2 o2 = max(_MapRect.xy - w, w - _MapRect.zw);
                    float far = smoothstep(96.0, 384.0, max(o2.x, o2.y));         // 6 m .. 24 m past the edge
                    float lum = dot(c, float3(0.3, 0.59, 0.11));
                    float3 hazed = lerp(lerp(c, lum.xxx, 0.22), _FarHaze.rgb, 0.18) * 1.06;
                    c = lerp(c, hazed, far);
                }

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
