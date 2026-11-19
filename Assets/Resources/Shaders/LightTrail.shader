// Meteor tail for a TrailRenderer (Built-in pipeline). Trail UV: x along the tail (Stretch: 0 head .. 1 end), y across (0..1).
// The colour comes from the trail's colour gradient (white core -> ice blue -> transparent); a swirl noise scrolls along the tail.
// Pass 1 alpha-blends a saturated _BodyColor (so the tail reads on snow), pass 2 adds the HDR light ([HDR] _EmissionColor scales the gradient).
Shader "ZenoniaProto/LightTrail"
{
    Properties
    {
        _NoiseTex ("Swirl noise (grey)", 2D) = "gray" {}
        _NoiseTiling ("Noise tiling (along, across)", Vector) = (2, 1, 0, 0)
        _NoiseScroll ("Noise scroll (uv/s)", Float) = 2.5
        [HDR] _EmissionColor ("Emission multiplier (HDR)", Color) = (1.6, 1.9, 2.6, 1)
        _CoreWhite ("White-hot centre line", Float) = 1.2
        _BodyColor ("Body under the light (alpha = strength)", Color) = (0.1, 0.42, 1, 0.55)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _NoiseTex;
        float4 _NoiseTiling, _EmissionColor, _BodyColor;
        float _NoiseScroll, _CoreWhite;

        struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.color = v.color;
            o.uv = v.uv;
            return o;
        }

        // x = tail intensity, y = centre line
        float2 Tail(v2f i)
        {
            float across = 1.0 - abs(i.uv.y * 2.0 - 1.0);
            float n = tex2D(_NoiseTex, i.uv * _NoiseTiling.xy + float2(_Time.y * _NoiseScroll, 0.0)).r;
            float body = pow(saturate(across), 1.3) * (0.7 + 0.6 * n) * i.color.a;
            float core = pow(saturate(across), 4.0) * i.color.a * i.color.a;
            return float2(body, core);
        }
        ENDCG

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBody
            half4 fragBody(v2f i) : SV_Target { return half4(_BodyColor.rgb, saturate(Tail(i).x * 1.4) * _BodyColor.a); }
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
                float2 t = Tail(i);
                return half4(i.color.rgb * _EmissionColor.rgb * t.x + _CoreWhite * t.y, 1.0);
            }
            ENDCG
        }
    }
}
