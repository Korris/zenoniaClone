// Warrior material with a faint warm rim (ChatGPT v18: rim light 8-12 %) so the hero separates from the grass at 12.5-22 m.
Shader "Proto/HeroRim"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1) _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {} _Glossiness ("Smoothness", Range(0,1)) = 0.35 _Metallic ("Metallic", Range(0,1)) = 0.1
        _RimColor ("Rim", Color) = (1, 0.92, 0.75, 1) _RimPower ("Rim power", Range(1,8)) = 3 _RimAmount ("Rim amount", Range(0,0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap; fixed4 _Color, _RimColor; half _Glossiness, _Metallic, _RimPower, _RimAmount;
        struct Input { float2 uv_MainTex; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb; o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Metallic = _Metallic; o.Smoothness = _Glossiness;
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _RimPower);
            o.Emission = _RimColor.rgb * rim * _RimAmount * 2.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
