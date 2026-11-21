// Thin energy ring circling a hero's body (Cleric maintained buff look, 2026-11-21, docs/reference/cleric-buff-status-concept/midgard-fx-map-rings-shadows-chatgpt.md
// section 3). Drawn on the procedural ellipse ribbon of ClericBuffBodyRing: uv.x = angle round the ring (0..1, continuous over the back and front
// halves), uv.y = across the ribbon (0 inner .. 1 outer edge).
// Pass 1 alpha-blends the solid colour: dark rim at both edges, body colour in the middle, the moving bright segments tinted to _HiColor.
// Pass 2 adds [HDR] _CoreColor on a thin core line (~16 % of the width) only, strongest inside the bright segments (Midgard rule: white only in tiny cores).
// Per renderer (MaterialPropertyBlock): _Fade (0..1), _Phase (segment position, turns), _Dissolve (0 whole .. 1 gone, broken up along the ring),
// _Flash (0..1: the whole ring turns to the bright tint and its core glows, a DEF hit; 2026-11-21 RO3 rings restored, palette set by ClericBuffStatusLook).
Shader "ZenoniaProto/BuffBodyRing"
{
    Properties
    {
        _BodyColor ("Body (a = alpha)", Color) = (0.91, 0.64, 0.18, 0.7)
        _RimColor ("Dark rim (a = alpha)", Color) = (0.66, 0.4, 0.1, 0.8)
        _HiColor ("Bright segment tint (a = amount)", Color) = (1, 0.84, 0.42, 0.9)
        [HDR] _CoreColor ("Core glow (HDR)", Color) = (0.6, 0.45, 0.15, 1)
        _Segs ("Bright segments round the ring", Float) = 2
        _SegLen ("Total bright length (fraction of the ring)", Float) = 0.3
        _Fade ("Fade", Range(0, 1)) = 1
        _Phase ("Segment phase (turns)", Float) = 0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Flash ("Flash", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        float4 _BodyColor, _RimColor, _HiColor, _CoreColor;
        float _Segs, _SegLen, _Fade, _Phase, _Dissolve, _Flash;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        // 1 inside a bright segment (soft ends), 0 elsewhere
        float Segment(float u)
        {
            float n = max(1.0, _Segs);
            float d = frac((u - _Phase) * n);              // 0..1 inside one period
            float len = saturate(_SegLen) ;                 // fraction of a period = total fraction of the ring
            float soft = len * 0.35;
            return smoothstep(0.0, soft, d) * (1.0 - smoothstep(len - soft, len, d));
        }

        // 0 = dissolved away: little pieces along the ring vanish one after another
        float Keep(float u)
        {
            if (_Dissolve <= 0.0) return 1.0;
            float h = frac(sin(floor(u * 72.0) * 12.9898) * 43758.5453);
            return saturate((h - _Dissolve * 1.1 + 0.05) * 10.0);
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
                float e = abs(i.uv.y * 2.0 - 1.0);                 // 0 centre .. 1 edge
                float aa = 1.0 - smoothstep(0.8, 1.0, e);           // soft outer edge (no stair steps on a 3-4 px line)
                float rim = smoothstep(0.42, 0.6, e);
                float seg = max(Segment(i.uv.x), _Flash);
                float3 c = lerp(_BodyColor.rgb, _HiColor.rgb, seg * _HiColor.a);
                c = lerp(c, _RimColor.rgb, rim);
                float a = lerp(_BodyColor.a, _RimColor.a, rim) * aa;
                a = lerp(a, max(a, 0.9 * aa), seg * (1.0 - rim));  // the bright part a little more opaque
                return half4(c, a * _Fade * Keep(i.uv.x));
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
                float e = abs(i.uv.y * 2.0 - 1.0);
                float core = 1.0 - smoothstep(0.06, 0.18, e);
                float seg = max(Segment(i.uv.x), _Flash);
                return half4(_CoreColor.rgb * core * (0.12 + 0.88 * seg + _Flash) * _Fade * Keep(i.uv.x), 1.0);
            }
            ENDCG
        }
    }
}
