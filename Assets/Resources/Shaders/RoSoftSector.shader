// "RO trailer style" ground sector (21/11/2026, docs/reference/ro-trailer-skill-style/prompt-templates.md): the big pale Bash / Spiral Pierce fan
// lying flat on the ground, drawn on a quad whose mesh is already turned toward the aim and squashed onto the ground plane (ClericLightFx.Aim).
// Quad uv -> p = uv*2-1; the fan points along +x and spans +-_Half rad; a full ring when _Half >= PI.
//  - body (Alpha Blend): Outer colour near the centre -> Body colour outward, alpha _BodyColor.a (0.18-0.40), uneven soft streaks (not a flat wedge),
//    transparent hole up to _Inner, soft side edges, soft outer fringe;
//  - Bright rim band near r = _RimR (alpha _BrightColor.a) and a thin Core line on it (additive, _CoreWhite), a soft light on the sweep front;
//  - _Reveal (0..1, per renderer): opening sweep from -_Half to +_Half (mesh v flipped = the other way round); 1 = whole fan;
//  - _Fade, _Dissolve (per renderer) like LightBlob. No dark rim anywhere (RO Translucent Light rules).
Shader "ZenoniaProto/RoSoftSector"
{
    Properties
    {
        _Half ("Half angle (rad)", Float) = 1.4
        _Inner ("Transparent centre radius", Float) = 0.12
        _RimR ("Rim band radius", Float) = 0.9
        _RimW ("Rim band width", Float) = 0.05
        _OuterColor ("Outer (inner part) colour", Color) = (0.7, 0.66, 0.45, 1)
        _BodyColor ("Body colour (a = body alpha)", Color) = (0.84, 0.76, 0.47, 0.28)
        _BrightColor ("Bright rim colour (a = rim alpha)", Color) = (0.96, 0.92, 0.49, 0.6)
        [HDR] _EmissionColor ("Additive rim / front light (HDR)", Color) = (0.5, 0.45, 0.2, 1)
        _CoreWhite ("Core line on the rim", Float) = 0.6
        _Streaks ("Uneven light amount", Float) = 0.35
        _Fade ("Fade", Range(0, 1)) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Reveal ("Opening sweep", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        float _Half, _Inner, _RimR, _RimW, _CoreWhite, _Streaks, _Fade, _Dissolve, _Reveal;
        float4 _OuterColor, _BodyColor, _BrightColor, _EmissionColor;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

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

        // x = coverage (sides, opening sweep, radius), y = rim band, z = core line, w = sweep-front light; r in o.
        float4 Shape(float2 uv, out float r, out float body)
        {
            float2 p = uv * 2.0 - 1.0;
            r = length(p);
            float a = atan2(p.y, p.x);
            float full = step(3.1, _Half);
            float s = a / max(0.05, _Half);                                    // -1 .. 1 across the fan
            float side = full > 0.5 ? 1.0 : 1.0 - smoothstep(0.84, 1.0, abs(s));
            float front = -1.0 + 2.0 * saturate(_Reveal) * 1.04;              // sweep front in s units
            float open = full > 0.5 ? step(frac((a / 6.2831853) + 0.5), saturate(_Reveal) * 1.0001) : 1.0 - smoothstep(front - 0.06, front, s);
            float lead = full > 0.5 ? 0.0 : exp(-pow((s - front + 0.05) / 0.09, 2.0)) * (1.0 - smoothstep(0.85, 1.0, _Reveal));
            float outer = 1.0 - smoothstep(_RimR + _RimW * 0.6, 1.0, r);
            float cover = side * open * outer * smoothstep(_Inner, _Inner + 0.22, r);
            float band = exp(-pow((r - _RimR) / _RimW, 2.0));
            float core = exp(-pow((r - _RimR) / (_RimW * 0.28), 2.0));
            // soft radial light rays (mostly angle-dependent noise: brighter / fainter spokes from the hero, as in the Bash concept)
            float sa = (full > 0.5 ? a * 2.0 : s * _Half * 2.0);
            float n = Noise(float2(sa * 4.5 + 3.0, r * 0.7)) * 0.65 + Noise(float2(sa * 11.0, r * 1.3)) * 0.35;
            float ray = smoothstep(0.62, 0.95, n);
            body = (0.6 + 0.4 * smoothstep(_Inner, _RimR, r)) * lerp(1.0, 0.5 + 0.7 * n + 0.6 * ray, _Streaks);
            return float4(cover, band * side * open, core * side * open, lead * side * outer * smoothstep(_Inner, _Inner + 0.3, r));
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
                float r, body;
                float4 sh = Shape(i.uv, r, body);
                float3 c = lerp(_OuterColor.rgb, _BodyColor.rgb, smoothstep(_Inner, _RimR * 0.8, r));
                c = lerp(c, _BrightColor.rgb, saturate(sh.y + sh.w * 0.7));
                float a = sh.x * body * _BodyColor.a + sh.y * _BrightColor.a + sh.w * _BrightColor.a * 0.6;
                return half4(c, saturate(a) * _Fade * Keep(i.uv));
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
                float r, body;
                float4 sh = Shape(i.uv, r, body);
                float k = _Fade * Keep(i.uv);
                float3 c = _EmissionColor.rgb * (sh.y * 0.8 + sh.w * 0.9 + sh.x * body * 0.08) + _CoreWhite * sh.z;
                return half4(c * k, 1.0);
            }
            ENDCG
        }
    }
}
