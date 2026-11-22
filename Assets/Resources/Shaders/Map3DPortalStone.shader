// Stone arch of the 3D warp gate (Map3DPortals / Map3DPortalMesh, 22/11/2026): the painted ruin stone (triplanar, like Map3DPainted _Mode 1) with moss
// on top, lit by the shared Map3D sun ramp + real / baked shadows, plus
//  - glowing runes carved on the front / back faces (vertex g = rune face, uv = glyph cell of _RuneTex), breathing with _Breath;
//  - a ForwardAdd pass: the gate's real point light (blue, from the vortex) lights the stone per pixel. The other Map3D shaders have no ForwardAdd,
//    so that light only costs this small mesh one extra pass.
// Vertex colour: r = AO, g = rune, b = moss. Hero occlusion dither like the other solids. World up is -z.
Shader "ZenoniaProto/Map3DPortalStone"
{
    Properties
    {
        _MainTex ("Stone", 2D) = "white" {}
        _MossTex ("Moss", 2D) = "white" {}
        _RuneTex ("Runes (alpha)", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _MossTint ("Moss tint", Color) = (0.66,0.70,0.50,1)
        _RuneColor ("Rune emission (HDR)", Color) = (0.3,0.75,1.6,1)
        _TriScale ("Triplanar texels per unit", Float) = 0.028
        _Breath ("Breath", Float) = 1
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _MossTex, _RuneTex;
    fixed4 _Tint, _MossTint;
    float4 _RuneColor;
    float _TriScale, _Breath;

    float3 PortalTri(sampler2D s, float3 p, float3 n)
    {
        float3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z);
        return tex2D(s, p.yz).rgb * w.x + tex2D(s, p.xz).rgb * w.y + tex2D(s, p.xy).rgb * w.z;
    }

    // painted stone + moss + AO; op = object-space position, n = world normal, wp = world position, col = vertex colour
    float3 PortalAlbedo(float3 op, float3 n, float3 wp, float4 col)
    {
        float3 objN = normalize(mul((float3x3)unity_WorldToObject, n));
        float3 albedo = PortalTri(_MainTex, op * _TriScale, objN) * _Tint.rgb;
        float up = saturate(-n.z);
        float breakup = PortalTri(_MossTex, wp * 0.011, n).g;
        float moss = smoothstep(0.66, 0.92, up + (breakup - 0.5) * 0.55) * saturate(col.b * 1.5) * 0.9;
        albedo = lerp(albedo, PortalTri(_MossTex, wp * 0.018, n) * _MossTint.rgb, moss);
        return albedo * lerp(0.72, 1.0, col.r);
    }
    ENDCG

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
            #pragma target 3.0
            #include "AutoLight.cginc"
            #include "Map3DCommon.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 wp : TEXCOORD2; float4 col : TEXCOORD3;
                float4 scr : TEXCOORD4; float eye : TEXCOORD5; float3 op : TEXCOORD8; SHADOW_COORDS(6) UNITY_FOG_COORDS(7)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.wp = wp; o.op = v.vertex.xyz; o.uv = v.uv; o.col = v.color;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.scr = ComputeScreenPos(o.pos);
                o.eye = -mul(UNITY_MATRIX_V, float4(wp, 1.0)).z;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                M3DHeroFade(i.scr, i.eye, 0.55);
                float3 n = normalize(i.n);
                float3 albedo = PortalAlbedo(i.op, n, i.wp, i.col);
                float glyph = tex2D(_RuneTex, i.uv).a * step(0.5, i.col.g);
                albedo *= 1.0 - glyph * 0.55; // carved groove under the glow
                float atten = SHADOW_ATTENUATION(i) * M3DBakedSun(i.wp, n);
                float3 c = M3DShade(albedo, n, normalize(_WorldSpaceLightPos0.xyz), atten, 0.25);
                c += glyph * _RuneColor.rgb * _Breath;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return fixed4(c, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityLightingCommon.cginc" // _LightColor0
            #include "AutoLight.cginc"
            #include "Map3DCommon.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 wp : TEXCOORD1; float4 col : TEXCOORD2; float3 op : TEXCOORD3;
                float4 scr : TEXCOORD4; float eye : TEXCOORD5; UNITY_FOG_COORDS(6) SHADOW_COORDS(7)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.wp = wp; o.op = v.vertex.xyz; o.col = v.color;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.scr = ComputeScreenPos(o.pos);
                o.eye = -mul(UNITY_MATRIX_V, float4(wp, 1.0)).z;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                M3DHeroFade(i.scr, i.eye, 0.55);
                float3 n = normalize(i.n);
                float3 albedo = PortalAlbedo(i.op, n, i.wp, i.col);
                float3 L = normalize(_WorldSpaceLightPos0.xyz - i.wp * _WorldSpaceLightPos0.w);
                UNITY_LIGHT_ATTENUATION(atten, i, i.wp);
                float w = saturate((dot(n, L) + 0.35) / 1.35); // wrapped: the glow reaches round the block edges
                float3 c = albedo * _LightColor0.rgb * w * atten;
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0, 0, 0, 0));
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
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { V2F_SHADOW_CASTER; };
            v2f vert(appdata v) { v2f o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) return o; }
            float4 frag(v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
