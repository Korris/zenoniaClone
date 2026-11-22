// Solid hand-painted meshes of the 3D map (Map3DTreeBuilder trunks / branches, Map3DRockBuilder rocks, logs, stumps; ChatGPT spec section 3):
// _Mode 0 = UV mapped (bark atlas strips: u round the trunk, v up it), _Mode 1 = triplanar (procedural rocks have no UVs worth painting on),
// _Mode 2 = tiling 2x2 atlas, cell picked by vertex alpha (Map3DWoodBuilder: planks, beam grain, shingles, thatch).
// Moss on up-facing surfaces (normal up 0.55 -> 0.85, broken by noise, spec 3.2) from the moss texture, vertex colour r = AO (crevices, the
// ground contact 0.72-0.88), g = wind weight (trunk tops sway a little with the crown), b = moss amount (0 none .. 1 the rule above).
// Wrap 0.25 + the shared soft ramp, receives / casts real shadows, hero occlusion dither like the leaves (trunks keep more coverage).
// Tree impostor LOD on trunks (TEXCOORD1 / 2 = tree root, Map3DCommon M3DImpFade; rocks / wood carry flag 0 and never fade).
Shader "ZenoniaProto/Map3DPainted"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _MossTex ("Moss", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _MossTint ("Moss tint", Color) = (0.62,0.75,0.40,1)
        _Mode ("0 uv, 1 triplanar, 2 atlas 2x2 tiled", Float) = 0
        _TriScale ("Triplanar texels per unit", Float) = 0.02
        _WindMul ("Wind", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Map3DCommon.cginc"

            sampler2D _MainTex, _MossTex;
            fixed4 _Tint, _MossTint;
            float _Mode, _TriScale, _WindMul;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float2 root : TEXCOORD1; float2 rootZ : TEXCOORD2; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 wp : TEXCOORD2; float4 col : TEXCOORD3;
                float4 scr : TEXCOORD4; float eye : TEXCOORD5; float3 op : TEXCOORD8; SHADOW_COORDS(6) UNITY_FOG_COORDS(7) float fade : TEXCOORD9;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += M3DWind(wp, v.color.g) * _WindMul;
                o.fade = M3DImpFade(v.root, v.rootZ);
                o.pos = o.fade >= 1.0 ? M3D_CULLED_POS : mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.wp = wp;
                o.op = v.vertex.xyz; // triplanar in object space: the rock texture does not swim when instances share a mesh
                o.uv = v.uv;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.col = v.color;
                o.scr = ComputeScreenPos(o.pos);
                o.eye = -mul(UNITY_MATRIX_V, float4(wp, 1.0)).z;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float3 Tri(sampler2D s, float3 p, float3 n)
            {
                float3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z);
                return tex2D(s, p.yz).rgb * w.x + tex2D(s, p.xz).rgb * w.y + tex2D(s, p.xy).rgb * w.z;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                M3DHeroFade(i.scr, i.eye, 0.55);
                M3DImpClipMesh(i.scr, i.fade);
                float3 n = normalize(i.n);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 objN = normalize(mul((float3x3)unity_WorldToObject, n));
                float3 albedo;
                if (_Mode > 1.5)
                {
                    // 2x2 atlas, the cell from vertex alpha (0, 1/3, 2/3, 1 = top-left, top-right, bottom-left, bottom-right), uv tiles inside the cell
                    float cell = floor(i.col.a * 3.0 + 0.5);
                    float2 off = float2(fmod(cell, 2.0), 1.0 - floor(cell * 0.5)) * 0.5;
                    float2 cuv = off + (frac(i.uv) * 0.97 + 0.015) * 0.5;
                    albedo = tex2Dgrad(_MainTex, cuv, ddx(i.uv * 0.5), ddy(i.uv * 0.5)).rgb;
                }
                else albedo = _Mode < 0.5 ? tex2D(_MainTex, i.uv).rgb : Tri(_MainTex, i.op * _TriScale, objN);
                albedo *= _Tint.rgb;
                float up = saturate(-n.z);
                float breakup = Tri(_MossTex, i.wp * 0.011, n).g;               // painted moss texture also breaks the edge of the moss
                float moss = smoothstep(0.66, 0.92, up + (breakup - 0.5) * 0.55) * saturate(i.col.b * 1.5) * 0.9; // round 1: 20-30 % of the top, patchy
                float3 mossCol = Tri(_MossTex, i.wp * 0.018, n) * _MossTint.rgb;
                albedo = lerp(albedo, mossCol, moss);
                albedo *= lerp(0.72, 1.0, i.col.r);
                float atten = SHADOW_ATTENUATION(i) * M3DBakedSun(i.wp, n);
                float3 c = M3DShade(albedo, n, L, atten, 0.25);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return fixed4(c, 1.0);
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
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Map3DCommon.cginc"
            float _WindMul;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 root : TEXCOORD1; float2 rootZ : TEXCOORD2; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { V2F_SHADOW_CASTER; float4 scr : TEXCOORD1; float fade : TEXCOORD2; };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += M3DWind(wp, v.color.g) * _WindMul;
                v.vertex = mul(unity_WorldToObject, float4(wp, 1.0));
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.fade = M3DImpFadeCaster(v.root, v.rootZ);
                if (o.fade >= 1.0) o.pos = M3D_CULLED_POS;
                o.scr = ComputeScreenPos(o.pos);
                return o;
            }
            float4 frag(v2f i) : SV_Target { M3DImpClipMesh(i.scr, i.fade); SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
