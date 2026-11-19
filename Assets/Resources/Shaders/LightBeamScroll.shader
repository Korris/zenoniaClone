// Light beam / light burst with UV scrolling, noise warp and noise dissolve (Built-in pipeline, quad MeshRenderer, UV 0..1).
// _MainTex = grey light streaks (tileable in y), _NoiseTex = grey swirl noise (tileable). Only the red channel is read, so masks on black work.
// Two passes so it reads on bright snow too (pure additive vanishes there, docs/sprite-rules.md Sổ lỗi 2026-11-03 / 2026-11-16):
//   pass 1 alpha-blends a saturated _BodyColor under the light, pass 2 adds [HDR] _EmissionColor (values > 1 pass the BloomLite threshold 1.25).
// _Polar = 0: vertical beam (u across, v up, rays scroll up). _Polar = 1: radial burst (rays point outward and scroll out, band at _Ring +- _RingWidth).
// Per renderer (MaterialPropertyBlock): _Fade (0..1 overall), _Dissolve (0 solid .. 1 gone), _Ring.
Shader "ZenoniaProto/LightBeamScroll"
{
    Properties
    {
        _MainTex ("Light streaks (grey)", 2D) = "white" {}
        _MainTiling ("Streak tiling (xy)", Vector) = (1, 1, 0, 0)
        _ScrollSpeed ("Scroll speed (uv/s)", Float) = 1.6
        _StreakMin ("Streak floor", Range(0, 1)) = 0.35
        _NoiseTex ("Swirl noise (grey)", 2D) = "gray" {}
        _NoiseTiling ("Noise tiling (xy)", Vector) = (1, 1, 0, 0)
        _NoiseScroll ("Noise scroll (uv/s)", Float) = 0.45
        _Distort ("Noise warp", Range(0, 0.5)) = 0.12
        [HDR] _EmissionColor ("Emission (HDR)", Color) = (0.55, 1.5, 3.2, 1)
        _CoreWhite ("White-hot core", Float) = 1.4
        _BodyColor ("Body under the light (alpha = strength)", Color) = (0.12, 0.45, 1, 0.45)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _DissolveEdge ("Dissolve edge width", Range(0.01, 0.5)) = 0.12
        [HDR] _EdgeColor ("Dissolve edge glow (HDR)", Color) = (1.2, 2.4, 4, 1)
        _Fade ("Fade", Range(0, 1)) = 1
        _EdgeSoft ("Beam side softness", Range(0.01, 1)) = 0.6
        _BottomFade ("Beam foot fade", Range(0.001, 1)) = 0.05
        _TopFade ("Beam top fade", Range(0.001, 1)) = 0.45
        _Polar ("Radial burst (0/1)", Float) = 0
        _Ring ("Burst band radius (0..1)", Range(0, 1)) = 0.6
        _RingWidth ("Burst band width", Range(0.01, 1)) = 0.35
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex, _NoiseTex;
    float4 _MainTiling, _NoiseTiling;
    float _ScrollSpeed, _StreakMin, _NoiseScroll, _Distort, _CoreWhite, _Dissolve, _DissolveEdge, _Fade;
    float _EdgeSoft, _BottomFade, _TopFade, _Polar, _Ring, _RingWidth;
    float4 _EmissionColor, _BodyColor, _EdgeColor;

    struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    // x = light intensity (0..1), y = dissolve edge glow (0..1)
    float2 Light(float2 uv)
    {
        float t = _Time.y;
        float2 beamUv, shapeUv;
        float shape;
        if (_Polar > 0.5)
        {
            float2 p = uv * 2.0 - 1.0;
            float r = length(p);
            float ang = atan2(p.y, p.x) * 0.15915494 + 0.5;          // 0..1 around the circle
            beamUv = float2(ang, r);
            shapeUv = float2(ang, r);
            float band = 1.0 - saturate(abs(r - _Ring) / _RingWidth);
            shape = band * band * (3.0 - 2.0 * band) * step(r, 1.0);
        }
        else
        {
            beamUv = uv;
            shapeUv = uv;
            float across = 1.0 - abs(uv.x * 2.0 - 1.0);
            shape = smoothstep(0.0, _EdgeSoft, across) * smoothstep(0.0, _BottomFade, uv.y) * (1.0 - smoothstep(1.0 - _TopFade, 1.0, uv.y));
        }
        float n = tex2D(_NoiseTex, shapeUv * _NoiseTiling.xy + float2(0.0, -t * _NoiseScroll)).r;
        float2 suv = beamUv * _MainTiling.xy + float2((n - 0.5) * _Distort, -t * _ScrollSpeed);  // rays travel up (beam) / outward (burst)
        float streak = lerp(_StreakMin, 1.0, tex2D(_MainTex, suv).r);
        float d = n - _Dissolve * (1.0 + _DissolveEdge);                    // _Dissolve 1 clears even the brightest noise
        float keep = smoothstep(0.0, _DissolveEdge, d);
        float edge = (1.0 - smoothstep(0.0, _DissolveEdge * 1.5, abs(d))) * step(0.001, _Dissolve);
        return float2(streak * shape * keep * _Fade, edge * shape * _Fade);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        // 1: saturated body, alpha-blended, so the beam still shows as blue on white snow
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBody
            half4 fragBody(v2f i) : SV_Target
            {
                float2 l = Light(i.uv);
                return half4(_BodyColor.rgb, saturate(l.x * 1.6) * _BodyColor.a);
            }
            ENDCG
        }

        // 2: HDR light added on top (bloom picks up everything above 1.25)
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGlow
            half4 fragGlow(v2f i) : SV_Target
            {
                float2 l = Light(i.uv);
                float3 c = _EmissionColor.rgb * l.x + _CoreWhite * pow(l.x, 2.5) + _EdgeColor.rgb * l.y;
                return half4(c, 1.0);
            }
            ENDCG
        }
    }
}
