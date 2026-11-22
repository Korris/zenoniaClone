// Distant tree / bush billboards of the 3D map (Map3DImpostors, 22/11 perf pass 2). Each quad's 4 vertices sit at the tree's crown centre;
// TEXCOORD0 = (corner x, corner y, sphere radius in world units, tree yaw rad), TEXCOORD1 / 2 = tree root (same cross-fade as the mesh tree:
// Map3DCommon M3DImpFade), TEXCOORD3 = the key's atlas rect (u0, v0, frame w, frame h). The vertex shader spreads the quad along the camera's
// right / up and picks the two baked views nearest to the camera's azimuth in the tree's own frame; the fragment blends them, clips the
// premultiplied coverage, keeps its half of the cross-fade dither and lights the baked albedo / object normal with the Map3DFoliage model
// (wrap 0.35 + soft ramp, light through the leaves, rim) and the baked prop shadow (_M3DBakedImp) at the real surface point (depth in the normal
// atlas alpha). No shadow caster: the mesh trees keep casting.
Shader "ZenoniaProto/Map3DImpostor"
{
    Properties
    {
        _Albedo ("Albedo atlas", 2D) = "black" {}
        _Normal ("Normal + depth atlas", 2D) = "grey" {}
        _Sss ("Subsurface", Range(0,0.4)) = 0.11
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="M3DImpostor" "IgnoreProjector"="True" "DisableBatching"="True" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Map3DCommon.cginc"
            sampler2D _Albedo, _Normal;
            float _Sss;
            struct appdata { float4 vertex : POSITION; float4 c0 : TEXCOORD0; float2 root : TEXCOORD1; float2 rootZ : TEXCOORD2; float4 rect : TEXCOORD3; };
            struct v2f
            {
                float4 pos : SV_POSITION; float4 uv : TEXCOORD0; float3 wp : TEXCOORD1; float4 scr : TEXCOORD2; float3 misc : TEXCOORD3; // misc: blend, fade, radius
                float2 yaw : TEXCOORD4; UNITY_FOG_COORDS(5)
            };
            v2f vert(appdata v)
            {
                v2f o;
                float fade = M3DImpFade(v.root, v.rootZ);
                float3 pivot = mul(unity_ObjectToWorld, v.vertex).xyz;
                float r = v.c0.z;
                float3 right = UNITY_MATRIX_V[0].xyz, up = UNITY_MATRIX_V[1].xyz;
                float3 wp = pivot + (right * v.c0.x + up * v.c0.y) * r;
                o.pos = fade <= 0.0 ? M3D_CULLED_POS : mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                float3 toCam = _WorldSpaceCameraPos - pivot;
                float f = (atan2(toCam.y, toCam.x) - v.c0.w) * (8.0 / 6.2831853);
                f -= 8.0 * floor(f / 8.0);
                float j0 = floor(f), j1 = j0 + 1.0;
                if (j1 >= 8.0) j1 = 0.0;
                float2 l = v.c0.xy * 0.5 + 0.5;
                l = l * 0.985 + 0.0075; // stay inside the frame: mips of the neighbour frame never bleed in
                o.uv = float4(v.rect.x + (j0 + l.x) * v.rect.z, v.rect.y + l.y * v.rect.w, v.rect.x + (j1 + l.x) * v.rect.z, v.rect.y + l.y * v.rect.w);
                o.misc = float3(f - j0, fade, r);
                o.yaw = float2(cos(v.c0.w), sin(v.c0.w));
                o.wp = wp;
                o.scr = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float w = i.misc.x;
                float4 a = lerp(tex2Dbias(_Albedo, float4(i.uv.xy, 0, -0.75)), tex2Dbias(_Albedo, float4(i.uv.zw, 0, -0.75)), w); // a bit sharper than trilinear: shown near 1:1
                clip(a.a - 0.5);
                if (i.misc.y < 1.0) clip(i.misc.y - M3DBayer(M3DPixel(i.scr))); // impostor half of the cross-fade
                float4 nd = lerp(tex2D(_Normal, i.uv.xy), tex2D(_Normal, i.uv.zw), w);
                float3 no = nd.xyz * 2.0 - 1.0;
                no = normalize(no + float3(0, 0, -1e-4));
                float3 n = float3(i.yaw.x * no.x - i.yaw.y * no.y, i.yaw.y * no.x + i.yaw.x * no.y, no.z); // object -> world (turn round z)
                float3 albedo = a.rgb / a.a;
                float3 V = normalize(_WorldSpaceCameraPos - i.wp);
                float3 surf = i.wp + V * ((nd.a * 2.0 - 1.0) * i.misc.z); // the visible leaf, not the billboard plane
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float atten = M3DBakedSunP(surf, n, _M3DBakedImp);
                float3 c = M3DShade(albedo, n, L, atten, 0.35);
                float back = pow(saturate(dot(V, -L)), 3.0) * saturate(dot(-n, L) + 0.4);
                c += albedo * _M3DSun.rgb * back * _Sss * (0.5 + 0.5 * atten);
                c += albedo * pow(1.0 - saturate(dot(n, V)), 3.0) * 0.06 * atten;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
}
