// Earth Break 3D greatsword (22/11/2026, EarthBreakGreatsword3D + EarthBreakGreatswordMesh): the procedural model wearing the ChatGPT-painted
// front view as its hand-painted texture. Built-in pipeline, gamma space, transparent queue (sorted with the other skill FX by sortingOrder),
// no depth writes (the mesh draws its parts back to front). Two passes:
//  1. halo: the model pushed out from its long axis, additive champagne glow round the silhouette (reads on snow and in caves);
//  2. body: painted albedo (alpha-clipped: the painted outline is the exact silhouette) x soft wrap light. On a 3D map (_SceneLit 1) the light is the
//     scene sun (_WorldSpaceLightPos0) through the Map3D painterly ramp (Map3DCommon.cginc M3DShade); elsewhere a fixed key light from the upper
//     left of the screen (_KeyDir, set per frame). Warm-steel highlight on the pale low-saturation texels (blade), champagne rim, pulsing gold
//     runes (_EmitTex mask), gem glow (vertex alpha), dissolve into golden light (object-space noise, _EdgeColor edge), ivory contact flash.
Shader "ZenoniaProto/GreatswordModel"
{
    Properties
    {
        _MainTex ("Painted albedo", 2D) = "white" {}
        _EmitTex ("Rune mask", 2D) = "black" {}
        _RuneColor ("Rune glow", Color) = (2.1, 1.85, 0.95, 1)
        _GemColor ("Gem glow", Color) = (1.25, 1.2, 0.95, 1)
        _PulseSpeed ("Rune pulse (Hz)", Float) = 0.6
        _PulseMin ("Rune pulse low", Float) = 0.4
        _RuneBoost ("Extra rune glow", Float) = 0
        _Fade ("Fade", Range(0, 1)) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeColor ("Dissolve edge", Color) = (2.2, 1.95, 1.2, 1)
        _Flash ("Contact flash", Range(0, 1)) = 0
        _SceneLit ("Lit by the scene sun", Float) = 0
        _KeyDir ("Fixed key light (toward the light, world)", Vector) = (-0.5, 0.6, -0.6, 0)
        _HaloColor ("Halo colour", Color) = (0.24, 0.21, 0.11, 1)
        _HaloWidth ("Halo width (model units)", Float) = 0.009
        _HaloAlpha ("Halo strength", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _EmitTex;
        float4 _RuneColor, _GemColor, _EdgeColor, _HaloColor, _KeyDir;
        float _PulseSpeed, _PulseMin, _RuneBoost, _Fade, _Dissolve, _Flash, _SceneLit, _HaloWidth, _HaloAlpha;

        float Hash3(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float Noise3(float3 x)
        {
            float3 i = floor(x), f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(Hash3(i), Hash3(i + float3(1, 0, 0)), f.x), lerp(Hash3(i + float3(0, 1, 0)), Hash3(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(Hash3(i + float3(0, 0, 1)), Hash3(i + float3(1, 0, 1)), f.x), lerp(Hash3(i + float3(0, 1, 1)), Hash3(i + float3(1, 1, 1)), f.x), f.y), f.z);
        }

        // burn value of a model point: noise + a bias that takes the pommel first and the point last; < 0 = burnt away
        float Burn(float3 op)
        {
            float n = Noise3(op * 26.0) * 0.65 + Noise3(op * 61.0) * 0.35;
            float k = n * 0.7 + op.y * 0.3;
            return k - (_Dissolve * 1.12 - 0.06);
        }
        ENDCG

        Pass // 1. halo shell
        {
            Tags { "LightMode"="Always" }
            Blend One One
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 op : TEXCOORD1; float gem : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz;
                float2 side = p.xz;
                float len = max(length(side), 1e-4);
                p.xz += side / len * _HaloWidth;          // out from the long axis: one smooth shell (facet normals would split it)
                p.y += (p.y - 0.5) * _HaloWidth * 0.6;
                o.pos = UnityObjectToClipPos(float4(p, 1.0));
                o.uv = v.uv;
                o.op = v.vertex.xyz;
                o.gem = v.color.a;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a - 0.45 + i.gem); // same painted outline as the body (the guard grid is clipped by it)
                clip(Burn(i.op));
                return fixed4(_HaloColor.rgb * _Fade * _HaloAlpha * (1.0 + _Flash * 0.8), 1.0);
            }
            ENDCG
        }

        Pass // 2. painted body
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Lighting.cginc"
            #include "Map3DCommon.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wn : TEXCOORD1; float3 wp : TEXCOORD2; float3 op : TEXCOORD3; float gem : TEXCOORD4; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.op = v.vertex.xyz;
                o.gem = v.color.a;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a - 0.45 + i.gem);              // the painted outline is the silhouette (the gem always stays)
                float burn = Burn(i.op);
                clip(burn);
                float edge = 1.0 - saturate(burn / 0.07);

                float3 N = normalize(i.wn);
                float3 V = normalize(_WorldSpaceCameraPos - i.wp);
                bool scene = _SceneLit > 0.5;
                float3 L = scene ? normalize(_WorldSpaceLightPos0.xyz) : normalize(_KeyDir.xyz);
                float3 col;
                if (scene) col = lerp(tex.rgb, M3DShade(tex.rgb, N, L, 1.0, 0.45), 0.85); // painted shading kept, the sun on top
                else
                {
                    float w = saturate((dot(N, L) + 0.45) / 1.45);
                    col = tex.rgb * lerp(0.6, 1.02, w);
                }
                float hi = max(tex.r, max(tex.g, tex.b)), sat = hi - min(tex.r, min(tex.g, tex.b));
                float steel = saturate(1.0 - sat * 3.0) * smoothstep(0.45, 0.85, hi);  // the pale blade (gold parts have saturated texels)
                float3 Hh = normalize(L + V);
                float spec = pow(saturate(dot(N, Hh)), 32.0) * (0.1 + 0.4 * steel);
                col += spec * (scene ? _M3DSun.rgb : float3(1.0, 0.97, 0.9)) * 0.5;
                col += pow(1.0 - saturate(dot(N, V)), 3.0) * 0.12 * float3(1.0, 0.93, 0.7);   // champagne rim

                float pulse = lerp(_PulseMin, 1.0, 0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2832));
                col += tex2D(_EmitTex, i.uv).r * _RuneColor.rgb * (pulse + _RuneBoost) * 0.38;
                col += i.gem * _GemColor.rgb * (0.25 + 0.25 * pulse + _RuneBoost * 0.2);
                col += edge * _EdgeColor.rgb;
                col = lerp(col, float3(1.0, 0.97, 0.88) * 1.5, _Flash * 0.55);
                return fixed4(col, _Fade);
            }
            ENDCG
        }
    }
}
