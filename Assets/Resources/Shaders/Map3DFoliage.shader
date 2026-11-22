// Leaf cards of the 3D map trees and bushes (Map3DTreeBuilder; ChatGPT spec section 2): hand-painted cluster atlas (ChatGPT), alpha cutout (mip-
// compensated so far crowns do not thin out), spherical / ellipsoidal normals baked in the mesh (factor 0.8-0.9), wrap 0.35 + soft 3-step ramp,
// fake subsurface on leaves facing away from the sun (+8-14 %), faint rim (+5 %), no specular. Vertex colour: r = interior / underside darkening
// (1 = outer top), g = wind weight, b = per-cluster random (value / hue spread +-6 %), a = species tint index blend (0 = _Tint, 1 = _Tint2).
// Depth pre-pass (22/11 perf pass): pass LEAFDEPTH writes only depth with the cutout + hero dissolve, the lit pass then runs once per pixel
// with ZTest Equal and no clip (crowns of overlapping alpha-tested cards no longer run the lit shader on every hidden layer). `--map3d-test noprepass` = the old single pass.
// Real shadows: ShadowCaster pass with the same cutout and sway; only cards of 6 atlas cells cast (Map3DMeshKit.CastsShadow, submesh 2). Hero occlusion: dithered dissolve (Map3DCommon M3DHeroFade).
Shader "ZenoniaProto/Map3DFoliage"
{
    Properties
    {
        _MainTex ("Leaf atlas", 2D) = "white" {}
        _Cutoff ("Cutoff", Range(0,1)) = 0.47
        _Tint ("Tint", Color) = (1,1,1,1)
        _Tint2 ("Tint 2", Color) = (1,1,1,1)
        _Sss ("Subsurface", Range(0,0.4)) = 0.12
        _WindMul ("Wind", Float) = 1
        [HideInInspector] _CastCells ("Only cards of the cast cells cast (Map3DShadowBake)", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _LeafZTest ("Lit pass ZTest (Equal with the depth pre-pass)", Float) = 3
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" "DisableBatching"="False" }
        Cull Off

        Pass
        {
            Name "LEAFDEPTH"
            Tags { "LightMode"="Always" }
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Map3DCommon.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float _Cutoff, _WindMul;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 scr : TEXCOORD1; float eye : TEXCOORD2; };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 wp = M3DLeafWorld(v.vertex, v.color.g, _WindMul);
                o.pos = M3DLeafClip(wp);
                o.uv = v.uv;
                o.scr = ComputeScreenPos(o.pos);
                o.eye = -mul(UNITY_MATRIX_V, float4(wp, 1.0)).z;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a * (1.0 + M3DMipLevel(i.uv, _MainTex_TexelSize.zw) * 0.22) - _Cutoff); // keep coverage in the small mips
                M3DHeroFade(i.scr, i.eye, 0.32);
                return 0;
            }
            ENDCG
        }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            ZTest [_LeafZTest]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ M3D_LEAF_NOPREPASS
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Map3DCommon.cginc"

            sampler2D _MainTex; float4 _MainTex_ST, _MainTex_TexelSize;
            float _Cutoff, _Sss, _WindMul;
            fixed4 _Tint, _Tint2;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 wp : TEXCOORD2; float4 col : TEXCOORD3;
                float4 scr : TEXCOORD4; float eye : TEXCOORD5; SHADOW_COORDS(6) UNITY_FOG_COORDS(7)
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 wp = M3DLeafWorld(v.vertex, v.color.g, _WindMul);
                o.pos = M3DLeafClip(wp); // same code as LEAFDEPTH: ZTest Equal needs the exact same depth
                o.wp = wp;
                o.uv = v.uv;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.col = v.color;
                o.scr = ComputeScreenPos(o.pos);
                o.eye = -mul(UNITY_MATRIX_V, float4(wp, 1.0)).z;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
            #if defined(M3D_LEAF_NOPREPASS)
                clip(t.a * (1.0 + M3DMipLevel(i.uv, _MainTex_TexelSize.zw) * 0.22) - _Cutoff); // keep coverage in the small mips
                M3DHeroFade(i.scr, i.eye, 0.32);
            #endif                                                                             // else LEAFDEPTH clipped: ZTest Equal keeps its pixels
                float3 n = normalize(i.n);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.wp);
                fixed3 tint = lerp(_Tint.rgb, _Tint2.rgb, i.col.a);
                float var = 1.0 + (i.col.b - 0.5) * 0.12;
                float3 albedo = t.rgb * tint * var * lerp(0.62, 1.0, i.col.r);
                float atten = SHADOW_ATTENUATION(i) * M3DBakedSun(i.wp, n);
                float3 c = M3DShade(albedo, n, L, atten, 0.35);
                float back = pow(saturate(dot(V, -L)), 3.0) * saturate(dot(-n, L) + 0.4);
                c += albedo * _M3DSun.rgb * back * _Sss * (0.5 + 0.5 * atten);               // light through the leaves
                c += albedo * pow(1.0 - saturate(dot(n, V)), 3.0) * 0.06 * atten;              // soft rim on the crown edge
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Map3DCommon.cginc"
            sampler2D _MainTex; float _Cutoff, _WindMul;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += M3DWind(wp, v.color.g) * _WindMul;
                v.vertex = mul(unity_WorldToObject, float4(wp, 1.0));
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.uv;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a * 1.25 - _Cutoff);
                float2 cell = floor(i.uv * 4.0);                                   // round 1: ~half the cards cast (by atlas cell), lighter dappled canopy shadows
                clip(0.3 - frac((cell.y * 4.0 + cell.x) * 0.618034));               // round 2: ~1/3 (6 of 16 cells); 22/11: a fixed cell set that Map3DMeshKit.CastsShadow
                                                                                    // matches, so the never-casting cards sit in a renderer that skips the shadow pass
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
