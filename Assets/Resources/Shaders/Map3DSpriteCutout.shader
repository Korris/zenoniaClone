// Props that stay sprites on a 3D map (ruins, shed, fence, reeds, signpost...: Map3DProps.MakeCutout). Standing billboards like on the 2.5D maps, but
// alpha-cutout in the opaque queue so the real sun shadow falls on them and they cast one (ShadowCaster pass), lit lightly (sprite art keeps
// ~65 % of its painted light: spec section 5.1 "85 % sprite colour" for characters, props take a bit more scene light), hero occlusion dither.
Shader "ZenoniaProto/Map3DSpriteCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0.5
        _SceneLight ("Scene light share", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Map3DCommon.cginc"
            sampler2D _MainTex; fixed4 _Color; float _Cutoff, _SceneLight;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 col : TEXCOORD1; float4 scr : TEXCOORD2; float eye : TEXCOORD3; SHADOW_COORDS(4) UNITY_FOG_COORDS(5) };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.col = v.color * _Color;
                o.scr = ComputeScreenPos(o.pos);
                o.eye = -UnityObjectToViewPos(v.vertex).z;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv) * i.col;
                clip(t.a - _Cutoff);
                M3DHeroFade(i.scr, i.eye, 0.45);
                float atten = SHADOW_ATTENUATION(i);
                float3 lit = lerp(_M3DShade.rgb, _M3DSun.rgb, M3DRamp(0.75 * atten + 0.1));
                float3 c = t.rgb * lerp(1.0.xxx, lit, _SceneLight);
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
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Cutoff;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; };
            v2f vert(appdata v) { v2f o; v.normal = float3(0,0,-1); TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) o.uv = v.uv; return o; }
            float4 frag(v2f i) : SV_Target { clip(tex2D(_MainTex, i.uv).a - _Cutoff); SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
