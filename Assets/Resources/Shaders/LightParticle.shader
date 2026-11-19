// Glowing particle for ParticleSystemRenderer (Built-in pipeline): portal motes (MapPortalLightFx).
// Mask = grey _MainTex (red channel) x vertex alpha (the particle system's colour-over-lifetime fade); vertex rgb tints both passes.
// Pass 1 alpha-blends the saturated _BodyColor so a mote still shows on white snow (pure additive vanishes there, docs/sprite-rules.md Sổ lỗi 2026-11-16),
// pass 2 adds [HDR] _EmissionColor (+ a white core) for the BloomLite threshold (1.25) on dark ground.
Shader "ZenoniaProto/LightParticle"
{
    Properties
    {
        _MainTex ("Mask (grey)", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission (HDR)", Color) = (0.5, 1.2, 2, 1)
        _CoreWhite ("White-hot centre", Float) = 0.6
        _BodyColor ("Tint (alpha = strength)", Color) = (0.1, 0.4, 1, 0.6)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float _CoreWhite;
        float4 _EmissionColor, _BodyColor;

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
        ENDCG

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBody
            half4 fragBody(v2f i) : SV_Target
            {
                float m = tex2D(_MainTex, i.uv).r * i.color.a;
                return half4(_BodyColor.rgb * i.color.rgb, saturate(m * 1.4) * _BodyColor.a);
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
                float m = tex2D(_MainTex, i.uv).r * i.color.a;
                return half4(_EmissionColor.rgb * i.color.rgb * m + _CoreWhite * m * m * m, 1.0);
            }
            ENDCG
        }
    }
}
