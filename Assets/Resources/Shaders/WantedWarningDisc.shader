// Ground warning of a Wanted Boss skill drawn by a shader (Mire Hydra Queen light FX, 2026-11-19), on a centred quad MeshRenderer scaled to 2R x 2R,
// so the circle is exactly the skill radius (the map is seen from above: round, never squashed). UV 0..1, p = uv * 2 - 1, r = |p|.
//  - shape: a full disc (_HalfAngle >= 180) or a sector of +-_HalfAngle degrees around local +X (the code rotates the quad to the facing);
//  - _Base: faint tint over the whole area from the first frame, so the full extent reads at once;
//  - fill: solid inside r < _Progress (0..1, grows to the rim exactly at the strike instant) with a brighter leading band at the front;
//  - rim: a band of _RimWidth (in r units, set per size so it stays the same width on screen) along the circle and the sector edges;
//  - swirl: _NoiseTex scrolls slowly and breaks the fill into toxic flecks (alpha x 0.75..1.1), no blinking.
// Pass 1 alpha-blends the colours (reads on the bright stone arena); pass 2 adds a little HDR on the rim only (kept low: no white-out, no bloom sheet).
Shader "ZenoniaProto/WantedWarningDisc"
{
    Properties
    {
        _NoiseTex ("Swirl noise (grey)", 2D) = "gray" {}
        _FillColor ("Fill (alpha = strength)", Color) = (0.55, 0.15, 0.8, 0.42)
        _FleckColor ("Swirl flecks", Color) = (0.45, 0.85, 0.2, 1)
        _RimColor ("Rim (alpha = strength)", Color) = (0.9, 0.35, 1, 0.95)
        [HDR] _RimGlow ("Rim glow (HDR, additive)", Color) = (0.25, 0.08, 0.35, 1)
        _Base ("Whole-area tint", Range(0, 1)) = 0.18
        _Progress ("Fill radius 0..1", Range(0, 1)) = 0.5
        _HalfAngle ("Sector half angle (deg, >= 180 = disc)", Float) = 180
        _RimWidth ("Rim width (r units)", Range(0.002, 0.3)) = 0.04
        _Fade ("Fade", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _NoiseTex;
        float4 _FillColor, _FleckColor, _RimColor, _RimGlow;
        float _Base, _Progress, _HalfAngle, _RimWidth, _Fade;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

        // x = inside (0/1 soft), y = rim strength, z = fill strength, w = fleck amount
        float4 Shape(float2 uv)
        {
            float2 p = uv * 2.0 - 1.0;
            float r = length(p);
            float aa = fwidth(r) * 1.5 + 1e-4;
            float inside = 1.0 - smoothstep(1.0 - aa, 1.0, r);
            float rim = 1.0 - smoothstep(0.0, _RimWidth, abs(r - (1.0 - _RimWidth)));
            if (_HalfAngle < 179.5)
            {
                float ang = abs(degrees(atan2(p.y, p.x)));                       // 0 along +X
                float edgeDist = (_HalfAngle - ang) * 0.01745329 * r;            // distance (r units) to the nearest sector edge
                float aaE = fwidth(edgeDist) * 1.5 + 1e-4;
                inside *= smoothstep(-aaE, aaE, edgeDist);
                rim = max(rim, (1.0 - smoothstep(0.0, _RimWidth * 1.2, abs(edgeDist - _RimWidth))) * step(r, 1.0));
                rim *= step(-_RimWidth, edgeDist);
            }
            float front = 1.0 - smoothstep(0.0, 0.08, abs(r - _Progress));      // bright band at the growing edge
            float fill = (1.0 - smoothstep(_Progress - 0.01, _Progress + 0.01, r)) * 0.8 + front * 0.6 * step(0.02, _Progress);
            float n = tex2D(_NoiseTex, p * 0.55 + float2(_Time.y * 0.05, -_Time.y * 0.08)).r;
            float n2 = tex2D(_NoiseTex, p * 1.1 + float2(-_Time.y * 0.07, _Time.y * 0.04)).r;
            float fleck = smoothstep(0.62, 0.8, n * 0.6 + n2 * 0.5);
            return float4(inside, rim * inside, fill * inside, fleck * inside);
        }
        ENDCG

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float4 s = Shape(i.uv);
                float fillA = (_Base + s.z) * _FillColor.a;
                float3 c = lerp(_FillColor.rgb, _FleckColor.rgb, s.w * saturate(s.z + _Base) * 0.8);
                float a = saturate(fillA * s.x * (0.85 + 0.3 * s.w));
                c = lerp(c, _RimColor.rgb, s.y);
                a = max(a, s.y * _RimColor.a);
                return half4(c, a * _Fade);
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
                float4 s = Shape(i.uv);
                return half4(_RimGlow.rgb * (s.y + s.z * 0.15) * _Fade, 1.0);
            }
            ENDCG
        }
    }
}
