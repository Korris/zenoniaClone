// Soft light blob on a quad MeshRenderer (Built-in pipeline): the fake ground light that replaces URP's Light2D, the meteor head and the impact ring.
// Mask = grey _MainTex (red channel; white = no texture) x radial falloff exp(-r^2 * _Falloff) (0 = texture only).
// Pass 1 alpha-blends the tint _BodyColor (what makes the light visible on white snow, where adding light changes nothing),
// pass 2 adds [HDR] _EmissionColor (the glow on dark ground; above 1.25 it blooms). _Fade (0..1) per renderer through a MaterialPropertyBlock.
// Optional (defaults change nothing), added 2026-11-20 for the Cleric buff light FX (Midgard Senki rule: only 20-35 % of a ring bright at once):
//  _Arc = (centre angle rad, half width rad, glow floor outside the arc, arcs around the circle): the additive pass keeps `floor` outside the arc
//         (1 = off), the body tints towards _ArcTint.rgb by _ArcTint.a inside it, so the bright arc still reads with Bloom off. Angle per renderer.
//  _Hollow (0..1): alpha grows towards the rim (r^4) and is clipped to the disc: a see-through bubble / shield face.
// Optional (defaults change nothing), added 2026-11-21 for the bold Midgard-style Cleric FX (solid colour body ~65 %, glow ~35 %):
//  _Solid (gain): body alpha = saturate(mask x gain): soft strokes turn into solid, ~30-50 % thicker bands of colour.
//  _RimColor (a = amount): body colour of the stroke edges (mask below _RimEnd), a darker rim that keeps the shape on snow / pale roads.
//  _HiColor (a = amount): body colour where the mask passes _HiStart (glyph / stroke centre), the highlight tone.
//  _GlowPow: the additive pass uses mask^_GlowPow: >1 narrows the glow to the stroke cores (10-20 % of the width), no wide bleaching halo.
// Optional (default 0 changes nothing), added 2026-11-21 for the Cleric RO3 Purification (bad status breaking apart):
//  _Dissolve (0 whole .. 1 gone, per renderer): procedural value noise over the quad, pixels whose noise is under the threshold vanish,
//         so a blob crumbles into specks instead of fading evenly.
// Optional (default 1 changes nothing), added 2026-11-21 for the richer Cleric buff receive FX (rune circle drawing itself in):
//  _Reveal (0 nothing .. 1 whole, per renderer): only the part of the quad within that fraction of a turn (clockwise from the top of the quad)
//         shows, with a brighter head on the sweep front, so a ring / sigil draws itself in.
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
        _Solid ("Body alpha gain", Float) = 1
        _RimColor ("Stroke edge colour (a = amount)", Color) = (0, 0, 0, 0)
        _RimEnd ("Mask where the rim gives way to the body", Float) = 0.4
        _HiColor ("Highlight colour (a = amount)", Color) = (1, 1, 1, 0)
        _HiStart ("Mask where the highlight starts", Float) = 0.8
        _GlowPow ("Glow narrowing power", Float) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Reveal ("Reveal sweep", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float _Falloff, _CoreWhite, _Fade, _Hollow, _Solid, _RimEnd, _HiStart, _GlowPow, _Dissolve, _Reveal;
        float4 _EmissionColor, _BodyColor, _Arc, _ArcTint, _RimColor, _HiColor;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        // Mask without the per-renderer _Fade (the tone / gain steps read the shape itself).
        float RawMask(float2 uv)
        {
            float2 p = uv * 2.0 - 1.0;
            float fall = _Falloff > 0.0 ? exp(-dot(p, p) * _Falloff) * step(dot(p, p), 1.0) : 1.0;
            float m = tex2D(_MainTex, uv).r * fall;
            if (_Hollow > 0.0)
            {
                float r2 = dot(p, p);
                m *= lerp(1.0, saturate(r2 * r2 * 1.15), _Hollow) * (1.0 - smoothstep(0.9, 1.0, sqrt(r2)));
            }
            return m;
        }

        float Mask(float2 uv) { return RawMask(uv) * _Fade; }

        // Dissolve: 1 = kept, 0 = gone. Value noise (two octaves) of the quad uv, threshold _Dissolve, soft 0.08 edge.
        float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        float Noise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
        }
        float Keep(float2 uv)
        {
            if (_Dissolve <= 0.0) return 1.0;
            float n = Noise(uv * 7.0) * 0.65 + Noise(uv * 17.0) * 0.35;
            return smoothstep(_Dissolve * 1.08 - 0.04, _Dissolve * 1.08 + 0.04, n);
        }

        // Reveal sweep: x = 1 on the drawn part (soft 0.015-turn edge), y = brightness of the sweep head (0 once whole).
        float2 RevealMask(float2 uv)
        {
            if (_Reveal >= 0.999) return float2(1.0, 0.0);
            float2 p = uv * 2.0 - 1.0;
            float a = frac((1.5707963 - atan2(p.y, p.x)) / 6.2831853); // 0 at the top, growing clockwise
            float k = 1.0 - smoothstep(_Reveal - 0.015, _Reveal, a);
            float d = (_Reveal - a) / 0.05;
            return float2(k, k * exp(-d * d));
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
                float raw = RawMask(i.uv);
                float3 c = _BodyColor.rgb;
                if (_RimColor.a > 0.0) c = lerp(c, _RimColor.rgb, (1.0 - smoothstep(_RimEnd * 0.35, _RimEnd, raw)) * _RimColor.a);
                if (_HiColor.a > 0.0) c = lerp(c, _HiColor.rgb, smoothstep(_HiStart, min(1.0, _HiStart + 0.15), raw) * _HiColor.a);
                if (_ArcTint.a > 0.0) c = lerp(c, _ArcTint.rgb, ArcMask(i.uv) * _ArcTint.a);
                float2 rv = RevealMask(i.uv);
                if (_HiColor.a > 0.0) c = lerp(c, _HiColor.rgb, rv.y * 0.8);
                return half4(c, saturate(raw * _Solid) * _Fade * _BodyColor.a * Keep(i.uv) * rv.x);
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
                float raw = RawMask(i.uv), m = raw * _Fade;
                float g = (_GlowPow != 1.0 ? pow(raw, _GlowPow) : raw) * _Fade;
                if (_Arc.z < 0.999) { float k = lerp(_Arc.z, 1.0, ArcMask(i.uv)); m *= k; g *= k; }
                float2 rv = RevealMask(i.uv);
                float keep = Keep(i.uv) * (rv.x + rv.y * 1.5); m *= keep; g *= keep;
                return half4(_EmissionColor.rgb * g + _CoreWhite * m * m * m, 1.0);
            }
            ENDCG
        }
    }
}
