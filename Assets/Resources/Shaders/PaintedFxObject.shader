// Painted solid object summoned by a skill (2026-11-21, sprite-rules exception: giant sword / shield / hammer painted by ChatGPT) drawn on a
// quad MeshRenderer of the ClericLightFx timeline, with the shader treatment around it (fire, light, dissolve stay shader / particles):
//  Pass 1 (alpha blend): the painted RGBA x _Tint, a warm under-light toward the foot of the quad (_HeatColor, a = amount, over the lowest
//         _HeatSpan of the height: the impact glow climbing the blade), alpha x _Fade x dissolve.
//  Pass 2 (additive): the rune mask _EmitTex (grey, same layout as _MainTex) in [HDR] _RuneColor, pulsing between _PulseMin and 1 at
//         _PulseSpeed (Hz); the burning edge of the dissolve in [HDR] _EdgeColor; [HDR] _SilColor over the whole silhouette (a hot outline
//         halo when drawn slightly larger behind the object, or the white compression flash; set _Tint.a = 0 for an additive-only copy).
// Per renderer through LightSkillAssets.Set: _Fade (0..1), _Dissolve (0 whole .. 1 gone; noise biased so it burns from the point up).
Shader "ZenoniaProto/PaintedFxObject"
{
    Properties
    {
        _MainTex ("Painted object (RGBA)", 2D) = "white" {}
        _EmitTex ("Rune / emission mask (grey)", 2D) = "black" {}
        _Tint ("Tint (a = body alpha)", Color) = (1, 1, 1, 1)
        _HeatColor ("Under-light colour (a = amount)", Color) = (1, 0.55, 0.2, 0)
        _HeatSpan ("Under-light height (0..1 of the quad)", Float) = 0.3
        [HDR] _RuneColor ("Rune glow (HDR)", Color) = (2.2, 1.1, 0.35, 1)
        _PulseSpeed ("Rune pulse (Hz)", Float) = 2.2
        _PulseMin ("Rune pulse floor", Float) = 0.45
        [HDR] _EdgeColor ("Dissolve edge (HDR)", Color) = (2.4, 0.9, 0.2, 1)
        [HDR] _SilColor ("Silhouette glow (HDR)", Color) = (0, 0, 0, 0)
        _Fade ("Fade", Range(0, 1)) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _EmitTex;
        float4 _Tint, _HeatColor, _RuneColor, _EdgeColor, _SilColor;
        float _HeatSpan, _PulseSpeed, _PulseMin, _Fade, _Dissolve;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        float Noise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
        }

        // x = kept (1) / gone (0), y = burning edge band just inside the cut. Noise in object space (tall quad: more cells along y),
        // biased by height so the point burns first and the erosion climbs to the pommel (ChatGPT 21/11: "from the blade tip upward").
        float2 DissolveMask(float2 uv)
        {
            if (_Dissolve <= 0.0) return float2(1.0, 0.0);
            float n = Noise(uv * float2(6.0, 14.0)) * 0.45 + Noise(uv * float2(15.0, 34.0)) * 0.25 + uv.y * 0.3;
            float t = _Dissolve * 1.1 - 0.05;
            float keep = smoothstep(t - 0.02, t + 0.02, n);
            float edge = keep * (1.0 - smoothstep(t + 0.02, t + 0.08, n));
            return float2(keep, edge);
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
                float4 t = tex2D(_MainTex, i.uv);
                float3 c = t.rgb * _Tint.rgb;
                float heat = _HeatColor.a * pow(saturate(1.0 - i.uv.y / max(0.01, _HeatSpan)), 2.0);
                c = lerp(c, c * 0.45 + _HeatColor.rgb * 0.75, heat);
                float2 d = DissolveMask(i.uv);
                return half4(c, t.a * _Tint.a * _Fade * d.x);
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
                float a = tex2D(_MainTex, i.uv).a * _Fade;
                float2 d = DissolveMask(i.uv);
                float pulse = lerp(_PulseMin, 1.0, 0.5 + 0.5 * sin(_Time.y * 6.2831853 * _PulseSpeed));
                float rune = tex2D(_EmitTex, i.uv).r * pulse;
                float3 g = _RuneColor.rgb * rune * d.x + _EdgeColor.rgb * d.y * 1.6 + _SilColor.rgb * d.x;
                return half4(g * a, 1.0);
            }
            ENDCG
        }
    }
}
