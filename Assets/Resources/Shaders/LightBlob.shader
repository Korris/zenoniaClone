// Soft light blob on a quad MeshRenderer (Built-in pipeline): the fake ground light that replaces URP's Light2D, the meteor head and the impact ring.
// Mask = grey _MainTex (red channel; white = no texture) x radial falloff exp(-r^2 * _Falloff) (0 = texture only).
// Pass 1 alpha-blends the tint _BodyColor (what makes the light visible on white snow, where adding light changes nothing),
// pass 2 adds [HDR] _EmissionColor (the glow on dark ground; above 1.25 it blooms). _Fade (0..1) per renderer through a MaterialPropertyBlock.
// Optional (defaults change nothing), added 2026-11-20 for the Cleric buff light FX (Midgard Senki rule: only 20-35 % of a ring bright at once):
//  _Arc = (centre angle rad, half width rad, glow floor outside the arc, arcs around the circle): the additive pass keeps `floor` outside the arc
//         (1 = off), the body tints towards _ArcTint.rgb by _ArcTint.a inside it, so the bright arc still reads with Bloom off. Angle per renderer.
//  _Hollow (0..1): alpha grows towards the rim (r^4) and is clipped to the disc: a see-through bubble / shield face.
Shader "ZenoniaProto/LightBlob"
{
    Properties
    {
        _MainTex ("Mask (grey)", 2D) = "white" {}
        _Falloff ("Radial falloff", Float) = 0
        [HDR] _EmissionColor ("Emission (HDR)", Color) = (0.5, 1.4, 3, 1)
        _CoreWhite ("White-hot centre", Float) = 0
        _BodyColor ("Tint (alpha = strength)", Color) = (0.2, 0.55, 1, 0.3)
        _Fade ("Fade", Range(0, 1)) = 1
        _Arc ("Arc (angle, half width, glow floor, count)", Vector) = (0, 1, 1, 1)
        _ArcTint ("Arc body tint (a = amount)", Color) = (1, 1, 1, 0)
        _Hollow ("Hollow centre", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float _Falloff, _CoreWhite, _Fade, _Hollow;
        float4 _EmissionColor, _BodyColor, _Arc, _ArcTint;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        float Mask(float2 uv)
        {
            float2 p = uv * 2.0 - 1.0;
            float fall = _Falloff > 0.0 ? exp(-dot(p, p) * _Falloff) * step(dot(p, p), 1.0) : 1.0;
            float m = tex2D(_MainTex, uv).r * fall * _Fade;
            if (_Hollow > 0.0)
            {
                float r2 = dot(p, p);
                m *= lerp(1.0, saturate(r2 * r2 * 1.15), _Hollow) * (1.0 - smoothstep(0.9, 1.0, sqrt(r2)));
            }
            return m;
        }

        // 1 inside the bright arc(s), 0 outside (soft edge over the outer 55 % of the half width).
        float ArcMask(float2 uv)
        {
            float2 p = uv * 2.0 - 1.0;
            float period = 6.2831853 / max(1.0, _Arc.w);
            float a = atan2(p.y, p.x) - _Arc.x;
            a -= period * floor(a / period + 0.5);
            return 1.0 - smoothstep(_Arc.y * 0.45, _Arc.y, abs(a));
        }
        ENDCG

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBody
            half4 fragBody(v2f i) : SV_Target
            {
                float3 c = _BodyColor.rgb;
                if (_ArcTint.a > 0.0) c = lerp(c, _ArcTint.rgb, ArcMask(i.uv) * _ArcTint.a);
                return half4(c, saturate(Mask(i.uv)) * _BodyColor.a);
            }
            ENDCG
        }
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGlow
            half4 fragGlow(v2f i) : SV_Target
            {
                float m = Mask(i.uv);
                if (_Arc.z < 0.999) m *= lerp(_Arc.z, 1.0, ArcMask(i.uv));
                return half4(_EmissionColor.rgb * m + _CoreWhite * m * m * m, 1.0);
            }
            ENDCG
        }
    }
}
