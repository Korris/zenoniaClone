// Soft light blob on a quad MeshRenderer (Built-in pipeline): the fake ground light that replaces URP's Light2D, the meteor head and the impact ring.
// Mask = grey _MainTex (red channel; white = no texture) x radial falloff exp(-r^2 * _Falloff) (0 = texture only).
// Pass 1 alpha-blends the tint _BodyColor (what makes the light visible on white snow, where adding light changes nothing),
// pass 2 adds [HDR] _EmissionColor (the glow on dark ground; above 1.25 it blooms). _Fade (0..1) per renderer through a MaterialPropertyBlock.
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
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float _Falloff, _CoreWhite, _Fade;
        float4 _EmissionColor, _BodyColor;

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
            return tex2D(_MainTex, uv).r * fall * _Fade;
        }
        ENDCG

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBody
            half4 fragBody(v2f i) : SV_Target { return half4(_BodyColor.rgb, saturate(Mask(i.uv)) * _BodyColor.a); }
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
                return half4(_EmissionColor.rgb * m + _CoreWhite * m * m * m, 1.0);
            }
            ENDCG
        }
    }
}
